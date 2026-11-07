using System.Collections.Generic;
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

    /// <summary>The LevelQuestions of the currently loaded level.</summary>
    public static LevelQuestions Current { get; private set; }

    public int PageCount => pages.Length;
    public QuestionDefinition[] GetStartQuestions(int page) => page < pages.Length ? pages[page].startQuestions : System.Array.Empty<QuestionDefinition>();

    private void Awake() { Current = this; }
    private void OnDestroy() { if (Current == this) Current = null; }

    /// <summary>Every question reachable from this level's start questions, through future questions and puzzles.</summary>
    public IEnumerable<QuestionDefinition> AllQuestions()
    {
        var visited = new HashSet<QuestionDefinition>();
        var toVisit = new Stack<QuestionDefinition>();
        foreach (var page in pages)
            foreach (var question in page.startQuestions) toVisit.Push(question);

        while (toVisit.Count > 0)
        {
            var question = toVisit.Pop();
            if (question == null || !visited.Add(question)) continue;
            yield return question;
            foreach (var future in question.FutureQuestions) toVisit.Push(future);
            foreach (var puzzle in question.FuturePuzzles)
                if (puzzle != null)
                    foreach (var future in puzzle.FutureQuestions) toVisit.Push(future);
        }
    }

    /// <summary>Every puzzle linked to one of this level's questions.</summary>
    public IEnumerable<PuzzleInteractable> AllPuzzles()
    {
        var visited = new HashSet<PuzzleInteractable>();
        foreach (var question in AllQuestions())
            foreach (var puzzle in question.FuturePuzzles)
                if (puzzle != null && visited.Add(puzzle)) yield return puzzle;
    }

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
