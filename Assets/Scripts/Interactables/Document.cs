using UnityEngine;
using TMPro;
using System;
using System.Runtime.CompilerServices;

[System.Serializable]
public class DocData
{
    public string text;
}


public class Document : MonoBehaviour
{
    [Header("UI Features")]
    
    [Tooltip("The name of the JSON file in Resources/")]
    [SerializeField] private TextAsset jsonFile;
    
    private DocData docData;
    private TMP_Text docTextAreaUI;
    private GameObject docCanvas;

    private PlayerState playerStateController;
    private bool isOpen = false;

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

    public void OpenDoc()
    {
        docTextAreaUI.text = docData.text;
        docCanvas.SetActive(true);
        isOpen = true;
    }
    public void CloseDoc()
    {
        docCanvas.SetActive(false);
        isOpen = false;
    }

    private void Update()
    {
        if (isOpen && playerStateController.GetMoving())
        {
            CloseDoc();
        }
    }
}
