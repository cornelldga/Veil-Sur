using TMPro;
using UnityEngine;

/// <summary>
/// Shows a tutorial prompt at a time 
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Tooltip("Text that displays the current tutorial prompt")]
    [SerializeField] private TMP_Text promptText;
    
    [Tooltip("Seconds for the prompt to fade in or out")]
    [SerializeField] private float fadeDuration = 0.4f;

    private readonly string[] prompts =
    {
        "wasd to move, hold shift to sprint",
        "take a photo! right click to aim, left click to take a photo!",
        //"press tab to open your notebook",
        //connect photo to question
        "aim your camera, then press E to switch to document mode",
        "aim at a document and left click to scan it",
        "press e to interact with objects"
    };

    private int step;
    private int photoCountAtStepStart;
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.PlayerMovement.Enable();
    }

    private void OnDisable()
    {
        controls.PlayerMovement.Disable();
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }

    private void Start()
    {
        promptText.text = "";
        promptText.alpha = 0f;
    }

    private void Update()
    {
        UpdatePromptFade();

        if (step < prompts.Length && IsStepComplete())
        {
            step++;
            photoCountAtStepStart = PhotoStorage.Instance.GetPhotoCount();
        }
    }

    private bool IsStepComplete()
    {
        GameManager.GameState state = GameManager.Instance.state;

        switch (step)
        {
            case 0: return controls.PlayerMovement.Move.ReadValue<Vector2>() != Vector2.zero;
            case 1: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            // case 2: return state == GameManager.GameState.NOTEBOOK;
            case 2: return state == GameManager.GameState.CAMERA && controls.PlayerMovement.SwapCameraMode.WasPerformedThisFrame();
            case 3: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            case 4: return controls.PlayerMovement.Interact.WasPerformedThisFrame() && UIManager.Instance.interact.gameObject.activeSelf;

            default: return false;
        }
    }

    /// <summary>
    /// moves to next prompt
    /// </summary>
    private void UpdatePromptFade()
    {
        string wantedText = step < prompts.Length ? prompts[step] : "";
        float fadeStep = Time.unscaledDeltaTime / fadeDuration;
        Debug.Log($"alpha={promptText.alpha} text='{promptText.text}' wanted='{wantedText}'");

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

}
