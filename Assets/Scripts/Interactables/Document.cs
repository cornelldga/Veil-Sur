using UnityEngine;
using TMPro;

[System.Serializable]
public class DocData
{
    public string text;
    // optional: one entry per line, joined with newlines. Easier to edit than one long text string.
    public string[] lines;
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
        if (jsonFile == null)
        {
            Debug.LogWarning($"Document '{name}' has no JSON file assigned.", this);
            return;
        }

        try
        {
            docData = JsonUtility.FromJson<DocData>(jsonFile.text);
        }
        catch (System.ArgumentException e)
        {
            Debug.LogWarning($"Document '{name}' has invalid JSON: {e.Message}", this);
            return;
        }

        // Multi-line documents use a "lines" array; join it into the single text the viewer shows.
        if (docData != null && docData.lines != null && docData.lines.Length > 0)
            docData.text = string.Join("\n", docData.lines);
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
        if (docData == null || string.IsNullOrEmpty(docData.text)) { return; }
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
