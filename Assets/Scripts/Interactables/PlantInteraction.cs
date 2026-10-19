using UnityEngine;
using System.Collections.Generic;

public class PlantInteraction : MonoBehaviour, Interactable
{
    /// <summary>
    /// Called when the player performs the interact action with a plant.
    /// 
    /// Causes a rustling SFX to play and petals to fall.
    /// </summary>

    [Tooltip("The audio effect to be played")]
    [SerializeField] private AudioClip plantSFX;

    [Tooltip("The attached particle system")]
    [SerializeField] private ParticleSystem interactionParticles;

    public void Start()
    {
        if (interactionParticles != null)
        {
            interactionParticles.Stop();
        }
    }
    public void Interact()
    {
        if (plantSFX != null)
        {
            // Play the rustling SFX at the position of the plant.
            AudioManager.Instance.PlaySfxAtPosition(plantSFX, transform.position);
        }
        if (interactionParticles != null)
        {
            interactionParticles.Play();
        }
    }
}
