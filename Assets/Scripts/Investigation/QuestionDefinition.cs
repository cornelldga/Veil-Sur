using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Question data, submitted evidence, and validation. Has no UI dependencies.</summary>
public class QuestionDefinition : MonoBehaviour
{
    [SerializeField] private List<PhotographableObject> required_objects = new();
    [TextArea]
    [SerializeField] private string questionText;
    [TextArea]
    [SerializeField] private string defaultFeedback = "Incorrect photo.";
    [Tooltip("Questions unlocked when this question is answered correctly.")]
    [SerializeField] private QuestionDefinition[] futureQuestions = Array.Empty<QuestionDefinition>();
    [Tooltip("Puzzles this question helps unlock. All questions linking to a puzzle must be answered correctly before it can be used.")]
    [SerializeField] private PuzzleInteractable[] futurePuzzles = Array.Empty<PuzzleInteractable>();
    [InspectorName("Require All Prerequisites")]
    [Tooltip("When enabled, all incoming questions must be answered and all incoming puzzles successfully interacted with. Otherwise, any one is enough.")]
    [SerializeField] private bool requireAllPrerequisiteQuestions = true;
    private readonly HashSet<QuestionDefinition> prerequisiteQuestions = new();
    private readonly HashSet<PuzzleInteractable> prerequisitePuzzles = new();
    private readonly List<Photo> photos = new();
    private PhotoStorage storage;

    public Photo Photo => photos.Count > 0 ? photos[0] : null;
    public IReadOnlyList<Photo> Photos => photos;
    public bool IsCorrect { get; private set; }
    public bool HasBeenUploaded { get; private set; }
    public string QuestionText => questionText;
    public string DefaultFeedback => defaultFeedback;
    public IReadOnlyList<QuestionDefinition> FutureQuestions => futureQuestions;
    public IReadOnlyList<PuzzleInteractable> FuturePuzzles => futurePuzzles;
    public string FeedbackText { get; private set; } = "";
    public event Action AnswerChanged;
    public event Action<QuestionDefinition> Solved;

    internal bool CanUnlock => requireAllPrerequisiteQuestions
        ? prerequisiteQuestions.All(question => question.IsCorrect) &&
            prerequisitePuzzles.All(puzzle => puzzle.HasBeenInteractedWith)
        : prerequisiteQuestions.Any(question => question.IsCorrect) ||
            prerequisitePuzzles.Any(puzzle => puzzle.HasBeenInteractedWith);

    internal void RegisterPrerequisite(QuestionDefinition question)
    {
        prerequisiteQuestions.Add(question);
    }

    internal void RegisterPrerequisite(PuzzleInteractable puzzle)
    {
        prerequisitePuzzles.Add(puzzle);
    }

    private void Start()
    {
        storage = PhotoStorage.Instance;
        if (storage != null) storage.PhotosChanged += CheckPhotos;
    }
    private void OnDestroy()
    {
        if (storage != null) storage.PhotosChanged -= CheckPhotos;
    }
    private void CheckPhotos()
    {
        if (photos.RemoveAll(photo => photo == null || !storage.Owns(photo)) > 0) ResetResult();
    }

    public void SetPhoto(Photo photo)
    {
        if (IsCorrect) return;
        if (photo != null) PhotoStorage.Instance.MoveToQuestion(photo, this);
        else if (Photo != null) PhotoStorage.Instance.ReturnPhoto(Photo);
    }

    internal void AssignPhoto(Photo photo)
    {
        photos.Clear();
        if (photo != null) photos.Add(photo);
        ResetResult();
    }
    public void AttachPhoto(Photo photo)
    {
        SetPhoto(photo);
    }
    public void RemovePhoto(Photo photo)
    {
        if (Photo == photo) SetPhoto(null);
    }
    public bool isSolved() { return IsCorrect; }

    public void ResetResult()
    {
        if (IsCorrect) return;
        IsCorrect = false;
        HasBeenUploaded = false;
        FeedbackText = "";
        AnswerChanged?.Invoke();
    }

    /// <summary>Checks the current answer without locking it or unlocking future questions.</summary>
    public bool CheckAnswer()
    {
        return IsCorrect || (HasStoredPhotos() && required_objects.Count > 0 &&
            required_objects.All(obj => obj != null && photos.Any(photo => photo.HasSubject(obj.SubjectId))));
    }

    private bool HasStoredPhotos()
    {
        return photos.Count > 0 && PhotoStorage.Instance != null &&
            photos.All(photo => photo != null && PhotoStorage.Instance.Owns(photo));
    }

    public void Verify()
    {
        if (IsCorrect) return;
        bool stored = HasStoredPhotos();
        IsCorrect = CheckAnswer();
        // FeedbackText = !stored ? "Select a photo for this question." : IsCorrect ? "Correct." : DefaultFeedback;
        HasBeenUploaded = true;
        if (IsCorrect)
            foreach (var puzzle in futurePuzzles) puzzle.DecreaseQuestionsRemaining();
        AnswerChanged?.Invoke();
        if (IsCorrect) Solved?.Invoke(this);
    }
}
