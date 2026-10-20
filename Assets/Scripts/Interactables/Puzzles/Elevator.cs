using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Elevator : PuzzleInteractable
{
    /// <summary>
    /// The elevator puzzle. Stays locked, showing the error box on interact, until Unlock is called
    /// and any linked questions are solved.
    /// </summary>
    [Tooltip("While true the elevator cannot be used, even if no questions are linked to it. Call Unlock() when the player finishes the puzzle.")]
    [SerializeField] private bool locked = true;

    [Tooltip("Seconds the elevator stays open before closing again.")]
    [SerializeField] private float closeDelay = 8f;

    private Animator animator;
    private Coroutine closeRoutine;

    /// <summary>
    /// The elevator only counts as solved once it is unlocked and its linked questions are answered.
    /// </summary>
    /// <return>True when the elevator can be used.</return>
    public override bool IsSolved()
    {
        return !locked && base.IsSolved();
    }

    /// <summary>
    /// Unlocks the elevator so interacting with it succeeds.
    /// </summary>
    public void Unlock()
    {
        locked = false;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public override void SuccessBehavior()
    {
        animator.SetTrigger("Open");

        // Restart the timer if the player interacts again.
        if (closeRoutine != null) StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        animator.SetTrigger("Close");
        closeRoutine = null;
    }

    public override void FailBehavior()
    {
        ErrorNotifier.Show("ERROR: elevator locked");
    }
}
