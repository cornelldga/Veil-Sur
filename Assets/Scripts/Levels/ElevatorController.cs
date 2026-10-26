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
///     The doors start closed. Two buttons outside the elevator, each with a
///     direction, queue the floor to be traveled to and open the doors.
///     The doors close again on their own if the Player never gets in.
///
///     When the Player walks in, they're temporarily frozen and the doors close
///     as the level is loaded (currently scene transition).
///
///     If there is no next or previous level, buttons do nothing.
///     If no button has been pressed, then walking in does nothing.
///
/// DOORS:
///
///     Moved the Animator logic into this script, to properly function 
///     with the lights and transition atmosphere. Elevator and ElevatorButton 
///     now function as interactables.
///
/// THE RIDE:
///
///     Once the doors close, the ride runs for rideDuration. A point light
///     sweeps from the bottom of the elevator to the top to simulate travel
///     through an elevator shaft of the facility, sweepCount times, while
///     the ride sound plays.
///
///     The light's top and bottom should be transforms positioned above and
///     below the front door of the elevator (natural enter and exit) in prefab
///
/// This script should be put on the GameObject that contains a BOX COLLIDER
/// with TRIGGER that is the interior of the elevator.
///
/// </summary>
public class ElevatorController : MonoBehaviour
{
    private enum State
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [Header("Go To:")]
    [Tooltip("Scene to be loaded when traveling Up. TBD: Replace with level ID once loading is functional")]
    [SerializeField] private string nextScene;
    [Tooltip("Scene to be loaded when traveling Down.")]
    [SerializeField] private string prevScene;

    [Header("Door Logic")]
    [Tooltip("Animator on the elevator model with Open and Close triggers.")]
    [SerializeField] private Animator doorAnimator;
    [Tooltip("Seconds between the Player walking in and the doors closing")]
    [SerializeField] private float doorOpenCloseDelay;
    [Tooltip("Seconds the doors stay open before closing again if the Player never gets in.")]
    [SerializeField] private float autoCloseDelay = 8f;
    [SerializeField] private BoxCollider doorCollider;

    [Header("Ride")]
    [Tooltip("Total seconds the ride lasts")]
    [SerializeField] private float rideDuration = 8f;

    [Header("Elevator Lighting")]
    [Tooltip("The light that sweeps up and down. Point Light with an emissive mesh as a child. Turned off except during the ride (hopefully this time)")]
    [SerializeField] private Transform shaftLight;
    [Tooltip("Start of light sweep, place an Empty Game Object below the elevator door (in front of it)")]
    [SerializeField] private Transform lightBottom;
    [Tooltip("End of light sweep, place an Empty Game Object above the elevator door (in front of it)")]
    [SerializeField] private Transform lightTop;
    [Tooltip("Number of times that the light travels bottom to top during the ride. Each sweep lasts rideDuration / sweepCount. MUST BE 1+")]
    [SerializeField] private int sweepCount = 4;
    [Tooltip("How fast the light is moving per sweep. Linear movement")]
    [SerializeField] private AnimationCurve sweepCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("SFX")]
    [Tooltip("Played once when the ride starts. Ideally about as long as rideDuration.")]
    [SerializeField] private AudioClip rideSfx;

    private State state;
    private ElevatorDirection? queuedFloor;
    private bool playerInside;
    private bool traveling;
    private Coroutine autoCloseRoutine;
    public ElevatorDirection? QueuedFloor => queuedFloor;

    private bool HasShaftLight => shaftLight != null && lightBottom != null && lightTop != null;

    private void Start()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInChildren<Animator>();
        }

        state = State.Closed;

        if (shaftLight != null)
        {
            shaftLight.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Queues the Scene where the player should travel to and opens the doors.
    ///
    /// It is invoked by the ElevatorButton script, which passes in a
    /// direction. This way, we can allow the player to travel back up
    /// (which I think was intended at the end).
    /// </summary>
    public bool QueueFloor(ElevatorDirection direction)
    {
        if (traveling || state == State.Closing)
        {
            return false;
        }

        string scene = GetSceneFor(direction);
        if (!CanTravelTo(scene))
        {
            return false;
        }

        queuedFloor = direction;

        if (state == State.Open)
        {
            RestartAutoClose(); // Pressed again while open, so give the Player more time
        }
        else
        {
            OpenDoors();
        }

        return true;
    }

    /// <summary>
    /// Opens the doors if closed.
    /// </summary>
    public void OpenDoors()
    {
        if (traveling || state != State.Closed) return;
        doorCollider.enabled = false;
        StartCoroutine(OpenDoorsRoutine());
    }

    /// <summary>
    /// Closes the doors if open.
    /// </summary>
    public void CloseDoors()
    {
        if (traveling || state != State.Open) return;
        doorCollider.enabled = true;
        StopAutoClose();
        StartCoroutine(SetDoorsAnimated(false));
    }

    /// <summary>
    /// Checks for Player inside, and starts the trip if the doors
    /// are open and a floor has been queued. TBD: Lock movement
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;

        if (traveling || state != State.Open || !queuedFloor.HasValue)
        {
            return;
        }

        StartCoroutine(TravelSequence(GetSceneFor(queuedFloor.Value)));
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInside = false;
        }
    }

    /// <summary>
    /// Returns whether a collider belongs to the Player.
    /// </summary>
    private bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<FirstPersonController>() != null; 
        // I have to double check with workflow if this is best way to check
    }

    /// <summary>
    /// Coroutine that opens the doors. Two outcomes:
    ///     Player is inside AND there is a floor to go to. In this case,
    ///     the Travel Sequence starts
    /// 
    ///     Else, restart the doors, and automatically close them
    /// </summary>
    private IEnumerator OpenDoorsRoutine()
    {
        yield return SetDoorsAnimated(true);

        if (playerInside && queuedFloor.HasValue)
        {
            StartCoroutine(TravelSequence(GetSceneFor(queuedFloor.Value)));
        }
        else
        {
            RestartAutoClose();
        }
    }

    /// <summary>
    /// Coroutine that closes the doors, runs the ride,
    /// and transitions to the target scene.
    /// </summary>
    private IEnumerator TravelSequence(string targetScene)
    {
        traveling = true;
        StopAutoClose();

        yield return new WaitForSeconds(doorOpenCloseDelay);

        yield return SetDoorsAnimated(false);
        yield return RunRide();

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

        if (AudioManager.Instance != null && rideSfx != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(rideSfx, transform.position);
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

    /// <summary>
    /// Coroutine that will automatically close the doors 
    /// if the Player has not entered. However, the doors will not close
    /// if the Player is inside and there is no available floor.
    /// </summary>
    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        yield return new WaitUntil(() => !playerInside);

        queuedFloor = null;
        yield return SetDoorsAnimated(false);

        autoCloseRoutine = null;
    }

    /// <summary>
    /// Restarts the auto-close timer. 
    /// </summary>
    private void RestartAutoClose()
    {
        StopAutoClose();

        if (autoCloseDelay > 0f)
        {
            autoCloseRoutine = StartCoroutine(AutoClose());
        }
    }

    /// <summary>
    /// Cancels the the close timer.
    /// </summary>
    private void StopAutoClose()
    {
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }
    }

    /// <summary>
    /// Coroutine that plays the open or close animation and waits for it to
    /// finish. Both triggers are cleared first so a leftover trigger can never
    /// replay an old animation.
    /// </summary>
    /// <param name="open">True to open the doors, false to close them</param>
    private IEnumerator SetDoorsAnimated(bool open)
    {
        if (doorAnimator == null)
        {
            state = open ? State.Open : State.Closed;
            yield break;
        }

        state = open ? State.Opening : State.Closing;

        doorAnimator.ResetTrigger("Open");
        doorAnimator.ResetTrigger("Close");
        doorAnimator.SetTrigger(open ? "Open" : "Close");

        yield return WaitForDoors(open);

        state = open ? State.Open : State.Closed;
    }

    /// <summary>
    /// Coroutine that waits until the Animator finishes opening or closing.
    /// Opening is done when the Open state has played through. Closing is done
    /// when the Animator returns to the Closed state. Gives up after
    /// doorWaitTimeout seconds.
    /// </summary>
    /// <param name="open">True to wait for the doors to finish opening, false for closing</param>
    private IEnumerator WaitForDoors(bool open)
    {
        string targetState = open ? "Open" : "Closed";

        yield return null;

        // 6 is arbirtrary here. Just waiting until the animations are done
        // This way, the ride doesn't run WHILE an animation is happening.
        for (float waited = 0f; waited < 6f; waited += Time.deltaTime)
        {
            AnimatorStateInfo info = doorAnimator.GetCurrentAnimatorStateInfo(0);

            bool inTargetState = info.IsName(targetState) && !doorAnimator.IsInTransition(0);
            bool finished = !open || info.normalizedTime >= 1f;

            if (inTargetState && finished)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Returns the scene for a direction
    /// </summary>
    private string GetSceneFor(ElevatorDirection direction)
    {
        return direction == ElevatorDirection.Up ? nextScene : prevScene;
    }
 
    /// <summary>
    /// Returns whether sceneName is a valid, reachable scene
    /// </summary>
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