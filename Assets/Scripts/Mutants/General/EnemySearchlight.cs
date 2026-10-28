using UnityEngine;
/// <summary>
/// This class handles state changes for enemy AI.
/// </summary>
/// 
public enum Priority {Sight = 0, Sound = 1, Smell = 2, None = 999}
public class EnemySearchlight : MonoBehaviour
{
    public enum AlertState { Idle, Patrol, Suspicious, Alert, LookAround, Stun }

    [Header("Target")]
    protected Transform player;
    [SerializeField] protected LayerMask obstacleMask;
    [SerializeField] protected LayerMask playerMask;

    [Header("Idle Behavior")]
    [SerializeField] protected float idleFacingYAngle = 0f;

    [Header("Vision Cone")]
    [SerializeField] protected float viewDistance = 10f;
    [SerializeField] protected float viewAngle = 60f;      // cone angle
    [SerializeField] protected float eyeHeight;     // raycast origin offset up from pivot
    [SerializeField] protected float chaseTurnSpeed = 540f;

    [Header("Sweep (Patrol)")]
    [SerializeField] protected float sweepAngle = 45f; 
    [SerializeField] protected float sweepSpeed = 30f;

    [Header("Detection Timing")]
    [SerializeField] protected float timeToSuspicious = 0.3f;
    [SerializeField] protected float timeToAlert = 0.6f;   
    [SerializeField] protected float suspicionDecayRate = 1f; 
    [SerializeField] protected float maxDetectionMultiplier = 3f; 
    [SerializeField] protected float loseAlertAfter = 3f; 
    [SerializeField] protected float chaseLostGrace = 0.75f;
    
    [SerializeField] protected float proximityDetection = 2f; // During chase, if player is within this distance, doesn't lose alert
    
    /** Number of times a mutant looks around after losing LOS during chase or hearing a sound */
    [SerializeField] protected int investigateCount = 3; 

    [Header("Stun Settings")]
    [SerializeField] protected float stunDuration = 1f; // How long stun lasts
    [SerializeField] protected float stunCooldown = 10f; // Time between stuns

    [Header("Visuals")]
    [SerializeField] protected Light spotLight;
    protected Color patrolColor = Color.green;
    protected Color suspiciousColor = new Color(1f, 0.85f, 0f);
    protected Color alertColor = Color.red;
    protected Color stunColor = Color.pink;

    [Header("Cone Mesh (visible in Game view)")]
    [SerializeField] protected bool showConeMesh = true;
    [SerializeField] protected MeshFilter coneMeshFilter;   // child object's MeshFilter
    [SerializeField] protected MeshRenderer coneMeshRenderer;
    protected int coneRayCount = 24; // resolution of the cone edge
    protected float coneAlpha = 0.35f;
    protected Mesh coneMesh;

    [Header("Public Fields")]
    public AlertState CurrentState { get; protected set; } = AlertState.Patrol;
    public Vector3 LastKnownPlayerPosition { get; protected set; }
    public bool canSeePlayer = false; // Whether the mutant can see the player without obstruction
    public float distanceToPlayer; // Used to increase mutant suspicion gain based on proximity


    [Header("Protected States")]
    protected float baseFacingAngle;
    protected float sweepTimer;
    protected float detectionMeter;
    protected float timesLookedAround; // How many times the mutant already looked around in investigate state.
    protected float lastSeenTimer;
    protected float timeSinceLastStun; // How long since the mutant was last stunned
    protected bool hasKnownPlayerPosition = false;
    protected Priority currentPriority = Priority.None;

    protected Vector3 lastPosition; // Used to determine where to face while moving


    protected virtual void Start()
    {
        player = GameManager.PlayerInstance.transform;
        baseFacingAngle = transform.eulerAngles.y;
        lastPosition = transform.position;
        timeSinceLastStun = stunCooldown + stunDuration;

        if (showConeMesh && coneMeshFilter != null)
        {
            coneMesh = new Mesh { name = "VisionCone" };
            coneMeshFilter.mesh = coneMesh;
        }
    }

    protected virtual void Update()
    {
        // SweepSearchlight();
        FaceTowardsMovement();
        canSeePlayer = CanSeePlayer();
        UpdateAlertState(canSeePlayer);
        UpdateVisual();
        DrawConeMesh();
    }

    protected Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    protected void SweepSearchlight() // Currently not used
    {
        if (CurrentState == AlertState.Alert)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, sweepSpeed * 3f * Time.deltaTime);
            }
            return;
        }

        sweepTimer += Time.deltaTime * sweepSpeed;
        float offset = Mathf.PingPong(sweepTimer, sweepAngle * 2f) - sweepAngle;
        Quaternion sweepTargetRot = Quaternion.Euler(0, baseFacingAngle + offset, 0);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, sweepTargetRot, sweepSpeed * 2f * Time.deltaTime);
    }

    /** When patrolling, search cone is in direction of mutant movement. */
    protected void FaceTowardsMovement()
    {
        Vector3 movement = transform.position - lastPosition;
        lastPosition = transform.position;

        if (CurrentState == AlertState.Stun)
            return;

        Vector3 direction =
            CurrentState == AlertState.Alert
                ? LastKnownPlayerPosition - transform.position
                : movement;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            if (CurrentState == AlertState.Alert)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    chaseTurnSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    sweepSpeed * Time.deltaTime);
            }
        }
        else if (CurrentState == AlertState.Idle)
        {
            Quaternion targetRotation =
                Quaternion.Euler(0f, idleFacingYAngle, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                sweepSpeed * Time.deltaTime);
        }
    }

    protected bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eye = EyePosition;
        Vector3 toPlayer = player.position - eye;
        distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer > viewDistance || CurrentState == AlertState.Stun)
        {
            return false;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
        if (angleToPlayer > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(
            eye,
            toPlayer.normalized,
            out RaycastHit hit,
            distanceToPlayer,
            obstacleMask | playerMask,
            QueryTriggerInteraction.Ignore))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                LastKnownPlayerPosition = player.position;
                hasKnownPlayerPosition = true;
                return true;
            }
        }
        
        return false;
    }

    protected virtual void UpdateAlertState(bool canSeePlayer)
    {
        // Debug.Log("Last seen timer: " +lastSeenTimer);
        // bool proximityKeepsAlert = CurrentState == AlertState.Alert && IsProximityDetected();

        bool closeRangeTracking =
            CurrentState == AlertState.Alert && IsProximityDetected();
        if (canSeePlayer || closeRangeTracking)
        {
            lastSeenTimer = 0f;
            LastKnownPlayerPosition = player.position;

            float proximity = Mathf.Clamp01(1f - distanceToPlayer / viewDistance);
            float multiplier = 1f + proximity * (maxDetectionMultiplier - 1f);

            detectionMeter += Time.deltaTime * multiplier;
        }
        else
        {
            lastSeenTimer += Time.deltaTime;
            detectionMeter -= suspicionDecayRate * Time.deltaTime;
        }

        detectionMeter = Mathf.Clamp(detectionMeter, 0f, timeToAlert);

        // Update stun timer
        timeSinceLastStun += Time.deltaTime;

        switch (CurrentState)
        {
            case AlertState.Idle :
            case AlertState.Patrol :
                if (detectionMeter >= timeToSuspicious) SetState(AlertState.Suspicious);
                break;

            case AlertState.Suspicious:
                if (detectionMeter >= timeToAlert) SetState(AlertState.Alert);
                else if (GetComponent<EnemyMover>().ReachedDestination() && detectionMeter <= 0f) {
                    SetState(AlertState.LookAround);
                    GetComponent<EnemyMover>().StartLookAround();
                    ResetSuspicion();
                }
                break;
            case AlertState.Alert:
            {
                bool lostTooLong = lastSeenTimer >= loseAlertAfter;

                bool reachedLastSeenPosition =
                    lastSeenTimer >= chaseLostGrace &&
                    GetComponent<EnemyMover>().ReachedDestination();

                if (lostTooLong || reachedLastSeenPosition)
                {
                    SetState(AlertState.Suspicious);
                    ReportSense(LastKnownPlayerPosition, Priority.Sight, "Player");
                }
                break;
            }
            case AlertState.LookAround:
                if (canSeePlayer && detectionMeter >= timeToAlert)
                {
                    SetState(AlertState.Alert);
                }
                else if (timesLookedAround >= investigateCount)
                {
                    SetState(AlertState.Patrol);
                    // Debug.Log("Resuming patrol");
                }
                break;
            case AlertState.Stun:
                if (timeSinceLastStun >= stunDuration)
                {
                    if (hasKnownPlayerPosition)
                    {
                        SetState(AlertState.Suspicious);
                        ReportSense(LastKnownPlayerPosition, Priority.Sight, "Player");
                    }
                    else
                    {
                        SetState(AlertState.Patrol);
                    }
                    
                }
                break;
        }
    }

    /// <summary>
    /// Lets another sense (scent, hearing) point the searchlight at a position.
    /// Raises Patrol to Suspicious and pins the meter there. Never escalates to Alert; only sight does.
    /// </summary>
    /// <param name="position">World position the player is believed to be at.</param>
    /// <param name="priority">Type of sense detected.</param>
    /// <param name="position">Name of the source. Used only in special cases.</param>
    public virtual void ReportSense(Vector3 position, Priority priority, string source)
    {
        if (CurrentState == AlertState.Alert ||
        CurrentState == AlertState.Stun ||
        priority > currentPriority ||
        (canSeePlayer && priority != Priority.Sight))
        {
            return;
        }

        LastKnownPlayerPosition = position;
        hasKnownPlayerPosition = true;
        detectionMeter = Mathf.Max(detectionMeter, timeToSuspicious);
        SetState(AlertState.Suspicious);
        currentPriority = priority;
        // Debug.Log("Now investigating " + position + " at priority " + priority);
    }

    protected void SetState(AlertState newState)
    {
        if (CurrentState == newState) return;
        AlertState previousState = CurrentState;
        CurrentState = newState;
        if (newState == AlertState.Patrol) sweepTimer = 0f;
        else if (newState == AlertState.Suspicious && previousState != AlertState.LookAround) timesLookedAround = 0;
    }

    /// <summary>
    /// Sets currentPriority to Priority.None so any suspicious event will attract the attention of the mutant.
    /// </summary>
    protected void ResetSuspicion()
    {
        currentPriority = Priority.None;   
    }

    protected void UpdateVisual()
    {
        if (spotLight == null) return;

        spotLight.color = GetDetectionColor();
        spotLight.spotAngle = viewAngle;
        spotLight.range = viewDistance;
    }

    protected void DrawConeMesh()
    {
        if (!showConeMesh || coneMesh == null) return;

        Vector3[] vertices = new Vector3[coneRayCount + 2];
        int[] triangles = new int[coneRayCount * 3];
        // Vector3 eyeLocal = Vector3.up * eyeHeight;
        // vertices[0] = eyeLocal;
        Vector3 eyeWorld = EyePosition;
        vertices[0] = coneMeshFilter.transform.InverseTransformPoint(eyeWorld);

        float startAngle = -viewAngle * 0.5f;
        float angleStep = viewAngle / coneRayCount;

        for (int i = 0; i <= coneRayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            float dist = viewDistance;
            if (Physics.Raycast(eyeWorld, dir, out RaycastHit hit, viewDistance, obstacleMask))
            {
                dist = hit.distance;
            }
            Vector3 worldPoint = eyeWorld + dir * dist;
            vertices[i + 1] =
                coneMeshFilter.transform.InverseTransformPoint(worldPoint);        
        }

        for (int i = 0; i < coneRayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();

        if (coneMeshRenderer != null)
        {
            Color c = GetDetectionColor();
            c.a = coneAlpha;
            coneMeshRenderer.material.color = c;
        }
    }

    // Chat helped make the cone color stuff prettier
    protected virtual Color GetDetectionColor()
    {
        if (CurrentState == AlertState.Alert)
        {
            return alertColor;
        }
        if (CurrentState == AlertState.Stun)
        {
            return stunColor;
        }

        if (detectionMeter <= timeToSuspicious)
        {
            float t = Mathf.InverseLerp(
                0f, timeToSuspicious, detectionMeter);

            return Color.Lerp(patrolColor, suspiciousColor, t);
        }

        float alertProgress = Mathf.InverseLerp(
            timeToSuspicious, timeToAlert, detectionMeter);

        return Color.Lerp(suspiciousColor, alertColor, alertProgress);
    }

    // PUBLIC METHODS
    /** Called by EnemyMover to change state based on enemy position; 
    possibly consider combining the two files atp? */
    // public void setInvestigate() 
    // {
    //     if (CurrentState == AlertState.Alert && canSeePlayer)
    //     {
    //         return;
    //     }
    //     SetState(AlertState.Investigate);
    //     timesLookedAround = 0;
        
    // }   

     /** Called by EnemyMover whenever a mutant reaches its wander target and gets
     a new one */
    public void UpdateLookAroundCounter() 
    {
        timesLookedAround++;
        // Debug.Log("Times wandered: " + timesLookedAround);
    }

    /// <summary>
    /// Returns whether the player is within proximityDetection distance from this mutant.
    /// Suspicion doesn't decay if the mutant is alerted and the player is too close.
    /// </summary>
    /// <returns>Whether the player is within proximityDetection distance from this mutant.</returns>
    public bool IsProximityDetected()
    {
        if (player == null || CurrentState == AlertState.Stun)
            return false;

        Vector3 toPlayer = player.position - EyePosition;
        float distance = toPlayer.magnitude;

        if (distance > proximityDetection)
            return false;

        // No angle check: detect nearby players in any direction,
        // provided there is no wall between us.
        return !Physics.Raycast(
            EyePosition,
            toPlayer.normalized,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }
    /// <summary>
    /// Called on camera flash to stun the mutant during chase. 
    /// </summary>
    public void Stun()
    {
        if ((CurrentState == AlertState.Alert || CurrentState == AlertState.Suspicious)
        && timeSinceLastStun >= stunCooldown + stunDuration)
        {
            // Debug.Log("Successful stun!");
            timeSinceLastStun = 0;
            SetState(AlertState.Stun);
            ResetSuspicion();
        }
    }

    /// <summary>
    /// Called to reset the look around counter when investigating a new event.
    /// </summary>
    public void ResetLookAroundCounter()
    {
        timesLookedAround = 0;
    }
}