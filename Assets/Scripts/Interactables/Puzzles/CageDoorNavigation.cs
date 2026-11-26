using UnityEngine;

/// <summary>
/// Enables the doorway blocker when the cage door is closed.
/// </summary>
public class CageDoorNavigation : MonoBehaviour
{
    [SerializeField] private GameObject doorwayBlocker;
    private Animator doorAnimator;

    private void Awake()
    {
        doorAnimator = GetComponentInParent<Animator>();
    }

    private void LateUpdate()
    {
        doorwayBlocker.SetActive(doorAnimator.GetCurrentAnimatorStateInfo(0).IsName("Closed"));
    }
}
