
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual effect of the document scan vfx.
/// </summary>
public class DocumentScanVisual : MonoBehaviour
{
    [Tooltip("Color of the scan line.")]
    [SerializeField] private Color scanColor = new Color(1f, 0.65f, 0.05f, 1f);
    [Tooltip("How long the scan effect takes.")]
    [SerializeField] private float scanTime = 0.75f;
    [Tooltip("Thickness of scan line.")]
    [SerializeField] private float lineThickness = 9f;

    private Image scanLine;
    private RectTransform lineRect;

    private Image fastLine;
    private RectTransform fastLineRect;
    [SerializeField] private float fastThickness = 5f;

    private void Awake()
    {
        // create ui object for scanning line w/ recttransform and image component
        GameObject line = new GameObject("Scan Line", typeof(RectTransform), typeof(Image));
        // put in document so moves with it
        line.transform.SetParent(transform, false);

        scanLine = line.GetComponent<Image>();
        lineRect = line.GetComponent<RectTransform>();

        // set color and ensure doesn't block interaction
        scanLine.color = scanColor;
        scanLine.raycastTarget = false;

        // stretch across document
        lineRect.anchorMin = new Vector2(0f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0.5f, 0.5f); //keep centered

        lineRect.sizeDelta = new Vector2(0f, lineThickness);
        lineRect.anchoredPosition = Vector2.zero;

        // create second smaller fast scan line
        GameObject smallerLine = new GameObject("Fast Scan Line", typeof(RectTransform), typeof(Image));
        smallerLine.transform.SetParent(transform, false);

        fastLine = smallerLine.GetComponent<Image>();
        fastLineRect = smallerLine.GetComponent<RectTransform>();

        fastLine.color = scanColor;
        fastLine.raycastTarget = false;

        // stretch
        fastLineRect.anchorMin = new Vector2(0f, 1f);
        fastLineRect.anchorMax = new Vector2(1f, 1f);
        fastLineRect.pivot = new Vector2(0.5f, 0.5f); //keep centered

        fastLineRect.sizeDelta = new Vector2(0f, fastThickness);
        fastLineRect.anchoredPosition = Vector2.zero;

        // hide until scan time!!!! yay
        line.SetActive(false);
        smallerLine.SetActive(false);
    }

    /// <summary>
    /// Plays scanning effect
    /// </summary>
    public IEnumerator Play()
    {
        float docHeight = ((RectTransform)transform).rect.height;

        // scan line go brr
        scanLine.gameObject.SetActive(true);
        fastLine.gameObject.SetActive(true);

        float time = 0f;

        while (time < scanTime)
        {
            time += Time.unscaledDeltaTime;

            float effectProgress = Mathf.Clamp01(time/scanTime);

            float y = effectProgress * docHeight * (-1);
            lineRect.anchoredPosition = new Vector2(0f, y);

            float fastY = Mathf.Max(-docHeight + fastThickness / 2f, effectProgress * docHeight * 1.09f * (-1));            fastLineRect.anchoredPosition = new Vector2(0f, fastY);

            // wait before moving again
            yield return null;
        }

        // scan done, goodbye lines
        scanLine.gameObject.SetActive(false);
        fastLine.gameObject.SetActive(false);
    }
}