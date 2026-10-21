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

    private const float IdleAlpha = 0.35f;
    private const float HeldAlpha = 1f;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Start()
    {
        SetOpen(false);
    }

    private void OnEnable()
    {
        PhotoDragPreview.DraggingChanged += SetOpen;
    }

    private void OnDisable()
    {
        PhotoDragPreview.DraggingChanged -= SetOpen;
        SetOpen(false);
    }

    ///<summary>
    /// This is called by the event system when a draggable item is dragged and dropped on this object.
    /// Accepts a photo slot or a legacy Photo.
    /// </summary>
    public void OnDrop(PointerEventData pointer)
    {
        Photo photo = GetDraggedPhoto(pointer);
        if (photo == null)
        {
            return;
        }

        // UIManager.Instance.ShowDeleteConfirmation(photo);
        if (PhotoStorage.Instance.RemovePhoto(photo))
        {
            AudioManager.Instance.PlaySfxAtPosition(crumpleClip, Camera.main.transform.position, pitch: 1.5f, volume: 0.67f);
        }
    }

    /// <summary>
    /// returns the photo being dragged by the pointer
    /// </summary>
    /// <param name="pointer">pointer event data containing the dragged object.</param>
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
        image.color = new Color(1f, 1f, 1f, open ? HeldAlpha : IdleAlpha);
    }
}
