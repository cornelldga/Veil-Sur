using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class PuzzleInteractable : MonoBehaviour, Interactable
{
    //If you don't like serialized fields, feel free to make the code just register the children questions
    [SerializeField] List<QuestionDefinition> questions;
    [SerializeField] List<PuzzleInteractable> next_puzzles;
    //True if this PuzzleInteractable is supposed to be the first or part of the set of first puzzles that the player is supposed to solve
    [SerializeField] bool first;

    public void Awake()
    {
        if (first)
        {
            foreach (QuestionDefinition q in questions)
            {
                InvestigationManager.current_investigation_manager.AddActiveQuestion(q);
            }
        }
    }

    public bool IsSolved()
    {
        return questions.All(question => question.isSolved());
    }

    public void Interact()
    {
        //if we got all the questions solved, we can add the questions from all the next interactables
        if (IsSolved())
        {
            foreach(QuestionDefinition q in questions)
            {
                //TODO: remove questions from the list
            }
            foreach(PuzzleInteractable puzzle_interactable in next_puzzles)
            {
                foreach(QuestionDefinition q in puzzle_interactable.questions)
                {
                    InvestigationManager.current_investigation_manager.AddActiveQuestion(q);
                }
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
