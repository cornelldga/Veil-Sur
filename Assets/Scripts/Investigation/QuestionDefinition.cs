using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Question data, submitted evidence, and validation. Has no UI dependencies.</summary>
public class QuestionDefinition : MonoBehaviour
{   
    [Tooltip("Whether we check photos by subject IDs or actual objects.")]
    [SerializeField] private bool requireIDs = false;
    [SerializeField] private List<PhotographableObject> required_objects = new();
    [SerializeField] private bool requireAllObjects = true;
    [Tooltip("If requireIDs is true, this is the list of subject IDs required.")]
    [SerializeField] private List<string> requiredSubjectIds = new();
    [Tooltip("If requireIDs is true, whether we need ALL IDs in the list to be present.")]
    [SerializeField] private bool requireAllIDs= true;
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
        bool changed = false;
        for (int i = 0; i < photos.Count; i++)
        {
            if (photos[i] == null || storage.Owns(photos[i])) continue;
            photos[i] = null;
            changed = true;
        }
        if (changed) ResetResult();
    }

    public Photo GetPhoto(int index)
    {
        return index >= 0 && index < photos.Count ? photos[index] : null;
    }

    internal int GetPhotoIndex(Photo photo) => photos.IndexOf(photo);

    public void SetPhoto(Photo photo, int index = 0)
    {
        if (IsCorrect || index < 0) return;
        if (photo != null) PhotoStorage.Instance.MoveToQuestion(photo, this, index);
        else if (GetPhoto(index) != null) PhotoStorage.Instance.ReturnPhoto(GetPhoto(index));
    }

    internal void AssignPhoto(Photo photo, int index = 0)
    {
        while (photos.Count <= index) photos.Add(null);
        photos[index] = photo;
        ResetResult();
    }
    public void AttachPhoto(Photo photo)
    {
        SetPhoto(photo);
    }
    public void RemovePhoto(Photo photo)
    {
        if (photo == null) return;
        int index = GetPhotoIndex(photo);
        if (index >= 0) SetPhoto(null, index);
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
        if (requireIDs)
        {
            return CheckAnswerIDs();
        }
        return IsCorrect || (HasStoredPhotos() && required_objects.Count > 0 &&
            (requireAllObjects ? required_objects.All(obj => obj != null && photos.Any(photo => photo != null && photo.HasSubject(obj.SubjectId))) : required_objects.Any(obj => obj != null && photos.Any(photo => photo != null && photo.HasSubject(obj.SubjectId)))));
    }

    /// <summary>
    /// Same function as CheckAnswer(), but for IDs
    /// </summary>
    /// <returns>Whether the linked photos contain all required subject IDs</returns>
    public bool CheckAnswerIDs() {
        if (IsCorrect) return true;

        if (!HasStoredPhotos() || requiredSubjectIds.Count == 0)
            return false;

        bool Matches(string subjectId)
        {
            return !string.IsNullOrWhiteSpace(subjectId) &&
                photos.Any(photo => photo != null && photo.HasSubject(subjectId));
        }

        return requireAllIDs
            ? requiredSubjectIds.All(Matches)
            : requiredSubjectIds.Any(Matches);
    }

    private bool HasStoredPhotos()
    {
        return photos.Any(photo => photo != null) && PhotoStorage.Instance != null &&
            photos.All(photo => photo == null || PhotoStorage.Instance.Owns(photo));
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
