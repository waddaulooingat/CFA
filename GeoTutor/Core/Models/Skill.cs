namespace GeoTutor.Core.Models;

public class Skill
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Unit { get; set; }
    public List<string> PrereqIds { get; set; } = [];
    public int DifficultyBand { get; set; } = 1;       // 1–5
    public double CurrentMastery { get; set; } = 0.0;  // 0.0–1.0
    public DateTime? LastSeen { get; set; }
    public bool IsUnlocked { get; set; }
    public List<double> RecentAttempts { get; set; } = []; // last 5
}
