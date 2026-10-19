using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// An elevator call button (the Up and Down cylinders outside the elevator).
///
/// On success it queues the floor in its direction and opens the elevator
/// doors. If there is no floor, nothing happens.
/// </summary>
public class ElevatorCallButton : PuzzleInteractable
{
    [SerializeField] private ElevatorController elevator;
    [SerializeField] private ElevatorDirection direction;

    public override void SuccessBehavior()
    {
        elevator.QueueFloor(direction);
    }

    public override void FailBehavior()
    {
        // TBD
    }
}