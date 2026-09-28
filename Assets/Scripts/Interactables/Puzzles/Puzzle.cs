using System.Collections.Generic;
using UnityEngine;

public abstract class Puzzle : MonoBehaviour, Interactable
{
    [HideInInspector] public GameManager.PuzzleState puzzleState;

    [SerializeField] private List<QuestionNote> questionNotes = new List<QuestionNote>();

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
        int current = 0;
        foreach (QuestionNote each in questionNotes) {
            if (each.IsCorrect)
            {
                current++;
            }
        }
        if (current==questionNotes.Count)
        {
            Solve();
        }
    }

    public abstract void PuzzleSolved();
}
