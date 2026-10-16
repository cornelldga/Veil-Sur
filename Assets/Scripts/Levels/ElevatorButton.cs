using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ElevatorButton : MonoBehaviour
{
    [SerializeField] private ElevatorController elevator;
    [SerializeField] private ElevatorDirection direction;

    /// <summary>
    /// Useless right now. Will work on implementing functional buttons.
    /// </summary>
    public void Interact()
    {
        elevator.QueueFloor(direction);
    }
}
