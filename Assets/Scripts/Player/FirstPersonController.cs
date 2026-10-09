using UnityEngine;

/// <summary>
/// Drives first-person control through CharacterController
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(StaminaController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float standSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float exhaustedSpeed = 4f; // Speed when out of stamina
    [SerializeField] private float gravity = -19.62f;

    [Header("View")]
    [SerializeField] private float mouseSens = 0.15f;
    [SerializeField] private float minLookDown = -85f;
    [SerializeField] private float maxLookUp = 85f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 15f;
    [SerializeField] private LayerMask ceilingCheckMask = ~0;

    private CharacterController controller;
    private PlayerControls controls;
    private PlayerState playerState;
    private StaminaController staminaController;

    private float verticalLookClamped;
    private float verticalVelocity;
    private float currentHeight;
    private Vector3 cameraStandLocalPos;
    private Vector3 cameraCrouchLocalPos;
    private bool isSprinting;

    public Camera playerCamera;

    private void Start()
    {
        // Moved this to a Start() function 
        Level.current_level.player = gameObject;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
        staminaController = GetComponent<StaminaController>();
        playerState = GetComponent<PlayerState>();

        currentHeight = standHeight;
        controller.height = standHeight;

        cameraStandLocalPos = new Vector3(0f, standHeight * 0.5f, 0f);
        cameraCrouchLocalPos = new Vector3(0f, crouchHeight * 0.5f, 0f);

        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = cameraStandLocalPos;
        }

        // Send player instance/camera to GameManager
        GameManager.PlayerInstance = gameObject;
        
        GameManager.PlayerCamera = playerCamera;
    }

    private void OnEnable()
    {
        if (controls != null) controls.PlayerMovement.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if(controls != null) controls.PlayerMovement.Disable();
    }

    private void Update()
    {
        if (playerState.GetPlayerHasControl())
        {
            HandleMove();
            HandleLook();
            HandleCrouch();
        }
    }

    /// <summary>
    /// Move the player depending on movement input, crouching, or stamina state.
    /// </summary>
    private void HandleMove()
    {
        bool sprintHeld = controls.PlayerMovement.Sprint.IsPressed();
        bool sprintPressedThisFrame = controls.PlayerMovement.Sprint.WasPressedThisFrame();
        
        Vector2 moveInput = controls.PlayerMovement.Move.ReadValue<Vector2>();
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Track whether the player released the sprint key
        if (!sprintHeld)
        {
            isSprinting = false;
        }

        // Can only start sprinting if the key was released first and not exhausted
        if (sprintPressedThisFrame && isMoving && !staminaController.isExhausted)
        {
            isSprinting = true;
        }

        // Cancel sprinting immediately if stamina hits zero or player stops moving
        if (staminaController.isExhausted || !isMoving)
        {
            isSprinting = false;
        }

        playerState.SetMoving(isMoving);
        playerState.SetSprinting(isSprinting);

        float speed;

        if (playerState.GetCrouching())
        {
            // 1. Crouch takes top priority
            speed = crouchSpeed;
            staminaController.RegenerateStamina();
        }
        else if (staminaController.isExhausted)
        {
            // 2. Out of stamina penalty: only recovers once sprint is released
            speed = exhaustedSpeed;
            staminaController.RegenerateStamina();
        }
        else if (isSprinting)
        {
            // 3. Actively sprinting
            speed = sprintSpeed;
            staminaController.DrainStamina();
        }
        else
        {
            // 4. Normal walking/standing: only recovers if not holding sprint
            speed = standSpeed;
            if (!sprintHeld) staminaController.RegenerateStamina();
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= speed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    public void SetRunSpeed(float speed)
    {
        sprintSpeed = speed;
    }

    /// <summary>
    /// Move the camera depending on look input.
    /// </summary>
    private void HandleLook()
    {
        Vector2 lookInput = controls.PlayerMovement.Look.ReadValue<Vector2>();
        float sens = mouseSens * GameManager.Instance.MouseSensitivity;
        float horizontalLook = lookInput.x * sens;
        float verticalLook = lookInput.y * sens;

        transform.Rotate(Vector3.up * horizontalLook);

        verticalLookClamped = Mathf.Clamp(verticalLookClamped - verticalLook, minLookDown, maxLookUp);
        if (playerCamera != null)
        {
            playerCamera.transform.localEulerAngles = new Vector3(verticalLookClamped, 0f, 0f);
        }
    }

    /// <summary>
    /// Handles crouching motion and ceiling checks.
    /// </summary>
    private void HandleCrouch()
    {
        bool crouchHeld = controls.PlayerMovement.Crouch.IsPressed();
        float targetHeight = crouchHeld ? crouchHeight : standHeight;

        if (!crouchHeld && targetHeight > currentHeight)
        {
            float checkDistance = standHeight - currentHeight;
            Vector3 origin = transform.position + Vector3.up * currentHeight;
            if (Physics.Raycast(origin, Vector3.up, checkDistance, ceilingCheckMask))
            {
                targetHeight = currentHeight;
            }
        }

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = new Vector3(0f, currentHeight * 0.5f, 0f);

        bool actuallyCrouching = currentHeight < standHeight - 0.01f;
        playerState.SetCrouching(actuallyCrouching);

        if (playerCamera != null)
        {
            Vector3 targetCamPos = Vector3.Lerp(cameraCrouchLocalPos, cameraStandLocalPos,
                (currentHeight - crouchHeight) / (standHeight - crouchHeight));
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition, targetCamPos, crouchTransitionSpeed * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
       controls?.Dispose();
    }
}
