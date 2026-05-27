namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Parallelogram ABCD.
/// Parameters:
///   base          – double (length of AB and DC)
///   slantSide     – double (length of AD and BC)
///   slantAngleDeg – double (interior angle at A, degrees)
///
/// Layout: A=(0,0), B=(base,0),
///         D=(slantSide*cos(angle), slantSide*sin(angle)),
///         C=B+D-A.
/// </summary>
public sealed class ParallelogramTemplate : ISceneTemplate
{
    public string TemplateName => "Parallelogram";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double b  = GetDouble(p, "base");
        double ss = GetDouble(p, "slantSide");
        double a  = GetDouble(p, "slantAngleDeg");
        if (b  <= 0) { error = "'base' must be positive.";          return false; }
        if (ss <= 0) { error = "'slantSide' must be positive.";     return false; }
        if (a  <= 0 || a >= 180) { error = "'slantAngleDeg' must be between 0° and 180°."; return false; }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double b   = GetDouble(p, "base");
        double ss  = GetDouble(p, "slantSide");
        double ang = GetDouble(p, "slantAngleDeg", 60);
        double rad = ang * Math.PI / 180.0;

        double ax = 0, ay = 0;
        double bx = b, by = 0;
        double dx = ss * Math.Cos(rad), dy = ss * Math.Sin(rad);
        double cx = bx + dx,            cy = by + dy;

        var spec = new SceneSpec { Id = "parallelogram" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy), (dx, dy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));
        spec.Points.Add(Pt("D", dx, dy));

        // Sides
        var segAB = Seg("AB", "A", "B", ticks: 1);
        var segDC = Seg("DC", "D", "C", ticks: 1);
        var segAD = Seg("AD", "A", "D", ticks: 2);
        var segBC = Seg("BC", "B", "C", ticks: 2);

        spec.Segments.Add(segAB);
        spec.Segments.Add(segBC);
        spec.Segments.Add(segDC);
        spec.Segments.Add(segAD);

        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.20,  0.20));
        spec.Labels.Add(Lbl("lD", "D", "D", -0.35,  0.20));

        // Parallel arrows: AB ∥ DC, AD ∥ BC
        spec.Marks.Add(ParallelMark("AB", 1));
        spec.Marks.Add(ParallelMark("DC", 1));
        spec.Marks.Add(ParallelMark("AD", 2));
        spec.Marks.Add(ParallelMark("BC", 2));

        // Congruence ticks on opposite sides
        spec.Marks.Add(TickMark("AB", 1));
        spec.Marks.Add(TickMark("DC", 1));
        spec.Marks.Add(TickMark("AD", 2));
        spec.Marks.Add(TickMark("BC", 2));

        // Measurements
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mAD", "AD"));
        spec.Measurements.Add(AngleMeasurement("mAngA", "D", "A", "B"));
        spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));

        // Constraints
        spec.Constraints.Add(new SceneConstraint { Type = ConstraintType.Parallel, Of = "DC", To = "AB" });
        spec.Constraints.Add(new SceneConstraint { Type = ConstraintType.Parallel, Of = "BC", To = "AD" });
        spec.Constraints.Add(new SceneConstraint { Type = ConstraintType.EqualLength, Of = "DC", To = "AB" });
        spec.Constraints.Add(new SceneConstraint { Type = ConstraintType.EqualLength, Of = "BC", To = "AD" });

        return spec;
    }
}
