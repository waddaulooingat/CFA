namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Isosceles triangle: A at origin, B at (base,0), C at apex.
/// Parameters:
///   base       – double  (length of the base AB)
///   legLength  – double  (length of each equal leg AC = BC)
/// </summary>
public sealed class IsoscelesTriangleTemplate : ISceneTemplate
{
    public string TemplateName => "IsoscelesTriangle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double b  = GetDouble(p, "base");
        double lg = GetDouble(p, "legLength");
        if (b <= 0)  { error = "'base' must be positive."; return false; }
        if (lg <= 0) { error = "'legLength' must be positive."; return false; }
        // Triangle inequality: each leg > base/2
        if (lg <= b / 2)
        {
            error = $"legLength ({lg}) must be greater than base/2 ({b / 2}) to form a valid triangle.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double b  = GetDouble(p, "base");
        double lg = GetDouble(p, "legLength");

        // Apex y-coordinate via Pythagoras: h² = leg² − (base/2)²
        double h = Math.Sqrt(lg * lg - (b / 2) * (b / 2));

        double ax = 0,     ay = 0;
        double bx = b,     by = 0;
        double cx = b / 2, cy = h;  // apex C is on the axis of symmetry

        var spec = new SceneSpec { Id = "isosceles-triangle" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));

        // AC and BC are the equal legs; AB is the base
        var segAC = Seg("AC", "A", "C", ticks: 1);
        var segBC = Seg("BC", "B", "C", ticks: 1);
        var segAB = Seg("AB", "A", "B", ticks: 0);

        spec.Segments.Add(segAC);
        spec.Segments.Add(segBC);
        spec.Segments.Add(segAB);

        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.00,  0.30));

        // Congruence tick marks on the two equal legs
        spec.Marks.Add(TickMark("AC", 1));
        spec.Marks.Add(TickMark("BC", 1));

        // Angle marks at the base: angle A == angle B
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 2 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 2 });

        // Measurements
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mAC", "AC"));
        spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
        spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
        spec.Measurements.Add(AngleMeasurement("mAngC", "A", "C", "B"));

        return spec;
    }
}
