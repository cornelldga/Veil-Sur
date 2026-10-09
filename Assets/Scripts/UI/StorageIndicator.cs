using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps the storage indicator image between a "not full" and a "full" sprite
/// whenever the photo storage changes.
/// </summary>
[RequireComponent(typeof(Image))]
public class StorageIndicator : MonoBehaviour
{
    [Tooltip("Sprite shown while photo storage has space")]
    [SerializeField] private Sprite notFullSprite;
    [Tooltip("Sprite shown when photo storage is full")]
    [SerializeField] private Sprite fullSprite;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
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
    /// Shows the full sprite or the not-full sprite.
    /// </summary>
    /// <param name="isFull"> true if photo storage is at max capacity.</param>
    private void UpdateSprite(bool isFull)
    {
        image.sprite = isFull ? fullSprite : notFullSprite;

        //uncomment if want no image for storage not full
        image.color = isFull ? Color.white : Color.clear;
    }
}
