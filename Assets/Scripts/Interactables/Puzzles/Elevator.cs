using UnityEngine;

/// <summary>
/// This script now determines the interactable behavior.
/// </summary>
public class Elevator : PuzzleInteractable
{
    [Tooltip("Game Object with ElevatorController script. This elevator.")]
    [SerializeField] private ElevatorController controller;

    /// <summary>
    /// Opens the elevator doors through the controller.
    /// </summary>
    public override void SuccessBehavior()
    {
        if (controller != null)
        {
            controller.OpenDoors();
        }
    }

    /// <summary>
    /// Runs when the interaction fails.
    /// </summary>
    public override void FailBehavior()
    {
        //dialogue that says "I don't know the code"
    }
}
