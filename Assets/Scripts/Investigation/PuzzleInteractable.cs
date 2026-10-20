using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Requires its linked questions to be solved, then reveals questions after interaction.</summary>
public abstract class PuzzleInteractable : MonoBehaviour, Interactable
{
    [Tooltip("Questions unlocked after this puzzle is successfully interacted with.")]
    [SerializeField] private QuestionDefinition[] futureQuestions = Array.Empty<QuestionDefinition>();
    private int questionsRemaining;

    public IReadOnlyList<QuestionDefinition> FutureQuestions => futureQuestions;
    public int QuestionsRemaining => questionsRemaining;
    public bool HasBeenInteractedWith { get; private set; }
    public event Action<PuzzleInteractable> Interacted;

    internal void IncreaseQuestionsRemaining() { questionsRemaining++; }
    internal void DecreaseQuestionsRemaining() { questionsRemaining--; }

    public virtual bool IsSolved() { return questionsRemaining == 0; }

    public void Interact()
    {
        if (!IsSolved())
        {
            FailBehavior();
            return;
        }

        SuccessBehavior();
        if (HasBeenInteractedWith) return;

        HasBeenInteractedWith = true;
        Interacted?.Invoke(this);
    }

    public abstract void SuccessBehavior();
    public abstract void FailBehavior();
}
