using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Receives a photo and displays the question's assigned answer.</summary>
public class QuestionSlotScript : MonoBehaviour, IDropHandler
{
    [SerializeField] private PhotoTabScript photoTab;
    [SerializeField] private QuestionNote question;
    [SerializeField] private Button button;
    [SerializeField] private RawImage preview;
    [SerializeField] private AspectRatioFitter fitter;
    public Photo Photo => question.Photo;

    private void Awake()
    {
        button.onClick.AddListener(InsertSelectedPhoto);
        question.AnswerChanged += RefreshPreview;
        RefreshPreview();
    }

    private void OnDestroy()
    {
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
        if (photoTab.SelectedPhoto != null) SetPhoto(photoTab.SelectedPhoto);
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
