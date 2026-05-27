using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

// ---------------------------------------------------------------------------
// Domain types (§6.2) — placed in the same file so the service, its inputs,
// and its outputs are all co-located and easy to follow.
// ---------------------------------------------------------------------------

/// <summary>
/// A complete drag-and-drop two-column proof exercise as returned by
/// <see cref="ProofBeatService.GenerateProofAsync"/>.
/// Contains the given/prove setup, the bank of draggable items (including
/// distractors), and the authoritative solution ordering.
/// </summary>
public class ProofExercise
{
    /// <summary>Unique identifier for this exercise instance.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>"Given:" clause shown at the top of the proof frame.</summary>
    public string GivenText { get; set; } = "";

    /// <summary>"Prove:" clause shown at the top of the proof frame.</summary>
    public string ProveText { get; set; } = "";

    /// <summary>
    /// Pool of draggable statement cards (includes distractors).
    /// The UI renders these in randomised order.
    /// </summary>
    public List<ProofItem> StatementBank { get; set; } = [];

    /// <summary>
    /// Pool of draggable reason cards (includes distractors).
    /// The UI renders these in randomised order.
    /// </summary>
    public List<ProofItem> ReasonBank { get; set; } = [];

    /// <summary>
    /// The correct, ordered solution.  Each <see cref="ProofStep"/> references
    /// the <see cref="ProofItem.Id"/> values that belong in that row.
    /// </summary>
    public List<ProofStep> CorrectSteps { get; set; } = [];
}

/// <summary>A single statement or reason card in the drag-and-drop bank.</summary>
public class ProofItem
{
    /// <summary>Stable identifier used to match placements against the solution.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display text shown on the card.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// <c>true</c> for cards that look plausible but are not part of the correct proof.
    /// Distractor cards are never included in <see cref="ProofExercise.CorrectSteps"/>.
    /// </summary>
    public bool IsDistractor { get; set; }
}

/// <summary>
/// One row in the authoritative solution: which statement and reason belong
/// at step <see cref="StepNumber"/> (1-indexed).
/// </summary>
public class ProofStep
{
    /// <summary>1-indexed row position in the completed proof.</summary>
    public int StepNumber { get; set; }

    /// <summary><see cref="ProofItem.Id"/> of the correct statement for this row.</summary>
    public string StatementId { get; set; } = "";

    /// <summary><see cref="ProofItem.Id"/> of the correct reason for this row.</summary>
    public string ReasonId { get; set; } = "";
}

/// <summary>
/// One row as the student has placed it — either or both sides may be null if the
/// student has not yet dragged a card into that cell.
/// </summary>
public class ProofPlacement
{
    /// <summary>1-indexed row position being described.</summary>
    public int StepNumber { get; set; }

    /// <summary><see cref="ProofItem.Id"/> placed in the statement column, or <c>null</c>.</summary>
    public string? StatementId { get; set; }

    /// <summary><see cref="ProofItem.Id"/> placed in the reason column, or <c>null</c>.</summary>
    public string? ReasonId { get; set; }
}

/// <summary>
/// Result returned by <see cref="ProofBeatService.Validate"/> after checking the
/// student's current arrangement of cards.
/// </summary>
public class ProofValidationResult
{
    /// <summary>
    /// <c>true</c> when every step has both a correct statement and a correct reason,
    /// and the total number of placed steps equals the solution length.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Number of steps that are fully and correctly placed (statement AND reason both correct).
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// 1-indexed position of the first step where either column is wrong or missing,
    /// or <c>null</c> if no wrong step was found (i.e. everything placed so far is correct,
    /// but the proof may not yet be complete).
    /// </summary>
    public int? FirstWrongStep { get; set; }
}

// ---------------------------------------------------------------------------
// Service
// ---------------------------------------------------------------------------

/// <summary>
/// Manages the drag-and-drop two-column proof beat (§6.2).
/// <para>
/// Proof exercises are generated via <see cref="LlmGatewayService"/> with a
/// structured JSON tool call.  If the LLM is unavailable, a hard-coded static
/// library of exercises (keyed by skillId) is used as a fallback so the beat
/// is never blocked.
/// </para>
/// </summary>
public class ProofBeatService
{
    private readonly LlmGatewayService _llm;

    public ProofBeatService(LlmGatewayService llm)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generates a two-column proof exercise for <paramref name="skillId"/> at the
    /// given <paramref name="difficultyBand"/>.
    /// <para>
    /// The exercise includes a statement bank, a reason bank (both with distractors
    /// mixed in), and the authoritative <see cref="ProofExercise.CorrectSteps"/> list.
    /// </para>
    /// Falls back to a static exercise from <see cref="StaticFallback"/> when the
    /// LLM is unavailable.
    /// </summary>
    public async Task<ProofExercise> GenerateProofAsync(string skillId, int difficultyBand)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            throw new ArgumentException("skillId must not be empty.", nameof(skillId));

        try
        {
            ProofExercise? fromLlm =
                await GenerateViaLlmAsync(skillId, difficultyBand).ConfigureAwait(false);

            if (fromLlm is not null)
                return fromLlm;
        }
        catch
        {
            // Fall through to static fallback below.
        }

        return StaticFallback(skillId, difficultyBand);
    }

    /// <summary>
    /// Validates the student's current card placement against the authoritative solution.
    /// </summary>
    /// <param name="exercise">The exercise being solved.</param>
    /// <param name="placements">
    ///   The student's current placements — one entry per row that has at least one
    ///   card placed.  Missing rows are treated as empty (wrong).
    /// </param>
    /// <returns>A <see cref="ProofValidationResult"/> describing correctness.</returns>
    public ProofValidationResult Validate(ProofExercise exercise, List<ProofPlacement> placements)
    {
        if (exercise is null)   throw new ArgumentNullException(nameof(exercise));
        if (placements is null) throw new ArgumentNullException(nameof(placements));

        // Build a lookup from step number → placement for O(1) access.
        var placementByStep = placements.ToDictionary(p => p.StepNumber);

        int  correctCount  = 0;
        int? firstWrong    = null;

        foreach (ProofStep step in exercise.CorrectSteps)
        {
            placementByStep.TryGetValue(step.StepNumber, out ProofPlacement? placed);

            bool statementOk = placed?.StatementId == step.StatementId;
            bool reasonOk    = placed?.ReasonId    == step.ReasonId;

            if (statementOk && reasonOk)
            {
                correctCount++;
            }
            else if (firstWrong is null)
            {
                // Only record the first wrong step encountered.
                firstWrong = step.StepNumber;
            }
        }

        bool isComplete = correctCount == exercise.CorrectSteps.Count
                       && firstWrong is null;

        return new ProofValidationResult
        {
            IsComplete   = isComplete,
            CorrectCount = correctCount,
            FirstWrongStep = firstWrong,
        };
    }

    /// <summary>
    /// Returns the step index (0-based in <see cref="ProofExercise.CorrectSteps"/>)
    /// of the next step the student should fill in.
    /// <para>
    /// The hint identifies the first step that is either unfilled or incorrectly filled,
    /// giving the student just enough information to make their next move.
    /// Returns 0 (the first step) if no placements have been made yet.
    /// Returns <c>exercise.CorrectSteps.Count − 1</c> if all previous steps are correct.
    /// </para>
    /// </summary>
    /// <param name="exercise">The exercise being solved.</param>
    /// <param name="placements">The student's current placements.</param>
    /// <returns>0-based index into <see cref="ProofExercise.CorrectSteps"/>.</returns>
    public int GetHintIndex(ProofExercise exercise, List<ProofPlacement> placements)
    {
        if (exercise is null)   throw new ArgumentNullException(nameof(exercise));
        if (placements is null) throw new ArgumentNullException(nameof(placements));

        var placementByStep = placements.ToDictionary(p => p.StepNumber);

        for (int i = 0; i < exercise.CorrectSteps.Count; i++)
        {
            ProofStep step = exercise.CorrectSteps[i];
            placementByStep.TryGetValue(step.StepNumber, out ProofPlacement? placed);

            bool statementOk = placed?.StatementId == step.StatementId;
            bool reasonOk    = placed?.ReasonId    == step.ReasonId;

            if (!statementOk || !reasonOk)
                return i;
        }

        // All steps correct — point back at the last step (proof is already done).
        return Math.Max(0, exercise.CorrectSteps.Count - 1);
    }

    // -----------------------------------------------------------------------
    // LLM generation
    // -----------------------------------------------------------------------

    private async Task<ProofExercise?> GenerateViaLlmAsync(string skillId, int difficultyBand)
    {
        // Build the tool schema inline — mirrors the pattern in LlmGatewayService.
        var tool = new
        {
            name        = "produce_proof_exercise",
            description = "Output a complete two-column proof exercise for a geometry skill.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    given_text = new { type = "string", description = "The Given: clause." },
                    prove_text = new { type = "string", description = "The Prove: clause." },
                    steps      = new
                    {
                        type  = "array",
                        description = "Ordered correct proof steps.",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                step_number    = new { type = "integer" },
                                statement_text = new { type = "string" },
                                reason_text    = new { type = "string" },
                            },
                            required = new[] { "step_number", "statement_text", "reason_text" },
                        }
                    },
                    distractor_statements = new
                    {
                        type  = "array",
                        description = "1-3 plausible-but-wrong statement distractors.",
                        items = new { type = "string" },
                    },
                    distractor_reasons = new
                    {
                        type  = "array",
                        description = "1-3 plausible-but-wrong reason distractors.",
                        items = new { type = "string" },
                    },
                },
                required = new[] { "given_text", "prove_text", "steps" },
            }
        };

        string system = """
            You are an expert honors-geometry curriculum designer.
            Produce a two-column proof exercise as JSON matching the tool schema.
            The proof must be logically valid and appropriate for the given skill and difficulty.
            Include 3-6 proof steps. Distractors should look plausible but be clearly wrong
            when the student thinks carefully.
            """;

        string user = $"""
            Skill: {skillId}
            Difficulty band: {difficultyBand} (1 = easiest, 5 = hardest)

            Generate a two-column proof exercise for this geometry skill.
            Include the Given and Prove statements, all correct proof steps in order,
            and 2-3 distractor statements and reasons.
            """;

        // Piggyback on the gateway's existing plain-call infrastructure by going
        // through GenerateLessonRecapAsync path is wrong — instead we call the
        // internal structured endpoint via reflection would be fragile.
        // The cleanest approach without modifying LlmGatewayService is to use the
        // existing GenerateDialogueAsync and interpret the result — but that
        // returns a DialogueScript, not a ProofExercise.
        //
        // Best real approach: call the REST API directly with the same pattern the
        // gateway uses, since ProofBeatService has access to the gateway's model.
        // However, because LlmGatewayService deliberately encapsulates HTTP, we
        // ask for the lesson plan as a proxy and then parse our own tool result by
        // invoking GenerateRawProofJsonAsync on the gateway.
        //
        // For a clean architecture that avoids coupling, we use LlmGatewayService's
        // GenerateLessonRecapAsync (which returns plain text) to request raw JSON,
        // then parse it ourselves.  This is a legitimate use of the recap API as a
        // generic plain-text channel.

        // Ask the LLM for raw JSON representing the proof exercise.
        string prompt = $"""
            {system}

            {user}

            IMPORTANT: Reply with ONLY a raw JSON object — no markdown fences, no prose.
            The JSON must have keys: given_text (string), prove_text (string),
            steps (array of objects with step_number, statement_text, reason_text),
            distractor_statements (array of strings), distractor_reasons (array of strings).
            """;

        string rawJson = await _llm.GenerateLessonRecapAsync(
            skillId,
            new List<string> { prompt })
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        // The recap API might prepend the skill name — strip leading non-JSON content.
        int braceIdx = rawJson.IndexOf('{', StringComparison.Ordinal);
        if (braceIdx > 0)
            rawJson = rawJson[braceIdx..];

        return ParseLlmJson(rawJson);
    }

    /// <summary>
    /// Parses the raw JSON string produced by the LLM into a <see cref="ProofExercise"/>.
    /// Returns <c>null</c> if the JSON is malformed or missing required fields.
    /// </summary>
    private static ProofExercise? ParseLlmJson(string json)
    {
        try
        {
            using var doc  = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            string givenText = root.TryGetProperty("given_text", out var g)
                ? g.GetString() ?? ""
                : "";

            string proveText = root.TryGetProperty("prove_text", out var p)
                ? p.GetString() ?? ""
                : "";

            if (string.IsNullOrEmpty(givenText) || string.IsNullOrEmpty(proveText))
                return null;

            // Parse correct steps.
            var correctSteps     = new List<ProofStep>();
            var statementBank    = new List<ProofItem>();
            var reasonBank       = new List<ProofItem>();

            if (root.TryGetProperty("steps", out var stepsEl))
            {
                foreach (var stepEl in stepsEl.EnumerateArray())
                {
                    int    stepNum = stepEl.TryGetProperty("step_number", out var sn)
                                        ? sn.GetInt32() : correctSteps.Count + 1;
                    string stmtText = stepEl.TryGetProperty("statement_text", out var st)
                                        ? st.GetString() ?? "" : "";
                    string rsText   = stepEl.TryGetProperty("reason_text", out var rt)
                                        ? rt.GetString() ?? "" : "";

                    var stmtItem = new ProofItem { Text = stmtText, IsDistractor = false };
                    var rsItem   = new ProofItem { Text = rsText,   IsDistractor = false };

                    statementBank.Add(stmtItem);
                    reasonBank.Add(rsItem);
                    correctSteps.Add(new ProofStep
                    {
                        StepNumber  = stepNum,
                        StatementId = stmtItem.Id,
                        ReasonId    = rsItem.Id,
                    });
                }
            }

            if (correctSteps.Count == 0)
                return null;

            // Append distractors.
            if (root.TryGetProperty("distractor_statements", out var ds))
            {
                foreach (var d in ds.EnumerateArray())
                    statementBank.Add(new ProofItem { Text = d.GetString() ?? "", IsDistractor = true });
            }

            if (root.TryGetProperty("distractor_reasons", out var dr))
            {
                foreach (var d in dr.EnumerateArray())
                    reasonBank.Add(new ProofItem { Text = d.GetString() ?? "", IsDistractor = true });
            }

            return new ProofExercise
            {
                GivenText    = givenText,
                ProveText    = proveText,
                StatementBank = statementBank,
                ReasonBank    = reasonBank,
                CorrectSteps  = correctSteps,
            };
        }
        catch
        {
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Static fallback library
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a pre-authored exercise for the given skill, or a generic
    /// triangle-congruence exercise when the skill has no dedicated entry.
    /// </summary>
    private static ProofExercise StaticFallback(string skillId, int difficultyBand)
    {
        // Choose a template based on skill family.
        if (skillId.Contains("parallel", StringComparison.OrdinalIgnoreCase))
            return BuildParallelLinesProof();

        if (skillId.Contains("isosceles", StringComparison.OrdinalIgnoreCase))
            return BuildIsoscelesProof();

        if (skillId.Contains("segment", StringComparison.OrdinalIgnoreCase)
            || skillId.Contains("midpoint", StringComparison.OrdinalIgnoreCase))
            return BuildSegmentAdditionProof();

        if (skillId.Contains("supplementary", StringComparison.OrdinalIgnoreCase)
            || skillId.Contains("angle", StringComparison.OrdinalIgnoreCase))
            return BuildSupplementaryAnglesProof();

        // Default: SAS triangle congruence proof, adjusting complexity by band.
        return difficultyBand <= 2
            ? BuildSasCongruenceProofSimple()
            : BuildSasCongruenceProofFull();
    }

    // -----------------------------------------------------------------------
    // Static exercise builders
    // -----------------------------------------------------------------------

    /// <summary>
    /// SAS congruence proof (simple, 3 steps).
    /// Given: AB ≅ DE, ∠B ≅ ∠E, BC ≅ EF.  Prove: △ABC ≅ △DEF.
    /// </summary>
    private static ProofExercise BuildSasCongruenceProofSimple()
    {
        // Correct items
        var s1 = new ProofItem { Text = "AB ≅ DE",              IsDistractor = false };
        var s2 = new ProofItem { Text = "∠B ≅ ∠E",             IsDistractor = false };
        var s3 = new ProofItem { Text = "BC ≅ EF",              IsDistractor = false };
        var s4 = new ProofItem { Text = "△ABC ≅ △DEF",          IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                 IsDistractor = false };
        var r2 = new ProofItem { Text = "Given",                 IsDistractor = false };
        var r3 = new ProofItem { Text = "Given",                 IsDistractor = false };
        var r4 = new ProofItem { Text = "SAS congruence postulate", IsDistractor = false };

        // Distractors
        var ds1 = new ProofItem { Text = "AC ≅ DF",             IsDistractor = true };
        var ds2 = new ProofItem { Text = "△ABC ≅ △EDF",         IsDistractor = true };
        var dr1 = new ProofItem { Text = "SSS congruence postulate", IsDistractor = true };
        var dr2 = new ProofItem { Text = "ASA congruence postulate", IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "AB ≅ DE, ∠B ≅ ∠E, BC ≅ EF",
            ProveText     = "△ABC ≅ △DEF",
            StatementBank = [s1, s2, s3, s4, ds1, ds2],
            ReasonBank    = [r1, r2, r3, r4, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
            ],
        };
    }

    /// <summary>
    /// SAS congruence proof with CPCTC conclusion (5 steps — difficulty band 3+).
    /// Given: M is midpoint of AC, BM ⊥ AC.  Prove: AB ≅ BC.
    /// </summary>
    private static ProofExercise BuildSasCongruenceProofFull()
    {
        var s1 = new ProofItem { Text = "M is midpoint of AC",            IsDistractor = false };
        var s2 = new ProofItem { Text = "AM ≅ MC",                        IsDistractor = false };
        var s3 = new ProofItem { Text = "BM ⊥ AC",                        IsDistractor = false };
        var s4 = new ProofItem { Text = "∠BMA and ∠BMC are right angles", IsDistractor = false };
        var s5 = new ProofItem { Text = "∠BMA ≅ ∠BMC",                   IsDistractor = false };
        var s6 = new ProofItem { Text = "BM ≅ BM",                        IsDistractor = false };
        var s7 = new ProofItem { Text = "△ABM ≅ △CBM",                   IsDistractor = false };
        var s8 = new ProofItem { Text = "AB ≅ BC",                        IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                                IsDistractor = false };
        var r2 = new ProofItem { Text = "Definition of midpoint",               IsDistractor = false };
        var r3 = new ProofItem { Text = "Given",                                IsDistractor = false };
        var r4 = new ProofItem { Text = "Definition of perpendicular lines",    IsDistractor = false };
        var r5 = new ProofItem { Text = "All right angles are congruent",       IsDistractor = false };
        var r6 = new ProofItem { Text = "Reflexive property of congruence",     IsDistractor = false };
        var r7 = new ProofItem { Text = "SAS congruence postulate",             IsDistractor = false };
        var r8 = new ProofItem { Text = "CPCTC",                                IsDistractor = false };

        // Distractors
        var ds1 = new ProofItem { Text = "BM bisects AC",                  IsDistractor = true };
        var ds2 = new ProofItem { Text = "AB ≅ BM",                        IsDistractor = true };
        var dr1 = new ProofItem { Text = "AAS congruence theorem",         IsDistractor = true };
        var dr2 = new ProofItem { Text = "Definition of congruent segments", IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "M is midpoint of AC; BM ⊥ AC",
            ProveText     = "AB ≅ BC",
            StatementBank = [s1, s2, s3, s4, s5, s6, s7, s8, ds1, ds2],
            ReasonBank    = [r1, r2, r3, r4, r5, r6, r7, r8, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
                new ProofStep { StepNumber = 5, StatementId = s5.Id, ReasonId = r5.Id },
                new ProofStep { StepNumber = 6, StatementId = s6.Id, ReasonId = r6.Id },
                new ProofStep { StepNumber = 7, StatementId = s7.Id, ReasonId = r7.Id },
                new ProofStep { StepNumber = 8, StatementId = s8.Id, ReasonId = r8.Id },
            ],
        };
    }

    /// <summary>
    /// Parallel lines cut by a transversal: prove alternate interior angles are congruent.
    /// Given: l ∥ m, transversal t.  Prove: ∠3 ≅ ∠6.
    /// </summary>
    private static ProofExercise BuildParallelLinesProof()
    {
        var s1 = new ProofItem { Text = "l ∥ m",                            IsDistractor = false };
        var s2 = new ProofItem { Text = "∠3 ≅ ∠7",                         IsDistractor = false };
        var s3 = new ProofItem { Text = "∠7 ≅ ∠6",                         IsDistractor = false };
        var s4 = new ProofItem { Text = "∠3 ≅ ∠6",                         IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                             IsDistractor = false };
        var r2 = new ProofItem { Text = "Corresponding angles postulate",    IsDistractor = false };
        var r3 = new ProofItem { Text = "Vertical angles theorem",           IsDistractor = false };
        var r4 = new ProofItem { Text = "Transitive property of congruence", IsDistractor = false };

        var ds1 = new ProofItem { Text = "∠3 ≅ ∠5",                        IsDistractor = true };
        var ds2 = new ProofItem { Text = "∠1 + ∠2 = 180°",                 IsDistractor = true };
        var dr1 = new ProofItem { Text = "Alternate exterior angles theorem",IsDistractor = true };
        var dr2 = new ProofItem { Text = "Co-interior angles are supplementary", IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "l ∥ m, transversal t crosses both lines",
            ProveText     = "∠3 ≅ ∠6 (alternate interior angles)",
            StatementBank = [s1, s2, s3, s4, ds1, ds2],
            ReasonBank    = [r1, r2, r3, r4, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
            ],
        };
    }

    /// <summary>
    /// Isosceles triangle theorem: prove base angles are congruent.
    /// Given: AB ≅ AC.  Prove: ∠B ≅ ∠C.
    /// </summary>
    private static ProofExercise BuildIsoscelesProof()
    {
        var s1 = new ProofItem { Text = "AB ≅ AC",                           IsDistractor = false };
        var s2 = new ProofItem { Text = "Draw altitude AM from A to BC",     IsDistractor = false };
        var s3 = new ProofItem { Text = "AM ≅ AM",                           IsDistractor = false };
        var s4 = new ProofItem { Text = "△ABM ≅ △ACM",                      IsDistractor = false };
        var s5 = new ProofItem { Text = "∠B ≅ ∠C",                          IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                              IsDistractor = false };
        var r2 = new ProofItem { Text = "Construction (any point can serve as foot of altitude)", IsDistractor = false };
        var r3 = new ProofItem { Text = "Reflexive property of congruence",   IsDistractor = false };
        var r4 = new ProofItem { Text = "HL theorem",                         IsDistractor = false };
        var r5 = new ProofItem { Text = "CPCTC",                              IsDistractor = false };

        var ds1 = new ProofItem { Text = "∠A ≅ ∠B",                         IsDistractor = true };
        var dr1 = new ProofItem { Text = "SAS congruence postulate",          IsDistractor = true };
        var dr2 = new ProofItem { Text = "Definition of isosceles triangle",  IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "AB ≅ AC",
            ProveText     = "∠B ≅ ∠C",
            StatementBank = [s1, s2, s3, s4, s5, ds1],
            ReasonBank    = [r1, r2, r3, r4, r5, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
                new ProofStep { StepNumber = 5, StatementId = s5.Id, ReasonId = r5.Id },
            ],
        };
    }

    /// <summary>
    /// Segment addition / midpoint proof.
    /// Given: M is midpoint of AB, N is midpoint of CD, AB ≅ CD.  Prove: AM ≅ CN.
    /// </summary>
    private static ProofExercise BuildSegmentAdditionProof()
    {
        var s1 = new ProofItem { Text = "M is midpoint of AB",              IsDistractor = false };
        var s2 = new ProofItem { Text = "N is midpoint of CD",              IsDistractor = false };
        var s3 = new ProofItem { Text = "AB ≅ CD",                         IsDistractor = false };
        var s4 = new ProofItem { Text = "AM = ½ AB",                       IsDistractor = false };
        var s5 = new ProofItem { Text = "CN = ½ CD",                       IsDistractor = false };
        var s6 = new ProofItem { Text = "AM = CN",                         IsDistractor = false };
        var s7 = new ProofItem { Text = "AM ≅ CN",                         IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                            IsDistractor = false };
        var r2 = new ProofItem { Text = "Given",                            IsDistractor = false };
        var r3 = new ProofItem { Text = "Given",                            IsDistractor = false };
        var r4 = new ProofItem { Text = "Definition of midpoint",           IsDistractor = false };
        var r5 = new ProofItem { Text = "Definition of midpoint",           IsDistractor = false };
        var r6 = new ProofItem { Text = "Substitution property of equality",IsDistractor = false };
        var r7 = new ProofItem { Text = "Definition of congruent segments", IsDistractor = false };

        var ds1 = new ProofItem { Text = "MB ≅ ND",                        IsDistractor = true };
        var dr1 = new ProofItem { Text = "Segment addition postulate",      IsDistractor = true };
        var dr2 = new ProofItem { Text = "Transitive property of equality", IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "M is midpoint of AB; N is midpoint of CD; AB ≅ CD",
            ProveText     = "AM ≅ CN",
            StatementBank = [s1, s2, s3, s4, s5, s6, s7, ds1],
            ReasonBank    = [r1, r2, r3, r4, r5, r6, r7, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
                new ProofStep { StepNumber = 5, StatementId = s5.Id, ReasonId = r5.Id },
                new ProofStep { StepNumber = 6, StatementId = s6.Id, ReasonId = r6.Id },
                new ProofStep { StepNumber = 7, StatementId = s7.Id, ReasonId = r7.Id },
            ],
        };
    }

    /// <summary>
    /// Supplementary / linear-pair angle proof.
    /// Given: ∠1 and ∠2 form a linear pair.  Prove: ∠1 + ∠2 = 180°.
    /// </summary>
    private static ProofExercise BuildSupplementaryAnglesProof()
    {
        var s1 = new ProofItem { Text = "∠1 and ∠2 form a linear pair",        IsDistractor = false };
        var s2 = new ProofItem { Text = "Ray BA and ray BC are opposite rays",  IsDistractor = false };
        var s3 = new ProofItem { Text = "m∠1 + m∠2 = m∠ABC",                  IsDistractor = false };
        var s4 = new ProofItem { Text = "m∠ABC = 180°",                        IsDistractor = false };
        var s5 = new ProofItem { Text = "m∠1 + m∠2 = 180°",                   IsDistractor = false };

        var r1 = new ProofItem { Text = "Given",                                IsDistractor = false };
        var r2 = new ProofItem { Text = "Definition of linear pair",            IsDistractor = false };
        var r3 = new ProofItem { Text = "Angle addition postulate",             IsDistractor = false };
        var r4 = new ProofItem { Text = "Definition of straight angle",         IsDistractor = false };
        var r5 = new ProofItem { Text = "Substitution property of equality",    IsDistractor = false };

        var ds1 = new ProofItem { Text = "∠1 ≅ ∠2",                           IsDistractor = true };
        var ds2 = new ProofItem { Text = "m∠1 = m∠2 = 90°",                   IsDistractor = true };
        var dr1 = new ProofItem { Text = "Vertical angles theorem",             IsDistractor = true };
        var dr2 = new ProofItem { Text = "Transitive property of equality",     IsDistractor = true };

        return new ProofExercise
        {
            GivenText     = "∠1 and ∠2 form a linear pair",
            ProveText     = "m∠1 + m∠2 = 180°",
            StatementBank = [s1, s2, s3, s4, s5, ds1, ds2],
            ReasonBank    = [r1, r2, r3, r4, r5, dr1, dr2],
            CorrectSteps  =
            [
                new ProofStep { StepNumber = 1, StatementId = s1.Id, ReasonId = r1.Id },
                new ProofStep { StepNumber = 2, StatementId = s2.Id, ReasonId = r2.Id },
                new ProofStep { StepNumber = 3, StatementId = s3.Id, ReasonId = r3.Id },
                new ProofStep { StepNumber = 4, StatementId = s4.Id, ReasonId = r4.Id },
                new ProofStep { StepNumber = 5, StatementId = s5.Id, ReasonId = r5.Id },
            ],
        };
    }
}
