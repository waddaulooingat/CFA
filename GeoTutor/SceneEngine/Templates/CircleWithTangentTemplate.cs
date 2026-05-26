namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Circle with a tangent line at a specified point on the circumference.
/// Parameters:
///   radius               – double
///   tangentPointAngleDeg – double (angle from positive x-axis of the tangent point, degrees)
///
/// The tangent is perpendicular to the radius at the tangent point T.
/// A right-angle mark is shown at T.
/// </summary>
public sealed class CircleWithTangentTemplate : ISceneTemplate
{
    public string TemplateName => "CircleWithTangent";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double r = GetDouble(p, "radius");
        if (r <= 0) { error = "'radius' must be positive."; return false; }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double r        = GetDouble(p, "radius");
        double tAngDeg  = GetDouble(p, "tangentPointAngleDeg", 45);
        double tAngRad  = tAngDeg * Math.PI / 180.0;

        double ox = 0, oy = 0;

        // Tangent point T on the circle
        double tx = ox + r * Math.Cos(tAngRad);
        double ty = oy + r * Math.Sin(tAngRad);

        // Tangent line is perpendicular to the radius OT at T.
        // Radius direction: (cos θ, sin θ). Tangent direction: (-sin θ, cos θ).
        double tangDirX = -Math.Sin(tAngRad);
        double tangDirY =  Math.Cos(tAngRad);

        // Extend tangent line by 'ext' units in both directions from T
        double ext = r * 1.2;
        double t1x = tx - tangDirX * ext;
        double t1y = ty - tangDirY * ext;
        double t2x = tx + tangDirX * ext;
        double t2y = ty + tangDirY * ext;

        var spec = new SceneSpec { Id = "circle-with-tangent" };
        spec.Viewport = FitViewport(
            new[] { (ox - r, oy - r), (ox + r, oy + r), (t1x, t1y), (t2x, t2y) },
            margin: 1.0);

        // Points
        spec.Points.Add(Pt("O",  ox,  oy,  draggable: false));
        spec.Points.Add(Pt("T",  tx,  ty,  draggable: false));
        spec.Points.Add(Pt("T1", t1x, t1y, draggable: false));
        spec.Points.Add(Pt("T2", t2x, t2y, draggable: false));

        // Full circle
        spec.Arcs.Add(new SceneArc
        {
            Id            = "circle",
            Center        = "O",
            Radius        = r,
            StartAngleDeg = 0,
            SweepAngleDeg = 360
        });

        // Radius segment O → T
        spec.Segments.Add(Seg("radius", "O", "T"));

        // Tangent line T1 → T2
        spec.Segments.Add(Seg("tangent", "T1", "T2"));

        // Right-angle mark at T (radius ⊥ tangent)
        spec.Marks.Add(RightAngleMark("T"));

        // Labels
        spec.Labels.Add(Lbl("lO", "O", "O", -0.30, -0.25));
        spec.Labels.Add(Lbl("lT", "T", "T",  0.20,  0.20));

        // Theorem label
        spec.Labels.Add(new SceneLabel
        {
            Id         = "theorem",
            Text       = "Tangent ⊥ Radius at point of tangency",
            AnchorPoint = "O",
            OffsetX    = 0,
            OffsetY    = -(r + 0.6)
        });

        // Constraint: OT perpendicular to tangent at T
        spec.Constraints.Add(new SceneConstraint
        {
            Type = ConstraintType.Perpendicular,
            Of   = "radius",
            To   = "tangent"
        });

        return spec;
    }
}
