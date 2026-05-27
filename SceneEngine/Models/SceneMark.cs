namespace GeoTutor.SceneEngine.Models;

public enum MarkType { RightAngle, ParallelArrow, CongruenceTick }

public class SceneMark
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MarkType Type { get; set; }
    public string At { get; set; } = "";       // point id for right-angle mark
    public string? OnSegment { get; set; }     // segment id for tick/parallel
    public int GroupIndex { get; set; } = 1;   // 1 or 2 for matching groups
}
