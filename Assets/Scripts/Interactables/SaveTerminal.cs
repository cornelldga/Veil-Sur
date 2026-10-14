using UnityEngine;
using System.Collections.Generic;

public class SaveTerminal : MonoBehaviour, Interactable
{

    /// <summary>
    /// Called when the player performs the interact action with a save terminal. Calls Game Manager's save function.
    /// </summary>
    
    [Tooltip("Interactables that are triggered when the terminal is used.")]
    [SerializeField] private List<MonoBehaviour> linkedInteracts = new List<MonoBehaviour>();
    public void Interact()
    {
        // Add a save game function to the GameManager and call it here to save the game when the player interacts with a save terminal.
        SaveManager.Instance.SaveGame();

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
