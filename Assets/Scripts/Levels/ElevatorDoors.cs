using System.Collections;
using UnityEngine;

/// <summary>
/// Plays the elevator doors' open and close animations, and tracks whether
/// the doors are closed, opening, open, or closing.
///
/// While the doors are anything but fully open, the door collider is on so it
/// blocks the doorway. Once they're open, it turns off so the Player can walk through.
///
/// This script only decides HOW the doors move. ElevatorController decides
/// WHEN they open or close.
///
/// This script should be put on the same GameObject as the ElevatorController.
/// </summary>
public class ElevatorDoors : MonoBehaviour
{
    private enum State
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [Tooltip("Animator on the elevator model with Open and Close triggers. If left empty, it is found in the children on Start.")]
    [SerializeField] private Animator doorAnimator;
    [Tooltip("Collider that blocks the doorway while the doors are not fully open.")]
    [SerializeField] private BoxCollider doorCollider;

    private State _state;
    private State state
    {
        get => _state;
        set
        {
            _state = value;
            if (doorCollider != null)
            {
                doorCollider.enabled = _state != State.Open;
            }
        }
    }

    public bool IsClosed => state == State.Closed;
    public bool IsOpen => state == State.Open;
    public bool IsClosing => state == State.Closing;

    /// <summary>
    /// Start with closed doors.
    /// </summary>
    private void Start()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInChildren<Animator>();
        }

        state = State.Closed;
    }

    /// <summary>
    /// Coroutine that plays the open animation and finishes once the doors are open.
    /// </summary>
    public IEnumerator Open()
    {
        return Play(true);
    }

    /// <summary>
    /// Coroutine that plays the close animation and finishes once the doors are closed.
    /// </summary>
    public IEnumerator Close()
    {
        return Play(false);
    }

    /// <summary>
    /// Coroutine that plays the open or close animation and waits for it to
    /// finish. Both triggers are cleared first so a leftover trigger can never
    /// replay an old animation.
    /// </summary>
    /// <param name="open">True to open the doors, false to close them</param>
    private IEnumerator Play(bool open)
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

        yield return WaitForAnimation(open);

        state = open ? State.Open : State.Closed;
    }

    /// <summary>
    /// Coroutine that waits until the Animator finishes opening or closing.
    /// Opening is done when the Open state has played through. Closing is done
    /// when the Animator returns to the Closed state. Gives up after 6 seconds.
    /// </summary>
    /// <param name="open">True to wait for the doors to finish opening, false for closing</param>
    private IEnumerator WaitForAnimation(bool open)
    {
        string targetState = open ? "Open" : "Closed";

        yield return null;

        // 6 is arbitrary here. Just waiting until the animations are done
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
}