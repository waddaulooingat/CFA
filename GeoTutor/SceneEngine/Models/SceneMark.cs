using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoTutor.SceneEngine.Models;

public enum MarkType { RightAngle, ParallelArrow, CongruenceTick }

[JsonConverter(typeof(SceneMarkConverter))]
public class SceneMark
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MarkType Type { get; set; }

    // Point-ID form:  "at": "P1"
    public string At { get; set; } = "";

    // Inline-coordinate form:  "at": { "x": 10, "y": 5 }
    public double? AtInlineX { get; set; }
    public double? AtInlineY { get; set; }

    // Two segment IDs that meet at the right angle (used with inline coords)
    public List<string>? Between { get; set; }

    public string? OnSegment { get; set; }   // segment id for tick/parallel marks
    public int GroupIndex { get; set; } = 1; // 1 or 2 for matching groups
}

internal sealed class SceneMarkConverter : JsonConverter<SceneMark>
{
    public override SceneMark Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var mark = new SceneMark();

        if (root.TryGetProperty("type", out var typeEl))
        {
            mark.Type = typeEl.GetString()?.ToLowerInvariant() switch
            {
                "rightangle"     => MarkType.RightAngle,
                "parallelarrow"  => MarkType.ParallelArrow,
                "congruencetick" => MarkType.CongruenceTick,
                _                => MarkType.RightAngle,
            };
        }

        // "at" is either a string (point ID) or an object { "x": ..., "y": ... }.
        if (root.TryGetProperty("at", out var atEl))
        {
            if (atEl.ValueKind == JsonValueKind.String)
                mark.At = atEl.GetString() ?? "";
            else if (atEl.ValueKind == JsonValueKind.Object)
            {
                if (atEl.TryGetProperty("x", out var xEl)) mark.AtInlineX = xEl.GetDouble();
                if (atEl.TryGetProperty("y", out var yEl)) mark.AtInlineY = yEl.GetDouble();
            }
        }

        if (root.TryGetProperty("between", out var betweenEl) &&
            betweenEl.ValueKind == JsonValueKind.Array)
        {
            mark.Between = betweenEl.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .Where(s => s.Length > 0)
                .ToList();
        }

        if (root.TryGetProperty("onSegment", out var onSegEl))
            mark.OnSegment = onSegEl.GetString();

        if (root.TryGetProperty("groupIndex", out var giEl))
            mark.GroupIndex = giEl.GetInt32();

        return mark;
    }

    public override void Write(Utf8JsonWriter writer, SceneMark value, JsonSerializerOptions options)
        => throw new NotSupportedException("SceneMark write not needed.");
}
