using UnityEngine;
using UnityEngine.InputSystem;

public class NotebookController : MonoBehaviour
{
    private PhotoCameraController photoCameraController;
    private PlayerControls controls;
    private PlayerState playerState;

    [Header("Audio")]
    [SerializeField] private AudioClip notebookOpenClip;

    private void Awake()
    {
        controls = new PlayerControls();
        playerState = GetComponent<PlayerState>();
        photoCameraController = GetComponent<PhotoCameraController>();
    }

    public GameObject GetNotebookMenu()
    {
        return UIManager.Instance.notebookGroup;
    }
    private void OnDisable()
    {
        controls.General.Notebook.performed -= ToggleNotebook;

        controls.General.Disable();
    }
    private void OnEnable()
    {
        controls.General.Enable();

        controls.General.Notebook.performed += ToggleNotebook;
    }
    /// <summary>
    /// When toggle notebook button is pressed, either open or close the notebook
    /// depending on if it is already open.
    /// </summary>
    private void ToggleNotebook(InputAction.CallbackContext ctx)
    {
        if (playerState.GetNotebookOpen())
        {
            GameManager.Instance.RequestStateChange(GameManager.GameState.DEFAULT);
        } else
        {
            GameManager.Instance.RequestStateChange(GameManager.GameState.NOTEBOOK);
        }

        bool notebookOpen = playerState.GetNotebookOpen();

        if (notebookOpen)
        {
            photoCameraController.CancelCamera();
            AudioManager.Instance.PlaySfxAtPosition(notebookOpenClip, transform.position);
        }

        playerState.SetPlayerHasControl(!notebookOpen);
        Cursor.lockState = notebookOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = notebookOpen;
    }
}
