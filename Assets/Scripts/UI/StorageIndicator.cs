using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the storage indicator image and "Error camera full" text
/// whenever photo storage is full.
/// </summary>
[RequireComponent(typeof(Image))]
public class StorageIndicator : MonoBehaviour
{
    [Tooltip("Sprite shown when photo storage is full")]
    [SerializeField] private Sprite fullSprite;
    [Tooltip("Child text shown when photo storage is full")]
    [SerializeField] private TextMeshProUGUI errorText;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.sprite = fullSprite;
    }

    private void Start()
    {
        UpdateSprite(PhotoStorage.Instance.IsPhotoStorageFull());
    }

    private void OnEnable()
    {
        PhotoStorage.OnStorageChanged += UpdateSprite;
    }

    private void OnDisable()
    {
        PhotoStorage.OnStorageChanged -= UpdateSprite;
    }

    /// <summary>
    /// Shows or hides the full sprite and error text.
    /// </summary>
    /// <param name="isFull"> true if photo storage is at max capacity.</param>
    private void UpdateSprite(bool isFull)
    {
        image.color = isFull ? Color.white : Color.clear;

        if (errorText != null)
        {
            errorText.text = "Error camera full";
            errorText.enabled = isFull;
        }
        }
}
