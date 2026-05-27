namespace GeoTutor.SceneEngine.Models;

public class ScenePoint
{
    public string Id { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public bool Draggable { get; set; }
    public bool Computed { get; set; }   // updated by constraint solver
    public bool Highlighted { get; set; }
}
