using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    [SerializeField] AudioManager audioManager;
    [SerializeField] DialogueManager dialogueManager;
    public static GameManager Instance { get; private set; }
    public static GameObject PlayerInstance { get; set; }
    public static Camera PlayerCamera { get; set; }

    [Header("Game Settings")]
    [SerializeField] private bool isDebugMode = false;

    // TEMP, REMOVE LATER
    [SerializeField] private GameObject UI;

    public enum PuzzleState {
        SOLVED,
        UNSOLVED
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
        if (UI!=null)
        {
            UI.SetActive(false);
        }
        Debug.Log("GameManager Initialized. Setting up systems...");
        // Setup sound, saving profiles, loading data, etc.
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
    }
    // Temporary method
    public void Win()
    {
        if (UI!=null)
        {
            UI.SetActive(true);
        }
    }
}
