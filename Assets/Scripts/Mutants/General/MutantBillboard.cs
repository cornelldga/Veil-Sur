using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 8-direction billboard for 2.5D mutants. Goes on a child of the mutant root: the root's
/// rotation is the mutant's real facing, and this child always turns to face the player camera.
/// Tells the Animator which of the 8 angles the camera sees and whether the mutant is moving;
/// the Animator's blend trees pick and play the clips.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class MutantBillboard : MonoBehaviour
{
    private const int DirectionCount = 8;
    private const float DegreesPerDirection = 360f / DirectionCount;

    private static readonly int DirectionParam = Animator.StringToHash("Direction");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");

    [Tooltip("Agent speed above which the mutant counts as moving")]
    [SerializeField] private float moveThreshold = 0.1f;

    private Animator animator;
    private Transform root;
    private NavMeshAgent agent;

    /// <summary>
    /// Caches the Animator, the mutant root, and the root's NavMeshAgent.
    /// </summary>
    private void Start()
    {
        animator = GetComponent<Animator>();
        root = transform.parent;
        agent = GetComponentInParent<NavMeshAgent>();
    }

    /// <summary>
    /// Turns the sprite toward the player camera and updates the Animator parameters.
    /// Runs in LateUpdate so the AI has already rotated the root this frame.
    /// </summary>
    private void LateUpdate()
    {
        Camera playerCamera = GameManager.PlayerCamera;

        Vector3 toCamera = playerCamera.transform.position - root.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-toCamera);

        animator.SetFloat(DirectionParam, GetDirectionIndex(toCamera));
        animator.SetBool(IsMovingParam, agent.velocity.magnitude > moveThreshold);
    }

    /// <summary>
    /// Finds which of the 8 drawn angles the camera is seeing, from the root's facing.
    /// </summary>
    /// <param name="toCamera">Flattened vector from the root to the camera.</param>
    /// <returns>0 = Front, counting clockwise in 45 degree slices (1 = SE, 2 = Right, ... 7 = SW).</returns>
    private int GetDirectionIndex(Vector3 toCamera)
    {
        Vector3 facing = root.forward;
        facing.y = 0f;

        float angle = Vector3.SignedAngle(facing, toCamera, Vector3.up);
        return (Mathf.RoundToInt(angle / DegreesPerDirection) + DirectionCount) % DirectionCount;
    }
}
