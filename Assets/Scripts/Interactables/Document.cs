using UnityEngine;
using TMPro;
using System;
using System.Runtime.CompilerServices;

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

    private PlayerState playerStateController;
    private bool isOpen = false;
    // True when opened via Interact() rather than the camera's document reader
    private bool openedByInteract = false;

    /// <summary>
    /// Awake() loads all of the JSON text into this document to display
    /// at instantiation.
    /// </summary>
    private void Awake()
    {
        LoadDocData();
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
        docData = JsonUtility.FromJson<DocData>(jsonFile.text);
    }

    /// <summary>
    /// Opens or closes the doc viewer, showing this document's contents the
    /// same way the camera's document reader does.
    /// </summary>
    public void Interact()
    {
        if (!isOpen)
        {
            OpenDoc();
            openedByInteract = true;
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
        openedByInteract = false;
    }
    public void CloseDoc()
    {
        docCanvas.SetActive(false);
        isOpen = false;
        openedByInteract = false;
    }

    private void Update()
    {
        if (isOpen && playerStateController.GetMoving())
        {
            CloseDoc();
        }
        // The camera reader manages its own docs, so only close interact-opened
        // docs when the camera or notebook takes over the screen
        else if (openedByInteract && (playerStateController.GetPhotoMode() || playerStateController.GetNotebookOpen()))
        {
            CloseDoc();
        }
    }
}
