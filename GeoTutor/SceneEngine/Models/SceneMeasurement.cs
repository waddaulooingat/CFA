namespace GeoTutor.SceneEngine.Models;

public enum MeasurementType { Length, Angle, Area, DistanceToLine, AngleBetweenLines }

public class SceneMeasurement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MeasurementType Type { get; set; }

    // Length — segment-id form
    public string? Segment { get; set; }

    // Length — content-pack from/to point-id form
    public string? From { get; set; }
    public string? To { get; set; }

    // Angle — Arm1/Arm2 form
    public string? Vertex { get; set; }
    public string? Arm1 { get; set; }
    public string? Arm2 { get; set; }

    // Angle — content-pack sides-array form: ["B","C"]
    public List<string>? Sides { get; set; }

    // DistanceToLine
    public string? Point { get; set; }
    public string? Line { get; set; }

    // AngleBetweenLines — two segment IDs
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }

    public string? Label { get; set; }
    public bool Show { get; set; } = true;
    public double? TargetValue { get; set; }
}
