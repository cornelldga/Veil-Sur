using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Notebook item representing a question.
/// </summary>
[RequireComponent(typeof(Image))]
public class QuestionNote : NotebookItem
{
    [SerializeField] private SolutionRule rule;
    public bool IsCorrect { get; private set; }
    private Image image;
    private Color originalColor;

    private void Start()
    {
        IsCorrect = false;
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    [SerializeField] private TextAsset investigationData;
    [SerializeField] private int section = 1;
    [SerializeField] private string questionId;
    private InvestigationData data;
    private InvestigationQuestion question;

    public string QuestionText {
        get {
            if (question == null) {
                return "";
            }
            return question.questionText;
        }
    }

    /// <summary>
    /// Gets the default feedback for an incorrect answer.
    /// If question has not beed loaded, returns an empty string.
    /// </summary>
    public string DefaultFeedback {
        get {
            if (question == null) {
                return "";
            }
            return question.defaultFeedback;
        }
    }

    private void Awake() {
        // When object is created, load question for this QuestionNote from JSON
        LoadQuestion();
    }

    public SolutionRule Rule() {
        return rule;
    }

    // TODO: add question-specific code like changing the question text
    // and validation

    public string Feedback(string subjectId) {
        // Check subject ids for feedback messages
        if (question == null || question.answers == null) {
            return DefaultFeedback;
        }

        foreach (InvestigationAnswer answer in question.answers) {
            if (answer.subjectId == subjectId) {
                return answer.message;
            }
        }

        // No matching subject ID, use default feedback
        return DefaultFeedback;
    }

    private void LoadQuestion() {
        if (investigationData == null)
        {
            return;
        }
        data = JsonUtility.FromJson<InvestigationData>(investigationData.text);

        // Search mental map sections
        foreach (InvestigationSection investigationSection in data.mentalMap) {
            // Sections that do not match QuestionNote's section number
            if (investigationSection.section != section || investigationSection.questions == null) {
                continue;
            }

            foreach (InvestigationQuestion investigationQuestion in investigationSection.questions) {
                // If id matches, store question & stop search
                if (investigationQuestion.questionId == questionId) {
                    question = investigationQuestion;
                    return;
                }
            }
        }
    }

    public void Verify()
    {
        StringPin connectedPin = Pin.GetConnectedPin();

        if (connectedPin == Pin || connectedPin == null)
        {
            IsCorrect = false;
            image.color = originalColor;
            return;
        }

        NotebookItem connectedNote = connectedPin == Pin ? null : connectedPin.GetComponentInParent<NotebookItem>();
        IsCorrect = rule != null && rule.IsCorrect(connectedNote);
        image.color = IsCorrect ? Color.green : Color.red;
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