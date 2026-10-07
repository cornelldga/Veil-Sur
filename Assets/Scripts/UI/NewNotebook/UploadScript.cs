using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UploadScript : MonoBehaviour
{
    [Tooltip("Required root containing the current page's questions. Assign it in the Inspector.")]
    [SerializeField] private Transform questionRoot;
    [SerializeField] private UnityEvent onAllCorrect = new();
    public bool AllCorrect { get; private set; }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Upload);
    }
    public void Upload()
    {
        var targets = questionRoot.GetComponentsInChildren<QuestionNote>();
        AllCorrect = targets.Length > 0;
        foreach (var question in targets)
        {
            question.Verify();
            AllCorrect &= question.IsCorrect;
        }
        if (AllCorrect) onAllCorrect.Invoke();
    }
}
