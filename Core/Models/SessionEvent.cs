namespace GeoTutor.Core.Models;

public class SessionEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = ""; // lesson_start, beat_enter, drag, answer, hint, dialogue_play, lesson_end
    public DateTime Timestamp { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
