using System.Collections;
using UnityEngine;

/// <summary>
/// Cycles a list of textures on a monitor's display surface.
/// Works on curved meshes because the mesh UVs handle the mapping.
/// Each display can optionally have a clue object that is only active
/// while that display is showing.
/// </summary>
public class InvestigationMonitor : MonoBehaviour
{
    [Tooltip("The renderer of the mesh that displays the picture.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Textures to cycle through, in order.")]
    [SerializeField] private Texture[] textures;

    [Tooltip("Same order as Textures. Leave an entry empty for displays that aren't clues.")]
    [SerializeField] private GameObject[] clueObjects;

    [Tooltip("Seconds each display is shown before switching.")]
    [SerializeField] private float interval = 5f;

    [Tooltip("Start on a random display so multiple monitors don't flip in sync.")]
    [SerializeField] private bool randomStart = false;

    [Tooltip("Also set the emission map, if the material uses emission to glow.")]
    [SerializeField] private bool setEmission = false;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

    private MaterialPropertyBlock block;
    private int currentIndex;

    private void OnEnable()
    {
        if (targetRenderer == null || textures == null || textures.Length == 0) return;

        block ??= new MaterialPropertyBlock();
        ShowDisplay(randomStart ? Random.Range(0, textures.Length) : 0);

        if (textures.Length > 1)
            StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            yield return wait;
            ShowDisplay((currentIndex + 1) % textures.Length);
        }
    }

    private void ShowDisplay(int index)
    {
        currentIndex = index;

        targetRenderer.GetPropertyBlock(block);
        block.SetTexture(BaseMapId, textures[index]);
        if (setEmission) block.SetTexture(EmissionMapId, textures[index]);
        targetRenderer.SetPropertyBlock(block);

        if (clueObjects == null) return;
        for (int i = 0; i < clueObjects.Length; i++)
        {
            if (clueObjects[i] != null)
                clueObjects[i].SetActive(i == index);
        }
    }
}