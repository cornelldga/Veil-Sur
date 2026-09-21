using UnityEngine;

/// <summary>
/// demo/testing script to try out the dialogue syste,
/// </summary>
public class DemoSceneScript : MonoBehaviour
{
    [Tooltip("the json script")]
    [SerializeField] private string dialogueFile = "Script";

    private void Start()
    {
        DialogueManager.Instance.StartDialogue(dialogueFile);
    }
}