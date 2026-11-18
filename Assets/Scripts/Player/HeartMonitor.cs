using UnityEngine;

/// <summary>
/// Beeps faster as the nearest HeartbeatSource mutant gets closer.
/// Walls between the mutant and the player silence it.
/// </summary>
public class HeartMonitor : MonoBehaviour
{
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private float range = 12f;
    [SerializeField] private float slowestInterval = 1.2f;
    [SerializeField] private float fastestInterval = 0.2f;
    [SerializeField, Range(0f, 1f)] private float quietestVolume = 0.15f;
    [SerializeField, Range(0f, 1f)] private float loudestVolume = 1f;

    private float timeSinceBeep;
    private ProximityVignette vignette;

    private void Start()
    {
        vignette = FindFirstObjectByType<ProximityVignette>();
    }

    private void Update()
    {
        float nearest = NearestAudibleDistance();
        if (nearest > range)
        {
            timeSinceBeep = slowestInterval;
            vignette.Hide();
            return;
        }

        float closeness = 1f - nearest / range;
        vignette.Show(closeness);
        float interval = Mathf.Lerp(slowestInterval, fastestInterval, closeness);

        timeSinceBeep += Time.deltaTime;
        if (timeSinceBeep >= interval)
        {
            float volume = Mathf.Lerp(quietestVolume, loudestVolume, closeness);
            AudioManager.Instance.PlayUISFX(beepClip, volume);
            vignette.Pulse();
            timeSinceBeep = 0f;
        }
    }

    private float NearestAudibleDistance()
    {
        float nearest = float.MaxValue;
        foreach (HeartbeatSource source in HeartbeatSource.Active)
        {
            float distance = source.Distance;
            if (distance > range || distance >= nearest) continue;
            if (source.IsBlockedByWall()) continue;
            nearest = distance;
        }
        return nearest;
    }
}
