namespace GeoTutor.Core.Models;

/// <summary>
/// Outcome of a post-lesson test as determined by <c>LessonEngineService.ScoreTest</c>.
/// </summary>
public enum LessonOutcome
{
    /// <summary>Score ≥ 80% — skill is fully mastered.</summary>
    Mastered,
    /// <summary>Score 60–79% — progressing but not yet mastered.</summary>
    Developing,
    /// <summary>Score &lt; 60% — lesson should be revisited.</summary>
    NeedsRevisit,
}
