using UnityEngine;

public class Puzzle : MonoBehaviour, Interactable
{
    [HideInInspector] public GameManager.PuzzleState puzzleState;

    [SerializeField] QuestionNote questionNote;

    void Awake() {
        puzzleState = GameManager.PuzzleState.UNSOLVED;
    }

    public void Interact()
    {
        if (questionNote.IsCorrect)
        {
            puzzleState = GameManager.PuzzleState.SOLVED;
        }
    }
}
