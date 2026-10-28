using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Binds editor-authored question data to the notebook's configured UI slots.</summary>
public class NotebookUIController : SoundEvent
{
    [System.Serializable]
    public class NotebookPage
    {
        public QuestionDefinition[] startQuestions = System.Array.Empty<QuestionDefinition>();
        [Tooltip("Question views for this page. Unassigned entries are ignored.")]
        public QuestionNote[] questionSlots = System.Array.Empty<QuestionNote>();
        internal readonly Queue<QuestionDefinition> pendingQuestions = new();
    }

    [Tooltip("Photo tab belonging to this notebook prefab.")]
    [SerializeField] private PhotoTabScript photoTab;
    [SerializeField] private NotebookPage[] pages = { new(), new() };
    [Header("Photo Preview")]
    [SerializeField] private GameObject photoPreview;
    [SerializeField] private RawImage photoPreviewImage;
    [SerializeField] private TextMeshProUGUI numberCorrectText;
    [Header("Incorrect Answer Feedback")]
    [SerializeField, Min(0f)] private float incorrectShakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float incorrectAlertRadius = 30f;
    [Tooltip("Maximum horizontal offset in UI units.")]
    [SerializeField, Min(0f)] private float incorrectShakeMagnitude = 6f;
    [SerializeField] private RectTransform shakeTarget;
    private Coroutine shakeCoroutine;
    private Vector2 shakeOrigin;
    private readonly Dictionary<QuestionDefinition, NotebookPage> questionPages = new();
    private readonly HashSet<PuzzleInteractable> puzzles = new();

    [Header("Developer Stuff")]
    [Tooltip("When enabled, the notebook will always allow uploads and whether questions are correct.")]
    public bool developerMode = false;

    public PhotoTabScript PhotoTab => photoTab;
    public int CurrentPageIndex { get; private set; }
    public int PageCount => pages.Length;
    public IReadOnlyList<QuestionDefinition> StartQuestions => pages[CurrentPageIndex].startQuestions;
    public IReadOnlyList<QuestionNote> QuestionSlots => pages[CurrentPageIndex].questionSlots;
    public bool AllCurrentQuestionsCorrect => QuestionSlots.Any(slot => slot != null && slot.Definition != null) &&
        QuestionSlots.Where(slot => slot != null && slot.Definition != null).All(slot => slot.IsCorrect);
    public event System.Action PageChanged;

    public void PreviousPage() { ShowPage(CurrentPageIndex - 1); }
    public void NextPage() { ShowPage(CurrentPageIndex + 1); }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;

        StopShake();
        ClosePhotoPreview();
        CurrentPageIndex = index;
        for (int i = 0; i < pages.Length; i++)
            foreach (var slot in pages[i].questionSlots)
                if (slot != null) slot.gameObject.SetActive(i == index && slot.Definition != null);
        RefreshPageFeedback();
        PageChanged?.Invoke();
    }

    internal void RefreshPageFeedback()
    {
        numberCorrectText.text = AllCurrentQuestionsCorrect ? "ALL LINKS CORRECT" : "LINKS INCORRECT";
    }
    /// <summary>True while the enlarged photo preview is showing.</summary>
    public bool IsPhotoPreviewOpen => photoPreview != null && photoPreview.activeSelf;

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
        RegisterSoundEvent(GameManager.PlayerInstance.transform.position, incorrectAlertRadius, "Player");
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

        foreach (var page in pages)
        {
            foreach (var slot in page.questionSlots)
            {
                if (slot == null) continue;
                slot.Unbind();
                slot.gameObject.SetActive(false);
            }

            foreach (var question in page.startQuestions) page.pendingQuestions.Enqueue(question);
            ShowPendingQuestions(page);
        }
        ShowPage(0);
    }

    private void RegisterPrerequisites()
    {
        var visited = new HashSet<QuestionDefinition>();
        // Explicit starting-page assignments take precedence over inherited ownership.
        foreach (var page in pages)
            foreach (var question in page.startQuestions)
                questionPages.TryAdd(question, page);

        var questionsToVisit = new Stack<QuestionDefinition>(pages.SelectMany(page => page.startQuestions).Reverse());
        while (questionsToVisit.Count > 0)
        {
            var question = questionsToVisit.Pop();
            if (!visited.Add(question)) continue;
            var page = questionPages[question];

            foreach (var futureQuestion in question.FutureQuestions)
            {
                futureQuestion.RegisterPrerequisite(question);
                questionPages.TryAdd(futureQuestion, page);
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
                    questionPages.TryAdd(futureQuestion, page);
                    questionsToVisit.Push(futureQuestion);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var slot in pages.SelectMany(page => page.questionSlots))
            if (slot != null && slot.Definition != null) slot.Definition.Solved -= OnQuestionSolved;
        foreach (var puzzle in puzzles)
            if (puzzle != null) puzzle.Interacted -= OnPuzzleInteracted;
    }

    private void OnQuestionSolved(QuestionDefinition question)
    {
        question.Solved -= OnQuestionSolved;
        foreach (var futureQuestion in question.FutureQuestions)
            if (futureQuestion.CanUnlock) questionPages[futureQuestion].pendingQuestions.Enqueue(futureQuestion);
        ShowPendingQuestions();
        RefreshPageFeedback();
    }

    private void ShowPendingQuestions()
    {
        foreach (var page in pages) ShowPendingQuestions(page);
    }

    private void ShowPendingQuestions(NotebookPage page)
    {
        while (page.pendingQuestions.Count > 0 && ShowQuestion(page.pendingQuestions.Peek()))
            page.pendingQuestions.Dequeue();
    }

    private void OnPuzzleInteracted(PuzzleInteractable puzzle)
    {
        foreach (var futureQuestion in puzzle.FutureQuestions)
            if (futureQuestion.CanUnlock) questionPages[futureQuestion].pendingQuestions.Enqueue(futureQuestion);
        ShowPendingQuestions();
    }

    /// <summary>Reveals an empty view first, then reuses a solved one. Returns false when all slots are in use.</summary>
    public bool ShowQuestion(QuestionDefinition question)
    {
        var page = questionPages[question];
        var questionSlots = page.questionSlots;
        if (question.IsCorrect || questionSlots.Any(view => view != null && view.Definition == question)) return true;

        var slot = questionSlots.FirstOrDefault(view => view != null && view.Definition == null)
            ?? questionSlots.FirstOrDefault(view => view != null && view.IsCorrect);
        if (slot == null) return false;

        question.Solved += OnQuestionSolved;
        slot.Bind(question);
        slot.gameObject.SetActive(true);
        numberCorrectText.text = "";
        slot.gameObject.SetActive(page == pages[CurrentPageIndex]);
        RefreshPageFeedback();
        
        return true;
    }
}
