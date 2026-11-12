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
///     When the Player walks in, they lose control and are eased to the
///     center of the elevator (playerStandPoint), turned to face the doors,
///     while the doors close and the next scene loads in the background.
///
///     In the next scene, the Player starts at the same spot relative to the
///     elevator, and gets control back as the doors open.
///
///     If there is no next or previous level, buttons do nothing.
///     If no button has been pressed, then walking in does nothing.
///
/// THIS SCRIPT is now the logic: what is queued, when the doors open or close,
/// and the trip itself. It hands the details off to two companions:
///
///     ElevatorDoors        Door animation and open/closed state
///     ElevatorRideEffects  Shaft lights, shaft cover, and ride sound
///
/// THE RIDE:
///
///     Once the doors close, the ride runs for rideDuration. The same ride
///     plays when the Player arrives from another scene.
///
/// This script should be put on the GameObject that contains a BOX COLLIDER
/// with TRIGGER that is the interior of the elevator. ElevatorDoors and
/// ElevatorRideEffects go on that same GameObject.
///
/// </summary>
[RequireComponent(typeof(ElevatorDoors))]
[RequireComponent(typeof(ElevatorRideEffects))]
public class ElevatorController : MonoBehaviour
{
    [Header("Go To:")]
    [Tooltip("Scene to be loaded when traveling Up. TBD: Replace with level ID once loading is functional")]
    [SerializeField] private string nextScene;
    [Tooltip("Scene to be loaded when traveling Down.")]
    [SerializeField] private string prevScene;

    [Header("Timing")]
    [Tooltip("Seconds between the Player walking in and the doors closing. Also the pause before the doors open on arrival.")]
    [SerializeField] private float doorOpenCloseDelay;
    [Tooltip("Seconds the doors stay open before closing again if the Player never gets in.")]
    [SerializeField] private float autoCloseDelay = 8f;
    [Tooltip("Total seconds the ride lasts")]
    [SerializeField] private float rideDuration = 5f;

    [Header("Player Placement")]
    [Tooltip("Where the Player is walked to inside the Elevator prefab. Should be where they start Levels.")]
    [SerializeField] private Transform playerStandPoint;
    [Tooltip("Seconds it will take to walk the Player to playerStandPoint and make them face the doors")]
    [SerializeField] private float scriptedDuration = 1.2f;
    [Tooltip("Camera pitch while standing in the elevator, in degrees. Positive looks down.")]
    [SerializeField] private float lookPitch = 15f;

    private ElevatorDoors doors;
    private ElevatorRideEffects effects;
    private ElevatorDirection? queuedFloor;
    private bool playerInside;
    private bool traveling;
    private Coroutine autoCloseRoutine;
    public ElevatorDirection? QueuedFloor => queuedFloor;

    /// <summary>
    /// Finds the companion components, hides the ride effects, and starts the
    /// arrival if the Player came here by elevator.
    /// </summary>
    private void Start()
    {
        doors = GetComponent<ElevatorDoors>();
        effects = GetComponent<ElevatorRideEffects>();

        bool arriving = GameManager.Instance.ArrivingByElevator;
        effects.Prepare(arriving);

        if (arriving)
        {
            GameManager.Instance.ArrivingByElevator = false; 
            StartCoroutine(ArrivalSequence());
        }
    }

    /// <summary>
    /// Queues the Scene where the player should travel to and opens the doors.
    ///
    /// It is invoked by the ElevatorButton script, which passes in a
    /// direction. This way, we can allow the player to travel back up
    /// (which I think was intended at the end).
    /// </summary>
    /// <param name="direction">Which way the button points</param>
    /// <returns>True if a floor was queued, false if there is no floor that way or the elevator is busy</returns>
    public bool QueueFloor(ElevatorDirection direction)
    {
        if (traveling || doors.IsClosing)
        {
            return false;
        }

        string scene = GetSceneFor(direction);
        if (!CanTravelTo(scene))
        {
            return false;
        }

        queuedFloor = direction;

        if (doors.IsOpen)
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
    /// Opens the doors if closed. Also clears the dialogue and tutorial prompt, since the level is over.
    /// </summary>
    public void OpenDoors()
    {
        if (traveling || !doors.IsClosed) return;

        if (DialogueManager.Instance != null) DialogueManager.Instance.CancelDialogue();
        if (TutorialManager.Instance != null) TutorialManager.Instance.Hide();

        StartCoroutine(OpenDoorsRoutine());
    }

    /// <summary>
    /// Closes the doors if open.
    /// </summary>
    public void CloseDoors()
    {
        if (traveling || !doors.IsOpen) return;
        StopAutoClose();
        StartCoroutine(doors.Close());
    }

    /// <summary>
    /// Checks for Player inside, and starts the trip if the doors
    /// are open and a floor has been queued.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;

        if (traveling || !doors.IsOpen || !queuedFloor.HasValue)
        {
            return;
        }

        StartCoroutine(TravelSequence(GetSceneFor(queuedFloor.Value)));
    }

    /// <summary>
    /// Records that the Player left the elevator.
    /// </summary>
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
        yield return doors.Open();

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
    /// and transitions to the target scene. The scene loads in the background
    /// during the ride and is only switched to once the ride has finished.
    /// </summary>
    /// <param name="targetScene">Name of the scene to travel to</param>
    private IEnumerator TravelSequence(string targetScene)
    {
        traveling = true;
        StopAutoClose();

        yield return new WaitForSeconds(doorOpenCloseDelay);

        // Guard agains the player leaving DURING the delay
        if (!playerInside)
        {
            traveling = false;
            RestartAutoClose();
            yield break;
        }

        GameManager.Instance.PreloadScene(targetScene);

        Coroutine scriptedCentering = StartCoroutine(CenterPlayer());
        yield return doors.Close();
        yield return effects.Play(rideDuration, true);

        // Waiting in Elevator UNTIL scene is loaded (even if default ride duration has ended)
        yield return new WaitUntil(() => GameManager.Instance.IsScenePreloaded);

        SavePlayerPose();
        GameManager.Instance.ArrivingByElevator = true;
        _ = GameManager.Instance.ActivatePreloadedScene();
    }

    /// <summary>
    /// Coroutine that takes control of the Player and eases them to the stand
    /// point, turning them to face the doors.
    /// </summary>
    private IEnumerator CenterPlayer()
    {
        FirstPersonController player = GameManager.PlayerInstance.GetComponent<FirstPersonController>();
        player.BeginScriptedMovement();

        float startYaw = player.Yaw;
        float startPitch = player.Pitch;
        Vector3 startPos = player.transform.position;
        Vector3 targetPos = playerStandPoint.position;
        float targetYaw = playerStandPoint.eulerAngles.y;

        for (float t = 0f; t < scriptedDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / scriptedDuration);

            Vector3 dest = Vector3.Lerp(startPos, targetPos, k);
            Vector3 delta = dest - player.transform.position;
            delta.y = 0f;

            player.GuidePlayerBy(delta, Mathf.LerpAngle(startYaw, targetYaw, k), Mathf.Lerp(startPitch, lookPitch, k));

            yield return null;
        }
    }

    /// <summary>
    /// Coroutine that runs when the Player arrives by the elevator.
    /// Places the Player where they stood in the last elevator, runs the
    /// ride, and opens the doors after doorOpenCloseDelay.
    /// </summary>
    private IEnumerator ArrivalSequence()
    {
        FirstPersonController player = GameManager.PlayerInstance.GetComponent<FirstPersonController>();

        Pose pose = GameManager.Instance.ArrivalPose;
        Vector3 position = transform.TransformPoint(pose.position);
        float yaw = (transform.rotation * pose.rotation).eulerAngles.y;
        player.PlacePositionAt(position, yaw, lookPitch);
        
        player.BeginScriptedMovement();

        yield return effects.Play(rideDuration, false);
        yield return new WaitForSeconds(doorOpenCloseDelay);
        
        player.EndScriptedMovement();
        OpenDoors();
    }

    /// <summary>
    /// Stores where the Player is standing relative to this Elevator object.
    /// The Elevator in the next scene will use this pose to place the Player.
    /// </summary>
    private void SavePlayerPose()
    {
        Transform player = GameManager.PlayerInstance.transform;

        GameManager.Instance.ArrivalPose = new Pose(
            transform.InverseTransformPoint(player.position), 
            Quaternion.Inverse(transform.rotation) * player.rotation);
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
        yield return doors.Close();

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
    /// Returns the scene for a direction
    /// </summary>
    /// <param name="direction">Direction of travel</param>
    private string GetSceneFor(ElevatorDirection direction)
    {
        return direction == ElevatorDirection.Up ? nextScene : prevScene;
    }
 
    /// <summary>
    /// Returns whether sceneName is a valid, reachable scene
    /// </summary>
    /// <param name="sceneName">Scene name to check</param>
    private bool CanTravelTo(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName);
    }
}