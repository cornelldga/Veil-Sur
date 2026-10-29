using UnityEngine;
using System.Collections.Generic;

public class SaveTerminal : MonoBehaviour, HoldInteractable
{

    /// <summary>
    /// Called when the player performs the interact action with a save terminal. Calls Game Manager's save function.
    /// </summary>
    
    [Tooltip("Interactables that are triggered when the terminal is used.")]
    [SerializeField] private List<MonoBehaviour> linkedInteracts = new List<MonoBehaviour>();
    [Tooltip("Monitor screen that reacts when the game is saved.")]
    [SerializeField] private TerminalScreen screen;
    [Tooltip("Seconds the interact key must be held to save.")]
    [SerializeField] private float holdDuration = 2f;

    public float HoldDuration => holdDuration;

    public void OnHoldProgress(float progress)
    {
        if (screen != null) screen.SetHoldProgress(progress);
    }

    public void OnHoldReleased()
    {
        if (screen != null) screen.SetHoldProgress(0f);
    }

    public void Interact()
    {
        // Add a save game function to the GameManager and call it here to save the game when the player interacts with a save terminal.
        SaveManager.Instance.SaveGame();
        if (screen != null) screen.ShowSaved();

        // Other interactables linked to save terminal
        foreach (MonoBehaviour linked in linkedInteracts)
        {
            if (linked is Interactable interactable)
            {
                interactable.Interact();
            }
        }
    }
}
