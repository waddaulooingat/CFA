namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Isosceles trapezoid ABCD.
/// Parameters:
///   topBase    – double (length of the top base DC)
///   bottomBase – double (length of the bottom base AB)
///   height     – double (perpendicular height)
///
/// Layout:
///   A=(0,0), B=(bottomBase,0) — bottom base
///   D=((bottomBase-topBase)/2, height)
///   C=((bottomBase+topBase)/2, height) — top base
/// </summary>
public sealed class TrapezoidTemplate : ISceneTemplate
{
    public string TemplateName => "Trapezoid";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double top    = GetDouble(p, "topBase");
        double bottom = GetDouble(p, "bottomBase");
        double h      = GetDouble(p, "height");
        if (top    <= 0) { error = "'topBase' must be positive.";    return false; }
        if (bottom <= 0) { error = "'bottomBase' must be positive."; return false; }
        if (h      <= 0) { error = "'height' must be positive.";     return false; }
        if (top >= bottom)
        {
            error = "'topBase' must be less than 'bottomBase' for an isosceles trapezoid.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double top    = GetDouble(p, "topBase");
        double bottom = GetDouble(p, "bottomBase");
        double h      = GetDouble(p, "height");

        double overhang = (bottom - top) / 2.0;

        double ax = 0,                ay = 0;
        double bx = bottom,           by = 0;
        double cx = bottom - overhang, cy = h;   // = overhang + top
        double dx = overhang,          dy = h;

        var spec = new SceneSpec { Id = "trapezoid" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy), (dx, dy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));
        spec.Points.Add(Pt("D", dx, dy));

        // Sides
        spec.Segments.Add(Seg("AB", "A", "B"));       // bottom base
        spec.Segments.Add(Seg("BC", "B", "C", ticks: 1)); // right leg
        spec.Segments.Add(Seg("CD", "C", "D"));       // top base
        spec.Segments.Add(Seg("DA", "D", "A", ticks: 1)); // left leg (= right leg)

        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.20,  0.20));
        spec.Labels.Add(Lbl("lD", "D", "D", -0.35,  0.20));

        // Parallel arrows: AB ∥ DC
        spec.Marks.Add(ParallelMark("AB", 1));
        spec.Marks.Add(ParallelMark("CD", 1));

        // Congruence ticks on equal legs
        spec.Marks.Add(TickMark("BC", 1));
        spec.Marks.Add(TickMark("DA", 1));

        // Measurements
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mCD", "CD"));
        spec.Measurements.Add(LengthMeasurement("mBC", "BC"));
        spec.Measurements.Add(AngleMeasurement("mAngA", "D", "A", "B"));
        spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));

        // Constraint: top parallel to bottom
        spec.Constraints.Add(new SceneConstraint { Type = ConstraintType.Parallel, Of = "CD", To = "AB" });

        return spec;
    }
}
