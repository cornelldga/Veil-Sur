using Unity.VisualScripting;
using UnityEngine;
using System.Collections;


/// <summary>
/// Controls the player's noise radius based on movement state and camera usage.
/// Updates the loudness detection collider so enemies can react to player-generated sound.
/// </summary>
public class LoudnessControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private PlayerStateController playerStateController;
    [Tooltip("Collider used to detect the player based on their noise radius")]
    [SerializeField] SphereCollider loudnessDetectorCollider;
    [SerializeField] int crouchRadius = 2;
    [SerializeField] int walkRadius = 5;
    [SerializeField] int runRadius = 10;
    [SerializeField] int cameraRadius = 10;

    private bool cameraAfterNoise = false;

    /// <summary>
    /// Retrieves required component references used by the loudness system.
    /// </summary>
    void Start()
    {
        playerStateController = GetComponent<PlayerStateController>();
    }

    /// <summary>
    /// Updates the loudness detection radius based on the player's current movement state
    /// and active camera noise effects.
    /// </summary>
    void Update()
    {
        //if we're still processing the noise from the camera, don't change radius
        //because if we allow radius changing it will be overwritten
        if (cameraAfterNoise) { return; }

        if (!playerStateController.GetMoving())
        {
            loudnessDetectorCollider.radius = 0;
        } else if (playerStateController.GetCrouching())
        {
            loudnessDetectorCollider.radius = crouchRadius;
        } else if(playerStateController.GetSprinting())
        {
            loudnessDetectorCollider.radius = runRadius;
        } else
        {
            loudnessDetectorCollider.radius = walkRadius;
        }

        if (playerStateController.HasTakenPicture())
        {
            loudnessDetectorCollider.radius = cameraRadius;
            cameraAfterNoise = true;
            StartCoroutine(Delay());
        }
    }

    /// <summary>
    /// Delays resetting the temporary camera noise effect.
    /// </summary>
    /// <returns>
    /// An IEnumerator used by the coroutine system.
    /// </returns>
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(1f);
        playerStateController.SetTakenPicture(false);
        cameraAfterNoise = false;
    }
}
