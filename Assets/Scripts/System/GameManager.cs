using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;
using UnityEngine.InputSystem; 

public class GameManager : MonoBehaviour
{
    [SerializeField] DialogueManager dialogueManager;
    public static GameManager Instance { get; private set; }
    public static GameObject PlayerInstance { get; set; }
    public static Camera PlayerCamera { get; set; }

    [Header("Music")]
    [SerializeField] private AudioClip lobbyMusic;

    [Header("Startup")]
    [SerializeField] private string titleSceneName = "TitleScreen";

    [Header("Game Settings")]
    [SerializeField] private bool isDebugMode = false;
    [SerializeField] GameObject losePanel;

    private const string SensitivityKey = "MouseSensitivity";
    public float MouseSensitivity { get; private set; } = 1f;

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = value;
        PlayerPrefs.SetFloat(SensitivityKey, value);
    }

    /// <summary>
    /// Fired after every state change as (oldState, newState). UIManager listens to this.
    /// </summary>
    public static event Action<GameState, GameState> StateChanged;

    private const string RebindsKey = "InputRebinds";
    private static PlayerControls _controls;

    /// <summary>
    /// The one shared PlayerControls instance. Created on first use, with any saved
    /// rebinds applied, so it doesn't matter which script asks for it first.
    /// </summary>
    public static PlayerControls Controls
    {
        get
        {
            if (_controls == null)
            {
                _controls = new PlayerControls();
                string json = PlayerPrefs.GetString(RebindsKey, "");
                if (!string.IsNullOrEmpty(json))
                    _controls.asset.LoadBindingOverridesFromJson(json);
            }
            return _controls;
        }
    }

    public static void SaveBindings()
    {
        PlayerPrefs.SetString(RebindsKey, Controls.asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    public static void ResetBindings()
    {
        Controls.asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(RebindsKey);
    }

    public static bool IsRebinding { get; set; }
    private GameState _state = GameState.DEFAULT;
    public GameState state
    {
        get => _state;
        set => RequestStateChange(value);
    }

    private GameState _stateBeforePause = GameState.DEFAULT;

    public enum GameState
    {
        MAIN_MENU,
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

        MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);

        // Keeps this object alive when switching scenes
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }
    
    private void OnEnable()
    {
        if (Instance != null && Instance != this) return; // duplicate about to be destroyed
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == titleSceneName)
            SetState(GameState.MAIN_MENU);        // entering the title scene
        else if (_state == GameState.MAIN_MENU)
            SetState(GameState.DEFAULT);          // leaving the title scene
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || _state == GameState.MAIN_MENU || IsRebinding) return;

        if (kb.escapeKey.wasPressedThisFrame)
            TogglePause();
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
    /// <param name="newState">the state you want to change to</param>
    public void RequestStateChange(GameState newState)
    {
        // While paused, only TogglePause can leave the state. This stops things like
        // releasing right-click (which sets DEFAULT) from silently unpausing the game.
        if (_state == GameState.PAUSED && newState != GameState.PAUSED) return;
        SetState(newState);
    }

     /// <summary>
     /// Pause/resume. Resume returns to whatever state you paused from.
     /// </summary>
    public void TogglePause()
    {
        SetState(_state == GameState.PAUSED ? _stateBeforePause : GameState.PAUSED);
    }
 
    /// <summary>
    /// The only place _state is actually written.
    /// </summary>
    /// <param name="newState"></param>
    private void SetState(GameState newState)
    {
        if (newState == _state) return;
 
        GameState old = _state;
        if (newState == GameState.PAUSED) _stateBeforePause = old;
 
        _state = newState;
        Time.timeScale = (newState == GameState.PAUSED) ? 0f : 1f;
 
        StateChanged?.Invoke(old, newState);
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
