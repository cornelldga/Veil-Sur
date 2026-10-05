using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks player state shared across systems (crouching, notebook open/closed)
/// and player functions like opening notebook.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerState : MonoBehaviour
{
    private bool notebookOpen = false;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isMoving = false;
    private bool hasTakenPhoto = false;
    private bool inPhotoMode = false;
    private bool playerHasControl = true;

    public bool GetCrouching()
    {
        return isCrouching;
    }

    public void SetCrouching(bool value)
    {
        isCrouching = value;
    }

    public void SetSprinting(bool value)
    {
        isSprinting = value;
    }

    public bool GetSprinting()
    {
        return isSprinting;
    }

    public void SetMoving(bool value)
    {
        isMoving = value;
    }

    public bool GetMoving()
    {
        return isMoving;
    }

    public void SetTakenPicture(bool value)
    {
        hasTakenPhoto = value;
    }

    public bool HasTakenPicture()
    {
        return hasTakenPhoto;
    }

    public bool GetPhotoMode()
    {
        return inPhotoMode;
    }

    public void SetPhotoMode(bool value)
    {
        inPhotoMode = value;
    }

    public bool GetPlayerHasControl()
    {
        return playerHasControl;
    }

    public void SetPlayerHasControl(bool value)
    {
        playerHasControl = value;
    }

    public bool GetNotebookOpen()
    {
        return GameManager.Instance.state == GameManager.GameState.NOTEBOOK;
    }
}
