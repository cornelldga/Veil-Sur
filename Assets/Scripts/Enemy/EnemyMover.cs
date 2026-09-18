using UnityEngine;
/// <summary>
/// This class handles enemy movement.
/// </summary>
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

    [Header("Investigate")]
    /** How long the mutant waits at a wander point before choosing a new one */
    [SerializeField] private float waitTime = 2f;
    /** Area around investigation target that the mutant can check */
    [SerializeField] private float wanderRadius = 5f;

    private UnityEngine.AI.NavMeshAgent agent;
    private EnemySearchlight searchlight;
    private int currentPatrolIndex;
    private Vector3 investigateTarget;
    private bool reachedInvestigateTarget;
    private bool reachedWanderTarget;
    private float nextWanderTime = 0f;

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

                if (player != null && searchlight.canSeePlayer)
                    agent.SetDestination(player.position);
                else
                    agent.SetDestination(searchlight.LastKnownPlayerPosition);

                break;
            case EnemySearchlight.AlertState.Investigate:
                agent.speed = suspiciousSpeed;
                Investigate();
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

    /** Handles enemy movement when investigating either the last known spot of 
    the player, or a sound it heard. */
    private void Investigate()
    {
        if (!reachedInvestigateTarget)
        {
            agent.SetDestination(investigateTarget);
            if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                reachedInvestigateTarget = true;
                if (TryGetWanderPosition(out Vector3 newPos))
                {
                    if (agent.SetDestination(newPos))
                    {
                        reachedWanderTarget = false;
                    }
                }
            }
        }
        else
        {
            if (reachedWanderTarget && Time.time >= nextWanderTime)
            {
                if (TryGetWanderPosition(out Vector3 newPos))
                {
                    if (agent.SetDestination(newPos))
                    {
                        reachedWanderTarget = false;
                    }
                }
            }
            else if (!reachedWanderTarget)
            {
                if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
                {
                    reachedWanderTarget = true;
                    searchlight.UpdateInvestigateCounter();
                    nextWanderTime = Time.time + waitTime;
                }
            }
        }
    }

    /** Used to randomly generate a valid position to wander to within a radius. */
    private bool TryGetWanderPosition(out Vector3 position)
    {
        Vector3 offset = Random.insideUnitSphere * wanderRadius;
        offset.y = 0f;

        Vector3 candidate = investigateTarget + offset;

        if (UnityEngine.AI.NavMesh.SamplePosition(
            candidate, out UnityEngine.AI.NavMeshHit hit, wanderRadius, agent.areaMask))
        {
            position = hit.position;
            return true;
        }

        position = default;
        return false;
    }
    
    // PUBLIC METHODS
    /** Called by EnemySearchlight whenever the mutant enters investigation state;
    necessary because some variables here need to be updated */
    public void RegisterDetectionEvent(Vector3 pos)
    {
        investigateTarget = pos;
        searchlight.setInvestigate();
        reachedInvestigateTarget = false;
        reachedWanderTarget = false;
        nextWanderTime = 0f;
    }

   
}