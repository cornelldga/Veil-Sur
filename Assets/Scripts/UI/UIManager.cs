using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public enum UIGroupId { MainMenu, Settings, Pause }

    [Header("Camera UI")]
    [SerializeField] public GameObject cameraGroup;
    [SerializeField] public Image snapOverlay;
    private PhotoCameraController.CameraMode cameraMode;

    [Header("Notebook UI")]
    [SerializeField] public GameObject notebookGroup;

    [Header("Screen Prefabs (assign from Project folder)")]
    [SerializeField] private UIScreen mainMenuPrefab;
    [SerializeField] private UIScreen settingsMenuPrefab;
    [SerializeField] private UIScreen pauseMenuPrefab;

    [Tooltip("Assign your Canvas (or a child of it). Screens spawn under this.")]
    [SerializeField] private Transform screenRoot;

    // Runtime instances, created once and kept for the manager's lifetime
    public UIScreen MainMenu { get; private set; }
    public UIScreen SettingsMenu { get; private set; }
    public UIScreen PauseMenu { get; private set; }

    private Dictionary<UIGroupId, UIScreen> _groups;
    private UIScreen _activeGroup;

    private readonly Stack<UIGroupId> _history = new Stack<UIGroupId>();
    private UIGroupId? _activeId;


    [Header("Doc Viewer")]
    [SerializeField] public GameObject docCanvas;
    [SerializeField] public TMP_Text docViewer;
    [SerializeField] public RectTransform docPaper;

    [Header("Delete Confirmation")]
    [SerializeField] private GameObject deleteConfirmation;

    private Photo pendingDeletion;

    [Header("Interact Popup")]
    [SerializeField] public TMP_Text interact;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Clear duplicates out of the scene
            return;
        }

        Instance = this;
        snapOverlay.canvasRenderer.SetAlpha(0f);
        InitializeScreens();
    }

    /// <summary>
    /// Subscribe/unsubscribe to GameManager's state event
    /// </summary>
    private void OnEnable()
    {
        if (Instance != null && Instance != this) return;
        GameManager.StateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.StateChanged -= OnGameStateChanged;
    }

    private void InitializeScreens()
    {
        if (mainMenuPrefab == null || settingsMenuPrefab == null || pauseMenuPrefab == null)
        {
            Debug.LogError("UIManager: assign all three screen prefabs on the GameManager prefab.", this);
            _groups = new Dictionary<UIGroupId, UIScreen>();
            return;
        }
        Transform parent = screenRoot != null ? screenRoot : transform;

        MainMenu = Instantiate(mainMenuPrefab, parent);
        SettingsMenu = Instantiate(settingsMenuPrefab, parent);
        PauseMenu = Instantiate(pauseMenuPrefab, parent);

        _groups = new Dictionary<UIGroupId, UIScreen>
        {
            { UIGroupId.MainMenu, MainMenu },
            { UIGroupId.Settings, SettingsMenu },
            { UIGroupId.Pause, PauseMenu }
        };

        foreach (var group in _groups.Values)
            group.Hide();
    }

    private void Start()
    {
        if (Instance != this) return;

        ShowInteractPrompt(false);
        ApplyState(GameManager.Instance.state);
    }

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        ApplyState(newState);
    }

    private void ApplyState(GameManager.GameState state)
    {
        HideAll();

        switch (state)
        {
            case GameManager.GameState.MAIN_MENU:
                Show(UIGroupId.MainMenu);
                break;

            case GameManager.GameState.DEFAULT:
                break; // no UI

            case GameManager.GameState.CAMERA:
                cameraGroup.SetActive(true);
                break;

            case GameManager.GameState.NOTEBOOK:
                notebookGroup.SetActive(true);
                break;

            case GameManager.GameState.PAUSED:
                Show(UIGroupId.Pause);
                break;
        }

        // Cursor + player control follow the state
        bool uiOpen = state == GameManager.GameState.MAIN_MENU
                || state == GameManager.GameState.PAUSED
                || state == GameManager.GameState.NOTEBOOK;

        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiOpen;

        if (GameManager.PlayerInstance != null)
            GameManager.PlayerInstance.GetComponent<PlayerState>().SetPlayerHasControl(!uiOpen);
    }

    public void Show(UIGroupId id)
    {
        if (_activeId == id) return;
        if (_activeId.HasValue)
            _history.Push(_activeId.Value);   // remember where we came from
        SwitchTo(id);
    }

    public void Back()
    {
        if (_history.Count > 0)
            SwitchTo(_history.Pop());
        else if (GameManager.Instance.state == GameManager.GameState.PAUSED)
            GameManager.Instance.TogglePause();
        else
            HideAll();
    }

    public void HideAll()
    {
        if (_activeGroup != null)
            _activeGroup.Hide();

        _activeGroup = null;
        _activeId = null;
        _history.Clear();
        cameraGroup.SetActive(false);
        notebookGroup.SetActive(false);
        snapOverlay.canvasRenderer.SetAlpha(0f);
    }

    private void SwitchTo(UIGroupId id)
    {
        if (_activeGroup != null)
            _activeGroup.Hide();

        if (_groups == null || !_groups.TryGetValue(id, out var screen))
        {
            Debug.LogError($"UIManager: no screen registered for {id}.", this);
            _activeGroup = null;
            _activeId = null;
            return;
        }

        _activeId = id;
        _activeGroup = screen;
        _activeGroup.Show();
    }

    // private void Update()
    // {
    //     if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
    //         UIManager.Instance.Show(UIGroupId.Pause);
    // }

    /// <summary>
    /// Displays the delete confirmation for the given photo.
    /// </summary>
    public void ShowDeleteConfirmation(Photo photo)
    {
        pendingDeletion = photo;
        if (deleteConfirmation != null)
        {
            deleteConfirmation.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the player confirms deletion of a photo. Removes the pending
    /// photo from the storage and hides the confimation pop-up.
    /// </summary>
    public void ConfirmDelete()
    {
        if (pendingDeletion != null)
        {
            PhotoStorage.Instance.RemovePhoto(pendingDeletion);
            pendingDeletion = null;
        }

        if (deleteConfirmation != null)
        {
            deleteConfirmation.SetActive(false);
        }
    }

    /// <summary>
    /// Called when the player cancels deletion. Does not remove the pending
    /// photo from the storage and hides the confirmation pop-up.
    /// </summary>
    public void CancelDelete()
    {
        pendingDeletion = null;
        if (deleteConfirmation != null)
        {
            deleteConfirmation.SetActive(false);
        }
    }

    public void ShowInteractPrompt(bool visible)
    {
        if (interact.gameObject.activeSelf != visible) interact.gameObject.SetActive(visible);
    }

    /// <summary>
    /// When the camera changes modes or states, this should be called
    /// </summary>
    public void SetCameraMode(PhotoCameraController.CameraMode cameraMode)
    {
        this.cameraMode = cameraMode;
        if (GameManager.Instance.state == GameManager.GameState.CAMERA)
            ApplyState(GameManager.GameState.CAMERA);
    }

    /// <summary>
    /// Should be called when a photo is taken. Does whatever animation is associated with a photograph being taken
    /// </summary>
    public void TakePhoto()
    {
        //puts white  overlay over and then fades it out to simulate a camera snap
        snapOverlay.canvasRenderer.SetAlpha(.5f);
        snapOverlay.CrossFadeAlpha(0f, 0.2f, ignoreTimeScale: true);
    }

    // /// <summary>
    // /// Turns document mode on/off. Only works when the game state is CameraMode
    // /// </summary>
    // private void CameraDocumentMode(bool active)
    // {
    //     if (GameManager.Instance.state != GameManager.GameState.CAMERA) { return; }
    //     if (active) CameraPictureMode(false);
    //     if (active)
    //     {
    //         snapOverlay.CrossFadeAlpha(0f, 0f, true); //cancel prev fade if still running
    //         cameraGroup.SetActive(true);
    //     }

    // }

    // /// <summary>
    // /// Turns camera mode on/off. Only works when game state is CameraMode
    // /// </summary>
    // private void CameraPictureMode(bool active)
    // {
    //     if (GameManager.Instance.state != GameManager.GameState.CAMERA) { return; }
    //     if (active) CameraDocumentMode(false);
    //     cameraGroup.SetActive(active);
    // }

    // public void FixedUpdate()
    // {
    //     //TODO this is janky hiding every time, somebody should be assigned to make this smoother
    //     HideAll();
    //     switch (GameManager.Instance.state)
    //     {
    //         case GameManager.GameState.CAMERA:
    //             if (cameraMode == PhotoCameraController.CameraMode.Photo)
    //             {
    //                 CameraPictureMode(true);
    //             }
    //             else
    //             {
    //                 CameraDocumentMode(true);
    //             }
    //             break;
    //         case GameManager.GameState.DEFAULT:
    //             //no UI
    //             break;
    //         case GameManager.GameState.NOTEBOOK:
    //             //TODO make it show up and make the notebook an official group id
    //             break;
    //         case GameManager.GameState.PAUSED:
    //             //pause menu
    //             break;
    //     }
    // }
    
    public void OnResumeClicked()   => GameManager.Instance.TogglePause();
    public void OnSettingsClicked() => Show(UIGroupId.Settings);
    public void OnBackClicked()     => Back();
    public void OnPlayClicked()
    {
        GameManager.Instance.RequestStateChange(GameManager.GameState.DEFAULT);
        GameManager.Instance.GoToLevel("TutorialLevel");
    }

}