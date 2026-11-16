using System.Collections;
using UnityEngine;

/// <summary>
/// The visuals and sound of the elevator ride, kept separate from the
/// elevator's logic.
///
/// LIGHTS:
///
///     A wide emissive panel (with an optional Point Light) sweeps from the
///     bottom of the elevator to the top to simulate travel through an elevator
///     shaft, sweepCount times. The panel is wider than the door window, so it
///     always covers the gap. Its top and bottom should be transforms positioned
///     above and below the door window (natural enter and exit) in prefab.
/// 
/// Everything here is hidden except during the ride. Select the elevator to see
/// the panels outlined in the Scene view.
///
/// This script should be put on the same GameObject as the ElevatorController.
/// </summary>
public class ElevatorRideEffects : MonoBehaviour
{
    [Header("Elevator Lighting")]
    [Tooltip("Root of the moving light effect. Put a wide emissive panel (and optionally a Point Light) under it. Hidden except during the ride.")]
    [SerializeField] private Transform shaftLight;
    [Tooltip("Start of light sweep. Place an Empty Game Object so the whole panel sits below the door window (outside it).")]
    [SerializeField] private Transform lightBottom;
    [Tooltip("End of light sweep. Place an Empty Game Object so the whole panel sits above the door window (outside it).")]
    [SerializeField] private Transform lightTop;
    [Tooltip("Number of times that the light travels bottom to top during the ride. Each sweep lasts rideDuration / sweepCount. MUST BE 1+")]
    [SerializeField] private int sweepCount = 4;
    [Tooltip("How fast the light is moving per sweep. Linear movement")]
    [SerializeField] private AnimationCurve sweepCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Shaft Cover")]
    [Tooltip("Dark cube that hides the level during the ride. Hidden except during the ride.")]
    [SerializeField] private Transform shaftCover;
    [Tooltip("Travel distance that is at least the cube's height.")]
    [SerializeField] private float coverTravelDistance = 4f;
    [Tooltip("Time to rise. MUST BE above 0 and below rideDuration.")]
    [SerializeField] private float coverRiseDuration = 2f;

    [Header("SFX")]
    [Tooltip("Played once when the ride starts. Ideally about as long as rideDuration.")]
    [SerializeField] private AudioClip rideSfx;

    private Vector3 coverHome; // World position where the cover covers the window
    private bool HasShaftLight => shaftLight != null && lightBottom != null && lightTop != null;

    /// <summary>
    /// Hides the effects until a ride needs them. Called by the ElevatorController
    /// on Start, so it never races with an arrival ride starting.
    /// </summary>
    /// <param name="arriving">True if the Player is arriving by elevator.</param>
    public void Prepare(bool arriving)
    {
        if (shaftLight != null)
        {
            shaftLight.gameObject.SetActive(false);
        }

        if (shaftCover != null)
        {
            coverHome = shaftCover.position;
            shaftCover.gameObject.SetActive(arriving);
        }
    }

    /// <summary>
    /// Coroutine that runs the ride effects.
    ///
    /// The ride is split into sweepCount movements of light. For each sweep,
    /// the light goes from lightBottom to lightTop to simulate travel.
    ///
    /// The sweep resets and starts back at lightBottom for the amount of
    /// sweeps that there are. The effect is only visible while this runs.
    ///
    /// The shaft cover moves alongside the lights. When leaving, it stays in
    /// place at the end, since the old scene is about to be replaced. When
    /// arriving, it rises out of the way and is hidden again.
    /// </summary>
    /// <param name="duration">Total seconds the ride lasts</param>
    /// <param name="leaving">True when leaving this level, false when arriving at it</param>
    public IEnumerator Play(float duration, bool leaving)
    {
        float sweepDuration = duration / sweepCount;

        if (HasShaftLight)
        {
            shaftLight.position = lightBottom.position;
            shaftLight.gameObject.SetActive(true);
        }

        if (shaftCover != null)
        {
            shaftCover.position = CoverPositionAt(0f, duration, leaving);
            shaftCover.gameObject.SetActive(true);
        }

        if (AudioManager.Instance != null && rideSfx != null)
        {
            AudioManager.Instance.PlaySfxAtPosition(rideSfx, transform.position);
        }

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float sweepProgress = elapsed / sweepDuration;
            int sweepIndex = Mathf.Min(Mathf.FloorToInt(sweepProgress), sweepCount - 1);
            float t = Mathf.Clamp01(sweepProgress - sweepIndex);

            if (HasShaftLight)
            {
                float eased = sweepCurve.Evaluate(t);
                shaftLight.position = Vector3.LerpUnclamped(lightBottom.position, lightTop.position, eased);
            }

            if (shaftCover != null)
            {
                shaftCover.position = CoverPositionAt(elapsed, duration, leaving);
            }

            yield return null;
        }

        if (HasShaftLight)
        {
            shaftLight.gameObject.SetActive(false);
            shaftLight.position = lightBottom.position;
        }

        // Leaving, the cover stays up so the window stays dark until the scene switches
        if (!leaving && shaftCover != null)
        {
            shaftCover.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Returns where the shaft cover should be at a moment in the ride.
    /// Leaving, it rises from below the window.
    /// Arriving, it starts covering the window and then rises.
    /// </summary>
    /// <param name="elapsed">Seconds since the ride started</param>
    /// <param name="duration">Total seconds the ride lasts</param>
    /// <param name="leaving">True when leaving this level, false when arriving at it</param>
    private Vector3 CoverPositionAt(float elapsed, float duration, bool leaving)
    {
        Vector3 offset = Vector3.up * coverTravelDistance;
        float riseStart = leaving ? 0f : duration - coverRiseDuration;
        float progress = Mathf.SmoothStep(0f, 1f, (elapsed - riseStart) / coverRiseDuration);

        return leaving
            ? Vector3.Lerp(coverHome - offset, coverHome, progress)
            : Vector3.Lerp(coverHome, coverHome + offset, progress);
    }

    /// <summary>
    /// When the elevator is selected, draws the light sweep path with the panel
    /// outlined at both ends (yellow), and the shaft cover outlined fully below
    /// the window, covering it, and fully above it (cyan). This way everything
    /// can be lined up with the door window without turning anything on.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (lightBottom != null && lightTop != null)
        {
            Renderer panel = shaftLight != null ? shaftLight.GetComponentInChildren<Renderer>(true) : null;
            Vector3 size = panel != null ? panel.bounds.size : Vector3.zero;
            if (size == Vector3.zero) size = Vector3.one * 0.4f;

            // The panel may be offset from the root object that gets moved
            Vector3 offset = panel != null ? panel.bounds.center - shaftLight.position : Vector3.zero;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lightBottom.position, lightTop.position);
            Gizmos.DrawWireCube(lightBottom.position + offset, size);
            Gizmos.DrawWireCube(lightTop.position + offset, size);
        }

        if (shaftCover != null)
        {
            Renderer cover = shaftCover.GetComponentInChildren<Renderer>(true);
            Vector3 size = cover != null ? cover.bounds.size : Vector3.one;
            Vector3 offset = cover != null ? cover.bounds.center - shaftCover.position : Vector3.zero;

            // While playing, the cover moves, so use the stored home position
            Vector3 home = Application.isPlaying ? coverHome : shaftCover.position;
            Vector3 travel = Vector3.up * coverTravelDistance;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(home - travel + offset, size);
            Gizmos.DrawWireCube(home + offset, size);
            Gizmos.DrawWireCube(home + travel + offset, size);
        }
    }
}