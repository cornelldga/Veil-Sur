using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance {get; private set;}

    [Header("Camera UI")]
    [SerializeField] public GameObject cameraGroup;
    [SerializeField] public Image snapOverlay;

    [Header("Notebook UI")]
    [SerializeField] public GameObject notebookGroup;

    [Header("Doc Viewer")]
    [SerializeField] public GameObject docCanvas;
    [SerializeField] public TMP_Text docViewer;
 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Clear duplicates out of the scene
            return;
        }

        Instance = this;
    }
}