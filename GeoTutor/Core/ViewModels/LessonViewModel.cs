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
using GeoTutor.SceneEngine.Models;
using Microsoft.Data.Sqlite;

/// <summary>
/// Controls a full lesson session: beat sequencing, item presentation, hint
/// reveals, wrong-answer dialogue, and retry problem flow.
/// </summary>
public partial class LessonViewModel : BaseViewModel
{
    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly LessonEngineService   _lessonEngine;
    private readonly DialogueEngineService _dialogueEngine;
    private readonly SessionLoggerService  _sessionLogger;
    private readonly SkillGraphService     _skillGraph;
    private readonly DatabaseService       _database;
    private readonly AudioPlayerService    _audio;

    // -----------------------------------------------------------------------
    // Internal state
    // -----------------------------------------------------------------------

    private readonly Stopwatch _answerTimer = new();
    private Item?  _retryItem;
    private bool   _inRetryMode;

    // Raised when a dialogue line requests a canvas action (highlight, morph, etc.)
    public event Action<CanvasAction>? CanvasActionRequested;

    // -----------------------------------------------------------------------
    // Observable properties — lesson / beat
    // -----------------------------------------------------------------------

    [ObservableProperty] private Lesson?   _currentLesson;
    [ObservableProperty] private Beat?     _currentBeat;
    [ObservableProperty] private int       _currentBeatIndex;

    // -----------------------------------------------------------------------
    // Observable properties — item / answer
    // -----------------------------------------------------------------------

    [ObservableProperty] private Item?    _currentItem;
    [ObservableProperty] private SceneSpec? _currentScene;
    [ObservableProperty] private string   _userAnswer      = "";
    [ObservableProperty] private string   _feedbackMessage = "";
    [ObservableProperty] private bool     _isAnswerCorrect;

    // -----------------------------------------------------------------------
    // Observable properties — hints
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool   _showHint;
    [ObservableProperty] private string _currentHint         = "";
    [ObservableProperty] private int    _hintsRevealedCount;

    // -----------------------------------------------------------------------
    // Observable properties — dialogue
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool           _showDialogue;
    [ObservableProperty] private DialogueScript? _activeDialogue;
    [ObservableProperty] private int            _dialogueLineIndex;
    [ObservableProperty] private bool           _isAudioPlaying;

    public DialogueLine? CurrentDialogueLine =>
        ActiveDialogue is not null && DialogueLineIndex < ActiveDialogue.Lines.Count
            ? ActiveDialogue.Lines[DialogueLineIndex]
            : null;

    partial void OnDialogueLineIndexChanged(int value) => OnPropertyChanged(nameof(CurrentDialogueLine));
    partial void OnActiveDialogueChanged(DialogueScript? value) => OnPropertyChanged(nameof(CurrentDialogueLine));

    // -----------------------------------------------------------------------
    // Observable properties — completion
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool _isLessonComplete;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public LessonViewModel(
        LessonEngineService   lessonEngine,
        DialogueEngineService dialogueEngine,
        SessionLoggerService  sessionLogger,
        SkillGraphService     skillGraph,
        DatabaseService       database,
        AudioPlayerService    audio)
    {
        _lessonEngine   = lessonEngine   ?? throw new ArgumentNullException(nameof(lessonEngine));
        _dialogueEngine = dialogueEngine ?? throw new ArgumentNullException(nameof(dialogueEngine));
        _sessionLogger  = sessionLogger  ?? throw new ArgumentNullException(nameof(sessionLogger));
        _skillGraph     = skillGraph     ?? throw new ArgumentNullException(nameof(skillGraph));
        _database       = database       ?? throw new ArgumentNullException(nameof(database));
        _audio          = audio          ?? throw new ArgumentNullException(nameof(audio));
        _audio.PlaybackEnded += () => IsAudioPlaying = false;
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads the lesson for the given skill and presents the first beat.
    /// </summary>
    [RelayCommand]
    private async Task LoadLesson(string skillId)
    {
        SetBusy(true, "Loading lesson…");

        IsLessonComplete   = false;
        CurrentBeatIndex   = 0;
        HintsRevealedCount = 0;
        ShowHint           = false;
        ShowDialogue       = false;
        IsAnswerCorrect    = false;
        FeedbackMessage    = "";
        UserAnswer         = "";
        _retryItem         = null;
        _inRetryMode       = false;

        _dialogueEngine.ResetEscalation();

        var skill = _skillGraph.GetSkill(skillId);
        int band  = skill?.DifficultyBand ?? 1;
        CurrentLesson = await _lessonEngine.BuildLessonAsync(skillId, band).ConfigureAwait(false);

        if (CurrentLesson is null || CurrentLesson.Beats.Count == 0)
        {
            FeedbackMessage = "No lesson content available for this skill.";
            SetBusy(false);
            return;
        }

        _sessionLogger.LogLessonStart(skillId, CurrentLesson.Id);
        SetBusy(false);
        ActivateBeat(0);
    }

    /// <summary>
    /// Evaluates the student's current answer.  On correct: advance; on wrong:
    /// classify error and launch dialogue remediation.
    /// </summary>
    [RelayCommand]
    private async Task SubmitAnswer()
    {
        if (CurrentItem is null || string.IsNullOrWhiteSpace(UserAnswer))
        {
            FeedbackMessage = "Please enter an answer.";
            return;
        }

        long timeMs = _answerTimer.ElapsedMilliseconds;
        _answerTimer.Reset();

        string trimmed  = UserAnswer.Trim();
        bool   correct  = EvaluateAnswer(CurrentItem, trimmed);
        IsAnswerCorrect = correct;

        _sessionLogger.LogAnswer(CurrentItem.Id, correct, timeMs, HintsRevealedCount);
        _skillGraph.UpdateMastery(CurrentItem.SkillId, correct, timeMs);

        if (correct)
        {
            FeedbackMessage = "Correct!";
            UserAnswer      = "";

            await Task.Delay(700);
            FeedbackMessage = "";

            if (_inRetryMode)
            {
                // Retry succeeded — close dialogue state and continue beat.
                _inRetryMode = false;
                _retryItem   = null;
                ShowDialogue = false;
                await AdvanceBeatOrComplete();
            }
            else
            {
                await AdvanceBeatOrComplete();
            }
        }
        else
        {
            FeedbackMessage = "That's not right — let's take a look.";
            UserAnswer      = "";

            await Task.Delay(500);
            await LaunchDialogueAsync(CurrentItem);
        }
    }

    /// <summary>
    /// Reveals the next unused hint for the current item.
    /// </summary>
    [RelayCommand]
    private void RevealHint()
    {
        if (CurrentItem is null) return;
        if (HintsRevealedCount >= CurrentItem.Hints.Count)
        {
            FeedbackMessage = "No more hints available.";
            return;
        }

        CurrentHint = CurrentItem.Hints[HintsRevealedCount];
        HintsRevealedCount++;
        ShowHint = true;

        _sessionLogger.LogHintReveal(CurrentItem.Id, HintsRevealedCount - 1);
    }

    /// <summary>
    /// Advances to the next beat when the current beat is non-interactive
    /// (e.g. a Concept prose beat that the student reads then clicks "Next").
    /// </summary>
    [RelayCommand]
    private async Task NextBeat()
    {
        await AdvanceBeatOrComplete();
    }

    /// <summary>
    /// Advances the active dialogue by one line.
    /// </summary>
    [RelayCommand]
    private void NextDialogueLine()
    {
        if (ActiveDialogue is null) return;

        DialogueLineIndex++;
        if (DialogueLineIndex >= ActiveDialogue.Lines.Count)
        {
            _audio.Stop();
            ShowDialogue = false;
            PresentRetryItem();
            return;
        }

        PlayCurrentDialogueLine();
    }

    [RelayCommand]
    private void SkipDialogue()
    {
        _audio.Stop();
        ShowDialogue = false;
        PresentRetryItem();
    }

    private void PlayCurrentDialogueLine()
    {
        if (ActiveDialogue is null || DialogueLineIndex >= ActiveDialogue.Lines.Count) return;

        var line = ActiveDialogue.Lines[DialogueLineIndex];

        if (line.AudioFilePath is not null)
        {
            IsAudioPlaying = true;
            _audio.Play(line.AudioFilePath);
        }

        if (line.Action is not null)
            CanvasActionRequested?.Invoke(line.Action);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void ActivateBeat(int index)
    {
        if (CurrentLesson is null || index >= CurrentLesson.Beats.Count)
        {
            FinishLesson();
            return;
        }

        CurrentBeatIndex   = index;
        CurrentBeat        = CurrentLesson.Beats[index];
        HintsRevealedCount = 0;
        ShowHint           = false;
        FeedbackMessage    = "";
        UserAnswer         = "";

        _sessionLogger.LogBeatEnter(
            CurrentBeat.Type.ToString(),
            CurrentBeat.SceneSpecId);

        // Load scene spec for this beat: prefer inline scene, then DB lookup.
        if (CurrentBeat.Scene is not null)
            CurrentScene = CurrentBeat.Scene;
        else if (!string.IsNullOrEmpty(CurrentBeat.SceneSpecId))
            CurrentScene = LoadSceneSpec(CurrentBeat.SceneSpecId);
        else
            CurrentScene = null;

        // Load first item for the beat (if any).
        if (CurrentBeat.ItemIds.Count > 0)
        {
            CurrentItem = LoadItem(CurrentBeat.ItemIds[0]);
            _answerTimer.Restart();
        }
        else
        {
            CurrentItem = null;
        }
    }

    private async Task AdvanceBeatOrComplete()
    {
        if (CurrentLesson is null) return;

        int nextIndex = CurrentBeatIndex + 1;
        if (nextIndex >= CurrentLesson.Beats.Count)
        {
            FinishLesson();
            return;
        }

        await Task.Yield(); // allow UI to refresh before switching beats
        ActivateBeat(nextIndex);
    }

    private void FinishLesson()
    {
        IsLessonComplete = true;
        FeedbackMessage  = "Lesson complete! Great work.";
        if (CurrentLesson is not null)
            _sessionLogger.LogLessonEnd(CurrentLesson.SkillId, 1.0);
    }

    private async Task LaunchDialogueAsync(Item failedItem)
    {
        SetBusy(true, "Preparing feedback…");

        string errorCategory = await Task.Run(() =>
            ClassifyError(failedItem, UserAnswer));

        _sessionLogger.LogDialoguePlay(errorCategory, failedItem.Id);

        // Determine which item will be offered as a retry.
        string retryItemId = string.IsNullOrEmpty(failedItem.Id)
            ? failedItem.Id
            : failedItem.Id;

        DialogueScript? script = await _dialogueEngine.PrepareDialogueAsync(
            errorCategory,
            retryItemId,
            CurrentScene is null ? "{}" : JsonSerializer.Serialize(CurrentScene));

        SetBusy(false);

        if (script is null)
        {
            // Fallback: just let the student retry immediately.
            PresentRetryItem();
            return;
        }

        _dialogueEngine.RecordDialoguePlayed(errorCategory);

        ActiveDialogue    = script;
        DialogueLineIndex = 0;
        ShowDialogue      = true;

        // Play line 0 immediately
        PlayCurrentDialogueLine();

        // Pre-store the retry item so PresentRetryItem can access it.
        if (!string.IsNullOrEmpty(script.RetryProblemId))
            _retryItem = LoadItem(script.RetryProblemId) ?? failedItem;
        else
            _retryItem = failedItem;
    }

    private void PresentRetryItem()
    {
        if (_retryItem is null) return;

        _inRetryMode       = true;
        CurrentItem        = _retryItem;
        HintsRevealedCount = 0;
        ShowHint           = false;
        FeedbackMessage    = "Let's try again.";
        UserAnswer         = "";
        _answerTimer.Restart();
    }

    // -----------------------------------------------------------------------
    // Answer evaluation (matches BaselineViewModel approach)
    // -----------------------------------------------------------------------

    private static bool EvaluateAnswer(Item item, string raw)
    {
        try
        {
            using var doc  = JsonDocument.Parse(item.AnswerJson);
            var       root = doc.RootElement;

            // Multiple-choice
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

            // Numeric ± tolerance
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

    /// <summary>
    /// Heuristic error classifier used when the LLM is unavailable or the
    /// answer submission is fast enough that we don't want to incur an LLM
    /// round-trip just for classification.
    /// </summary>
    private static string ClassifyError(Item item, string raw)
    {
        // Check if user attempted a numeric answer with a sign issue.
        if (double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double givenVal))
        {
            try
            {
                using var doc = JsonDocument.Parse(item.AnswerJson);
                if (doc.RootElement.TryGetProperty("value", out var vp))
                {
                    double expected = vp.GetDouble();
                    if (Math.Abs(givenVal + expected) < 0.5)
                        return "sign-error";
                    if (Math.Abs(givenVal - expected) == 1.0)
                        return "off-by-one-arc";
                }
            }
            catch { /* ignore */ }
        }

        // Skill-ID-based heuristic fallback
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

        return "sign-error"; // generic fallback
    }

    // -----------------------------------------------------------------------
    // Database helpers
    // -----------------------------------------------------------------------

    private Item? LoadItem(string itemId)
    {
        var conn = _database.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, template, params_json, prompt, answer_json,
                   hints_json, solution_steps_json, skill_id, difficulty, source, item_type
            FROM gt_items
            WHERE id = @id
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@id", itemId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadItem(reader) : null;
    }

    private SceneSpec? LoadSceneSpec(string sceneSpecId)
    {
        try
        {
            var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT spec_json FROM gt_scene_specs WHERE id = @id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", sceneSpecId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return JsonSerializer.Deserialize<SceneSpec>(reader.GetString(0));
        }
        catch
        {
            return null;
        }
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
