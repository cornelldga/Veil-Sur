using UnityEngine;
using TMPro;
using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

[System.Serializable]
public class DocData
{
    public string text;
}


public class DocController : MonoBehaviour
{
    [Header("UI Features")]

    [Tooltip("The in-scene Document View canvas")]
    [SerializeField] private GameObject docCanvas;
    
    [Tooltip("The Document View text mesh of the canvas")]
    [SerializeField] private TMP_Text docTextAreaUI;
    
    [Tooltip("The name of the JSON file in Resources/")]
    [SerializeField] private string docFile = "File";
    
    private DocData docData;

    private bool isOpen = false;

    /// <summary>
    /// Awake() loads all of the JSON text into this document to display
    /// at instantiation.
    /// </summary>
    private void Awake()
    {
        LoadDocData();
    }


    /// <summary>
    /// LoadDocData() reads and parses the JSON file. It stores the text
    /// of the document 
    /// </summary>
    private void LoadDocData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(docFile);
        docData = JsonUtility.FromJson<DocData>(jsonFile.text);
    }


    /// <summary>
    /// ShowDoc() reveals the UI panel of the DocView.
    /// It presents the text of the document.
    /// </summary>
    public void ShowDoc()
    {
        docTextAreaUI.text = docData.text;
        docCanvas.SetActive(true);
        isOpen = true;
    }


    /// <summary>
    /// CloseDoc() hides the DocView canvas and sets isOpen to false.
    /// </summary>
    public void CloseDoc()
    {
        docCanvas.SetActive(false);
        isOpen = false;
    }


    /// <summary>
    /// Update() checks once per frame for if the document is open.
    /// If it is open, give the player the opportunity to input the close key
    /// and close the document.
    /// </summary>
    private void Update()
    {
        if (isOpen)
        {
            // TBD: Work with player team to close on input
            CloseDoc();
        }
    }
}
