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
    public class QuestionSaveData
    {
        public string questionId;
        public bool isCorrect;
        // notebook page the question is unlocked on, -1 if it isn't unlocked
        public int unlockedPage = -1;
        public PhotoSaveData photo;
    }

    [System.Serializable]
    public class PuzzleSaveData
    {
        public string puzzleId;
        public bool hasBeenInteractedWith;
    }

    [System.Serializable]
    public class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public Quaternion playerRotation = Quaternion.identity;
        public float cameraVerticalLook;
        public List<PhotoSaveData> photos = new();
        public List<QuestionSaveData> questions = new();
        public List<PuzzleSaveData> puzzles = new();
        public bool tutorialStarted;
        public int tutorialStep;
        public List<string> playedDialogue = new();
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

        if (Level.current_level.player != null)
        {
            data.playerPosition = Level.current_level.player.transform.position;
            data.playerRotation = Level.current_level.player.transform.rotation;

            FirstPersonController fpc = Level.current_level.player.GetComponent<FirstPersonController>();
            if (fpc != null) data.cameraVerticalLook = fpc.VerticalLook;
        }

        string photoFolder = Path.Combine(Application.persistentDataPath, "Photos");

        if (!Directory.Exists(photoFolder))
            Directory.CreateDirectory(photoFolder);

        // Clear old images
        foreach (string file in Directory.GetFiles(photoFolder))
            File.Delete(file);

        for (int i = 0; i < PhotoStorage.Instance.Photos.Count; i++)
        {
            PhotoSaveData savePhoto = SavePhoto(PhotoStorage.Instance.Photos[i], photoFolder, $"photo_{i}.png", i);
            if (savePhoto != null)
                data.photos.Add(savePhoto);
        }

        // each question saves its progress and the photo attached to it
        if (LevelQuestions.Current != null)
        {
            NotebookUIController notebook = UIManager.Instance != null ? UIManager.Instance.Notebook : null;
            int questionIndex = 0;
            foreach (QuestionDefinition question in LevelQuestions.Current.AllQuestions())
            {
                data.questions.Add(new QuestionSaveData
                {
                    questionId = SaveId(question),
                    isCorrect = question.IsCorrect,
                    unlockedPage = notebook != null ? notebook.UnlockedPage(question) : -1,
                    photo = SavePhoto(question.Photo, photoFolder, $"question_{questionIndex++}.png", -1)
                });
            }

            foreach (PuzzleInteractable puzzle in LevelQuestions.Current.AllPuzzles())
            {
                data.puzzles.Add(new PuzzleSaveData
                {
                    puzzleId = SaveId(puzzle),
                    hasBeenInteractedWith = puzzle.HasBeenInteractedWith
                });
            }
        }

        if (TutorialManager.Instance != null)
        {
            data.tutorialStarted = TutorialManager.Instance.TutorialStarted;
            data.tutorialStep = TutorialManager.Instance.Step;
        }

        if (DialogueManager.Instance != null)
            data.playedDialogue = DialogueManager.Instance.GetPlayedSections();

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
        Level.current_level.player.transform.rotation = data.playerRotation;
        Level.current_level.player.SetActive(true);

        FirstPersonController fpc = Level.current_level.player.GetComponent<FirstPersonController>();
        if (fpc != null) fpc.SetVerticalLook(data.cameraVerticalLook);

        // load photos
        LoadPhotos(data);

        // wait a frame so the new scene's Start methods (which load the level's dialogue) have run
        await Awaitable.NextFrameAsync();

        // load question and puzzle progress, now that LevelQuestions has loaded this level's questions into the notebook
        LoadQuestionProgress(data);
        LoadPuzzleProgress(data);

        // load dialogue progress
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.RestorePlayedSections(data.playedDialogue);

        // load tutorial progress, after photos so the tutorial counts from the restored photos
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.RestoreProgress(data.tutorialStep, data.tutorialStarted);
    }

    private void LoadPhotos(SaveData data)
    {
        PhotoStorage.Instance.ClearAll();

        string photoFolder = Path.Combine(Application.persistentDataPath, "Photos");

        // question photos first: each one passes through storage and is moved out to its question,
        // so they never take up the storage slots the stored photos need
        LoadQuestionPhotos(data, photoFolder);

        foreach (PhotoSaveData savePhoto in data.photos)
        {
            Photo photo = LoadPhoto(savePhoto, photoFolder);
            if (photo != null && !PhotoStorage.Instance.AddPhoto(photo))
                DestroyPhoto(photo);
        }
    }

    private void LoadQuestionPhotos(SaveData data, string photoFolder)
    {
        if (data.questions.Count == 0)
            return;

        if (LevelQuestions.Current == null)
        {
            Debug.LogWarning("No LevelQuestions in the loaded level, skipping question photos.");
            return;
        }

        Dictionary<string, QuestionDefinition> questions = FindQuestions();

        foreach (QuestionSaveData saveQuestion in data.questions)
        {
            if (!questions.TryGetValue(saveQuestion.questionId, out QuestionDefinition question))
            {
                Debug.LogWarning($"Saved question '{saveQuestion.questionId}' no longer exists, skipping it.");
                continue;
            }

            Photo photo = LoadPhoto(saveQuestion.photo, photoFolder);
            if (photo == null)
                continue;

            // same path the notebook uses: the photo has to be owned by storage before it can move to a question
            if (!PhotoStorage.Instance.AddPhoto(photo) || !PhotoStorage.Instance.MoveToQuestion(photo, question))
            {
                Debug.LogWarning($"Couldn't restore the photo for question '{saveQuestion.questionId}'.");
                if (!PhotoStorage.Instance.RemovePhoto(photo))
                    DestroyPhoto(photo);
            }
        }
    }

    private void LoadQuestionProgress(SaveData data)
    {
        if (data.questions.Count == 0 || LevelQuestions.Current == null)
            return;

        NotebookUIController notebook = UIManager.Instance != null ? UIManager.Instance.Notebook : null;
        Dictionary<string, QuestionDefinition> questions = FindQuestions();

        // solved first, so the notebook can reuse solved questions' slots for the unlocked ones
        foreach (QuestionSaveData saveQuestion in data.questions)
            if (saveQuestion.isCorrect && questions.TryGetValue(saveQuestion.questionId, out QuestionDefinition question))
                question.RestoreSolved();

        if (notebook == null)
            return;

        foreach (QuestionSaveData saveQuestion in data.questions)
            if (saveQuestion.unlockedPage >= 0 && questions.TryGetValue(saveQuestion.questionId, out QuestionDefinition question))
                notebook.RestoreUnlocked(question, saveQuestion.unlockedPage);

        notebook.RefreshPageFeedback();
    }

    private void LoadPuzzleProgress(SaveData data)
    {
        if (data.puzzles.Count == 0 || LevelQuestions.Current == null)
            return;

        Dictionary<string, PuzzleInteractable> puzzles = FindById(LevelQuestions.Current.AllPuzzles());

        foreach (PuzzleSaveData savePuzzle in data.puzzles)
        {
            if (!puzzles.TryGetValue(savePuzzle.puzzleId, out PuzzleInteractable puzzle))
            {
                Debug.LogWarning($"Saved puzzle '{savePuzzle.puzzleId}' no longer exists, skipping it.");
                continue;
            }

            if (savePuzzle.hasBeenInteractedWith)
                puzzle.RestoreInteracted();
        }
    }

    private Dictionary<string, QuestionDefinition> FindQuestions()
    {
        return FindById(LevelQuestions.Current.AllQuestions());
    }

    private static Dictionary<string, T> FindById<T>(IEnumerable<T> objects) where T : Component
    {
        var byId = new Dictionary<string, T>();
        foreach (T obj in objects)
            if (!byId.TryAdd(SaveId(obj), obj))
                Debug.LogWarning($"Two objects share the save id '{SaveId(obj)}', give them different names.");
        return byId;
    }

    /// <summary>Hierarchy path of an object in the level, used to find it again when loading a save.</summary>
    private static string SaveId(Component component)
    {
        string path = component.name;
        for (Transform parent = component.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }

    /// <summary>Writes a photo's image to the photo folder. Returns null if there is nothing to save.</summary>
    private PhotoSaveData SavePhoto(Photo photo, string photoFolder, string fileName, int slot)
    {
        if (photo == null)
            return null;

        Texture2D tex = photo.ImageTexture as Texture2D;

        if (tex == null)
            return null;

        File.WriteAllBytes(Path.Combine(photoFolder, fileName), tex.EncodeToPNG());

        return new PhotoSaveData
        {
            fileName = fileName,
            subjectId = photo.SubjectId(),
            slot = slot
        };
    }

    /// <summary>Creates a Photo from a saved image. Returns null if the image is missing.</summary>
    private Photo LoadPhoto(PhotoSaveData savePhoto, string photoFolder)
    {
        if (savePhoto == null || string.IsNullOrEmpty(savePhoto.fileName))
            return null;

        string path = Path.Combine(photoFolder, savePhoto.fileName);

        if (!File.Exists(path))
            return null;

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));

        Photo photo = new GameObject("Photo").AddComponent<Photo>();
        photo.SetImage(tex);
        photo.SetSubject(savePhoto.subjectId);
        return photo;
    }

    private void DestroyPhoto(Photo photo)
    {
        Destroy(photo.ImageTexture);
        Destroy(photo.gameObject);
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