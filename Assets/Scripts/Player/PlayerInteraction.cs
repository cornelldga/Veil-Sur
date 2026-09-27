using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Document Raycast Features")]
    
    [Tooltip("The length of the raycast used to find readable documents")]
    [SerializeField] private float rayLength = 5f;

    private Camera camera;
    private PlayerControls controls;

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
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If player interacts and the object
    /// has an interactable component, the interact action is successful.
    /// </summary>
    private void Update()
    {
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, rayLength))
        { 
            if (controls.PlayerMovement.Interact.WasPerformedThisFrame())
            {
                            Debug.Log("within range");
                Debug.Log($"Hit: {hit.collider.gameObject.name}");

                var doc = hit.collider.gameObject.GetComponent<Document>();
                Debug.Log($"Document component found: {doc != null}");

                var interactable = hit.collider.gameObject.GetComponent<Interactable>();
                Debug.Log($"Interactable component found: {interactable != null}");
                Debug.Log("within range");
                if (interactable != null)
                {
                    Debug.Log("interaction success");
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
