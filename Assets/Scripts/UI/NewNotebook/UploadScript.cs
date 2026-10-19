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
    private Button button;
    public bool AllCorrect { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.interactable = false;
        button.onClick.AddListener(Upload);
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
        button.interactable = CanUpload();

        Color tmp = GetComponent<RawImage>().color;
        tmp.a = CanUpload() ? 1f : 80 / 255f;
        GetComponent<RawImage>().color = tmp;
    }

    public void Upload()
    {
        if (!CanUpload()) return;

        var questions = UIManager.Instance.Notebook.QuestionSlots
            .Where(slot => slot.isActiveAndEnabled && slot.Definition != null)
            .Select(slot => slot.Definition)
            .ToArray();

        int correctCount = questions.Count(q => q.CheckAnswer());
        AllCorrect = questions.Length > 0 && correctCount == questions.Length;
        Debug.Log($"Upload: {correctCount}/{questions.Length} correct. All correct: {AllCorrect}");
        if (AllCorrect)
        {
            numberCorrectText.text = "ALL LINKS CORRECT";
            foreach (var question in questions)
                question.Verify();
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Upload);
    }
}
