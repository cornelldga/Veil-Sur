using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Monitor puzzle from Level One. The monitor displays static until the ID puzzle is solved, at which point it
/// displays the security camera feed. This security feed must then be photographed by the player.
/// Attempting to interact with the monitor before completion of the ID puzzle is prevented and displays a message.
/// </summary>
public class MonitorPuzzle : MonoBehaviour, Interactable
{
    [Tooltip("Renderer for the monitor screen.")]
    [SerializeField] private Renderer screen;

    [Tooltip("Static displayed on screen prior to completion of ID puzzle.")]
    [SerializeField] private Material staticScreen;

    [Tooltip("Feed that is displayed on screen after completion of ID puzzle.")]
    [SerializeField] private Material cameraScreen;

    [Tooltip("Photographable aspect of the monitor screen, only enabled after completion of ID puzzle.")]
    [SerializeField] private PhotographableObject feedSubject;

    [Tooltip("What is triggered if the player interacts with the monitor early.")]
    [SerializeField] private UnityEvent earlyInteract;

    private bool idFound;


    private void Start()
    {
        screen.material = staticScreen;
        feedSubject.enabled = false;
    }

    /// <summary>
    /// Switches the monitor display from the static feed to the camera feed.
    /// Called when the ID puzzle is solved properly.
    /// </summary>
    public void Access()
    {
        idFound = true;
        screen.material = cameraScreen;
        feedSubject.enabled = true;
    }

    /// <summary>
    /// Called when the player attempts to interact with the monitor.
    /// </summary>
    public void Interact()
    {
        if (!idFound)
        {
            earlyInteract.Invoke();
        }
    }
}