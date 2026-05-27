namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using System.Text.Json;

/// <summary>
/// Static utility methods shared by all template implementations.
/// </summary>
internal static class TemplateHelpers
{
    // ── Parameter extraction ──────────────────────────────────────────────────

    public static double GetDouble(Dictionary<string, object> p, string key, double fallback = 0)
    {
        if (!p.TryGetValue(key, out var raw)) return fallback;
        return ToDouble(raw, fallback);
    }

    public static string GetString(Dictionary<string, object> p, string key, string fallback = "")
    {
        if (!p.TryGetValue(key, out var raw)) return fallback;
        return raw?.ToString() ?? fallback;
    }

    public static (double x, double y) GetPoint(Dictionary<string, object> p, string key)
    {
        if (!p.TryGetValue(key, out var raw))
            throw new ArgumentException($"Missing required point parameter '{key}'.");
        return ParsePoint(raw);
    }

    public static List<(double x, double y)> GetPointList(Dictionary<string, object> p, string key)
    {
        if (!p.TryGetValue(key, out var raw))
            throw new ArgumentException($"Missing required vertices parameter '{key}'.");
        return ParsePointList(raw);
    }

    private static (double x, double y) ParsePoint(object raw)
    {
        if (raw is JsonElement je)
        {
            double x = je.GetProperty("x").GetDouble();
            double y = je.GetProperty("y").GetDouble();
            return (x, y);
        }
        if (raw is Dictionary<string, object> dict)
        {
            double x = ToDouble(dict["x"]);
            double y = ToDouble(dict["y"]);
            return (x, y);
        }
        throw new ArgumentException($"Cannot parse point from {raw}");
    }

    private static List<(double x, double y)> ParsePointList(object raw)
    {
        var result = new List<(double, double)>();
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in je.EnumerateArray())
            {
                double x = el.GetProperty("x").GetDouble();
                double y = el.GetProperty("y").GetDouble();
                result.Add((x, y));
            }
            return result;
        }
        if (raw is List<object> list)
        {
            foreach (var item in list)
                result.Add(ParsePoint(item));
            return result;
        }
        throw new ArgumentException("Cannot parse vertex list.");
    }

    private static double ToDouble(object? raw, double fallback = 0)
    {
        if (raw is null) return fallback;
        if (raw is JsonElement je) return je.GetDouble();
        if (raw is double d) return d;
        if (raw is float f) return f;
        if (raw is int i) return i;
        if (raw is long l) return l;
        if (double.TryParse(raw.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            return parsed;
        return fallback;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    public static (double x, double y) Rotate(double x, double y, double angleDeg)
    {
        double rad = angleDeg * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        return (x * cos - y * sin, x * sin + y * cos);
    }

    public static (double x, double y) Translate(double x, double y, double dx, double dy)
        => (x + dx, y + dy);

    public static double Distance(double x1, double y1, double x2, double y2)
        => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

    public static double AngleDeg(double dx, double dy)
        => Math.Atan2(dy, dx) * 180.0 / Math.PI;

    /// <summary>Midpoint of two 2-D points.</summary>
    public static (double x, double y) Midpoint(double x1, double y1, double x2, double y2)
        => ((x1 + x2) / 2, (y1 + y2) / 2);

    // ── Builder helpers ───────────────────────────────────────────────────────

    public static ScenePoint Pt(string id, double x, double y,
        bool draggable = true, bool computed = false)
        => new() { Id = id, X = x, Y = y, Draggable = draggable, Computed = computed };

    public static SceneSegment Seg(string id, string from, string to, int ticks = 0)
        => new() { Id = id, From = from, To = to, CongruenceTicks = ticks };

    public static SceneLabel Lbl(string id, string text, string anchorId,
        double ox = 0.25, double oy = 0.25)
        => new() { Id = id, Text = text, AnchorPoint = anchorId, OffsetX = ox, OffsetY = oy };

    public static SceneMark RightAngleMark(string at)
        => new() { Type = MarkType.RightAngle, At = at };

    public static SceneMark TickMark(string segId, int group = 1)
        => new() { Type = MarkType.CongruenceTick, OnSegment = segId, GroupIndex = group };

    public static SceneMark ParallelMark(string segId, int group = 1)
        => new() { Type = MarkType.ParallelArrow, OnSegment = segId, GroupIndex = group };

    public static SceneMeasurement LengthMeasurement(string id, string segId)
        => new() { Id = id, Type = MeasurementType.Length, Segment = segId, Show = true };

    public static SceneMeasurement AngleMeasurement(string id, string arm1, string vertex, string arm2)
        => new() { Id = id, Type = MeasurementType.Angle, Arm1 = arm1, Vertex = vertex, Arm2 = arm2, Show = true };

    /// <summary>Compute a viewport that fits all the supplied points with margin.</summary>
    public static Viewport FitViewport(IEnumerable<(double x, double y)> pts, double margin = 2.0)
    {
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var (x, y) in pts)
        {
            if (x < xMin) xMin = x;
            if (x > xMax) xMax = x;
            if (y < yMin) yMin = y;
            if (y > yMax) yMax = y;
        }
        return new Viewport
        {
            XMin = xMin - margin,
            XMax = xMax + margin,
            YMin = yMin - margin,
            YMax = yMax + margin,
        };
    }
}
