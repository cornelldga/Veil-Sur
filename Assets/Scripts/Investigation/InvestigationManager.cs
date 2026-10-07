using UnityEngine;
using System.Collections.Generic;

public class InvestigationManager : MonoBehaviour
{
    private readonly List<QuestionDefinition> current_questions = new();
    public IReadOnlyList<QuestionDefinition> Questions => current_questions;
    public static InvestigationManager current_investigation_manager;

    private void Awake()
    {
        current_investigation_manager = this;
    }

    public void RemoveActiveQuestion(QuestionDefinition question) { current_questions.Remove(question); }

    public void AddActiveQuestion(QuestionDefinition question)
    {
        if (!current_questions.Contains(question)) current_questions.Add(question);
    }
}
