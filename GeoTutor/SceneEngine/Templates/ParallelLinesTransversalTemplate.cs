namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Two horizontal parallel lines with a transversal crossing both.
/// Parameters:
///   gap                 – double (vertical distance between the two lines)
///   transversalAngleDeg – double (angle of transversal from horizontal, 0–180 exclusive)
///   markedPair          – string (corresponding | alternate-interior | alternate-exterior | co-interior)
///
/// The lines run from x=-1 to x=lineLen; the transversal crosses both lines
/// near x=lineLen/2.
/// </summary>
public sealed class ParallelLinesTransversalTemplate : ISceneTemplate
{
    public string TemplateName => "ParallelLinesTransversal";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double gap = GetDouble(p, "gap");
        if (gap <= 0)
        {
            error = "'gap' must be positive.";
            return false;
        }
        double ang = GetDouble(p, "transversalAngleDeg", 60);
        if (ang <= 0 || ang >= 180)
        {
            error = "'transversalAngleDeg' must be strictly between 0 and 180.";
            return false;
        }
        var pair = GetString(p, "markedPair", "corresponding").ToLowerInvariant();
        if (pair is not ("corresponding" or "alternate-interior" or "alternate-exterior" or "co-interior"))
        {
            error = "markedPair must be: corresponding, alternate-interior, alternate-exterior, or co-interior.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double gap  = GetDouble(p, "gap", 4);
        double ang  = GetDouble(p, "transversalAngleDeg", 60);
        string pair = GetString(p, "markedPair", "corresponding").ToLowerInvariant();

        // Line layout: bottom line at y=0, top line at y=gap
        double lineLen = 10.0;
        double midX    = lineLen / 2.0;

        // Transversal crosses bottom line at (midX, 0), top line at intersection
        double rad     = ang * Math.PI / 180.0;
        double slope   = Math.Tan(rad); // dy/dx for transversal going upward-right
        // Extend transversal by 'ext' units beyond each intersection
        double ext     = 2.5;
        double dx      = ext * Math.Cos(rad);
        double dy      = ext * Math.Sin(rad);

        // Intersection with bottom line y=0: (midX, 0)
        double intBotX = midX;
        double intBotY = 0;

        // Intersection with top line y=gap: x shift = gap / slope (if slope != 0)
        double intTopX = Math.Abs(slope) > 1e-9 ? midX + gap / slope : midX;
        double intTopY = gap;

        // Transversal endpoints (extending past each intersection)
        double tvBotStartX = intBotX - dx;
        double tvBotStartY = intBotY - dy;
        double tvTopEndX   = intTopX + dx;
        double tvTopEndY   = intTopY + dy;

        var spec = new SceneSpec { Id = "parallel-transversal" };

        // ── Points ────────────────────────────────────────────────────────────
        // Bottom line endpoints
        spec.Points.Add(Pt("BL1", 0,       0,      draggable: false));
        spec.Points.Add(Pt("BL2", lineLen, 0,      draggable: false));
        // Top line endpoints
        spec.Points.Add(Pt("TL1", 0,       gap,    draggable: false));
        spec.Points.Add(Pt("TL2", lineLen, gap,    draggable: false));
        // Transversal endpoints
        spec.Points.Add(Pt("TV1", tvBotStartX, tvBotStartY, draggable: false));
        spec.Points.Add(Pt("TV2", tvTopEndX,   tvTopEndY,   draggable: false));
        // Intersection points
        spec.Points.Add(Pt("IB", intBotX, intBotY, draggable: false));
        spec.Points.Add(Pt("IT", intTopX, intTopY, draggable: false));

        // ── Segments ─────────────────────────────────────────────────────────
        spec.Segments.Add(Seg("bottomLine", "BL1", "BL2"));
        spec.Segments.Add(Seg("topLine",    "TL1", "TL2"));
        spec.Segments.Add(Seg("transversal","TV1", "TV2"));

        // ── Parallel arrows ───────────────────────────────────────────────────
        spec.Marks.Add(ParallelMark("bottomLine", 1));
        spec.Marks.Add(ParallelMark("topLine",    1));

        // ── Labels ────────────────────────────────────────────────────────────
        spec.Labels.Add(Lbl("lblBot", "l₁", "BL2",  0.3,  0.1));
        spec.Labels.Add(Lbl("lblTop", "l₂", "TL2",  0.3,  0.1));

        // ── Highlighted angle measurements ────────────────────────────────────
        // We describe which angle pairs to highlight/measure based on markedPair.
        // Angles at bottom intersection IB:
        //   above-right = transversal going up-right, bottom-line going right
        //   above-left  = transversal going up-left (i.e., from IB toward TV1), bottom-line going right
        // We use angle measurements from SceneMeasurement (vertex=IB or IT).

        // For the transversal direction at each intersection we need the nearest endpoint:
        // At IB: arm toward TV2 (upper) and arm toward TV1 (lower)
        // At IT: arm toward TV2 (upper) and arm toward TV1 (lower via IB direction)
        // For line arms: at IB use BL2 (right) and BL1 (left); at IT use TL2 (right) and TL1 (left)

        switch (pair)
        {
            case "corresponding":
                // Upper-right angle at bottom (TV2 / BL2) ↔ upper-right at top (TV2 / TL2)
                spec.Measurements.Add(AngleMeasurementHighlighted("mCor1", "TV2", "IB", "BL2", true));
                spec.Measurements.Add(AngleMeasurementHighlighted("mCor2", "TV2", "IT", "TL2", true));
                spec.Labels.Add(new SceneLabel { Id = "pairLbl", Text = "Corresponding Angles", OffsetX = 0, OffsetY = -1, AnchorPoint = "IB" });
                break;

            case "alternate-interior":
                // Upper-right at bottom (above bottom line) ↔ lower-left at top (below top line)
                spec.Measurements.Add(AngleMeasurementHighlighted("mAlt1", "TV2", "IB", "BL2", true));
                spec.Measurements.Add(AngleMeasurementHighlighted("mAlt2", "TV1", "IT", "TL1", true));
                spec.Labels.Add(new SceneLabel { Id = "pairLbl", Text = "Alternate Interior Angles", OffsetX = 0, OffsetY = -1, AnchorPoint = "IB" });
                break;

            case "alternate-exterior":
                // Lower-left at bottom (below bottom line) ↔ upper-right at top (above top line)
                spec.Measurements.Add(AngleMeasurementHighlighted("mAlt1", "TV1", "IB", "BL1", true));
                spec.Measurements.Add(AngleMeasurementHighlighted("mAlt2", "TV2", "IT", "TL2", true));
                spec.Labels.Add(new SceneLabel { Id = "pairLbl", Text = "Alternate Exterior Angles", OffsetX = 0, OffsetY = -1, AnchorPoint = "IB" });
                break;

            case "co-interior":
                // Upper-right at bottom + upper-left at top (both between lines, same side)
                spec.Measurements.Add(AngleMeasurementHighlighted("mCoInt1", "TV2", "IB", "BL2", true));
                spec.Measurements.Add(AngleMeasurementHighlighted("mCoInt2", "TL2", "IT", "TV2", true));
                spec.Labels.Add(new SceneLabel { Id = "pairLbl", Text = "Co-interior (Same-Side Interior) Angles", OffsetX = 0, OffsetY = -1, AnchorPoint = "IB" });
                break;
        }

        spec.Viewport = FitViewport(
            new[] { (0.0, -1.5), (lineLen, gap + 1.5), (tvBotStartX, tvBotStartY), (tvTopEndX, tvTopEndY) },
            margin: 1.5);

        return spec;
    }

    private static SceneMeasurement AngleMeasurementHighlighted(
        string id, string arm1, string vertex, string arm2, bool show)
        => new()
        {
            Id = id,
            Type = MeasurementType.Angle,
            Arm1 = arm1,
            Vertex = vertex,
            Arm2 = arm2,
            Show = show
        };
}
