namespace GeoTutor.Tests.SceneEngine;

using System.Collections.Generic;
using GeoTutor.SceneEngine;
using GeoTutor.SceneEngine.Models;
using GeoTutor.SceneEngine.Templates;
using SkiaSharp;
using Xunit;

public class RendererSnapshotTests
{
    private const int W = 600;
    private const int H = 400;

    [Fact]
    public void Render_EmptySpec_ReturnsWhiteBackground()
    {
        var spec = new SceneSpec { Id = "empty" };
        using var bmp = SceneRenderer.Render(spec, W, H);

        Assert.Equal(W, bmp.Width);
        Assert.Equal(H, bmp.Height);

        // Top-left pixel should be white (background)
        SKColor topLeft = bmp.GetPixel(0, 0);
        Assert.Equal(SKColors.White, topLeft);
    }

    [Fact]
    public void Render_SingleSegment_NoException()
    {
        var spec = new SceneSpec
        {
            Id = "segment-test",
            Points =
            [
                new ScenePoint { Id = "A", X = 0, Y = 0 },
                new ScenePoint { Id = "B", X = 5, Y = 0 },
            ],
            Segments =
            [
                new SceneSegment { Id = "AB", From = "A", To = "B" },
            ],
        };

        using var bmp = SceneRenderer.Render(spec, W, H);
        Assert.Equal(W, bmp.Width);
    }

    [Fact]
    public void Render_FullCircle_NoException()
    {
        var spec = new SceneSpec
        {
            Id = "circle-test",
            Points = [new ScenePoint { Id = "O", X = 5, Y = 4 }],
            Arcs =
            [
                new SceneArc { Id = "c1", Center = "O", Radius = 3, SweepAngleDeg = 360 },
            ],
        };

        using var bmp = SceneRenderer.Render(spec, W, H);
        Assert.Equal(W, bmp.Width);
    }

    [Fact]
    public void Render_RightAngleMark_NoException()
    {
        var spec = new SceneSpec
        {
            Id = "right-angle-test",
            Points =
            [
                new ScenePoint { Id = "A", X = 0, Y = 0 },
                new ScenePoint { Id = "B", X = 4, Y = 0 },
                new ScenePoint { Id = "C", X = 4, Y = 3 },
            ],
            Segments =
            [
                new SceneSegment { Id = "AB", From = "A", To = "B" },
                new SceneSegment { Id = "BC", From = "B", To = "C" },
                new SceneSegment { Id = "AC", From = "A", To = "C" },
            ],
            Marks = [new SceneMark { Type = MarkType.RightAngle, At = "B" }],
        };

        using var bmp = SceneRenderer.Render(spec, W, H);
        Assert.Equal(W, bmp.Width);
    }

    [Fact]
    public void Render_CongruenceTicks_NoException()
    {
        var spec = new SceneSpec
        {
            Id = "ticks-test",
            Points =
            [
                new ScenePoint { Id = "A", X = 0, Y = 0 },
                new ScenePoint { Id = "B", X = 3, Y = 0 },
                new ScenePoint { Id = "C", X = 0, Y = 3 },
                new ScenePoint { Id = "D", X = 3, Y = 3 },
            ],
            Segments =
            [
                new SceneSegment { Id = "AB", From = "A", To = "B", CongruenceTicks = 1 },
                new SceneSegment { Id = "CD", From = "C", To = "D", CongruenceTicks = 1 },
                new SceneSegment { Id = "AC", From = "A", To = "C", CongruenceTicks = 2 },
                new SceneSegment { Id = "BD", From = "B", To = "D", CongruenceTicks = 2 },
            ],
        };

        using var bmp = SceneRenderer.Render(spec, W, H);
        Assert.Equal(W, bmp.Width);
    }

    [Fact]
    public void Render_Measurements_NoException()
    {
        var spec = new SceneSpec
        {
            Id = "measure-test",
            Points =
            [
                new ScenePoint { Id = "A", X = 0, Y = 0 },
                new ScenePoint { Id = "B", X = 5, Y = 0 },
                new ScenePoint { Id = "C", X = 2.5, Y = 4 },
            ],
            Segments =
            [
                new SceneSegment { Id = "AB", From = "A", To = "B" },
                new SceneSegment { Id = "BC", From = "B", To = "C" },
                new SceneSegment { Id = "AC", From = "A", To = "C" },
            ],
            Measurements =
            [
                new SceneMeasurement { Type = MeasurementType.Length, Segment = "AB", Show = true },
                new SceneMeasurement { Type = MeasurementType.Angle, Vertex = "A", Arm1 = "B", Arm2 = "C", Show = true },
            ],
        };

        using var bmp = SceneRenderer.Render(spec, W, H);
        Assert.Equal(W, bmp.Width);
    }
}
