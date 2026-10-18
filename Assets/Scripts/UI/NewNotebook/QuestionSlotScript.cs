using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Receives a photo and displays the question's assigned answer.</summary>
[RequireComponent(typeof(Button))]
public class QuestionSlotScript : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPhotoDragSource
{
    [Tooltip("Question view owning this slot within the question prefab.")]
    [SerializeField] private QuestionNote question;
    [Tooltip("Photo preview within this question slot prefab.")]
    [SerializeField] private RawImage preview;
    private Button button;
    private AspectRatioFitter fitter;
    private NotebookUIController notebook;
    private readonly PhotoDragPreview drag = new();
    public Photo Photo => question.Photo;
    public Photo DraggedPhoto => drag.Photo;

    private void Start()
    {
        button = GetComponent<Button>();
        fitter = preview.GetComponent<AspectRatioFitter>();
        notebook = UIManager.Instance.Notebook;
        question.AnswerChanged += RefreshPreview;
        RefreshPreview();
    }

    private void OnDestroy()
    {
        if (notebook == null) return;
        question.AnswerChanged -= RefreshPreview;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var source = eventData.pointerDrag.GetComponent<IPhotoDragSource>();
        if (source != null && source.DraggedPhoto != null) SetPhoto(source.DraggedPhoto);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2 && button.IsInteractable())
            notebook.ShowPhotoPreview(Photo);
    }

    public void SetPhoto(Photo photo) { question.SetPhoto(photo); }
    public void Clear() { SetPhoto(null); }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || Photo == null || question.IsCorrect) return;
        drag.Begin(Photo, preview, eventData);
    }
    public void OnDrag(PointerEventData eventData) { drag.Drag(eventData); }
    public void OnEndDrag(PointerEventData eventData) { drag.End(); }
    private void OnDisable() { drag.End(); }

    private void RefreshPreview()
    {
        if (question.IsCorrect || drag.Photo != Photo) drag.End();
        preview.texture = Photo != null ? Photo.ImageTexture : null;
        preview.enabled = Photo != null && drag.Photo == null;
        if (preview.texture != null) fitter.aspectRatio = (float)preview.texture.width / preview.texture.height;
    }
}
