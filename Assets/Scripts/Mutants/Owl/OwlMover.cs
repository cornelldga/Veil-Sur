using UnityEngine;

public class OwlMover : EnemyMover
{
    [Header("Owl-Specific")]
    [SerializeField] protected float farSpeed = 1.5f; // Speed when far from player
    [SerializeField] protected float closeSpeed = 5f; // Speed when close to player
    
    [SerializeField] private Transform targetObject;
    [SerializeField] private float viewAngleThreshold = 60f; // Degrees off-center allowed for player viewing detection

    [SerializeField] private float acceleration = 100f;

    protected bool isPlayerLooking;

    protected override void Start()
    {
        base.Start();
        agent.acceleration = acceleration;
    }
    

    /// <summary>
    /// Always move to player. If far from player, use farSpeed. If close to player and player is not
    /// looking at it, move at closeSpeed. If close to player but player is looking, freeze.
    /// </summary>
    protected override void Update()
    {
        IsPlayerLooking();

        switch (searchlight.CurrentState)
        {
            case EnemySearchlight.AlertState.Suspicious:
                agent.speed = isPlayerLooking ? 0 : farSpeed;
                if (player != null)
                    agent.SetDestination(player.position);
                break;
            case EnemySearchlight.AlertState.Alert:
                agent.speed = isPlayerLooking ? 0 : closeSpeed;
                if (player != null)
                    agent.SetDestination(player.position);
                break;
        }
    }

    /// <summary>
    /// Sets the variable representing whether the player is looking at this mutant. Used to determine when to freeze.
    /// </summary>
    protected void IsPlayerLooking()
        {
            if (player == null) return;

            // 1. Get the direction vector from the camera to the target object
            Vector3 directionToTarget = (player.position - transform.position).normalized;

            // 2. Calculate the angle between the player's forward vector and the target direction vector
            float angle = Vector3.Angle(player.forward, directionToTarget);

            // 3. Check if the angle is within your field-of-view threshold
            isPlayerLooking = (searchlight.canSeePlayer && (180 - angle) < viewAngleThreshold);
            return;
        }


}
