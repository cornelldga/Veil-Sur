using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Pawprint puzzle at the beginning of Level One. This puzzle is solved when the question regarding where
/// the mutants went is answered correctly.
/// </summary>
public class PawprintPuzzle : MonoBehaviour
{
    [Tooltip("The question about mutants' direction.")]
    [SerializeField] private QuestionDefinition question;

    // nextPuzzles isn't used yet but included just in case completion of this puzzle should prompt the next
    // questions to appear
    [Tooltip("Puzzles whose questions become active once this is solved.")]
    [SerializeField] private List<PuzzleInteractable> nextPuzzles;
    
    [Tooltip("Triggered when the question is answered correctly.")]
    [SerializeField] private UnityEvent onSolved;

    private bool solved;

    /// <summary>
    /// Called when the answer to the question changes (photo attached changes).
    /// Solves the puzzle when correct.
    /// note: nothing calls this yet, need mental map UI
    /// </summary>
    public void CheckSolved()
    {
        if (solved || !question.isSolved())
        {
            return;
        }

        solved = true;
        // later, add activation of nextpuzzle questions (if this puzzle does that)
        onSolved.Invoke();
    }
}