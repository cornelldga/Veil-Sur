using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// trash can open when hover, closed when not hover, and delete photo when drop on it
/// </summary>
[RequireComponent(typeof(Image))]
public class TrashCan : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("trash can idle")]
    [SerializeField] private Sprite closedSprite;
    [Tooltip("trash can hover")]
    [SerializeField] private Sprite openSprite;

    private const float IdleAlpha = 0.35f;
    private const float HoverAlpha = 1f;

    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
        SetOpen(false);
    }

    private void OnDisable()
    {
        if (image != null)
        {
            SetOpen(false);
        }
    }

    /// <summary>
    /// opens trash can if pointer is hovering and photo being dragged
    /// </summary>
    public void OnPointerEnter(PointerEventData pointer)
    {
        if (GetDraggedPhoto(pointer) != null)
        {
            SetOpen(true);
        }
    }

    /// <summary>
    /// closes trash can when pointer no longer on
    /// </summary>
    public void OnPointerExit(PointerEventData pointer)
    {
        SetOpen(false);
    }

    ///<summary>
    /// This is called by the event system when a draggable item is dragged and dropped on this object.
    /// Accepts a photo slot or a legacy Photo.
    /// </summary>
    public void OnDrop(PointerEventData pointer)
    {
        SetOpen(false);

        Photo photo = GetDraggedPhoto(pointer);
        if (photo == null)
        {
            return;
        }

        // UIManager.Instance.ShowDeleteConfirmation(photo);
        PhotoStorage.Instance.RemovePhoto(photo);
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
        image.color = new Color(1f, 1f, 1f, open ? HoverAlpha : IdleAlpha);
    }
}
