using UnityEngine;
using UnityEngine.UI;

/// <summary>Opens the photo tab and fills the assigned slots from storage.</summary>
public class PhotoTabScript : MonoBehaviour
{
    [SerializeField] private bool startsOpen;
    [SerializeField] private Graphic openedImage;
    [SerializeField] private Graphic closedImage;
    [SerializeField] private Button openedToggle;
    [SerializeField] private Button closedToggle;
    [SerializeField] private RectTransform photoPanel;
    [SerializeField] private PhotoSlotScript[] slots;
    private PhotoStorage storage;

    public bool IsOpen { get; private set; }
    public int SlotCount => slots.Length;
    public Photo SelectedPhoto { get; private set; }

    private void Awake()
    {
        openedToggle.onClick.AddListener(Close);
        closedToggle.onClick.AddListener(Open);
        foreach (var slot in slots) slot.Initialize(this);
        SetOpen(startsOpen);
    }

    private void Start()
    {
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
