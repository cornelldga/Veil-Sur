using UnityEngine;

/// <summary>
/// Drives first-person control through CharacterController
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStateController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float standSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 8f;
     // 2x normal gravity, snappier feel
    [SerializeField] private float gravity = -19.62f;

    [Header("View")]
    [SerializeField] private float mouseSens = 0.15f;
    [SerializeField] private float minLookDown = -85f;
    [SerializeField] private float maxLookUp = 85f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 15f;
     // What objects block uncrouching
    [SerializeField] private LayerMask ceilingCheckMask = ~0;

    private Camera playerCamera;
    private CharacterController controller;
    private PlayerControls controls;
    private PlayerStateController playerStateController;

    private float verticalLookClamped;
    private float verticalVelocity;
    private float currentHeight;
    private Vector3 cameraStandLocalPos;
    private Vector3 cameraCrouchLocalPos;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
        playerStateController = GetComponent<PlayerStateController>();

        currentHeight = standHeight;
        controller.height = standHeight;

        cameraStandLocalPos = new Vector3(0f, standHeight * 0.5f, 0f);
        cameraCrouchLocalPos = new Vector3(0f, crouchHeight * 0.5f, 0f);

        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = cameraStandLocalPos;
        }
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    private void Update()
    {
        HandleMove();
        HandleLook();
        HandleCrouch();
    }

    /// <summary>
    /// Move the player depending on the movement input at either crouch, normal,
    /// or sprint speed. Uses character controller component to move.
    /// </summary>
    private void HandleMove()
    {
        bool sprintHeld = controls.PlayerMovement.Sprint.IsPressed();

        // Crouch takes precedence over sprinting
        float speed = sprintHeld ? sprintSpeed : standSpeed;
        speed = playerStateController.GetCrouching() ? crouchSpeed : speed;

        Vector2 moveInput = controls.PlayerMovement.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= speed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            // Small downward force to keep grounded
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// Move the camera depending on the look input. Rotates the player object left
    /// and right but not up and down, and clamps up and down minimum and maximum.
    /// </summary>
    private void HandleLook()
    {
        Vector2 lookInput = controls.PlayerMovement.Look.ReadValue<Vector2>();
        float horizontalLook = lookInput.x * mouseSens;
        float verticalLook = lookInput.y * mouseSens;

        transform.Rotate(Vector3.up * horizontalLook);

        verticalLookClamped = Mathf.Clamp(verticalLookClamped - verticalLook, minLookDown, maxLookUp);
        if (playerCamera != null)
        {
            playerCamera.transform.localEulerAngles = new Vector3(verticalLookClamped, 0f, 0f);
        }
    }

    /// <summary>
    /// Handles crouching motion. The player can only stop crouching if the space
    /// above them is unblocked. Moves camera and changes controller height.
    /// </summary>
    private void HandleCrouch()
    {
        bool crouchHeld = controls.PlayerMovement.Crouch.IsPressed();
        float targetHeight = crouchHeld ? crouchHeight : standHeight;

        // Don't let the player stand up into a low ceiling
        if (!crouchHeld && targetHeight > currentHeight)
        {
            float checkDistance = standHeight - currentHeight;
            Vector3 origin = transform.position + Vector3.up * currentHeight;
            if (Physics.Raycast(origin, Vector3.up, checkDistance, ceilingCheckMask))
            {
                targetHeight = currentHeight; // blocked, stay crouched
            }
        }

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = new Vector3(0f, currentHeight * 0.5f, 0f);

        bool actuallyCrouching = currentHeight < standHeight - 0.01;
        playerStateController.SetCrouching(actuallyCrouching);

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