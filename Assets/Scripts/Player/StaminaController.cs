using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [Header("Stamina Parameters")]
    private float playerStamina;
    public float currentStamina => playerStamina;
    public bool isExhausted = false;
    [SerializeField] private float maxStamina = 100.0f;

    [Header("Stamina Regen Parameters")]
    [SerializeField] private float staminaDrain = 25f;
    [SerializeField] private float staminaRegen = 45f;
    public float StaminaPercent => playerStamina / maxStamina;

    [Header("Audio")]
    [SerializeField] private AudioClip exhaustClip;
    [SerializeField] private float exhaustVolume = 0.8f;

    void Start()
    {
        playerStamina = maxStamina;
    }

    public void DrainStamina()
    {
        playerStamina -= staminaDrain * Time.deltaTime;
        playerStamina = Mathf.Clamp(playerStamina, 0f, maxStamina);

        // Fire once on the edge transition into exhaustion
        if (playerStamina <= 0f && !isExhausted)
        {
            isExhausted = true;
            
            if (AudioManager.Instance != null && exhaustClip != null)
            {
                AudioManager.Instance.PlaySfxAttached(exhaustClip, transform, volume: exhaustVolume);
            }
        }
    }

    public void RegenerateStamina()
    {
        if (playerStamina < maxStamina)
        {
            playerStamina += staminaRegen * Time.unscaledDeltaTime;
            playerStamina = Mathf.Clamp(playerStamina, 0f, maxStamina);

            if (playerStamina >= maxStamina)
            {
                isExhausted = false;
            }
        }
    }
}
