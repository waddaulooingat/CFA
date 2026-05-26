namespace GeoTutor.Core.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Services;
using Microsoft.Data.Sqlite;

public enum AppView { Baseline, Lesson, Test, ParentDashboard, Loading }

public partial class MainViewModel : BaseViewModel
{
    private readonly SkillGraphService _skillGraphService;
    private readonly DatabaseService _databaseService;

    [ObservableProperty] private AppView _currentView = AppView.Loading;
    [ObservableProperty] private string _currentSkillId = "";
    [ObservableProperty] private string _currentSkillName = "";
    [ObservableProperty] private double _sessionProgress; // 0.0–1.0

    public MainViewModel(
        SkillGraphService skillGraphService,
        DatabaseService databaseService)
    {
        _skillGraphService = skillGraphService ?? throw new ArgumentNullException(nameof(skillGraphService));
        _databaseService   = databaseService   ?? throw new ArgumentNullException(nameof(databaseService));
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    [RelayCommand]
    private void NavigateToBaseline()
    {
        CurrentView = AppView.Baseline;
        SessionProgress = 0.0;
    }

    [RelayCommand]
    private void NavigateToLesson(string skillId)
    {
        var skill = _skillGraphService.GetSkill(skillId);
        CurrentSkillId   = skillId;
        CurrentSkillName = skill?.Name ?? skillId;
        CurrentView      = AppView.Lesson;
        SessionProgress  = 0.0;
    }

    [RelayCommand]
    private void NavigateToParentDashboard()
    {
        CurrentView = AppView.ParentDashboard;
    }

    // -----------------------------------------------------------------------
    // Initialization
    // -----------------------------------------------------------------------

    /// <summary>
    /// Called once on app start. Seeds skills, checks baseline, routes to the
    /// correct initial view.
    /// </summary>
    public async Task InitializeAsync()
    {
        SetBusy(true, "Loading...");

        try
        {
            await Task.Run(() =>
            {
                // 1. Ensure skill graph is populated.
                _skillGraphService.SeedIfEmpty();
            });

            // 2. Check whether a baseline result has ever been recorded.
            bool baselineRun = await Task.Run(() => HasBaselineResult());

            if (!baselineRun)
            {
                // 3a. No baseline — send student to the adaptive assessment.
                CurrentView = AppView.Baseline;
            }
            else
            {
                // 3b. Baseline exists — pick the next skill to study.
                var nextSkill = await Task.Run(() => SelectNextSkill());

                if (nextSkill is not null)
                {
                    CurrentSkillId   = nextSkill.Id;
                    CurrentSkillName = nextSkill.Name;
                    CurrentView      = AppView.Lesson;
                }
                else
                {
                    // All skills mastered — fall back to parent dashboard.
                    CurrentView = AppView.ParentDashboard;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Startup error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private bool HasBaselineResult()
    {
        var conn = _databaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM gt_baseline_results;";
        return Convert.ToInt64(cmd.ExecuteScalar()!) > 0;
    }

    /// <summary>
    /// Returns the unlocked skill with the lowest current mastery, preferring
    /// skills whose mastery is below the 0.80 "solid" threshold.
    /// Returns null only when every unlocked skill is fully mastered (≥ 1.0).
    /// </summary>
    private Models.Skill? SelectNextSkill()
    {
        var candidates = _skillGraphService.GetUnlockedSkillsBelowMastery(0.80);
        if (candidates.Count > 0)
            return candidates[0]; // already sorted by mastery ASC

        // All below-0.80 skills exhausted — look for anything below perfect.
        var remaining = _skillGraphService.GetUnlockedSkillsBelowMastery(1.0);
        return remaining.Count > 0 ? remaining[0] : null;
    }
}
