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
    [SerializeField] private LayerMask photoOcclusionMask = ~0;

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

        List<string> subjectIds = DetectPhotographedSubjects();
        GameObject photograph = Instantiate(photographPrefab, notebookMenu.transform);
        PhotoNote photoNote = photograph.GetComponent<PhotoNote>();
        photoNote.setBounds(notebookMenu.transform as RectTransform);
        photoNote.LoadImage(photo);
        foreach (string subjectId in subjectIds)
        {
            photoNote.AddSubject(subjectId);
        }
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
    /// Finds all PhotographableObjects in the scene that appear, at least
    /// partially unobstructed, within the cropped area of the last photo.
    /// </summary>
    private List<string> DetectPhotographedSubjects()
    {
        var subjectIds = new List<string>();
        var candidates = FindObjectsByType<PhotographableObject>(FindObjectsInactive.Exclude);

        foreach (PhotographableObject candidate in candidates)
        {
            if (IsSubjectInPhoto(candidate) && !subjectIds.Contains(candidate.SubjectId))
            {
                subjectIds.Add(candidate.SubjectId);
            }
        }

        return subjectIds;
    }

    /// <summary>
    /// Checks whether any representative point of the subject lands inside
    /// the captured square and has a clear line of sight to the camera.
    /// </summary>
    private bool IsSubjectInPhoto(PhotographableObject subject)
    {
        RectInt captureRect = GetCaptureScreenRect(Screen.width, Screen.height);
        Renderer subjectRenderer = subject.GetComponentInChildren<Renderer>();

        foreach (Vector3 point in GetTestPoints(subject.transform, subjectRenderer))
        {
            Vector3 screenPoint = targetCamera.WorldToScreenPoint(point);
            // In front of (not behind) screen
            if (screenPoint.z <= 0f)
            {
                continue;
            }
            // In frame
            if (!captureRect.Contains(new Vector2Int((int)screenPoint.x, (int)screenPoint.y)))
            {
                continue;
            }
            // Unblocked
            if (IsPointVisibleToCamera(point))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerable list of the points at the corners of a bounding
    /// box around the subject, based on it's renderer and transform.
    /// </summary>
    /// TODO: Review in testing whether we need more points or different system
    private IEnumerable<Vector3> GetTestPoints(Transform subjectTransform, Renderer subjectRenderer)
    {
        if (subjectRenderer == null)
        {
            yield return subjectTransform.position;
            yield break;
        }

        Bounds bounds = subjectRenderer.bounds;
        yield return bounds.center;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                }
            }
        }
    }

    /// <summary>
    /// Raycasts from the camera to worldPoint; the point is considered
    /// visible if nothing blocks the line of sight before reaching it.
    /// </summary>
    private bool IsPointVisibleToCamera(Vector3 worldPoint)
    {
        Vector3 origin = targetCamera.transform.position;
        Vector3 delta = worldPoint - origin;

        return !Physics.Raycast(origin, delta.normalized, delta.magnitude * 0.98f, photoOcclusionMask, QueryTriggerInteraction.Ignore);
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
