namespace GeoTutor.Core.Models;

public class Attempt
{
    public long Id { get; set; }
    public string ItemId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool Correct { get; set; }
    public long TimeMs { get; set; }
    public int HintsUsed { get; set; }
    public bool DialoguePlayed { get; set; }
    public string? ErrorCategory { get; set; }
}
