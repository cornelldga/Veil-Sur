using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Document Raycast Features")]
    
    [Tooltip("The length of the raycast used to find readable documents")]
    [SerializeField] private float rayLength = 5f;

    private Camera camera;
    private PlayerControls controls;
    private PlayerStateController playerStateController;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();
    }

    private void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    void Start()
    {
        camera = GameManager.PlayerCamera;
        playerStateController = GameManager.PlayerInstance.GetComponent<PlayerStateController>();
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If player interacts and the object
    /// has an interactable component, the interact action is successful.
    /// UI component will popup when looking at an interactable component.
    /// </summary>
    private void Update()
    {
        if (playerStateController.GetPhotoMode())
        {
            // Don't show 'E to Interact' when Camera is on
            UIManager.Instance.ShowInteractPrompt(false); 
            return; // Block interaction when Camera is open (Doc mode).
        }

        Interactable interactable = null;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, rayLength))
        { 
            interactable = hit.collider.GetComponentInParent<Interactable>();
        }
            UIManager.Instance.ShowInteractPrompt(interactable != null);
        if (controls.PlayerMovement.Interact.WasPerformedThisFrame() & interactable!= null) {
            interactable.Interact();
        }
            
    }


    private void OnDestroy()
    {
        controls?.Dispose();
    }
}
