using UnityEngine;

/// <summary>
/// Notebook item representing a photo.
/// </summary>
public class PhotoNote : NotebookItem
{
    [SerializeField] private UnityEngine.UI.RawImage displayImage;
    // subjectID, empty if no subject
    [SerializeField] private string subjectId;

    public string SubjectId()
    {
        return subjectId;
    }

    public bool HasSubject(string subjectId)
    {
        return this.subjectId == subjectId;
    }

    public void SetSubject(string subjectId)
    {
        this.subjectId = subjectId;
    }

    public void LoadImage(Texture2D content)
    {
        displayImage.color = Color.white;
        displayImage.texture = content;
    }

    // TODO: add photo-specific code like validation
}
