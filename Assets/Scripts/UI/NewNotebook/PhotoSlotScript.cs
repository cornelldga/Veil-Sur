using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Displays, selects, and drags one stored photo.</summary>
[RequireComponent(typeof(Button))]
[DefaultExecutionOrder(-10)]
public class PhotoSlotScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Photo preview belonging to this slot prefab.")]
    [SerializeField] private RawImage preview;
    private Button button;
    private AspectRatioFitter fitter;
    private PhotoTabScript photoTab;
    private RawImage dragPreview;

    public Photo Photo { get; private set; }
    public Photo DraggedPhoto { get; private set; }

    private void Start()
    {
        button = GetComponent<Button>();
        fitter = preview.GetComponent<AspectRatioFitter>();
        button.onClick.AddListener(Select);
        SetPhoto(null);
    }

    public void Initialize(PhotoTabScript tab)
    {
        photoTab = tab;
    }

    public void SetPhoto(Photo photo)
    {
        if (Photo != photo) EndDrag();
        Photo = photo;
        button.interactable = photo != null;
        button.targetGraphic.enabled = photo != null;
        preview.texture = photo != null ? photo.ImageTexture : null;
        preview.enabled = photo != null;
        if (preview.texture != null) fitter.aspectRatio = (float)preview.texture.width / preview.texture.height;
    }

    private void Select() { photoTab.SelectPhoto(Photo); }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || Photo == null) return;
        Select();
        DraggedPhoto = Photo;
        // A separate preview leaves the source slot in place and lets drops reach the UI below it.
        var ghost = new GameObject("Dragged Photo", typeof(RectTransform), typeof(RawImage));
        var dragRoot = (RectTransform)UIManager.Instance.notebookGroup.transform;
        ghost.transform.SetParent(dragRoot, false);
        dragPreview = ghost.GetComponent<RawImage>();
        dragPreview.texture = preview.texture;
        dragPreview.raycastTarget = false;
        dragPreview.rectTransform.sizeDelta = preview.rectTransform.rect.size *
            (preview.rectTransform.lossyScale.x / dragRoot.lossyScale.x);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview == null) return;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)UIManager.Instance.notebookGroup.transform, eventData.position,
            eventData.pressEventCamera, out var position))
            dragPreview.rectTransform.position = position;
    }

    public void OnEndDrag(PointerEventData eventData) { EndDrag(); }
    private void OnDisable() { EndDrag(); }
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Select);
    }

    private void EndDrag()
    {
        DraggedPhoto = null;
        if (dragPreview != null) Destroy(dragPreview.gameObject);
        dragPreview = null;
    }
}
