using UnityEngine;
using UnityEngine.EventSystems;

public class StringPin : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform line;
    [SerializeField] private RectTransform stringPrefab;
    private StringPin connectedPin;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Disconnect();
        Transform stringLayer = GetComponentInParent<Canvas>()?.transform.Find("Strings")
            ?? transform.parent;
        line = Instantiate(stringPrefab, stringLayer);
        line.gameObject.SetActive(true);
        line.SetAsLastSibling();
        UpdateLine(eventData.position);
    }

    public void Disconnect()
    {
        if (line != null)
        {
            Destroy(line.gameObject);
            line = null;
        }
        if (connectedPin != null)
        {
            StringPin previousPin = connectedPin;
            connectedPin = null;
            previousPin.connectedPin = null;
            previousPin.line = null;
            previousPin.VerifyQuestion();
            VerifyQuestion();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateLine(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        foreach (var result in eventData.hovered)
        {
            StringPin pin = result.GetComponent<StringPin>();
            if (pin != null && pin != this)
            {
                connectedPin = pin;
                connectedPin.SetConnectedPin(this);
                UpdateLine(pin.transform.position);
                VerifyQuestion();
                return;
            }
        }
        Disconnect();
    }

   void UpdateLine(Vector2 end)
    {
        RectTransform pinRect = (RectTransform)transform;

        Vector2 pinPosition = pinRect.position;
        Vector2 direction = (end - pinPosition).normalized;
        float pinRadius = Mathf.Max(pinRect.rect.width, pinRect.rect.height) / 2f;

        Vector2 start = pinPosition + direction * pinRadius;

        Vector2 lineDirection = end - start;
        float distance = lineDirection.magnitude;
        line.position = start;

        line.rotation = Quaternion.FromToRotation(
            Vector3.right,
            lineDirection
        );

        line.sizeDelta = new Vector2(
            distance,
            line.sizeDelta.y
        );
    }

    // If it has a connected pin, returns that pin; else, returns self
    public StringPin GetConnectedPin()
    {
        return connectedPin == null ? this : connectedPin;
    }

    public RectTransform GetLine()
    {
        return line;
    }

    public void SetConnectedPin(StringPin newPin)
    {
        if (connectedPin != null && connectedPin != newPin)
        {
            connectedPin.connectedPin = null;
            connectedPin.line = null;
            connectedPin.VerifyQuestion();
        }
        connectedPin = newPin;
        if (line!=null)
        {
            Destroy(line.gameObject);
        }
        line = newPin.GetLine();
        VerifyQuestion();
    }

    private void VerifyQuestion()
    {
        GetComponentInParent<QuestionNote>()?.Verify();
    }
}
