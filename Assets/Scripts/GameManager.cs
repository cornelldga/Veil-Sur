using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int filmCount = 0; 
    [SerializeField] int maxFilmCount = 10; // for playtesting

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
    
    /// <summary>
    /// Returns the current amount of film the player has.
    /// </summary>
    /// <returns> int filmCount : amount of film </returns>
    public int GetFilmCount()
    {
        return filmCount;
    }

    /// <summary>
    /// Increments the film counter by one if it is less than the max amount of film
    /// the player can have.
    /// </summary>
    public void IncrementFilmCounter()
    {
        if(filmCount < maxFilmCount) 
        {
            filmCount++;
            UIManager.Instance.UpdateFilmCounter();
        }
    }
}
