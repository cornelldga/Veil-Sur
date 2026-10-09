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
        // "press tab to open your notebook",
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

            default: return false;
        }
    }


}
