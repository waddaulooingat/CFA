namespace GeoTutor.SceneEngine.Models;

public enum ConstraintType
{
    Perpendicular, Parallel, EqualLength, FixedLength, FixedAngle,
    Horizontal, Vertical, Midpoint, OnSegment, OnCircle
}

public class SceneConstraint
{
    public ConstraintType Type { get; set; }
    public string? Of { get; set; }
    public string? To { get; set; }
    public string? Point { get; set; }
    public string? Segment { get; set; }
    public string? Circle { get; set; }
    public double? Value { get; set; }
}
