using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows a tutorial prompt at a time 
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Tooltip("Text that displays the current tutorial prompt")]
    [SerializeField] private TMP_Text promptText;
    
    [Tooltip("Seconds for the prompt to fade in or out")]
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("How far it moves up and down")]
    [SerializeField] private float amplitude = 10f;
    [Tooltip("Speed of movement")]
    [SerializeField] private float moveSpeed = 1f;

    // Built on demand (not stored) so prompts always show the player's current keybinds.
    private const int PromptCount = 22;

    private string GetPrompt(int index)
    {
        PlayerControls c = GameManager.Controls;
        switch (index)
        {
            case 0: return $"Use {KeyName(c.PlayerMovement.Move)} to move.";
            case 1: return $"Hold {KeyName(c.PlayerMovement.Sprint)} to sprint.";
            case 2: return $"Hold {KeyName(c.PlayerMovement.Crouch)} to crouch.";
            case 3: return $"Press {KeyName(c.PlayerMovement.AimCamera)} to aim your camera, and {KeyName(c.PlayerMovement.TakePicture)} to take a photo!";
            case 4: return "Take four more photos of objects around the room! 0/4";
            case 5: return "Take four more photos of objects around the room! 1/4 done!";
            case 6: return "Take four more photos of objects around the room! 2/4 done!";
            case 7: return "Take four more photos of objects around the room! 3/4 done!";
            case 8: return "Nice! Try to take one more photo, your storage is full!";
            case 9: return $"Press {KeyName(c.General.Notebook)} to open your notebook.";
            case 10: return "Double click on images in your notebook to look at them larger.";
            case 11: return "Drag a photo onto a question to connect it.";
            case 12: return "Drag a photo onto the trash icon to delete it. Now you can take more!";
            case 13: return "Close your notebook.";
            case 14: return $"Aim your camera at a document, then press {KeyName(c.PlayerMovement.SwapCameraMode)} to switch to document mode. This allows you to view documents!";
            case 15: return $"{KeyName(c.PlayerMovement.TakePicture)} in document mode to scan it into your notebook.";
            case 16: return "Open your notebook again.";
            case 17: return "You scanned the document! Drag it onto a question to fill its answer slot.";
            case 18: return "Click upload to check how many of your links are correct.";
            case 19: return "Close your notebook.";
            case 20: return $"Press {KeyName(c.PlayerMovement.Interact)} to interact with objects that have the interact icon.";
            // Maybe in the above ^ prompt we'll include a picture of the interact icon.
            // Pause isn't an input action (GameManager reads the ESC key directly), so it can't be looked up like the others.
            case 21: return "You can always pause by pressing <mark=#FFFFFF18 padding=\"6,6,1,1\">ESC</mark> and change keybinds in settings.";

            default: return "";
        }
    }

    /// <summary>
    /// Readable name of the key(s) currently bound to an action, from the shared (rebindable) controls.
    /// </summary>
    private static string KeyName(InputAction action)
    {
        var parts = new List<string>();

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite) continue;
            if (string.IsNullOrEmpty(b.effectivePath)) continue;

            parts.Add(b.effectivePath switch
            {
                "<Mouse>/leftButton" => "Left click",
                "<Mouse>/rightButton" => "Right click",
                "<Mouse>/middleButton" => "Middle click",
                _ => InputControlPath.ToHumanReadableString(b.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)
            });
        }

        // Several bindings on one action (e.g. duplicate defaults) shouldn't repeat the same name.
        parts = parts.Distinct().ToList();
        // Highlight the whole key list (separators included) with a 15% opacity white box (TMP rich text).
        string keys = parts.Count == 0 ? "[unbound]" : string.Join(parts.Count == 4 ? "/" : " or ", parts);
        return $"<mark=#FFFFFF18 padding=\"6,6,1,1\">{keys}</mark>";
    }

    private int step;
    private int photoCountAtStepStart;
    private PlayerControls controls;
    private RectTransform promptRect;
    private Vector2 basePosition;
    private bool hasSeenDialogue;
    private bool tutorialStarted;
    private bool uploadClicked;

    /// <summary>
    /// how other scripts can reach the tutorial (null in levels without one)
    /// </summary>
    public static TutorialManager Instance { get; private set; }

    /// <summary>
    /// fades out the current prompt and skips the rest of the tutorial, e.g. when the level ends
    /// </summary>
    public void Hide()
    {
        step = PromptCount;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        // Shared instance, so the player's rebinds apply. Other scripts own its enable/disable state.
        controls = GameManager.Controls;
        promptRect = promptText.rectTransform;
        basePosition = promptRect.anchoredPosition;
    }

    private void Start()
    {
        promptText.text = "";
        promptText.alpha = 0f;
    }

    private void Update()
    {
        if (!tutorialStarted)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                hasSeenDialogue = true;
                return;
            }
            else if (hasSeenDialogue)
            {
                tutorialStarted = true;
                photoCountAtStepStart = PhotoStorage.Instance.GetTotalOwnedPhotoCount();
            }
            else
            {
                return;
            }
        }

        UpdatePromptFade();
        MoveUpandDown();

        if (step < PromptCount && IsStepComplete())
        {
            step++;
            uploadClicked = false;
            photoCountAtStepStart = PhotoStorage.Instance.GetTotalOwnedPhotoCount();
            if (step == PromptCount && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue("leave-room");
        }
    }

    private void OnEnable()
    {
        UploadScript.Uploaded += OnUploaded;
    }

    private void OnDisable()
    {
        UploadScript.Uploaded -= OnUploaded;
    }

    /// <summary>
    /// remembers that the player clicked upload during the current step
    /// </summary>
    private void OnUploaded()
    {
        uploadClicked = true;
    }

    /// <summary>
    /// true once every visible question slot in the notebook has a photo, which is what enables upload
    /// </summary>
    private static bool AllQuestionSlotsFilled()
    {
        if (UIManager.Instance.Notebook == null) return false;

        bool hasQuestions = false;
        foreach (var slot in UIManager.Instance.Notebook.QuestionSlots)
        {
            if (slot == null || !slot.isActiveAndEnabled || slot.Definition == null) continue;
            hasQuestions = true;
            if (slot.Photo == null) return false;
        }
        return hasQuestions;
    }

    private bool IsStepComplete()
    {
        GameManager.GameState state = GameManager.Instance.state;

        switch (step)
        {
            case 0: return controls.PlayerMovement.Move.ReadValue<Vector2>() != Vector2.zero;
            case 1: return controls.PlayerMovement.Sprint.IsPressed();
            case 2: return controls.PlayerMovement.Crouch.IsPressed();
            case 3: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart;
            case 4: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart; //1
            case 5: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart; //2
            case 6: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart; //3
            case 7: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart; //4
            case 8: return controls.PlayerMovement.TakePicture.WasPerformedThisFrame() && PhotoStorage.Instance.IsPhotoStorageFull();
            case 9: return state == GameManager.GameState.NOTEBOOK;
            case 10: return UIManager.Instance.Notebook != null && UIManager.Instance.Notebook.IsPhotoPreviewOpen;
            case 11: return UIManager.Instance.Notebook != null && UIManager.Instance.Notebook.QuestionSlots.Any(q => q.Photo != null);
            case 12: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() < photoCountAtStepStart;
            case 13: return state != GameManager.GameState.NOTEBOOK;
            case 14: return state == GameManager.GameState.CAMERA && controls.PlayerMovement.SwapCameraMode.WasPerformedThisFrame();
            case 15: return PhotoStorage.Instance.GetTotalOwnedPhotoCount() > photoCountAtStepStart;
            case 16: return state == GameManager.GameState.NOTEBOOK;
            case 17: return AllQuestionSlotsFilled();
            case 18: return uploadClicked;
            case 19: return state != GameManager.GameState.NOTEBOOK;
            case 20: return controls.PlayerMovement.Interact.WasPerformedThisFrame() && UIManager.Instance.interact.gameObject.activeSelf;
            case 21: return state == GameManager.GameState.PAUSED; // ESC pauses the game
            default: return false;
        }
    }

    /// <summary>
    /// moves to next prompt
    /// </summary>
    private void UpdatePromptFade()
    {
        string wantedText = step < PromptCount ? GetPrompt(step) : "";
        float fadeStep = Time.unscaledDeltaTime / fadeDuration;

        if (promptText.text != wantedText)
        {
            promptText.alpha = Mathf.MoveTowards(promptText.alpha, 0f, fadeStep);
            if (promptText.alpha == 0f)
            {
                promptText.text = wantedText;
            }
        }
        else
        {
            float target = wantedText == "" ? 0f : 1f;
            promptText.alpha = Mathf.MoveTowards(promptText.alpha, target, fadeStep);
        }
    }

    /// <summary>
    /// Move prompt up and down 
    /// </summary>
    private void MoveUpandDown()
    {
        float offset = Mathf.Sin(Time.unscaledTime * moveSpeed * Mathf.PI * 2f) * amplitude;
        promptRect.anchoredPosition = basePosition + new Vector2(0f, offset);
    }

}
