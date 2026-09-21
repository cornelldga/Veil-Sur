using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [Header("Stamina Parameters")]
    private float playerStamina;
    public bool isExhausted = false;
    [SerializeField] private float maxStamina = 100.0f;

    [Header("Stamina Regen Parameters")]
    private float staminaDrain = 25;
    private float staminaRegen = 15f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStamina = maxStamina;
    }

    void Update()
    {
    }

    // Update is called once per frame
    public void DrainStamina()
    {
        playerStamina -= staminaDrain * Time.deltaTime;
        playerStamina = Mathf.Clamp(playerStamina, 0f, maxStamina);

        if (playerStamina <= 0f)
        {
            isExhausted = true;
        }
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
    }
}
