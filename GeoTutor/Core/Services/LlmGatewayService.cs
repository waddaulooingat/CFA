using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Thin gateway to the Anthropic Messages API.
/// All generation calls use tool_use to force structured JSON output.
/// Retries up to 3 times with exponential backoff (1s, 2s, 4s).
/// Returns null on persistent failure so callers can use library items.
/// </summary>
public class LlmGatewayService
{
    private const string ApiBase        = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVer   = "2023-06-01";
    private const string SonnetModel    = "claude-sonnet-4-5";
    private const string HaikuModel     = "claude-haiku-4-5";
    private const int    MaxRetries     = 3;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = false,
    };

    private readonly HttpClient      _http;
    private readonly DatabaseService _db;
    private readonly string          _apiKey;

    public LlmGatewayService(DatabaseService db, string apiKey)
    {
        _db     = db ?? throw new ArgumentNullException(nameof(db));
        _apiKey = apiKey ?? "";   // empty string = offline mode; callers receive null returns

        _http = new HttpClient();
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", AnthropicVer);
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
        _http.Timeout = TimeSpan.FromSeconds(90);
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generate a lesson plan for <paramref name="skillId"/> at the given
    /// <paramref name="difficultyBand"/>. Cached in gt_lesson_plans by
    /// (skillId, band). Returns null when the API is unavailable after retries.
    /// </summary>
    public async Task<LessonPlan?> GenerateLessonPlanAsync(string skillId, int difficultyBand)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        string cacheKey = $"{skillId}:{difficultyBand}";
        var cached = LoadCachedLessonPlan(cacheKey);
        if (cached is not null)
            return cached;

        const string system = """
            You are an expert honors-geometry curriculum designer.
            Produce a structured lesson plan as a JSON object matching the tool schema.
            Each beat must have a clear pedagogical purpose and flow from concept introduction
            through worked examples to independent practice.
            BeatType values: Concept, Manipulate, WorkedExample, Check, Practice, Reflection.
            Keep Prose concise (1-3 sentences). TemplateRef must be a valid geometry template name
            or empty string. ParamsJson must be a compact JSON object or {}.
            """;

        string user = $"""
            Create a lesson plan for skill '{skillId}' at difficulty band {difficultyBand} (1=easiest, 5=hardest).
            Include 5-8 beats that progress logically through the skill.
            """;

        var tool = BuildTool("produce_lesson_plan",
            "Output a complete lesson plan for the requested geometry skill.",
            LessonPlanSchema());

        var json = await CallWithRetryAsync(SonnetModel, system, user, tool);
        if (json is null) return null;

        try
        {
            var plan = JsonSerializer.Deserialize<LessonPlan>(json, _jsonOpts);
            if (plan is null) return null;
            plan.SkillId       = skillId;
            plan.DifficultyBand = difficultyBand;
            CacheLessonPlan(cacheKey, plan);
            return plan;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generate an item envelope (template + params + prompt + answer + hints).
    /// Cached by (skillId, band, seed) in gt_item_envelopes.
    /// Returns null on API failure.
    /// </summary>
    public async Task<LlmEnvelope?> GenerateItemAsync(
        string skillId, int difficultyBand, string seed = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        string cacheKey = $"{skillId}:{difficultyBand}:{seed}";
        var cached = LoadCachedEnvelope(cacheKey);
        if (cached is not null)
            return cached;

        const string system = """
            You are an expert geometry problem author.
            Produce a single geometry problem as a JSON object matching the tool schema.
            Template must be one of: GenericTriangle, RightTriangle, IsoscelesTriangle,
            EquilateralTriangle, ParallelLinesTransversal, TwoCongruentTriangles,
            SimilarTriangles, Circle, CircleWithChord, CircleWithInscribedAngle,
            CircleWithTangent, Parallelogram, Trapezoid, CoordinatePolygon, TransformationPair.
            Params must match that template's expected parameters as a JSON object.
            Prompt is the question shown to the student (plain text).
            Answer: set Value+Tolerance for numeric answers, or Choices+CorrectIndex for multiple-choice.
            SolutionSteps: 2-5 numbered steps showing complete reasoning.
            Hints: exactly 3, ordered from least to most revealing.
            SkillTag matches the skillId. Difficulty is 1-5.
            """;

        string user = $"""
            Create one geometry problem for skill '{skillId}' at difficulty band {difficultyBand}.
            Seed: '{seed}'. Use the seed to vary the specific numbers or configuration.
            """;

        var tool = BuildTool("produce_item_envelope",
            "Output a complete geometry item envelope.",
            LlmEnvelopeSchema());

        var json = await CallWithRetryAsync(SonnetModel, system, user, tool);
        if (json is null) return null;

        try
        {
            var envelope = JsonSerializer.Deserialize<LlmEnvelope>(json, _jsonOpts);
            if (envelope is null) return null;
            CacheEnvelope(cacheKey, envelope);
            return envelope;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Classify a wrong answer into a short error-category slug (e.g. "sign-error").
    /// Uses Haiku for economy. Not cached.
    /// </summary>
    public async Task<string> ClassifyErrorAsync(
        string prompt, string studentAnswer, string correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return "sign-error";

        string user = $"""
            Geometry problem: {prompt}
            Student answer:   {studentAnswer}
            Correct answer:   {correctAnswer}

            Classify the student's error with a single short slug (e.g. sign-error,
            wrong-formula, arithmetic, angle-misidentification, unit-error, conceptual,
            rounding, other). Reply with only the slug, no explanation.
            """;

        var result = await CallPlainTextWithRetryAsync(HaikuModel, null, user);
        return result?.Trim().ToLowerInvariant() ?? "other";
    }

    /// <summary>
    /// Generate a Coach/Peer dialogue script for an error category + problem.
    /// Cached by (errorCategory + ':' + problemId) in gt_dialogue_cache.
    /// Returns null on API failure.
    /// </summary>
    public async Task<DialogueScript?> GenerateDialogueAsync(
        string errorCategory, string problemId, string sceneSpecJson)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        string cacheKey = $"{errorCategory}:{problemId}";
        var cached = LoadCachedDialogue(cacheKey);
        if (cached is not null)
            return cached;

        const string system = """
            You are a warm, encouraging geometry tutor writing a short coach/peer dialogue
            for a student who just made a specific type of error.
            The dialogue should gently guide the student to understand their mistake
            without giving the answer away immediately.
            Speaker values: Coach or Peer.
            CanvasAction types: highlight, morph, annotate, loadScene (optional — omit if not useful).
            Write 4-8 dialogue lines. RetryProblemId should equal the original problem id.
            """;

        string user = $"""
            Error category: {errorCategory}
            Problem id: {problemId}
            Scene context: {sceneSpecJson}

            Write a short coaching dialogue that helps the student understand and correct
            their '{errorCategory}' error.
            """;

        var tool = BuildTool("produce_dialogue_script",
            "Output a dialogue script for tutoring a geometry error.",
            DialogueScriptSchema());

        var json = await CallWithRetryAsync(SonnetModel, system, user, tool);
        if (json is null) return null;

        try
        {
            var script = JsonSerializer.Deserialize<DialogueScript>(json, _jsonOpts);
            if (script is null) return null;
            CacheDialogue(cacheKey, script);
            return script;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Overload called by <see cref="DialogueEngineService"/> that accepts an escalation
    /// tier, using it to vary the dialogue depth and tone.
    /// Delegates to <see cref="GenerateDialogueAsync"/> after embedding the tier in the
    /// scene spec JSON so the system prompt can calibrate verbosity.
    /// </summary>
    public Task<DialogueScript?> GenerateDialogueScriptAsync(
        string errorCategory, string problemId, string sceneSpecJson, EscalationTier tier)
    {
        // Embed the escalation tier into the scene context so the LLM knows how deep to go.
        string augmented = System.Text.Json.JsonSerializer.Serialize(new
        {
            sceneSpec = sceneSpecJson,
            escalationTier = tier.ToString(),
        });
        return GenerateDialogueAsync(errorCategory, problemId, augmented);
    }

    /// <summary>
    /// Generate a short lesson recap (1-3 sentences). Uses Haiku. Not cached.
    /// </summary>
    public async Task<string> GenerateLessonRecapAsync(
        string skillName, List<string> conceptsCovered)
    {
        string concepts = string.Join(", ", conceptsCovered);
        string user = $"""
            Geometry skill: {skillName}
            Concepts covered in today's lesson: {concepts}

            Write a 1-3 sentence recap of what the student learned, suitable for display
            at the end of a lesson. Be encouraging and specific. Plain text only.
            """;

        var result = await CallPlainTextWithRetryAsync(HaikuModel, null, user);
        return result?.Trim() ?? $"Great work on {skillName} today!";
    }

    // -----------------------------------------------------------------------
    // Core HTTP helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Send a Messages API request with a single forced-tool schema.
    /// Returns the parsed tool_use input JSON string, or null on failure.
    /// Retries up to MaxRetries with exponential backoff.
    /// </summary>
    private async Task<string?> CallWithRetryAsync(
        string model, string? system, string userMessage, object tool)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));  // 1s, 2s, 4s

            try
            {
                string? result = await SendStructuredAsync(model, system, userMessage, tool);
                if (result is not null)
                    return result;
            }
            catch (Exception)
            {
                // Will retry; on final attempt fall through to return null
            }
        }
        return null;
    }

    /// <summary>
    /// Send a Messages API request expecting plain text content.
    /// Returns trimmed content string, or null on failure.
    /// </summary>
    private async Task<string?> CallPlainTextWithRetryAsync(
        string model, string? system, string userMessage)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));

            try
            {
                string? result = await SendPlainAsync(model, system, userMessage);
                if (result is not null)
                    return result;
            }
            catch (Exception)
            {
                // Will retry
            }
        }
        return null;
    }

    private async Task<string?> SendStructuredAsync(
        string model, string? system, string userMessage, object toolObj)
    {
        // Serialize the entire request body as a JsonNode so we can compose freely.
        var body = new JsonObject();
        body["model"]      = model;
        body["max_tokens"] = 4096;

        if (!string.IsNullOrEmpty(system))
            body["system"] = system;

        body["messages"] = new JsonArray
        {
            new JsonObject
            {
                ["role"]    = "user",
                ["content"] = userMessage,
            }
        };

        // Serialize the tool definition through our JsonSerializerOptions so that
        // property names are snake_case.
        var toolJson  = JsonSerializer.Serialize(toolObj, _jsonOpts);
        var toolNode  = JsonNode.Parse(toolJson)!;
        body["tools"] = new JsonArray { toolNode };

        // Force the model to call our single tool.
        body["tool_choice"] = new JsonObject
        {
            ["type"] = "tool",
            ["name"] = (string?)toolNode["name"]!,
        };

        var content = new StringContent(
            body.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync(ApiBase, content);
        if (!response.IsSuccessStatusCode)
            return null;

        var responseBody = await response.Content.ReadAsStringAsync();
        return ExtractToolUseInput(responseBody);
    }

    private async Task<string?> SendPlainAsync(
        string model, string? system, string userMessage)
    {
        var body = new JsonObject();
        body["model"]      = model;
        body["max_tokens"] = 512;

        if (!string.IsNullOrEmpty(system))
            body["system"] = system;

        body["messages"] = new JsonArray
        {
            new JsonObject
            {
                ["role"]    = "user",
                ["content"] = userMessage,
            }
        };

        var content = new StringContent(
            body.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync(ApiBase, content);
        if (!response.IsSuccessStatusCode)
            return null;

        var responseBody = await response.Content.ReadAsStringAsync();
        return ExtractTextContent(responseBody);
    }

    // -----------------------------------------------------------------------
    // Response parsing
    // -----------------------------------------------------------------------

    /// <summary>
    /// Pull the JSON string from the first tool_use content block's input field.
    /// </summary>
    private static string? ExtractToolUseInput(string responseBody)
    {
        try
        {
            using var doc  = JsonDocument.Parse(responseBody);
            var root       = doc.RootElement;
            if (!root.TryGetProperty("content", out var contentArr))
                return null;

            foreach (var block in contentArr.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out var typeEl)) continue;
                if (typeEl.GetString() != "tool_use") continue;

                if (!block.TryGetProperty("input", out var inputEl))
                    return null;

                // Re-serialize the input element as a raw JSON string.
                return inputEl.GetRawText();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Pull the text from the first text content block.
    /// </summary>
    private static string? ExtractTextContent(string responseBody)
    {
        try
        {
            using var doc  = JsonDocument.Parse(responseBody);
            var root       = doc.RootElement;
            if (!root.TryGetProperty("content", out var contentArr))
                return null;

            foreach (var block in contentArr.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out var typeEl)) continue;
                if (typeEl.GetString() != "text") continue;
                if (!block.TryGetProperty("text", out var textEl)) continue;
                return textEl.GetString();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Tool schema builders
    // -----------------------------------------------------------------------

    private static object BuildTool(string name, string description, object inputSchema)
        => new { name, description, input_schema = inputSchema };

    private static object LessonPlanSchema() => new
    {
        type = "object",
        properties = new
        {
            skill_id = new { type = "string", description = "The skill identifier." },
            difficulty_band = new { type = "integer", description = "Difficulty band 1-5." },
            beats = new
            {
                type  = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        type         = new { type = "string", @enum = new[] { "Concept", "Manipulate", "WorkedExample", "Check", "Practice", "Reflection" } },
                        template_ref = new { type = "string", description = "Template name or empty string." },
                        params_json  = new { type = "string", description = "JSON object of template params or {}." },
                        prose        = new { type = "string", description = "1-3 sentence description of the beat." },
                        item_count   = new { type = "integer", description = "Number of items for this beat.", minimum = 1, maximum = 10 },
                    },
                    required = new[] { "type", "prose", "item_count" },
                }
            }
        },
        required = new[] { "skill_id", "difficulty_band", "beats" }
    };

    private static object LlmEnvelopeSchema() => new
    {
        type = "object",
        properties = new
        {
            template = new { type = "string", description = "Template name from the allowed list." },
            @params  = new { type = "object", description = "Template parameters object.", additionalProperties = true },
            prompt   = new { type = "string", description = "Question text shown to the student." },
            answer   = new
            {
                type = "object",
                properties = new
                {
                    value         = new { type = "number",              description = "Numeric answer (if numeric type)." },
                    tolerance     = new { type = "number",              description = "Acceptable tolerance around value." },
                    choices       = new { type = "array", items = new { type = "string" }, description = "Multiple-choice options." },
                    correct_index = new { type = "integer",             description = "0-based index of the correct choice." },
                },
            },
            solution_steps = new { type = "array", items = new { type = "string" }, description = "Step-by-step solution." },
            hints          = new { type = "array", items = new { type = "string" }, description = "Exactly 3 hints, least to most revealing.", minItems = 3, maxItems = 3 },
            skill_tag      = new { type = "string", description = "Skill identifier." },
            difficulty     = new { type = "integer", description = "Difficulty 1-5.", minimum = 1, maximum = 5 },
        },
        required = new[] { "template", "params", "prompt", "answer", "solution_steps", "hints", "skill_tag", "difficulty" }
    };

    private static object DialogueScriptSchema() => new
    {
        type = "object",
        properties = new
        {
            error_category    = new { type = "string", description = "Error category slug." },
            retry_problem_id  = new { type = "string", description = "The id of the problem to retry." },
            lines             = new
            {
                type  = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        speaker = new { type = "string", @enum = new[] { "Coach", "Peer" } },
                        text    = new { type = "string", description = "Dialogue text." },
                        action  = new
                        {
                            type = "object",
                            properties = new
                            {
                                type   = new { type = "string", @enum = new[] { "highlight", "morph", "annotate", "loadScene" } },
                                target = new { type = "string" },
                                to     = new { type = "string" },
                                label  = new { type = "string" },
                            },
                            required = new[] { "type" },
                        }
                    },
                    required = new[] { "speaker", "text" },
                }
            }
        },
        required = new[] { "error_category", "retry_problem_id", "lines" }
    };

    // -----------------------------------------------------------------------
    // Cache helpers — SQLite
    // -----------------------------------------------------------------------

    private void EnsureLlmCacheTables()
    {
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS gt_lesson_plans (
                cache_key   TEXT PRIMARY KEY,
                plan_json   TEXT NOT NULL,
                created_at  TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS gt_item_envelopes (
                cache_key      TEXT PRIMARY KEY,
                envelope_json  TEXT NOT NULL,
                created_at     TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private LessonPlan? LoadCachedLessonPlan(string cacheKey)
    {
        EnsureLlmCacheTables();
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = "SELECT plan_json FROM gt_lesson_plans WHERE cache_key = @k;";
        cmd.Parameters.AddWithValue("@k", cacheKey);
        var raw = cmd.ExecuteScalar() as string;
        if (raw is null) return null;
        try { return JsonSerializer.Deserialize<LessonPlan>(raw, _jsonOpts); }
        catch { return null; }
    }

    private void CacheLessonPlan(string cacheKey, LessonPlan plan)
    {
        EnsureLlmCacheTables();
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO gt_lesson_plans (cache_key, plan_json, created_at)
            VALUES (@k, @j, @ts);
            """;
        cmd.Parameters.AddWithValue("@k",  cacheKey);
        cmd.Parameters.AddWithValue("@j",  JsonSerializer.Serialize(plan, _jsonOpts));
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private LlmEnvelope? LoadCachedEnvelope(string cacheKey)
    {
        EnsureLlmCacheTables();
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = "SELECT envelope_json FROM gt_item_envelopes WHERE cache_key = @k;";
        cmd.Parameters.AddWithValue("@k", cacheKey);
        var raw = cmd.ExecuteScalar() as string;
        if (raw is null) return null;
        try { return JsonSerializer.Deserialize<LlmEnvelope>(raw, _jsonOpts); }
        catch { return null; }
    }

    private void CacheEnvelope(string cacheKey, LlmEnvelope envelope)
    {
        EnsureLlmCacheTables();
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO gt_item_envelopes (cache_key, envelope_json, created_at)
            VALUES (@k, @j, @ts);
            """;
        cmd.Parameters.AddWithValue("@k",  cacheKey);
        cmd.Parameters.AddWithValue("@j",  JsonSerializer.Serialize(envelope, _jsonOpts));
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private DialogueScript? LoadCachedDialogue(string cacheKey)
    {
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = "SELECT script_json FROM gt_dialogue_cache WHERE cache_key = @k;";
        cmd.Parameters.AddWithValue("@k", cacheKey);
        var raw = cmd.ExecuteScalar() as string;
        if (raw is null) return null;
        try { return JsonSerializer.Deserialize<DialogueScript>(raw, _jsonOpts); }
        catch { return null; }
    }

    private void CacheDialogue(string cacheKey, DialogueScript script)
    {
        using var cmd = _db.GetConnection().CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO gt_dialogue_cache (cache_key, script_json, created_at)
            VALUES (@k, @j, @ts);
            """;
        cmd.Parameters.AddWithValue("@k",  cacheKey);
        cmd.Parameters.AddWithValue("@j",  JsonSerializer.Serialize(script, _jsonOpts));
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }
}
