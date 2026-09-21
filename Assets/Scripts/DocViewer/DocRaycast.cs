using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DocRaycast : MonoBehaviour
{
    [Header("Document Raycast Features")]
    [SerializeField] private float rayLength = 5f;

    private Camera _camera;
    
    private DocController _docController;

    void Start()
    {
        _camera = GetComponent<Camera>();
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If it can find a DocController
    /// component, the document opens.
    /// TBD: Work with Player team to add input.
    /// </summary>
    private void Update()
    {
        if (Physics.Raycast(_camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f)), transform.forward, out RaycastHit hit, rayLength))
        { 
            var readableDoc = hit.collider.GetComponent<DocController>();
            if (readableDoc != null)
            {
                _docController = readableDoc;
            }
            else
            {
                ClearDoc();
            }
        }
        else
        {
            ClearDoc();
        }
        if (_docController != null)
            {
                // Here, I want to work with someone on the Player team 
                // so we can put a conditional on input.
                _docController.ShowDoc();
            }
    }


    /// <summary>
    /// ClearDoc() will wipe the docController and set it to null.
    /// </summary>
    void ClearDoc()
    {
        if (_docController != null)
        {
            _docController = null;
        }
    }
}
