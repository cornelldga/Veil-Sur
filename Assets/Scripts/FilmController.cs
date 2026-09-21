using TMPro;
using UnityEngine;

public class FilmController : MonoBehaviour
{
    /// <summary>
    /// If film detects collision by player, increment the film counter by 1 and destroy itself
    /// </summary>
    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            GameManager.Instance.IncrementFilmCounter();
            Destroy(gameObject);
        }
    }
}
