using UnityEngine;

public interface Interactable
{
    /// <summary>
    /// Called when the player performs the interact action with an interactable object.
    /// </summary>
    void Interact();
}

/// <summary>
/// An interactable that requires the interact key to be held down. Interact()
/// is called once the key has been held for HoldDuration seconds.
/// </summary>
public interface HoldInteractable : Interactable
{
    /// <summary>
    /// Seconds the interact key must be held to trigger Interact().
    /// </summary>
    float HoldDuration { get; }

    /// <summary>
    /// Called every frame while the key is held.
    /// </summary>
    /// <param name="progress">hold progress from 0 to 1</param>
    void OnHoldProgress(float progress);

    /// <summary>
    /// Called when the hold ends, either by releasing the key or looking away.
    /// </summary>
    void OnHoldReleased();
}
