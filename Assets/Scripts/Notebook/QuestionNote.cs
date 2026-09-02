using UnityEngine;

/// <summary>
/// Notebook item representing a question.
/// </summary>
public class QuestionNote : NotebookItem
{
    [SerializeField] private SolutionRule rule;

    public SolutionRule Rule() {
        return rule;
    }

    // TODO: add question-specific code like changing the question text
    // and validation
}
