using UnityEngine;

/// <summary>Per-level question data. Lives in the scene and hands its questions to the persistent notebook on load.</summary>
public class LevelQuestions : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public QuestionDefinition[] startQuestions = System.Array.Empty<QuestionDefinition>();
    }

    [Tooltip("Starting questions for each notebook page, in page order.")]
    [SerializeField] private Page[] pages = { new(), new() };

    public int PageCount => pages.Length;
    public QuestionDefinition[] GetStartQuestions(int page) => page < pages.Length ? pages[page].startQuestions : System.Array.Empty<QuestionDefinition>();

    private void Start()
    {
        if (UIManager.Instance == null || UIManager.Instance.Notebook == null)
        {
            Debug.LogWarning("LevelQuestions: no notebook to load questions into.", this);
            return;
        }
        UIManager.Instance.Notebook.Load(this);
    }
}
