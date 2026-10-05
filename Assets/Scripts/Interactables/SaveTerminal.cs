using UnityEngine;

public class SaveTerminal : MonoBehaviour, Interactable
{

    /// <summary>
    /// Called when the player performs the interact action with a save terminal. Calls Game Manager's save function.
    /// </summary>
    public void Interact()
    {
        // Add a save game function to the GameManager and call it here to save the game when the player interacts with a save terminal.
        SaveManager.Instance.SaveGame();
    }
}
