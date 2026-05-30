namespace GeoTutor.SceneEngine.Models;

public class SceneArc
{
    public string Id { get; set; } = "";

    // Content-pack format: angle-mark arc defined by three point references.
    public string? Vertex { get; set; }
    public string? From   { get; set; }
    public string? To     { get; set; }
    public string? Label  { get; set; }

    // Center-based format (legacy / programmatic).
    public string Center { get; set; } = "";
    public double Radius { get; set; }
    public double StartAngleDeg { get; set; }
    public double SweepAngleDeg { get; set; } = 360; // full circle by default
    public bool Highlighted { get; set; }
}
