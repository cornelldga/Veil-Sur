using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
    }

    public async Task LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            await Task.Yield();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
