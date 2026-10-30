using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [Header("Stamina Parameters")]
    private float playerStamina;
    public bool isExhausted = false;
    [SerializeField] private float maxStamina = 100.0f;

    [Header("Stamina Regen Parameters")]
    [SerializeField] private float staminaDrain = 25f;
    [SerializeField] private float staminaRegen = 45f;
    public float StaminaPercent => playerStamina/maxStamina;

    [SerializeField] private RectTransform staminaBar;
    [SerializeField] private float barMaxLength = 540f;
    [SerializeField] private CanvasGroup staminaUI;
    [Tooltip("Pause before regenerated stamina bar disappears.")]
    [SerializeField] private float hideDelay = 1f;
    [SerializeField] private float fadeTime = 0.5f;
    private float hideTimer;
    private float fadeTimer;
    private bool isFading;
    private bool usedStamina = false;

    void Start()
    {
        playerStamina = maxStamina;
        staminaUI.alpha = 0f;
        UpdateStaminaBar();
    }

    private void Update()
    {
        if (usedStamina && playerStamina >= maxStamina)
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

    public void DrainStamina()
    {
        playerStamina -= staminaDrain * Time.deltaTime;
        playerStamina = Mathf.Clamp(playerStamina, 0f, maxStamina);

        if (playerStamina <= 0f)
        {
            isExhausted = true;
        }
        UpdateStaminaBar();
    }

    public void RegenerateStamina()
    {
        if (playerStamina < maxStamina)
        {
            playerStamina += staminaRegen * Time.deltaTime;
            playerStamina = Mathf.Clamp(playerStamina, 0f, maxStamina);

            // Once fully recovered, clear the exhausted state
            if (playerStamina >= maxStamina)
            {
                isExhausted = false;
            }
        }
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        if (playerStamina < maxStamina)
        {
            usedStamina = true;
            hideTimer = hideDelay;
            isFading = false;
            fadeTimer = fadeTime;
            staminaUI.alpha = 1f;
        }
        float newLength = barMaxLength * StaminaPercent;
        staminaBar.sizeDelta = new Vector2(newLength, staminaBar.sizeDelta.y);
    }
}