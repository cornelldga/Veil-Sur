using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]

[RequireComponent(typeof(RawImage))]
public class UploadScript : MonoBehaviour
{
    [Tooltip("Text component that displays the number of correct questions on the notebook page.")]
    [SerializeField] private TextMeshProUGUI numberCorrectText;
    private bool developerMode => UIManager.Instance != null && UIManager.Instance.Notebook != null && UIManager.Instance.Notebook.developerMode;
    private Button button;
    public bool AllCorrect { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.interactable = developerMode;
        button.onClick.AddListener(Upload);

        Color tmp = GetComponent<RawImage>().color;
        tmp.a = developerMode ? 1f : 80 / 255f;
        GetComponent<RawImage>().color = tmp;
    }

    private bool CanUpload()
    {
        var notebook = UIManager.Instance != null ? UIManager.Instance.Notebook : null;
        if (notebook == null) return false;

        bool hasQuestions = false;
        foreach (var slot in notebook.QuestionSlots)
        {
            if (slot == null || !slot.isActiveAndEnabled || slot.Definition == null) continue;
            hasQuestions = true;
            if (slot.Photo == null) return false;
        }
        return hasQuestions;
    }

    private void Update()
    {
        bool canUpload = developerMode || CanUpload();
        button.interactable = canUpload;

        Color tmp = GetComponent<RawImage>().color;
        tmp.a = canUpload ? 1f : 80 / 255f;
        GetComponent<RawImage>().color = tmp;
    }

    public void Upload()
    {
        if (!developerMode && !CanUpload()) return;

        var questions = UIManager.Instance.Notebook.QuestionSlots
            .Where(slot => slot.isActiveAndEnabled && slot.Definition != null)
            .Select(slot => slot.Definition)
            .ToArray();

        if (developerMode)
        {
            foreach (var question in questions)
                question.Verify();
        }

        int correctCount = questions.Count(q => q.CheckAnswer());
        AllCorrect = questions.Length > 0 && correctCount == questions.Length;
        Debug.Log($"Upload: {correctCount}/{questions.Length} correct. All correct: {AllCorrect}");
        if (correctCount < questions.Length)
            UIManager.Instance.Notebook.ShakeIncorrectAnswer();

        if (AllCorrect)
        {
            numberCorrectText.text = "ALL LINKS CORRECT";

            if (developerMode) return;
            foreach (var question in questions)
                question.Verify();
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Upload);
    }
}
