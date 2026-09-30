using UnityEngine;
/// <summary>
/// This class handles state changes for enemy AI.
/// </summary>
/// 
public enum Priority {Sight = 0, Sound = 1, Smell = 2, None = 999}
public class EnemySearchlight : MonoBehaviour
{
    public enum AlertState { Patrol, Suspicious, Alert, LookAround }

    [Header("Target")]
    [SerializeField] protected Transform player;
    [SerializeField] protected LayerMask obstacleMask;
    [SerializeField] protected LayerMask playerMask;

    [Header("Vision Cone")]
    [SerializeField] protected float viewDistance = 10f; 
    [SerializeField] protected float viewAngle = 60f;      // vision detection cone angle
    [SerializeField] protected float eyeHeight;     // raycast origin offset up from pivot

    [Header("Sweep (Patrol)")]
    [SerializeField] protected float sweepAngle = 45f; 
    [SerializeField] protected float sweepSpeed = 30f;

    [Header("Detection Timing")]
    [SerializeField] protected float timeToSuspicious = 0.3f;
    [SerializeField] protected float timeToAlert = 0.6f;   
    [SerializeField] protected float suspicionDecayRate = 1f; 
    [SerializeField] protected float maxDetectionMultiplier = 3f; // How quickly detection meter increases close-up
    [SerializeField] protected float loseAlertAfter = 3f; 
    /** Number of times a mutant looks around after losing LOS during chase or hearing a sound */
    [SerializeField] protected int investigateCount = 3; 

    [Header("Visuals")]
    [SerializeField] protected Light spotLight;
    protected Color patrolColor = Color.green;
    protected Color suspiciousColor = new Color(1f, 0.85f, 0f);
    protected Color alertColor = Color.red;

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
    public bool canSeePlayer = false;

    [Header("protected States")]
    protected float baseFacingAngle;
    protected float sweepTimer;
    protected float detectionMeter; // Transitions the mutant's state from Patrol -> Suspicious -> Alert
    protected float timesLookedAround; // How many times the mutant already looked around in investigate state.
    protected float lastSeenTimer; // Used to determine
    protected Priority currentPriority = Priority.None; // The priority of the suspicious event the mutant is currently investigating
    // While the mutant is investigating, it will only switch to investigating another event if it has higher or equal priority.

    protected Vector3 lastPosition; // This mutant's position last frame. Used to determine where to face while moving
    
    protected float distanceToPlayer; // How far the mutant is from the player if within LOS.
    // Used to determine how quickly the suspicion meter is raised (smaller distance = faster suspicion)

    protected void Start()
    {
        baseFacingAngle = transform.eulerAngles.y;
        lastPosition = transform.position;

        if (showConeMesh && coneMeshFilter != null)
        {
            coneMesh = new Mesh { name = "VisionCone" };
            coneMeshFilter.mesh = coneMesh;
        }
    }

    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Game over [CHANGE THIS WHEN GAMEMANAGER IS IMPLEMENTED]!");
            // GameManager.Instance.Lose();
        }
    }

    protected void Update()
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
    
    /// <summary>
    /// Faces mutant's search cone is the direction of its movement while the mutant is patrolling.
    /// </summary>
    protected void FaceTowardsMovement()
    {
        // Calculate the movement direction direction vector
        Vector3 direction = transform.position - lastPosition;

        // Flatten the Y-axis if you don't want the object tilting up/down on slopes
        // direction.y = 0; 

        if (direction.sqrMagnitude > 0.000001f)
        {
            // Smoothly rotate towards the target direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // transform.rotation = targetRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * sweepSpeed);
        }

        // Store the position for the next frame
        lastPosition = transform.position;
    }

    /// <summary>
    /// Updates whether the mutant has LOS to the player and how far away the player is.
    /// </summary>
    protected bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eye = EyePosition;
        Vector3 toPlayer = player.position - eye;
        float distance = toPlayer.magnitude;
        distanceToPlayer = distance;
        if (distance > viewDistance)
        {
            return false;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
        if (angleToPlayer > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, distance, obstacleMask | playerMask))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                LastKnownPlayerPosition = player.position;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Updates the mutant's current state.
    /// </summary>
    /// <param name="canSeePlayer">Whether the mutant sees the player in its vision cone or not.</param>
    protected virtual void UpdateAlertState(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            lastSeenTimer = 0f;
            // Detect player faster at closer distances
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

        switch (CurrentState)
        {
            case AlertState.Patrol:
                if (detectionMeter >= timeToSuspicious) SetState(AlertState.Suspicious);
                break;

            case AlertState.Suspicious:
                if (detectionMeter >= timeToAlert) SetState(AlertState.Alert);
                else if (GetComponent<EnemyMover>().ReachedSuspicionTarget() && detectionMeter <= 0f) {
                    SetState(AlertState.LookAround);
                    GetComponent<EnemyMover>().StartLookAround();
                    ResetSuspicion();
                }
                break;

            case AlertState.Alert:
                if (!canSeePlayer && lastSeenTimer >= loseAlertAfter)
                {
                    // GetComponent<EnemyMover>()
                    //     .RegisterDetectionEvent(LastKnownPlayerPosition);
                    SetState(AlertState.Suspicious);
                    ReportSense(LastKnownPlayerPosition, Priority.Sight);
                }
                break;
            case AlertState.LookAround:
                if (canSeePlayer && detectionMeter >= timeToAlert)
                {
                    SetState(AlertState.Alert);
                }
                else if (timesLookedAround >= investigateCount)
                {
                    SetState(AlertState.Patrol);
                    Debug.Log("Resuming patrol");
                }
                break;
        }
    }

    /// <summary>
    /// Lets another sense (scent, hearing) point the searchlight at a position.
    /// Sets state to Suspicious. Never escalates to Alert; only sight does.
    /// </summary>
    /// <param name="position">World position the player is believed to be at.</param>
    public void ReportSense(Vector3 position, Priority priority)
    {
        if (CurrentState == AlertState.Alert || priority > currentPriority) return;

        LastKnownPlayerPosition = position;
        detectionMeter = Mathf.Max(detectionMeter, timeToSuspicious);
        SetState(AlertState.Suspicious);
        currentPriority = priority;
        Debug.Log("Now investigating " + position + " at priority " + priority);
    }

    /// <summary>
    /// Sets mutant state.
    /// </summary>
    /// <param name="newState">State to set the mutant to</param>
    protected void SetState(AlertState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        // Debug.Log(newState);

        if (newState == AlertState.Patrol) sweepTimer = 0f;
        else if (newState == AlertState.LookAround) timesLookedAround = 0;
    }

    /// <summary>
    /// Sets currentPriority to Priority.None so any suspicious event will attract the attention of the mutant.
    /// </summary>
    protected void ResetSuspicion()
    {
        currentPriority = Priority.None;   
    }

    /// <summary>
    /// Updates the color of the detection cone visual.
    /// </summary>
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
        Vector3 eyeLocal = Vector3.up * eyeHeight;
        vertices[0] = eyeLocal;

        float startAngle = -viewAngle * 0.5f;
        float angleStep = viewAngle / coneRayCount;
        Vector3 eyeWorld = EyePosition;

        for (int i = 0; i <= coneRayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

            float dist = viewDistance;
            if (Physics.Raycast(eyeWorld, dir, out RaycastHit hit, viewDistance, obstacleMask))
            {
                dist = hit.distance;
            }
            vertices[i + 1] = transform.InverseTransformDirection(dir) * dist + eyeLocal;
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

    /// <summary>
    /// Determines the color of the vision cone based on how suspicious the mutant is. Credit to Chat
    /// </summary>
    protected virtual Color GetDetectionColor()
    {
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

    /// <summary>
    /// Called by EnemyMover whenever a mutant finishes reaching a patrol point during its LookAround state.
    /// Used to determine when to return the mutant to the Patrol state.
    /// </summary>
    public void UpdateLookAroundCounter() 
    {
        timesLookedAround++;
        // Debug.Log("Times wandered: " + timesLookedAround);
    }
}