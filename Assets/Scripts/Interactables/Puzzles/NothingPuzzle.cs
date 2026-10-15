using UnityEngine;

public class NothingPuzzle : PuzzleInteractable
{
    public override void FailBehavior()
    {
       Debug.Log("You have not solved the puzzle yet.");
    }

    public override void SuccessBehavior()
    {
        Debug.Log("Puzzle solved!");
    }
}
