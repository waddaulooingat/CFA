using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Manages the full two-voice tutoring remediation flow (§9).
/// Classifies the student's error, retrieves or generates a dialogue script via
/// LlmGatewayService (with a static fallback when Claude is offline), pre-generates
/// all TTS audio, and returns a fully populated DialogueScript ready for playback.
/// Also tracks per-session escalation state across repeated errors.
/// </summary>
public class DialogueEngineService
{
    // -----------------------------------------------------------------------
    // Static fallback catalogue (~15 common geometry error categories)
    // -----------------------------------------------------------------------

    private static readonly Dictionary<string, string> StaticExplanations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["invalid-congruence-SSA"] =
                "SSA (Side-Side-Angle) is not a valid congruence postulate because two different " +
                "triangles can share the same two sides and a non-included angle. To prove triangles " +
                "congruent, use SSS, SAS, ASA, AAS, or HL (right triangles only).",

            ["sign-error"] =
                "Check the signs of your terms carefully. A common mistake is dropping a negative when " +
                "distributing or transposing a term across the equals sign. Remember: subtracting a " +
                "negative is the same as adding a positive.",

            ["off-by-one-arc"] =
                "Arc measure equals the central angle that intercepts it, so a 90° central angle " +
                "creates a 90° arc — not 91° or 89°. Double-check which angle is truly at the center " +
                "of the circle.",

            ["inscribed-angle-half-arc"] =
                "An inscribed angle is exactly half the intercepted arc. If the arc measures 120°, " +
                "the inscribed angle is 60°. Students often confuse inscribed angles with central " +
                "angles, which equal the arc directly.",

            ["exterior-angle-theorem"] =
                "The exterior angle of a triangle equals the sum of the two non-adjacent interior " +
                "angles, not their difference. Add the two remote interior angles to find the exterior " +
                "angle.",

            ["parallel-lines-transversal"] =
                "When a transversal crosses parallel lines, alternate interior angles are equal, " +
                "co-interior (same-side interior) angles are supplementary (sum to 180°), and " +
                "corresponding angles are equal. Be sure you've identified which pair you have.",

            ["pythagorean-theorem-legs"] =
                "In the Pythagorean theorem, c² = a² + b², c must be the hypotenuse — the side " +
                "opposite the right angle. If you solve for a leg, isolate it: a² = c² − b². " +
                "Don't add when you should subtract.",

            ["angle-sum-triangle"] =
                "The three interior angles of any triangle always add up to exactly 180°. If your " +
                "three angles don't sum to 180°, at least one value is incorrect. Check your algebra.",

            ["similar-triangles-ratio"] =
                "In similar triangles, corresponding sides are proportional. Set up your ratio so " +
                "that both fractions use sides from the same position (short-to-long or large-to-small) " +
                "in each triangle before cross-multiplying.",

            ["segment-addition-postulate"] =
                "The Segment Addition Postulate states that if B is between A and C, then " +
                "AB + BC = AC. Make sure the point is actually between the two endpoints before " +
                "writing the equation.",

            ["midpoint-formula"] =
                "The midpoint of a segment with endpoints (x₁, y₁) and (x₂, y₂) is " +
                "((x₁+x₂)/2, (y₁+y₂)/2). A common error is subtracting the coordinates instead " +
                "of adding them before dividing by 2.",

            ["distance-formula"] =
                "The distance between two points is d = √((x₂−x₁)² + (y₂−y₁)²). Both differences " +
                "must be squared before adding — and the square of a negative is positive, so the " +
                "order of subtraction doesn't matter.",

            ["midsegment-theorem"] =
                "A midsegment of a triangle connects the midpoints of two sides and is parallel to " +
                "the third side with half its length. If you know the midsegment length, multiply by " +
                "2 to find the base — not divide.",

            ["complementary-supplementary-swap"] =
                "Complementary angles sum to 90°; supplementary angles sum to 180°. These are " +
                "easy to mix up. A helpful tip: 'C' comes before 'S' in the alphabet, just as 90 " +
                "comes before 180.",

            ["wrong-congruence-postulate"] =
                "You've chosen a congruence postulate that doesn't match the given information. " +
                "List which sides and angles are marked congruent, then check whether those match " +
                "the pattern for SSS, SAS, ASA, AAS, or HL before selecting your postulate.",
        };

    // -----------------------------------------------------------------------
    // Escalation state
    // -----------------------------------------------------------------------

    // Maps errorCategory → how many times dialogue has been played this session
    private readonly Dictionary<string, int> _playCount = new(StringComparer.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly LlmGatewayService _llm;
    private readonly TtsService _tts;
    private readonly SessionLoggerService _logger;
    private readonly DatabaseService _db;

    public DialogueEngineService(
        LlmGatewayService llm,
        TtsService tts,
        SessionLoggerService logger,
        DatabaseService db)
    {
        _llm    = llm    ?? throw new ArgumentNullException(nameof(llm));
        _tts    = tts    ?? throw new ArgumentNullException(nameof(tts));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Full pipeline: classify error → get/generate dialogue → generate all TTS
    /// → return a ready DialogueScript.
    /// <paramref name="errorCategory"/> is the slug from
    /// <c>LlmGatewayService.ClassifyErrorAsync</c>.
    /// Returns <c>null</c> if both LLM and static fallback are unavailable.
    /// </summary>
    public async Task<DialogueScript?> PrepareDialogueAsync(
        string errorCategory,
        string problemId,
        string sceneSpecJson)
    {
        if (string.IsNullOrWhiteSpace(errorCategory))
            throw new ArgumentException("errorCategory must not be empty.", nameof(errorCategory));

        // 1. Check the dialogue cache first (keyed by category + tier).
        EscalationTier tier = GetCurrentTier(errorCategory);
        string cacheKey     = $"{errorCategory}::{(int)tier}";
        DialogueScript? script = TryLoadFromCache(cacheKey);

        if (script is null)
        {
            // 2. Ask the LLM to generate a contextual dialogue script.
            script = await TryGenerateViaLlmAsync(errorCategory, problemId, sceneSpecJson)
                         .ConfigureAwait(false);
        }

        if (script is null)
        {
            // 3. Static fallback: build a minimal two-line script from the dictionary.
            string explanation = GetStaticExplanation(errorCategory);
            if (string.IsNullOrEmpty(explanation))
                return null;   // unknown category and LLM offline — nothing to play

            script = BuildStaticScript(errorCategory, problemId, explanation);
            // Don't cache static fallback — it will be replaced once LLM is available.
        }
        else
        {
            PersistToCache(cacheKey, script);
        }

        // 4. Pre-generate TTS for every line concurrently.
        await _tts.PreGenerateScriptAsync(script).ConfigureAwait(false);

        // 5. Log the dialogue-prepare event.
        _logger.Log("dialogue_prepare", new
        {
            errorCategory,
            tier    = tier.ToString(),
            problemId,
            lineCount = script.Lines.Count,
        });

        return script;
    }

    /// <summary>
    /// Returns the static fallback explanation for a known error category.
    /// Used when Claude is offline.
    /// </summary>
    public string GetStaticExplanation(string errorCategory)
    {
        if (StaticExplanations.TryGetValue(errorCategory, out string? text))
            return text;
        return string.Empty;
    }

    // -----------------------------------------------------------------------
    // Escalation state
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the current escalation tier for the given error category within
    /// the active lesson session.
    /// </summary>
    public EscalationTier GetCurrentTier(string errorCategory)
    {
        _playCount.TryGetValue(errorCategory, out int count);
        return count switch
        {
            0 => EscalationTier.Tier1,
            1 => EscalationTier.Tier2,
            _ => EscalationTier.Tier3,
        };
    }

    /// <summary>
    /// Records that a dialogue was played for the given error category,
    /// escalating the tier on subsequent calls.
    /// </summary>
    public void RecordDialoguePlayed(string errorCategory)
    {
        _playCount.TryGetValue(errorCategory, out int count);
        _playCount[errorCategory] = count + 1;

        _logger.Log("dialogue_play", new
        {
            errorCategory,
            newTier = GetCurrentTier(errorCategory).ToString(),
        });
    }

    /// <summary>
    /// Returns <c>true</c> when the student has reached Tier 3 for this error
    /// category, signalling that a parent/teacher alert should be raised.
    /// </summary>
    public bool ShouldFlagForParent(string errorCategory) =>
        GetCurrentTier(errorCategory) == EscalationTier.Tier3;

    /// <summary>
    /// Resets all escalation state — call at the start of a new lesson.
    /// </summary>
    public void ResetEscalation()
    {
        _playCount.Clear();
        _logger.Log("escalation_reset", new { });
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task<DialogueScript?> TryGenerateViaLlmAsync(
        string errorCategory,
        string problemId,
        string sceneSpecJson)
    {
        try
        {
            return await _llm.GenerateDialogueAsync(
                errorCategory, problemId, sceneSpecJson)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // LLM offline or quota exceeded — log and fall through to static path.
            _logger.Log("llm_dialogue_error", new
            {
                errorCategory,
                error = ex.Message,
            });
            return null;
        }
    }

    private static DialogueScript BuildStaticScript(
        string errorCategory,
        string problemId,
        string explanation)
    {
        return new DialogueScript
        {
            ErrorCategory   = errorCategory,
            RetryProblemId  = problemId,
            Lines           =
            [
                new DialogueLine
                {
                    Speaker = Speaker.Coach,
                    Text    = $"Let's take a closer look at that. {explanation}",
                },
                new DialogueLine
                {
                    Speaker = Speaker.Peer,
                    Text    = "I made the same kind of mistake before. Once you see the pattern, " +
                              "it really clicks. Want to try the problem again?",
                },
            ],
        };
    }

    // -----------------------------------------------------------------------
    // Cache helpers (gt_dialogue_cache)
    // -----------------------------------------------------------------------

    private DialogueScript? TryLoadFromCache(string cacheKey)
    {
        try
        {
            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT script_json FROM gt_dialogue_cache WHERE cache_key = @key LIMIT 1;";
            cmd.Parameters.AddWithValue("@key", cacheKey);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            string json = reader.GetString(0);
            return JsonSerializer.Deserialize<DialogueScript>(json);
        }
        catch
        {
            return null;
        }
    }

    private void PersistToCache(string cacheKey, DialogueScript script)
    {
        try
        {
            string json = JsonSerializer.Serialize(script);
            var conn    = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO gt_dialogue_cache (cache_key, script_json, created_at)
                VALUES (@key, @json, @ts)
                ON CONFLICT(cache_key) DO UPDATE SET script_json = @json, created_at = @ts;
                """;
            cmd.Parameters.AddWithValue("@key",  cacheKey);
            cmd.Parameters.AddWithValue("@json", json);
            cmd.Parameters.AddWithValue("@ts",   DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Cache write failure is non-fatal.
        }
    }
}

/// <summary>Escalation tier for repeated dialogue within a single lesson session.</summary>
public enum EscalationTier
{
    /// <summary>First encounter — standard peer explanation.</summary>
    Tier1,
    /// <summary>Second encounter — deeper coach-led walkthrough.</summary>
    Tier2,
    /// <summary>Third+ encounter — flag for parent/teacher review.</summary>
    Tier3,
}
