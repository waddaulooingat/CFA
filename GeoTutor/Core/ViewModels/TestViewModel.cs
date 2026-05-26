namespace GeoTutor.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Models;
using GeoTutor.Core.Services;
using Microsoft.Data.Sqlite;

/// <summary>
/// Controls the post-lesson test: no hints, items drawn from the skill's
/// difficulty band, then a post-test review with dialogue for wrong answers.
/// </summary>
public partial class TestViewModel : BaseViewModel
{
    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly LessonEngineService   _lessonEngine;
    private readonly DialogueEngineService _dialogueEngine;
    private readonly SessionLoggerService  _sessionLogger;
    private readonly SkillGraphService     _skillGraph;
    private readonly CoordinatorService    _coordinator;
    private readonly DatabaseService       _database;

    // -----------------------------------------------------------------------
    // Internal state
    // -----------------------------------------------------------------------

    private string    _skillId      = "";
    private int       _difficultyBand;
    private readonly Stopwatch _answerTimer = new();

    // -----------------------------------------------------------------------
    // Observable properties — test items
    // -----------------------------------------------------------------------

    [ObservableProperty] private List<Item> _testItems = [];
    [ObservableProperty] private int        _currentItemIndex;
    [ObservableProperty] private Item?      _currentTestItem;
    [ObservableProperty] private string     _userAnswer  = "";

    // -----------------------------------------------------------------------
    // Observable properties — outcome
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool         _isTestComplete;
    [ObservableProperty] private double       _percentCorrect;
    [ObservableProperty] private TestOutcome _outcome;
    [ObservableProperty] private string       _outcomeSummary = "";

    // -----------------------------------------------------------------------
    // Observable properties — post-test review
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool                         _showPostTestReview;
    [ObservableProperty] private List<(Item item, bool correct)> _results = [];
    [ObservableProperty] private int                          _reviewItemIndex;
    [ObservableProperty] private DialogueScript?              _reviewDialogue;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public TestViewModel(
        LessonEngineService   lessonEngine,
        DialogueEngineService dialogueEngine,
        SessionLoggerService  sessionLogger,
        SkillGraphService     skillGraph,
        CoordinatorService    coordinator,
        DatabaseService       database)
    {
        _lessonEngine   = lessonEngine   ?? throw new ArgumentNullException(nameof(lessonEngine));
        _dialogueEngine = dialogueEngine ?? throw new ArgumentNullException(nameof(dialogueEngine));
        _sessionLogger  = sessionLogger  ?? throw new ArgumentNullException(nameof(sessionLogger));
        _skillGraph     = skillGraph     ?? throw new ArgumentNullException(nameof(skillGraph));
        _coordinator    = coordinator    ?? throw new ArgumentNullException(nameof(coordinator));
        _database       = database       ?? throw new ArgumentNullException(nameof(database));
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads a fresh set of test items for the given skill at the specified
    /// difficulty band and presents the first one.
    /// </summary>
    [RelayCommand]
    private async Task LoadTest(string skillId, int difficultyBand)
    {
        SetBusy(true, "Loading test…");

        _skillId        = skillId;
        _difficultyBand = difficultyBand;

        IsTestComplete      = false;
        ShowPostTestReview  = false;
        CurrentItemIndex    = 0;
        PercentCorrect      = 0.0;
        OutcomeSummary      = "";
        UserAnswer          = "";
        Results             = [];
        ReviewItemIndex     = 0;
        ReviewDialogue      = null;

        _dialogueEngine.ResetEscalation();

        var items = await Task.Run(() => FetchTestItems(skillId, difficultyBand, count: 5));
        TestItems = items;

        SetBusy(false);

        if (TestItems.Count == 0)
        {
            OutcomeSummary = "No test items found for this skill.";
            IsTestComplete = true;
            return;
        }

        CurrentTestItem = TestItems[0];
        _answerTimer.Restart();
    }

    /// <summary>
    /// Overload that accepts a skill-id–only call (uses skill's current band).
    /// </summary>
    [RelayCommand]
    private async Task LoadTestForSkill(string skillId)
    {
        var skill = await Task.Run(() => _skillGraph.GetSkill(skillId));
        int band  = skill?.DifficultyBand ?? 1;
        await LoadTest(skillId, band);
    }

    /// <summary>
    /// Records the student's answer, advances through the test, and on
    /// completion triggers scoring and the post-test review flow.
    /// </summary>
    [RelayCommand]
    private async Task SubmitAnswer()
    {
        if (CurrentTestItem is null || string.IsNullOrWhiteSpace(UserAnswer))
        {
            return;
        }

        long   timeMs  = _answerTimer.ElapsedMilliseconds;
        string trimmed = UserAnswer.Trim();
        bool   correct = EvaluateAnswer(CurrentTestItem, trimmed);

        _sessionLogger.LogAnswer(CurrentTestItem.Id, correct, timeMs, hintsUsed: 0);
        _skillGraph.UpdateMastery(CurrentTestItem.SkillId, correct, timeMs);

        Results = [.. Results, (CurrentTestItem, correct)];
        UserAnswer = "";

        int next = CurrentItemIndex + 1;
        if (next >= TestItems.Count)
        {
            await FinalizeTestAsync();
        }
        else
        {
            CurrentItemIndex = next;
            CurrentTestItem  = TestItems[next];
            _answerTimer.Restart();
        }
    }

    /// <summary>
    /// Advances through the post-test review, loading dialogue for incorrect
    /// items when available, and cycling to the next wrong answer when dismissed.
    /// </summary>
    [RelayCommand]
    private async Task NextReviewItem()
    {
        // Find the next incorrect result starting after the current review index.
        int nextWrong = FindNextWrongIndex(ReviewItemIndex + 1);

        if (nextWrong < 0)
        {
            // All reviewed — close review panel.
            ShowPostTestReview = false;
            ReviewDialogue     = null;
            return;
        }

        ReviewItemIndex = nextWrong;
        var (item, _)  = Results[nextWrong];

        SetBusy(true, "Loading review…");
        ReviewDialogue = await LoadReviewDialogueAsync(item);
        SetBusy(false);
    }

    // -----------------------------------------------------------------------
    // Private helpers — test scoring
    // -----------------------------------------------------------------------

    private async Task FinalizeTestAsync()
    {
        int    correct      = Results.Count(r => r.correct);
        double pct          = Results.Count > 0 ? (double)correct / Results.Count : 0.0;
        PercentCorrect      = pct;
        Outcome             = DetermineOutcome(pct);
        OutcomeSummary      = BuildOutcomeSummary(Outcome, correct, Results.Count);
        IsTestComplete      = true;

        _coordinator.RecordLessonResult(_skillId, pct);
        _sessionLogger.LogLessonEnd(_skillId, pct);

        // Start post-test review if any items were wrong.
        int firstWrong = FindNextWrongIndex(0);
        if (firstWrong >= 0)
        {
            ReviewItemIndex    = firstWrong;
            ShowPostTestReview = true;

            var (item, _)  = Results[firstWrong];
            SetBusy(true, "Loading review…");
            ReviewDialogue = await LoadReviewDialogueAsync(item);
            SetBusy(false);
        }
    }

    private static TestOutcome DetermineOutcome(double pct) => pct switch
    {
        >= 0.90 => TestOutcome.Mastered,
        >= 0.70 => TestOutcome.Passed,
        >= 0.50 => TestOutcome.NeedsReview,
        _       => TestOutcome.Failed
    };

    private static string BuildOutcomeSummary(TestOutcome outcome, int correct, int total) =>
        outcome switch
        {
            TestOutcome.Mastered    => $"Excellent! {correct}/{total} correct — skill mastered!",
            TestOutcome.Passed      => $"Good work! {correct}/{total} correct.",
            TestOutcome.NeedsReview => $"{correct}/{total} correct — keep practising!",
            _                         => $"{correct}/{total} correct — let's review the concepts.",
        };

    private int FindNextWrongIndex(int startFrom)
    {
        for (int i = startFrom; i < Results.Count; i++)
        {
            if (!Results[i].correct)
                return i;
        }
        return -1;
    }

    // -----------------------------------------------------------------------
    // Private helpers — dialogue
    // -----------------------------------------------------------------------

    private async Task<DialogueScript?> LoadReviewDialogueAsync(Item item)
    {
        string errorCategory = ClassifyError(item);
        _sessionLogger.LogDialoguePlay(errorCategory, item.Id);

        var script = await _dialogueEngine.PrepareDialogueAsync(
            errorCategory, item.Id, "{}");

        if (script is not null)
            _dialogueEngine.RecordDialoguePlayed(errorCategory);

        return script;
    }

    private static string ClassifyError(Item item)
    {
        string skillId = item.SkillId ?? "";
        if (skillId.Contains("congruence")) return "wrong-congruence-postulate";
        if (skillId.Contains("inscribed"))  return "inscribed-angle-half-arc";
        if (skillId.Contains("parallel"))   return "parallel-lines-transversal";
        if (skillId.Contains("pythagorean"))return "pythagorean-theorem-legs";
        if (skillId.Contains("similar"))    return "similar-triangles-ratio";
        if (skillId.Contains("segment"))    return "segment-addition-postulate";
        if (skillId.Contains("midpoint"))   return "midpoint-formula";
        if (skillId.Contains("distance"))   return "distance-formula";
        if (skillId.Contains("midsegment")) return "midsegment-theorem";
        return "sign-error";
    }

    // -----------------------------------------------------------------------
    // Private helpers — answer evaluation
    // -----------------------------------------------------------------------

    private static bool EvaluateAnswer(Item item, string raw)
    {
        try
        {
            using var doc  = JsonDocument.Parse(item.AnswerJson);
            var       root = doc.RootElement;

            if (root.TryGetProperty("correct", out var correctProp) &&
                root.TryGetProperty("choices", out var choicesProp))
            {
                int correctIndex = correctProp.GetInt32();
                var choices = choicesProp.EnumerateArray()
                                         .Select(c => c.GetString() ?? "")
                                         .ToList();

                if (int.TryParse(raw, out int idx))
                    return idx == correctIndex;

                if (correctIndex < choices.Count)
                    return string.Equals(raw, choices[correctIndex],
                                         StringComparison.OrdinalIgnoreCase);
            }

            if (root.TryGetProperty("value", out var valueProp))
            {
                double expected  = valueProp.GetDouble();
                double tolerance = root.TryGetProperty("tolerance", out var tolProp)
                                   ? tolProp.GetDouble() : 0.01;

                if (double.TryParse(raw,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double given))
                    return Math.Abs(given - expected) <= tolerance;
            }
        }
        catch { /* fall through */ }

        return string.Equals(raw, "ok", StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Private helpers — database
    // -----------------------------------------------------------------------

    private List<Item> FetchTestItems(string skillId, int difficultyBand, int count)
    {
        var conn = _database.GetConnection();
        var items = new List<Item>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, template, params_json, prompt, answer_json,
                   hints_json, solution_steps_json, skill_id, difficulty, source, item_type
            FROM gt_items
            WHERE skill_id  = @skill
              AND difficulty = @band
            ORDER BY RANDOM()
            LIMIT @count;
            """;
        cmd.Parameters.AddWithValue("@skill", skillId);
        cmd.Parameters.AddWithValue("@band",  difficultyBand);
        cmd.Parameters.AddWithValue("@count", count);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            items.Add(ReadItem(reader));

        // If exact band has fewer than requested, top up from adjacent bands.
        if (items.Count < count)
        {
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = """
                SELECT id, template, params_json, prompt, answer_json,
                       hints_json, solution_steps_json, skill_id, difficulty, source, item_type
                FROM gt_items
                WHERE skill_id  = @skill
                  AND difficulty != @band
                ORDER BY ABS(difficulty - @band), RANDOM()
                LIMIT @count;
                """;
            cmd2.Parameters.AddWithValue("@skill", skillId);
            cmd2.Parameters.AddWithValue("@band",  difficultyBand);
            cmd2.Parameters.AddWithValue("@count", count - items.Count);

            using var r2 = cmd2.ExecuteReader();
            while (r2.Read())
            {
                var extra = ReadItem(r2);
                // Avoid duplicates by id
                if (!items.Any(i => i.Id == extra.Id))
                    items.Add(extra);
            }
        }

        return items;
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
            Type          = itemType
        };
    }
}

// ---------------------------------------------------------------------------
// Supporting enums
// ---------------------------------------------------------------------------

/// <summary>Outcome categories for a completed post-lesson test.</summary>
public enum TestOutcome
{
    /// <summary>Score ≥ 90% — skill is fully mastered.</summary>
    Mastered,
    /// <summary>Score 70–89% — passed, continue to next skill.</summary>
    Passed,
    /// <summary>Score 50–69% — passed but warrants extra practice.</summary>
    NeedsReview,
    /// <summary>Score &lt; 50% — lesson should be repeated.</summary>
    Failed,
}
