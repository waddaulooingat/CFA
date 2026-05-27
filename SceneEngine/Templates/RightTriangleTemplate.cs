namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Right triangle with the right angle at B.
/// Parameters:
///   legA         – double (horizontal leg length, A→B)
///   legB         – double (vertical leg length, B→C)
///   rotationDeg  – double (rotate the whole figure, default 0)
/// Unrotated layout: A=(0,0), B=(legA,0), C=(legA,legB).
/// </summary>
public sealed class RightTriangleTemplate : ISceneTemplate
{
    public string TemplateName => "RightTriangle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double la = GetDouble(p, "legA");
        double lb = GetDouble(p, "legB");
        if (la <= 0 || lb <= 0)
        {
            error = "legA and legB must both be positive.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double la  = GetDouble(p, "legA");
        double lb  = GetDouble(p, "legB");
        double rot = GetDouble(p, "rotationDeg", 0);

        // Base positions (right angle at B)
        (double ax, double ay) = (0, 0);
        (double bx, double by) = (la, 0);
        (double cx, double cy) = (la, lb);

        // Hypotenuse length (for measurement)
        double hyp = Math.Sqrt(la * la + lb * lb);

        // Apply rotation around origin
        if (rot != 0)
        {
            (ax, ay) = Rotate(ax, ay, rot);
            (bx, by) = Rotate(bx, by, rot);
            (cx, cy) = Rotate(cx, cy, rot);
        }

        var spec = new SceneSpec { Id = "right-triangle" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));

        spec.Segments.Add(Seg("AB", "A", "B")); // leg a
        spec.Segments.Add(Seg("BC", "B", "C")); // leg b
        spec.Segments.Add(Seg("CA", "C", "A")); // hypotenuse

        spec.Labels.Add(Lbl("lA", "A", "A", -0.3, -0.3));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.2, -0.3));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.2,  0.2));

        // Right-angle mark at B
        spec.Marks.Add(RightAngleMark("B"));

        // Length measurements for all three sides
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mBC", "BC"));
        spec.Measurements.Add(LengthMeasurement("mCA", "CA"));

        return spec;
    }
}
