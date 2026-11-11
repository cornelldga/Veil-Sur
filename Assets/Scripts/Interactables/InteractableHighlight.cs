using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brightens every renderer under this object using its own texture and color,
/// so the original look is preserved. Added automatically by PlayerInteraction,
/// or placed manually on a prefab to customize the intensity.
/// </summary>
public class InteractableHighlight : MonoBehaviour
{
    [Tooltip("How strongly the object brightens. 0 = none, 0.1 to 0.2 = subtle")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.20f;

    [Tooltip("Optional tint mixed into the glow. White keeps the original colors")]
    [SerializeField] private Color tint = Color.white;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

    private struct MatState
    {
        public Material material;
        public Color originalEmission;
        public Texture originalMap;
        public bool hadEmission;
    }

    private readonly List<MatState> states = new List<MatState>();
    private bool initialized;

    private void Init()
    {
        initialized = true;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;

            foreach (Material m in r.materials) // instances, so shared assets aren't modified
            {
                if (!m.HasProperty(EmissionColorId)) continue;
                states.Add(new MatState
                {
                    material = m,
                    originalEmission = m.GetColor(EmissionColorId),
                    originalMap = m.GetTexture(EmissionMapId),
                    hadEmission = m.IsKeywordEnabled("_EMISSION")
                });
            }
        }
    }

    public void SetHighlighted(bool on)
    {
        if (!initialized) Init();

        foreach (MatState s in states)
        {
            Material m = s.material;
            if (m == null) continue;

            if (on)
            {
                m.EnableKeyword("_EMISSION");
                // Emit the object's own texture and color so its look is kept
                m.SetTexture(EmissionMapId, m.mainTexture);
                Color baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                                : m.HasProperty("_Color") ? m.GetColor("_Color")
                                : Color.white;
                m.SetColor(EmissionColorId, s.originalEmission + baseColor * tint * intensity);
            }
            else
            {
                m.SetTexture(EmissionMapId, s.originalMap);
                m.SetColor(EmissionColorId, s.originalEmission);
                if (!s.hadEmission) m.DisableKeyword("_EMISSION");
            }
        }
    }

    private void OnDisable() => SetHighlighted(false);
}