using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// General-purpose error box. Shows its image and the message text briefly whenever
/// ErrorNotifier.Show is called (e.g. storage full, locked elevator). Hidden otherwise.
/// </summary>
[RequireComponent(typeof(Image))]
public class ErrorBox : MonoBehaviour
{
    [Tooltip("Sprite shown as the error box background")]
    [SerializeField] private Sprite fullSprite;
    [Tooltip("Child text that displays the error message")]
    [SerializeField] private TextMeshProUGUI errorText;
    [Tooltip("Seconds the error stays visible after it is triggered")]
    [SerializeField] private float errorDuration = 1.5f;

    private Image image;
    private Coroutine errorRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.sprite = fullSprite;
        Hide();
    }

    private void OnEnable()
    {
        ErrorNotifier.OnError += ShowError;
    }

    private void OnDisable()
    {
        ErrorNotifier.OnError -= ShowError;
        Hide();
    }

    /// <summary>
    /// Shows the error box with the given text for a short time.
    /// </summary>
    /// <param name="message">Text to display in the error box.</param>
    private void ShowError(string message)
    {
        image.color = Color.white;
        if (errorText != null)
        {
            errorText.text = message;
            errorText.enabled = true;
        }
        if (errorRoutine != null) StopCoroutine(errorRoutine);
        errorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorDuration);
        Hide();
    }

    /// <summary>
    /// Hides the image and error text.
    /// </summary>
    private void Hide()
    {
        image.color = Color.clear;
        if (errorText != null) errorText.enabled = false;
        errorRoutine = null;
    }
}
