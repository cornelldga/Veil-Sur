using UnityEngine;

/// <summary>
/// Manages player stamina consumption, regeneration, and exhaustion states.
/// </summary>
public class StaminaController : MonoBehaviour
{
    [Header("Stamina Settings")]
    private float maxStamina = 100.0f;
    [SerializeField] private float staminaDrainRate = 25.0f;
    [SerializeField] private float staminaRegenRate = 30.0f;

    private float currentStamina;

    /// <summary>
    /// Indicates whether the player is fully exhausted and must recover before sprinting.
    /// </summary>
    public bool IsExhausted { get; private set; }

    private void Start()
    {
        currentStamina = maxStamina;
    }

    /// <summary>
    /// Drains stamina over time based.
    /// </summary>
    public void DrainStamina()
    {
        currentStamina = Mathf.Max(currentStamina - (staminaDrainRate * Time.deltaTime), 0f);

        if (currentStamina <= 0f)
        {
            IsExhausted = true;
        }
    }

    /// <summary>
    /// Regenerates stamina over time.
    /// Clears `IsExhausted` once stamina reaches maxStamina.
    /// </summary>
    public void RegenerateStamina()
    {
        if (currentStamina >= maxStamina) return;

        currentStamina = Mathf.Min(currentStamina + (staminaRegenRate * Time.deltaTime), maxStamina);

        if (currentStamina >= maxStamina)
        {
            IsExhausted = false;
        }
    }
}
