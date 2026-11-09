using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
// </summary>
<<<<<<<< HEAD:Assets/Scripts/Dialogue/Dialogue1.cs
public class Dialogue1 : MonoBehaviour {
========
public class TutorialDialogue : MonoBehaviour {
>>>>>>>> main:Assets/Scripts/Dialogue/TutorialDialogue.cs

    void Start() {
        var m0 = DialogueManager.Instance;
        m0.LoadLevel("0");
        m0.StartDialogue("welcome");
    }
}