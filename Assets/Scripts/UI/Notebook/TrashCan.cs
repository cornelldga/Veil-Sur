using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Icon photos get dragged to for deletion. When a photo is dragged and dropped onto this object,
/// it is removed from the storage and space is cleared up.
/// </summary>

public class TrashCan : MonoBehaviour, IDropHandler
{
    ///<summary>
    /// This is called by the event system when a draggable item is dragged and dropped on this object.
    /// If the dragged item is a PhotoNote, a confirmation for deletion is displayed.
    /// </summary>
    public void OnDrop(PointerEventData pointer)
    {
        if (pointer.pointerDrag == null) {
            return;
        }

        PhotoNote photo = pointer.pointerDrag.GetComponent<PhotoNote>();
        if (photo == null)
        {
            return;
        }

        UIManager.Instance.ShowDeleteConfirmation(photo);
    }
}