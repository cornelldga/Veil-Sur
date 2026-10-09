using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    [SerializeField] DialogueManager dialogueManager;
    public static GameManager Instance { get; private set; }
    public static GameObject PlayerInstance { get; set; }
    public static Camera PlayerCamera { get; set; }

    [Header("Music")]
    [SerializeField] private AudioClip lobbyMusic;

    

    [Header("Game Settings")]
    [SerializeField] private bool isDebugMode = false;
    [SerializeField] GameObject losePanel;

    public GameState state {  get; set; }

    public enum GameState
    {
        PAUSED,
        DEFAULT,
        CAMERA,
        NOTEBOOK
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Clear duplicates out of the scene
            return;
        }

        Instance = this;

        // Keeps this object alive when switching scenes
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        Debug.Log("GameManager Initialized. Setting up systems...");
        // Setup sound, saving profiles, loading data, etc.
        //HandleSceneMusic("MainMenu");
        // This is TEMPORARY
        AudioManager.Instance.PlayMusic(lobbyMusic);
    }

    public async void GoToLevel(string sceneName)
    {
        await LoadSceneAsync(sceneName);
    }
    
    public async Task LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"Scene '{sceneName}' could not be loaded. Is it in Build Settings?");
            return;
        }

        while (!op.isDone)
            await Task.Yield();

        HandleSceneMusic(sceneName);
    }

    /// <summary>
    /// Requests the game manager to change the state.
    /// There is no guarantee that the change actually happens.
    /// </summary>
    /// <param name="state">the state you want to change to</param>
    public void RequestStateChange(GameState state)
    {
        this.state = state;
    }

    /// <summary>
    /// The temporary lose state for the game. 
    /// Stops time for the game and enables the lose panel 
    /// to be seen for the player.
    /// </summary>
    public void LoseGame()
    {
        losePanel.SetActive(true);
        Time.timeScale = 0f;
        PlayerInstance.GetComponent<PlayerState>().SetPlayerHasControl(false);
    }

    /// <summary>
    /// Plays the correct music clip based on the scene.
    /// </summary>
    /// <param name="sceneName">the scene we're transitioning to</param>
    private void HandleSceneMusic(string sceneName)
    {
        switch (sceneName)
        {
            case "TutorialLevel":
                AudioManager.Instance.PlayMusic(lobbyMusic);
                break;

            default:
                Debug.LogWarning($"No music assigned for scene: {sceneName}");
                break;
        }
    }

    
}
