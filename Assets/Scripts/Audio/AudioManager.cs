using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent singleton audio controller that plays 3D positional sound effects 
/// through a pool of reused AudioSources. Survives scene changes,
/// so should not really be reinstatiated ever in game
/// 
/// If no instance exists yet (e.g. a GameManager
/// hasn't created one), accessing Instance creates one automatically
/// 
/// 
/// 
/// 
/// Author: Andrew Hu 
/// Sprint 1 Veil Sur
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                // creates a new game object that beomes audio manager
                instance = new GameObject("AudioManager").AddComponent<AudioManager>();
            }
            return instance;
        }
    }

    [Header("SFX Pool")]
    [Tooltip("How many audio sources are initially present. More sources = more conseecutive sounds that can be played.")] 
    [SerializeField] private int initialPoolSize = 8;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 25f;

    [Header("Mixer")]
    [Tooltip("Master audio mixer with exposed SFXVolume and MusicVolume parameters.")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("Mixer group all pooled SFX sources route through.")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [Tooltip("Mixer group the music source routes through.")]
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Music")]
    [Tooltip("Dedicated AudioSource used for background music playback.")]
    [SerializeField] private AudioSource musicSource;
    private const string SfxVolumeParam = "SFXVolume";
    private const string MusicVolumeParam = "MusicVolume";

    private float _currentSfxVolume = 1f;
    private float _currentMusicVolume = 1f;

    private readonly Queue<AudioSource> availableSources = new Queue<AudioSource>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            availableSources.Enqueue(CreatePooledSource());
        }
    }

    /// <summary>
    /// Sets the SFX bus volume. Value is linear (0 = silent, 1 = full volume).
    /// </summary>
    public void SetSfxVolume(float linearVolume)
    {
        _currentSfxVolume = linearVolume;
        SetMixerVolume(SfxVolumeParam, linearVolume);
    }

    /// <summary>
    /// Sets the music bus volume. Value is linear (0 = silent, 1 = full volume).
    /// </summary>
    public void SetMusicVolume(float linearVolume)
    {
        _currentMusicVolume = linearVolume;
        SetMixerVolume(MusicVolumeParam, linearVolume);
    }

    public float GetSfxVolume() => _currentSfxVolume;
    public float GetMusicVolume() => _currentMusicVolume;

    private void SetMixerVolume(string exposedParam, float linearVolume)
    {
        float dB = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
        audioMixer.SetFloat(exposedParam, dB);
    }

    /// <summary>
    /// Plays a looping music track through the dedicated music source.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }


    /// <summary>
    /// Plays a one-shot 3D sound effect at fixed world <paramref name="position"/> using a pooled
    /// AudioSource. It will add to the pool if every source is currently busy.
    /// </summary>
    /// 
    /// <param name="position"> Where the emission of the audio is in the world </param>
    /// 
    /// 
    public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.transform.SetParent(transform);
        source.transform.position = position;
        PlayOnSource(source, clip, volume, pitch);
    }

    /// <summary>
    /// Plays a one-shot 3D sound effect that follows a moving transform (e.g.
    /// footsteps) by parenting a pooled AudioSource to it for the duration of playback.
    /// </summary>
    public void PlaySfxAttached(AudioClip clip, Transform attachTo, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || attachTo == null) return;

        AudioSource source = GetAvailableSource();
        source.transform.SetParent(attachTo, false);
        source.transform.localPosition = Vector3.zero;
        PlayOnSource(source, clip, volume, pitch);
    }

    private AudioSource CreatePooledSource()
    {
        GameObject sourceObject = new GameObject("PooledAudioSource");
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f; // pretty sure this is what makes it heard relative to the AudioListener on the player camera
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.outputAudioMixerGroup = sfxGroup;

        return source;
    }

    private AudioSource GetAvailableSource()
    {
        return availableSources.Count > 0 ? availableSources.Dequeue() : CreatePooledSource();
    }

    private void PlayOnSource(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        StartCoroutine(ReclaimWhenFinished(source));
    }

    private IEnumerator ReclaimWhenFinished(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);

        source.transform.SetParent(transform);
        source.transform.localPosition = Vector3.zero;
        source.clip = null;
        availableSources.Enqueue(source);
    }
}
