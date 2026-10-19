using System.Collections;
using UnityEngine;

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
///     If there is no next or previous level, buttons do nothing. 
///     If no button has been pressed, then walking in does nothing.
///
/// THE RIDE:
///
///     Once the doors close, the ride runs for rideDuration. A point light
///     sweeps from the bottom of the elevator to the top to simulate travel
///     through an elevator shaft of the facility
/// 
///     It goes up and down for sweepCount times. TBD: Give sweeps an effect
///     AND give a ride sound.
///
///     The light's top and bottom should be transforms positioned above and 
///     below the front door of the elevator (natural enter and exit)
///
/// The Open and Close states are currently the door objects being inactive and
/// active. This is temporary until the animations are added.
///
/// This script should be put on the GameObject that contains a BOX COLLIDER 
/// with TRIGGER that is the interior of the elevator.
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
    [Tooltip("Seconds between the Player walking in and the doors closing")]
    [SerializeField] private float doorOpenCloseDelay;

    [Header("Ride")]
    [Tooltip("Total seconds the ride lasts")]
    [SerializeField] private float rideDuration = 8f;

    [Header("Elevator Lighting")]
    [Tooltip("The light that sweeps up and down. Can be a Point Light with an emissive mesh as a child. Turned off except during the ride.")]
    [SerializeField] private Transform shaftLight;
    [Tooltip("Start of light sweep, place an Empty Game Object below the elevator door (in front of it)")]
    [SerializeField] private Transform lightBottom;
    [Tooltip("End of light sweep, place an Empty Game Object above the elevator door (in front of it)")]
    [SerializeField] private Transform lightTop;
    [Tooltip("Number of times that the light travels bottom to top during the ride. Each sweep lasts rideDuration / sweepCount. MUST BE 1+")]
    [SerializeField] private int sweepCount = 4;
    [Tooltip("How fast the light is moving per sweep. Linear movement")]
    [SerializeField] private AnimationCurve sweepCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // [Header("SFX")]
    // [SerializeField] private AudioSource sfxSource;
    // [SerializeField] private AudioClip doorCloseSfx;
    // [SerializeField] private AudioClip sweepSfx;
    // [SerializeField] private AudioSource rideLoopSource;
    // [SerializeField] private float rideLoopMinPitch = 0.8f;
    // [SerializeField] private float rideLoopMinVolume = 0.4f;

    private State state;
    private ElevatorDirection? queuedFloor;
    private bool playerInside;
    public ElevatorDirection? QueuedFloor => queuedFloor;

    private bool HasShaftLight => shaftLight != null && lightBottom != null && lightTop != null;

    private void Start()
    {
        SetDoors(startOpen);

        if (shaftLight != null)
        {
            shaftLight.gameObject.SetActive(false);
        }
    }

    public bool QueueFloor(ElevatorDirection direction)
    {
        if (state != State.Open)
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

        if (other.GetComponentInParent<FirstPersonController>() == null) return; // I have to double check with workflow if this is best way to check

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

    /// <summary>
    /// Coroutine that closes the doors, runs the ride, 
    /// and transitions to the target scene.
    /// </summary>
    private IEnumerator TravelSequence(string targetScene)
    {
        state = State.Closing;

        yield return new WaitForSeconds(doorOpenCloseDelay);

        CloseDoors();

        yield return StartCoroutine(RunRide());

        GameManager.Instance.GoToLevel(targetScene); // Should always go to TutorialLevel right now. 
        // Might need to make the position of the elevator at the origin across all levels
    }

    /// <summary>
    /// Coroutine that runs the Ride logic
    /// 
    /// The ride is split into sweepCount movements of light. For each sweep, 
    /// the light goes from lightBottom to lightTop to simulate travel.
    /// 
    /// The sweep resets and starts back at lightBottom for the amount of 
    /// sweeps that there are.
    /// </summary>
    private IEnumerator RunRide()
    {
        float sweepDuration = rideDuration / sweepCount;

        if (HasShaftLight)
        {
            shaftLight.position = lightBottom.position;
            shaftLight.gameObject.SetActive(true);
        }

        int lastSweep = -1;

        for (float elapsed = 0f; elapsed < rideDuration; elapsed += Time.deltaTime)
        {
            float sweepProgress = elapsed / sweepDuration;
            int sweepIndex = Mathf.Min(Mathf.FloorToInt(sweepProgress), sweepCount - 1);
            float t = Mathf.Clamp01(sweepProgress - sweepIndex);

            if (sweepIndex != lastSweep)
            {
                lastSweep = sweepIndex;
            }

            if (HasShaftLight)
            {
                float eased = sweepCurve.Evaluate(t);
                shaftLight.position = Vector3.LerpUnclamped(lightBottom.position, lightTop.position, eased);
            }

            yield return null;
        }

        if (HasShaftLight)
        {
            shaftLight.gameObject.SetActive(false);
            shaftLight.position = lightBottom.position;
        }
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
        return !string.IsNullOrEmpty(sceneName);
    }

    private void OnDrawGizmosSelected()
    {
        if (lightBottom == null || lightTop == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(lightBottom.position, lightTop.position);
        Gizmos.DrawWireSphere(lightBottom.position, 0.2f);
        Gizmos.DrawWireSphere(lightTop.position, 0.2f);
    }
}