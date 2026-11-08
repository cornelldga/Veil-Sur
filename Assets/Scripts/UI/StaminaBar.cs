using UnityEngine;

/// <summary>
/// Handles the visual stamina bar on the screen. The stamina bar shrinks towards the middle when
/// stamina is being used, then grows back outwards and disappears when stamina is regenerating/regenerated.
/// </summary>
public class StaminaBar : MonoBehaviour
{
    private RectTransform staminaBar;
    private CanvasGroup staminaUI;

    [SerializeField] private float barMaxLength = 540f;
    [SerializeField] private float hideDelay = 1f;
    [SerializeField] private float fadeTime = 0.5f;

    private float hideTimer;
    private float fadeTimer;
    private bool isFading;
    private bool usedStamina;

    void Start()
    {
        staminaUI = GetComponent<CanvasGroup>();
        staminaBar = transform.Find("StaminaBar").GetComponent<RectTransform>();

        staminaUI.alpha = 0f;
    }

    private void Update()
    {
        float staminaPercent = GameManager.Instance.GetStaminaPercentage();

        UpdateStaminaBar();

        if (usedStamina && staminaPercent >= 1f)
        {
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
            } else
            {
                if (!isFading)
                {
                    isFading = true;
                    fadeTimer = fadeTime;
                }

                fadeTimer -= Time.deltaTime;
                staminaUI.alpha = Mathf.Clamp01(fadeTimer/fadeTime);
            }
        }
    }
    private void UpdateStaminaBar()
    {
        float staminaPercent = GameManager.Instance.GetStaminaPercentage();

        if (staminaPercent < 1f)
        {
            usedStamina = true;
            hideTimer = hideDelay;
            fadeTimer = fadeTime;
            staminaUI.alpha = 1f;
        }

        float newLength = barMaxLength * staminaPercent;
        staminaBar.sizeDelta = new Vector2(newLength, staminaBar.sizeDelta.y);
    }
}