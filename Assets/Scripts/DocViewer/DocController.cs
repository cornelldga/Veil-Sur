using UnityEngine;
using TMPro;
using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

public class DocController : MonoBehaviour
{
    [Header("UI Features")]
    [SerializeField] private GameObject docCanvas;
    [SerializeField] private TMP_Text docTextAreaUI;

    [SerializeField] [TextArea] private string docText;

    private bool isOpen = false;

    /// <summary>
    /// ShowDoc() reveals the UI panel of the DocView.
    /// It presents the text of the document.
    /// </summary>
    public void ShowDoc()
    {
        docTextAreaUI.text = docText;
        docCanvas.SetActive(true);
        isOpen = true;
    }

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
