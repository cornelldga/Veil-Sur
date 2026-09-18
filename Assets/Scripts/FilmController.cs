using TMPro;
using UnityEngine;

public class FilmController : MonoBehaviour
{
    [Tooltip("TEMPORARY: the ui element that displays the current amount of film the player has")]
    [SerializeField] TMP_Text filmCounter; // temporary, for visualization

    [SerializeField] int maxFilmCount = 20; 
    private int currFilmCount = 0;

   
    /// <summary>
    /// If film detects collision by player, increment the film counter by 1 and destroy itself
    /// </summary>
    void OnTriggerEnter(Collider collider)
    {
        currFilmCount = int.Parse(filmCounter.text.Trim()); // get current film count (temp)
        if(currFilmCount < maxFilmCount)
        {
            filmCounter.text = (currFilmCount+1).ToString();
            Destroy(gameObject);
        }
    }
}
