using UnityEngine;
using UnityEngine.UI;

public class ControlPanelScript : MonoBehaviour, HoldInteractable
{
    [SerializeField] private PhotographableObject photographableObject;
    [SerializeField] private TerminalScreen textDisplay;
    [SerializeField] private Image fillBar;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float disableAfterSeconds = 5f;
    [SerializeField] private string displayText = "Hold to activate";
    [SerializeField] private string activatedText = "5162";

    private bool isHolding;
    private float drainSpeed;

    public float HoldDuration => holdTime;
    public float HoldProgress => fillBar.fillAmount;

    void Start()
    {
        photographableObject.enabled = false;
        fillBar.fillAmount = 0f;
        textDisplay.SetText(displayText);
    }

    void Update()
    {
        if (isHolding || fillBar.fillAmount <= 0f) return;

        fillBar.fillAmount = Mathf.MoveTowards(fillBar.fillAmount, 0f, drainSpeed * Time.deltaTime);
        if (fillBar.fillAmount <= 0f)
        {
            photographableObject.enabled = false;
            textDisplay.SetText(displayText);
        }
    }

    public void OnHoldProgress(float progress)
    {
        isHolding = true;
        fillBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void OnHoldReleased()
    {
        isHolding = false;
        float duration = photographableObject.enabled
            ? disableAfterSeconds * fillBar.fillAmount
            : holdTime * (1f - fillBar.fillAmount);

        drainSpeed = duration > 0f ? fillBar.fillAmount / duration : float.MaxValue;
    }

    public void Interact()
    {
        photographableObject.enabled = true;
        fillBar.fillAmount = 1f;
        textDisplay.SetText(activatedText);
        OnHoldReleased();
    }
}
