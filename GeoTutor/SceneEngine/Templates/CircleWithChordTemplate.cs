namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Circle with a chord and the perpendicular bisector from the center to the chord.
/// Parameters:
///   radius             – double
///   chordLengthFraction – double (0 &lt; f &lt; 2, chord = f * radius; must be &lt; diameter)
///
/// Layout: center O at origin. Chord is horizontal, symmetrical about the y-axis.
/// </summary>
public sealed class CircleWithChordTemplate : ISceneTemplate
{
    public string TemplateName => "CircleWithChord";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double r = GetDouble(p, "radius");
        if (r <= 0) { error = "'radius' must be positive."; return false; }

        double f = GetDouble(p, "chordLengthFraction", 1.0);
        if (f <= 0 || f >= 2)
        {
            error = "'chordLengthFraction' must be in (0, 2) — it is a multiple of the radius.";
            return false;
        }
        // chord half-length = f*r/2; must be < r
        double halfChord = f * r / 2.0;
        if (halfChord >= r)
        {
            error = $"Chord half-length ({halfChord}) must be strictly less than radius ({r}).";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double r = GetDouble(p, "radius");
        double f = GetDouble(p, "chordLengthFraction", 1.0);

        double halfChord = f * r / 2.0;

        // Height from center to chord (positive, chord below center)
        double h = Math.Sqrt(r * r - halfChord * halfChord);

        // Center O at origin
        double ox = 0, oy = 0;

        // Chord endpoints: A and B are symmetric about y-axis at y = -h
        double ax = -halfChord, ay = -h;
        double bx =  halfChord, by = -h;

        // Midpoint M of chord (foot of perpendicular from O)
        double mx = 0, my = -h;

        var spec = new SceneSpec { Id = "circle-with-chord" };
        spec.Viewport = FitViewport(new[] { (ox - r, oy - r), (ox + r, oy + r) });

        // Points
        spec.Points.Add(Pt("O", ox, oy, draggable: false));
        spec.Points.Add(Pt("A", ax, ay, draggable: true));
        spec.Points.Add(Pt("B", bx, by, draggable: true));
        spec.Points.Add(Pt("M", mx, my, draggable: false, computed: true));

        // Full circle
        spec.Arcs.Add(new SceneArc
        {
            Id            = "circle",
            Center        = "O",
            Radius        = r,
            StartAngleDeg = 0,
            SweepAngleDeg = 360
        });

        // Chord AB
        spec.Segments.Add(Seg("chord", "A", "B"));

        // Perpendicular bisector from O to M
        spec.Segments.Add(Seg("perpBisector", "O", "M"));

        // Right-angle mark at M
        spec.Marks.Add(RightAngleMark("M"));

        // Labels
        spec.Labels.Add(Lbl("lO", "O", "O", -0.30,  0.20));
        spec.Labels.Add(Lbl("lA", "A", "A", -0.30, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lM", "M", "M",  0.20,  0.10));

        // Measurements
        spec.Measurements.Add(LengthMeasurement("mChord",    "chord"));
        spec.Measurements.Add(LengthMeasurement("mPerp",     "perpBisector"));

        // Constraint: M is midpoint of A and B
        spec.Constraints.Add(new SceneConstraint
        {
            Type    = ConstraintType.Midpoint,
            Point   = "M",
            Of      = "A",
            To      = "B"
        });

        // Constraint: OM perpendicular to AB
        spec.Constraints.Add(new SceneConstraint
        {
            Type = ConstraintType.Perpendicular,
            Of   = "perpBisector",
            To   = "chord"
        });

        return spec;
    }
}
