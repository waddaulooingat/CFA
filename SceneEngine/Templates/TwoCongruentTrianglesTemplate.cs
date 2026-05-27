namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Two congruent triangles side by side, marking the congruent parts
/// appropriate for the chosen congruence postulate.
/// Parameters:
///   sharedSide      – double (length of the "matching" measurement)
///   congruenceType  – string (SSS | SAS | ASA | AAS | HL)
///
/// Layout: △ABC on the left, △DEF on the right, separated by a gap.
/// </summary>
public sealed class TwoCongruentTrianglesTemplate : ISceneTemplate
{
    public string TemplateName => "TwoCongruentTriangles";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        double s = GetDouble(p, "sharedSide");
        if (s <= 0)
        {
            error = "'sharedSide' must be positive.";
            return false;
        }
        var ct = GetString(p, "congruenceType", "SSS").ToUpperInvariant();
        if (ct is not ("SSS" or "SAS" or "ASA" or "AAS" or "HL"))
        {
            error = "congruenceType must be SSS, SAS, ASA, AAS, or HL.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        double s  = GetDouble(p, "sharedSide");
        string ct = GetString(p, "congruenceType", "SSS").ToUpperInvariant();

        // Build a fixed-shape triangle with deterministic proportions
        // △ABC: A=(0,0), B=(s,0), C=(s*0.4, s*0.75)
        // △DEF: shifted right by (s + gap)
        double gap  = s * 0.6;
        double offX = s + gap;

        double ax = 0,           ay = 0;
        double bx = s,           by = 0;
        double cx = s * 0.4,     cy = s * 0.75;

        double dx = offX,        dy = 0;
        double ex = offX + s,    ey = 0;
        double fx = offX + s * 0.4, fy = s * 0.75;

        var spec = new SceneSpec { Id = "two-congruent-triangles" };
        spec.Viewport = FitViewport(
            new[] { (ax, ay), (bx, by), (cx, cy), (dx, dy), (ex, ey), (fx, fy) });

        // Points
        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));
        spec.Points.Add(Pt("D", dx, dy));
        spec.Points.Add(Pt("E", ex, ey));
        spec.Points.Add(Pt("F", fx, fy));

        // Segments for △ABC
        spec.Segments.Add(Seg("AB", "A", "B"));
        spec.Segments.Add(Seg("BC", "B", "C"));
        spec.Segments.Add(Seg("CA", "C", "A"));

        // Segments for △DEF
        spec.Segments.Add(Seg("DE", "D", "E"));
        spec.Segments.Add(Seg("EF", "E", "F"));
        spec.Segments.Add(Seg("FD", "F", "D"));

        // Labels
        spec.Labels.Add(Lbl("lA", "A", "A", -0.35, -0.25));
        spec.Labels.Add(Lbl("lB", "B", "B",  0.20, -0.25));
        spec.Labels.Add(Lbl("lC", "C", "C",  0.00,  0.30));
        spec.Labels.Add(Lbl("lD", "D", "D", -0.35, -0.25));
        spec.Labels.Add(Lbl("lE", "E", "E",  0.20, -0.25));
        spec.Labels.Add(Lbl("lF", "F", "F",  0.00,  0.30));

        // Congruence statement label
        spec.Labels.Add(new SceneLabel
        {
            Id = "congruenceStmt",
            Text = $"△ABC ≅ △DEF  ({ct})",
            AnchorPoint = "C",
            OffsetX = s * 0.5,
            OffsetY = 0.6
        });

        // Mark the congruent parts based on the postulate
        MarkCongruentParts(spec, ct);

        // Measurements for the marked sides
        spec.Measurements.Add(LengthMeasurement("mAB", "AB"));
        spec.Measurements.Add(LengthMeasurement("mDE", "DE"));

        return spec;
    }

    private static void MarkCongruentParts(SceneSpec spec, string ct)
    {
        switch (ct)
        {
            case "SSS":
                // All three pairs of sides congruent
                spec.Segments.First(s => s.Id == "AB").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "DE").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "BC").CongruenceTicks = 2;
                spec.Segments.First(s => s.Id == "EF").CongruenceTicks = 2;
                spec.Segments.First(s => s.Id == "CA").CongruenceTicks = 3;
                spec.Segments.First(s => s.Id == "FD").CongruenceTicks = 3;
                spec.Marks.Add(TickMark("AB", 1)); spec.Marks.Add(TickMark("DE", 1));
                spec.Marks.Add(TickMark("BC", 2)); spec.Marks.Add(TickMark("EF", 2));
                spec.Marks.Add(TickMark("CA", 3)); spec.Marks.Add(TickMark("FD", 3));
                break;

            case "SAS":
                // Two sides (AB=DE, CA=FD) and the included angle at A=D
                spec.Segments.First(s => s.Id == "AB").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "DE").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "CA").CongruenceTicks = 2;
                spec.Segments.First(s => s.Id == "FD").CongruenceTicks = 2;
                spec.Marks.Add(TickMark("AB", 1)); spec.Marks.Add(TickMark("DE", 1));
                spec.Marks.Add(TickMark("CA", 2)); spec.Marks.Add(TickMark("FD", 2));
                // Included angle marks at A and D
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "D", GroupIndex = 1 });
                spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
                spec.Measurements.Add(AngleMeasurement("mAngD", "F", "D", "E"));
                break;

            case "ASA":
                // One side (AB=DE) and the two angles at its endpoints
                spec.Segments.First(s => s.Id == "AB").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "DE").CongruenceTicks = 1;
                spec.Marks.Add(TickMark("AB", 1)); spec.Marks.Add(TickMark("DE", 1));
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "D", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 2 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "E", GroupIndex = 2 });
                spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
                spec.Measurements.Add(AngleMeasurement("mAngD", "F", "D", "E"));
                spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
                spec.Measurements.Add(AngleMeasurement("mAngE", "D", "E", "F"));
                break;

            case "AAS":
                // Two angles (at A=D and B=E) and the non-included side BC=EF
                spec.Segments.First(s => s.Id == "BC").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "EF").CongruenceTicks = 1;
                spec.Marks.Add(TickMark("BC", 1)); spec.Marks.Add(TickMark("EF", 1));
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "D", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 2 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "E", GroupIndex = 2 });
                spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
                spec.Measurements.Add(AngleMeasurement("mAngD", "F", "D", "E"));
                spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
                spec.Measurements.Add(AngleMeasurement("mAngE", "D", "E", "F"));
                break;

            case "HL":
                // Hypotenuse-Leg: right angle at B and E, hypotenuse CA=FD, leg AB=DE
                spec.Marks.Add(RightAngleMark("B"));
                spec.Marks.Add(RightAngleMark("E"));
                spec.Segments.First(s => s.Id == "CA").CongruenceTicks = 1; // hypotenuse
                spec.Segments.First(s => s.Id == "FD").CongruenceTicks = 1;
                spec.Segments.First(s => s.Id == "AB").CongruenceTicks = 2; // leg
                spec.Segments.First(s => s.Id == "DE").CongruenceTicks = 2;
                spec.Marks.Add(TickMark("CA", 1)); spec.Marks.Add(TickMark("FD", 1));
                spec.Marks.Add(TickMark("AB", 2)); spec.Marks.Add(TickMark("DE", 2));
                break;
        }
    }
}
