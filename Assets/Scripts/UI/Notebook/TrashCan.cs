using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// trash can opens while any photo is being held, closes when released, and deletes photo when dropped on it
/// </summary>
[RequireComponent(typeof(Image))]
public class TrashCan : MonoBehaviour, IDropHandler
{
    [Tooltip("trash can idle")]
    [SerializeField] private Sprite closedSprite;
    [Tooltip("trash can while a photo is held")]
    [SerializeField] private Sprite openSprite;
    [Tooltip("sound played when a photo is deleted")]
    [SerializeField] private AudioClip crumpleClip;

    private const float idleAlpha = 0.35f;
    private const float heldAlpha = 1f;

    private Image image;

    /// <summary>
    /// caches the Image component that shows the trash can sprite
    /// </summary>
    private void Awake()
    {
        image = GetComponent<Image>();
    }

    /// <summary>
    /// starts the trash can in its closed, dimmed state
    /// </summary>
    private void Start()
    {
        SetOpen(false);
    }

    /// <summary>
    /// subscribes to photo drag events so the trash can opens while a photo is held
    /// </summary>
    private void OnEnable()
    {
        PhotoDragPreview.DraggingChanged += SetOpen;
    }

    /// <summary>
    /// unsubscribes from photo drag events and resets the trash can to closed
    /// </summary>
    private void OnDisable()
    {
        PhotoDragPreview.DraggingChanged -= SetOpen;
        SetOpen(false);
    }

    ///<summary>
    /// This is called by the event system when a draggable item is dragged and dropped on this object.
    /// Accepts a photo slot or a legacy Photo.
    /// </summary>
    /// <param name="pointer">pointer event data for the drop, containing the dragged object.</param>
    public void OnDrop(PointerEventData pointer)
    {
        Photo photo = GetDraggedPhoto(pointer);
        if (photo == null)
        {
            return;
        }

        if (PhotoStorage.Instance.RemovePhoto(photo))
        {
            AudioManager.Instance.PlaySfxAtPosition(crumpleClip, Camera.main.transform.position, pitch: 1.5f, volume: 0.5f);
        }
    }

    /// <summary>
    /// returns the photo being dragged by the pointer
    /// </summary>
    /// <param name="pointer">pointer event data containing the dragged object.</param>
    /// <returns>The dragged Photo, or null if nothing valid is being dragged.</returns>
    private Photo GetDraggedPhoto(PointerEventData pointer)
    {
        if (pointer.pointerDrag == null)
        {
            return null;
        }

        Photo photo = pointer.pointerDrag.GetComponent<Photo>();
        if (pointer.pointerDrag.TryGetComponent<IPhotoDragSource>(out var slot))
        {
            photo = slot.DraggedPhoto;
        }
        return photo;
    }

    /// <summary>
    /// swaps the icon between the open (full opacity) and closed (dimmed) frames
    /// </summary>
    /// <param name="open">true = open, false = closed</param>
    private void SetOpen(bool open)
    {
        image.sprite = (open && openSprite != null) ? openSprite : closedSprite;
        image.color = new Color(1f, 1f, 1f, open ? heldAlpha : idleAlpha);
    }
}
