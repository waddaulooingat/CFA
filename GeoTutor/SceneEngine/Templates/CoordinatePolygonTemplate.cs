namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Polygon with user-supplied vertices on a coordinate grid.
/// Parameters:
///   vertices – list of {x,y}  (minimum 3 vertices)
///
/// Draws the polygon, labels each vertex A, B, C, …, and adds
/// integer-interval axis tick labels on both axes.
/// </summary>
public sealed class CoordinatePolygonTemplate : ISceneTemplate
{
    public string TemplateName => "CoordinatePolygon";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        if (!p.ContainsKey("vertices"))
        {
            error = "Missing required parameter 'vertices'.";
            return false;
        }
        List<(double, double)> verts;
        try
        {
            verts = GetPointList(p, "vertices");
        }
        catch (Exception ex)
        {
            error = $"Could not parse 'vertices': {ex.Message}";
            return false;
        }
        if (verts.Count < 3)
        {
            error = "At least 3 vertices are required to form a polygon.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        var verts = GetPointList(p, "vertices");
        int n     = verts.Count;

        var spec = new SceneSpec { Id = "coordinate-polygon" };

        // Fit viewport with extra room for axis labels
        spec.Viewport = FitViewport(verts, margin: 2.5);

        // Add vertices with letter names
        for (int i = 0; i < n; i++)
        {
            string name = VertexName(i);
            var (vx, vy) = verts[i];
            spec.Points.Add(Pt(name, vx, vy));
            spec.Labels.Add(Lbl($"l{name}", name, name, 0.20, 0.20));
        }

        // Polygon edges (closed)
        for (int i = 0; i < n; i++)
        {
            string from = VertexName(i);
            string to   = VertexName((i + 1) % n);
            spec.Segments.Add(Seg($"edge{i}", from, to));
        }

        // Coordinate axis grid labels at integer intervals
        AddAxisLabels(spec, spec.Viewport);

        // Length measurements for each edge
        for (int i = 0; i < n; i++)
            spec.Measurements.Add(LengthMeasurement($"mEdge{i}", $"edge{i}"));

        return spec;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string VertexName(int index)
    {
        // A, B, C, … Z, then AA, AB, …
        if (index < 26) return ((char)('A' + index)).ToString();
        return ((char)('A' + index / 26 - 1)).ToString()
             + ((char)('A' + index % 26)).ToString();
    }

    private static void AddAxisLabels(SceneSpec spec, Viewport vp)
    {
        // X-axis tick labels
        int xMin = (int)Math.Ceiling(vp.XMin);
        int xMax = (int)Math.Floor(vp.XMax);
        for (int x = xMin; x <= xMax; x++)
        {
            if (x == 0) continue; // skip origin for clarity
            string id = $"xTick{x}";
            // Place a non-draggable ghost point on the x-axis for anchoring the label
            spec.Points.Add(new ScenePoint { Id = id, X = x, Y = 0, Draggable = false });
            spec.Labels.Add(new SceneLabel
            {
                Id          = $"lxTick{x}",
                Text        = x.ToString(),
                AnchorPoint = id,
                OffsetX     = 0,
                OffsetY     = -0.35
            });
        }

        // Y-axis tick labels
        int yMin = (int)Math.Ceiling(vp.YMin);
        int yMax = (int)Math.Floor(vp.YMax);
        for (int y = yMin; y <= yMax; y++)
        {
            if (y == 0) continue;
            string id = $"yTick{y}";
            spec.Points.Add(new ScenePoint { Id = id, X = 0, Y = y, Draggable = false });
            spec.Labels.Add(new SceneLabel
            {
                Id          = $"lyTick{y}",
                Text        = y.ToString(),
                AnchorPoint = id,
                OffsetX     = -0.40,
                OffsetY     = 0
            });
        }

        // Origin label
        spec.Points.Add(new ScenePoint { Id = "origin", X = 0, Y = 0, Draggable = false });
        spec.Labels.Add(new SceneLabel
        {
            Id          = "lOrigin",
            Text        = "O",
            AnchorPoint = "origin",
            OffsetX     = -0.30,
            OffsetY     = -0.30
        });
    }
}
