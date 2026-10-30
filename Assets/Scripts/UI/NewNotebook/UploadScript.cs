using System.Linq;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]

[RequireComponent(typeof(RawImage))]
public class UploadScript : MonoBehaviour
{
    private bool developerMode => UIManager.Instance != null && UIManager.Instance.Notebook != null && UIManager.Instance.Notebook.developerMode;
    private Button button;
    public bool AllCorrect => UIManager.Instance.Notebook.AllCurrentQuestionsCorrect;

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
            .Where(slot => slot != null && slot.isActiveAndEnabled && slot.Definition != null)
            .Select(slot => slot.Definition)
            .ToArray();

        if (developerMode)
        {
            foreach (var question in questions)
                question.Verify();
        }

        int correctCount = questions.Count(q => q.CheckAnswer());
        bool allCorrect = questions.Length > 0 && correctCount == questions.Length;
        Debug.Log($"Upload: {correctCount}/{questions.Length} correct. All correct: {allCorrect}");
        UIManager.Instance.Notebook.PlayUploadResult(allCorrect);
        if (correctCount < questions.Length)
            UIManager.Instance.Notebook.ShakeIncorrectAnswer();

        if (allCorrect && !developerMode)
        {
            foreach (var question in questions)
                question.Verify();
        }
        UIManager.Instance.Notebook.RefreshPageFeedback();
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Upload);
    }
}
