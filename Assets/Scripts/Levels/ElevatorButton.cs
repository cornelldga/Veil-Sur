using UnityEngine;

/// <summary>
/// An elevator call button (the Up and Down cylinders outside the elevator).
///
/// On success it queues the floor of its direction to the ElevatorController
/// If there is no floor that way, nothing happens.
/// </summary>
public class ElevatorButton : PuzzleInteractable
{
    [Tooltip("Elevator this button calls. Probably the main elevator")]
    [SerializeField] private ElevatorController elevator;
    [Tooltip("Direction this button queues. Nothing happens if there is no floor that way.")]
    [SerializeField] private ElevatorDirection direction;

    /// <summary>
    /// Queues the floor in this button's direction. QueueFloor returns false
    /// when there is nothing to do.
    /// </summary>
    public override void SuccessBehavior()
    {
        if (elevator != null)
        {
            elevator.QueueFloor(direction);
        }
    }

    /// <summary>
    /// Runs when the interaction fails.
    /// </summary>
    public override void FailBehavior()
    {
        ErrorNotifier.Show("ERROR: Locked. Requires a code.");
    }
}