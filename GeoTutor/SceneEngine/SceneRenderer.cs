using GeoTutor.SceneEngine.Models;
using SkiaSharp;

namespace GeoTutor.SceneEngine;

/// <summary>
/// Pure-function renderer: spec → pixels.
/// All coordinate math is deterministic; no mutable state is kept.
/// </summary>
public static class SceneRenderer
{
    // ── Palette ───────────────────────────────────────────────────────────────

    private static readonly SKColor Background        = SKColors.White;
    private static readonly SKColor SegmentDefault    = new(0x33, 0x33, 0x33);   // dark gray
    private static readonly SKColor SegmentHighlight  = new(0x1A, 0x73, 0xE8);   // blue
    private static readonly SKColor PointDefault      = new(0x33, 0x33, 0x33);
    private static readonly SKColor PointHighlight    = new(0x1A, 0x73, 0xE8);
    private static readonly SKColor PointDraggable    = new(0x0D, 0x9E, 0x73);   // teal
    private static readonly SKColor LabelDefault      = new(0x22, 0x22, 0x22);
    private static readonly SKColor LabelTemporary    = new(0xAA, 0xAA, 0xAA);
    private static readonly SKColor MeasureColor      = new(0xC0, 0x39, 0x2B);   // deep red
    private static readonly SKColor MarkColor         = new(0x33, 0x33, 0x33);

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Renders <paramref name="spec"/> into a newly allocated <see cref="SKBitmap"/>.
    /// Caller owns the bitmap and must dispose it.
    /// </summary>
    public static SKBitmap Render(SceneSpec spec, int widthPx, int heightPx)
    {
        var bmp = new SKBitmap(widthPx, heightPx, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);

        // Build a coordinate transform closure captured by local helpers.
        float ToScreenX(double wx) =>
            (float)((wx - spec.Viewport.XMin) / (spec.Viewport.XMax - spec.Viewport.XMin) * widthPx);
        float ToScreenY(double wy) =>
            (float)((1.0 - (wy - spec.Viewport.YMin) / (spec.Viewport.YMax - spec.Viewport.YMin)) * heightPx);

        SKPoint ToScreen(double wx, double wy) => new(ToScreenX(wx), ToScreenY(wy));

        // World-unit to pixel scale (for sizing decorations consistently).
        float scaleX = widthPx  / (float)(spec.Viewport.XMax - spec.Viewport.XMin);
        float scaleY = heightPx / (float)(spec.Viewport.YMax - spec.Viewport.YMin);
        float scale  = MathF.Min(scaleX, scaleY);   // use the tighter axis

        // Build a fast point-lookup dictionary.
        var ptMap = spec.Points.ToDictionary(p => p.Id, p => p);

        // ── Background ────────────────────────────────────────────────────────
        canvas.Clear(Background);

        // ── Segments ──────────────────────────────────────────────────────────
        foreach (var seg in spec.Segments)
            DrawSegment(canvas, spec, seg, ptMap, ToScreenX, ToScreenY, ToScreen, scale);

        // ── Arcs / Circles ────────────────────────────────────────────────────
        foreach (var arc in spec.Arcs)
            DrawArc(canvas, arc, ptMap, ToScreenX, ToScreenY, scale);

        // ── Marks (right-angle, tick, parallel) ───────────────────────────────
        foreach (var mark in spec.Marks)
            DrawMark(canvas, spec, mark, ptMap, ToScreen, scale);

        // ── Measurements ─────────────────────────────────────────────────────
        foreach (var m in spec.Measurements)
            if (m.Show)
                DrawMeasurement(canvas, spec, m, ptMap, ToScreen, scale);

        // ── Points ────────────────────────────────────────────────────────────
        foreach (var pt in spec.Points)
            DrawPoint(canvas, pt, ToScreenX, ToScreenY, scale);

        // ── Labels ────────────────────────────────────────────────────────────
        foreach (var lbl in spec.Labels)
            DrawLabel(canvas, lbl, ptMap, ToScreenX, ToScreenY, scale);

        return bmp;
    }

    // ── Segments ──────────────────────────────────────────────────────────────

    private static void DrawSegment(
        SKCanvas canvas,
        SceneSpec spec,
        SceneSegment seg,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, float> toSX,
        Func<double, float> toSY,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        if (!ptMap.TryGetValue(seg.From, out var from) ||
            !ptMap.TryGetValue(seg.To,   out var to))
            return;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = seg.Highlighted ? SegmentHighlight : SegmentDefault,
            StrokeWidth = seg.Highlighted ? 3f : 2f,
            StrokeCap   = SKStrokeCap.Round,
        };

        var pFrom = toScreen(from.X, from.Y);
        var pTo   = toScreen(to.X,   to.Y);

        if (seg.Kind == SegmentKind.Segment)
        {
            canvas.DrawLine(pFrom, pTo, paint);
        }
        else
        {
            // For Ray / Line we extend beyond the viewport clip — let SkiaSharp clip.
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-10) return;
            double ux = dx / len;
            double uy = dy / len;
            double extend = 1000;  // world units — well outside any viewport

            if (seg.Kind == SegmentKind.Ray)
            {
                var far = toScreen(to.X + ux * extend, to.Y + uy * extend);
                canvas.DrawLine(pFrom, far, paint);
            }
            else // Line
            {
                var nearFar = toScreen(from.X - ux * extend, from.Y - uy * extend);
                var farFar  = toScreen(to.X   + ux * extend, to.Y   + uy * extend);
                canvas.DrawLine(nearFar, farFar, paint);
            }
        }
    }

    // ── Arcs / Circles ────────────────────────────────────────────────────────

    private static void DrawArc(
        SKCanvas canvas,
        SceneArc arc,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, float> toSX,
        Func<double, float> toSY,
        float scale)
    {
        if (!ptMap.TryGetValue(arc.Center, out var center)) return;
        if (arc.Radius <= 0) return;

        float cx = toSX(center.X);
        float cy = toSY(center.Y);
        // The radius in screen pixels: use the x-axis scale (assumes aspect ≈ 1).
        float rx = (float)(arc.Radius * (toSX(center.X + 1) - cx));   // delta in screen X per 1 world unit
        float ry = (float)(arc.Radius * (cy - toSY(center.Y + 1)));  // delta in screen Y per 1 world unit (flipped)

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = arc.Highlighted ? SegmentHighlight : SegmentDefault,
            StrokeWidth = arc.Highlighted ? 3f : 2f,
        };

        bool isFullCircle = Math.Abs(arc.SweepAngleDeg - 360.0) < 0.01;

        if (isFullCircle)
        {
            canvas.DrawOval(cx, cy, Math.Abs(rx), Math.Abs(ry), paint);
        }
        else
        {
            var rect = new SKRect(cx - Math.Abs(rx), cy - Math.Abs(ry),
                                  cx + Math.Abs(rx), cy + Math.Abs(ry));
            // SkiaSharp arcs: angle measured clockwise from +x in screen space.
            // Our world Y is flipped, so negate angles to match standard math orientation.
            float startScreen = -(float)arc.StartAngleDeg;
            float sweepScreen = -(float)arc.SweepAngleDeg;
            using var path = new SKPath();
            path.AddArc(rect, startScreen, sweepScreen);
            canvas.DrawPath(path, paint);
        }
    }

    // ── Points ────────────────────────────────────────────────────────────────

    private static void DrawPoint(
        SKCanvas canvas,
        ScenePoint pt,
        Func<double, float> toSX,
        Func<double, float> toSY,
        float scale)
    {
        float px = toSX(pt.X);
        float py = toSY(pt.Y);
        float r  = pt.Draggable ? 8f : 6f;

        SKColor color = pt.Highlighted ? PointHighlight
                      : pt.Draggable   ? PointDraggable
                      :                  PointDefault;

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = color,
        };
        using var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = SKColors.White,
            StrokeWidth = 2f,
        };

        canvas.DrawCircle(px, py, r, fillPaint);
        canvas.DrawCircle(px, py, r, strokePaint);
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    private static void DrawLabel(
        SKCanvas canvas,
        SceneLabel lbl,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, float> toSX,
        Func<double, float> toSY,
        float scale)
    {
        if (string.IsNullOrWhiteSpace(lbl.Text)) return;

        float x, y;

        if (lbl.AnchorPoint != null && ptMap.TryGetValue(lbl.AnchorPoint, out var anchor))
        {
            // OffsetX/OffsetY are in world units.
            x = toSX(anchor.X + lbl.OffsetX);
            y = toSY(anchor.Y + lbl.OffsetY);
        }
        else
        {
            // Treat OffsetX/Y as absolute world coords when there is no anchor.
            x = toSX(lbl.OffsetX);
            y = toSY(lbl.OffsetY);
        }

        SKColor color = lbl.IsTemporary ? LabelTemporary : LabelDefault;
        float   size  = MathF.Max(10f, scale * 0.35f);   // scale with viewport zoom

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color       = color,
            TextSize    = size,
            Typeface    = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.SemiBold,
                                                     SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextAlign   = SKTextAlign.Center,
        };

        canvas.DrawText(lbl.Text, x, y, paint);
    }

    // ── Marks ─────────────────────────────────────────────────────────────────

    private static void DrawMark(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMark mark,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        switch (mark.Type)
        {
            case MarkType.RightAngle:
                DrawRightAngleMark(canvas, spec, mark, ptMap, toScreen, scale);
                break;
            case MarkType.CongruenceTick:
                DrawCongruenceTick(canvas, spec, mark, ptMap, toScreen, scale);
                break;
            case MarkType.ParallelArrow:
                DrawParallelArrow(canvas, spec, mark, ptMap, toScreen, scale);
                break;
        }
    }

    /// <summary>
    /// Draws a small square at the vertex where two segments meet at a right angle.
    /// Finds up to two segments that share the vertex and uses their directions.
    /// </summary>
    private static void DrawRightAngleMark(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMark mark,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        if (!ptMap.TryGetValue(mark.At, out var vertex)) return;

        // Collect the two segments that share this vertex.
        var touching = spec.Segments
            .Where(s => s.From == mark.At || s.To == mark.At)
            .Take(2)
            .ToList();

        if (touching.Count < 2) return;

        // Direction vectors away from vertex for each segment.
        SKPoint Dir(SceneSegment seg)
        {
            bool isFrom = seg.From == mark.At;
            var other   = ptMap[isFrom ? seg.To : seg.From];
            float dx    = (float)(other.X - vertex.X);
            float dy    = (float)(other.Y - vertex.Y);   // world
            float len   = MathF.Sqrt(dx * dx + dy * dy);
            return len < 1e-6f ? new SKPoint(1, 0) : new SKPoint(dx / len, dy / len);
        }

        var d1w = Dir(touching[0]);
        var d2w = Dir(touching[1]);

        // Convert world direction to screen direction (Y is flipped).
        SKPoint WorldDirToScreen(SKPoint wd) => new(wd.X, -wd.Y);

        var d1s = WorldDirToScreen(d1w);
        var d2s = WorldDirToScreen(d2w);

        var vScreen = toScreen(vertex.X, vertex.Y);
        float sz    = MathF.Max(8f, scale * 0.18f);   // size of the square in screen px

        // Square corner points: vertex + offset along each arm + corner.
        var p1 = new SKPoint(vScreen.X + d1s.X * sz, vScreen.Y + d1s.Y * sz);
        var p2 = new SKPoint(vScreen.X + d2s.X * sz, vScreen.Y + d2s.Y * sz);
        var pc = new SKPoint(p1.X + d2s.X * sz, p1.Y + d2s.Y * sz);

        using var path = new SKPath();
        path.MoveTo(p1);
        path.LineTo(pc);
        path.LineTo(p2);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = MarkColor,
            StrokeWidth = 1.5f,
        };
        canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Draws 1, 2, or 3 short perpendicular tick marks at the midpoint of a segment.
    /// Tick count comes from <see cref="SceneMark.GroupIndex"/> (1–3).
    /// </summary>
    private static void DrawCongruenceTick(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMark mark,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        var seg = ResolveSegment(spec, mark);
        if (seg == null) return;
        if (!ptMap.TryGetValue(seg.From, out var pFrom) ||
            !ptMap.TryGetValue(seg.To,   out var pTo))   return;

        var sFrom = toScreen(pFrom.X, pFrom.Y);
        var sTo   = toScreen(pTo.X,   pTo.Y);

        DrawTicksOnSegment(canvas, sFrom, sTo, mark.GroupIndex, scale, MarkColor);
    }

    /// <summary>Helper called by both <see cref="DrawCongruenceTick"/> and segment-level ticks.</summary>
    private static void DrawTicksOnSegment(
        SKCanvas canvas,
        SKPoint sFrom,
        SKPoint sTo,
        int tickCount,
        float scale,
        SKColor color)
    {
        tickCount = Math.Clamp(tickCount, 1, 3);

        float dx   = sTo.X - sFrom.X;
        float dy   = sTo.Y - sFrom.Y;
        float len  = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1e-3f) return;

        // Unit vector along segment (screen).
        float ux = dx / len;
        float uy = dy / len;
        // Perpendicular.
        float px = -uy;
        float py =  ux;

        float tickLen  = MathF.Max(8f, scale * 0.15f);
        float tickGap  = MathF.Max(3f, scale * 0.05f);
        float halfLen  = tickLen / 2f;

        var mid = new SKPoint((sFrom.X + sTo.X) / 2f, (sFrom.Y + sTo.Y) / 2f);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = color,
            StrokeWidth = 1.8f,
            StrokeCap   = SKStrokeCap.Round,
        };

        // Spacing: center the group of ticks around the midpoint.
        float totalSpan = (tickCount - 1) * tickGap;
        float startOff  = -totalSpan / 2f;

        for (int i = 0; i < tickCount; i++)
        {
            float offset = startOff + i * tickGap;
            var   center = new SKPoint(mid.X + ux * offset, mid.Y + uy * offset);
            var   a      = new SKPoint(center.X + px * halfLen, center.Y + py * halfLen);
            var   b      = new SKPoint(center.X - px * halfLen, center.Y - py * halfLen);
            canvas.DrawLine(a, b, paint);
        }
    }

    /// <summary>
    /// Draws one or two chevron (&gt;) symbols at the midpoint of a segment to denote parallelism.
    /// GroupIndex 1 → single chevron, GroupIndex 2 → double chevron.
    /// </summary>
    private static void DrawParallelArrow(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMark mark,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        var seg = ResolveSegment(spec, mark);
        if (seg == null) return;
        if (!ptMap.TryGetValue(seg.From, out var pFrom) ||
            !ptMap.TryGetValue(seg.To,   out var pTo))   return;

        var sFrom = toScreen(pFrom.X, pFrom.Y);
        var sTo   = toScreen(pTo.X,   pTo.Y);

        float dx  = sTo.X - sFrom.X;
        float dy  = sTo.Y - sFrom.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1e-3f) return;

        float ux = dx / len;
        float uy = dy / len;
        float px = -uy;
        float py =  ux;

        var mid   = new SKPoint((sFrom.X + sTo.X) / 2f, (sFrom.Y + sTo.Y) / 2f);
        float arm = MathF.Max(6f, scale * 0.12f);
        float gap = MathF.Max(4f, scale * 0.07f);

        int chevrons = mark.GroupIndex == 2 ? 2 : 1;
        float totalSpan = (chevrons - 1) * gap;
        float startOff  = -totalSpan / 2f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            Color       = MarkColor,
            StrokeWidth = 1.8f,
            StrokeJoin  = SKStrokeJoin.Round,
            StrokeCap   = SKStrokeCap.Round,
        };

        for (int i = 0; i < chevrons; i++)
        {
            float offset  = startOff + i * gap;
            var   center  = new SKPoint(mid.X + ux * offset, mid.Y + uy * offset);
            // Chevron: two lines forming a ">" shape pointing along the segment direction.
            var   tip     = new SKPoint(center.X + ux * arm * 0.5f, center.Y + uy * arm * 0.5f);
            var   backTop = new SKPoint(center.X - ux * arm * 0.5f + px * arm * 0.5f,
                                        center.Y - uy * arm * 0.5f + py * arm * 0.5f);
            var   backBot = new SKPoint(center.X - ux * arm * 0.5f - px * arm * 0.5f,
                                        center.Y - uy * arm * 0.5f - py * arm * 0.5f);

            using var path = new SKPath();
            path.MoveTo(backTop);
            path.LineTo(tip);
            path.LineTo(backBot);
            canvas.DrawPath(path, paint);
        }
    }

    // ── Measurements ─────────────────────────────────────────────────────────

    private static void DrawMeasurement(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMeasurement m,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        switch (m.Type)
        {
            case MeasurementType.Length:
                DrawLengthMeasurement(canvas, spec, m, ptMap, toScreen, scale);
                break;
            case MeasurementType.Angle:
                DrawAngleMeasurement(canvas, spec, m, ptMap, toScreen, scale);
                break;
            case MeasurementType.Area:
                // Area requires polygon; not rendered as a single entity here.
                break;
        }
    }

    private static void DrawLengthMeasurement(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMeasurement m,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        if (m.Segment == null) return;
        var seg = spec.Segments.FirstOrDefault(s => s.Id == m.Segment);
        if (seg == null) return;
        if (!ptMap.TryGetValue(seg.From, out var pFrom) ||
            !ptMap.TryGetValue(seg.To,   out var pTo))   return;

        double dist = Math.Sqrt(Math.Pow(pTo.X - pFrom.X, 2) + Math.Pow(pTo.Y - pFrom.Y, 2));
        string text = $"{dist:0.00}";
        if (m.TargetValue.HasValue)
            text += $" / {m.TargetValue:0.00}";

        // Place text slightly above the midpoint (offset perpendicular to segment).
        var sMid = toScreen((pFrom.X + pTo.X) / 2.0, (pFrom.Y + pTo.Y) / 2.0);
        float dx  = (float)(pTo.X - pFrom.X);
        float dy  = (float)(pTo.Y - pFrom.Y);   // world
        float len = MathF.Sqrt(dx * dx + dy * dy);
        // Perpendicular direction in screen space (Y inverted).
        float perpX = len < 1e-6f ? 0f : -(-dy) / len;   // screen perp
        float perpY = len < 1e-6f ? -1f :  (-dx) / len;
        float nudge = MathF.Max(14f, scale * 0.25f);

        DrawMeasurementText(canvas, text, sMid.X + perpX * nudge, sMid.Y + perpY * nudge, scale);
    }

    private static void DrawAngleMeasurement(
        SKCanvas canvas,
        SceneSpec spec,
        SceneMeasurement m,
        Dictionary<string, ScenePoint> ptMap,
        Func<double, double, SKPoint> toScreen,
        float scale)
    {
        if (m.Vertex == null || m.Arm1 == null || m.Arm2 == null) return;
        if (!ptMap.TryGetValue(m.Vertex, out var vPt)) return;
        if (!ptMap.TryGetValue(m.Arm1,   out var a1))  return;
        if (!ptMap.TryGetValue(m.Arm2,   out var a2))  return;

        // Vectors from vertex to arm endpoints.
        double v1x = a1.X - vPt.X, v1y = a1.Y - vPt.Y;
        double v2x = a2.X - vPt.X, v2y = a2.Y - vPt.Y;

        double angleDeg = AngleBetween(v1x, v1y, v2x, v2y);
        string text = $"{angleDeg:0.0}°";
        if (m.TargetValue.HasValue)
            text += $" / {m.TargetValue:0.0}°";

        // Bisector direction for label placement.
        double len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
        double len2 = Math.Sqrt(v2x * v2x + v2y * v2y);
        if (len1 < 1e-10 || len2 < 1e-10) return;
        double bisX = v1x / len1 + v2x / len2;
        double bisY = v1y / len1 + v2y / len2;
        double bisLen = Math.Sqrt(bisX * bisX + bisY * bisY);
        if (bisLen < 1e-10) { bisX = -v1y / len1; bisY = v1x / len1; bisLen = 1.0; }
        bisX /= bisLen; bisY /= bisLen;

        float nudge = MathF.Max(20f, scale * 0.45f);
        var   vSc   = toScreen(vPt.X, vPt.Y);

        // Convert bisector (world) to screen (Y flipped).
        float labelX = vSc.X + (float)bisX * nudge;
        float labelY = vSc.Y - (float)bisY * nudge;   // Y flip

        DrawMeasurementText(canvas, text, labelX, labelY, scale);
    }

    private static void DrawMeasurementText(
        SKCanvas canvas, string text, float x, float y, float scale)
    {
        float size = MathF.Max(10f, scale * 0.28f);
        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = new SKColor(255, 255, 255, 210),
        };
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color       = MeasureColor,
            TextSize    = size,
            Typeface    = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal,
                                                     SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextAlign   = SKTextAlign.Center,
        };

        float textWidth = paint.MeasureText(text);
        float pad = 3f;
        var   bgRect = new SKRect(x - textWidth / 2 - pad, y - size - pad,
                                  x + textWidth / 2 + pad, y + pad);
        canvas.DrawRect(bgRect, bgPaint);
        canvas.DrawText(text, x, y, paint);
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    private static double AngleBetween(double v1x, double v1y, double v2x, double v2y)
    {
        double dot   = v1x * v2x + v1y * v2y;
        double cross = v1x * v2y - v1y * v2x;   // z-component of cross product
        double angle = Math.Atan2(Math.Abs(cross), dot) * 180.0 / Math.PI;
        return Math.Max(0.0, Math.Min(180.0, angle));
    }

    private static SceneSegment? ResolveSegment(SceneSpec spec, SceneMark mark)
    {
        if (mark.OnSegment != null)
            return spec.Segments.FirstOrDefault(s => s.Id == mark.OnSegment);
        // Fallback: if At refers to a point, nothing to do for segment-based marks.
        return null;
    }
}
