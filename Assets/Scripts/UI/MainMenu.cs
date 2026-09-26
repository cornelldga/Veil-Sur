/*
 * MainMenu.cs
 *
 * Description: Handles actions in main menu like starting/quitting game.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : UIScreen
{
    /// <summary>
    /// Loads the scene you want to start teh game on.
    /// </summary>
    public void PlayGame()
    {
        // or alternatively SceneManagement.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        //change logic so that gamemanger loads levels
        GameManager.Instance.GoToLevel("SampleScene");

    }
    
    public void OpenSettings()
    {
        UIManager.Instance.Show(UIManager.UIGroupId.Settings);
    }

    /// <summary>
    /// Quits the game.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
