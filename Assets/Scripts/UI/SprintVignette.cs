using UnityEngine;

/// <summary>
/// Handles the visual vignette effect (darkening around edges) when sprinting.
/// </summary>
public class SprintVignette : MonoBehaviour
{
    private CanvasGroup sprintVignette;
    [SerializeField] private float vignetteOpacity = 0.9f;
    [SerializeField] private float vignetteFadeSpeed = 4f;

    private void Awake()
    {
        sprintVignette = GetComponent<CanvasGroup>();
        sprintVignette.alpha = 0f;
    }

    public void UpdateSprintVignette(bool isSprinting)
    {
        float targetAlpha;

        if (isSprinting)
        {
            targetAlpha = vignetteOpacity;
        } else
        {
            targetAlpha = 0f;
        }

        sprintVignette.alpha = Mathf.Lerp(
            sprintVignette.alpha, targetAlpha, vignetteFadeSpeed * Time.deltaTime
        );
    }
}