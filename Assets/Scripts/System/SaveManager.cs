using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    [System.Serializable]
    public class PhotoSaveData
    {
        public string fileName;
        public string subjectId;
        public int slot;
    }

    [System.Serializable]
    public class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public List<PhotoSaveData> photos = new();
    }
    string savePath;

    public static SaveManager Instance;
    private void Awake()
    {
        // GameManager destroys duplicate copies of its prefab, so don't let one take over Instance
        if (Instance != null && Instance != this)
            return;

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
        SaveData data = new SaveData();

        data.sceneName = GameManager.Instance.CurrentLevelName();

        data.playerPosition = Level.current_level.player.transform.position;

        string photoFolder = Path.Combine(Application.persistentDataPath, "Photos");

        if (!Directory.Exists(photoFolder))
            Directory.CreateDirectory(photoFolder);

        // Clear old images
        foreach (string file in Directory.GetFiles(photoFolder))
            File.Delete(file);

        for (int i = 0; i < PhotoStorage.Instance.Photos.Count; i++)
        {
            Photo photo = PhotoStorage.Instance.Photos[i];

            if (photo == null)
                continue;

            Texture2D tex = photo.ImageTexture as Texture2D;

            if (tex == null)
                continue;

            string fileName = $"photo_{i}.png";
            string fullPath = Path.Combine(photoFolder, fileName);

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            data.photos.Add(new PhotoSaveData
            {
                fileName = fileName,
                subjectId = photo.SubjectId(),
                slot = i
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    /// <summary>
    /// Loads the game from the current save file. If no save exists, creates a new one
    /// </summary>
    public async void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            return;
        }
        string json = File.ReadAllText(savePath);

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        // saves from before scenes were saved by name have no sceneName
        if (data == null || string.IsNullOrEmpty(data.sceneName))
        {
            Debug.LogWarning("Save file is missing a scene name, ignoring it.");
            return;
        }

        // wait for the scene to finish loading so we apply the save to the new level's player,
        // and so GameManager's scene-load ClearAll runs before we restore photos
        if (!await GameManager.Instance.LoadSceneAsync(data.sceneName))
            return;

        // load player first
        Level.current_level.player.SetActive(false);
        Level.current_level.player.transform.position = data.playerPosition;
        Level.current_level.player.SetActive(true);

        // load photos
        LoadPhotos(data);
    }

    private void LoadPhotos(SaveData data)
    {
        PhotoStorage.Instance.ClearAll();

        string photoFolder = Path.Combine(Application.persistentDataPath, "Photos");

        foreach (PhotoSaveData savePhoto in data.photos)
        {
            string path = Path.Combine(photoFolder, savePhoto.fileName);

            if (!File.Exists(path))
                continue;

            byte[] bytes = File.ReadAllBytes(path);

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            GameObject photoObj = new GameObject("Photo");
            Photo photo = photoObj.AddComponent<Photo>();

            photo.SetImage(tex);
            photo.SetSubject(savePhoto.subjectId);

            PhotoStorage.Instance.AddPhoto(photo);
        }
    }

    //NOTE: this is all temporary code until GameManager has UI for loading. Just having a temporary binding until
    private PlayerControls controls;

    private void OnEnable()
    {
        if (Instance != this) return; // duplicate about to be destroyed
        controls.General.Enable();
        controls.General.Load.performed += OnLoad;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        controls.General.Disable();
        controls.General.Load.performed -= OnLoad;
    }

    private void OnLoad(InputAction.CallbackContext ctx)
    {
        LoadGame();
    }

    /// <summary>
    /// Returns true if there is already a save for the current level.
    /// </summary>
    public bool HasCurrentLevelSave()
    {
        if (!File.Exists(savePath))
            return false;

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
            return false;

        return data.sceneName == GameManager.Instance.CurrentLevelName();
    }
}