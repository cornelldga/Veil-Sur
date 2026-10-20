using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Binds editor-authored question data to the notebook's configured UI slots.</summary>
public class NotebookUIController : MonoBehaviour
{
    [Tooltip("Photo tab belonging to this notebook prefab.")]
    [SerializeField] private PhotoTabScript photoTab;
    [Tooltip("Question data shown when this notebook starts, in display order.")]
    [SerializeField] private QuestionDefinition[] startQuestions = System.Array.Empty<QuestionDefinition>();
    [Tooltip("Reusable question views, in slot order. Their position in the hierarchy does not matter.")]
    [SerializeField] private QuestionNote[] questionSlots = System.Array.Empty<QuestionNote>();
    [Header("Photo Preview")]
    [SerializeField] private GameObject photoPreview;
    [SerializeField] private RawImage photoPreviewImage;
    [SerializeField] private TextMeshProUGUI numberCorrectText;
    [Header("Incorrect Answer Feedback")]
    [SerializeField, Min(0f)] private float incorrectShakeDuration = 0.2f;
    [Tooltip("Maximum horizontal offset in UI units.")]
    [SerializeField, Min(0f)] private float incorrectShakeMagnitude = 6f;
    [SerializeField] private RectTransform shakeTarget;
    private Coroutine shakeCoroutine;
    private Vector2 shakeOrigin;
    private readonly Queue<QuestionDefinition> pendingQuestions = new();
    private readonly HashSet<PuzzleInteractable> puzzles = new();

    public PhotoTabScript PhotoTab => photoTab;
    public IReadOnlyList<QuestionDefinition> StartQuestions => startQuestions;
    public IReadOnlyList<QuestionNote> QuestionSlots => questionSlots;
    
    [Header("Developer Stuff")]
    [Tooltip("When enabled, the notebook will always allow uploads and whether questions are correct.")]
    public bool developerMode = false;

    public void ShowPhotoPreview(Photo photo)
    {
        if (photo == null || photo.ImageTexture == null) return;

        photoPreviewImage.texture = photo.ImageTexture;
        photoPreviewImage.GetComponent<AspectRatioFitter>().aspectRatio =
            (float)photo.ImageTexture.width / photo.ImageTexture.height;
        photoPreview.transform.SetAsLastSibling();
        photoPreview.SetActive(true);
    }

    public void ClosePhotoPreview()
    {
        photoPreview.SetActive(false);
        photoPreviewImage.texture = null;
    }

    private void OnDisable()
    {
        StopShake();
        if (photoPreview != null) ClosePhotoPreview();
    }

    public void ShakeIncorrectAnswer()
    {
        StopShake();
        if (!isActiveAndEnabled || incorrectShakeDuration <= 0f || incorrectShakeMagnitude <= 0f || shakeTarget == null) return;

        shakeOrigin = shakeTarget.anchoredPosition;
        shakeCoroutine = StartCoroutine(ShakeNotebook());
    }

    private IEnumerator ShakeNotebook()
    {
        for (float elapsed = 0f; elapsed < incorrectShakeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / incorrectShakeDuration;
            float offset = Mathf.Sin(progress * Mathf.PI * 6f) * incorrectShakeMagnitude * (1f - progress);
            shakeTarget.anchoredPosition = shakeOrigin + Vector2.right * offset;
            yield return null;
        }

        shakeTarget.anchoredPosition = shakeOrigin;
        shakeCoroutine = null;
    }

    private void StopShake()
    {
        if (shakeCoroutine == null) return;

        StopCoroutine(shakeCoroutine);
        shakeCoroutine = null;
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeOrigin;
    }

    internal void Initialize()
    {
        RegisterPrerequisites();

        foreach (var slot in questionSlots)
        {
            slot.Unbind();
            slot.gameObject.SetActive(false);
        }

        foreach (var question in startQuestions) pendingQuestions.Enqueue(question);
        ShowPendingQuestions();
    }

    private void RegisterPrerequisites()
    {
        var visited = new HashSet<QuestionDefinition>();
        var questionsToVisit = new Stack<QuestionDefinition>(startQuestions);
        while (questionsToVisit.Count > 0)
        {
            var question = questionsToVisit.Pop();
            if (!visited.Add(question)) continue;

            foreach (var futureQuestion in question.FutureQuestions)
            {
                futureQuestion.RegisterPrerequisite(question);
                questionsToVisit.Push(futureQuestion);
            }

            foreach (var puzzle in question.FuturePuzzles)
            {
                puzzle.IncreaseQuestionsRemaining();
                if (!puzzles.Add(puzzle)) continue;

                puzzle.Interacted += OnPuzzleInteracted;
                foreach (var futureQuestion in puzzle.FutureQuestions)
                {
                    futureQuestion.RegisterPrerequisite(puzzle);
                    questionsToVisit.Push(futureQuestion);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var slot in questionSlots)
            if (slot != null && slot.Definition != null) slot.Definition.Solved -= OnQuestionSolved;
        foreach (var puzzle in puzzles)
            if (puzzle != null) puzzle.Interacted -= OnPuzzleInteracted;
    }

    private void OnQuestionSolved(QuestionDefinition question)
    {
        question.Solved -= OnQuestionSolved;
        foreach (var futureQuestion in question.FutureQuestions)
            if (futureQuestion.CanUnlock) pendingQuestions.Enqueue(futureQuestion);
        ShowPendingQuestions();
    }

    private void ShowPendingQuestions()
    {
        while (pendingQuestions.Count > 0 && ShowQuestion(pendingQuestions.Peek()))
            pendingQuestions.Dequeue();
    }

    private void OnPuzzleInteracted(PuzzleInteractable puzzle)
    {
        foreach (var futureQuestion in puzzle.FutureQuestions)
            if (futureQuestion.CanUnlock) pendingQuestions.Enqueue(futureQuestion);
        ShowPendingQuestions();
    }

    /// <summary>Reveals an empty view first, then reuses a solved one. Returns false when all slots are in use.</summary>
    public bool ShowQuestion(QuestionDefinition question)
    {
        if (question.IsCorrect || questionSlots.Any(view => view.Definition == question)) return true;

        var slot = questionSlots.FirstOrDefault(view => view.Definition == null)
            ?? questionSlots.FirstOrDefault(view => view.IsCorrect);
        if (slot == null) return false;

        question.Solved += OnQuestionSolved;
        slot.Bind(question);
        slot.gameObject.SetActive(true);
        numberCorrectText.text = "LINKS INCORRECT";
        
        return true;
    }
}
