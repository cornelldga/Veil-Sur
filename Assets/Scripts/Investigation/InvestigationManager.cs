using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InvestigationManager : MonoBehaviour
{
    List<QuestionDefinition> current_questions;
    public static InvestigationManager current_investigation_manager;

    private void Awake()
    {
        current_investigation_manager = this;
    }

    public void AddActiveQuestion(QuestionDefinition question)
    {
        current_questions.Add(question);
    }
}
