using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InvestigationManager : MonoBehaviour
{
    List<QuestionDefinition> current_questions;

    void AddQuestion(QuestionDefinition question)
    {
        current_questions.Add(question);
    }
}
