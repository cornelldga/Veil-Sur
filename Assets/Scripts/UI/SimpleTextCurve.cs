using TMPro;
using UnityEngine;

/// <summary>
/// Bends UI text in depth across its RectTransform without changing its timing.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class SimpleTextCurve : MonoBehaviour
{
    [Tooltip("Depth at the left and right edges of the text rectangle relative to its centre, in local canvas units. Use a negative value to reverse the curve; zero is flat.")]
    [SerializeField] private float curveDepth = 100f;

    private TextMeshProUGUI textComponent;

    private void OnEnable()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.OnPreRenderText += CurveText;
        RefreshText();
    }

    private void OnDisable()
    {
        if (textComponent == null) return;

        textComponent.OnPreRenderText -= CurveText;
        RefreshText();
    }

    private void OnValidate()
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null) return;

        textComponent.havePropertiesChanged = true;
        textComponent.SetVerticesDirty();
    }

    private void CurveText(TMP_TextInfo textInfo)
    {
        Rect rect = textComponent.rectTransform.rect;
        float halfWidth = rect.width * 0.5f;
        if (halfWidth <= 0f) return;

        // TMP supplies fresh geometry here whenever the text is rebuilt.
        for (int meshIndex = 0; meshIndex < textInfo.materialCount; meshIndex++)
        {
            TMP_MeshInfo mesh = textInfo.meshInfo[meshIndex];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 vertex = mesh.vertices[i];
                float x = (vertex.x - rect.center.x) / halfWidth;
                vertex.z += curveDepth * x * x;
                mesh.vertices[i] = vertex;
            }
        }
    }
}
