using System.Linq;
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
        "Use WASD to move.",
        "Hold shift to sprint.",
        "Right click to aim your camera, and left click to take a photo!",
        "Take four more photos of what you like!",
        "Try to take one more photo, your storage is full!",
        "Press tab to open your notebook.",
        "Connect a photo to a question.",
        "Drag a photo onto the trash icon to delete it. Now you can take more!",
        "Close your notebook. ",
        "Aim your camera at a document, then press E to switch to document mode.",
        "Left click in document mode to scan it into your notebook.",
        "Open your notebook again.",
        "You scanned the document! You can also connect it to questions. Close your notebook.",
        "Press E to interact with objects that have the interact icon."
        // Maybe in the above ^ prompt we'll include a picture of the interact icon.
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
            case 1: return controls.PlayerMovement.Sprint.IsPressed();
            case 2: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            case 3: return PhotoStorage.Instance.GetPhotoCount() >= photoCountAtStepStart + 4;
            case 4: return controls.PlayerMovement.TakePicture.WasPerformedThisFrame() && PhotoStorage.Instance.IsPhotoStorageFull();
            case 5: return state == GameManager.GameState.NOTEBOOK;
            case 6: return UIManager.Instance.Notebook != null && UIManager.Instance.Notebook.Questions.Any(q => q.Photo != null);
            case 7: return PhotoStorage.Instance.GetPhotoCount() < photoCountAtStepStart;
            case 8: return state != GameManager.GameState.NOTEBOOK;
            case 9: return state == GameManager.GameState.CAMERA && controls.PlayerMovement.SwapCameraMode.WasPerformedThisFrame();
            case 10: return PhotoStorage.Instance.GetPhotoCount() > photoCountAtStepStart;
            case 11: return state == GameManager.GameState.NOTEBOOK;
            case 12: return state != GameManager.GameState.NOTEBOOK;
            case 13: return controls.PlayerMovement.Interact.WasPerformedThisFrame() && UIManager.Instance.interact.gameObject.activeSelf;

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
