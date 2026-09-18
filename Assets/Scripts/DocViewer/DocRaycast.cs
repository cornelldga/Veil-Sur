using System;
using UnityEngine;

public class DocRaycast : MonoBehaviour
{
    [Header("Document Raycast Features")]
    [SerializeField] private float rayLength = 5f;

    private Camera _camera;
    
    private DocController _docController;

    [SerializeField] private KeyCode docInteractKey;


    void Start()
    {
        _camera = GetComponent<Camera>();
    }


    /// <summary>
    /// Update() will, once per frame, use Raycast to check if the player is
    /// looking at a valid readable document. If it can find a DocController
    /// component, the player can then input docInteractKey to open the viewer.
    /// </summary>
    private void Update()
    {
        if (Physics.Raycast(_camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f)), transform.forward, out RaycastHit hit, rayLength))
        { 
            var readableDoc = hit.collider.GetComponent<DocController>();
            if (readableDoc != null)
            {
                // doccontroller = readabledoc
                Debug.Log("Hit!");
            }
            else
            {
                // clear item
            }
        }
        else
        {
            // also clear here!
        }
        if (_docController != null)
            {
                if (Input.GetKeyDown(docInteractKey))
                {
                    // doccontroller.Show, we put it on the screen
                }
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
