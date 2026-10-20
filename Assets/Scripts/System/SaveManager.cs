using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Overlays;
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
        public int levelNumber;
        public Vector3 playerPosition;
        public List<PhotoSaveData> photos = new();
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
        SaveData data = new SaveData();

        data.levelNumber = GameManager.Instance.CurrentLevel();

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
    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            return;
        }
        string json = File.ReadAllText(savePath);

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        //load level
        GameManager.Instance.LoadLevel(data.levelNumber);

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

        return data.levelNumber == GameManager.Instance.CurrentLevel();
    }
}