using System.IO;
using UnityEditor.Overlays;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [System.Serializable]
    public class SaveData
    {
        public Vector3 playerPosition;
    }
    string savePath;

    public static SaveManager Instance;
    private void Awake()
    {
        Instance = this;
        savePath = Application.persistentDataPath + "/save.json";
        print(savePath);
    }

    /// <summary>
    /// Saves the game to the file name chosen by the player
    /// </summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();

        data.playerPosition =
        Level.current_level.player.transform.position;

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);

        Debug.Log("Game Saved!");
        Debug.Log(json);
    }

    /// <summary>
    /// Loads the game from the current save file. If no save exists, creates a new one
    /// </summary>
    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found.");
            return;
        }

        string json = File.ReadAllText(savePath);

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        Level.current_level.player.transform.position =
        data.playerPosition;

        Debug.Log("Game Loaded!");
    }
}