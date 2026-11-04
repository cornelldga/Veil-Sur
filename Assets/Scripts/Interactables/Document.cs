using UnityEngine;
using TMPro;

[System.Serializable]
public class DocData
{
    public string text;
}


public class Document : MonoBehaviour, Interactable
{
    [Header("UI Features")]

    [Tooltip("The name of the JSON file in Resources/")]
    [SerializeField] private TextAsset jsonFile;

    private DocData docData;
    private TMP_Text docTextAreaUI;
    private GameObject docCanvas;

    private PhotographableObject subject;
    private PlayerState playerStateController;
    private bool isOpen = false;

    /// <summary>
    /// Awake() loads all of the JSON text into this document to display
    /// at instantiation.
    /// </summary>
    private void Awake()
    {
        LoadDocData();
        subject = GetComponent<PhotographableObject>();
    }

    private void Start()
    {
        docTextAreaUI = UIManager.Instance.docViewer;
        docCanvas = UIManager.Instance.docCanvas;
        playerStateController = GameManager.PlayerInstance.GetComponent<PlayerState>();
    }


    /// <summary>
    /// LoadDocData() reads and parses the JSON file. It stores the text
    /// of the document 
    /// </summary>
    private void LoadDocData()
    {
        if (jsonFile != null)
            docData = JsonUtility.FromJson<DocData>(jsonFile.text);
        else
            docData = new DocData { text = "" };
    }

    /// <summary>
    /// Opens or closes the doc viewer on interaction.
    /// </summary>
    public void Interact()
    {
        if (!isOpen)
        {
            OpenDoc();
        }
        else
        {
            CloseDoc();
        }
    }

    public void OpenDoc()
    {
        docTextAreaUI.text = docData.text;
        docCanvas.SetActive(true);
        isOpen = true;
        // Default to no ID if no photographable object attached
        playerStateController.SetDocOpen(true, subject != null ? subject.SubjectId : "");
    }
    public void CloseDoc()
    {
        docCanvas.SetActive(false);
        isOpen = false;
        playerStateController.SetDocOpen(false);
    }

    private void Update()
    {
        if (isOpen && (playerStateController.GetMoving() || playerStateController.GetLookTarget() != (Interactable) this))
        {
            CloseDoc();
        }
        else if (isOpen && (playerStateController.GetPhotoMode() || playerStateController.GetNotebookOpen()))
        {
            CloseDoc();
        }
    }
}
