using System;
using System.Collections.Generic;
using System.Linq;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Encapsulates all mastery mathematics (§4.2, §7.1).
/// Pure static functions — no I/O, no state — so every caller can depend on
/// identical, testable arithmetic.
/// </summary>
public static class MasteryService
{
    // -----------------------------------------------------------------------
    // Scoring constants (§4.2)
    // -----------------------------------------------------------------------

    /// <summary>Base mastery gain for a correct answer with no hints used.</summary>
    private const double CorrectBaseGain   =  0.15;

    /// <summary>Mastery penalty applied per hint consumed on a correct attempt.</summary>
    private const double HintPenaltyPerUse =  0.25;

    /// <summary>Mastery penalty for an incorrect attempt.</summary>
    private const double WrongPenalty      = -0.12;

    /// <summary>Weekly decay multiplier applied to skills not recently seen (§7.1).</summary>
    private const double WeeklyDecayFactor =  0.97;

    /// <summary>Mastery threshold required for a promotion check (§4.2).</summary>
    private const double PromoteThreshold  =  0.80;

    /// <summary>Minimum number of recent attempts required to evaluate promotion.</summary>
    private const int    PromoteWindowSize =     5;

    /// <summary>Number of consecutive incorrect attempts that triggers a demotion.</summary>
    private const int    DemoteConsecutiveWrong = 2;

    /// <summary>Minimum mastery every prerequisite must reach before a node unlocks.</summary>
    private const double PrereqGateThreshold = 0.70;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the delta to apply to a skill's mastery score after a single attempt.
    /// <para>
    /// Correct with no hints: +0.15<br/>
    /// Correct with hints: +0.15 − (0.25 × hintsUsed), floored at 0.0<br/>
    /// Incorrect: −0.12
    /// </para>
    /// The returned delta is not clamped here; callers should apply
    /// <see cref="Clamp"/> after adding it to the current mastery.
    /// </summary>
    /// <param name="correct">Whether the student's answer was correct.</param>
    /// <param name="hintsUsed">Number of hints the student revealed before answering.</param>
    public static double ComputeMasteryDelta(bool correct, int hintsUsed)
    {
        if (!correct)
            return WrongPenalty;

        double gain = CorrectBaseGain - HintPenaltyPerUse * Math.Max(0, hintsUsed);
        return Math.Max(0.0, gain);   // hint overuse can reduce gain to 0 but never negative
    }

    /// <summary>
    /// Applies spaced-repetition decay for the time since the skill was last practiced.
    /// Formula: m × 0.97^weeks, where weeks = elapsed.TotalDays / 7.
    /// </summary>
    /// <param name="mastery">Current mastery score in [0, 1].</param>
    /// <param name="elapsed">Time since the skill was last seen.</param>
    /// <returns>Decayed mastery, clamped to [0, 1].</returns>
    public static double ApplyDecay(double mastery, TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return Clamp(mastery);

        double weeks   = elapsed.TotalDays / 7.0;
        double decayed = mastery * Math.Pow(WeeklyDecayFactor, weeks);
        return Clamp(decayed);
    }

    /// <summary>
    /// Promotion check (§4.2): the student should be promoted to the next difficulty
    /// band when the last <see cref="PromoteWindowSize"/> attempts are all correct
    /// (i.e. RecentAttempts are all ≥ 0.80 – checked by mastery level being ≥ 0.80)
    /// AND the median response time is below <paramref name="thresholdMs"/>.
    /// </summary>
    /// <param name="skill">
    ///   The skill to evaluate. <see cref="Skill.CurrentMastery"/> must be ≥ 0.80 and
    ///   <see cref="Skill.RecentAttempts"/> must contain at least
    ///   <see cref="PromoteWindowSize"/> entries all at or above 0.80.
    /// </param>
    /// <param name="medianTimeMs">
    ///   Median response time in milliseconds across the recent attempt window.
    /// </param>
    /// <param name="thresholdMs">
    ///   Maximum acceptable median response time (default: 45 s = 45 000 ms).
    /// </param>
    /// <returns><c>true</c> if a band promotion is warranted.</returns>
    public static bool ShouldPromote(Skill skill, long medianTimeMs, long thresholdMs = 45_000)
    {
        if (skill is null) throw new ArgumentNullException(nameof(skill));

        // Need at least a full window of recent attempts.
        if (skill.RecentAttempts.Count < PromoteWindowSize)
            return false;

        // Check that the last N attempts are all at or above the promotion threshold.
        var window = skill.RecentAttempts
            .Skip(skill.RecentAttempts.Count - PromoteWindowSize)
            .ToList();

        bool allAboveThreshold = window.All(m => m >= PromoteThreshold);
        if (!allAboveThreshold)
            return false;

        // Also check the skill's overall mastery and speed gate.
        return skill.CurrentMastery >= PromoteThreshold
            && medianTimeMs < thresholdMs;
    }

    /// <summary>
    /// Demotion check (§4.2): the student should be moved down a difficulty band when
    /// the last two recorded attempts in <see cref="Skill.RecentAttempts"/> are both
    /// wrong (score of 0.0 is treated as "incorrect", anything positive as "correct").
    /// </summary>
    /// <param name="skill">The skill to evaluate.</param>
    /// <returns><c>true</c> if a band demotion is warranted.</returns>
    public static bool ShouldDemote(Skill skill)
    {
        if (skill is null) throw new ArgumentNullException(nameof(skill));

        if (skill.RecentAttempts.Count < DemoteConsecutiveWrong)
            return false;

        // Inspect the most-recent N entries (stored in chronological order).
        var tail = skill.RecentAttempts
            .Skip(skill.RecentAttempts.Count - DemoteConsecutiveWrong)
            .ToList();

        // A mastery delta of exactly 0.0 or negative means the attempt was wrong
        // (positive gain signals a correct answer; 0.0 means wrong or zero-gain correct).
        // We compare against the wrong penalty to detect incorrect attempts reliably:
        // an entry < 0 unambiguously came from a wrong answer (delta = −0.12 added).
        // We treat any non-positive delta as "incorrect" for the demotion gate.
        return tail.All(m => m <= 0.0);
    }

    /// <summary>
    /// Prerequisite gate check (§4.2): every prerequisite node must have
    /// <see cref="Skill.CurrentMastery"/> ≥ 0.70 for <paramref name="skill"/> to be
    /// considered unlockable.
    /// Nodes with no prerequisites always return <c>true</c>.
    /// </summary>
    /// <param name="skill">The candidate skill node.</param>
    /// <param name="allSkills">
    ///   Complete dictionary of all skill nodes keyed by <see cref="Skill.Id"/>.
    /// </param>
    /// <returns>
    ///   <c>true</c> if all prerequisite mastery scores meet the gate threshold,
    ///   or if the skill has no prerequisites.
    /// </returns>
    public static bool ArePrereqsMet(Skill skill, Dictionary<string, Skill> allSkills)
    {
        if (skill is null)     throw new ArgumentNullException(nameof(skill));
        if (allSkills is null) throw new ArgumentNullException(nameof(allSkills));

        if (skill.PrereqIds.Count == 0)
            return true;

        foreach (string prereqId in skill.PrereqIds)
        {
            if (!allSkills.TryGetValue(prereqId, out Skill? prereq))
                return false;   // prerequisite not found — treat as unmet

            if (prereq.CurrentMastery < PrereqGateThreshold)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Clamps a mastery score to the valid range [0.0, 1.0].
    /// </summary>
    public static double Clamp(double mastery) => Math.Clamp(mastery, 0.0, 1.0);
}
