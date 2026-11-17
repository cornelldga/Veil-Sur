using UnityEngine;

/// <summary>
/// Potion cabinet that opens and closes its doors each time the Player interacts with it.
/// </summary>
public class PotionCabinet : MonoBehaviour, Interactable
{
    [Tooltip("Animator on the potion cabinet model with Open and Close triggers.")]
    [SerializeField] private Animator doorAnimator;

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
        Debug.Log($"[PotionCabinet] Interact called. animator={(doorAnimator != null ? doorAnimator.name : "NULL")}", this);
        if (doorAnimator == null || doorAnimator.IsInTransition(0)) return;

        AnimatorStateInfo info = doorAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[PotionCabinet] Interact. animator on '{doorAnimator.gameObject.name}', enabled={doorAnimator.enabled}, Closed={info.IsName("Closed")}, Door_Open={info.IsName("Door_Open")}, t={info.normalizedTime:F2}", this);

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
