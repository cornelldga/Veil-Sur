using System.Collections;
using TMPro;
using UnityEngine;

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
    [Tooltip("Text shown right after the game is saved")]
    [SerializeField] private string savedMessage = "PROGRESS SAVED";
    [Tooltip("Seconds the saved message stays up before returning to idle")]
    [SerializeField] private float savedDuration = 2f;

    private Coroutine messageRoutine;

    private void Awake()
    {
        SetText(idleMessage);
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
    /// Shows the saved message, then returns to the idle message.
    /// </summary>
    public void ShowSaved()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowTemporary(savedMessage));
    }

    private IEnumerator ShowTemporary(string text)
    {
        SetText(text);
        yield return new WaitForSeconds(savedDuration);
        SetText(idleMessage);
        messageRoutine = null;
    }
}
