using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Notebook item representing a question.
/// </summary>
[RequireComponent(typeof(Image))]
public class QuestionNote : NotebookItem
{
    [SerializeField] private SolutionRule rule;
    public bool IsCorrect { get; private set; }
    private Image image;
    private Color originalColor;

    private void Start()
    {
        IsCorrect = false;
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public SolutionRule Rule() {
        return rule;
    }

    // TODO: add question-specific code like changing the question text
    // and validation
    public void Verify()
    {
        StringPin connectedPin = Pin.GetConnectedPin();

        if (connectedPin == Pin || connectedPin == null)
        {
            IsCorrect = false;
            image.color = originalColor;
            return;
        }

        NotebookItem connectedNote = connectedPin == Pin ? null : connectedPin.GetComponentInParent<NotebookItem>();
        IsCorrect = rule != null && rule.IsCorrect(connectedNote);
        image.color = IsCorrect ? Color.green : Color.red;
    }
}
