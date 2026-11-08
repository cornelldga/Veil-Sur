using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
// </summary>
public class Dialogue1 : MonoBehaviour {

    void Start() {
        var m0 = DialogueManager.Instance;
        m0.LoadLevel("0");
        m0.StartDialogue("welcome");
    }
}