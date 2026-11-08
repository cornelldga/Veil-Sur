using UnityEngine;
using UnityEngine.UI;

public class PageToggleScript : MonoBehaviour
{
    [SerializeField] private NotebookUIController notebook;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [SerializeField, Range(0f, 1f)] private float disabledOpacity = 80f / 255f;

    private void Awake()
    {
        ConfigureButton(leftButton);
        ConfigureButton(rightButton);
    }

    private void ConfigureButton(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        var disabledColor = colors.disabledColor;
        disabledColor.a = disabledOpacity;
        colors.disabledColor = disabledColor;
        button.colors = colors;
    }

    private void OnEnable()
    {
        leftButton.onClick.AddListener(notebook.PreviousPage);
        rightButton.onClick.AddListener(notebook.NextPage);
        notebook.PageChanged += RefreshButtons;
        RefreshButtons();
    }

    private void OnDisable()
    {
        leftButton.onClick.RemoveListener(notebook.PreviousPage);
        rightButton.onClick.RemoveListener(notebook.NextPage);
        notebook.PageChanged -= RefreshButtons;
    }

    private void RefreshButtons()
    {
        leftButton.interactable = notebook.CurrentPageIndex > 0;
        rightButton.interactable = notebook.CurrentPageIndex < notebook.PageCount - 1;
    }
}
