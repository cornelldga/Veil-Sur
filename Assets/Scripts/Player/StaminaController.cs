using UnityEngine;

/// <summary>
/// Manages player stamina consumption, regeneration, and exhaustion states.
/// </summary>
public class StaminaController : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100.0f;
    [SerializeField] private float staminaDrainRate = 25.0f;
    [SerializeField] private float staminaRegenRate = 30.0f;

    private float currentStamina;

    /// <summary>
    /// Gets the player's current stamina value.
    /// </summary>
    public float CurrentStamina => currentStamina;

    /// <summary>
    /// Gets the maximum possible stamina.
    /// </summary>
    public float MaxStamina => maxStamina;

    /// <summary>
    /// Indicates whether the player is fully exhausted and must recover before sprinting.
    /// </summary>
    public bool IsExhausted { get; private set; }

    private void Start()
    {
        currentStamina = maxStamina;
    }

    /// <summary>
    /// Drains stamina over time based on deltaTime.
    /// Flags IsExhausted as true when stamina reaches zero.
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
    /// Regenerates stamina over time based on deltaTime.
    /// Clears IsExhausted once stamina reaches maxStamina.
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
