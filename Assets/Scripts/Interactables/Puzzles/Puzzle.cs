using System;
using UnityEngine;

public abstract class Puzzle : MonoBehaviour, Interactable
{
    [HideInInspector] public GameManager.PuzzleState puzzleState;

    [SerializeField] QuestionNote questionNote;

    void Awake() {
        puzzleState = GameManager.PuzzleState.UNSOLVED;
    }

    public void Solve()
    {
        puzzleState = GameManager.PuzzleState.SOLVED;
        PuzzleSolved();
    }

    public void Interact()
    {
        if (questionNote.IsCorrect)
        {
            Solve();
        }
    }

    public abstract void PuzzleSolved();
}
