using System.Collections.Generic;
using UnityEngine;

/// <summary>Provides the notebook's configured tab and registered question views.</summary>
public class NotebookUIController : MonoBehaviour
{
    [Tooltip("Photo tab belonging to this notebook prefab.")]
    [SerializeField] private PhotoTabScript photoTab;
    private readonly List<QuestionNote> questions = new();

    public PhotoTabScript PhotoTab => photoTab;
    public IReadOnlyList<QuestionNote> Questions => questions;

    public void RegisterQuestion(QuestionNote question)
    {
        if (!questions.Contains(question)) questions.Add(question);
    }

    public void RemoveQuestion(QuestionNote question) { questions.Remove(question); }
}
