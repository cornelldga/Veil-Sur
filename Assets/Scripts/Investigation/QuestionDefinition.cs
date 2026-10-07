using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Question data, submitted evidence, and validation. Has no UI dependencies.</summary>
public class QuestionDefinition : MonoBehaviour
{
    [SerializeField] private List<PhotographableObject> required_objects = new();
    [SerializeField] private SolutionRule rule;
    [SerializeField] private TextAsset investigationData;
    [SerializeField] private int section = 1;
    [SerializeField] private string questionId;
    private readonly List<Photo> photos = new();
    private InvestigationQuestion question;
    private PhotoStorage storage;

    public Photo Photo => photos.Count > 0 ? photos[0] : null;
    public IReadOnlyList<Photo> Photos => photos;
    public bool IsCorrect { get; private set; }
    public bool HasBeenUploaded { get; private set; }
    public string QuestionText => question?.questionText ?? "";
    public string DefaultFeedback => question?.defaultFeedback ?? "";
    public string FeedbackText { get; private set; } = "";
    public event Action AnswerChanged;

    private void Awake() { LoadQuestion(); }
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
    public SolutionRule Rule() { return rule; }
    public bool isSolved() { return IsCorrect; }

    private InvestigationAnswer FindAnswer(string subjectId)
    {
        if (question?.answers != null)
            foreach (var answer in question.answers)
                if (answer != null && answer.subjectId == subjectId) return answer;
        return null;
    }

    public string Feedback(string subjectId)
    {
        return FindAnswer(subjectId)?.message ?? DefaultFeedback;
    }

    private void LoadQuestion()
    {
        if (investigationData == null) return;
        var data = JsonUtility.FromJson<InvestigationData>(investigationData.text);
        if (data?.mentalMap == null) return;
        foreach (var entry in data.mentalMap)
        {
            if (entry == null || entry.section != section || entry.questions == null) continue;
            foreach (var candidate in entry.questions)
            {
                if (candidate == null || candidate.questionId != questionId) continue;
                question = candidate;
                return;
            }
        }
    }

    public void ResetResult()
    {
        IsCorrect = false;
        HasBeenUploaded = false;
        FeedbackText = "";
        AnswerChanged?.Invoke();
    }

    public void Verify()
    {
        var answer = Photo != null ? FindAnswer(Photo.SubjectId()) : null;
        bool stored = photos.Count > 0 && PhotoStorage.Instance != null &&
            photos.All(photo => photo != null && PhotoStorage.Instance.Owns(photo));
        if (rule != null) IsCorrect = stored && rule.IsCorrect(Photo);
        else if (required_objects.Count > 0)
            IsCorrect = stored && required_objects.All(obj => obj != null && photos.Any(photo => photo.HasSubject(obj.SubjectId)));
        else IsCorrect = stored && answer != null && answer.isCorrect;
        FeedbackText = !stored ? "Select a photo for this question." :
            answer?.message ?? question?.defaultFeedback ?? (IsCorrect ? "Correct." : "Incorrect photo.");
        HasBeenUploaded = true;
        AnswerChanged?.Invoke();
    }

    // Loaded investigation data
    [System.Serializable]
    public class InvestigationData {
        public InvestigationSection[] mentalMap;
    }

    // One section and questions
    [System.Serializable]
    public class InvestigationSection
    {
        public int section;
        public InvestigationQuestion[] questions;
    }

    // One question and its answers
    [System.Serializable]
    public class InvestigationQuestion
    {
        public string questionId;
        public string questionText;
        public string defaultFeedback;
        public InvestigationAnswer[] answers;
    }

    // Answer associated with subject id
    [System.Serializable]
    public class InvestigationAnswer {
        public string subjectId;
        public bool isCorrect;
        public string message;
    }
}
