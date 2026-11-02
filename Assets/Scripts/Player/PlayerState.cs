using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks player state shared across systems (crouching, notebook open/closed)
/// and player functions like opening notebook.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerState : MonoBehaviour
{
    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isMoving = false;
    private bool hasTakenPhoto = false;
    private bool inPhotoMode = false;
    private bool playerHasControl = true;
    private bool isDocOpen = false;
    private string openDocSubjectId = "";

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

    public bool GetDocOpen()
    {
        return isDocOpen;
    }

    /// <summary>
    /// Sets whether a document is open, and records the document's subject ID
    /// (for scans). Closing a document clears the open document ID.
    /// </summary>
    public void SetDocOpen(bool value, string subjectId = "")
    {
        isDocOpen = value;
        openDocSubjectId = value ? subjectId : "";
    }

    /// <summary>
    /// Returns the subject ID of the open document, or an empty string if there is
    /// subject or no document is open.
    /// </summary>
    public string GetOpenDocSubjectId()
    {
        return openDocSubjectId;
    }

    public bool GetNotebookOpen()
    {
        return GameManager.Instance.state == GameManager.GameState.NOTEBOOK;
    }
}
