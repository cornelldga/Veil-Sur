using UnityEngine;

/// <summary>
/// This class handles states for owls specifically.
/// </summary>
/// 
public class OwlSearchlight : EnemySearchlight
{   
    /// <summary>
    /// Always keeps owl in the alert state so they move towards the player.
    /// </summary>
    /// <param name="canSeePlayer"></param>
    [Header("Owl-Specific")]
    [SerializeField] protected float speedupDist = 20f; // Distance at which mutant uses closeSpeed

    private static readonly int DistanceParam = Animator.StringToHash("Distance");
    private Animator spriteAnimator;

    /// <summary>
    /// Caches the sprite child's Animator, which picks the Far, Close, or Closer art from Distance.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        spriteAnimator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Do everything in parent update except for drawing the cone mesh
    /// </summary>
    protected override void Update()
    {
        FaceWalkingDirection();
        canSeePlayer = CanSeePlayer(); // Not relevant whether mutant can actually see player, but updates distance to player
        UpdateAlertState(canSeePlayer);
        spriteAnimator.SetFloat(DistanceParam, distanceToPlayer);
    }

    /// <summary>
    /// Turns the body toward where the owl walks and holds that facing while frozen.
    /// The owl art always looks at the player, so the body never turns toward the player itself.
    /// </summary>
    private void FaceWalkingDirection()
    {
        Vector3 movement = transform.position - lastPosition;
        lastPosition = transform.position;
        movement.y = 0f;

        if (movement.sqrMagnitude < 0.000001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(movement),
            sweepSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Owl states are simpler than general mutant states. Suspicious if far from player; alert if close
    /// </summary>
    /// <param name="canSeePlayer">Whether mutant is in LOS of player</param>
    protected override void UpdateAlertState(bool canSeePlayer)
    {
        if (distanceToPlayer <= speedupDist)
        {
            SetState(AlertState.Alert);
        }
        else
        {
            SetState(AlertState.Suspicious);
        }
    }

    /// <summary>
    /// Make detection circle red if close to player and yellow if far
    /// </summary>
    /// <returns>Color of the detection circle</returns>
    protected override UnityEngine.Color GetDetectionColor()
    {
        switch (CurrentState)
        {
            case AlertState.Suspicious:
                return suspiciousColor;
            default:
                return alertColor;
        }
    }

    /// <summary>
    /// Not relevant for the owl. Do nothing.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="priority"></param>
    public virtual void ReportSense(Vector3 position, Priority priority)
    {
        return;
    }
}