using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public enum UIGroupId { MainMenu, Settings }

    [Header("Camera UI")]
    [SerializeField] public GameObject cameraGroup;
    [SerializeField] public Image snapOverlay;

    [Header("Notebook UI")]
    [SerializeField] public GameObject notebookGroup;

    [Header("Main Menu UI")]
    [SerializeField] public UIScreen mainMenu;

    [Header("Settings UI")]
    [SerializeField] public UIScreen settingsMenu;

    private Dictionary<UIGroupId, UIScreen> _groups;
    private UIScreen _activeGroup;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Clear duplicates out of the scene
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _groups = new Dictionary<UIGroupId, UIScreen>
        {
            { UIGroupId.MainMenu, mainMenu },
            { UIGroupId.Settings, settingsMenu }
        };

        foreach (var group in _groups.Values)
            group.Hide();
        Show(UIGroupId.MainMenu);
    }

    public void Show(UIGroupId id)
    {
        if (_activeGroup != null)
            _activeGroup.Hide();

        _activeGroup = _groups[id];
        _activeGroup.Show();
    }

    public void HideAll()
    {
        if (_activeGroup != null)
            _activeGroup.Hide();

        _activeGroup = null;
    }

}