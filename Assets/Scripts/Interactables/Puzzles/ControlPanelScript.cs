using UnityEngine;

public class ControlPanelScript : MonoBehaviour, Interactable
{
    [SerializeField] private PhotographableObject photographableObject;
    [SerializeField] private GameObject screen;
    [SerializeField] private float timeAllowedBetweenInteractions = 0.5f;
    [SerializeField] private int interactionsNeeded = 4;
    [SerializeField] private float disableAfterSeconds = 5f;
    float timeSinceLastInteraction = 0f;
    int interactionCount = 0;
    
    void Start()
    {
        photographableObject.enabled = false;
        screen.SetActive(false);
    }

    void Update()
    {
        timeSinceLastInteraction += Time.deltaTime; 
        if (timeSinceLastInteraction > timeAllowedBetweenInteractions)
        {
            interactionCount = 0;
        }
    }

    public void Interact()
    {
        timeSinceLastInteraction = 0f;
        interactionCount++;
        if (interactionCount >= interactionsNeeded)
        {
            photographableObject.enabled = true;
            screen.SetActive(true);
            StartCoroutine(DisableAfterDelay(disableAfterSeconds));
        }
    }

    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        photographableObject.enabled = false;
        screen.SetActive(false);
    }
}
