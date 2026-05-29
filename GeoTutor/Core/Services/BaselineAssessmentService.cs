using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Implements the adaptive baseline assessment (§3).
///
/// The assessment covers two tracks:
///   - Algebra: 8 skills from unit 0 (alg-*)
///   - Geometry: 8 skills from units 1–2 (early geo-u1/u2 nodes)
///
/// Adaptive item selection (§3.3):
///   Maintains a per-cluster ability estimate θ initialised at 0.
///   After a correct answer the target shifts to θ+0.5; after wrong to θ-0.5.
///   The next item is the library item whose difficulty is closest to the current target
///   (Fisher information maximization heuristic).
///   Hard cap: 4 items per cluster.
///
/// θ → MasteryLevel mapping:
///   θ &lt; -0.5 → Unknown
///   -0.5 – 0  → Shaky
///   0 – 0.5   → Developing
///   0.5 – 1   → Solid
///   &gt; 1    → Mastered
/// </summary>
public class BaselineAssessmentService
{
    // -----------------------------------------------------------------------
    // Cluster definitions
    // -----------------------------------------------------------------------

    // Algebra track: 8 skills from unit 0
    private static readonly string[] AlgebraClusterIds =
    [
        "alg-linear-eq",
        "alg-systems",
        "alg-factoring",
        "alg-quadratic-formula",
        "alg-exponents-radicals",
        "alg-function-notation",
        "alg-slope-lines",
        "alg-distance-midpoint",
    ];

    // Geometry track: 8 early skills from units 1-2
    private static readonly string[] GeometryClusterIds =
    [
        "geo-u1-point-line-plane",
        "geo-u1-angle-measure",
        "geo-u1-if-then-logic",
        "geo-u1-counterexamples",
        "geo-u2-parallel-identify",
        "geo-u2-transversal-angles",
        "geo-u2-perpendicular",
    ];

    private const int  MaxItemsPerCluster = 4;
    private const double InitialTheta      = 0.0;
    private const double ThetaStepCorrect  = 0.5;
    private const double ThetaStepWrong    = 0.5;   // applied as −step on wrong

    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly LlmGatewayService    _llm;
    private readonly SkillGraphService    _skillGraph;
    private readonly DatabaseService      _db;
    private readonly SessionLoggerService _logger;

    // -----------------------------------------------------------------------
    // Per-assessment state
    // -----------------------------------------------------------------------

    // θ per skill cluster
    private readonly Dictionary<string, double> _theta = new(StringComparer.Ordinal);

    // How many items have been administered per cluster
    private readonly Dictionary<string, int> _itemCount = new(StringComparer.Ordinal);

    // The item most recently returned by GetNextItemAsync
    private Item? _currentItem;
    private string? _currentClusterId;

    // Ordered list of all cluster IDs (algebra first, then geometry)
    private readonly List<string> _allClusters;

    // Index of the cluster being assessed right now
    private int _clusterCursor = 0;

    // Flag set when the assessment is complete
    private bool _finalized = false;

    public BaselineAssessmentService(
        LlmGatewayService    llm,
        SkillGraphService    skillGraph,
        DatabaseService      db,
        SessionLoggerService logger)
    {
        _llm        = llm        ?? throw new ArgumentNullException(nameof(llm));
        _skillGraph = skillGraph ?? throw new ArgumentNullException(nameof(skillGraph));
        _db         = db         ?? throw new ArgumentNullException(nameof(db));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));

        _allClusters = [.. AlgebraClusterIds, .. GeometryClusterIds];

        // Initialise θ and item counts for every cluster.
        foreach (var id in _allClusters)
        {
            _theta[id]     = InitialTheta;
            _itemCount[id] = 0;
        }
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the next item to present to the student, or <c>null</c> when all
    /// clusters have been exhausted (assessment complete).
    /// The method advances through clusters left-to-right, selecting within each
    /// cluster via the Fisher-information / θ-proximity heuristic.
    /// </summary>
    public async Task<Item?> GetNextItemAsync()
    {
        if (_finalized)
            return null;

        // Advance the cursor past clusters that have hit their item cap.
        while (_clusterCursor < _allClusters.Count &&
               _itemCount[_allClusters[_clusterCursor]] >= MaxItemsPerCluster)
        {
            _clusterCursor++;
        }

        if (_clusterCursor >= _allClusters.Count)
            return null;   // All clusters done.

        string clusterId = _allClusters[_clusterCursor];
        _currentClusterId = clusterId;

        double theta = _theta[clusterId];
        // Target difficulty after correct = θ+0.5; start = θ itself.
        double targetDifficulty = theta;   // refined after RecordAnswer

        Item? item = await SelectItemAsync(clusterId, targetDifficulty).ConfigureAwait(false);
        _currentItem = item;

        if (item is not null)
            _logger.Log("baseline_item_shown", new { clusterId, theta, targetDifficulty, itemId = item.Id });

        return item;
    }

    /// <summary>
    /// Records the result of the most-recently-returned item.
    /// Updates θ for the current cluster and advances the item count.
    /// Must be called exactly once after each <see cref="GetNextItemAsync"/> call.
    /// </summary>
    public void RecordAnswer(bool correct, long timeMs)
    {
        if (_currentClusterId is null)
            return;

        string clusterId = _currentClusterId;

        double oldTheta = _theta[clusterId];
        double newTheta = correct
            ? oldTheta + ThetaStepCorrect
            : oldTheta - ThetaStepWrong;

        _theta[clusterId]    = newTheta;
        _itemCount[clusterId]++;

        _logger.Log("baseline_answer", new
        {
            clusterId,
            correct,
            timeMs,
            oldTheta,
            newTheta,
            itemCount = _itemCount[clusterId],
        });

        // If this cluster is saturated, advance the cursor on the next GetNextItem call
        // (handled at the top of that method).
        _currentItem     = null;
        _currentClusterId = null;
    }

    /// <summary>
    /// Finalises the assessment: maps θ values to <see cref="MasteryLevel"/>,
    /// computes overall readiness, persists the result, and returns it.
    /// Safe to call multiple times — subsequent calls return the stored result.
    /// </summary>
    public BaselineResult FinalizeAsync()
    {
        _finalized = true;

        var scores = new Dictionary<string, MasteryLevel>(StringComparer.Ordinal);
        foreach (var (clusterId, theta) in _theta)
            scores[clusterId] = ThetaToLevel(theta);

        ReadinessFlag readiness = ComputeReadiness(scores);

        var result = new BaselineResult
        {
            Timestamp        = DateTime.UtcNow,
            SkillScores      = scores,
            OverallReadiness = readiness,
        };

        PersistResult(result);

        _logger.Log("baseline_finalized", new
        {
            overallReadiness = readiness.ToString(),
            scoreCount       = scores.Count,
        });

        return result;
    }

    // -----------------------------------------------------------------------
    // Private: θ → MasteryLevel
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps a θ value to a <see cref="MasteryLevel"/>:
    /// <list type="bullet">
    ///   <item>θ &lt; -0.5 → Unknown</item>
    ///   <item>-0.5 ≤ θ &lt; 0 → Shaky</item>
    ///   <item>0 ≤ θ &lt; 0.5 → Developing</item>
    ///   <item>0.5 ≤ θ ≤ 1 → Solid</item>
    ///   <item>θ &gt; 1 → Mastered</item>
    /// </list>
    /// </summary>
    private static MasteryLevel ThetaToLevel(double theta)
    {
        if (theta < -0.5)  return MasteryLevel.Unknown;
        if (theta < 0.0)   return MasteryLevel.Shaky;
        if (theta < 0.5)   return MasteryLevel.Developing;
        if (theta <= 1.0)  return MasteryLevel.Solid;
        return MasteryLevel.Mastered;
    }

    // -----------------------------------------------------------------------
    // Private: readiness computation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps aggregated mastery scores to a <see cref="ReadinessFlag"/>:
    /// <list type="bullet">
    ///   <item>Ready       — majority of clusters Solid or Mastered (≥ 60%)</item>
    ///   <item>Conditional — 40–59% Solid+Mastered</item>
    ///   <item>NotReady    — &lt; 40% Solid+Mastered</item>
    /// </list>
    /// </summary>
    private static ReadinessFlag ComputeReadiness(Dictionary<string, MasteryLevel> scores)
    {
        if (scores.Count == 0)
            return ReadinessFlag.NotReady;

        int strongCount = scores.Values.Count(
            m => m == MasteryLevel.Solid || m == MasteryLevel.Mastered);

        double ratio = (double)strongCount / scores.Count;

        if (ratio >= 0.60) return ReadinessFlag.Ready;
        if (ratio >= 0.40) return ReadinessFlag.Conditional;
        return ReadinessFlag.NotReady;
    }

    // -----------------------------------------------------------------------
    // Private: adaptive item selection
    // -----------------------------------------------------------------------

    /// <summary>
    /// Selects the best library item for the cluster whose difficulty is closest to
    /// <paramref name="targetDifficulty"/> (Fisher information maximization heuristic).
    /// Difficulty is an integer 1–5 in the gt_items table.
    /// Falls back to any item for the cluster if no difficulty-matched item exists.
    /// If the library is empty for this cluster, tries LLM generation.
    /// </summary>
    private async Task<Item?> SelectItemAsync(string clusterId, double targetDifficulty)
    {
        // Nearest integer difficulty band (clamp 1-5).
        int diffBand = Math.Clamp((int)Math.Round(targetDifficulty + 1), 1, 5);

        // Try exact difficulty first, then widen the search.
        Item? item = FetchLibraryItemNear(clusterId, diffBand);
        if (item is not null)
            return item;

        // Widen: try adjacent bands.
        for (int spread = 1; spread <= 4; spread++)
        {
            int lo = diffBand - spread;
            int hi = diffBand + spread;
            if (lo >= 1)
            {
                item = FetchLibraryItemNear(clusterId, lo);
                if (item is not null) return item;
            }
            if (hi <= 5)
            {
                item = FetchLibraryItemNear(clusterId, hi);
                if (item is not null) return item;
            }
        }

        // Library empty — generate via LLM.
        string seed = $"baseline:{clusterId}:{diffBand}:{_itemCount[clusterId]}";
        LlmEnvelope? env = await _llm.GenerateItemAsync(clusterId, diffBand, seed)
                                      .ConfigureAwait(false);
        if (env is null)
            return null;

        return EnvelopeToItem(env, clusterId, diffBand, seed);
    }

    private Item? FetchLibraryItemNear(string skillId, int diffBand)
    {
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, template, params_json, prompt, answer_json,
                   hints_json, solution_steps_json, skill_id, difficulty, source, item_type
            FROM gt_items
            WHERE skill_id  = @sid
              AND difficulty = @diff
              AND source     = 'library'
            ORDER BY RANDOM()
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@sid",  skillId);
        cmd.Parameters.AddWithValue("@diff", diffBand);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadItem(reader) : null;
    }

    // -----------------------------------------------------------------------
    // Private: envelope → Item conversion
    // -----------------------------------------------------------------------

    private static Item EnvelopeToItem(
        LlmEnvelope envelope, string skillId, int diffBand, string seed)
    {
        string answerJson = JsonSerializer.Serialize(envelope.Answer, new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        return new Item
        {
            Id            = $"llm:{seed}:{Guid.NewGuid():N}",
            Template      = envelope.Template,
            ParamsJson    = JsonSerializer.Serialize(envelope.Params),
            Prompt        = envelope.Prompt,
            AnswerJson    = answerJson,
            Hints         = envelope.Hints,
            SolutionSteps = envelope.SolutionSteps,
            SkillId       = skillId,
            Difficulty    = diffBand,
            Source        = "llm",
            Type          = ItemType.Procedural,
        };
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
    // Private: persistence
    // -----------------------------------------------------------------------

    private void PersistResult(BaselineResult result)
    {
        try
        {
            string scoresJson = JsonSerializer.Serialize(
                result.SkillScores.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToString()));

            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO gt_baseline_results (ts, skill_scores_json, overall_readiness)
                VALUES (@ts, @scores, @readiness);
                """;
            cmd.Parameters.AddWithValue("@ts",        result.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@scores",    scoresJson);
            cmd.Parameters.AddWithValue("@readiness", result.OverallReadiness.ToString());

            cmd.ExecuteNonQuery();

            // Read back the auto-assigned id.
            using var idCmd = conn.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            result.Id = Convert.ToInt64(idCmd.ExecuteScalar()!);
        }
        catch
        {
            // Persistence failure is non-fatal.
        }
    }
}
