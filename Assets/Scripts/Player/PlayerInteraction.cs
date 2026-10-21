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
        if (playerState.GetPhotoMode())
        {
            // Don't show 'E to Interact' when Camera is on
            UIManager.Instance.ShowInteractPrompt(false);
            return; // Block interaction when Camera is open (Doc mode).
        }

        Interactable interactable = null;
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, rayLength, rayMask))
        { 
            interactable = hit.collider.GetComponentInParent<Interactable>();
        }
            UIManager.Instance.ShowInteractPrompt(interactable != null);
        if (controls.PlayerMovement.Interact.WasPerformedThisFrame() && interactable!= null) {
            interactable.Interact();
        }
            
    }


    // private void OnDestroy()
    // {
    //     controls?.Dispose();
    // }
}
