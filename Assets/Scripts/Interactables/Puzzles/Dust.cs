using UnityEngine;

/// <summary>
/// Dust covering something hidden (so far, just the pawprints). When the player interacts
/// with it, the dust disappears to reveal the hidden object underneath.
/// </summary>
public class Dust : MonoBehaviour, Interactable
{
    [Tooltip("The object that is revealed when the dust is cleaned off.")]
    [SerializeField] private GameObject hidden;

    private bool cleaned;

    private void Start()
    {
        hidden.SetActive(false);
    }

    /// <summary>
    /// Called after player interacts with dust.
    /// </summary>
    public void Interact()
    {
        if (cleaned)
        {
            return;
        }

        cleaned = true;
        hidden.SetActive(true);
        gameObject.SetActive(false);
    }
}