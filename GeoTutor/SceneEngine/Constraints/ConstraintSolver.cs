namespace GeoTutor.SceneEngine.Constraints;
using GeoTutor.SceneEngine.Models;

/// <summary>
/// Propagates SceneConstraints against the points in a SceneSpec.
///
/// Supports all 10 constraint types:
///   Perpendicular, Parallel, EqualLength, FixedLength, FixedAngle,
///   Horizontal, Vertical, Midpoint, OnSegment, OnCircle
/// </summary>
public static class ConstraintSolver
{
    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Apply all constraints in <paramref name="spec"/> to its points.
    /// Mutates the points in place.
    /// Runs up to <see cref="MaxIterations"/> passes to allow dependent chains to settle.
    /// </summary>
    public static void Apply(SceneSpec spec)
    {
        if (spec.Constraints.Count == 0) return;

        var pts = BuildIndex(spec.Points);
        var segs = BuildSegIndex(spec.Segments);

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            bool anyChange = false;
            foreach (var c in spec.Constraints)
                anyChange |= ApplyOne(c, pts, segs);

            if (!anyChange) break;
        }
    }

    /// <summary>
    /// Clip a proposed drag for <paramref name="pointId"/> so that no hard
    /// constraints are violated.  Returns the (possibly adjusted) position.
    /// </summary>
    public static (double x, double y) ClipDrag(
        SceneSpec spec, string pointId, double proposedX, double proposedY)
    {
        var pts  = BuildIndex(spec.Points);
        var segs = BuildSegIndex(spec.Segments);

        double x = proposedX;
        double y = proposedY;

        foreach (var c in spec.Constraints)
            (x, y) = ClipOne(c, pointId, x, y, pts, segs);

        return (x, y);
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    private const int MaxIterations = 8;
    private const double Epsilon    = 1e-10;

    // ── Indexers ─────────────────────────────────────────────────────────────

    private static Dictionary<string, ScenePoint> BuildIndex(IEnumerable<ScenePoint> points)
    {
        var d = new Dictionary<string, ScenePoint>(StringComparer.Ordinal);
        foreach (var pt in points) d[pt.Id] = pt;
        return d;
    }

    private static Dictionary<string, SceneSegment> BuildSegIndex(IEnumerable<SceneSegment> segs)
    {
        var d = new Dictionary<string, SceneSegment>(StringComparer.Ordinal);
        foreach (var s in segs) d[s.Id] = s;
        return d;
    }

    // ── Single-constraint application ─────────────────────────────────────────

    /// <returns>true if any point position changed.</returns>
    private static bool ApplyOne(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        return c.Type switch
        {
            ConstraintType.Perpendicular => ApplyPerpendicular(c, pts, segs),
            ConstraintType.Parallel      => ApplyParallel(c, pts, segs),
            ConstraintType.EqualLength   => ApplyEqualLength(c, pts, segs),
            ConstraintType.FixedLength   => ApplyFixedLength(c, pts, segs),
            ConstraintType.FixedAngle    => ApplyFixedAngle(c, pts, segs),
            ConstraintType.Horizontal    => ApplyHorizontal(c, pts, segs),
            ConstraintType.Vertical      => ApplyVertical(c, pts, segs),
            ConstraintType.Midpoint      => ApplyMidpoint(c, pts),
            ConstraintType.OnSegment     => ApplyOnSegment(c, pts, segs),
            ConstraintType.OnCircle      => ApplyOnCircle(c, pts),
            _                            => false
        };
    }

    // ── Perpendicular ─────────────────────────────────────────────────────────
    // "segment Of is perpendicular to segment To"
    // We move the computed endpoint of 'Of' so the segment is perpendicular to 'To'.
    private static bool ApplyPerpendicular(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        if (c.Of is null || c.To is null) return false;
        if (!segs.TryGetValue(c.Of, out var segOf)) return false;
        if (!segs.TryGetValue(c.To, out var segTo)) return false;

        // Find the computed (moveable) endpoint of 'Of'
        if (!TryGetComputedEndpoint(segOf, pts, out var movePt, out var fixedPt)) return false;
        if (!pts.TryGetValue(segTo.From, out var toA)) return false;
        if (!pts.TryGetValue(segTo.To,   out var toB)) return false;

        // Direction of 'To' segment
        double tdx = toB.X - toA.X;
        double tdy = toB.Y - toA.Y;
        double tLen = Math.Sqrt(tdx * tdx + tdy * tdy);
        if (tLen < Epsilon) return false;

        // Desired direction for 'Of': perpendicular to 'To' = (-tdy, tdx) / tLen
        double perpX = -tdy / tLen;
        double perpY =  tdx / tLen;

        // Length of segment 'Of'
        double ofLen = Dist(fixedPt, movePt);
        if (ofLen < Epsilon) return false;

        // Project movePt onto the foot-of-perpendicular from movePt onto the line through segTo,
        // passing through fixedPt.  Actually: place movePt at fixedPt + ofLen * perpDirection,
        // choosing the sign that keeps movePt closer to its current position.
        double newX1 = fixedPt.X + perpX * ofLen;
        double newY1 = fixedPt.Y + perpY * ofLen;
        double newX2 = fixedPt.X - perpX * ofLen;
        double newY2 = fixedPt.Y - perpY * ofLen;

        double d1 = DistSq(movePt.X, movePt.Y, newX1, newY1);
        double d2 = DistSq(movePt.X, movePt.Y, newX2, newY2);

        double nx = d1 <= d2 ? newX1 : newX2;
        double ny = d1 <= d2 ? newY1 : newY2;

        return MovePoint(movePt, nx, ny);
    }

    // ── Parallel ──────────────────────────────────────────────────────────────
    // "segment Of is parallel to segment To"
    // Adjust the computed endpoint of 'Of' so its direction matches 'To'.
    private static bool ApplyParallel(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        if (c.Of is null || c.To is null) return false;
        if (!segs.TryGetValue(c.Of, out var segOf)) return false;
        if (!segs.TryGetValue(c.To, out var segTo)) return false;

        if (!TryGetComputedEndpoint(segOf, pts, out var movePt, out var fixedPt)) return false;
        if (!pts.TryGetValue(segTo.From, out var toA)) return false;
        if (!pts.TryGetValue(segTo.To,   out var toB)) return false;

        double tdx = toB.X - toA.X;
        double tdy = toB.Y - toA.Y;
        double tLen = Math.Sqrt(tdx * tdx + tdy * tdy);
        if (tLen < Epsilon) return false;

        double dirX = tdx / tLen;
        double dirY = tdy / tLen;

        double ofLen = Dist(fixedPt, movePt);
        if (ofLen < Epsilon) return false;

        // Two candidates: fixedPt ± ofLen * dir
        double nx1 = fixedPt.X + dirX * ofLen;
        double ny1 = fixedPt.Y + dirY * ofLen;
        double nx2 = fixedPt.X - dirX * ofLen;
        double ny2 = fixedPt.Y - dirY * ofLen;

        double d1 = DistSq(movePt.X, movePt.Y, nx1, ny1);
        double d2 = DistSq(movePt.X, movePt.Y, nx2, ny2);

        double nx = d1 <= d2 ? nx1 : nx2;
        double ny = d1 <= d2 ? ny1 : ny2;

        return MovePoint(movePt, nx, ny);
    }

    // ── EqualLength ───────────────────────────────────────────────────────────
    // Segment 'Of' has the same length as segment 'To'.
    // Move the computed endpoint of 'Of' so |Of| = |To|.
    private static bool ApplyEqualLength(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        if (c.Of is null || c.To is null) return false;
        if (!segs.TryGetValue(c.Of, out var segOf)) return false;
        if (!segs.TryGetValue(c.To, out var segTo)) return false;

        if (!TryGetComputedEndpoint(segOf, pts, out var movePt, out var fixedPt)) return false;
        if (!pts.TryGetValue(segTo.From, out var toA)) return false;
        if (!pts.TryGetValue(segTo.To,   out var toB)) return false;

        double targetLen = Dist(toA, toB);
        return ScaleEndpointToLength(movePt, fixedPt, targetLen);
    }

    // ── FixedLength ───────────────────────────────────────────────────────────
    // Segment 'Of' (or 'Segment') has a fixed length Value.
    private static bool ApplyFixedLength(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        string? segId = c.Of ?? c.Segment;
        if (segId is null || c.Value is null) return false;
        if (!segs.TryGetValue(segId, out var seg)) return false;

        if (!TryGetComputedEndpoint(seg, pts, out var movePt, out var fixedPt)) return false;
        return ScaleEndpointToLength(movePt, fixedPt, c.Value.Value);
    }

    // ── FixedAngle ────────────────────────────────────────────────────────────
    // The angle that segment 'Of' makes with segment 'To' is fixed at Value degrees.
    // Rotate the computed endpoint of 'Of' around its fixed endpoint.
    private static bool ApplyFixedAngle(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        if (c.Of is null || c.To is null || c.Value is null) return false;
        if (!segs.TryGetValue(c.Of, out var segOf)) return false;
        if (!segs.TryGetValue(c.To, out var segTo)) return false;

        if (!TryGetComputedEndpoint(segOf, pts, out var movePt, out var fixedPt)) return false;
        if (!pts.TryGetValue(segTo.From, out var toA)) return false;
        if (!pts.TryGetValue(segTo.To,   out var toB)) return false;

        // Direction of the reference segment 'To'
        double tdx = toB.X - toA.X;
        double tdy = toB.Y - toA.Y;
        double tLen = Math.Sqrt(tdx * tdx + tdy * tdy);
        if (tLen < Epsilon) return false;

        double baseAngleRad = Math.Atan2(tdy, tdx);
        double targetAngleRad = baseAngleRad + c.Value.Value * Math.PI / 180.0;

        double ofLen = Dist(fixedPt, movePt);
        if (ofLen < Epsilon) return false;

        double nx = fixedPt.X + ofLen * Math.Cos(targetAngleRad);
        double ny = fixedPt.Y + ofLen * Math.Sin(targetAngleRad);

        return MovePoint(movePt, nx, ny);
    }

    // ── Horizontal ────────────────────────────────────────────────────────────
    // Segment stays horizontal: clamp the Y of the computed endpoint to match the fixed endpoint.
    private static bool ApplyHorizontal(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        string? segId = c.Of ?? c.Segment;
        if (segId is null) return false;
        if (!segs.TryGetValue(segId, out var seg)) return false;

        if (!TryGetComputedEndpoint(seg, pts, out var movePt, out var fixedPt)) return false;
        return MovePoint(movePt, movePt.X, fixedPt.Y);
    }

    // ── Vertical ─────────────────────────────────────────────────────────────
    // Segment stays vertical: clamp the X of the computed endpoint to match the fixed endpoint.
    private static bool ApplyVertical(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        string? segId = c.Of ?? c.Segment;
        if (segId is null) return false;
        if (!segs.TryGetValue(segId, out var seg)) return false;

        if (!TryGetComputedEndpoint(seg, pts, out var movePt, out var fixedPt)) return false;
        return MovePoint(movePt, fixedPt.X, movePt.Y);
    }

    // ── Midpoint ──────────────────────────────────────────────────────────────
    // Point 'Point' is the midpoint of 'Of' and 'To' (both are point IDs here).
    private static bool ApplyMidpoint(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts)
    {
        if (c.Point is null || c.Of is null || c.To is null) return false;
        if (!pts.TryGetValue(c.Point, out var mid)) return false;
        if (!pts.TryGetValue(c.Of,    out var ptA)) return false;
        if (!pts.TryGetValue(c.To,    out var ptB)) return false;

        double nx = (ptA.X + ptB.X) / 2.0;
        double ny = (ptA.Y + ptB.Y) / 2.0;
        return MovePoint(mid, nx, ny);
    }

    // ── OnSegment ─────────────────────────────────────────────────────────────
    // Point 'Point' stays on segment 'Segment' — project onto the segment.
    private static bool ApplyOnSegment(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        if (c.Point is null || c.Segment is null) return false;
        if (!pts.TryGetValue(c.Point, out var movePt)) return false;
        if (!segs.TryGetValue(c.Segment, out var seg)) return false;
        if (!pts.TryGetValue(seg.From, out var ptA)) return false;
        if (!pts.TryGetValue(seg.To,   out var ptB)) return false;

        var (nx, ny) = ProjectOntoSegment(movePt.X, movePt.Y, ptA.X, ptA.Y, ptB.X, ptB.Y);
        return MovePoint(movePt, nx, ny);
    }

    // ── OnCircle ─────────────────────────────────────────────────────────────
    // Point 'Point' stays on a circle: center is 'Circle' (a point id), radius is 'Value'.
    private static bool ApplyOnCircle(
        SceneConstraint c,
        Dictionary<string, ScenePoint> pts)
    {
        if (c.Point is null || c.Circle is null || c.Value is null) return false;
        if (!pts.TryGetValue(c.Point,  out var movePt)) return false;
        if (!pts.TryGetValue(c.Circle, out var center)) return false;

        double r = c.Value.Value;
        if (r < Epsilon) return false;

        double dx = movePt.X - center.X;
        double dy = movePt.Y - center.Y;
        double d  = Math.Sqrt(dx * dx + dy * dy);

        if (d < Epsilon)
        {
            // Point is at the center — project to the right (0°)
            return MovePoint(movePt, center.X + r, center.Y);
        }

        double nx = center.X + (dx / d) * r;
        double ny = center.Y + (dy / d) * r;
        return MovePoint(movePt, nx, ny);
    }

    // ── ClipDrag helpers ──────────────────────────────────────────────────────

    private static (double x, double y) ClipOne(
        SceneConstraint c,
        string pointId,
        double x, double y,
        Dictionary<string, ScenePoint> pts,
        Dictionary<string, SceneSegment> segs)
    {
        return c.Type switch
        {
            ConstraintType.Horizontal => ClipHorizontal(c, pointId, x, y, pts, segs),
            ConstraintType.Vertical   => ClipVertical(c, pointId, x, y, pts, segs),
            ConstraintType.OnSegment  => ClipOnSegment(c, pointId, x, y, pts, segs),
            ConstraintType.OnCircle   => ClipOnCircle(c, pointId, x, y, pts),
            _                         => (x, y)  // other constraints handled by Apply, not clip
        };
    }

    private static (double x, double y) ClipHorizontal(
        SceneConstraint c, string pointId, double x, double y,
        Dictionary<string, ScenePoint> pts, Dictionary<string, SceneSegment> segs)
    {
        string? segId = c.Of ?? c.Segment;
        if (segId is null) return (x, y);
        if (!segs.TryGetValue(segId, out var seg)) return (x, y);

        // Only clip if pointId is the computed endpoint of this segment
        if (!TryGetComputedEndpoint(seg, pts, out var movePt, out var fixedPt)) return (x, y);
        if (movePt.Id != pointId) return (x, y);

        return (x, fixedPt.Y);
    }

    private static (double x, double y) ClipVertical(
        SceneConstraint c, string pointId, double x, double y,
        Dictionary<string, ScenePoint> pts, Dictionary<string, SceneSegment> segs)
    {
        string? segId = c.Of ?? c.Segment;
        if (segId is null) return (x, y);
        if (!segs.TryGetValue(segId, out var seg)) return (x, y);

        if (!TryGetComputedEndpoint(seg, pts, out var movePt, out var fixedPt)) return (x, y);
        if (movePt.Id != pointId) return (x, y);

        return (fixedPt.X, y);
    }

    private static (double x, double y) ClipOnSegment(
        SceneConstraint c, string pointId, double x, double y,
        Dictionary<string, ScenePoint> pts, Dictionary<string, SceneSegment> segs)
    {
        if (c.Point != pointId || c.Segment is null) return (x, y);
        if (!segs.TryGetValue(c.Segment, out var seg)) return (x, y);
        if (!pts.TryGetValue(seg.From, out var ptA)) return (x, y);
        if (!pts.TryGetValue(seg.To,   out var ptB)) return (x, y);

        return ProjectOntoSegment(x, y, ptA.X, ptA.Y, ptB.X, ptB.Y);
    }

    private static (double x, double y) ClipOnCircle(
        SceneConstraint c, string pointId, double x, double y,
        Dictionary<string, ScenePoint> pts)
    {
        if (c.Point != pointId || c.Circle is null || c.Value is null) return (x, y);
        if (!pts.TryGetValue(c.Circle, out var center)) return (x, y);

        double r  = c.Value.Value;
        double dx = x - center.X;
        double dy = y - center.Y;
        double d  = Math.Sqrt(dx * dx + dy * dy);
        if (d < Epsilon) return (center.X + r, center.Y);

        return (center.X + dx / d * r, center.Y + dy / d * r);
    }

    // ── Geometry primitives ───────────────────────────────────────────────────

    /// <summary>
    /// Projects point (px,py) onto the line segment (ax,ay)→(bx,by),
    /// clamping to the segment's endpoints.
    /// </summary>
    private static (double x, double y) ProjectOntoSegment(
        double px, double py,
        double ax, double ay,
        double bx, double by)
    {
        double dx = bx - ax;
        double dy = by - ay;
        double lenSq = dx * dx + dy * dy;
        if (lenSq < Epsilon) return (ax, ay);

        double t = ((px - ax) * dx + (py - ay) * dy) / lenSq;
        t = Math.Max(0, Math.Min(1, t));
        return (ax + t * dx, ay + t * dy);
    }

    /// <summary>
    /// Scale (or move) movePt so it is exactly <paramref name="length"/> units
    /// from <paramref name="fixedPt"/>, keeping the current direction.
    /// </summary>
    private static bool ScaleEndpointToLength(
        ScenePoint movePt, ScenePoint fixedPt, double length)
    {
        double dx = movePt.X - fixedPt.X;
        double dy = movePt.Y - fixedPt.Y;
        double d  = Math.Sqrt(dx * dx + dy * dy);
        if (d < Epsilon)
        {
            // Degenerate: push along +x
            return MovePoint(movePt, fixedPt.X + length, fixedPt.Y);
        }
        double nx = fixedPt.X + (dx / d) * length;
        double ny = fixedPt.Y + (dy / d) * length;
        return MovePoint(movePt, nx, ny);
    }

    /// <summary>
    /// Finds the computed endpoint of a segment.
    /// Returns true if exactly one endpoint is Computed (and the other is not).
    /// </summary>
    private static bool TryGetComputedEndpoint(
        SceneSegment seg,
        Dictionary<string, ScenePoint> pts,
        out ScenePoint computed,
        out ScenePoint anchored)
    {
        computed = null!;
        anchored = null!;

        if (!pts.TryGetValue(seg.From, out var from)) return false;
        if (!pts.TryGetValue(seg.To,   out var to))   return false;

        if (from.Computed && !to.Computed)
        {
            computed = from;
            anchored = to;
            return true;
        }
        if (to.Computed && !from.Computed)
        {
            computed = to;
            anchored = from;
            return true;
        }
        // If both or neither are Computed, prefer the 'To' endpoint as moveable
        // (fallback for templates that don't mark Computed)
        if (!from.Computed && !to.Computed)
        {
            computed = to;
            anchored = from;
            return true;
        }
        return false;
    }

    private static double Dist(ScenePoint a, ScenePoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double DistSq(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2;
        double dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Move a point to (nx, ny). Returns true if the position actually changed.
    /// </summary>
    private static bool MovePoint(ScenePoint pt, double nx, double ny)
    {
        if (Math.Abs(pt.X - nx) < Epsilon && Math.Abs(pt.Y - ny) < Epsilon)
            return false;
        pt.X = nx;
        pt.Y = ny;
        return true;
    }
}
