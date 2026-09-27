using UnityEngine;

public class Elevator : Puzzle, Interactable
{
    public override void PuzzleSolved()
    {
       // Elevator opens 
       Debug.Log("YOU WIN");
    }
}
