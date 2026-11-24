using System.Collections;
using UnityEngine;

/// <summary> Level one tooltip </summary>
public class TimedTooltip : MonoBehaviour
{
    [Min(0f)] [SerializeField] private float disappearAfterSeconds = 15f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(disappearAfterSeconds);
        gameObject.SetActive(false);
    }
}
