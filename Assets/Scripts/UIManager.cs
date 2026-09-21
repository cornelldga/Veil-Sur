using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance {get; private set;}


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
    }

    /// <summary>
    /// Sets the Film Counter UI to the current film count.
    /// </summary>
    public void UpdateFilmCounter()
    {
        TMP_Text filmCounter = transform.GetChild(0).GetComponent<TMP_Text>();
        filmCounter.text = GameManager.Instance.GetFilmCount().ToString();    
    }
}