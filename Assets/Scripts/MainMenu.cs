/*
 * MainMenu.cs
 *
 * Description: Handles actions in main menu like starting/quitting game.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Loads the scene you want to start teh game on.
    /// </summary>
    public void PlayGame(){
        SceneManager.LoadScene("SampleScene"); //whatever the tutorial level is?
        // or alternatively SceneManagement.LoadScene(SceneManager.GetActiveScene().buildIndex+1);

    }

    /// <summary>
    /// Quits the game.
    /// </summary>
    public void QuitGame(){
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
