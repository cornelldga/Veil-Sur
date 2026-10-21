using UnityEngine;
using UnityEngine.UI;

public class Settings : UIScreen
{
    [Header("Audio Sliders")]
    [Tooltip("Slider controlling sound effect volume.")]
    [SerializeField] private Slider sfxSlider;
    [Tooltip("Slider controlling music volume.")]
    [SerializeField] private Slider musicSlider;  
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider ambienceSlider;
    public void OnBackPressed()
    {
        UIManager.Instance.Back();
    }

    public override void Show() //override so sliders reflect current values every time this opens
    {
        base.Show();

        sfxSlider.value = AudioManager.Instance.GetSfxVolume();
        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sensitivitySlider.value = GameManager.Instance.MouseSensitivity;
        ambienceSlider.value = AudioManager.Instance.GetAmbienceVolume();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        sensitivitySlider.onValueChanged.AddListener(GameManager.Instance.SetMouseSensitivity);
        ambienceSlider.onValueChanged.AddListener(AudioManager.Instance.SetAmbienceVolume);
    }

    /// <summary>
    /// resets key bindings to default
    /// </summary>
    public void ResetKeyBindings()
    {
        GameManager.ResetBindings();

        // Update every row's label to show the default key again
        foreach (KeyBinding row in GetComponentsInChildren<KeyBinding>())
            row.RefreshLabel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
