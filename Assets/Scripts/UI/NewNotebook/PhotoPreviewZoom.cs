using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles zoomind and dragging of photo that has been clicked on to preview.
/// </summary>
public class PhotoPreviewZoom : MonoBehaviour, IScrollHandler, IDragHandler
{
    [SerializeField] private float zoomSpeed = 0.2f;
    [SerializeField] private float maxZoom = 4f;
    private RectTransform photoTransform;
    private float currZoom = 1f;

    /// <summary>
    /// Gets photo's RectTransform when initialized
    /// </summary>
    private void Awake()
    {
        photoTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Zooms the photo based on scroll direction. Zoom level is kept between 1x and max
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        currZoom += eventData.scrollDelta.y * zoomSpeed;
        currZoom = Mathf.Clamp(currZoom, 1f, maxZoom);

        photoTransform.localScale = Vector3.one * currZoom;

        if (currZoom == 1f)
        {
            photoTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Allows player to drag photo around to observe while zoomed in.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (currZoom > 1f)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            float scaleFactor;
            if (canvas != null)
            {
                scaleFactor = canvas.scaleFactor;
            } else
            {
                scaleFactor = 1f;
            }
            photoTransform.anchoredPosition += eventData.delta/scaleFactor;
        }
    }

    /// <summary>
    /// Resets photo to original zoom/position
    /// </summary>
    public void ResetZoom()
    {
        currZoom = 1f;
        if (photoTransform == null)
        {
            photoTransform = GetComponent<RectTransform>();
        } 
        photoTransform.localScale = Vector3.one;
        photoTransform.anchoredPosition = Vector2.zero;
    }
}