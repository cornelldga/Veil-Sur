using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
// </summary>
public class LevelOneDialogue : MonoBehaviour {

    void Start() {
        var m1 = DialogueManager.Instance;
        m1.LoadLevel("1");
        m1.StartDialogue("welcome");
    }
}