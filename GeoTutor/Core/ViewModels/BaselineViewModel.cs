namespace GeoTutor.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Models;
using GeoTutor.Core.Services;
using Microsoft.Data.Sqlite;

public partial class BaselineViewModel : BaseViewModel
{
    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly DatabaseService _databaseService;
    private readonly SkillGraphService _skillGraphService;

    // Internal assessment state
    private readonly List<(string skillId, bool correct)> _answers = [];
    private readonly Random _rng = new();

    // Ordered list of skill IDs to probe during baseline
    private List<string> _assessmentQueue = [];
    private int _queueIndex;

    // -----------------------------------------------------------------------
    // Observable properties
    // -----------------------------------------------------------------------

    [ObservableProperty] private Item? _currentItem;
    [ObservableProperty] private string _userAnswer = "";
    [ObservableProperty] private int _itemsCompleted;
    [ObservableProperty] private int _totalEstimatedItems = 30;
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private BaselineResult? _result;
    [ObservableProperty] private string _feedbackMessage = "";

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public BaselineViewModel(
        DatabaseService databaseService,
        SkillGraphService skillGraphService)
    {
        _databaseService   = databaseService   ?? throw new ArgumentNullException(nameof(databaseService));
        _skillGraphService = skillGraphService  ?? throw new ArgumentNullException(nameof(skillGraphService));
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Begins the baseline by building a probe queue and loading the first item.
    /// </summary>
    [RelayCommand]
    private async Task StartBaseline()
    {
        SetBusy(true, "Preparing assessment…");
        IsComplete       = false;
        ItemsCompleted   = 0;
        UserAnswer       = "";
        FeedbackMessage  = "";
        Result           = null;
        _answers.Clear();

        await Task.Run(() => BuildAssessmentQueue());

        SetBusy(false);
        await LoadNextItem();
    }

    /// <summary>
    /// Records the student's answer for the current item, shows brief feedback,
    /// then either advances to the next item or finalises the assessment.
    /// </summary>
    [RelayCommand]
    private async Task SubmitAnswer()
    {
        if (CurrentItem is null || string.IsNullOrWhiteSpace(UserAnswer))
        {
            FeedbackMessage = "Please enter an answer before submitting.";
            return;
        }

        bool correct = EvaluateAnswer(CurrentItem, UserAnswer.Trim());

        // Track this response against the skill.
        string skillId = CurrentItem.SkillId;
        _answers.Add((skillId, correct));
        ItemsCompleted++;

        FeedbackMessage = correct ? "Correct!" : "Not quite — moving on.";

        // Persist attempt to database.
        await Task.Run(() => RecordAttempt(CurrentItem.Id, correct));

        UserAnswer = "";

        // Short pause so the student sees the feedback before the screen updates.
        await Task.Delay(800);
        FeedbackMessage = "";

        bool done = _queueIndex >= _assessmentQueue.Count
                    || ItemsCompleted >= TotalEstimatedItems;

        if (done)
            await FinalizeBaseline();
        else
            await LoadNextItem();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds the probe queue: one representative item per unlocked skill,
    /// ordered by unit then difficulty so easier concepts come first.
    /// </summary>
    private void BuildAssessmentQueue()
    {
        var skills = _skillGraphService.GetAllSkills();

        // Sort: unit 0 (algebra prereqs) first, then ascending by unit and band.
        skills.Sort((a, b) =>
        {
            int cmp = a.Unit.CompareTo(b.Unit);
            return cmp != 0 ? cmp : a.DifficultyBand.CompareTo(b.DifficultyBand);
        });

        // Keep up to TotalEstimatedItems skills; skip deeply-locked ones
        // by including all skills (the queue cap handles excess).
        _assessmentQueue = [];
        foreach (var s in skills)
        {
            _assessmentQueue.Add(s.Id);
            if (_assessmentQueue.Count >= TotalEstimatedItems)
                break;
        }

        TotalEstimatedItems = _assessmentQueue.Count;
        _queueIndex = 0;
    }

    /// <summary>
    /// Loads a simple probe item for the next skill in the queue.
    /// Falls back to a generic text question if no stored item exists.
    /// </summary>
    private async Task LoadNextItem()
    {
        if (_queueIndex >= _assessmentQueue.Count)
        {
            await FinalizeBaseline();
            return;
        }

        string skillId = _assessmentQueue[_queueIndex++];
        var skill = _skillGraphService.GetSkill(skillId);

        // Try to fetch a library item for this skill from the database.
        Item? item = await Task.Run(() => FetchItemForSkill(skillId));

        if (item is null)
        {
            // Synthesise a lightweight placeholder item so the loop keeps moving.
            item = new Item
            {
                Id         = $"baseline-probe-{skillId}",
                SkillId    = skillId,
                Template   = "text",
                Prompt     = $"[Baseline] Describe or solve a problem relating to: {skill?.Name ?? skillId}",
                AnswerJson = """{"value":"ok","tolerance":0}""",
                Difficulty = 1,
                Source     = "baseline"
            };
        }

        CurrentItem     = item;
        FeedbackMessage = "";
    }

    private Item? FetchItemForSkill(string skillId)
    {
        var conn = _databaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, template, params_json, prompt, answer_json,
                   hints_json, solution_steps_json, skill_id, difficulty, source, item_type
            FROM gt_items
            WHERE skill_id = @skill AND difficulty <= 2
            ORDER BY difficulty ASC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@skill", skillId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return ReadItem(reader);
    }

    private void RecordAttempt(string itemId, bool correct)
    {
        var conn = _databaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO gt_attempts (item_id, ts, correct, time_ms, hints_used, dialogue_played, error_category)
            VALUES (@item, @ts, @correct, 0, 0, 0, NULL);
            """;
        cmd.Parameters.AddWithValue("@item",    itemId);
        cmd.Parameters.AddWithValue("@ts",      DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@correct", correct ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Aggregates answers into skill mastery levels, writes a baseline result
    /// row, and updates mastery in the skill graph.
    /// </summary>
    private async Task FinalizeBaseline()
    {
        SetBusy(true, "Scoring baseline…");

        var skillScores = await Task.Run(() => ComputeSkillScores());

        // Determine overall readiness.
        double overall = skillScores.Count > 0
            ? (double)skillScores.Values.Sum(m => (int)m) / (skillScores.Count * (int)MasteryLevel.Mastered)
            : 0.0;

        var readiness = overall switch
        {
            >= 0.70 => ReadinessFlag.Ready,
            >= 0.40 => ReadinessFlag.Conditional,
            _       => ReadinessFlag.NotReady
        };

        var baselineResult = new BaselineResult
        {
            Timestamp        = DateTime.UtcNow,
            SkillScores      = skillScores,
            OverallReadiness = readiness
        };

        await Task.Run(() =>
        {
            PersistBaselineResult(baselineResult);

            // Seed mastery into the skill graph so first lessons are calibrated.
            foreach (var (skillId, level) in skillScores)
            {
                double mastery = (int)level / (double)(int)MasteryLevel.Mastered;
                SetSkillMastery(skillId, mastery);
            }
        });

        Result          = baselineResult;
        IsComplete      = true;
        FeedbackMessage = "Baseline assessment complete!";
        SetBusy(false);
    }

    private Dictionary<string, MasteryLevel> ComputeSkillScores()
    {
        var scores = new Dictionary<string, MasteryLevel>();

        // Group answers by skill; a skill with ≥ 1 answer gets a score.
        var bySkill = new Dictionary<string, List<bool>>();
        foreach (var (skillId, correct) in _answers)
        {
            if (!bySkill.TryGetValue(skillId, out var list))
            {
                list = [];
                bySkill[skillId] = list;
            }
            list.Add(correct);
        }

        foreach (var (skillId, attempts) in bySkill)
        {
            double rate = attempts.Count(c => c) / (double)attempts.Count;
            MasteryLevel level = rate switch
            {
                >= 0.90 => MasteryLevel.Mastered,
                >= 0.75 => MasteryLevel.Solid,
                >= 0.50 => MasteryLevel.Developing,
                >= 0.25 => MasteryLevel.Shaky,
                _       => MasteryLevel.Unknown
            };
            scores[skillId] = level;
        }

        return scores;
    }

    private void PersistBaselineResult(BaselineResult r)
    {
        var conn = _databaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO gt_baseline_results (ts, skill_scores_json, overall_readiness)
            VALUES (@ts, @scores, @readiness);
            """;
        cmd.Parameters.AddWithValue("@ts",        r.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@scores",     JsonSerializer.Serialize(r.SkillScores));
        cmd.Parameters.AddWithValue("@readiness",  r.OverallReadiness.ToString());
        cmd.ExecuteNonQuery();
    }

    private void SetSkillMastery(string skillId, double mastery)
    {
        var conn = _databaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE gt_skills
            SET current_mastery = @m,
                last_seen       = @ts
            WHERE id = @id;
            """;
        cmd.Parameters.AddWithValue("@m",  Math.Clamp(mastery, 0.0, 1.0));
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@id", skillId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Lightweight answer evaluator.  Handles numeric and free-text answers.
    /// The prompt spec places a numeric answer in AnswerJson as {value, tolerance}
    /// and a multiple-choice answer as {choices, correct}.
    /// </summary>
    private static bool EvaluateAnswer(Item item, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(item.AnswerJson);
            var root = doc.RootElement;

            // Multiple-choice: match by zero-based index or by text.
            if (root.TryGetProperty("correct", out var correctProp) &&
                root.TryGetProperty("choices", out var choicesProp))
            {
                int correctIndex = correctProp.GetInt32();
                var choices      = choicesProp.EnumerateArray().Select(c => c.GetString() ?? "").ToList();

                // Accept the index as a string ("0", "1", …) or the choice text.
                if (int.TryParse(raw, out int idx))
                    return idx == correctIndex;

                if (correctIndex < choices.Count)
                    return string.Equals(raw, choices[correctIndex], StringComparison.OrdinalIgnoreCase);
            }

            // Numeric: value ± tolerance.
            if (root.TryGetProperty("value", out var valueProp))
            {
                double expected  = valueProp.GetDouble();
                double tolerance = root.TryGetProperty("tolerance", out var tolProp)
                                   ? tolProp.GetDouble()
                                   : 0.01;

                if (double.TryParse(raw, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double given))
                    return Math.Abs(given - expected) <= tolerance;
            }
        }
        catch
        {
            // Malformed JSON — fall through to text comparison.
        }

        // Last resort: case-insensitive text match.
        return string.Equals(raw.Trim(), "ok", StringComparison.OrdinalIgnoreCase);
    }

    private static Item ReadItem(SqliteDataReader r)
    {
        List<string> hints, steps;
        try   { hints = JsonSerializer.Deserialize<List<string>>(r.GetString(5)) ?? []; }
        catch { hints = []; }
        try   { steps = JsonSerializer.Deserialize<List<string>>(r.GetString(6)) ?? []; }
        catch { steps = []; }

        Enum.TryParse<ItemType>(r.GetString(10), out var itemType);

        return new Item
        {
            Id             = r.GetString(0),
            Template       = r.GetString(1),
            ParamsJson     = r.GetString(2),
            Prompt         = r.GetString(3),
            AnswerJson     = r.GetString(4),
            Hints          = hints,
            SolutionSteps  = steps,
            SkillId        = r.GetString(7),
            Difficulty     = r.GetInt32(8),
            Source         = r.GetString(9),
            Type           = itemType
        };
    }
}
