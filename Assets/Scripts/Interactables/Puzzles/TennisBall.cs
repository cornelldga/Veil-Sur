using UnityEngine;

/// <summary>
/// Physics-driven ball. After [Launch], it flies under gravity until it hits something, then makes
/// a sound and destroys itself.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class TennisBall : SoundEvent
{
    [SerializeField, Tooltip("Seconds before the ball destroys itself if nothing is hit")] float maxLifetime = 5f;

    private Rigidbody rb;
    private float ballVolume;
    private bool ended;

    public void SetVolume(float volume) { ballVolume = volume; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Invoke(nameof(EndOfPath), maxLifetime);
    }

    /// <summary>
    /// Stop the ball from colliding with [colliders] (used to not collide with launcher)
    /// </summary>
    public void IgnoreCollisions(Collider[] colliders)
    {
        Collider ballCollider = GetComponent<SphereCollider>();
        foreach (Collider col in colliders)
        {
            Physics.IgnoreCollision(ballCollider, col);
        }
    }

    /// <summary>
    /// Calculate launch velocity given [height] and [distance] parameters.
    /// </summary>
    public void Launch(Vector3 direction, float distance, float height)
    {
        direction.y = 0f;
        float gravity = -Physics.gravity.y; // hi! -jason
        float upSpeed = Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, height));
        float flightTime = 2f * upSpeed / gravity;

        rb.linearVelocity = direction.normalized * (distance / flightTime) + Vector3.up * upSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EndOfPath();
    }

    private void EndOfPath()
    {
        // Only execute once, that way the sound doesn't play for multiple collisions at the same time.
        if (ended) return;
        ended = true;

        RegisterSoundEvent(rb.position, ballVolume, "Ball");
    }
}
