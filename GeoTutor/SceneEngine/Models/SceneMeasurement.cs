namespace GeoTutor.SceneEngine.Models;

public enum MeasurementType { Length, Angle, Area, DistanceToLine }

public class SceneMeasurement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MeasurementType Type { get; set; }
    public string? Segment { get; set; }     // for Length
    public string? Vertex { get; set; }      // for Angle (the angle vertex)
    public string? Arm1 { get; set; }        // for Angle
    public string? Arm2 { get; set; }        // for Angle
    public string? Point { get; set; }       // for DistanceToLine
    public string? Line { get; set; }        // for DistanceToLine (segment id)
    public string? Label { get; set; }       // display label
    public bool Show { get; set; } = true;
    public double? TargetValue { get; set; } // for Manipulate beats: "drag until = X"
}
