using UnityEngine;

/// <summary>
/// Screen-edge darkness that closes in as a heartbeat mutant nears and throbs on each beep.
/// Driven by HeartMonitor.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ProximityVignette : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float farOpacity = 0.5f;
    [SerializeField, Range(0f, 1f)] private float nearOpacity = 0.95f;
    [SerializeField, Min(1f)] private float farScale = 1.8f;
    [SerializeField, Min(1f)] private float nearScale = 1.15f;
    [SerializeField] private float pulseSize = 0.1f;
    [SerializeField] private float pulseFadeSpeed = 5f;

    private CanvasGroup canvasGroup;
    private bool isShowing;
    private float closeness;
    private float pulse;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
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

        if (!isShowing)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = Mathf.Lerp(farOpacity, nearOpacity, closeness);

        float scale = Mathf.Lerp(farScale, nearScale, closeness) - pulseSize * pulse * closeness;
        transform.localScale = Vector3.one * Mathf.Max(scale, 1f);
    }
}
