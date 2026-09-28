using UnityEngine;

public class Elevator : Puzzle, Interactable
{
    public override void PuzzleSolved()
    {
       // Elevator opens 
       GameManager.Instance.Win();
    }
}
