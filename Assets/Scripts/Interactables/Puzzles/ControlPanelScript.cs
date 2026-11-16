using TMPro;
using UnityEngine;

public class ControlPanelScript : MonoBehaviour, Interactable
{
    [SerializeField] private PhotographableObject photographableObject;
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private float timeAllowedBetweenInteractions = 0.5f;
    [SerializeField] private int interactionsNeeded = 4;
    [SerializeField] private float disableAfterSeconds = 5f;
    float timeSinceLastInteraction = 0f;
    int interactionCount = 0;
    
    void Start()
    {
        photographableObject.enabled = false;
        textDisplay.SetText("Interact {0} times to activate", interactionsNeeded - interactionCount);
    }

    void Update()   
    {
        timeSinceLastInteraction += Time.deltaTime; 
        if (timeSinceLastInteraction > timeAllowedBetweenInteractions)
        {
            interactionCount = 0;
            if (!photographableObject.enabled)
            {
                textDisplay.SetText("Interact {0} times to activate", interactionsNeeded - interactionCount);
            }
        }
    }

    public void Interact()
    {
        if (photographableObject.enabled)
        {
            return;
        }

        timeSinceLastInteraction = 0f;
        interactionCount++;
        
        if (interactionCount >= interactionsNeeded)
        {
            photographableObject.enabled = true;
            interactionCount = 0;
            textDisplay.SetText("5162");
            StartCoroutine(DisableAfterDelay(disableAfterSeconds));
        } else 
        {
            textDisplay.SetText("Interact {0} times to activate", interactionsNeeded - interactionCount);
        }
    }

    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        photographableObject.enabled = false;
        textDisplay.SetText("Interact {0} times to activate", interactionsNeeded - interactionCount);
    }
}
