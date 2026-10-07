using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Receives a photo and displays the question's assigned answer.</summary>
[RequireComponent(typeof(Button))]
public class QuestionSlotScript : MonoBehaviour, IDropHandler
{
    [Tooltip("Question view owning this slot within the question prefab.")]
    [SerializeField] private QuestionNote question;
    [Tooltip("Photo preview within this question slot prefab.")]
    [SerializeField] private RawImage preview;
    private Button button;
    private AspectRatioFitter fitter;
    private NotebookUIController notebook;
    public Photo Photo => question.Photo;

    private void Start()
    {
        button = GetComponent<Button>();
        fitter = preview.GetComponent<AspectRatioFitter>();
        notebook = UIManager.Instance.Notebook;
        notebook.RegisterQuestion(question);
        button.onClick.AddListener(InsertSelectedPhoto);
        question.AnswerChanged += RefreshPreview;
        RefreshPreview();
    }

    private void OnDestroy()
    {
        if (notebook == null) return;
        notebook.RemoveQuestion(question);
        question.AnswerChanged -= RefreshPreview;
        button.onClick.RemoveListener(InsertSelectedPhoto);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var source = eventData.pointerDrag.GetComponent<PhotoSlotScript>();
        if (source != null && source.DraggedPhoto != null) SetPhoto(source.DraggedPhoto);
    }

    public void InsertSelectedPhoto()
    {
        if (notebook.PhotoTab.SelectedPhoto != null) SetPhoto(notebook.PhotoTab.SelectedPhoto);
    }

    public void SetPhoto(Photo photo) { question.SetPhoto(photo); }
    public void Clear() { SetPhoto(null); }

    private void RefreshPreview()
    {
        preview.texture = Photo != null ? Photo.ImageTexture : null;
        preview.enabled = Photo != null;
        if (preview.texture != null) fitter.aspectRatio = (float)preview.texture.width / preview.texture.height;
    }
}
