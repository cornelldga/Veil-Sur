using System.Collections;
using UnityEngine;

/// <summary>
/// The printer puzzle from the final level. After interacting with the printer, the printer turns on and prints out a page,
/// which must be photographed.
/// </summary>
public class Printer : MonoBehaviour, Interactable
{
    [Tooltip("The page that is printed.")]
    [SerializeField] private GameObject page;

    [Tooltip("Where the page starts.")]
    [SerializeField] private Transform pageStartPos;

    [Tooltip("Where the page ends (sticking out of printer).")]
    [SerializeField] private Transform pageEndPos;
        
    [Tooltip("How long it takes the page to print.")]
    [SerializeField] private float printTime = 2f;
    
    [Tooltip("Delay before page begins printing.")]
    [SerializeField] private float printDelay = 0.5f;

    [Tooltip("Sound played when printer starts up.")]
    [SerializeField] private AudioClip startSound;

    [Tooltip("Sound played while printing.")]
    [SerializeField] private AudioClip printSound;
    
    private bool isPluggedIn;
    private bool hasPrinted;


    private void Start()
    {
        page.SetActive(false);
    }

    /// <summary>
    /// Called when the printer is plugged in and turns the printer on. If the printer is already on, it does notihng.
    /// </summary>
    public void TurnOn()
    {
        if (isPluggedIn)
        {
            return;
        }

        isPluggedIn = true;

        if (startSound != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(startSound, transform.position);
        }
    }

    /// <summary>
    /// Called when the player interacts with the printer. The printer turns on and prints the page.
    /// If the printer has already printed the page, interaction does nothing.
    /// </summary>
    public void Interact()
    {
        if (hasPrinted)
        {
            return;
        }

        if (!isPluggedIn)
        {
            ErrorNotifier.Show("PRINTER HAS NO POWER!");
            return;
        }

        hasPrinted = true;
        StartCoroutine(Print());
    }

    /// <summary>
    /// Plays the printing sound, then prints the page out of the printer.
    /// </summary>
    private IEnumerator Print()
    {
        if (printSound != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(printSound, transform.position);
        }

        yield return new WaitForSeconds(printDelay);

        page.transform.position = pageStartPos.position;
        page.SetActive(true);

        for (float t = 0f; t < printTime; t += Time.deltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, t/printTime);
            page.transform.position = Vector3.Lerp(pageStartPos.position, pageEndPos.position, progress);
            yield return null;
        }

        page.transform.position = pageEndPos.position;
    }
}