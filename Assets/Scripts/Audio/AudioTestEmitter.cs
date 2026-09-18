using UnityEngine;

/// <summary>
/// This is a test object that emits a sound every [interval] seconds 
/// at [volume] volume. Attach to any entity.
/// </summary>
public class AudioTestEmitter : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private float interval = 2f;
    [SerializeField] private float volume = 1f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval) return;

        timer = 0f;
        AudioManager.Instance.PlaySfxAtPosition(clip, transform.position, volume);
    }
}
