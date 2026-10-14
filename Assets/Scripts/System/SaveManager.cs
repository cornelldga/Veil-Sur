using System;
using System.IO;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;

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
        controls = new PlayerControls();
        Instance = this;
        savePath = Application.persistentDataPath + "/save.json";
        //uncomment to see where the save file went
        //print(savePath);
    }

    /// <summary>
    /// Saves the game to the file name chosen by the player
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("load");
        SaveData data = new SaveData();

        data.playerPosition = Level.current_level.player.transform.position;

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);
    }

    /// <summary>
    /// Loads the game from the current save file. If no save exists, creates a new one
    /// </summary>
    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            return;
        }
        string json = File.ReadAllText(savePath);

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        Debug.Log(Level.current_level.player.transform.position);
        Debug.Log(data.playerPosition);

        //disabling is necessary so player controller doesn't override the load location
        Level.current_level.player.SetActive(false);
        Level.current_level.player.transform.position = data.playerPosition;
        Level.current_level.player.SetActive(true);
    }

    //NOTE: this is all temporary code until GameManager has UI for loading. Just having a temporary binding until
    private PlayerControls controls;

    private void OnEnable()
    {
        controls.General.Enable();
        controls.General.Load.performed += OnLoad;
    }

    private void OnDisable()
    {
        controls.General.Disable();
        controls.General.Load.performed -= OnLoad;
    }

    private void OnLoad(InputAction.CallbackContext ctx)
    {
        LoadGame();
    }
}