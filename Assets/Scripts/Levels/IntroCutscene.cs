using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "TutorialLevel";

    private bool isLeaving;

    private void Start()
    {
        // Skip straight to the tutorial until the intro video exists.
        if (videoPlayer.clip == null)
        {
            Leave();
            return;
        }

        Cursor.visible = false;
        videoPlayer.loopPointReached += _ => Leave();
        // Decoding isn't ready on the first frame, so playing immediately skips the start to catch up
        videoPlayer.prepareCompleted += source => source.Play();
        videoPlayer.Prepare();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            Leave();
    }

    // Skip and the video ending can land on the same frame, so only load once.
    private void Leave()
    {
        if (isLeaving) return;
        isLeaving = true;
        GameManager.Instance.GoToLevel(nextSceneName);
    }
}