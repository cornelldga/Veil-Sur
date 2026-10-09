using UnityEngine;
/// <summary>
/// This class handles enemy movement.
/// </summary>
public class EnemyMover : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolSpeed = 2f;
    [SerializeField] protected float waypointTolerance = 0.3f;

    [Header("Suspicious")]
    [SerializeField] protected float suspiciousSpeed = 3f;

    [Header("Alert")]
    [SerializeField] protected float chaseSpeed = 5f;
    protected Transform player;

    [Header("Investigate")]
    /** How long the mutant waits at a wander point before choosing a new one */
    [SerializeField] protected float waitTime = 2f;
    /** Area around investigation target that the mutant can check */
    [SerializeField] protected float wanderRadius = 5f;

    protected UnityEngine.AI.NavMeshAgent agent;
    protected EnemySearchlight searchlight;
    protected int currentPatrolIndex;
    protected Vector3 investigateTarget;
    protected bool reachedSuspicionTarget;
    protected bool reachedWanderTarget;
    protected float nextWanderTime = 0f;

    protected void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        searchlight = GetComponent<EnemySearchlight>();
        agent.updateRotation = false;
    }

    protected virtual void Start()
    {
        player = GameManager.PlayerInstance.transform;

    }

    protected virtual void Update()
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

                if (player != null && (searchlight.canSeePlayer || searchlight.IsProximityDetected()))
                    agent.SetDestination(player.position);
                else
                    agent.SetDestination(searchlight.LastKnownPlayerPosition);

                break;
            case EnemySearchlight.AlertState.LookAround:
                agent.speed = suspiciousSpeed;
                LookAround();
                break;
        }

        if (searchlight.CurrentState == EnemySearchlight.AlertState.Suspicious &&
        !agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            reachedSuspicionTarget = true;
        }
        else
        {
            reachedSuspicionTarget = false;
        }
    }

    protected void Patrol()
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
    protected void LookAround()
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
                searchlight.UpdateLookAroundCounter();
                nextWanderTime = Time.time + waitTime;
            }
        }
    }

    /** Used to randomly generate a valid position to wander to within a radius. */
    protected bool TryGetWanderPosition(out Vector3 position)
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
    // public void RegisterDetectionEvent(Vector3 pos)
    // {
    //     investigateTarget = pos;
    //     searchlight.setInvestigate();
    //     reachedInvestigateTarget = false;
    //     reachedWanderTarget = false;
    //     nextWanderTime = 0f;
    //     Debug.Log("Now investigating " + pos);
    // }

    public void StartLookAround()
    {
        Debug.Log("Beginning to look around");
        reachedWanderTarget = false;
        nextWanderTime = 0f;
    }

    public bool ReachedSuspicionTarget()
    {
        return reachedSuspicionTarget;
    }

    /// <summary>
    /// Calls GameManager.LoseGame when the player enters the mutant's trigger.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.LoseGame();
        }
    }

   
}