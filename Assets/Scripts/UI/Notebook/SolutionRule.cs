using UnityEngine;

/// <summary>
/// Base class for puzzle solution rules. Concrete rules are created as
/// ScriptableObject assets and assigned to notes in the Inspector, so puzzle
/// design doesn't require coding.
/// </summary>
public abstract class SolutionRule : ScriptableObject
{
    /// <summary>
    /// Returns whether the submitted photo is correct according to
    /// this rule. photo may be null (question not yet answered), which
    /// must be treated as not correct rather than throwing. Must be pure —
    /// no side effects.
    /// </summary>
    public abstract bool IsCorrect(Photo photo);
}
