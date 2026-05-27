namespace GeoTutor.SceneEngine.Models;

public enum SegmentKind { Segment, Ray, Line }

public class SceneSegment
{
    public string Id { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public SegmentKind Kind { get; set; } = SegmentKind.Segment;
    public bool Highlighted { get; set; }
    public int CongruenceTicks { get; set; }  // 0 = none, 1–3 = tick marks
}
