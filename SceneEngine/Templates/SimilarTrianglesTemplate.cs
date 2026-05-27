namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Two similar triangles: △ABC (large) and △DEF (small), scaled by scaleFactor.
/// Parameters:
///   scaleFactor – double (ratio DEF/ABC, e.g. 0.5)
///   orientation – string (same | flipped)
///
/// △ABC is on the left; △DEF is on the right (flipped = reflected about a vertical axis).
/// </summary>
public sealed class SimilarTrianglesTemplate : ISceneTemplate
{
    public string TemplateName => "SimilarTriangles";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double sf = GetDouble(p, "scaleFactor");
        if (sf <= 0 || sf >= 10)
        {
            error = "'scaleFactor' must be positive and less than 10.";
            return false;
        }
        var ori = GetString(p, "orientation", "same").ToLowerInvariant();
        if (ori is not ("same" or "flipped"))
        {
            error = "orientation must be 'same' or 'flipped'.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double sf  = GetDouble(p, "scaleFactor", 0.5);
        string ori = GetString(p, "orientation", "same").ToLowerInvariant();

        // Large triangle △ABC: A=(0,0), B=(6,0), C=(2.4, 4.5)
        double baseLen = 6.0;
        double ax = 0,            ay = 0;
        double bx = baseLen,      by = 0;
        double cx = baseLen * 0.4, cy = baseLen * 0.75;

        // Small triangle △DEF, scaled from same base shape
        double sDx = cx + 1.5;   // horizontal offset for DEF
        double sby  = 0;

        double smallBase = baseLen * sf;
        double dx, dy, ex, ey, fx, fy;

        if (ori == "flipped")
        {
            // Reflect about a vertical axis (mirror the x-coords of the scaled shape)
            dx = sDx + smallBase;
            dy = sby;
            ex = sDx;
            ey = sby;
            fx = sDx + smallBase * (1 - 0.4);
            fy = smallBase * 0.75;
        }
        else
        {
            dx = sDx;
            dy = sby;
            ex = sDx + smallBase;
            ey = sby;
            fx = sDx + smallBase * 0.4;
            fy = smallBase * 0.75;
        }

        var spec = new SceneSpec { Id = "similar-triangles" };
        spec.Viewport = FitViewport(
            new[] { (ax, ay), (bx, by), (cx, cy), (dx, dy), (ex, ey), (fx, fy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));
        spec.Points.Add(Pt("D", dx, dy));
        spec.Points.Add(Pt("E", ex, ey));
        spec.Points.Add(Pt("F", fx, fy));

        spec.Segments.Add(Seg("AB", "A", "B"));
        spec.Segments.Add(Seg("BC", "B", "C"));
        spec.Segments.Add(Seg("CA", "C", "A"));
        spec.Segments.Add(Seg("DE", "D", "E"));
        spec.Segments.Add(Seg("EF", "E", "F"));
        spec.Segments.Add(Seg("FD", "F", "D"));

        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.00,  0.30));
        spec.Labels.Add(Lbl("lD", "D", "D", -0.35, -0.25));
        spec.Labels.Add(Lbl("lE", "E", "E",  0.20, -0.25));
        spec.Labels.Add(Lbl("lF", "F", "F",  0.00,  0.30));

        // Similarity statement
        spec.Labels.Add(new SceneLabel
        {
            Id = "simStmt",
            Text = $"△ABC ~ △DEF  (k = {sf:G4})",
            AnchorPoint = "C",
            OffsetX = 0,
            OffsetY = 0.7
        });

        // Equal angle marks at corresponding vertices (AA similarity)
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "D", GroupIndex = 1 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 2 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "E", GroupIndex = 2 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "C", GroupIndex = 3 });
        spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "F", GroupIndex = 3 });

        // Proportion measurements — show all six sides
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mBC", "BC"));
        spec.Measurements.Add(LengthMeasurement("mCA", "CA"));
        spec.Measurements.Add(LengthMeasurement("mDE", "DE"));
        spec.Measurements.Add(LengthMeasurement("mEF", "EF"));
        spec.Measurements.Add(LengthMeasurement("mFD", "FD"));

        // Show angle equality
        spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
        spec.Measurements.Add(AngleMeasurement("mAngD", "F", "D", "E"));

        return spec;
    }
}
