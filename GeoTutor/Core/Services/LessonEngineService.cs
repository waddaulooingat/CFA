using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;
using GeoTutor.SceneEngine.Templates;

namespace GeoTutor.Core.Services;

/// <summary>
/// Orchestrates a complete lesson session: builds lesson plans from LLM output,
/// evaluates student answers with mastery-weighted scoring, provides 3-tier hints,
/// constructs post-lesson tests with a calibrated item mix, and scores the results.
/// Falls back to library items when the LLM is unavailable.
/// </summary>
public class LessonEngineService
{
    // Mastery delta deductions per hint revealed (each hint costs 0.25 of the delta)
    private const double HintPenaltyPerReveal = 0.25;

    // Base mastery delta for a correct answer (no hints)
    private const double CorrectBaseDelta   =  0.08;
    private const double IncorrectBaseDelta = -0.06;

    // Post-lesson test item count bounds and type ratios
    private const int  TestMinItems          = 6;
    private const int  TestMaxItems          = 10;
    private const double ProceduralRatio     = 0.40;
    private const double ConceptualRatio     = 0.30;
    // Transfer = 0.30 (implied)

    // Outcome thresholds
    private const double MasteredThreshold   = 0.80;
    private const double DevelopingThreshold = 0.60;

    private readonly LlmGatewayService    _llm;
    private readonly SkillGraphService    _skillGraph;
    private readonly SessionLoggerService _logger;
    private readonly DatabaseService      _db;

    public LessonEngineService(
        LlmGatewayService    llm,
        SkillGraphService    skillGraph,
        SessionLoggerService logger,
        DatabaseService      db)
    {
        _llm        = llm        ?? throw new ArgumentNullException(nameof(llm));
        _skillGraph = skillGraph ?? throw new ArgumentNullException(nameof(skillGraph));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
        _db         = db         ?? throw new ArgumentNullException(nameof(db));
    }

    // -----------------------------------------------------------------------
    // BuildLessonAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Build a <see cref="Lesson"/> for the given skill + band.
    /// Calls <see cref="LlmGatewayService.GenerateLessonPlanAsync"/> to get an LLM-authored
    /// beat sequence. For each beat that references a template the engine attempts to validate
    /// the scene spec via <see cref="TemplateRegistry.TryBuild"/>; invalid specs are dropped.
    /// Falls back to library items from the database when the LLM is unavailable.
    /// </summary>
    public async Task<Lesson> BuildLessonAsync(string skillId, int difficultyBand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        string lessonId = $"lesson:{skillId}:{difficultyBand}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogLessonStart(skillId, lessonId);

        // 1. Try the LLM route.
        LessonPlan? plan = await _llm.GenerateLessonPlanAsync(skillId, difficultyBand)
                                     .ConfigureAwait(false);

        List<Beat> beats;
        if (plan is not null && plan.Beats.Count > 0)
        {
            beats = BuildBeatsFromPlan(plan, skillId, difficultyBand);
        }
        else
        {
            // 2. LLM unavailable — synthesise a minimal lesson from library items.
            beats = BuildFallbackBeats(skillId, difficultyBand);
        }

        var lesson = new Lesson
        {
            Id      = lessonId,
            SkillId = skillId,
            Beats   = beats,
            Version = 1,
        };

        PersistLesson(lesson);
        return lesson;
    }

    // -----------------------------------------------------------------------
    // GetHints
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the 3-tier hint list for an item.  The list is already ordered
    /// least-to-most revealing (as authored by the LLM or the library).
    /// Each reveal costs the student 0.25 × base mastery delta.
    /// </summary>
    public List<string> GetHints(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Hints may be stored directly on the item or need to be parsed from JSON.
        if (item.Hints.Count > 0)
            return item.Hints;

        // Attempt to parse from AnswerJson or a companion field if hints are missing.
        return [];
    }

    // -----------------------------------------------------------------------
    // EvaluateAnswer
    // -----------------------------------------------------------------------

    /// <summary>
    /// Evaluates the student's answer against the item's answer spec.
    /// Returns (isCorrect, masteryDelta). The delta is reduced by 0.25 per hint
    /// revealed before the answer was submitted.
    /// </summary>
    public (bool isCorrect, double masteryDelta) EvaluateAnswer(
        Item item, string userAnswer, int hintsRevealed)
    {
        ArgumentNullException.ThrowIfNull(item);

        bool correct = CheckAnswer(item, userAnswer);

        double baseDelta = correct ? CorrectBaseDelta : IncorrectBaseDelta;

        // Apply hint penalty only on correct answers (hints don't worsen a wrong answer further).
        double penalty = correct
            ? Math.Min(hintsRevealed, 3) * HintPenaltyPerReveal * CorrectBaseDelta
            : 0.0;

        double masteryDelta = baseDelta - penalty;

        return (correct, masteryDelta);
    }

    // -----------------------------------------------------------------------
    // BuildPostLessonTestAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a post-lesson test of 6–10 items split across item types:
    /// 40% Procedural, 30% Conceptual, 30% Transfer.
    /// Tries LLM generation first, then library fallback.
    /// </summary>
    public async Task<List<Item>> BuildPostLessonTestAsync(string skillId, int difficultyBand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        // Target 8 items by default; clamp to [TestMinItems, TestMaxItems].
        int targetTotal = 8;
        int nProcedural = (int)Math.Round(targetTotal * ProceduralRatio);  // 3
        int nConceptual = (int)Math.Round(targetTotal * ConceptualRatio);  // 2
        int nTransfer   = targetTotal - nProcedural - nConceptual;          // 3

        var items = new List<Item>(targetTotal);

        // Helper: generate one item of a given type, falling back to library.
        async Task<Item?> GenerateItemOfType(ItemType type, int index)
        {
            string seed = $"test:{skillId}:{difficultyBand}:{type}:{index}";
            LlmEnvelope? env = await _llm.GenerateItemAsync(skillId, difficultyBand, seed)
                                         .ConfigureAwait(false);
            if (env is not null)
                return EnvelopeToItem(env, skillId, type, seed);

            return FetchLibraryItem(skillId, difficultyBand, type);
        }

        // Build item tasks for each type bucket.
        var tasks = new List<Task<Item?>>();
        for (int i = 0; i < nProcedural; i++) tasks.Add(GenerateItemOfType(ItemType.Procedural, i));
        for (int i = 0; i < nConceptual; i++) tasks.Add(GenerateItemOfType(ItemType.Conceptual, i));
        for (int i = 0; i < nTransfer;   i++) tasks.Add(GenerateItemOfType(ItemType.Transfer,   i));

        Item?[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        foreach (var item in results)
        {
            if (item is not null)
                items.Add(item);
        }

        // Clamp to [TestMinItems, TestMaxItems] — truncate if over, no-op if under.
        if (items.Count > TestMaxItems)
            items = items.Take(TestMaxItems).ToList();

        // Shuffle so item types aren't grouped.
        Shuffle(items);

        return items;
    }

    // -----------------------------------------------------------------------
    // ScoreTest
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scores a completed test.  Returns (percentCorrect, lessonOutcome):
    /// <list type="bullet">
    ///   <item>Mastered   — ≥ 80%</item>
    ///   <item>Developing — 60–79%</item>
    ///   <item>NeedsRevisit — &lt; 60%</item>
    /// </list>
    /// </summary>
    public (double percentCorrect, LessonOutcome outcome) ScoreTest(
        List<(Item item, bool correct)> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
            return (0.0, LessonOutcome.NeedsRevisit);

        double percentCorrect = (double)results.Count(r => r.correct) / results.Count;

        LessonOutcome outcome = percentCorrect >= MasteredThreshold
            ? LessonOutcome.Mastered
            : percentCorrect >= DevelopingThreshold
                ? LessonOutcome.Developing
                : LessonOutcome.NeedsRevisit;

        _logger.Log("test_scored", new { percentCorrect, outcome = outcome.ToString() });

        return (percentCorrect, outcome);
    }

    // -----------------------------------------------------------------------
    // Private: beat construction
    // -----------------------------------------------------------------------

    private List<Beat> BuildBeatsFromPlan(LessonPlan plan, string skillId, int difficultyBand)
    {
        var beats = new List<Beat>(plan.Beats.Count);

        foreach (var descriptor in plan.Beats)
        {
            var beat = new Beat
            {
                Type  = descriptor.Type,
                Prose = descriptor.Prose,
            };

            // Validate the scene spec if a template is specified.
            if (!string.IsNullOrWhiteSpace(descriptor.TemplateRef))
            {
                Dictionary<string, object> paramDict;
                try
                {
                    paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        descriptor.ParamsJson) ?? [];
                }
                catch
                {
                    paramDict = [];
                }

                if (TemplateRegistry.TryBuild(descriptor.TemplateRef, paramDict,
                        out var sceneSpec, out _) && sceneSpec is not null)
                {
                    string specId = PersistSceneSpec(sceneSpec, descriptor.TemplateRef);
                    beat.SceneSpecId = specId;
                }
                // If TryBuild fails the beat is still included — just without a scene spec.
            }

            // Attach item references generated for this beat.
            beat.ItemIds = GenerateBeatItemIds(skillId, difficultyBand,
                descriptor.Type, descriptor.ItemCount);

            beats.Add(beat);
        }

        return beats;
    }

    private List<Beat> BuildFallbackBeats(string skillId, int difficultyBand)
    {
        // Minimal 3-beat structure: Concept → Practice → Reflection
        var libraryItems = FetchLibraryItems(skillId, difficultyBand, 4);
        var itemIds      = libraryItems.Select(i => i.Id).ToList();

        return
        [
            new Beat
            {
                Type     = BeatType.Concept,
                Prose    = $"Introducing {skillId} at difficulty band {difficultyBand}.",
                ItemIds  = [],
            },
            new Beat
            {
                Type     = BeatType.Practice,
                Prose    = "Work through these practice problems.",
                ItemIds  = itemIds,
            },
            new Beat
            {
                Type     = BeatType.Reflection,
                Prose    = "Review what you've learned.",
                ItemIds  = [],
            },
        ];
    }

    private List<string> GenerateBeatItemIds(
        string skillId, int difficultyBand, BeatType beatType, int count)
    {
        // For non-practice beats return an empty list (prose/scene only).
        if (beatType is BeatType.Concept or BeatType.Reflection)
            return [];

        var ids = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            // Use a deterministic seed so the same beat always maps to the same item.
            string seed = $"{skillId}:{difficultyBand}:{beatType}:{i}";
            ids.Add(seed);   // IDs are generated on demand when the beat is rendered.
        }
        return ids;
    }

    // -----------------------------------------------------------------------
    // Private: answer evaluation
    // -----------------------------------------------------------------------

    private static bool CheckAnswer(Item item, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(item.AnswerJson);
            var root = doc.RootElement;

            // Multiple-choice answer: compare correct_index to user's integer input.
            if (root.TryGetProperty("correct_index", out var ciEl) ||
                root.TryGetProperty("correctIndex",  out ciEl))
            {
                if (int.TryParse(userAnswer.Trim(), out int userIdx))
                    return userIdx == ciEl.GetInt32();
                return false;
            }

            // Numeric answer with optional tolerance.
            if (root.TryGetProperty("value", out var valEl))
            {
                if (!double.TryParse(userAnswer.Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double userVal))
                    return false;

                double expected  = valEl.GetDouble();
                double tolerance = 0.0;
                if (root.TryGetProperty("tolerance", out var tolEl))
                    tolerance = tolEl.GetDouble();

                return Math.Abs(userVal - expected) <= tolerance;
            }
        }
        catch
        {
            // Fall through to string comparison.
        }

        // Last resort: exact string match (case-insensitive, trimmed).
        return string.Equals(userAnswer.Trim(), item.AnswerJson.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Private: item helpers
    // -----------------------------------------------------------------------

    private Item EnvelopeToItem(
        LlmEnvelope envelope, string skillId, ItemType type, string seed)
    {
        string answerJson = JsonSerializer.Serialize(envelope.Answer, new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        string paramsJson = JsonSerializer.Serialize(envelope.Params);

        var item = new Item
        {
            Id            = $"llm:{seed}:{Guid.NewGuid():N}",
            Template      = envelope.Template,
            ParamsJson    = paramsJson,
            Prompt        = envelope.Prompt,
            AnswerJson    = answerJson,
            Hints         = envelope.Hints,
            SolutionSteps = envelope.SolutionSteps,
            SkillId       = skillId,
            Difficulty    = envelope.Difficulty,
            Source        = "llm",
            Type          = type,
        };

        PersistItem(item);
        return item;
    }

    private Item? FetchLibraryItem(string skillId, int difficultyBand, ItemType type)
    {
        var items = FetchLibraryItems(skillId, difficultyBand, 1, type);
        return items.Count > 0 ? items[0] : null;
    }

    private List<Item> FetchLibraryItems(
        string skillId, int difficultyBand, int limit, ItemType? type = null)
    {
        var conn   = _db.GetConnection();
        var result = new List<Item>();

        using var cmd = conn.CreateCommand();

        if (type.HasValue)
        {
            cmd.CommandText = """
                SELECT id, template, params_json, prompt, answer_json,
                       hints_json, solution_steps_json, skill_id, difficulty, source, item_type
                FROM gt_items
                WHERE skill_id = @sid
                  AND difficulty = @band
                  AND item_type  = @type
                  AND source     = 'library'
                ORDER BY RANDOM()
                LIMIT @lim;
                """;
            cmd.Parameters.AddWithValue("@type", type.Value.ToString());
        }
        else
        {
            cmd.CommandText = """
                SELECT id, template, params_json, prompt, answer_json,
                       hints_json, solution_steps_json, skill_id, difficulty, source, item_type
                FROM gt_items
                WHERE skill_id = @sid
                  AND difficulty = @band
                  AND source     = 'library'
                ORDER BY RANDOM()
                LIMIT @lim;
                """;
        }

        cmd.Parameters.AddWithValue("@sid",  skillId);
        cmd.Parameters.AddWithValue("@band", difficultyBand);
        cmd.Parameters.AddWithValue("@lim",  limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadItem(reader));

        return result;
    }

    // -----------------------------------------------------------------------
    // Private: persistence helpers
    // -----------------------------------------------------------------------

    private string PersistSceneSpec(GeoTutor.SceneEngine.Models.SceneSpec spec, string templateRef)
    {
        string id  = $"spec:{templateRef}:{Guid.NewGuid():N}";
        string json = JsonSerializer.Serialize(spec);

        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO gt_scene_specs (id, spec_json, created_at)
            VALUES (@id, @json, @ts);
            """;
        cmd.Parameters.AddWithValue("@id",   id);
        cmd.Parameters.AddWithValue("@json", json);
        cmd.Parameters.AddWithValue("@ts",   DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return id;
    }

    private void PersistItem(Item item)
    {
        try
        {
            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR IGNORE INTO gt_items
                    (id, template, params_json, prompt, answer_json,
                     hints_json, solution_steps_json, skill_id, difficulty, source, item_type)
                VALUES
                    (@id, @tpl, @params, @prompt, @ans,
                     @hints, @steps, @skill, @diff, @src, @type);
                """;
            cmd.Parameters.AddWithValue("@id",     item.Id);
            cmd.Parameters.AddWithValue("@tpl",    item.Template);
            cmd.Parameters.AddWithValue("@params", item.ParamsJson);
            cmd.Parameters.AddWithValue("@prompt", item.Prompt);
            cmd.Parameters.AddWithValue("@ans",    item.AnswerJson);
            cmd.Parameters.AddWithValue("@hints",  JsonSerializer.Serialize(item.Hints));
            cmd.Parameters.AddWithValue("@steps",  JsonSerializer.Serialize(item.SolutionSteps));
            cmd.Parameters.AddWithValue("@skill",  item.SkillId);
            cmd.Parameters.AddWithValue("@diff",   item.Difficulty);
            cmd.Parameters.AddWithValue("@src",    item.Source);
            cmd.Parameters.AddWithValue("@type",   item.Type.ToString());
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Persistence failure is non-fatal — item is still usable in memory.
        }
    }

    private void PersistLesson(Lesson lesson)
    {
        try
        {
            string beatJson = JsonSerializer.Serialize(lesson.Beats);
            var conn        = _db.GetConnection();
            using var cmd   = conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO gt_lessons (id, skill_id, beat_sequence, version)
                VALUES (@id, @sid, @beats, @ver);
                """;
            cmd.Parameters.AddWithValue("@id",    lesson.Id);
            cmd.Parameters.AddWithValue("@sid",   lesson.SkillId);
            cmd.Parameters.AddWithValue("@beats", beatJson);
            cmd.Parameters.AddWithValue("@ver",   lesson.Version);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Non-fatal.
        }
    }

    // -----------------------------------------------------------------------
    // Private: SQLite row → Item
    // -----------------------------------------------------------------------

    private static Item ReadItem(SqliteDataReader r)
    {
        List<string> hints, steps;
        try { hints = JsonSerializer.Deserialize<List<string>>(r.GetString(5)) ?? []; }
        catch { hints = []; }
        try { steps = JsonSerializer.Deserialize<List<string>>(r.GetString(6)) ?? []; }
        catch { steps = []; }

        ItemType itemType = ItemType.Procedural;
        if (Enum.TryParse<ItemType>(r.GetString(10), out var parsed))
            itemType = parsed;

        return new Item
        {
            Id            = r.GetString(0),
            Template      = r.GetString(1),
            ParamsJson    = r.GetString(2),
            Prompt        = r.GetString(3),
            AnswerJson    = r.GetString(4),
            Hints         = hints,
            SolutionSteps = steps,
            SkillId       = r.GetString(7),
            Difficulty    = r.GetInt32(8),
            Source        = r.GetString(9),
            Type          = itemType,
        };
    }

    // -----------------------------------------------------------------------
    // Private: shuffle utility
    // -----------------------------------------------------------------------

    private static void Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// LessonOutcome is defined in GeoTutor.Core.Models.LessonOutcome.
