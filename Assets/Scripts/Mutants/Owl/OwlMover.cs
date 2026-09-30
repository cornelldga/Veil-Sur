using UnityEngine;

public class OwlMover : EnemyMover
{
    [Header("Owl-Specific")]
    [SerializeField] protected float farSpeed = 1.5f; // Speed when far from player
    [SerializeField] protected float closeSpeed = 5f; // Speed when close to player

    /// <summary>
    /// Always move to player. If far from player, use farSpeed. If close to player and player is not
    /// looking at it, move at closeSpeed. If close to player but player is looking, freeze.
    /// </summary>
    protected override void Update()
    {
        switch (searchlight.CurrentState)
        {
            case EnemySearchlight.AlertState.Suspicious:
                agent.speed = farSpeed;
                if (player != null)
                    agent.SetDestination(player.position);
                break;
            case EnemySearchlight.AlertState.Alert:
                agent.speed = closeSpeed;

                if (player != null)
                    agent.SetDestination(player.position);
                break;
        }
    }
}
