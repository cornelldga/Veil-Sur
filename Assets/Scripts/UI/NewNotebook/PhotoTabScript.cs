using UnityEngine;
using UnityEngine.UI;

/// <summary>Opens the photo tab and fills the assigned slots from storage.</summary>
public class PhotoTabScript : MonoBehaviour
{
    [Header("Tab State")]
    [Tooltip("Whether the photo tab opens when the notebook first starts.")]
    [SerializeField] private bool startsOpen;
    [Header("Tab Controls")]
    [Tooltip("Opened tab graphic within the notebook prefab.")]
    [SerializeField] private Graphic openedImage;
    [Tooltip("Closed tab graphic within the notebook prefab.")]
    [SerializeField] private Graphic closedImage;
    [Tooltip("Button that closes the opened tab.")]
    [SerializeField] private Button openedToggle;
    [Tooltip("Button that opens the closed tab.")]
    [SerializeField] private Button closedToggle;
    [Header("Photo Slots")]
    [Tooltip("Panel containing the manually configured photo slots.")]
    [SerializeField] private RectTransform photoPanel;
    [Tooltip("Slots within this prefab, in storage order. Their count sets photo capacity.")]
    [SerializeField] private PhotoSlotScript[] slots;
    private PhotoStorage storage;

    public bool IsOpen { get; private set; }
    public int SlotCount => slots.Length;
    public Photo SelectedPhoto { get; private set; }

    private void Start()
    {
        openedToggle.onClick.AddListener(Close);
        closedToggle.onClick.AddListener(Open);
        for (int i = 0; i < slots.Length; i++) slots[i].Initialize(this, i);
        SetOpen(startsOpen);
        storage = PhotoStorage.Instance;
        if (storage != null) storage.PhotosChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (storage != null) storage.PhotosChanged -= Refresh;
        openedToggle.onClick.RemoveListener(Close);
        closedToggle.onClick.RemoveListener(Open);
    }

    public void Open() { SetOpen(true); }
    public void Close() { SetOpen(false); }
    public void Toggle() { SetOpen(!IsOpen); }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        openedImage.gameObject.SetActive(open);
        closedImage.gameObject.SetActive(!open);
        openedToggle.gameObject.SetActive(open);
        closedToggle.gameObject.SetActive(!open);
        photoPanel.gameObject.SetActive(open);
    }

    public void SelectPhoto(Photo photo)
    {
        SelectedPhoto = photo;
    }

    public void Refresh()
    {
        var photos = storage != null ? storage.Photos : null;
        if (SelectedPhoto != null && !storage.Contains(SelectedPhoto)) SelectedPhoto = null;
        for (int i = 0; i < slots.Length; i++)
        {
            var photo = photos != null && i < photos.Count ? photos[i] : null;
            slots[i].SetPhoto(photo);
        }
    }
}
