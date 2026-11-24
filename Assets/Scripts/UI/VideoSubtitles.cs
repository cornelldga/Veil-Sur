using System;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Displays authored subtitle entries using the linked video's playback time.
/// </summary>
public class VideoSubtitles : MonoBehaviour
{
    [Serializable]
    public class SubtitleEntry
    {
        [TextArea(2, 5)] public string text;
        [Tooltip("Time in seconds when this text appears.")]
        public float startTime;
        [Tooltip("Time in seconds when this text disappears.")]
        public float stopTime;
    }

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text subtitleText;
    [Tooltip("The first entry matching the playback time is displayed. Entries can be authored in any order.")]
    [SerializeField] private SubtitleEntry[] subtitles = Array.Empty<SubtitleEntry>();

    private void OnEnable()
    {
        if (subtitleText != null) subtitleText.raycastTarget = false;
        ShowSubtitle(string.Empty);
    }

    private void OnDisable()
    {
        ShowSubtitle(string.Empty);
    }

    private void LateUpdate()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
        {
            ShowSubtitle(string.Empty);
            return;
        }

        double time = videoPlayer.time;
        foreach (SubtitleEntry entry in subtitles)
        {
            if (entry != null && time >= entry.startTime && time < entry.stopTime)
            {
                ShowSubtitle(entry.text ?? string.Empty);
                return;
            }
        }

        ShowSubtitle(string.Empty);
    }

    private void ShowSubtitle(string text)
    {
        if (subtitleText != null && subtitleText.text != text)
            subtitleText.SetText(text);
    }
}
