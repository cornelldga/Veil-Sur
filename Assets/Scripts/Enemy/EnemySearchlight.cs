using UnityEngine;

public class EnemySearchlight : MonoBehaviour
{
    public enum AlertState { Patrol, Suspicious, Alert }

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

    [Header("Visuals")]
    [SerializeField] private Light spotLight;
    private Color patrolColor = Color.white;
    private Color suspiciousColor = new Color(1f, 0.85f, 0f);
    private Color alertColor = Color.red;

    [Header("Cone Mesh (visible in Game view)")]
    [SerializeField] private bool showConeMesh = true;
    [SerializeField] private MeshFilter coneMeshFilter;   // child object's MeshFilter
    [SerializeField] private MeshRenderer coneMeshRenderer;
    private int coneRayCount = 24; // resolution of the cone edge
    private float coneAlpha = 0.35f;

    private Mesh coneMesh;

    public AlertState CurrentState { get; private set; } = AlertState.Patrol;
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private float baseFacingAngle;
    private float sweepTimer;
    private float detectionMeter;
    private float lastSeenTimer;

    private void Start()
    {
        baseFacingAngle = transform.eulerAngles.y;

        if (showConeMesh && coneMeshFilter != null)
        {
            coneMesh = new Mesh { name = "VisionCone" };
            coneMeshFilter.mesh = coneMesh;
        }
    }

    private void Update()
    {
        SweepSearchlight();
        bool canSeePlayer = CanSeePlayer();
        UpdateAlertState(canSeePlayer);
        UpdateVisual();
        DrawConeMesh();
    }

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    private void SweepSearchlight()
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

    //FINITE STATE MACHINE YAH
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
                    SetState(AlertState.Suspicious);
                break;
        }
    }

    private void SetState(AlertState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        if (newState == AlertState.Patrol) sweepTimer = 0f;
    }

    private void UpdateVisual()
    {
        if (spotLight == null) return;
        Color target = CurrentState switch
        {
            AlertState.Suspicious => suspiciousColor,
            AlertState.Alert => alertColor,
            _ => patrolColor
        };
        spotLight.color = Color.Lerp(spotLight.color, target, Time.deltaTime * 5f);
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
            Color c = CurrentState switch
            {
                AlertState.Suspicious => suspiciousColor,
                AlertState.Alert => alertColor,
                _ => patrolColor
            };
            c.a = coneAlpha;
            coneMeshRenderer.material.color = c;
        }
    }
}