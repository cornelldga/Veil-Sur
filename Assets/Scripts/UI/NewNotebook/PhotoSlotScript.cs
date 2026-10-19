using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Displays, previews, and drags one stored photo.</summary>
[RequireComponent(typeof(Button))]
[DefaultExecutionOrder(-10)]
public class PhotoSlotScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPhotoDragSource
{
    [Tooltip("Photo preview belonging to this slot prefab.")]
    [SerializeField] private RawImage preview;
    private Button button;
    private AspectRatioFitter fitter;
    private readonly PhotoDragPreview drag = new();
    private int storageSlot;

    public Photo Photo { get; private set; }
    public Photo DraggedPhoto => drag.Photo;

    private void Start()
    {
        button = GetComponent<Button>();
        fitter = preview.GetComponent<AspectRatioFitter>();
        SetPhoto(null);
    }

    public void Initialize(int slot)
    {
        storageSlot = slot;
    }

    public void SetPhoto(Photo photo)
    {
        if (Photo != photo) drag.End();
        Photo = photo;
        // Keep the transparent background raycastable so empty slots can receive drops.
        button.targetGraphic.enabled = true;
        button.targetGraphic.color = Color.clear;
        preview.texture = photo != null ? photo.ImageTexture : null;
        preview.enabled = photo != null && drag.Photo == null;
        if (preview.texture != null) fitter.aspectRatio = (float)preview.texture.width / preview.texture.height;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
            UIManager.Instance.Notebook.ShowPhotoPreview(Photo);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || Photo == null) return;
        drag.Begin(Photo, preview, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        drag.Drag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || Photo != null) return;
        var source = eventData.pointerDrag.GetComponent<IPhotoDragSource>();
        if (source != null) PhotoStorage.Instance.ReturnPhoto(source.DraggedPhoto, storageSlot);
    }

    public void OnEndDrag(PointerEventData eventData) { drag.End(); }
    private void OnDisable() { drag.End(); }
}
