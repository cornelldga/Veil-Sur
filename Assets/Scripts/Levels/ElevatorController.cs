using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public enum ElevatorDirection
{
    Up, 
    Down
}

/// <summary>
/// Elevator that moves the Player up or down a floor (Level Transition)
/// 
/// INTENDED BEHAVIOR (not fully functional yet):
/// 
///     Two buttons outside the elevator, each with a direction, queue the
///     floor to be traveled to.
/// 
///     When the Player walks in, they're temporarily frozen and the doors close
///     as the level is loaded (currently scene transition).
/// 
///     If there is no next or previous level, buttons do nothing. If no button has
///     been pressed, then walking in does nothing.
/// 
/// The Open and Close states are currently the door objects being inactive and
/// active. This is temporary until the animations are added.
/// 
/// This script should be put on the GameObject that contains a TRIGGER BOX COLLIDER
/// that is the interior of the elevator.
/// 
/// </summary>
public class ElevatorController : MonoBehaviour
{
    private enum State
    {
        Open,
        Closing, // For later animations? IDK how we are implementing this
        Closed
    }
    [Header("Go To:")]
    [Tooltip("Scene to be loaded. TBD: Replace with level ID once loading is functional")]
    [SerializeField] private string nextScene;
    [SerializeField] private string prevScene;

    [Header("Door Logic")]
    [SerializeField] private GameObject elevatorDoor;
    [SerializeField] private bool startOpen = true;

    [Header("Ride Data")]
    [SerializeField] private float rideDuration;
    [SerializeField] private float doorOpenCloseDelay;

    private State state;
    private ElevatorDirection? queuedFloor;
    private bool playerInside;
    public ElevatorDirection? QueuedFloor => queuedFloor;

    private void Start()
    {
        SetDoors(startOpen);
    }

    public bool QueueFloor(ElevatorDirection direction)
    {
        if(state != State.Open)
        {
            return false;
        }

        string scene = GetSceneFor(direction);
        if (!CanTravelTo(scene))
        {
            return false;
        }

        queuedFloor = direction;

        if (playerInside)
        {
            StartCoroutine(TravelSequence(scene));
        }

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state != State.Open) return;

        if (other.GetComponentInParent<FirstPersonController>() == null) return;

        playerInside = true;

        if (!CanTravelTo(nextScene))
        {
            return;
        }

        // Right now, always go up. Need to get interaction functional.
        queuedFloor = ElevatorDirection.Up;
        StartCoroutine(TravelSequence(nextScene));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<FirstPersonController>() != null)
        {
            playerInside = false;
        }    
    }

    private IEnumerator TravelSequence(string targetScene)
    {
        state = State.Closing;
        
        yield return new WaitForSeconds(doorOpenCloseDelay);

        CloseDoors();

        yield return new WaitForSeconds(rideDuration);

        GameManager.Instance.GoToLevel(targetScene); // Should always go to TutorialLevel right now.
    }

    public void OpenDoors()
    {
        SetDoors(true);
    }

    public void CloseDoors()
    {
        SetDoors(false);
    }

    private void SetDoors(bool open)
    {
        if (elevatorDoor != null)
        {
            elevatorDoor.SetActive(!open);
        }

        state = open ? State.Open : State.Closed;
    }

    private string GetSceneFor(ElevatorDirection direction)
    {
        return direction == ElevatorDirection.Up ? nextScene : prevScene;
    }

    private bool CanTravelTo(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }
        return true;
    }
}
