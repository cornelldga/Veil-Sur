using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Notebook item representing a photo.
/// </summary>
public class PhotoNote : NotebookItem
{
    [SerializeField] private UnityEngine.UI.RawImage displayImage;
    [SerializeField] private List<string> subjectIds = new();

    public IReadOnlyList<string> SubjectIds() {
        return subjectIds;
    }

    public bool HasSubject(string subjectId) {
        return subjectIds.Contains(subjectId);
    }

    public void AddSubject(string subjectId) {
        if (!subjectIds.Contains(subjectId)) {
            subjectIds.Add(subjectId);
        }
    }

    public void LoadImage(Texture2D content)
    {
        displayImage.color = Color.white;
        displayImage.texture = content;
    }

    // TODO: add photo-specific code like validation
}
