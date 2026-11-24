using UnityEngine;

/// <summary>
/// Potion cabinet that opens and closes its doors each time the Player interacts with it.
/// </summary>
public class PotionCabinet : MonoBehaviour, Interactable, IHighlightOverride
{
    [Tooltip("Animator on the potion cabinet model with Open and Close triggers.")]
    [SerializeField] private Animator doorAnimator;

    [Tooltip("Object that glows when looked at (the Door child). Leave empty to glow the whole cabinet.")]
    [SerializeField] private GameObject highlightObject;

    public GameObject HighlightObject => highlightObject;

    private void Start()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Opens the doors if closed, closes them if open. Ignored while the doors are moving.
    /// </summary>
    public void Interact()
    {
        if (doorAnimator == null || doorAnimator.IsInTransition(0)) return;

        AnimatorStateInfo info = doorAnimator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName("Closed"))
        {
            doorAnimator.SetTrigger("Open");
        }
        else if (info.IsName("Door_Open") && info.normalizedTime >= 1f)
        {
            doorAnimator.SetTrigger("Close");
        }
    }
}
