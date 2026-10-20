using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Elevator : PuzzleInteractable
{
    [Tooltip("Seconds the elevator stays open before closing again.")]
    [SerializeField] private float closeDelay = 8f;

    private Animator animator;
    private Coroutine closeRoutine;

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
        //dialogue that says "I don't know the code"
    }
}
