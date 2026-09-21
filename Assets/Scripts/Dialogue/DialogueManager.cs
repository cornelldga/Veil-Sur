using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueLine {
    public string text;
}

[System.Serializable]
public class DialogueData {
    public DialogueLine[] lines;
}


/// <summary>
/// controls dialogue loaded from json files. showing and hiding dialogue box and progressing throughout lines. 
/// other scripts can access through DialogueManager.Instance.
/// <summary>
public class DialogueManager : MonoBehaviour {
    /// <summary>
    /// how other scripts can access the dialogue manager
    /// </summary>
    public static DialogueManager Instance { get; private set; }

    [Tooltip("the panel GameObject that gets shown or hidden")]
    [SerializeField] private GameObject dialogueBox;

    [Tooltip("TextMeshProUGUI component that displays the current line")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    private DialogueData dialogueData;
    private int currentLine = 0; //index
    private bool isDialogueActive = false;

    /// <summary>
    /// Sets up the singleton instance and ensures the dialogue box starts hidden.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HideDialogue();
    }

    /// <summary>
    ///loads a dialogue JSON file from a Resources folder and displays it
    /// </summary>
    /// <param name="jsonFileName"> json file with dialogue data </param>
    public void StartDialogue(string jsonFileName) {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        dialogueData = JsonUtility.FromJson<DialogueData>(jsonFile.text);
        currentLine = 0;
        isDialogueActive = true;

        ShowDialogue();
        DisplayCurrentLine();
    }

    /// <summary>
    /// goes to next line of dialogue when box is clicked 
    /// </summary>
    public void NextLine(){
        if (!isDialogueActive || dialogueData == null){
            return;
        }

        currentLine++;

        if (currentLine >= dialogueData.lines.Length){
            EndDialogue();
            return;
        }

        DisplayCurrentLine();
    }

    /// <summary>
    /// updates the dialogue text ui with the current line
    /// </summary>
    private void DisplayCurrentLine()
    {
        dialogueText.text = dialogueData.lines[currentLine].text;
    }

    /// <summary>
    /// hides dialogue box when dialogue is done
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        HideDialogue();
    }

    /// <summary>
    /// call this when you want to show the dialogue box
    /// </summary>
    public void ShowDialogue()
    {
        dialogueBox.SetActive(true);
    }

    /// <summary>
    /// call this when you want to hide the dialogue box
    /// </summary>
    public void HideDialogue()
    {
        dialogueBox.SetActive(false);
    }
}