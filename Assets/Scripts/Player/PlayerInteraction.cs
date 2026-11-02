using UnityEngine;

/// <summary>
/// Raycasts from the player's camera to find Interactable objects, shows the
/// interact prompt while one is targeted, and interacts with it on key press.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Document Raycast Features")]
    
    [Tooltip("The length of the raycast used to find readable documents")]
    [SerializeField] private float rayLength = 5f;

    private const string playerLayerName = "Player";
    private Camera camera;
    private PlayerControls controls;
    private PlayerState playerState;
    private int rayMask;

    // Hold interaction state
    private HoldInteractable heldTarget;
    private float holdTimer;
    private bool holdCompleted;

    private void Awake()
    {
        // controls = new PlayerControls();
        //switch to this for key binds
        controls = GameManager.Controls;
        camera = Camera.main;
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();
    }

    private void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    /// <summary>
    /// Gets the player's state and builds the interaction raycast mask.
    /// </summary>
    void Start()
    {
        playerState = GameManager.PlayerInstance.GetComponent<PlayerState>();

        // Interaction ray skips the Player layer so it can't hit the player's own colliders
        rayMask = Physics.DefaultRaycastLayers;
        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer != -1)
        {
            rayMask &= ~(1 << playerLayer);
        }
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If player interacts and the object
    /// has an interactable component, the interact action is successful.
    /// UI component will popup when looking at an interactable component.
    /// The ray ignores the Player layer so it can't hit the player's own colliders.
    /// </summary>
    private void Update()
    {
        if (playerState.GetPhotoMode() || playerState.GetNotebookOpen())
        {
            // Don't show 'E to Interact' when Camera is on or Notebook is open
            UIManager.Instance.ShowInteractPrompt(false);
            ResetHold();
            return; // Block interaction when Camera is open (Doc mode).
        }

        Interactable interactable = null;
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, rayLength, rayMask))
        { 
            interactable = hit.collider.GetComponentInParent<Interactable>();
        }
            UIManager.Instance.ShowInteractPrompt(interactable != null);
        if (interactable is HoldInteractable holdable)
        {
            UpdateHold(holdable);
            return;
        }

        ResetHold();
        if (controls.PlayerMovement.Interact.WasPerformedThisFrame() && interactable!= null) {
            interactable.Interact();
        }

    }

    /// <summary>
    /// Advances the hold timer while the interact key is held on a HoldInteractable,
    /// and interacts once the hold duration is reached. The key must be released
    /// before the same hold can trigger again.
    /// </summary>
    /// <param name="holdable">the hold interactable currently being looked at</param>
    private void UpdateHold(HoldInteractable holdable)
    {
        if (holdable != heldTarget)
        {
            ResetHold();
            heldTarget = holdable;
        }

        if (!controls.PlayerMovement.Interact.IsPressed())
        {
            ResetHold();
            return;
        }

        if (holdCompleted) return;

        holdTimer += Time.deltaTime;
        float progress = holdable.HoldDuration > 0f ? Mathf.Clamp01(holdTimer / holdable.HoldDuration) : 1f;
        holdable.OnHoldProgress(progress);

        if (progress >= 1f)
        {
            holdCompleted = true;
            holdable.Interact();
        }
    }

    /// <summary>
    /// Cancels any in-progress hold and notifies the held target.
    /// </summary>
    private void ResetHold()
    {
        if (heldTarget != null && holdTimer > 0f)
        {
            heldTarget.OnHoldReleased();
        }
        heldTarget = null;
        holdTimer = 0f;
        holdCompleted = false;
    }


    // private void OnDestroy()
    // {
    //     controls?.Dispose();
    // }
}
