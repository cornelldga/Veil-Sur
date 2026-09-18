using UnityEngine;
/// <summary>
/// This class handles state changes for enemy AI.
/// </summary>
public class EnemySearchlight : MonoBehaviour
{
    public enum AlertState { Patrol, Suspicious, Alert, Investigate }

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Vision Cone")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 60f;      // cone angle
    [SerializeField] private float eyeHeight;     // raycast origin offset up from pivot

    [Header("Sweep (Patrol)")]
    [SerializeField] private float sweepAngle = 45f; 
    [SerializeField] private float sweepSpeed = 30f;

    [Header("Detection Timing")]
    [SerializeField] private float timeToSuspicious = 0.3f;
    [SerializeField] private float timeToAlert = 0.6f;   
    [SerializeField] private float suspicionDecayRate = 1f; 
    [SerializeField] private float loseAlertAfter = 3f; 
    /** Number of times a mutant looks around after losing LOS during chase or hearing a sound */
    [SerializeField] private int investigateCount = 3; 



    [Header("Visuals")]
    [SerializeField] private Light spotLight;
    private Color patrolColor = Color.green;
    private Color suspiciousColor = new Color(1f, 0.85f, 0f);
    private Color alertColor = Color.red;

    [Header("Cone Mesh (visible in Game view)")]
    [SerializeField] private bool showConeMesh = true;
    [SerializeField] private MeshFilter coneMeshFilter;   // child object's MeshFilter
    [SerializeField] private MeshRenderer coneMeshRenderer;
    private int coneRayCount = 24; // resolution of the cone edge
    private float coneAlpha = 0.35f;

    private Mesh coneMesh;

    [Header("Public Fields")]
    public AlertState CurrentState { get; private set; } = AlertState.Patrol;
    public Vector3 LastKnownPlayerPosition { get; private set; }
    public bool canSeePlayer = false;

    private float baseFacingAngle;
    private float sweepTimer;
    private float detectionMeter;
    private float timesInvestigated; // How many times the mutant already looked around in investigate state.
    private float lastSeenTimer;

    private Vector3 lastPosition; // Used to determine where to face while moving

    private void Start()
    {
        baseFacingAngle = transform.eulerAngles.y;
        lastPosition = transform.position;

        if (showConeMesh && coneMeshFilter != null)
        {
            coneMesh = new Mesh { name = "VisionCone" };
            coneMeshFilter.mesh = coneMesh;
        }
    }

    private void Update()
    {
        // SweepSearchlight();
        FaceTowardsMovement();
        canSeePlayer = CanSeePlayer();
        UpdateAlertState(canSeePlayer);
        UpdateVisual();
        DrawConeMesh();
    }

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    private void SweepSearchlight() // Currently not used
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
    private void FaceTowardsMovement()
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

    private bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eye = EyePosition;
        Vector3 toPlayer = player.position - eye;
        float distance = toPlayer.magnitude;
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

    private void UpdateAlertState(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            lastSeenTimer = 0f;
            detectionMeter += Time.deltaTime;
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
                else if (detectionMeter <= 0f) SetState(AlertState.Patrol);
                break;

            case AlertState.Alert:
                if (!canSeePlayer && lastSeenTimer >= loseAlertAfter)
                {
                    GetComponent<EnemyMover>()
                        .RegisterDetectionEvent(LastKnownPlayerPosition);
                }
                break;
            case AlertState.Investigate:
                if (canSeePlayer && detectionMeter >= timeToAlert)
                {
                    SetState(AlertState.Alert);
                }
                else if (!canSeePlayer &&
                        timesInvestigated >= investigateCount &&
                        detectionMeter <= 0f)
                {
                    SetState(AlertState.Patrol);
                }
                break;
        }
    }

    private void SetState(AlertState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        Debug.Log(newState);

        if (newState == AlertState.Patrol) sweepTimer = 0f;
    }

    private void UpdateVisual()
    {
        if (spotLight == null) return;

        spotLight.color = GetDetectionColor();
        spotLight.spotAngle = viewAngle;
        spotLight.range = viewDistance;
    }

    private void DrawConeMesh()
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

    // Chat helped make the cone color stuff prettier
    private Color GetDetectionColor()
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
    public void setInvestigate() 
    {
        if (CurrentState == AlertState.Alert && canSeePlayer)
        {
            return;
        }
        SetState(AlertState.Investigate);
        timesInvestigated = 0;
        
    }   

     /** Called by EnemyMover whenever a mutant reaches its wander target and gets
     a new one */
    public void UpdateInvestigateCounter() 
    {
        timesInvestigated++;
        Debug.Log("Times wandered: " + timesInvestigated);
    }
}