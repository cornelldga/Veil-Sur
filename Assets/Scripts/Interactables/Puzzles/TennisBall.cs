using UnityEngine;

/// <summary>
/// Travels along a parabolic arc from [startPosition], then destroys itself.
/// The arc travels with [height] [distance] and [speed], which can be set through scripting.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class TennisBall : SoundEvent
{
    [SerializeField, Tooltip("Height to travel along arc")] float height = 2f;
    [SerializeField, Tooltip("Distance to travel along arc")] float distance = 6f;
    [SerializeField, Tooltip("Units per second along the arc")] float speed = 8f;
    [SerializeField] LayerMask collisionMask = ~0;

    private float ballVolume;
    private float radius;
    private Vector3 startPosition;
    private Vector3 direction;
    // Arc progress, 0 at start and 1 at [distance]
    private float t;

    public void SetHeight(float h) { height = h; }
    public void SetDistance(float d) { distance = Mathf.Max(0.01f, d); }
    public void SetSpeed(float s) { speed = s; }
    public void SetVolume(float volume) { ballVolume = volume; }
    public void SetDirection(Vector3 dir) { dir.y = 0f; direction = dir.normalized; }

    private void Awake()
    {
        // Set radius based on collider
        Vector3 scale = transform.lossyScale;
        radius = GetComponent<SphereCollider>().radius *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private void Start()
    {
        startPosition = transform.position;
        if (direction == Vector3.zero) SetDirection(transform.forward);
        if (direction == Vector3.zero) direction = Vector3.forward;
    }

    private void Update()
    {
        /* Animate parabolic movement: advance t so the ball moves [speed]
           units/second along the arc itself (math done with Claude) */
        float horizontal = Mathf.Max(0.01f, distance);
        float vertical = 4f * height * (1f - 2f * t);
        float arcSpeed = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
        t += speed * Time.deltaTime / arcSpeed;

        // End path when hitting an object
        Vector3 next = PositionAt(t);
        Vector3 delta = next - transform.position;
        // Only check collisions if moving and if not at start of path (avoid collisions with launcher)
        if (delta.sqrMagnitude > 0f && t >= 0.15f &&
            Physics.SphereCast(transform.position, radius, delta.normalized, out RaycastHit hit,
                               delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + hit.normal * radius;
            EndOfPath();
            return;
        }
        // Fallback if ball left bounds of level
        else if (t >= 2.0f)
        {
            EndOfPath();
        }

        // Update position
        transform.position = next;
    }

    /// <summary>
    /// Parabola: 0 at t=0 and t=1, [height] at t=0.5
    /// </summary>
    private Vector3 PositionAt(float t)
    {
        float arc = 4f * height * t * (1f - t);
        return startPosition + direction * (t * distance) + Vector3.up * arc;
    }

    private void EndOfPath()
    {
        RegisterSoundEvent(transform.position, ballVolume);
        Destroy(gameObject);
    }
}
