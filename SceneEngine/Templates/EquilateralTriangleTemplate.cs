namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Equilateral triangle with all sides equal.
/// Parameters:
///   side – double (length of each side)
/// Layout: A=(0,0), B=(side,0), C=(side/2, side*√3/2)
/// </summary>
public sealed class EquilateralTriangleTemplate : ISceneTemplate
{
    public string TemplateName => "EquilateralTriangle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double s = GetDouble(p, "side");
        if (s <= 0)
        {
            error = "'side' must be positive.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double s = GetDouble(p, "side");

        double ax = 0,       ay = 0;
        double bx = s,       by = 0;
        double cx = s / 2.0, cy = s * Math.Sqrt(3.0) / 2.0;

        var spec = new SceneSpec { Id = "equilateral-triangle" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));

        spec.Segments.Add(Seg("AB", "A", "B", ticks: 1));
        spec.Segments.Add(Seg("BC", "B", "C", ticks: 1));
        spec.Segments.Add(Seg("CA", "C", "A", ticks: 1));

        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.00,  0.30));

        // Congruence ticks on all three sides
        spec.Marks.Add(TickMark("AB", 1));
        spec.Marks.Add(TickMark("BC", 1));
        spec.Marks.Add(TickMark("CA", 1));

        // Equal angle marks at each vertex
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 1 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "C", GroupIndex = 1 });

        // Measurements — all angles are 60°
        spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
        spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
        spec.Measurements.Add(AngleMeasurement("mAngC", "B", "C", "A"));
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));

        return spec;
    }
}
