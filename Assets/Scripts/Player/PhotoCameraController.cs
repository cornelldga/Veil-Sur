using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls camera zoom, UI, and photo capture logic for
/// the player's photo camera.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PhotoCameraController : SoundEvent
{
    public enum CameraMode
    {
        Photo
    }

    private Camera targetCamera;
    [Header("Camera Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;

    [Tooltip("How many seconds must pass before taking another photo.")]
    [SerializeField] private float photoCooldown = 0.79f;
    [SerializeField] private bool toggleZoom = false;
    [Tooltip("The maximum range that a subject can be from the camera")]
    [Header("Detection")]
    [SerializeField] private float maxPhotoRange = 10f;
    [SerializeField] private LayerMask photoOcclusionMask = ~0;
    [Header("Blur Settings")]
    [Tooltip("Distance past maxPhotoRange where the zoom blur reaches full strength")]
    [SerializeField] private float blurRangePastMax = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip shutterClip;
    [SerializeField] private float shutterDelay = 0.1f;
    [Header("Mutant-Related Settings")]
    [SerializeField] private float detectRadius = 10f; // How far the sound cone extends
    [SerializeField] private float stunRadius = 10f; // How far the stun cone extends
    [Header("Mutant Stun Settings")]
    [Range(0, 360)]
    [SerializeField] private float coneAngle = 60f; // Total width of the cone in degrees
    [SerializeField] private LayerMask targetMask; // Layer containing enemies or interactable items
    [SerializeField] private LayerMask obstacleMask; // Layer containing walls/blocking objects

    // private const int RaysPerCircle = 8;
    private const string playerLayerName = "Player";
    private const string cameraIgnoreLayerName = "CameraIgnore";
    private const float ErrorShakeDuration = 0.25f; // Seconds the camera shakes when an error is shown
    private const float ErrorShakeMagnitude = 0.1f; // Max distance the camera is offset while shaking
    private PlayerControls controls;
    private PlayerState playerState;
    private float targetFOV;
    private Volume blurVolume;
    private Coroutine shakeCoroutine;
    private float lastPhotoTime = float.NegativeInfinity;
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
        // Followed the same logic for colliders we want to ignore
        int cameraIgnoreLayer = LayerMask.NameToLayer(cameraIgnoreLayerName);
        if (cameraIgnoreLayer != -1)
        {
            rayMask &= ~(1 << cameraIgnoreLayer);
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

        ErrorNotifier.OnError += OnErrorShown;
    }

    private void OnDisable()
    {
        ErrorNotifier.OnError -= OnErrorShown;

        controls.PlayerMovement.AimCamera.performed -= OnAimCameraPerformed;
        controls.PlayerMovement.AimCamera.canceled -= OnAimCameraCanceled;
        controls.PlayerMovement.TakePicture.performed -= OnSnap;

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
    }

    private void OnSnap(InputAction.CallbackContext ctx)
    {
        if (!playerState.GetPhotoMode())
        {
            return;
        }

        if (Time.unscaledTime - lastPhotoTime < photoCooldown)
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
            RegisterSoundEvent(transform.position, detectRadius, "Player");
        }

        lastPhotoTime = Time.unscaledTime;
        RenderTexture captureRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        Texture2D photo = CapturePhoto(captureRT);
        RenderTexture.ReleaseTemporary(captureRT);
        //AudioManager.Instance.PlaySfxAtPosition(shutterClip, targetCamera.transform.position);
        StartCoroutine(PlayShutterDelayed());
        UIManager.Instance.TakePhoto();
        PhotoStorage.Instance.CreatePhoto(photo, DetectPhotographedSubject());
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
    /// Returns a Texture2D (not a Sprite) of the screen of targetCamera.
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
    /// Returns the screen-space rect (in pixels) that crops the final photo.
    /// </summary>
    private RectInt GetCaptureScreenRect(int screenWidth, int screenHeight)
    {
        int width = screenHeight * 198 / 137;
        int xOffset = (screenWidth - width) / 2;
        return new RectInt(xOffset, 0, width, screenHeight);
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
        Ray centerRay = new(targetCamera.transform.position, targetCamera.transform.forward);

        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxPhotoRange, rayMask, QueryTriggerInteraction.Ignore))
        {
            PhotographableObject centeredSubject = centerHit.collider.GetComponentInParent<PhotographableObject>();

            Debug.DrawLine(centerRay.origin, centerHit.point, Color.cyan, 5f, false);

            if (centeredSubject != null)
            {
                return centeredSubject.SubjectId;
            }
        } 
        
        return "";
    }

    /// <summary>
    /// On camera snap, stuns any mutants detected in a cone in front of the player.
    /// </summary>
    private void StunMutantsInCone()
    {
        // 1. Gather all targets within a circular range (cheap physics test)
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, stunRadius, targetMask);

        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            if (!targetsInRadius[i].CompareTag("Mutant"))
            {
                continue;
            }

            Transform target = targetsInRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            EnemySearchlight mutant = target.gameObject.GetComponent<EnemySearchlight>();
            mutant.Stun();
            // Debug.Log("Attempted stun");

            // 2. Check if the target is within the forward alignment angle
            // if (Vector3.Angle(transform.forward, dirToTarget) < coneAngle / 2)
            // {
            //     float dstToTarget = Vector3.Distance(transform.position, target.position);
            //     EnemySearchlight mutant = target.gameObject.GetComponent<EnemySearchlight>();
            //     mutant.Stun();
            //     // 3. Optional: Line-of-sight raycast to ensure a wall isn't blocking the view
            //     // if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
            //     // {
            //     //     // Debug.Log($"Detected: {target.name} inside the vision cone!");
            //     //     EnemySearchlight mutant = target.gameObject.GetComponent<EnemySearchlight>();
            //     //     mutant.Stun();
            //     // }
            // }
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
    }

    /// <summary>
    /// Plays shutter sound after slight delay to match the vfx.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayShutterDelayed()
    {
        if (shutterDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(shutterDelay);
        }
        AudioManager.Instance.PlaySfxAtPosition(shutterClip, targetCamera.transform.position);
    }
}
