namespace GeoTutor.Tests.SceneEngine;

using System.Collections.Generic;
using GeoTutor.SceneEngine.Models;
using GeoTutor.SceneEngine.Templates;
using Xunit;

public class TemplateTests
{
    [Theory]
    [InlineData("RightTriangle",   new[] { "legA", "3", "legB", "4" })]
    [InlineData("IsoscelesTriangle", new[] { "base", "6", "legLength", "5" })]
    [InlineData("EquilateralTriangle", new[] { "side", "5" })]
    [InlineData("Circle",           new[] { "radius", "4" })]
    [InlineData("Parallelogram",    new[] { "base", "6", "slantSide", "4", "slantAngleDeg", "60" })]
    [InlineData("Trapezoid",        new[] { "topBase", "4", "bottomBase", "8", "height", "3" })]
    public void TryBuild_ValidParams_ReturnsSpec(string templateName, string[] kvPairs)
    {
        var parameters = ParseKvPairs(kvPairs);

        bool ok = TemplateRegistry.TryBuild(templateName, parameters, out var spec, out string error);

        Assert.True(ok, $"Expected success for '{templateName}' but got error: {error}");
        Assert.NotNull(spec);
        Assert.NotEmpty(spec!.Points);
    }

    [Fact]
    public void TryBuild_UnknownTemplate_ReturnsFalse()
    {
        bool ok = TemplateRegistry.TryBuild(
            "NonExistentTemplate", [], out var spec, out string error);

        Assert.False(ok);
        Assert.Null(spec);
        Assert.Contains("Unknown template", error);
    }

    [Fact]
    public void TryBuild_RightTriangle_HasRightAngleMark()
    {
        var p = new Dictionary<string, object>
        {
            ["legA"] = 3.0,
            ["legB"] = 4.0,
        };

        bool ok = TemplateRegistry.TryBuild("RightTriangle", p, out var spec, out _);

        Assert.True(ok);
        Assert.Contains(spec!.Marks, m => m.Type == MarkType.RightAngle);
    }

    [Fact]
    public void TryBuild_EquilateralTriangle_ThreeCongruenceTicks()
    {
        var p = new Dictionary<string, object> { ["side"] = 5.0 };

        bool ok = TemplateRegistry.TryBuild("EquilateralTriangle", p, out var spec, out _);

        Assert.True(ok);
        int tickedSegments = spec!.Segments.Count(s => s.CongruenceTicks > 0);
        Assert.Equal(3, tickedSegments);
    }

    [Fact]
    public void TryBuild_Circle_HasFullArc()
    {
        var p = new Dictionary<string, object> { ["radius"] = 3.0 };

        bool ok = TemplateRegistry.TryBuild("Circle", p, out var spec, out _);

        Assert.True(ok);
        Assert.Contains(spec!.Arcs, a => a.SweepAngleDeg == 360);
    }

    [Fact]
    public void TryBuild_ParallelLinesTransversal_HasTwoSegments()
    {
        var p = new Dictionary<string, object>
        {
            ["gap"]                = 4.0,
            ["transversalAngleDeg"] = 60.0,
            ["markedPair"]         = "corresponding",
        };

        bool ok = TemplateRegistry.TryBuild("ParallelLinesTransversal", p, out var spec, out _);

        Assert.True(ok);
        Assert.True(spec!.Segments.Count >= 3, "Expected at least 3 segments (2 parallel + transversal)");
    }

    [Fact]
    public void AllTemplatesRegistered()
    {
        string[] expected =
        [
            "GenericTriangle", "RightTriangle", "IsoscelesTriangle", "EquilateralTriangle",
            "ParallelLinesTransversal", "TwoCongruentTriangles", "SimilarTriangles",
            "Circle", "CircleWithChord", "CircleWithInscribedAngle", "CircleWithTangent",
            "Parallelogram", "Trapezoid", "CoordinatePolygon", "TransformationPair",
        ];

        foreach (string name in expected)
        {
            var t = TemplateRegistry.Get(name);
            Assert.NotNull(t);
        }
    }

    private static Dictionary<string, object> ParseKvPairs(string[] kvPairs)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i < kvPairs.Length - 1; i += 2)
        {
            string key = kvPairs[i];
            if (double.TryParse(kvPairs[i + 1], out double dv))
                d[key] = dv;
            else
                d[key] = kvPairs[i + 1];
        }
        return d;
    }
}
