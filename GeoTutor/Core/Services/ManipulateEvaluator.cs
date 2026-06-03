using System;
using System.Collections.Generic;
using System.Linq;
using GeoTutor.Core.Models;
using GeoTutor.SceneEngine.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Pure-function evaluator for Manipulate beat success conditions.
/// All methods operate on the current SceneSpec coordinate state.
/// </summary>
public static class ManipulateEvaluator
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="cond"/> is satisfied given the current
    /// scene coordinates.  For explorationGoal, also mutates <paramref name="reachedMilestones"/>
    /// to record newly-reached milestones (never removes entries).
    /// </summary>
    public static bool Evaluate(
        BeatSuccessCondition cond,
        SceneSpec scene,
        HashSet<string> reachedMilestones)
    {
        var ptMap  = scene.Points  .ToDictionary(p => p.Id, p => p);
        var segMap = scene.Segments.ToDictionary(s => s.Id, s => s);

        return cond.Type.ToLowerInvariant() switch
        {
            "collinear"       => EvalCollinear      (cond, ptMap),
            "equallengths"    => EvalEqualLengths    (cond, ptMap, segMap),
            "anglemeasure"    => EvalAngleMeasure    (cond, ptMap),
            "linesparallel"   => EvalLinesParallel   (cond, ptMap, segMap),
            "counterexample"  => EvalCounterexample  (cond, ptMap, scene),
            "explorationgoal" => EvalExplorationGoal (cond, ptMap, scene, reachedMilestones),
            _                 => false,
        };
    }

    // ── collinear ─────────────────────────────────────────────────────────────

    private static bool EvalCollinear(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint> ptMap)
    {
        if (cond.Points.Count < 3) return false;
        if (!ptMap.TryGetValue(cond.Points[0], out var a)) return false;
        if (!ptMap.TryGetValue(cond.Points[1], out var b)) return false;
        if (!ptMap.TryGetValue(cond.Points[2], out var c)) return false;

        // Perpendicular distance from C to line AB.
        double abX = b.X - a.X, abY = b.Y - a.Y;
        double abLen = Math.Sqrt(abX * abX + abY * abY);
        if (abLen < 1e-10) return false;

        double acX = c.X - a.X, acY = c.Y - a.Y;
        double cross = Math.Abs(abX * acY - abY * acX);
        double dist  = cross / abLen;

        // tolerancePx is in screen pixels.  Convert to world units using the
        // viewport width (12 units default) and an assumed canvas ~750 px wide.
        // Result: ~0.19 world units per 12px → use 0.3 as a generous threshold.
        double worldTolerance = Math.Max(0.3, cond.TolerancePx * 0.025);
        return dist < worldTolerance;
    }

    // ── equalLengths ─────────────────────────────────────────────────────────

    private static bool EvalEqualLengths(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint> ptMap,
        Dictionary<string, SceneSegment> segMap)
    {
        if (!TrySegmentLength(cond.Of,  ptMap, segMap, out double len1)) return false;
        if (!TrySegmentLength(cond.And, ptMap, segMap, out double len2)) return false;
        return Math.Abs(len1 - len2) <= cond.Tolerance;
    }

    private static bool TrySegmentLength(
        string? segId,
        Dictionary<string, ScenePoint> ptMap,
        Dictionary<string, SceneSegment> segMap,
        out double length)
    {
        length = 0;
        if (segId is null) return false;
        if (!segMap.TryGetValue(segId, out var seg)) return false;
        if (!ptMap.TryGetValue(seg.From, out var p1)) return false;
        if (!ptMap.TryGetValue(seg.To,   out var p2)) return false;
        length = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        return true;
    }

    // ── angleMeasure ──────────────────────────────────────────────────────────

    private static bool EvalAngleMeasure(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint> ptMap)
    {
        if (cond.Vertex is null || cond.Sides is null || cond.Sides.Count < 2) return false;
        if (!cond.TargetDegrees.HasValue) return false;

        if (!ptMap.TryGetValue(cond.Vertex,   out var v))  return false;
        if (!ptMap.TryGetValue(cond.Sides[0], out var a1)) return false;
        if (!ptMap.TryGetValue(cond.Sides[1], out var a2)) return false;

        double angle = AngleBetween(a1.X - v.X, a1.Y - v.Y, a2.X - v.X, a2.Y - v.Y);
        return Math.Abs(angle - cond.TargetDegrees.Value) <= cond.Tolerance;
    }

    // ── linesParallel ─────────────────────────────────────────────────────────

    private static bool EvalLinesParallel(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint>   ptMap,
        Dictionary<string, SceneSegment> segMap)
    {
        if (cond.Line1 is null || cond.Line2 is null) return false;
        if (!segMap.TryGetValue(cond.Line1, out var s1)) return false;
        if (!segMap.TryGetValue(cond.Line2, out var s2)) return false;
        if (!ptMap.TryGetValue(s1.From, out var p1a) ||
            !ptMap.TryGetValue(s1.To,   out var p1b)) return false;
        if (!ptMap.TryGetValue(s2.From, out var p2a) ||
            !ptMap.TryGetValue(s2.To,   out var p2b)) return false;

        double d1x = p1b.X - p1a.X, d1y = p1b.Y - p1a.Y;
        double d2x = p2b.X - p2a.X, d2y = p2b.Y - p2a.Y;

        // Acute angle between the two direction vectors (0 = parallel, 90 = perpendicular).
        double cross = Math.Abs(d1x * d2y - d1y * d2x);
        double dot   = Math.Abs(d1x * d2x + d1y * d2y);
        double angleDeg = Math.Atan2(cross, dot) * 180.0 / Math.PI;

        return angleDeg <= cond.Tolerance;
    }

    // ── counterexample ────────────────────────────────────────────────────────

    private static bool EvalCounterexample(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint> ptMap,
        SceneSpec scene)
    {
        bool satisfies = cond.SatisfiesHypothesis?.ToLowerInvariant() switch
        {
            "twosidesequal" => AnyTwoSidesEqual(scene, ptMap, cond.Tolerance),
            "allsidesequal" => AllSidesEqual(scene, ptMap, cond.Tolerance),
            _               => false,
        };
        if (!satisfies) return false;

        bool violates = cond.ViolatesConclusion?.ToLowerInvariant() switch
        {
            "norightangle"   => NoRightAngle   (scene, ptMap, cond.Tolerance),
            "noobtuseangled" => NoObtuseAngle  (scene, ptMap),
            _                => false,
        };
        return violates;
    }

    // ── explorationGoal ───────────────────────────────────────────────────────

    private static bool EvalExplorationGoal(
        BeatSuccessCondition cond,
        Dictionary<string, ScenePoint> ptMap,
        SceneSpec scene,
        HashSet<string> reached)
    {
        foreach (var m in cond.Milestones)
        {
            if (reached.Contains(m.Id)) continue;

            bool hit = m.Test.ToLowerInvariant() switch
            {
                "allsidesequal"                  => AllSidesEqual(scene, ptMap, cond.Tolerance),
                "hasrightangleandtwosidesequal"  => AnyRightAngle(scene, ptMap, cond.Tolerance) &&
                                                    AnyTwoSidesEqual(scene, ptMap, cond.Tolerance),
                "nosidesequal"                   => NoTwoSidesEqual(scene, ptMap, cond.Tolerance),
                _                                => false,
            };

            if (hit) reached.Add(m.Id);
        }

        return cond.CompleteWhenAll
            ? cond.Milestones.All(m => reached.Contains(m.Id))
            : reached.Count > 0;
    }

    // ── Shared triangle helpers ───────────────────────────────────────────────

    /// <summary>Returns all side lengths of segments in the scene.</summary>
    private static List<double> SideLengths(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap)
    {
        var lengths = new List<double>();
        foreach (var seg in scene.Segments)
        {
            if (ptMap.TryGetValue(seg.From, out var p1) &&
                ptMap.TryGetValue(seg.To,   out var p2))
            {
                lengths.Add(Math.Sqrt(
                    Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)));
            }
        }
        return lengths;
    }

    private static bool AnyTwoSidesEqual(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap, double tol)
    {
        var lengths = SideLengths(scene, ptMap);
        for (int i = 0; i < lengths.Count; i++)
            for (int j = i + 1; j < lengths.Count; j++)
                if (Math.Abs(lengths[i] - lengths[j]) <= tol)
                    return true;
        return false;
    }

    private static bool NoTwoSidesEqual(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap, double tol)
        => !AnyTwoSidesEqual(scene, ptMap, tol);

    private static bool AllSidesEqual(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap, double tol)
    {
        var lengths = SideLengths(scene, ptMap);
        if (lengths.Count == 0) return false;
        double first = lengths[0];
        return lengths.All(l => Math.Abs(l - first) <= tol);
    }

    /// <summary>
    /// Returns all interior angles (degrees) at every point that is shared by
    /// exactly two segments (i.e. a vertex of a polygon).
    /// </summary>
    private static List<double> InteriorAngles(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap)
    {
        var angles = new List<double>();
        foreach (var pt in scene.Points)
        {
            var arms = scene.Segments
                .Where(s => s.From == pt.Id || s.To == pt.Id)
                .Select(s => s.From == pt.Id ? s.To : s.From)
                .Distinct()
                .ToList();

            if (arms.Count < 2) continue;
            if (!ptMap.TryGetValue(arms[0], out var a1)) continue;
            if (!ptMap.TryGetValue(arms[1], out var a2)) continue;

            angles.Add(AngleBetween(
                a1.X - pt.X, a1.Y - pt.Y,
                a2.X - pt.X, a2.Y - pt.Y));
        }
        return angles;
    }

    private static bool NoRightAngle(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap, double tol)
        => InteriorAngles(scene, ptMap).All(a => Math.Abs(a - 90.0) > tol);

    private static bool AnyRightAngle(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap, double tol)
        => InteriorAngles(scene, ptMap).Any(a => Math.Abs(a - 90.0) <= tol);

    private static bool NoObtuseAngle(
        SceneSpec scene, Dictionary<string, ScenePoint> ptMap)
        => InteriorAngles(scene, ptMap).All(a => a <= 90.0);

    // ── Geometry ──────────────────────────────────────────────────────────────

    private static double AngleBetween(double v1x, double v1y, double v2x, double v2y)
    {
        double dot   = v1x * v2x + v1y * v2y;
        double cross = v1x * v2y - v1y * v2x;
        double angle = Math.Atan2(Math.Abs(cross), dot) * 180.0 / Math.PI;
        return Math.Max(0.0, Math.Min(180.0, angle));
    }
}
