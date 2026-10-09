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
    public void OnBackPressed()
    {
        UIManager.Instance.Back();
    }

    [Header("Test (temporary)")]
    [Tooltip("Test clip used to preview SFX volume. Remove once done testing.")]
    [SerializeField] private AudioClip testSfxClip;
    public void PlayTestSfx() // TEMP — wire a button to this, remove later
    {
        AudioManager.Instance.PlaySfxAtPosition(testSfxClip, Camera.main.transform.position);
    }

    public override void Show() //override so sliders reflect current values every time this opens
    {
        base.Show();

        sfxSlider.value = AudioManager.Instance.GetSfxVolume();
        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sensitivitySlider.value = GameManager.Instance.MouseSensitivity;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        sensitivitySlider.onValueChanged.AddListener(GameManager.Instance.SetMouseSensitivity);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
