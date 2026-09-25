using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;

public class DialogueSection {
    public string puzzle;
    public string[] lines;
}

public class DialogueData {
    //levels[level #][puzzle name]
    public Dictionary<string, Dictionary<string, DialogueSection>> levels;
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

    //
    private DialogueData dialogueData;
    private const string jsonFileName = "Script";

    //sectioned stored so that StartDialogue("name") can directly reference the dialogue in that section
    private Dictionary<string, DialogueSection> levelSections;
    private DialogueSection currentSection;

    //set so that there are no duplicates and dialogue doesn't repeat
    private HashSet<string> played = new HashSet<string>();
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
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        dialogueData = JsonConvert.DeserializeObject<DialogueData>(jsonFile.text);
        HideDialogue();
    }

    
    /// <summary>
    /// loads the levl
    /// </summary>
    /// <param name="level"> corresponding lvl</param>
    public void LoadLevel(string level) {
        levelSections = dialogueData.levels[level];
        played.Clear();
        EndDialogue();
    }

    /// <summary>
    ///loads a dialogue JSON file from a Resources folder and displays it
    /// </summary>
    /// <param name="jsonFileName"> json file with dialogue data </param>
    public void StartDialogue(string sectionID) {
        if (!levelSections.TryGetValue(sectionID, out DialogueSection section)) return;
        if (isDialogueActive || played.Contains(sectionID)) return; //if played already then fade
        //add something later about if a puzzle is solved or not, if already solved then don't need to
        //bring up the dialogue even if the player hasn't seen it before
        
        played.Add(sectionID); //add the sectionID to the set of already played
        currentSection = section;
        currentLine = 0;
        isDialogueActive = true;

        ShowDialogue();
        DisplayCurrentLine();
    }

    /// <summary>
    /// goes to next line of dialogue when box is clicked 
    /// </summary>
    public void NextLine(){
        if (!isDialogueActive || currentSection == null){
            return;
        }

        currentLine++;

        if (currentLine >= currentSection.lines.Length){
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
        dialogueText.text = currentSection.lines[currentLine];
    }

    /// <summary>
    /// hides dialogue box when dialogue is done
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        currentSection = null;
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