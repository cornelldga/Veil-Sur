using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.InputSystem;

public class DialogueSection {
    public string puzzle;
    public string[] lines;
}   

/// <summary>
/// controls dialogue loaded from json files. showing and hiding dialogue box and progressing throughout lines. 
/// other scripts can access through DialogueManager.Instance.
/// </summary>
public class DialogueManager : MonoBehaviour {
    /// <summary>
    /// how other scripts can access the dialogue manager
    /// </summary>
    public static DialogueManager Instance { get; private set; }

    /// <summary>
    /// true while a dialogue section is currently playing.
    /// </summary>
    public bool IsDialogueActive => isDialogueActive;

    [Tooltip("the panel GameObject that gets shown or hidden")]
    [SerializeField] private GameObject dialogueBox;

    [Tooltip("TextMeshProUGUI component that displays the current line")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    
    [Tooltip("characters per second")]
    [SerializeField] private float charactersPerSecond = 40f;

    [Tooltip("sound for the type-y sound that plays when a dialogue box appears")]
    [SerializeField] AudioClip dialogueTickSound;

    [Tooltip("the click tooltip that shows if the player waits too long to advance")]
    [SerializeField] private GameObject clickPrompt;

    [Tooltip("seconds before the click prompt shows")]
    [SerializeField] private float clickPromptDelay = 5f;

    [Tooltip("tracker that helps us know when to play the next clicking sound")]
    int lastVisibleCharacters;
    //variable that makes sure we don't OD spam the sound effect
    int charactersPerTick = 3;

    private const string dialogueFolder = "Dialogue/Level ";
    //sectioned stored so that StartDialogue("name") can directly reference the dialogue in that section
    private Dictionary<string, DialogueSection> levelSections;
    private DialogueSection currentSection;

    //set so that there are no duplicates and dialogue doesn't repeat
    private HashSet<string> played = new HashSet<string>();
    private int currentLine = 0; //index
    private bool isDialogueActive = false;
    private int startedFrame = -1; //the frame a dialogue opened on, so the click that triggered it doesn't skip a line
    private bool isTyping = false; //true while the current line is still being typed out
    private float idleTime = 0f; //seconds the finished line has gone unclicked
    private float typedCharacters = 0f; //how many characters of the current line are visible (fractional between frames)

    /// <summary>
    /// Sets up the singleton instance
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
    /// loads the levl 
    /// </summary>
    /// <param name="level"> corresponding lvl</param>
    public void LoadLevel(string level) {
        TextAsset jsonFile = Resources.Load<TextAsset>(dialogueFolder + level);
        if (jsonFile == null) return;
        levelSections = JsonConvert.DeserializeObject<Dictionary<string, DialogueSection>>(jsonFile.text);
        played.Clear();
        EndDialogue();
    }

    /// <summary>
    /// plays dialogue of corresponding section IF it hasn't been played yet
    /// </summary>
    /// <param name="sectionID"> the corresponding ID for that section of dialogue </param>
    public void StartDialogue(string sectionID) {
        if (levelSections == null) return;
        if (!levelSections.TryGetValue(sectionID, out DialogueSection section)) return;
        if (isDialogueActive || played.Contains(sectionID)) return; //if played already then fade
        //add something later about if a puzzle is solved or not, if already solved then don't need to
        //bring up the dialogue even if the player hasn't seen it before
        
        played.Add(sectionID); //add the sectionID to the set of already played
        currentSection = section;
        currentLine = 0;
        isDialogueActive = true;
        startedFrame = Time.frameCount;

        ShowDialogue();
        DisplayCurrentLine();
    }

    /// <summary>
    /// the IDs of every dialogue section already played this level, for saving
    /// </summary>
    public List<string> GetPlayedSections()
    {
        return new List<string>(played);
    }

    /// <summary>
    /// restores already played sections from a save. closes the current dialogue if it was already played
    /// </summary>
    /// <param name="sectionIDs"> the played section IDs from the save </param>
    public void RestorePlayedSections(List<string> sectionIDs)
    {
        played = new HashSet<string>(sectionIDs);

        // the scene's opening dialogue (e.g. "welcome") may have started before the save was applied
        if (isDialogueActive && currentSection != null)
        {
            foreach (string id in played)
            {
                if (levelSections != null && levelSections.TryGetValue(id, out DialogueSection section) && section == currentSection)
                {
                    EndDialogue();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// goes to next line of dialogue when box is clicked
    /// </summary>
    public void NextLine(){
        if (!isDialogueActive || currentSection == null || IsPaused()){
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
    /// updates the dialogue text ui with the current line and starts typing it out from the first character
    /// </summary>
    private void DisplayCurrentLine()
    {
        dialogueText.text = currentSection.lines[currentLine].ToUpper();
        dialogueText.maxVisibleCharacters = 0;
        typedCharacters = 0f;
        lastVisibleCharacters = 0;
        isTyping = true;
        HideClickPrompt();
    }

    /// <summary>
    /// reveals more of the current line based on elapsed time
    /// stops typing once the whole line is visible
    /// </summary>
    private void TypeCurrentLine()
    {
        dialogueText.ForceMeshUpdate();
        int totalCharacters = dialogueText.textInfo.characterCount;

        typedCharacters += charactersPerSecond * Time.unscaledDeltaTime;

        int visibleCharacters = Mathf.FloorToInt(typedCharacters);

        if (visibleCharacters > lastVisibleCharacters)
        {
            if (visibleCharacters % charactersPerTick == 0)
            {
                AudioManager.Instance.PlayUISFX(dialogueTickSound, 1);
            }
            lastVisibleCharacters = visibleCharacters;
        }

        dialogueText.maxVisibleCharacters = visibleCharacters;

        if (visibleCharacters >= totalCharacters)
            FinishTyping();
    }

    /// <summary>
    /// skips typing animation and shows the whole current line
    /// </summary>
    private void FinishTyping()
    {
        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        idleTime = 0f;
    }

    /// <summary>
    /// true while the game is paused, so clicks and typing shouldn't progress the dialogue
    /// </summary>
    private bool IsPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.state == GameManager.GameState.PAUSED;
    }

    /// <summary>
    /// hides the click prompt and restarts its countdown
    /// </summary>
    private void HideClickPrompt()
    {
        idleTime = 0f;
        if (clickPrompt != null) clickPrompt.SetActive(false);
    }

    /// <summary>
    /// ends any dialogue that is playing right now and hides the box, e.g. when the level ends
    /// </summary>
    public void CancelDialogue()
    {
        EndDialogue();
    }

    /// <summary>
    /// hides dialogue box when dialogue is done
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        currentSection = null;
        HideClickPrompt();
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

    /// <summary>
    /// types out the current line, and lets the player click to skip the typing or progress to the next line
    /// </summary>
    private void Update()
    {
        if (!isDialogueActive || Time.frameCount == startedFrame || IsPaused()) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping) FinishTyping();
            else NextLine();
            return;
        }

        if (isTyping)
        {
            TypeCurrentLine();
            return;
        }

        idleTime += Time.unscaledDeltaTime;
        if (idleTime >= clickPromptDelay && clickPrompt != null) clickPrompt.SetActive(true);
    }
}