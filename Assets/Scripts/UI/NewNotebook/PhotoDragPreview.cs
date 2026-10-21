using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPhotoDragSource
{
    Photo DraggedPhoto { get; }
}

/// <summary>Shared drag preview for storage and question slots.</summary>
public class PhotoDragPreview
{
    private RawImage image;
    private RawImage sourcePreview;
    private bool sourceWasEnabled;
    private RectTransform root;
    public Photo Photo { get; private set; }

    /// <summary>Raised with true when any photo drag starts and false when it ends.</summary>
    public static event System.Action<bool> DraggingChanged;

    public void Begin(Photo photo, RawImage preview, PointerEventData eventData)
    {
        End();
        Photo = photo;
        root = (RectTransform)UIManager.Instance.notebookGroup.transform;
        var ghost = new GameObject("Dragged Photo", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(root, false);
        image = ghost.GetComponent<RawImage>();
        image.texture = photo.ImageTexture;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = preview.rectTransform.rect.size *
            (preview.rectTransform.lossyScale.x / root.lossyScale.x);
        sourcePreview = preview;
        sourceWasEnabled = preview.enabled;
        sourcePreview.enabled = false;
        Drag(eventData);
        DraggingChanged?.Invoke(true);
    }

    public void Drag(PointerEventData eventData)
    {
        if (image == null) return;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            root, eventData.position, eventData.pressEventCamera, out var position))
            image.rectTransform.position = position;
    }

    public void End()
    {
        bool wasDragging = Photo != null;
        if (sourcePreview != null) sourcePreview.enabled = sourceWasEnabled;
        sourcePreview = null;
        Photo = null;
        if (image != null) Object.Destroy(image.gameObject);
        image = null;
        if (wasDragging) DraggingChanged?.Invoke(false);
    }
}
