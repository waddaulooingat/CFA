namespace GeoTutor.Data;

using GeoTutor.SceneEngine.Models;
using System.Collections.Generic;

/// <summary>
/// Hard-coded sample SceneSpecs used in Phase 1 (no LLM calls required).
/// </summary>
public static class SampleScenes
{
    public static readonly List<(string Title, SceneSpec Scene)> All =
    [
        ("Right Triangle (3-4-5)", RightTriangle345()),
        ("Parallel Lines & Transversal", ParallelLinesTransversal()),
        ("Isosceles Triangle", IsoscelesTriangle()),
        ("Circle with Chord", CircleWithChord()),
        ("Coordinate Polygon", CoordinatePolygon()),
    ];

    // -----------------------------------------------------------------------

    private static SceneSpec RightTriangle345() => new()
    {
        Id = "sample-rt-345",
        Viewport = new Viewport { XMin = -1, XMax = 9, YMin = -1, YMax = 7 },
        Points =
        [
            new ScenePoint { Id = "A", X = 0, Y = 0 },
            new ScenePoint { Id = "B", X = 6, Y = 0 },
            new ScenePoint { Id = "C", X = 6, Y = 4 },
        ],
        Segments =
        [
            new SceneSegment { Id = "AB", From = "A", To = "B" },
            new SceneSegment { Id = "BC", From = "B", To = "C" },
            new SceneSegment { Id = "AC", From = "A", To = "C" },
        ],
        Labels =
        [
            new SceneLabel { Id = "lA", Text = "A", AnchorPoint = "A", OffsetX = -0.4, OffsetY = -0.4 },
            new SceneLabel { Id = "lB", Text = "B", AnchorPoint = "B", OffsetX =  0.3, OffsetY = -0.4 },
            new SceneLabel { Id = "lC", Text = "C", AnchorPoint = "C", OffsetX =  0.3, OffsetY =  0.2 },
        ],
        Marks = [new SceneMark { Type = MarkType.RightAngle, At = "B" }],
        Measurements =
        [
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "AB", Show = true },
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "BC", Show = true },
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "AC", Show = true },
        ],
    };

    private static SceneSpec ParallelLinesTransversal() => new()
    {
        Id = "sample-parallel",
        Viewport = new Viewport { XMin = -1, XMax = 11, YMin = -1, YMax = 8 },
        Points =
        [
            // top parallel line
            new ScenePoint { Id = "A", X = 0, Y = 6 },
            new ScenePoint { Id = "B", X = 10, Y = 6 },
            // bottom parallel line
            new ScenePoint { Id = "C", X = 0, Y = 2 },
            new ScenePoint { Id = "D", X = 10, Y = 2 },
            // transversal intersections
            new ScenePoint { Id = "P", X = 3, Y = 6 },
            new ScenePoint { Id = "Q", X = 5, Y = 2 },
            // transversal endpoints
            new ScenePoint { Id = "T1", X = 1.5, Y = 8 },
            new ScenePoint { Id = "T2", X = 6.5, Y = 0 },
        ],
        Segments =
        [
            new SceneSegment { Id = "line1", From = "A", To = "B" },
            new SceneSegment { Id = "line2", From = "C", To = "D" },
            new SceneSegment { Id = "transv", From = "T1", To = "T2" },
        ],
        Labels =
        [
            new SceneLabel { Id = "lP", Text = "P", AnchorPoint = "P", OffsetX = -0.4, OffsetY =  0.3 },
            new SceneLabel { Id = "lQ", Text = "Q", AnchorPoint = "Q", OffsetX = -0.4, OffsetY = -0.4 },
            new SceneLabel { Id = "l1", Text = "ℓ₁", AnchorPoint = "B", OffsetX = 0.2, OffsetY = 0.2 },
            new SceneLabel { Id = "l2", Text = "ℓ₂", AnchorPoint = "D", OffsetX = 0.2, OffsetY = 0.2 },
        ],
        Marks =
        [
            new SceneMark { Type = MarkType.ParallelArrow, OnSegment = "line1", GroupIndex = 1 },
            new SceneMark { Type = MarkType.ParallelArrow, OnSegment = "line2", GroupIndex = 1 },
        ],
    };

    private static SceneSpec IsoscelesTriangle() => new()
    {
        Id = "sample-isosceles",
        Viewport = new Viewport { XMin = -1, XMax = 9, YMin = -1, YMax = 8 },
        Points =
        [
            new ScenePoint { Id = "A", X = 0, Y = 0 },
            new ScenePoint { Id = "B", X = 8, Y = 0 },
            new ScenePoint { Id = "C", X = 4, Y = 6 },
        ],
        Segments =
        [
            new SceneSegment { Id = "AB", From = "A", To = "B" },
            new SceneSegment { Id = "AC", From = "A", To = "C", CongruenceTicks = 1 },
            new SceneSegment { Id = "BC", From = "B", To = "C", CongruenceTicks = 1 },
        ],
        Labels =
        [
            new SceneLabel { Id = "lA", Text = "A", AnchorPoint = "A", OffsetX = -0.4, OffsetY = -0.4 },
            new SceneLabel { Id = "lB", Text = "B", AnchorPoint = "B", OffsetX =  0.3, OffsetY = -0.4 },
            new SceneLabel { Id = "lC", Text = "C", AnchorPoint = "C", OffsetX =  0.0, OffsetY =  0.4 },
        ],
        Measurements =
        [
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "AC", Show = true },
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "BC", Show = true },
        ],
    };

    private static SceneSpec CircleWithChord() => new()
    {
        Id = "sample-circle-chord",
        Viewport = new Viewport { XMin = -6, XMax = 6, YMin = -6, YMax = 6 },
        Points =
        [
            new ScenePoint { Id = "O", X = 0,    Y = 0 },
            new ScenePoint { Id = "A", X = -3.6, Y =  2.7 },
            new ScenePoint { Id = "B", X =  3.6, Y =  2.7 },
            new ScenePoint { Id = "M", X = 0,    Y =  2.7 },  // midpoint of chord
        ],
        Segments =
        [
            new SceneSegment { Id = "chord", From = "A", To = "B" },
            new SceneSegment { Id = "OM",    From = "O", To = "M" },
            new SceneSegment { Id = "OA",    From = "O", To = "A" },
            new SceneSegment { Id = "OB",    From = "O", To = "B" },
        ],
        Arcs =
        [
            new SceneArc { Id = "circle", Center = "O", Radius = 4.5, SweepAngleDeg = 360 },
        ],
        Labels =
        [
            new SceneLabel { Id = "lO", Text = "O", AnchorPoint = "O", OffsetX = -0.4, OffsetY = -0.4 },
            new SceneLabel { Id = "lA", Text = "A", AnchorPoint = "A", OffsetX = -0.4, OffsetY =  0.2 },
            new SceneLabel { Id = "lB", Text = "B", AnchorPoint = "B", OffsetX =  0.3, OffsetY =  0.2 },
            new SceneLabel { Id = "lM", Text = "M", AnchorPoint = "M", OffsetX =  0.3, OffsetY = -0.4 },
        ],
        Marks = [new SceneMark { Type = MarkType.RightAngle, At = "M" }],
    };

    private static SceneSpec CoordinatePolygon() => new()
    {
        Id = "sample-coord-poly",
        Viewport = new Viewport { XMin = -1, XMax = 9, YMin = -1, YMax = 7 },
        Points =
        [
            new ScenePoint { Id = "A", X = 1, Y = 1 },
            new ScenePoint { Id = "B", X = 7, Y = 1 },
            new ScenePoint { Id = "C", X = 7, Y = 5 },
            new ScenePoint { Id = "D", X = 1, Y = 5 },
        ],
        Segments =
        [
            new SceneSegment { Id = "AB", From = "A", To = "B" },
            new SceneSegment { Id = "BC", From = "B", To = "C" },
            new SceneSegment { Id = "CD", From = "C", To = "D" },
            new SceneSegment { Id = "DA", From = "D", To = "A" },
        ],
        Labels =
        [
            new SceneLabel { Id = "lA", Text = "A(1,1)", AnchorPoint = "A", OffsetX = -1.2, OffsetY = -0.5 },
            new SceneLabel { Id = "lB", Text = "B(7,1)", AnchorPoint = "B", OffsetX =  0.2, OffsetY = -0.5 },
            new SceneLabel { Id = "lC", Text = "C(7,5)", AnchorPoint = "C", OffsetX =  0.2, OffsetY =  0.2 },
            new SceneLabel { Id = "lD", Text = "D(1,5)", AnchorPoint = "D", OffsetX = -1.2, OffsetY =  0.2 },
        ],
        Measurements =
        [
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "AB", Show = true },
            new SceneMeasurement { Type = MeasurementType.Length, Segment = "BC", Show = true },
        ],
    };
}
