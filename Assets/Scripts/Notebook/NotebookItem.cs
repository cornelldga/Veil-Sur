using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for draggable notebook items. Handles drag input and clamps the
/// item's position within assigned bounds. Concrete note types (e.g.
/// TextNote, QuestionNote) inherit from this to add their own content.
/// </summary>
public abstract class NotebookItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform bounds;
    [SerializeField] private StringPin pin;
    [SerializeField] private float focusScale = 3f;
    private RectTransform rect;
    private Vector2 mousePosition;
    private bool isFocused;
    private Vector2 unfocusedAnchoredPosition;
    private Vector3 unfocusedScale;

    public StringPin Pin => pin;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        mousePosition = rect.anchoredPosition;
    }

    public void OnDrag(PointerEventData e)
    {
        if (isFocused)
        {
            return;
        }

        mousePosition += e.delta;
        rect.anchoredPosition = ClampToBounds(mousePosition);
    }

    public void OnEndDrag(PointerEventData e)
    {
        // Empty for now
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            ToggleFocus();
        }
    }

    private void ToggleFocus()
    {
        if (isFocused)
        {
            Unfocus();
        }
        else
        {
            Focus();
        }
    }

    private void Focus()
    {
        unfocusedAnchoredPosition = rect.anchoredPosition;
        unfocusedScale = rect.localScale;

        rect.SetAsLastSibling();
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = unfocusedScale * focusScale;
        // Keep drag updated if focused while dragging
        mousePosition = rect.anchoredPosition;

        isFocused = true;
    }

    private void Unfocus()
    {
        rect.anchoredPosition = unfocusedAnchoredPosition;
        rect.localScale = unfocusedScale;
        // Keep drag updated if focused while dragging
        mousePosition = rect.anchoredPosition;

        isFocused = false;
    }

    private Vector2 ClampToBounds(Vector2 toClamp)
    {
        // Half-sizes, since anchoredPosition is measured from the pivot
        Vector2 halfSize = rect.rect.size * 0.5f;
        Vector2 boundHalfSize = bounds.rect.size * 0.5f;

        float minX = -boundHalfSize.x + halfSize.x;
        float maxX = boundHalfSize.x - halfSize.x;
        float minY = -boundHalfSize.y + halfSize.y;
        float maxY = boundHalfSize.y - halfSize.y;

        toClamp.x = Mathf.Clamp(toClamp.x, minX, maxX);
        toClamp.y = Mathf.Clamp(toClamp.y, minY, maxY);

        return toClamp;
    }

    public void setBounds(RectTransform newBounds)
    {
        bounds = newBounds;
    }
}