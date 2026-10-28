using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>
/// Monitor puzzle from Level One. The monitor displays static until the ID question is solved, at which point it
/// displays the security camera feed. This security feed must then be photographed by the player.
/// Interacting with the monitor after the poster question is solved reports how many IDs are active, which
/// reveals the ID question. Attempting to interact with the monitor before then is prevented and displays a message.
/// </summary>
public class MonitorPuzzle : PuzzleInteractable
{
    [Tooltip("Renderer for the monitor screen.")]
    [SerializeField] private Renderer screen;

    [Tooltip("Static displayed on screen prior to completion of ID question.")]
    [SerializeField] private Material staticScreen;

    [Tooltip("Feed that is displayed on screen after completion of ID question.")]
    [SerializeField] private Material cameraScreen;

    [Tooltip("Photographable aspect of the monitor screen, only enabled after completion of ID question.")]
    [SerializeField] private GameObject feedSubject;

    [Tooltip("What is triggered if the player interacts with the monitor early.")]
    [SerializeField] private UnityEvent earlyInteract;

    [Tooltip("Video player for the camera feed.")]
    [SerializeField] private VideoPlayer cameraVideo;

    [Tooltip("Question that switches the monitor to the camera feed when solved.")]
    [SerializeField] private QuestionDefinition idQuestion;

    [Tooltip("Number of inactive IDs reported when the player interacts with the monitor.")]
    [SerializeField] private int inactiveCount = 3;

    private bool idFound;


    private void Start()
    {
        screen.material = staticScreen;
        feedSubject.SetActive(false);
        if (idQuestion != null) idQuestion.Solved += OnIdQuestionSolved;
    }

    private void OnDestroy()
    {
        if (idQuestion != null) idQuestion.Solved -= OnIdQuestionSolved;
    }

    private void OnIdQuestionSolved(QuestionDefinition question)
    {
        Access();
    }

    /// <summary>
    /// Switches the monitor display from the static feed to the camera feed.
    /// Called when the ID question is solved properly.
    /// </summary>
    public void Access()
    {
        if (idFound)
        {
            return;
        }

        idFound = true;
        screen.material = cameraScreen;
        feedSubject.SetActive(true);
        cameraVideo.Play();
    }

    public override void SuccessBehavior()
    {
        if (idFound)
        {
            return;
        }

        ErrorNotifier.Show($"1 active, {inactiveCount} inactive (replace with normal popup rather than error later)");
    }

    public override void FailBehavior()
    {
        earlyInteract.Invoke();
    }
}
