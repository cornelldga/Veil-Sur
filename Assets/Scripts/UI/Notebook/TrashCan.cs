using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Icon photos get dragged to for deletion. When a photo is dragged and dropped onto this object,
/// removes it from storage immediately while the confirmation UI is unfinished.
/// </summary>

public class TrashCan : MonoBehaviour, IDropHandler
{
    ///<summary>
    /// This is called by the event system when a draggable item is dragged and dropped on this object.
    /// Accepts a photo slot or a legacy Photo.
    /// </summary>
    public void OnDrop(PointerEventData pointer)
    {
        if (pointer.pointerDrag == null) {
            return;
        }

        Photo photo = pointer.pointerDrag.GetComponent<Photo>();
        if (pointer.pointerDrag.TryGetComponent<PhotoSlotScript>(out var slot))
        {
            photo = slot.DraggedPhoto;
        }
        if (photo == null)
        {
            return;
        }

        // UIManager.Instance.ShowDeleteConfirmation(photo);
        PhotoStorage.Instance.RemovePhoto(photo);
    }
}
