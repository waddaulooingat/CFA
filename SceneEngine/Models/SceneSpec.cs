namespace GeoTutor.SceneEngine.Models;

public class SceneSpec
{
    public string Id { get; set; } = "";
    public Viewport Viewport { get; set; } = new();
    public List<ScenePoint> Points { get; set; } = [];
    public List<SceneSegment> Segments { get; set; } = [];
    public List<SceneArc> Arcs { get; set; } = [];
    public List<SceneLabel> Labels { get; set; } = [];
    public List<SceneMark> Marks { get; set; } = [];
    public List<SceneMeasurement> Measurements { get; set; } = [];
    public List<SceneConstraint> Constraints { get; set; } = [];
}

public class Viewport
{
    public double XMin { get; set; } = -2;
    public double XMax { get; set; } = 12;
    public double YMin { get; set; } = -2;
    public double YMax { get; set; } = 8;
}
