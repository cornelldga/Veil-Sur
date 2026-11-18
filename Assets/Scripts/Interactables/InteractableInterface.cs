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
/// is called once the hold reaches full progress.
/// </summary>
public interface HoldInteractable : Interactable
{
    /// <summary>
    /// Seconds needed to reach full progress from zero while holding interact.
    /// </summary>
    float HoldDuration { get; }

    /// <summary>
    /// Progress from 0 to 1 to resume when a new hold starts.
    /// Return 0 for interactions that always start fresh.
    /// </summary>
    float HoldProgress { get; }

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
