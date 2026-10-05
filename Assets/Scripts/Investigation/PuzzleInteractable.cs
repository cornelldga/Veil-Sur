using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class PuzzleInteractable : MonoBehaviour, Interactable
{
    [SerializeField] List<QuestionDefinition> questions;
    [SerializeField] List<PuzzleInteractable> next_puzzles;

    public bool IsSolved()
    {
        return questions.All(question => question.isSolved());
    }

    public void Interact()
    {
        //if we got all the questions solved, we can add the questions from all the next interactables
        if (IsSolved())
        {
            foreach(PuzzleInteractable puzzle_interactable in next_puzzles)
            {

            }
            //notify investigation manager of new questions
            SuccessBehavior();
        } else
        {
            FailBehavior();
        }
    }

    /// <summary>
    /// Any extra behavior that a PuzzleInteractable does if it is successful
    /// </summary>
    public abstract void SuccessBehavior();

    /// <summary>
    /// Any extra behavior that a PuzzleInteractable does if the player doesn't have all questions solved
    /// </summary>
    public abstract void FailBehavior();


}
