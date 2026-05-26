namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Circle with an inscribed angle BAC and the corresponding central angle BOC.
/// Parameters:
///   radius  – double
///   arcDeg  – double (measure of arc BC, so central angle BOC = arcDeg,
///                     inscribed angle BAC = arcDeg / 2)
///
/// Layout:
///   O at origin.
///   B at angle 90° + arcDeg/2 from positive x-axis.
///   C at angle 90° − arcDeg/2.
///   A at the bottom of the circle (angle 270°) to make a nice inscribed angle.
/// </summary>
public sealed class CircleWithInscribedAngleTemplate : ISceneTemplate
{
    public string TemplateName => "CircleWithInscribedAngle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double r = GetDouble(p, "radius");
        if (r <= 0) { error = "'radius' must be positive."; return false; }

        double arc = GetDouble(p, "arcDeg");
        if (arc <= 0 || arc >= 360)
        {
            error = "'arcDeg' must be between 0 and 360 (exclusive).";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double r      = GetDouble(p, "radius");
        double arcDeg = GetDouble(p, "arcDeg", 120);

        double ox = 0, oy = 0;

        // Place B and C symmetrically at the top of the circle
        double halfArc = arcDeg / 2.0;
        double bAngle  = 90.0 + halfArc;  // degrees from +x axis
        double cAngle  = 90.0 - halfArc;

        // A is at the bottom of the circle — opposite side from arc BC
        double aAngle = 270.0;

        double bx = ox + r * Math.Cos(bAngle * Math.PI / 180.0);
        double by = oy + r * Math.Sin(bAngle * Math.PI / 180.0);
        double cx = ox + r * Math.Cos(cAngle * Math.PI / 180.0);
        double cy = oy + r * Math.Sin(cAngle * Math.PI / 180.0);
        double ax = ox + r * Math.Cos(aAngle * Math.PI / 180.0);
        double ay = oy + r * Math.Sin(aAngle * Math.PI / 180.0);

        var spec = new SceneSpec { Id = "circle-inscribed-angle" };
        spec.Viewport = FitViewport(new[] { (ox - r, oy - r), (ox + r, oy + r) });

        // Points
        spec.Points.Add(Pt("O", ox, oy, draggable: false));
        spec.Points.Add(Pt("A", ax, ay, draggable: true));
        spec.Points.Add(Pt("B", bx, by, draggable: true));
        spec.Points.Add(Pt("C", cx, cy, draggable: true));

        // Full circle
        spec.Arcs.Add(new SceneArc
        {
            Id            = "circle",
            Center        = "O",
            Radius        = r,
            StartAngleDeg = 0,
            SweepAngleDeg = 360
        });

        // Arc BC (the minor arc, highlighted) — sweep from cAngle to bAngle
        double arcStart = cAngle;
        double arcSweep = arcDeg;
        spec.Arcs.Add(new SceneArc
        {
            Id            = "arcBC",
            Center        = "O",
            Radius        = r,
            StartAngleDeg = arcStart,
            SweepAngleDeg = arcSweep,
            Highlighted   = true
        });

        // Inscribed angle sides: A→B and A→C
        spec.Segments.Add(Seg("AB", "A", "B"));
        spec.Segments.Add(Seg("AC", "A", "C"));

        // Central angle sides: O→B and O→C
        spec.Segments.Add(Seg("OB", "O", "B"));
        spec.Segments.Add(Seg("OC", "O", "C"));

        // Labels
        spec.Labels.Add(Lbl("lO", "O", "O", -0.30,  0.20));
        spec.Labels.Add(Lbl("lA", "A", "A",  0.00, -0.35));
        spec.Labels.Add(Lbl("lB", "B", "B", -0.30,  0.20));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.20,  0.20));

        // Angle measurements
        spec.Measurements.Add(AngleMeasurement("mInscribed", "B", "A", "C"));
        spec.Measurements.Add(AngleMeasurement("mCentral",   "B", "O", "C"));

        // Inscribed angle theorem label
        spec.Labels.Add(new SceneLabel
        {
            Id         = "theorem",
            Text       = $"∠BAC = {arcDeg / 2:G4}°  (inscribed)   ∠BOC = {arcDeg:G4}°  (central)",
            AnchorPoint = "O",
            OffsetX    = 0,
            OffsetY    = -(r + 0.7)
        });

        return spec;
    }
}
