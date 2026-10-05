using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Saves the game to the file name chosen by the player
    /// </summary>
    public void SaveGame()
    {
        //get information from the level instance
    }

    /// <summary>
    /// Loads the game from the current save file. If no save exists, creates a new one
    /// </summary>
    public void LoadGame()
    {
        //put information into the current level instance
    }
}
