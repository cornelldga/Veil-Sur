using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UploadScript : MonoBehaviour
{
    [Tooltip("Called when all active questions on the notebook page are correct.")]
    [SerializeField] private UnityEvent onAllCorrect = new();
    private Button button;
    public bool AllCorrect { get; private set; }

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Upload);
    }
    public void Upload()
    {
        var questions = UIManager.Instance.Notebook.QuestionSlots
            .Where(slot => slot.isActiveAndEnabled && slot.Definition != null)
            .Select(slot => slot.Definition)
            .ToArray();
        AllCorrect = questions.Length > 0;
        foreach (var question in questions)
        {
            question.Verify();
            AllCorrect &= question.IsCorrect;
        }
        if (AllCorrect) onAllCorrect.Invoke();
    }
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Upload);
    }
}
