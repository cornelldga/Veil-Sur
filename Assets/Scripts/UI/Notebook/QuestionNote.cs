using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Reusable question view that displays data from its bound QuestionDefinition.</summary>
public class QuestionNote : MonoBehaviour
{
    [FormerlySerializedAs("image")]
    [SerializeField] private Graphic resultGraphic;
    [Tooltip("UI text component in this variant that displays the definition's question text.")]
    [SerializeField] private TMP_Text questionLabel;
    [Tooltip("UI text component in this variant that displays the definition's current feedback.")]
    [SerializeField] private TMP_Text feedbackLabel;
    private Color originalColor;
    private QuestionDefinition definition;

    public QuestionDefinition Definition => definition;
    public Photo Photo => definition != null ? definition.Photo : null;
    public bool IsCorrect => definition != null && definition.IsCorrect;
    public bool HasBeenUploaded => definition != null && definition.HasBeenUploaded;
    public string QuestionText => definition != null ? definition.QuestionText : "";
    public string DefaultFeedback => definition != null ? definition.DefaultFeedback : "";
    public string FeedbackText => definition != null ? definition.FeedbackText : "";
    public event Action AnswerChanged;

    private void Awake()
    {
        if (resultGraphic != null) originalColor = resultGraphic.color;
    }

    private void OnEnable()
    {
        if (definition != null) definition.AnswerChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (definition != null) definition.AnswerChanged -= Refresh;
    }

    public void Bind(QuestionDefinition question)
    {
        if (isActiveAndEnabled && definition != null) definition.AnswerChanged -= Refresh;
        definition = question;
        if (isActiveAndEnabled)
        {
            if (definition != null) definition.AnswerChanged += Refresh;
            Refresh();
        }
    }

    public void Unbind() { Bind(null); }

    public void SetPhoto(Photo photo) { definition?.SetPhoto(photo); }
    public void ResetResult() { definition?.ResetResult(); }
    public void Verify() { definition?.Verify(); }

    private void Refresh()
    {
        if (questionLabel != null) questionLabel.text = QuestionText;
        if (feedbackLabel != null) feedbackLabel.text = FeedbackText;
        if (resultGraphic != null)
            resultGraphic.color = !HasBeenUploaded ? originalColor : IsCorrect ? Color.green : Color.red;
        AnswerChanged?.Invoke();
    }
}
