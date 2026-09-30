using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Controls camera zoom, UI, and photo capture logic for
/// the player's photo camera.
/// </summary>
[RequireComponent(typeof(PlayerStateController))]
public class PhotoCameraController : MonoBehaviour
{
    private GameObject cameraUI;
    [SerializeField] private GameObject photographPrefab;
    private Camera targetCamera;
    [Header("Camera Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;
    [Tooltip("The maximum range that a subject can be from the camera")]
    [Header("Detection")]
    [SerializeField] private float maxPhotoRange = 10f;
    [SerializeField] private LayerMask photoOcclusionMask = ~0;
    [Tooltip("Inner circle radius in world units")]
    [SerializeField] private float innerRayRadius = 0.07f;
    [Tooltip("Outer circle radius in world units")]
    [SerializeField] private float outerRayRadius = 0.21f;
    [Tooltip("Minimum number of raycasts that must hit a subject for it to be considered the subject of the photo")]
    [SerializeField] private int minRayCasts = 7;
    [Header("Blur Settings")]
    [Tooltip("Distance past maxPhotoRange where the zoom blur reaches full strength")]
    [SerializeField] private float blurRangePastMax = 2f;

    private const int RaysPerCircle = 8;
    private PlayerControls controls;
    private PlayerStateController playerStateController;
    private GameObject notebookMenu;
    private float targetFOV;
    private Image snapOverlay;
    private Volume blurVolume;
    private void Awake()
    {
        controls = new PlayerControls();
        playerStateController = GetComponent<PlayerStateController>();
    }

    private void Start()
    {
        notebookMenu = playerStateController.GetNotebookMenu();
        snapOverlay = UIManager.Instance.snapOverlay;
        snapOverlay.canvasRenderer.SetAlpha(0f);

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        targetFOV = normalFOV;
        cameraUI = UIManager.Instance.cameraGroup;
        cameraUI.SetActive(false);

        CreateBlurVolume();
    }

    /// <summary>
    /// Creates a runtime Volume with a Gaussian Depth of Field override used
    /// to blur anything past maxPhotoRange - 1.5f while zoomed in
    /// </summary>
    private void CreateBlurVolume()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        DepthOfField depthOfField = profile.Add<DepthOfField>(true);

        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Gaussian;

        depthOfField.gaussianStart.overrideState = true;
        depthOfField.gaussianStart.value = maxPhotoRange - 1.5f;

        depthOfField.gaussianEnd.overrideState = true;
        depthOfField.gaussianEnd.value = maxPhotoRange + blurRangePastMax;

        GameObject blurVolumeObject = new GameObject("PhotoBlurVolume");
        blurVolumeObject.transform.SetParent(transform, false);
        blurVolume = blurVolumeObject.AddComponent<Volume>();
        blurVolume.isGlobal = true;
        blurVolume.priority = 10f;
        blurVolume.weight = 0f;
        blurVolume.sharedProfile = profile;
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();

        controls.PlayerMovement.AimCamera.performed += OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled += OnAimCameraCanceled;
        controls.PlayerMovement.TakePicture.performed += OnSnap;
    }

    private void OnDisable()
    {
        controls.PlayerMovement.AimCamera.performed -= OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled -= OnAimCameraCanceled;
        controls.PlayerMovement.TakePicture.performed -= OnSnap;

        controls.PlayerMovement.Disable();
    }

    private void OnAimCameraPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerStateController.GetPlayerHasControl())
        {
            return;
        }

        targetFOV = zoomedFOV;
        blurVolume.weight = 1f;
        cameraUI.SetActive(true);
        playerStateController.SetPhotoMode(true);
        snapOverlay.CrossFadeAlpha(0f, 0f, true); //cancel prev fade if still running
    }

    private void OnAimCameraCanceled(InputAction.CallbackContext ctx)
    {
        CancelCamera();
    }

    public void CancelCamera()
    {
        targetFOV = normalFOV;
        blurVolume.weight = 0f;
        cameraUI.SetActive(false);
        playerStateController.SetPhotoMode(false);
    }

    private void OnSnap(InputAction.CallbackContext ctx)
    {
        if (!playerStateController.GetPhotoMode())
        {
            return;
        }

        if (PhotoStorage.Instance.IsPhotoStorageFull())
        {
            return;
        }

        RenderTexture captureRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        Texture2D photo = CapturePhoto(captureRT);
        RenderTexture.ReleaseTemporary(captureRT);

        //puts white  overlay over and then fades it out to simulate a camera snap
        snapOverlay.canvasRenderer.SetAlpha(.5f);
        snapOverlay.CrossFadeAlpha(0f, 0.2f, ignoreTimeScale: true);

        GameObject photograph = Instantiate(photographPrefab, notebookMenu.transform);
        PhotoNote photoNote = photograph.GetComponent<PhotoNote>();
        photoNote.SetSubject(DetectPhotographedSubject());
        photoNote.SetBounds(notebookMenu.transform as RectTransform);
        photoNote.LoadImage(photo);

        // Add photo to storage
        PhotoStorage.Instance.AddPhoto(photoNote);
    }

    /// <summary>
    /// Returns a square Texture2D (not a Sprite) of the center of
    /// the screen of targetCamera, cropping the sides but keeping
    /// the full height.
    /// </summary>
    private Texture2D CapturePhoto(RenderTexture captureRT)
    {
        var previousTarget = targetCamera.targetTexture;

        targetCamera.targetTexture = captureRT;
        targetCamera.Render();

        RectInt captureRect = GetCaptureScreenRect(captureRT.width, captureRT.height);

        RenderTexture.active = captureRT;
        var photo = new Texture2D(captureRect.width, captureRect.height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(captureRect.x, captureRect.y, captureRect.width, captureRect.height), 0, 0);
        photo.Apply();

        RenderTexture.active = null;
        targetCamera.targetTexture = previousTarget;
        return photo;
    }

    /// <summary>
    /// Returns the screen-space rect (in pixels) that gets cropped into the
    /// final photo: full height, centered horizontally.
    /// </summary>
    private RectInt GetCaptureScreenRect(int screenWidth, int screenHeight)
    {
        int squareSize = screenHeight;
        int xOffset = (screenWidth - squareSize) / 2;
        return new RectInt(xOffset, 0, squareSize, squareSize);
    }

    /// <summary>
    /// Raycasts to find the subject of the most recently taken photo.
    /// Returns the SubjectId with the most hits, as long as it has at least
    /// minRayCasts hits. Empty string if no such subject exists.
    /// </summary>
    private string DetectPhotographedSubject()
    {
        var tally = new Dictionary<string, int>();
        var tallyInner = new Dictionary<string, bool>();

        foreach (var (ray, isInner) in GetPhotoRays())
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, maxPhotoRange, photoOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(ray.origin, ray.direction * maxPhotoRange, Color.red, 5f, false);
                continue;
            }
            // Return parents too in case subject hits a child component of a photographable object (like lightbulb of lamp or smthn)
            PhotographableObject subject = hit.collider.GetComponentInParent<PhotographableObject>();
            Debug.DrawLine(ray.origin, hit.point, subject != null ? Color.green : Color.yellow, 5f, false);
            if (subject == null)
            {
                continue;
            }

            if (tally.ContainsKey(subject.SubjectId))
            {
                tally[subject.SubjectId] = tally[subject.SubjectId] + 1;
                if (isInner)
                {
                    tallyInner[subject.SubjectId] = true;
                }
            }
            else
            {
                tally[subject.SubjectId] = 1;
                tallyInner[subject.SubjectId] = isInner;
            }
        }

        Ray centerRay = new(targetCamera.transform.position, targetCamera.transform.forward);

        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxPhotoRange, photoOcclusionMask, QueryTriggerInteraction.Ignore))
        {
            PhotographableObject centeredSubject = centerHit.collider.GetComponentInParent<PhotographableObject>();

            Debug.DrawLine(centerRay.origin, centerHit.point, Color.cyan, 5f, false);

            if (centeredSubject != null && tally.TryGetValue(centeredSubject.SubjectId, out int hits) && hits >= minRayCasts - 1)
            {
                return centeredSubject.SubjectId;
            }
        } 
        else
        {
            Debug.DrawRay(centerRay.origin, centerRay.direction * maxPhotoRange, Color.cyan, 5f, false);
        }

        string bestSubjectId = "";
        int bestCount = minRayCasts - 1;
        foreach (var entry in tally)
        {
            if (tallyInner[entry.Key] && entry.Value > bestCount)
            {
                bestSubjectId = entry.Key;
                bestCount = entry.Value;
            }
        }

        return bestSubjectId;
    }

    /// <summary>
    /// Returns 16 rays (2 concentric circles of 8, evenly
    /// spaced) used when a photo is taken, centered on the
    /// cropped square.
    /// </summary>
    private IEnumerable<(Ray ray, bool isInner)> GetPhotoRays()
    {
        Transform cam = targetCamera.transform;
        float[] radii = { innerRayRadius, outerRayRadius };

        for (int circle = 0; circle < radii.Length; circle++)
        {
            for (int i = 0; i < RaysPerCircle; i++)
            {
                float angle = i * Mathf.PI * 2f / RaysPerCircle;
                Vector3 offset = radii[circle] * (cam.right * Mathf.Cos(angle) + cam.up * Mathf.Sin(angle));

                yield return (new Ray(cam.position + offset, cam.forward), circle == 0);
            }
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        // Update FOV change
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }
}
