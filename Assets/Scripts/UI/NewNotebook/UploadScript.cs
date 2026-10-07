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
        int submitted = 0;
        AllCorrect = true;
        foreach (var question in UIManager.Instance.Notebook.Questions)
        {
            if (!question.isActiveAndEnabled) continue;
            submitted++;
            question.Verify();
            AllCorrect &= question.IsCorrect;
        }
        AllCorrect &= submitted > 0;
        if (AllCorrect) onAllCorrect.Invoke();
    }
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Upload);
    }
}
