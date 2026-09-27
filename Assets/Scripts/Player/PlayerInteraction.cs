using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Document Raycast Features")]
    
    [Tooltip("The length of the raycast used to find readable documents")]
    [SerializeField] private float rayLength = 5f;

    private Camera camera;
    private PlayerControls controls;
    
    private Document _docController;

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
        camera = gameObject.GetComponent<FirstPersonController>().playerCamera;
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If player interacts and the object
    /// has an interactable component, the interact action is successful.
    /// </summary>
    private void Update()
    {
        if (Physics.Raycast(camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f)), transform.forward, out RaycastHit hit, rayLength))
        { 
            if (controls.PlayerMovement.Interact.WasPerformedThisFrame())
            {
                var interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }
}
