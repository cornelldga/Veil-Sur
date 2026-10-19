using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the storage-full image and "Error camera full" text briefly after the player
/// tries to take a photo while photo storage is full. Hidden otherwise.
/// </summary>
[RequireComponent(typeof(Image))]
public class StorageIndicator : MonoBehaviour
{
    [Tooltip("Sprite shown when a photo is blocked because storage is full")]
    [SerializeField] private Sprite fullSprite;
    [Tooltip("Child text shown when a photo is blocked because storage is full")]
    [SerializeField] private TextMeshProUGUI errorText;
    [Tooltip("Seconds the error stays visible after a blocked photo attempt")]
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
        PhotoStorage.OnPhotoBlocked += ShowError;
    }

    private void OnDisable()
    {
        PhotoStorage.OnPhotoBlocked -= ShowError;
        Hide();
    }

    /// <summary>
    /// Shows the full image and error text for a short time after a blocked photo attempt.
    /// </summary>
    private void ShowError()
    {
        image.color = Color.white;
        if (errorText != null)
        {
            errorText.text = "Error camera full";
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
