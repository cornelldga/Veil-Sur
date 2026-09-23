using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject canvas;
    [SerializeField] AudioManager audioManager;
    [SerializeField] DialogueManager dialogueManager;
    public static GameManager Instance { get; private set; }
    public static GameObject PlayerInstance { get; set; }

    [Header("Game Settings")]
    [SerializeField] private bool isDebugMode = false;

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
    }
}
