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

    [Header("Dimming Settings")]
    [Tooltip("Alpha applied while stamina is recovering.")]
    [SerializeField] private float rechargingAlpha = 0.45f;
    [Tooltip("Speed of transition between dimmed and full opacity.")]
    [SerializeField] private float dimFadeSpeed = 6f;

    private float lastStaminaPercent = 1f;
    private float currentDimAlpha = 1f;


    private bool isFading;
    private bool usedStamina;

    void Start()
    {
        staminaUI = GetComponent<CanvasGroup>();
        staminaBar = transform.Find("StaminaBar").GetComponent<RectTransform>();

        staminaUI.alpha = 0f;
        currentDimAlpha = 0.5f;
    }

    private void Update()
    {
        float staminaPercent = GameManager.Instance.GetStaminaPercentage();

        UpdateStaminaBar(staminaPercent);

        if (usedStamina && staminaPercent >= 1f)
        {
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                currentDimAlpha = Mathf.MoveTowards(currentDimAlpha, 1f, dimFadeSpeed * Time.deltaTime);
                staminaUI.alpha = currentDimAlpha;
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
        lastStaminaPercent = staminaPercent;
    }
    private void UpdateStaminaBar(float staminaPercent)
    {

        if (staminaPercent < 1f)
        {
            usedStamina = true;
            hideTimer = hideDelay;
            fadeTimer = fadeTime;
            isFading = false;

            bool isRecharging = staminaPercent > lastStaminaPercent;
            float targetAlpha = isRecharging ? rechargingAlpha : 1f;

            currentDimAlpha = Mathf.MoveTowards(currentDimAlpha, targetAlpha, dimFadeSpeed * Time.deltaTime);
            staminaUI.alpha = currentDimAlpha;
        }

        float newLength = barMaxLength * staminaPercent;
        staminaBar.sizeDelta = new Vector2(newLength, staminaBar.sizeDelta.y);
    }
}