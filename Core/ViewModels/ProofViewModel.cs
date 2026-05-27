namespace GeoTutor.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Services;
using ServiceProofExercise = GeoTutor.Core.Services.ProofExercise;
using ServiceProofPlacement = GeoTutor.Core.Services.ProofPlacement;

/// <summary>
/// Controls the drag-and-drop two-column proof beat.
/// Students drag statement/reason tiles into numbered rows; the view model
/// validates the placement against the authoritative step list held in the
/// <see cref="ProofExercise"/>.
/// </summary>
public partial class ProofViewModel : BaseViewModel
{
    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly ProofBeatService    _proofBeatService;
    private readonly SessionLoggerService _sessionLogger;

    // -----------------------------------------------------------------------
    // Observable properties
    // -----------------------------------------------------------------------

    [ObservableProperty] private ProofExercise?        _exercise;
    [ObservableProperty] private List<ProofPlacement>  _placements       = [];
    [ObservableProperty] private ProofValidationResult? _validationResult;
    [ObservableProperty] private int                   _hintsUsed;
    [ObservableProperty] private bool                  _isComplete;
    [ObservableProperty] private string                _feedbackMessage  = "";

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public ProofViewModel(
        ProofBeatService     proofBeatService,
        SessionLoggerService sessionLogger)
    {
        _proofBeatService = proofBeatService ?? throw new ArgumentNullException(nameof(proofBeatService));
        _sessionLogger    = sessionLogger    ?? throw new ArgumentNullException(nameof(sessionLogger));
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads a proof exercise for the given skill and difficulty band, then
    /// initialises an empty placement list with one slot per step.
    /// </summary>
    private async Task LoadProof(string skillId, int difficultyBand)
    {
        SetBusy(true, "Loading proof…");

        IsComplete        = false;
        HintsUsed         = 0;
        FeedbackMessage   = "";
        ValidationResult  = null;

        var serviceExercise = await _proofBeatService.GenerateProofAsync(skillId, difficultyBand);
        Exercise = MapFromServiceExercise(serviceExercise, skillId, difficultyBand);

        if (Exercise is null)
        {
            FeedbackMessage = "No proof exercise available for this skill.";
            SetBusy(false);
            return;
        }

        // Create one empty placement slot per step.
        Placements = Enumerable.Range(1, Exercise.Steps.Count)
            .Select(i => new ProofPlacement { StepNumber = i })
            .ToList();

        _sessionLogger.Log("proof_load", new
        {
            skillId,
            difficultyBand,
            exerciseId = Exercise.Id,
            stepCount  = Exercise.Steps.Count,
        });

        SetBusy(false);
    }

    /// <summary>
    /// Places a statement/reason pair into the specified step slot.
    /// Replaces any existing placement at that step.
    /// </summary>
    private void PlaceItem(int stepNumber, string statementId, string reasonId)
    {
        if (Exercise is null) return;

        int idx = Placements.FindIndex(p => p.StepNumber == stepNumber);
        if (idx < 0) return;

        // Create a new list so the ObservableProperty change notification fires.
        List<ProofPlacement> updated = [.. Placements];
        updated[idx] = new ProofPlacement
        {
            StepNumber  = stepNumber,
            StatementId = statementId,
            ReasonId    = reasonId,
        };
        Placements = updated;

        FeedbackMessage = "";
    }

    /// <summary>
    /// Clears the statement and reason from the given step slot.
    /// </summary>
    [RelayCommand]
    private void ClearStep(int stepNumber)
    {
        int idx = Placements.FindIndex(p => p.StepNumber == stepNumber);
        if (idx < 0) return;

        List<ProofPlacement> updated = [.. Placements];
        updated[idx] = new ProofPlacement { StepNumber = stepNumber };
        Placements = updated;
    }

    /// <summary>
    /// Validates the student's current placements against the correct step
    /// sequence.  Populates <see cref="ValidationResult"/> with per-step
    /// feedback and sets <see cref="IsComplete"/> when all steps are correct.
    /// </summary>
    [RelayCommand]
    private async Task Validate()
    {
        if (Exercise is null) return;

        SetBusy(true, "Checking proof…");

        var result = BuildValidationResult(Exercise, Placements);
        ValidationResult = result;

        if (result.AllCorrect)
        {
            IsComplete      = true;
            FeedbackMessage = "Proof complete — excellent reasoning!";

            _sessionLogger.Log("proof_complete", new
            {
                exerciseId = Exercise.Id,
                hintsUsed  = HintsUsed,
                correct    = true,
            });
        }
        else
        {
            int wrong = result.StepResults.Count(s => !s.IsCorrect);
            FeedbackMessage = wrong == 1
                ? "One step needs to be fixed."
                : $"{wrong} steps need to be fixed.";

            _sessionLogger.Log("proof_validate_fail", new
            {
                exerciseId    = Exercise.Id,
                wrongSteps    = result.StepResults
                                      .Where(s => !s.IsCorrect)
                                      .Select(s => s.StepNumber)
                                      .ToList(),
            });
        }

        SetBusy(false);
    }

    /// <summary>
    /// Reveals a hint for the first incorrect or empty step.
    /// Records the hint reveal in the session log.
    /// </summary>
    [RelayCommand]
    private void RevealHint()
    {
        if (Exercise is null) return;

        // Find the first step that is either empty or marked wrong.
        ProofPlacement? targetSlot = ValidationResult is not null
            ? FindFirstWrongSlot()
            : FindFirstEmptySlot();

        if (targetSlot is null)
        {
            FeedbackMessage = "All steps look good — try submitting.";
            return;
        }

        var step = Exercise.Steps.FirstOrDefault(s => s.StepNumber == targetSlot.StepNumber);
        if (step is null) return;

        // Show the correct statement as a hint, leave reason for the student.
        FeedbackMessage = $"Hint for step {step.StepNumber}: " +
                          $"The statement should be \"{step.StatementText}\".";
        HintsUsed++;

        _sessionLogger.Log("proof_hint", new
        {
            exerciseId = Exercise.Id,
            stepNumber = step.StepNumber,
            hintsUsed  = HintsUsed,
        });
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private ProofPlacement? FindFirstEmptySlot() =>
        Placements.FirstOrDefault(
            p => string.IsNullOrEmpty(p.StatementId) || string.IsNullOrEmpty(p.ReasonId));

    private ProofPlacement? FindFirstWrongSlot()
    {
        if (ValidationResult is null) return FindFirstEmptySlot();

        foreach (var sr in ValidationResult.StepResults.Where(s => !s.IsCorrect))
        {
            var slot = Placements.FirstOrDefault(p => p.StepNumber == sr.StepNumber);
            if (slot is not null) return slot;
        }

        return FindFirstEmptySlot();
    }

    private static ProofExercise MapFromServiceExercise(ServiceProofExercise source, string skillId, int difficultyBand)
    {
        return new ProofExercise
        {
            Id             = source.Id,
            SkillId        = skillId,
            DifficultyBand = difficultyBand,
            Title          = source.GivenText + " / " + source.ProveText,
            GivenText      = source.GivenText,
            ProveText      = source.ProveText,
            Steps          = source.CorrectSteps.Select(s => new ProofStep
            {
                StepNumber   = s.StepNumber,
                StatementId  = s.StatementId,
                StatementText = source.StatementBank.FirstOrDefault(p => p.Id == s.StatementId)?.Text ?? "",
                ReasonId     = s.ReasonId,
                ReasonText   = source.ReasonBank.FirstOrDefault(p => p.Id == s.ReasonId)?.Text ?? "",
            }).ToList(),
            StatementPool = source.StatementBank.Select(p => new ProofTile { Id = p.Id, Text = p.Text }).ToList(),
            ReasonPool    = source.ReasonBank.Select(p => new ProofTile { Id = p.Id, Text = p.Text }).ToList(),
        };
    }

    private static ProofValidationResult BuildValidationResult(ProofExercise exercise, List<ProofPlacement> placements)
    {
        var results = exercise.Steps.Select(step =>
        {
            var placement = placements.FirstOrDefault(p => p.StepNumber == step.StepNumber);
            bool statementCorrect = placement?.StatementId == step.StatementId;
            bool reasonCorrect    = placement?.ReasonId    == step.ReasonId;
            return new ProofStepResult
            {
                StepNumber       = step.StepNumber,
                IsCorrect        = statementCorrect && reasonCorrect,
                StatementCorrect = statementCorrect,
                ReasonCorrect    = reasonCorrect,
                HintText         = null,
            };
        }).ToList();

        return new ProofValidationResult
        {
            AllCorrect  = results.Count > 0 && results.All(r => r.IsCorrect),
            StepResults = results,
        };
    }
}

// ---------------------------------------------------------------------------
// Proof domain models
// ---------------------------------------------------------------------------

/// <summary>A complete two-column proof exercise.</summary>
public class ProofExercise
{
    public string            Id            { get; set; } = "";
    public string            SkillId       { get; set; } = "";
    public int               DifficultyBand{ get; set; } = 1;
    public string            Title         { get; set; } = "";
    public string            GivenText     { get; set; } = "";
    public string            ProveText     { get; set; } = "";

    /// <summary>Ordered correct steps (ground truth).</summary>
    public List<ProofStep>   Steps         { get; set; } = [];

    /// <summary>Pool of statement tiles available to the student.</summary>
    public List<ProofTile>   StatementPool { get; set; } = [];

    /// <summary>Pool of reason tiles available to the student.</summary>
    public List<ProofTile>   ReasonPool    { get; set; } = [];
}

/// <summary>One correct step in the proof (statement + reason).</summary>
public class ProofStep
{
    public int    StepNumber     { get; set; }
    public string StatementId   { get; set; } = "";
    public string StatementText { get; set; } = "";
    public string ReasonId      { get; set; } = "";
    public string ReasonText    { get; set; } = "";
}

/// <summary>A draggable tile shown in the statement or reason pool.</summary>
public class ProofTile
{
    public string Id   { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// The student's placement of a statement/reason into a specific step row.
/// </summary>
public class ProofPlacement
{
    public int    StepNumber  { get; set; }
    public string StatementId { get; set; } = "";
    public string ReasonId    { get; set; } = "";
}

/// <summary>The outcome of validating all student placements.</summary>
public class ProofValidationResult
{
    public bool                       AllCorrect   { get; set; }
    public List<ProofStepResult>      StepResults  { get; set; } = [];
}

/// <summary>Per-step validation feedback.</summary>
public class ProofStepResult
{
    public int    StepNumber        { get; set; }
    public bool   IsCorrect         { get; set; }
    public bool   StatementCorrect  { get; set; }
    public bool   ReasonCorrect     { get; set; }
    public string? HintText         { get; set; }
}
