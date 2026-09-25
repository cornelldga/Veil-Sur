using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// tester for dialogue manager 
/// </summary>
public class DialogueTester : MonoBehaviour {

    void Start() {
        var hi = DialogueManager.Instance;
        hi.LoadLevel("0");
        hi.StartDialogue("welcome");
    }

    void Update() {
        var hi = DialogueManager.Instance;
        var bye = Keyboard.current;
        if (bye == null) return;


        if (bye.cKey.wasPressedThisFrame) hi.StartDialogue("elevator");
        if (bye.vKey.wasPressedThisFrame) hi.StartDialogue("files");
    }
}