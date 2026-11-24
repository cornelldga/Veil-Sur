using System.Collections;
using UnityEngine;

/// <summary> Level one tooltip </summary>
public class LevelOneTooltip : MonoBehaviour
{
    [Min(0f)] [SerializeField] private float disappearAfterSeconds = 15f;
    [SerializeField] private float amplitude = 7.5f;
    [SerializeField] private float moveSpeed = 1f;

    private RectTransform promptRect;
    private Vector2 basePosition;

    private IEnumerator Start()
    {
        promptRect = GetComponent<RectTransform>();
        basePosition = promptRect.anchoredPosition;

        yield return new WaitForSeconds(disappearAfterSeconds);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.unscaledTime * moveSpeed * Mathf.PI * 2f) * amplitude;
        promptRect.anchoredPosition = basePosition + new Vector2(0f, offset);
    }
}
