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
    [Tooltip("How far it moves up and down")]
    [SerializeField] private float amplitude = 10f;
    [Tooltip("Speed of movement")]
    [SerializeField] private float moveSpeed = 1f;

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
    private RectTransform promptRect;
    private Vector2 basePosition;

    private void Awake()
    {
        controls = new PlayerControls();
        promptRect = promptText.rectTransform;
        basePosition = promptRect.anchoredPosition;
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
        MoveUpandDown();

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
            case 2: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            // case 2: return state == GameManager.GameState.NOTEBOOK;
            case 3: return state == GameManager.GameState.CAMERA && controls.PlayerMovement.SwapCameraMode.WasPerformedThisFrame();
            case 4: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            case 5: return controls.PlayerMovement.Interact.WasPerformedThisFrame() && UIManager.Instance.interact.gameObject.activeSelf;

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
