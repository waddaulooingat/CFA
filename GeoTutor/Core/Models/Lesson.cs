using GeoTutor.SceneEngine.Models;

namespace GeoTutor.Core.Models;

public class Lesson
{
    public string Id { get; set; } = "";
    public string SkillId { get; set; } = "";
    public List<Beat> Beats { get; set; } = [];
    public int Version { get; set; } = 1;
}

public enum BeatType { Concept, Manipulate, WorkedExample, Check, Practice, Reflection }

public class Beat
{
    public BeatType Type { get; set; }
    public string Title { get; set; } = "";
    public string SceneSpecId { get; set; } = "";
    public SceneSpec? Scene { get; set; }          // inline scene from content pack
    public string Prose { get; set; } = "";
    public List<string> ItemIds { get; set; } = [];
    public BeatSuccessCondition? SuccessCondition { get; set; }
    public string SuccessMessage { get; set; } = "";
}

// ── Success condition models ──────────────────────────────────────────────────

public class BeatSuccessCondition
{
    public string Type { get; set; } = "";

    // collinear
    public List<string> Points { get; set; } = [];
    public double TolerancePx { get; set; } = 6;

    // equalLengths ("of" and "and" are segment IDs)
    public string? Of { get; set; }
    public string? And { get; set; }

    // angleMeasure
    public string? Vertex { get; set; }
    public List<string>? Sides { get; set; }
    public double? TargetDegrees { get; set; }

    // counterexample
    public string? SatisfiesHypothesis { get; set; }
    public string? ViolatesConclusion { get; set; }

    // shared tolerance (degrees for angles, world-units for lengths)
    public double Tolerance { get; set; } = 1.0;

    // explorationGoal
    public List<ExplorationMilestone> Milestones { get; set; } = [];
    public bool CompleteWhenAll { get; set; } = true;
}

public class ExplorationMilestone
{
    public string Id { get; set; } = "";
    public string Test { get; set; } = "";
}
