namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using static GeoTutor.SceneEngine.Templates.TemplateHelpers;

/// <summary>
/// Pre-image (blue) and image (green) of a shape under a transformation.
/// Parameters:
///   shapeType   – string (triangle | rectangle)
///   transform   – string (rotation90 | reflection-x | reflection-y | dilation)
///   scaleFactor – double (used only when transform = "dilation", default 2)
///
/// The pre-image vertices are labeled A, B, C[, D]; image vertices A′, B′, C′[, D′].
/// Highlighting: pre-image Highlighted=false, image Highlighted=true.
/// </summary>
public sealed class TransformationPairTemplate : ISceneTemplate
{
    public string TemplateName => "TransformationPair";

    public bool ValidateParams(Dictionary<string, object> p, out string error)
    {
        var st = GetString(p, "shapeType", "triangle").ToLowerInvariant();
        if (st is not ("triangle" or "rectangle"))
        {
            error = "shapeType must be 'triangle' or 'rectangle'.";
            return false;
        }
        var tf = GetString(p, "transform", "rotation90").ToLowerInvariant();
        if (tf is not ("rotation90" or "reflection-x" or "reflection-y" or "dilation"))
        {
            error = "transform must be rotation90, reflection-x, reflection-y, or dilation.";
            return false;
        }
        if (tf == "dilation")
        {
            double sf = GetDouble(p, "scaleFactor", 2);
            if (sf <= 0) { error = "'scaleFactor' must be positive for dilation."; return false; }
        }
        error = string.Empty;
        return true;
    }

    public SceneSpec Build(Dictionary<string, object> p)
    {
        string shapeType = GetString(p, "shapeType", "triangle").ToLowerInvariant();
        string transform = GetString(p, "transform",  "rotation90").ToLowerInvariant();
        double sf        = GetDouble(p, "scaleFactor", 2);

        // Define pre-image vertices
        List<(double x, double y, string name)> preVerts = shapeType == "rectangle"
            ? new() { (1, 1, "A"), (4, 1, "B"), (4, 3, "C"), (1, 3, "D") }
            : new() { (1, 1, "A"), (4, 1, "B"), (2.5, 4, "C") };

        // Compute image vertices
        var imgVerts = preVerts
            .Select(v =>
            {
                var (ix, iy) = ApplyTransform(v.x, v.y, transform, sf);
                return (ix, iy, v.name + "′");
            })
            .ToList();

        // Collect all points for viewport
        var allPts = preVerts.Select(v => (v.x, v.y))
            .Concat(imgVerts.Select(v => (v.Item1, v.Item2)))
            .ToList();

        var spec = new SceneSpec { Id = "transformation-pair" };
        spec.Viewport = FitViewport(allPts);

        // Pre-image points (not highlighted = "original/blue")
        foreach (var (px, py, pname) in preVerts)
        {
            spec.Points.Add(new ScenePoint
                { Id = pname, X = px, Y = py, Draggable = true, Highlighted = false });
            spec.Labels.Add(Lbl($"l{pname}", pname, pname, -0.30, -0.25));
        }

        // Image points (highlighted = "transformed/green")
        foreach (var (ix, iy, iname) in imgVerts)
        {
            spec.Points.Add(new ScenePoint
                { Id = iname, X = ix, Y = iy, Draggable = false, Highlighted = true });
            spec.Labels.Add(Lbl($"l{iname}", iname, iname, 0.20, 0.20));
        }

        // Pre-image edges
        int n = preVerts.Count;
        for (int i = 0; i < n; i++)
        {
            string from = preVerts[i].name;
            string to   = preVerts[(i + 1) % n].name;
            spec.Segments.Add(new SceneSegment
                { Id = $"pre{i}", From = from, To = to, Highlighted = false });
        }

        // Image edges
        for (int i = 0; i < n; i++)
        {
            string from = imgVerts[i].Item3;
            string to   = imgVerts[(i + 1) % n].Item3;
            spec.Segments.Add(new SceneSegment
                { Id = $"img{i}", From = from, To = to, Highlighted = true });
        }

        // Transformation description label
        string desc = transform switch
        {
            "rotation90"   => "Rotation 90° CCW about origin",
            "reflection-x" => "Reflection over the x-axis",
            "reflection-y" => "Reflection over the y-axis",
            "dilation"     => $"Dilation with center O, scale factor {sf:G4}",
            _              => transform
        };
        spec.Labels.Add(new SceneLabel
        {
            Id      = "transformDesc",
            Text    = desc,
            OffsetX = 0,
            OffsetY = spec.Viewport.YMin + 0.5
        });

        return spec;
    }

    private static (double x, double y) ApplyTransform(
        double x, double y, string transform, double sf)
        => transform switch
        {
            "rotation90"   => (-y, x),           // 90° CCW about origin
            "reflection-x" => (x, -y),
            "reflection-y" => (-x, y),
            "dilation"     => (x * sf, y * sf),
            _              => (x, y)
        };
}
