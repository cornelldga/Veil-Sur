using UnityEngine;
using UnityEngine.Serialization;
/// <summary>
/// This class handles enemy movement.
/// </summary>
public class EnemyMover : MonoBehaviour
{
    [Header("Idle")]
    [SerializeField] protected Vector3 idlePosition;

    [Header("Patrol")]
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolSpeed = 2f;
    [SerializeField] protected float waypointTolerance = 0.3f;

    [Header("Suspicious")]
    [SerializeField] protected float suspiciousSpeed = 3f;

    [Header("Alert")]
    [SerializeField] protected float chaseSpeed = 5f;
    [SerializeField] protected float chaseAcceleration = 40f;
    protected float normalAcceleration;
    protected Transform player;


    [Header("Notice")]
    [Tooltip("Seconds the mutant stands still on first becoming suspicious. Match the notice animation length.")]
    [FormerlySerializedAs("alertPauseDuration")]
    [SerializeField] protected float noticePauseDuration = 0f;
    [Tooltip("Degrees per second the mutant turns toward the player before the notice animation plays.")]
    [SerializeField] protected float noticeTurnSpeed = 360f;
    protected EnemySearchlight.AlertState previousState;
    protected float noticePauseUntil;
    protected bool isTurningToNotice;

    // Half of one billboard slice (360 / 8 / 2), so the camera sees the front-facing clip.
    private const float FacingToleranceDegrees = 22.5f;

    public bool IsNoticePausing => Time.time < noticePauseUntil;

    [Header("Investigate")]
    /** How long the mutant waits at a wander point before choosing a new one */
    [SerializeField] protected float waitTime = 2f;
    /** Area around investigation target that the mutant can check */
    [SerializeField] protected float wanderRadius = 5f;

    [Header("Jumpscare")]
    [SerializeField] private Sprite[] jumpscareFrames;
    [SerializeField] private float jumpscareFps = 8f;
    [SerializeField] private AudioClip jumpscareSound;

    protected UnityEngine.AI.NavMeshAgent agent;
    protected EnemySearchlight searchlight;
    protected int currentPatrolIndex;
    protected Vector3 investigateTarget;
    protected bool reachedDestination;
    protected bool reachedWanderTarget;
    protected float nextWanderTime = 0f;

    protected bool isCollidingPlayer = false;

    protected void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        searchlight = GetComponent<EnemySearchlight>();
        agent.updateRotation = false;
        normalAcceleration = agent.acceleration;
    }

    protected virtual void Start()
    {
        player = GameManager.PlayerInstance.transform;

    }

    protected virtual void Update()
    {
        // Kill player if they're colliding with mutant after stun expires
        if (isCollidingPlayer && 
        searchlight.CurrentState != EnemySearchlight.AlertState.Stun)
        {
            GameManager.Instance.LoseGame(jumpscareFrames, jumpscareFps, jumpscareSound, gameObject);
        }

        EnemySearchlight.AlertState state = searchlight.CurrentState;
        // LookAround counts as patrolling here 
        bool justNoticed = state == EnemySearchlight.AlertState.Suspicious &&
            (previousState == EnemySearchlight.AlertState.Patrol || previousState == EnemySearchlight.AlertState.LookAround
            || previousState == EnemySearchlight.AlertState.Idle);
        if (justNoticed && noticePauseDuration > 0f)
        {
            isTurningToNotice = true;
            agent.velocity = Vector3.zero;
        }
        previousState = state;

        if (state != EnemySearchlight.AlertState.Suspicious) isTurningToNotice = false;
        if (isTurningToNotice && TurnTowards(searchlight.LastKnownPlayerPosition))
        {
            isTurningToNotice = false;
            noticePauseUntil = Time.time + noticePauseDuration;
        }
        agent.isStopped = isTurningToNotice || IsNoticePausing;

        bool chasing = searchlight.CurrentState == EnemySearchlight.AlertState.Alert;
        agent.acceleration = chasing ? chaseAcceleration : normalAcceleration;
        agent.autoBraking = !chasing;
        
        switch (searchlight.CurrentState)
        {
            case EnemySearchlight.AlertState.Idle:
                agent.SetDestination(idlePosition);
                if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
                {
                    agent.speed = 0;
                }
                else
                {
                    agent.speed = patrolSpeed;
                }
                break;
            case EnemySearchlight.AlertState.Patrol:
                agent.speed = patrolSpeed;
                Patrol();
                break;
            case EnemySearchlight.AlertState.Suspicious:
                agent.speed = suspiciousSpeed;
                agent.SetDestination(searchlight.LastKnownPlayerPosition);
                break;
            case EnemySearchlight.AlertState.Alert:
                agent.speed = chaseSpeed;
                agent.SetDestination(searchlight.LastKnownPlayerPosition);
                break;
            case EnemySearchlight.AlertState.LookAround:
                agent.speed = suspiciousSpeed;
                LookAround();
                break;
            case EnemySearchlight.AlertState.Stun:
                agent.speed = 0;
                break;
        }

        reachedDestination = !agent.pathPending && agent.remainingDistance <= waypointTolerance;
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

    /// <summary>
    /// Rotates toward a point at noticeTurnSpeed. Returns true once facing it.
    /// </summary>
    /// <param name="target">world position to face</param>
    protected bool TurnTowards(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return true;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, noticeTurnSpeed * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, targetRotation) <= FacingToleranceDegrees;
    }

    public void StartLookAround()
    {
        // Debug.Log("Beginning to look around");
        investigateTarget = searchlight.LastKnownPlayerPosition;
        searchlight.ResetLookAroundCounter();
        reachedWanderTarget = true;
        nextWanderTime = 0f;
    }

    public bool ReachedDestination()
    {
        return reachedDestination;
    }

    /// <summary>
    /// Calls GameManager.LoseGame when the player enters the mutant's trigger.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCollidingPlayer = true;
            if (searchlight.CurrentState != EnemySearchlight.AlertState.Stun)
            {
                GameManager.Instance.LoseGame(jumpscareFrames, jumpscareFps, jumpscareSound, gameObject);
            }
        }
    }

    /// <summary>
    /// Sets isCollidingPlayer flag to false
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           isCollidingPlayer = false;
        }
    }

    /// <summary>
    /// Returns the speed of the mutant.
    /// </summary>
    /// <returns>Mutant speed</returns>
    public float getSpeed()
    {
        return agent.speed;
    }

   
}