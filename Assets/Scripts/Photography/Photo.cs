using UnityEngine;

/// <summary>
/// Notebook item representing a photo.
/// </summary>
public class Photo
{
    private Texture2D image;

    //subjectID is empty if there is no subject
    private string subjectId;

    public string SubjectId()
    {
        return subjectId;
    }

    public bool HasSubject(string subjectId)
    {
        return this.subjectId == subjectId;
    }

    public void SetImage(Texture2D image)
    {
        this.image = image;
    }

    public void SetSubject(string subjectId)
    {
        this.subjectId = subjectId;
    }
}
