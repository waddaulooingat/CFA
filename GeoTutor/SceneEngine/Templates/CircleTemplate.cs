namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Full circle with center O labeled.
/// Parameters:
///   radius  – double
///   centerX – double (default 0)
///   centerY – double (default 0)
/// </summary>
public sealed class CircleTemplate : ISceneTemplate
{
    public string TemplateName => "Circle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double r = GetDouble(p, "radius");
        if (r <= 0)
        {
            error = "'radius' must be positive.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double r  = GetDouble(p, "radius");
        double cx = GetDouble(p, "centerX", 0);
        double cy = GetDouble(p, "centerY", 0);

        var spec = new SceneSpec { Id = "circle" };
        spec.Viewport = FitViewport(
            new[] { (cx - r, cy - r), (cx + r, cy + r) });

        // Center point
        spec.Points.Add(Pt("O", cx, cy, draggable: false));

        // A point on the circle (rightmost) for radius segment
        double px = cx + r;
        double py = cy;
        spec.Points.Add(Pt("P", px, py, draggable: true));

        // Full circle arc
        spec.Arcs.Add(new SceneArc
        {
            Id            = "circle",
            Center        = "O",
            Radius        = r,
            StartAngleDeg = 0,
            SweepAngleDeg = 360
        });

        // Radius segment from O to P
        spec.Segments.Add(Seg("radius", "O", "P"));

        // Labels
        spec.Labels.Add(Lbl("lO", "O", "O", -0.30, -0.25));
        spec.Labels.Add(Lbl("lP", "P", "P",  0.20,  0.10));
        spec.Labels.Add(Lbl("lR", "r", "O",  (r / 2.0) / r * 0.5, 0.15));

        // Radius measurement
        spec.Measurements.Add(LengthMeasurement("mRadius", "radius"));

        // Constraint: P must stay on the circle
        spec.Constraints.Add(new SceneConstraint
        {
            Type   = ConstraintType.OnCircle,
            Point  = "P",
            Circle = "O",
            Value  = r
        });

        return spec;
    }
}
