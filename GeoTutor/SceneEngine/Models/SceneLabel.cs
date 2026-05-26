namespace GeoTutor.SceneEngine.Models;

public class SceneLabel
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string? AnchorPoint { get; set; }  // point id
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public bool IsTemporary { get; set; }
}
