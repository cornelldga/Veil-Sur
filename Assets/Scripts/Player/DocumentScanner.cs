using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Scans the open document into a photo when the player presses the take
/// picture button (left click) while a document is open.
/// </summary>
public class DocumentScanner : MonoBehaviour
{
    [SerializeField] private AudioClip documentScanSfx;

    private PlayerControls controls;
    private PlayerState playerState;
    private Camera targetCamera;
    private bool isScanning = false;

    private void Awake()
    {
        controls = GameManager.Controls;
    }

    private void Start()
    {
        playerState = GameManager.PlayerInstance.GetComponent<PlayerState>();
        targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();
        controls.PlayerMovement.TakePicture.performed += OnScan;
    }

    private void OnDisable()
    {
        controls.PlayerMovement.TakePicture.performed -= OnScan;
    }

    /// <summary>
    /// Starts scanning a document is open, the player is not already in the camera or notebook, and storage has space.
    /// </summary>
    private void OnScan(InputAction.CallbackContext ctx)
    {
        if (isScanning || !playerState.GetDocOpen() || playerState.GetPhotoMode() || playerState.GetNotebookOpen())
        {
            return;
        }

        if (PhotoStorage.Instance.IsPhotoStorageFull())
        {
            ErrorNotifier.Show("ERROR: camera full");
            return;
        }

        StartCoroutine(ScanDocument());
    }

    /// <summary>
    /// Coroutine to manage the document scanner.
    /// Plays the scan sound and visual.
    /// Then, gets the rectangle/paper of the DocView in coordinates.
    /// Creates a new texture the size of the paper.
    /// Finally, creates a photo by copying the paper into a scan.
    /// Fails if the document is closed before scanning is finished.
    /// </summary>
    private IEnumerator ScanDocument()
    {
        isScanning = true;
        try
        {
            AudioManager.Instance.PlaySfxAtPosition(documentScanSfx, targetCamera.transform.position);

            DocumentScanVisual scanVisual = UIManager.Instance.docPaper.GetComponent<DocumentScanVisual>();

            if (scanVisual != null)
            {
                yield return scanVisual.Play();
            }

            yield return new WaitForEndOfFrame();

            if (!playerState.GetDocOpen())
            {
                yield break;
            }

            Vector3[] corners = new Vector3[4];
            UIManager.Instance.docPaper.GetWorldCorners(corners);
            Rect paperRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

            Texture2D scan = new Texture2D((int)paperRect.width, (int)paperRect.height, TextureFormat.RGB24, false);

            scan.ReadPixels(paperRect, 0, 0);
            scan.Apply();

            Debug.Log("SUBJECTID of Doc: " + playerState.GetOpenDocSubjectId());
            PhotoStorage.Instance.CreatePhoto(scan, playerState.GetOpenDocSubjectId());
        }
        finally
        {
            isScanning = false;
        }
    }
}
