using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dog mutant scent trail. Samples the player's position on a timer, and when the dog
/// walks over a fresh scent point it follows the trail point by point by feeding
/// positions into EnemySearchlight.ReportSense. Sight always takes priority.
/// </summary>
[RequireComponent(typeof(EnemySearchlight))]
public class DogSniff : MonoBehaviour
{
    private struct ScentPoint
    {
        public Vector3 position;
        public float time;
    }

    [Tooltip("Player transform the scent trail is sampled from")]
    [SerializeField] private Transform player;
    [Tooltip("Seconds between scent samples")]
    [SerializeField] private float sampleInterval = 0.5f;
    [Tooltip("Seconds before a scent point fades")]
    [SerializeField] private float scentLifetime = 10f;
    [Tooltip("How close the dog must be to a scent point to smell it or count it as reached")]
    [SerializeField] private float sniffRadius = 1.5f;
    [Tooltip("Seconds on a trail without seeing the player before the dog gives up")]
    [SerializeField] private float giveUpAfter = 4f;

    private EnemySearchlight searchlight;
    private readonly List<ScentPoint> trail = new List<ScentPoint>();
    private int trailIndex = -1;
    private float sampleTimer;
    private float followTimer;
    private float ignoreScentBefore = -1f;

    private void Start()
    {
        searchlight = GetComponent<EnemySearchlight>();
    }

    private void Update()
    {
        SampleScent();
        ExpireScent();

        if (searchlight.CurrentState == EnemySearchlight.AlertState.Alert)
        {
            trailIndex = -1;
            return;
        }

        if (trailIndex < 0)
        {
            trailIndex = NewestPointInRange();
            followTimer = 0f;
        }

        if (trailIndex >= 0)
        {
            FollowTrail();
        }
    }

    /// <summary>
    /// Records the player's position every sampleInterval seconds.
    /// </summary>
    private void SampleScent()
    {
        if (player == null) return;

        sampleTimer += Time.deltaTime;
        if (sampleTimer < sampleInterval) return;

        sampleTimer = 0f;
        trail.Add(new ScentPoint { position = player.position, time = Time.time });
    }

    /// <summary>
    /// Drops points older than scentLifetime from the front of the trail.
    /// </summary>
    private void ExpireScent()
    {
        while (trail.Count > 0 && Time.time - trail[0].time > scentLifetime)
        {
            trail.RemoveAt(0);
            if (trailIndex >= 0) trailIndex--;
        }
    }

    /// <summary>
    /// Returns the index of the newest scent point within sniffRadius, or -1 if none.
    /// </summary>
    private int NewestPointInRange()
    {
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            if (trail[i].time <= ignoreScentBefore) break;
            if (IsWithinSniffRadius(trail[i].position)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Reports the current trail point to the searchlight, advancing to the next
    /// point once reached. Ends the trail after the newest point or after
    /// giveUpAfter seconds without a sighting.
    /// </summary>
    private void FollowTrail()
    {
        followTimer = searchlight.canSeePlayer ? 0f : followTimer + Time.deltaTime;
        if (followTimer >= giveUpAfter)
        {
            trailIndex = -1;
            ignoreScentBefore = Time.time;
            return;
        }

        if (IsWithinSniffRadius(trail[trailIndex].position))
        {
            trailIndex++;
            if (trailIndex >= trail.Count)
            {
                trailIndex = -1;
                return;
            }
        }

        searchlight.ReportSense(trail[trailIndex].position, Priority.Smell);
        Debug.Log("Sniffing " + trail[trailIndex].position);
        
    }

    /// <summary>
    /// Returns whether a point is within sniffRadius of the dog, ignoring height
    /// so the player's capsule center and the dog's pivot don't skew the check.
    /// </summary>
    private bool IsWithinSniffRadius(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= sniffRadius * sniffRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        foreach (ScentPoint point in trail)
        {
            Gizmos.DrawSphere(point.position, 0.15f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sniffRadius);
    }
}
