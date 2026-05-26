namespace GeoTutor.Core.Models;

public class BaselineResult
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, MasteryLevel> SkillScores { get; set; } = [];
    public ReadinessFlag OverallReadiness { get; set; }
}

public enum MasteryLevel { Unknown = 0, Shaky = 1, Developing = 2, Solid = 3, Mastered = 4 }

public enum ReadinessFlag { NotReady, Conditional, Ready }
