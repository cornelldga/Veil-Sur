using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Screen-edge darkness that grows inward as a heartbeat mutant nears and throbs on each beep.
/// Driven by HeartMonitor.
/// </summary>
public class ProximityVignette : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float farIntensity = 0f;
    [SerializeField, Range(0f, 1f)] private float nearIntensity = 0.55f;
    [SerializeField, Range(0.01f, 1f)] private float smoothness = 0.5f;
    [SerializeField] private float pulseSize = 0.1f;
    [SerializeField] private float pulseFadeSpeed = 5f;

    private Volume volume;
    private Vignette vignette;
    private bool isShowing;
    private float closeness;
    private float pulse;

    private void Awake()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(true);
        vignette.smoothness.value = smoothness;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;
        volume.sharedProfile = profile;
    }

    public void Show(float newCloseness)
    {
        isShowing = true;
        closeness = newCloseness;
    }

    public void Hide()
    {
        isShowing = false;
    }

    public void Pulse()
    {
        pulse = 1f;
    }

    private void Update()
    {
        pulse = Mathf.MoveTowards(pulse, 0f, pulseFadeSpeed * Time.deltaTime);

        volume.weight = isShowing ? 1f : 0f;
        if (!isShowing)
        {
            return;
        }

        float intensity = Mathf.Lerp(farIntensity, nearIntensity, closeness);
        vignette.intensity.value = intensity + pulseSize * pulse * closeness;
    }
}
