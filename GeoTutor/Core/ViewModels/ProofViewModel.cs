namespace GeoTutor.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Services;

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
    public async Task LoadProof(string skillId, int difficultyBand)
    {
        SetBusy(true, "Loading proof…");

        IsComplete        = false;
        HintsUsed         = 0;
        FeedbackMessage   = "";
        ValidationResult  = null;

        Exercise = await _proofBeatService.GenerateProofAsync(skillId, difficultyBand);

        if (Exercise is null)
        {
            FeedbackMessage = "No proof exercise available for this skill.";
            SetBusy(false);
            return;
        }

        // Create one empty placement slot per step.
        Placements = Enumerable.Range(1, Exercise.CorrectSteps.Count)
            .Select(i => new ProofPlacement { StepNumber = i })
            .ToList();

        _sessionLogger.Log("proof_load", new
        {
            skillId,
            difficultyBand,
            exerciseId = Exercise.Id,
            stepCount  = Exercise.CorrectSteps.Count,
        });

        SetBusy(false);
    }

    /// <summary>
    /// Places a statement/reason pair into the specified step slot.
    /// Replaces any existing placement at that step.
    /// </summary>
    public void PlaceItem(int stepNumber, string statementId, string reasonId)
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
    /// sequence.
    /// </summary>
    [RelayCommand]
    private async Task Validate()
    {
        if (Exercise is null) return;

        SetBusy(true, "Checking proof…");

        var result = await Task.Run(() => _proofBeatService.Validate(Exercise, Placements));
        ValidationResult = result;

        if (result.IsComplete)
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
            int wrong = Exercise.CorrectSteps.Count - result.CorrectCount;
            FeedbackMessage = wrong == 1
                ? "One step needs to be fixed."
                : $"{wrong} steps need to be fixed.";

            _sessionLogger.Log("proof_validate_fail", new
            {
                exerciseId    = Exercise.Id,
                firstWrongStep = result.FirstWrongStep,
            });
        }

        SetBusy(false);
    }

    /// <summary>
    /// Reveals a hint for the first incorrect or empty step.
    /// </summary>
    [RelayCommand]
    private void RevealHint()
    {
        if (Exercise is null) return;

        ProofPlacement? targetSlot = ValidationResult is not null
            ? FindFirstWrongSlot()
            : FindFirstEmptySlot();

        if (targetSlot is null)
        {
            FeedbackMessage = "All steps look good — try submitting.";
            return;
        }

        var step = Exercise.CorrectSteps.FirstOrDefault(s => s.StepNumber == targetSlot.StepNumber);
        if (step is null) return;

        // Look up the statement text from the bank using the step's StatementId.
        string statementText = Exercise.StatementBank
            .FirstOrDefault(item => item.Id == step.StatementId)?.Text ?? step.StatementId;

        FeedbackMessage = $"Hint for step {step.StepNumber}: " +
                          $"The statement should be \"{statementText}\".";
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

        if (ValidationResult.FirstWrongStep.HasValue)
        {
            var slot = Placements.FirstOrDefault(p => p.StepNumber == ValidationResult.FirstWrongStep.Value);
            if (slot is not null) return slot;
        }

        return FindFirstEmptySlot();
    }
}
