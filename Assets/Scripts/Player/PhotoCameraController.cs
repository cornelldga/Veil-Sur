using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Controls camera zoom, UI, and photo capture logic for
/// the player's photo camera.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PhotoCameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Photo,
        Document
    }
    private Camera targetCamera;
    [Header("Camera Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private bool toggleZoom = false;
    [Tooltip("The maximum range that a subject can be from the camera")]
    [Header("Detection")]
    [SerializeField] private float maxPhotoRange = 10f;
    [Tooltip("How far away (in meters) a document can be read in document mode")]
    [SerializeField] private float maxDocReadRange = 3f;
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

    [Header("Audio")]
    [SerializeField] private AudioClip shutterClip;
    [Header("Mutant Stun Settings")]
    [SerializeField] private float maxRadius = 10f; // How far the cone extends
    [Range(0, 360)]
    [SerializeField] private float coneAngle = 60f; // Total width of the cone in degrees
    [SerializeField] private LayerMask targetMask; // Layer containing enemies or interactable items
    [SerializeField] private LayerMask obstacleMask; // Layer containing walls/blocking objects

    private const int RaysPerCircle = 8;
    private const string playerLayerName = "Player";
    private const float ErrorShakeDuration = 0.25f; // Seconds the camera shakes when an error is shown
    private const float ErrorShakeMagnitude = 0.1f; // Max distance the camera is offset while shaking
    private PlayerControls controls;
    private PlayerState playerState;
    private float targetFOV;
    private Volume blurVolume;
    private CameraMode currentMode = CameraMode.Photo;
    private Document hoveredDocument;
    private Coroutine shakeCoroutine;
    private int rayMask;
    
    private void Awake()
    {
        // controls = new PlayerControls();
        //switch to this for key binds
        controls = GameManager.Controls;
        playerState = GetComponent<PlayerState>();
    }

    /// <summary>
    /// Gets the target camera, sets the starting FOV, builds the camera
    /// raycast mask, and creates the blur volume.
    /// </summary>
    private void Start()
    {        
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        targetFOV = normalFOV;

        // Camera rays skip the Player layer so they can't hit the player's own colliders
        rayMask = photoOcclusionMask;
        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer != -1)
        {
            rayMask &= ~(1 << playerLayer);
        }

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

        controls.PlayerMovement.SwapCameraMode.performed += OnSwapCameraMode;

        ErrorNotifier.OnError += OnErrorShown;
    }

    private void OnDisable()
    {
        ErrorNotifier.OnError -= OnErrorShown;

        controls.PlayerMovement.AimCamera.performed -= OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled -= OnAimCameraCanceled;
        controls.PlayerMovement.TakePicture.performed -= OnSnap;

        controls.PlayerMovement.SwapCameraMode.performed -= OnSwapCameraMode;

        controls.PlayerMovement.Disable();
    }

    private void OnAimCameraPerformed(InputAction.CallbackContext ctx)
    {
        if (toggleZoom && playerState.GetPhotoMode())
        {
            CancelCamera();
            return;
        }
        if (!playerState.GetPlayerHasControl())
        {
            return;
        }
        targetFOV = zoomedFOV;
        blurVolume.weight = 1f;
        GameManager.Instance.state = GameManager.GameState.CAMERA;
        playerState.SetPhotoMode(true);
    }

    private void OnAimCameraCanceled(InputAction.CallbackContext ctx)
    {
        if (toggleZoom) { return; }
        CancelCamera();
        GameManager.Instance.state = GameManager.GameState.DEFAULT;
    }

    public void CancelCamera()
    {
        targetFOV = normalFOV;
        blurVolume.weight = 0f;
        playerState.SetPhotoMode(false);
        playerState.SetPhotoMode(false);
        HideDocPreview();
    }

    /// <summary>
    /// Function to swap between Photo and Document modes. 
    /// Activates only while camera is being aimed, on Q press.
    /// </summary>
    private void OnSwapCameraMode(InputAction.CallbackContext ctx)
    {
        if (!playerState.GetPhotoMode())
        {
            return;
        }

        // Currently, just swap between two modes with Q
        // In the future with more camera modes,
        // THIS would need to change ***.
        currentMode = (currentMode == CameraMode.Photo) ?
        CameraMode.Document : CameraMode.Photo;

        UIManager.Instance.SetCameraMode(currentMode);

        if (currentMode == CameraMode.Photo)
        {
            HideDocPreview();
        }
    }

    private void OnSnap(InputAction.CallbackContext ctx)
    {
        if (!playerState.GetPhotoMode())
        {
            return;
        }
        if (PhotoStorage.Instance.IsPhotoStorageFull())
        {
            ErrorNotifier.Show("ERROR: camera full");
            return;
        }
        else
        {
            StunMutantsInCone();
        }

        if (currentMode == CameraMode.Document && hoveredDocument != null)
        {
            StartCoroutine(ScanDocument());
            return;
        }

        RenderTexture captureRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        Texture2D photo = CapturePhoto(captureRT);
        RenderTexture.ReleaseTemporary(captureRT);
        AudioManager.Instance.PlaySfxAtPosition(shutterClip, targetCamera.transform.position);
        UIManager.Instance.TakePhoto();
        CreatePhotoNote(photo);
    }

    private Vector3 shakeOrigin = Vector3.zero;

    /// <summary>
    /// Called whenever any error message is shown through ErrorNotifier.
    /// Shakes camera and brings camera back to origin so it doesn't get offset
    /// </summary>
    /// <param name="message">Error text being displayed (unused).</param>
    private void OnErrorShown(string message)
    {
        if (targetCamera == null)
        {
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            targetCamera.transform.localPosition = shakeOrigin;
        }

        shakeOrigin = targetCamera.transform.localPosition;
        shakeCoroutine = StartCoroutine(ShakeCamera(ErrorShakeDuration, ErrorShakeMagnitude));
    }

    /// <summary>
    /// camera shake
    /// </summary>
    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float fade = 1f - t / duration;
            targetCamera.transform.localPosition = shakeOrigin + Vector3.right * Mathf.Sin(t * 40f) * magnitude * fade;
            yield return null;
        }

        targetCamera.transform.localPosition = shakeOrigin;
        shakeCoroutine = null;
    }
    /// <summary>
    /// Creates backend Photo data from a captured texture.
    /// This way, seperate camera modes can produce separate Textures.
    /// </summary>
    /// <param name="photo">Texture2D object representing the photo</param>
    private void CreatePhotoNote(Texture2D photo)
    {
        Photo photoNote = new GameObject("Photo").AddComponent<Photo>();
        photoNote.SetSubject(DetectPhotographedSubject());
        photoNote.SetImage(photo);

        // Add photo to storage
        if (!PhotoStorage.Instance.AddPhoto(photoNote))
        {
            Destroy(photoNote.gameObject);
            Destroy(photo);
        }
    }

    /// <summary>
    /// Coroutine to manage the document scanner.
    /// Plays the shutter sound and hides the viewfinder for the captured frame, then shows it again if the game is still in the camera state.
    /// Gets the rectangle/paper of the DocView in coordinates.
    /// Creates a new texture the size of the paper.
    /// Finally, copies the paper into the scan, and creates a photo note.
    /// </summary>
    private IEnumerator ScanDocument()
    {
        AudioManager.Instance.PlaySfxAtPosition(shutterClip, targetCamera.transform.position);
        UIManager.Instance.cameraGroup.SetActive(false);
        yield return new WaitForEndOfFrame();

        Vector3[] corners = new Vector3[4];
        UIManager.Instance.docPaper.GetWorldCorners(corners);
        Rect paperRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

        Texture2D scan = new Texture2D((int)paperRect.width, (int)paperRect.height, TextureFormat.RGB24, false);
        
        scan.ReadPixels(paperRect, 0, 0);
        scan.Apply();

        CreatePhotoNote(scan);
        UIManager.Instance.cameraGroup.SetActive(GameManager.Instance.state == GameManager.GameState.CAMERA);
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
    /// Raycasts from the camera while in Document mode (similar to interaction).
    /// Opens a hovered Document, and will close if the ray moves away.
    /// The ray ignores the Player layer so it can't hit the player's own colliders.
    /// The ray only reaches maxDocReadRange, so documents must be read up close.
    /// </summary>
    private void UpdateDocPreview()
    { 
        Document newHoveredDocument = null;

        if (Physics.Raycast(targetCamera.transform.position, targetCamera.transform.forward, out RaycastHit hit, maxDocReadRange, rayMask, QueryTriggerInteraction.Ignore))
        {
            newHoveredDocument = hit.collider.GetComponent<Document>();
        }
        if (newHoveredDocument != hoveredDocument && hoveredDocument != null)
        {
            hoveredDocument.CloseDoc();
        }
        hoveredDocument = newHoveredDocument;

        if (hoveredDocument != null)
        {
            hoveredDocument.OpenDoc();
        }
    }

    private void HideDocPreview()
    {
        if (hoveredDocument != null)
        {
            hoveredDocument.CloseDoc();
            hoveredDocument = null;
        }
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
    /// minRayCasts hits. The rays ignore the Player layer so they can't hit
    /// the player's own colliders.
    /// </summary>
    /// <returns>The photographed SubjectId, or an empty string if no such subject exists.</returns>
    private string DetectPhotographedSubject()
    {
        var tally = new Dictionary<string, int>();
        var tallyInner = new Dictionary<string, bool>();

        foreach (var (ray, isInner) in GetPhotoRays())
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, maxPhotoRange, rayMask, QueryTriggerInteraction.Ignore))
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

        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxPhotoRange, rayMask, QueryTriggerInteraction.Ignore))
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
    /// On camera snap, stuns any mutants detected in a cone in front of the player.
    /// </summary>
    private void StunMutantsInCone()
    {
        // 1. Gather all targets within a circular range (cheap physics test)
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, maxRadius, targetMask);

        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            if (!targetsInRadius[i].CompareTag("Mutant"))
            {
                continue;
            }

            Transform target = targetsInRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            // 2. Check if the target is within the forward alignment angle
            if (Vector3.Angle(transform.forward, dirToTarget) < coneAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                // 3. Optional: Line-of-sight raycast to ensure a wall isn't blocking the view
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    // Debug.Log($"Detected: {target.name} inside the vision cone!");
                    EnemySearchlight mutant = target.gameObject.GetComponent<EnemySearchlight>();
                    mutant.Stun();
                }
            }
        }
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

        if (GameManager.Instance.state != GameManager.GameState.CAMERA)
        {
            CancelCamera();
        }

        // Update FOV change
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);

        if (playerState.GetPhotoMode() && currentMode == CameraMode.Document)
        {
            UpdateDocPreview();
        }
    }
}
