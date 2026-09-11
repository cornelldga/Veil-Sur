using UnityEngine;
public class EnemyMover : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointTolerance = 0.3f;

    [Header("Suspicious")]
    [SerializeField] private float suspiciousSpeed = 3f;

    [Header("Alert")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private Transform player;

    private UnityEngine.AI.NavMeshAgent agent;
    private EnemySearchlight searchlight;
    private int currentPatrolIndex;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        searchlight = GetComponent<EnemySearchlight>();
        agent.updateRotation = false;
    }

    private void Update()
    {
        switch (searchlight.CurrentState)
        {
            case EnemySearchlight.AlertState.Patrol:
                Patrol();
                break;
            case EnemySearchlight.AlertState.Suspicious:
                agent.speed = suspiciousSpeed;
                agent.SetDestination(searchlight.LastKnownPlayerPosition);
                break;
            case EnemySearchlight.AlertState.Alert:
                agent.speed = chaseSpeed;
                if (player != null) agent.SetDestination(player.position);
                break;
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        agent.speed = patrolSpeed;
        Transform target = patrolPoints[currentPatrolIndex];
        agent.SetDestination(target.position);

        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }
}