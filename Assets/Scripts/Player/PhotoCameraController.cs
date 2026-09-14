using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controls camera zoom, UI, and photo capture logic for
/// the player's photo camera.
/// </summary>
[RequireComponent(typeof(PlayerStateController))]
public class PhotoCameraController : MonoBehaviour
{
    [SerializeField] private Canvas cameraUI;
    [SerializeField] private GameObject photographPrefab;
    private Camera targetCamera;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;
    [Tooltip("The maximum range that a subject can be from the camera")]
    [SerializeField] private float maxPhotoRange = 10f;
    [SerializeField] private LayerMask photoOcclusionMask = ~0;
    [Tooltip("Radius of the inner ray circle used to determine subject")]
    [SerializeField] private float innerRadiusFraction = 0.3f;
    [Tooltip("Radius of the outer ray circle used to determine subject")]
    [SerializeField] private float outerRadiusFraction = 0.7f;

    // Minimum amount of raycasts needed for subject to be considering in photo
    private int minRayCasts = 4;
    private const int RaysPerCircle = 8;
    private PlayerControls controls;
    private PlayerStateController playerStateController;
    private GameObject notebookMenu;
    private float targetFOV;
    [SerializeField] private Image snapOverlay;
    private void Awake()
    {
        controls = new PlayerControls();
        playerStateController = GetComponent<PlayerStateController>();
        notebookMenu = playerStateController.GetNotebookMenu();
        snapOverlay.canvasRenderer.SetAlpha(0f);

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        targetFOV = normalFOV;
        cameraUI.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();

        controls.PlayerMovement.AimCamera.performed += OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled += OnAimCameraCanceled;
        controls.PlayerMovement.Interact.performed += OnSnap;
    }

    private void OnDisable()
    {
        controls.PlayerMovement.AimCamera.performed -= OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled -= OnAimCameraCanceled;
        controls.PlayerMovement.Interact.performed -= OnSnap;

        controls.PlayerMovement.Disable();
    }

    private void OnAimCameraPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerStateController.GetPlayerHasControl())
        {
            return;
        }

        targetFOV = zoomedFOV;
        cameraUI.gameObject.SetActive(true);
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
        cameraUI.gameObject.SetActive(false);
        playerStateController.SetPhotoMode(false);
    }

    private void OnSnap(InputAction.CallbackContext ctx)
    {
        if (!playerStateController.GetPhotoMode())
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
        photoNote.setBounds(notebookMenu.transform as RectTransform);
        photoNote.LoadImage(photo);
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

        foreach (Vector2 screenPoint in GetPhotoRayScreenPoints())
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxPhotoRange, photoOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            // Return parents too in case subject hits a child component of a photographable object (like lightbulb of lamp or smthn)
            PhotographableObject subject = hit.collider.GetComponentInParent<PhotographableObject>();
            if (subject == null)
            {
                continue;
            }

            if (tally.ContainsKey(subject.SubjectId))
            {
                tally[subject.SubjectId] = tally[subject.SubjectId] + 1;
            }
            else
            {
                tally[subject.SubjectId] = 1;
            }
        }

        string bestSubjectId = "";
        int bestCount = minRayCasts - 1;
        foreach (var entry in tally)
        {
            if (entry.Value > bestCount)
            {
                bestSubjectId = entry.Key;
                bestCount = entry.Value;
            }
        }

        return bestSubjectId;
    }

    /// <summary>
    /// Returns 16 screen-space points (2 concentric circles of 8, evenly
    /// spaced) used to raycast when a photo is taken, centered on the
    /// cropped square.
    /// </summary>
    private IEnumerable<Vector2> GetPhotoRayScreenPoints()
    {
        RectInt captureRect = GetCaptureScreenRect(Screen.width, Screen.height);
        Vector2 center = new Vector2(captureRect.x + captureRect.width * 0.5f, captureRect.y + captureRect.height * 0.5f);
        float halfSize = captureRect.width * 0.5f;

        // Iterate through two circles
        foreach (float radiusFraction in new[] { innerRadiusFraction, outerRadiusFraction })
        {
            float pixelRadius = radiusFraction * halfSize;
            // Iterate by points in circle
            for (int i = 0; i < RaysPerCircle; i++)
            {
                float angle = i * Mathf.PI * 2f / RaysPerCircle;
                yield return center + pixelRadius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }
}
