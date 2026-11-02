using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the world-space canvas on a terminal's monitor. Add any UI under the
/// screen canvas and control it from here.
/// </summary>
public class TerminalScreen : MonoBehaviour
{
    [Tooltip("Main text shown on the monitor")]
    [SerializeField] private TextMeshProUGUI screenText;
    [Tooltip("Text shown when the terminal is idle")]
    [SerializeField] private string idleMessage = "SAVE TERMINAL";
    [Tooltip("Text shown while the player is holding interact")]
    [SerializeField] private string savingMessage = "SAVING...";
    [Tooltip("Text shown right after the game is saved")]
    [SerializeField] private string savedMessage = "PROGRESS SAVED";
    [Tooltip("Seconds the saved message stays up before returning to idle")]
    [SerializeField] private float savedDuration = 2f;
    [Tooltip("Seconds between each on/off blink of the saved message")]
    [SerializeField] private float flashInterval = 0.25f;

    [Header("Interact Icon")]
    [Tooltip("Icon that spins while the player holds the interact key")]
    [SerializeField] private RectTransform interactIcon;
    [Tooltip("Spin speed of the interact icon in degrees per second")]
    [SerializeField] private float spinSpeed = 900f;

    [Header("Storage Bar")]
    [Tooltip("Filled image of the full bar, layered over the empty bar")]
    [SerializeField] private Image storageFill;

    private Coroutine messageRoutine;
    private bool spinning;

    private void Awake()
    {
        SetText(idleMessage);
        SetFill(0f);
    }

    private void Update()
    {
        if (spinning && interactIcon != null)
        {
            // Negative z spins clockwise as seen by the player
            interactIcon.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Replaces the text on the monitor.
    /// </summary>
    /// <param name="text">text to display</param>
    public void SetText(string text)
    {
        if (screenText != null) screenText.text = text;
    }

    /// <summary>
    /// Spins the interact icon and shows the saving message while a hold is in
    /// progress, and resets both when the hold ends.
    /// </summary>
    /// <param name="progress">hold progress from 0 to 1</param>
    public void SetHoldProgress(float progress)
    {
        bool wasSpinning = spinning;
        spinning = progress > 0f && progress < 1f;

        if (spinning && !wasSpinning)
        {
            // Interrupt a saved flash if the player starts holding again
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
                messageRoutine = null;
            }
            if (screenText != null) screenText.enabled = true;
            SetText(savingMessage);
        }
        else if (!spinning && wasSpinning)
        {
            SetText(idleMessage);
        }

        if (!spinning && interactIcon != null) interactIcon.localRotation = Quaternion.identity;

        // Keep the bar full while the saved message is flashing
        if (messageRoutine == null || progress > 0f) SetFill(progress);
    }

    /// <summary>
    /// Sets how much of the full storage bar is revealed over the empty one.
    /// </summary>
    /// <param name="amount">fill amount from 0 to 1</param>
    private void SetFill(float amount)
    {
        if (storageFill != null) storageFill.fillAmount = amount;
    }

    /// <summary>
    /// Flashes the saved message, then returns to the idle message.
    /// </summary>
    public void ShowSaved()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(FlashTemporary(savedMessage));
    }

    private IEnumerator FlashTemporary(string text)
    {
        SetText(text);
        if (screenText != null)
        {
            float elapsed = 0f;
            while (elapsed < savedDuration)
            {
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
                screenText.enabled = !screenText.enabled;
            }
            screenText.enabled = true;
        }
        SetText(idleMessage);
        SetFill(0f);
        messageRoutine = null;
    }
}
