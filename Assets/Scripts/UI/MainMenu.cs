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
        GameManager.Instance.GoToLevel("TutorialLevel");

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
