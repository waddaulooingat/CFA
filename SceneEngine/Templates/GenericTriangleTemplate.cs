namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Draws triangle ABC from three explicit vertex coordinates.
/// Parameters:
///   vertexA  – {x,y}
///   vertexB  – {x,y}
///   vertexC  – {x,y}
///   classification – "scalene" | "isosceles" | "equilateral"
/// </summary>
public sealed class GenericTriangleTemplate : ISceneTemplate
{
    public string TemplateName => "GenericTriangle";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        foreach (var key in new[] { "vertexA", "vertexB", "vertexC" })
        {
            if (!p.ContainsKey(key))
            {
                error = $"Missing required parameter '{key}'.";
                return false;
            }
        }
        var cls = GetString(p, "classification", "scalene").ToLowerInvariant();
        if (cls is not ("scalene" or "isosceles" or "equilateral"))
        {
            error = "classification must be scalene, isosceles, or equilateral.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        var (ax, ay) = GetPoint(p, "vertexA");
        var (bx, by) = GetPoint(p, "vertexB");
        var (cx, cy) = GetPoint(p, "vertexC");
        var cls = GetString(p, "classification", "scalene").ToLowerInvariant();

        var spec = new SceneSpec { Id = "generic-triangle" };
        spec.Viewport = FitViewport(new[] { (ax, ay), (bx, by), (cx, cy) });

        spec.Points.Add(Pt("A", ax, ay));
        spec.Points.Add(Pt("B", bx, by));
        spec.Points.Add(Pt("C", cx, cy));

        spec.Segments.Add(Seg("AB", "A", "B"));
        spec.Segments.Add(Seg("BC", "B", "C"));
        spec.Segments.Add(Seg("CA", "C", "A"));

        spec.Labels.Add(Lbl("lA", "A", "A", -0.3, 0.1));
        spec.Labels.Add(Lbl("lB", "B", "B", 0.1, -0.3));
        spec.Labels.Add(Lbl("lC", "C", "C", 0.1, 0.1));

        switch (cls)
        {
            case "equilateral":
                // All three sides congruent — one tick group
                spec.Segments[0].CongruenceTicks = 1;
                spec.Segments[1].CongruenceTicks = 1;
                spec.Segments[2].CongruenceTicks = 1;
                // Angle marks at every vertex
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "A", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 1 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "C", GroupIndex = 1 });
                spec.Measurements.Add(AngleMeasurement("mAngA", "C", "A", "B"));
                spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
                spec.Measurements.Add(AngleMeasurement("mAngC", "B", "C", "A"));
                break;

            case "isosceles":
                // AB == CA (legs), BC is base
                spec.Segments[0].CongruenceTicks = 1; // AB
                spec.Segments[2].CongruenceTicks = 1; // CA
                // Base angles equal
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "B", GroupIndex = 2 });
                spec.Marks.Add(new SceneMark { Type = MarkType.CongruenceTick, At = "C", GroupIndex = 2 });
                spec.Measurements.Add(AngleMeasurement("mAngB", "A", "B", "C"));
                spec.Measurements.Add(AngleMeasurement("mAngC", "B", "C", "A"));
                break;

            default: // scalene — no special marks, just labels
                break;
        }

        return spec;
    }
}
