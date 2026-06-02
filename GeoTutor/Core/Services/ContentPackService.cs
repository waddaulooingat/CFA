using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GeoTutor.Core.Models;
using GeoTutor.SceneEngine.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Loads lessons from the content pack (Content/unit-*/lessons/*.json + *.md).
/// Raw scene specs are deserialized directly into SceneSpec and attached inline to each Beat.
/// </summary>
public static class ContentPackService
{
    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new MeasurementTypeConverter() },
    };

    // Maps content-pack strings like "angleMeasure" to the MeasurementType enum.
    private sealed class MeasurementTypeConverter : JsonConverter<MeasurementType>
    {
        public override MeasurementType Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? raw = reader.GetString();
            return raw?.ToLowerInvariant() switch
            {
                "length"         => MeasurementType.Length,
                "angle"          => MeasurementType.Angle,
                "anglemeasure"   => MeasurementType.Angle,
                "area"           => MeasurementType.Area,
                "distancetoline" => MeasurementType.DistanceToLine,
                _                => MeasurementType.Length,
            };
        }

        public override void Write(Utf8JsonWriter writer, MeasurementType value,
                                   JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }

    private static readonly Dictionary<string, (string SubDir, string BaseName)> SkillFiles = new()
    {
        // Unit 1 — Foundations & Logical Reasoning
        ["geo-u1-point-line-plane"]  = ("unit-01/unit-01/lessons", "01-point-line-plane"),
        ["geo-u1-segments-and-rays"] = ("unit-01/unit-01/lessons", "02-segments-and-rays"),
        ["geo-u1-angle-measure"]     = ("unit-01/unit-01/lessons", "03-angle-measure"),
        ["geo-u1-if-then-logic"]     = ("unit-01/unit-01/lessons", "04-if-then-logic"),
        ["geo-u1-counterexamples"]   = ("unit-01/unit-01/lessons", "05-counterexamples"),

        // Unit 2 — Parallel Lines & Transversals
        ["geo-u2-parallel-basics"]     = ("unit-02/unit-02/lessons", "01-parallel-basics"),
        ["geo-u2-transversal-angles"]  = ("unit-02/unit-02/lessons", "02-transversal-angles"),
        ["geo-u2-parallel-theorems"]   = ("unit-02/unit-02/lessons", "03-parallel-theorems"),
        ["geo-u2-proving-parallel"]    = ("unit-02/unit-02/lessons", "04-proving-parallel"),
        ["geo-u2-coordinate-parallel"] = ("unit-02/unit-02/lessons", "05-coordinate-parallel"),
    };

    /// <summary>
    /// Attempts to build a <see cref="Lesson"/> from the content pack for the given skill.
    /// Returns <c>false</c> if no content pack entry exists or files cannot be read.
    /// </summary>
    public static bool TryBuildLesson(string skillId, out Lesson? lesson)
    {
        lesson = null;
        if (!SkillFiles.TryGetValue(skillId, out var entry)) return false;

        string contentRoot = FindContentRoot();
        string dir         = Path.Combine(contentRoot,
                                 entry.SubDir.Replace('/', Path.DirectorySeparatorChar));
        string jsonPath = Path.Combine(dir, entry.BaseName + ".json");
        string mdPath   = Path.Combine(dir, entry.BaseName + ".md");

        if (!File.Exists(jsonPath)) return false;

        try
        {
            string jsonText = File.ReadAllText(jsonPath);
            string mdText   = File.Exists(mdPath) ? File.ReadAllText(mdPath) : "";

            using var doc = JsonDocument.Parse(jsonText);
            var root      = doc.RootElement;

            // Parse scenes dict: { "sceneId": { raw SceneSpec } }
            var scenes = ParseScenes(root);

            // Parse beat sequence
            var beats = new List<Beat>();
            if (root.TryGetProperty("beats", out var beatsEl))
            {
                foreach (var beatEl in beatsEl.EnumerateArray())
                    beats.Add(ParseBeat(beatEl, scenes, mdText));
            }

            lesson = new Lesson
            {
                Id      = $"cp:{skillId}",
                SkillId = skillId,
                Beats   = beats,
                Version = 1,
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Parsing helpers ──────────────────────────────────────────────────────

    private static Dictionary<string, SceneSpec> ParseScenes(JsonElement root)
    {
        var scenes = new Dictionary<string, SceneSpec>();
        if (!root.TryGetProperty("scenes", out var scenesEl)) return scenes;

        foreach (var kv in scenesEl.EnumerateObject())
        {
            try
            {
                string specJson = kv.Value.GetRawText();
                var spec = JsonSerializer.Deserialize<SceneSpec>(specJson, CaseInsensitive);
                if (spec is not null)
                {
                    spec.Id = kv.Name;
                    scenes[kv.Name] = spec;
                    Debug.WriteLine(
                        $"[ContentPack] Parsed scene '{kv.Name}': " +
                        $"points={spec.Points.Count}, segs={spec.Segments.Count}, " +
                        $"measurements={spec.Measurements.Count}, " +
                        $"annotations={spec.Annotations.Count}, " +
                        $"banner={(spec.StatementBanner is null ? "none" : $"'{spec.StatementBanner.Text[..Math.Min(40, spec.StatementBanner.Text.Length)]}'")})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContentPack] !! Failed to parse scene '{kv.Name}': {ex.Message}");
            }
        }

        return scenes;
    }

    private static Beat ParseBeat(
        JsonElement el,
        Dictionary<string, SceneSpec> scenes,
        string mdText)
    {
        var beat = new Beat();

        if (el.TryGetProperty("type", out var typeEl) &&
            Enum.TryParse<BeatType>(typeEl.GetString(), ignoreCase: true, out var beatType))
        {
            beat.Type = beatType;
        }

        if (el.TryGetProperty("title", out var titleEl))
            beat.Title = titleEl.GetString() ?? "";

        // Inline scene from the scenes dict
        if (el.TryGetProperty("scene", out var sceneKeyEl))
        {
            string sceneKey = sceneKeyEl.GetString() ?? "";
            if (scenes.TryGetValue(sceneKey, out var spec))
                beat.Scene = spec;
        }

        // Prose: try proseRef → markdown section, then instruction, then steps
        if (el.TryGetProperty("proseRef", out var proseRefEl))
        {
            string proseRef = proseRefEl.GetString() ?? "";
            beat.Prose = ExtractMarkdownSection(mdText, proseRef);
        }
        else if (el.TryGetProperty("instruction", out var instrEl))
        {
            beat.Prose = instrEl.GetString() ?? "";
        }
        else if (el.TryGetProperty("steps", out var stepsEl))
        {
            beat.Prose = ExtractStepsProse(stepsEl);
        }

        // Success condition + message (Manipulate beats)
        if (el.TryGetProperty("successCondition", out var scEl))
        {
            try
            {
                beat.SuccessCondition = JsonSerializer.Deserialize<BeatSuccessCondition>(
                    scEl.GetRawText(), CaseInsensitive);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ContentPack] Failed to parse successCondition: {ex.Message}");
            }
        }

        if (el.TryGetProperty("successMessage", out var smEl))
            beat.SuccessMessage = smEl.GetString() ?? "";

        return beat;
    }

    private static string ExtractMarkdownSection(string mdText, string proseRef)
    {
        if (string.IsNullOrEmpty(mdText)) return "";

        int hashIdx = proseRef.LastIndexOf('#');
        if (hashIdx < 0) return "";

        string anchor = proseRef[(hashIdx + 1)..].ToLowerInvariant();

        string[] lines   = mdText.Split('\n');
        int      startLine = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!line.StartsWith("## ")) continue;

            // Normalize heading: lowercase, strip non-alphanumeric except hyphens
            string heading = line[3..].ToLowerInvariant();
            string slug    = NormalizeSlug(heading);

            if (slug.StartsWith(anchor, StringComparison.Ordinal) ||
                anchor.StartsWith(slug,  StringComparison.Ordinal))
            {
                startLine = i + 1;
                break;
            }
        }

        if (startLine < 0) return "";

        var sb = new StringBuilder();
        for (int i = startLine; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd();
            if (trimmed == "---" || trimmed.StartsWith("## ", StringComparison.Ordinal))
                break;
            sb.AppendLine(lines[i]);
        }

        return StripMarkdown(sb.ToString().Trim());
    }

    private static string ExtractStepsProse(JsonElement stepsEl)
    {
        var sb = new StringBuilder();
        foreach (var step in stepsEl.EnumerateArray())
        {
            if (step.TryGetProperty("text", out var textEl))
            {
                int order = 0;
                if (step.TryGetProperty("order", out var orderEl))
                    order = orderEl.GetInt32();
                sb.AppendLine($"Step {order}: {textEl.GetString()}");
            }
        }
        return sb.ToString().Trim();
    }

    private static string StripMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove fenced code blocks
        text = Regex.Replace(text, @"```[\s\S]*?```", "", RegexOptions.Multiline);
        // Remove inline code
        text = Regex.Replace(text, @"`[^`]+`", m => m.Value.Trim('`'));
        // Remove LaTeX display blocks $$...$$
        text = Regex.Replace(text, @"\$\$[\s\S]*?\$\$", "[formula]");
        // Remove inline LaTeX $...$
        text = Regex.Replace(text, @"\$[^\$\n]+\$", "[formula]");
        // Strip bold/italic markers: ***text***, **text**, *text*, __text__, _text_
        text = Regex.Replace(text, @"\*{1,3}([^\*\n]+)\*{1,3}", "$1");
        text = Regex.Replace(text, @"_{1,2}([^_\n]+)_{1,2}", "$1");
        // Strip headings — turn ## Heading into just Heading
        text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        // Strip markdown links: [text](url) → text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        // Strip horizontal rules
        text = Regex.Replace(text, @"^\s*[-*_]{3,}\s*$", "", RegexOptions.Multiline);
        // Collapse 3+ blank lines down to 2
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }

    private static string NormalizeSlug(string text)
    {
        var sb = new StringBuilder();
        bool lastWasHyphen = false;
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && sb.Length > 0)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }
        // Trim trailing hyphen
        if (sb.Length > 0 && sb[^1] == '-')
            sb.Length--;
        return sb.ToString();
    }

    // ── Content root discovery ───────────────────────────────────────────────

    private static string FindContentRoot()
    {
        // 1. Check alongside the executable (deployed layout).
        string baseDir   = AppDomain.CurrentDomain.BaseDirectory;
        string candidate = Path.Combine(baseDir, "Content");
        if (Directory.Exists(candidate)) return candidate;

        // 2. Walk up the directory tree (development layout: executable is deep in bin/).
        string? dir = baseDir;
        for (int i = 0; i < 8; i++)
        {
            dir = Path.GetDirectoryName(dir);
            if (dir is null) break;

            string c = Path.Combine(dir, "Content");
            if (Directory.Exists(c)) return c;
        }

        // Fallback — callers will see File.Exists return false and fail gracefully.
        return candidate;
    }
}
