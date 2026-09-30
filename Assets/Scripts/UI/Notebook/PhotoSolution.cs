using UnityEngine;

/// <summary>
/// ScriptiableObject that confirms when photo is correct.
/// </summary>
[CreateAssetMenu(fileName = "PhotoSolution", menuName = "Notebook/Solutions/Photo Solution")]
public class PhotoSolution : SolutionRule
{
    [SerializeField] private string requiredSubjectId;

    public override bool IsCorrect(NotebookItem connected)
    {
        if (connected is not PhotoNote photoNote)
        {
            return false;
        }

        return photoNote.HasSubject(requiredSubjectId);
    }
}
