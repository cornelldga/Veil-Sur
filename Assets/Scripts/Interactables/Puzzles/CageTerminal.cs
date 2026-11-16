using System.Collections;
using UnityEngine;
using System.Text;
using TMPro;

/// <summary>
/// Old terminal in the storage room that displays the log for cage openings.
/// </summary>
public class CageTerminal : MonoBehaviour, Interactable
{
    [Header("Terminal Display")]
    [Tooltip("Text component on the terminal screen.")]
    [SerializeField] private TMP_Text logText;

    private string[] bootingLines =
    {
        "OYO KENNEL CONTROL LOG",
        "LOADING HISTORY...",
        "BOOTING SOFTWARE...",
        "...",
        "...",
        "...",
        "..."
    };

    private string[] logLines =
    {
        "08/27  09:12  CAGE 008",
        "09/04  01:40  CAGE 004",
        "10/09  12:15  CAGE 001",
        "10/09  12:15  CAGE 002",
        "10/09  12:15  CAGE 003",
        "10/09  12:15  CAGE 004",
        "10/09  12:15  CAGE 005",
        "10/09  12:15  CAGE 006"
    };

    [Tooltip("Seconds between typed characters.")]
    [SerializeField] private float charTypeDelay = 0.1f;

    [Tooltip("Seconds between the lines that appear upon booting the terminal.")]
    [SerializeField] private float lineDelay = 1f;

    [Tooltip("The photographable object that appears once the log text is displayed.")]
    [SerializeField] private GameObject photographableTarget;

    [Header("Audio")]
    [Tooltip("The sound that plays when the terminal powers on.")]
    [SerializeField] private AudioClip powerOnSound;
    // Maybe?
    [Tooltip("Sound played while the typing.")]
    [SerializeField] private AudioClip typingSound;

    [Tooltip("Time waited after booting before log lines are displayed.")]
    [SerializeField] private float bootDelay = 1f;

    private bool finishedBooting;
    private readonly StringBuilder buffer = new StringBuilder();

    /// <summary>
    /// Clears the terminal screen, hides photographable object.
    /// </summary>
    private void Start()
    {
        logText.text = "";
        photographableTarget.SetActive(false);
    }

    /// <summary>
    /// Called after interaction with the terminal. If the terminal has already finished booting, does nothing. Otherwise,
    /// the terminal boots and the text is typed out on the screen.
    /// </summary>
    public void Interact()
    {
        if (finishedBooting)
        {
            return;
        }

        finishedBooting = true;
        StartCoroutine(BootingCoroutine());
    }

    /// <summary>
    /// Shows the lines to be shown at the time of booting, then clears the screen, then types out the log.
    /// After the logi s finished typing, the photographable object on the screen is set to be active.
    /// </summary>
    private IEnumerator BootingCoroutine()
    {
        if (powerOnSound != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(powerOnSound, transform.position);
        }

        foreach (string line in bootingLines) {
            buffer.AppendLine(line);
            logText.text = buffer.ToString();
            yield return new WaitForSeconds(lineDelay);
        }

        buffer.Clear();
        logText.text = "";
        yield return new WaitForSeconds(bootDelay);

        foreach (string line in logLines)
        {
            yield return TypeLine(line);
            buffer.AppendLine();
            yield return new WaitForSeconds(lineDelay);
        }

        photographableTarget.SetActive(true);

    }

    /// <summary>
    /// Types out the line of text one character at a time T^T
    /// </summary>
    private IEnumerator TypeLine(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            logText.text = buffer.ToString() + line.Substring(0, i+1) + "_";

            if (typingSound != null && i % 3 == 0)
            {
                AudioManager.Instance.PlaySfxAtPosition(typingSound, transform.position);
            }

            yield return new WaitForSeconds(charTypeDelay);
        }

        buffer.Append(line);
        logText.text = buffer.ToString();
    }
}