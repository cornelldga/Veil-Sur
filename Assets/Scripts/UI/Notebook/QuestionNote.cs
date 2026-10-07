using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Displays a backend question and forwards answer/submission input to it.</summary>
public class QuestionNote : MonoBehaviour
{
    [SerializeField] private QuestionDefinition definition;
    [FormerlySerializedAs("image")]
    [SerializeField] private Graphic resultGraphic;
    [SerializeField] private TMP_Text questionLabel;
    [SerializeField] private TMP_Text feedbackLabel;
    private Color originalColor;

    public QuestionDefinition Definition => definition;
    public Photo Photo => definition.Photo;
    public bool IsCorrect => definition.IsCorrect;
    public bool HasBeenUploaded => definition.HasBeenUploaded;
    public string QuestionText => definition.QuestionText;
    public string DefaultFeedback => definition.DefaultFeedback;
    public event Action AnswerChanged
    {
        add { definition.AnswerChanged += value; }
        remove { definition.AnswerChanged -= value; }
    }

    private void Awake()
    {
        if (resultGraphic != null) originalColor = resultGraphic.color;
        definition.AnswerChanged += Refresh;
    }
    private void Start() { Refresh(); }
    private void OnDestroy() { definition.AnswerChanged -= Refresh; }

    public void SetPhoto(Photo photo) { definition.SetPhoto(photo); }
    public void ResetResult() { definition.ResetResult(); }
    public void Verify() { definition.Verify(); }
    public SolutionRule Rule() { return definition.Rule(); }
    public string Feedback(string subjectId) { return definition.Feedback(subjectId); }

    private void Refresh()
    {
        if (questionLabel != null && !string.IsNullOrEmpty(QuestionText)) questionLabel.text = QuestionText;
        if (feedbackLabel != null) feedbackLabel.text = definition.FeedbackText;
        if (resultGraphic != null)
            resultGraphic.color = !HasBeenUploaded ? originalColor : IsCorrect ? Color.green : Color.red;
    }
}
