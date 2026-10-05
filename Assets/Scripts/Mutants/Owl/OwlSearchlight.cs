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

    /// <summary>
    /// Do everything in parent update except for drawing the cone mesh
    /// </summary>
    protected override void Update()
    {
        FaceTowardsMovement();
        canSeePlayer = CanSeePlayer(); // Not relevant whether mutant can actually see player, but updates distance to player
        UpdateAlertState(canSeePlayer);
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