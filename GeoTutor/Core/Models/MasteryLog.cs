namespace GeoTutor.Core.Models;

public class MasteryLog
{
    public long Id { get; set; }
    public string SkillId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public double MasteryScore { get; set; }
    public int DifficultyBand { get; set; }
    public string Event { get; set; } = ""; // "attempt", "decay", "reset"
}
