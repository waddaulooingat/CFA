namespace GeoTutor.SceneEngine.Models;

public class SceneArc
{
    public string Id { get; set; } = "";
    public string Center { get; set; } = "";
    public double Radius { get; set; }
    public double StartAngleDeg { get; set; }
    public double SweepAngleDeg { get; set; } = 360; // full circle by default
    public bool Highlighted { get; set; }
}
