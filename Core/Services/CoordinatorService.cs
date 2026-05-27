using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Implements the lesson-selection policy from §4.3.
///
/// Scoring function:
///   score(n) = (1 - m_n) * prereqReadiness(n) * recencyBoost(n) + interleaveBonus(n)
///
/// Every 4th lesson forces an interleave: the coordinator picks a previously-Solid
/// (mastery >= 0.70) node for mixed review rather than running the normal scorer.
/// </summary>
public class CoordinatorService
{
    private readonly SkillGraphService _skillGraph;
    private readonly DatabaseService  _db;

    // Mastery threshold for a skill to be considered "Solid" for interleave review.
    private const double SolidThreshold = 0.70;

    // Recency window: skills seen within this many hours get a recency penalty
    // (we prefer spreading practice over time).
    private const double RecencyWindowHours = 4.0;
    private const double RecencyPenalty     = 0.5;

    // Interleave bonus applied to Solid skills when it is NOT an interleave turn
    // (small pull to keep recently-mastered skills from being totally ignored).
    private const double InterleaveBonusValue = 0.05;

    // Per-session state
    private int _lessonsCompleted = 0;
    private readonly List<string> _sessionSkillHistory = [];

    public int LessonsCompletedThisSession => _lessonsCompleted;

    public CoordinatorService(SkillGraphService skillGraph, DatabaseService db)
    {
        _skillGraph = skillGraph ?? throw new ArgumentNullException(nameof(skillGraph));
        _db         = db         ?? throw new ArgumentNullException(nameof(db));
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the next skill to teach.
    /// On every 4th lesson (1-indexed: lessons 4, 8, 12, …) an interleave is forced:
    /// a previously-Solid skill is chosen for mixed review.
    /// Otherwise the skill with the highest scoring-function value is returned.
    /// </summary>
    public Skill SelectNextSkill()
    {
        var allSkills = _skillGraph.GetAllSkills();

        bool forceInterleave = _lessonsCompleted > 0 && (_lessonsCompleted % 4 == 0);

        if (forceInterleave)
        {
            var interleaveCandidate = SelectInterleaveSkill(allSkills);
            if (interleaveCandidate is not null)
                return interleaveCandidate;
            // Fall through to normal selection if no Solid skills exist yet.
        }

        var unlocked = allSkills.Where(s => s.IsUnlocked).ToList();
        if (unlocked.Count == 0)
            throw new InvalidOperationException("No unlocked skills are available for selection.");

        // Score every unlocked skill and return the highest.
        var scored = unlocked
            .Select(s => (Skill: s, Score: ScoreSkill(s, allSkills)))
            .OrderByDescending(x => x.Score)
            .ToList();

        return scored[0].Skill;
    }

    /// <summary>
    /// Must be called after a lesson completes so the coordinator can track session
    /// progress and write a lesson-result row to the session log.
    /// </summary>
    public void RecordLessonResult(string skillId, double testScore)
    {
        _lessonsCompleted++;
        _sessionSkillHistory.Add(skillId);

        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO gt_session_log (event_type, ts, payload_json)
            VALUES ('lesson_result', @ts, @payload);
            """;
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@payload",
            System.Text.Json.JsonSerializer.Serialize(new { skillId, testScore }));
        cmd.ExecuteNonQuery();
    }

    // -----------------------------------------------------------------------
    // Scoring helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// score(n) = (1 - m_n) * prereqReadiness(n) * recencyBoost(n) + interleaveBonus(n)
    /// </summary>
    private double ScoreSkill(Skill skill, List<Skill> allSkills)
    {
        double gap        = 1.0 - skill.CurrentMastery;          // how much is left to learn
        double prereqR    = PrereqReadiness(skill, allSkills);
        double recencyB   = RecencyBoost(skill);
        double interleaveB = InterleaveBonus(skill);

        return gap * prereqR * recencyB + interleaveB;
    }

    /// <summary>
    /// prereqReadiness(n): average mastery of all direct prerequisites, or 1.0 if none.
    /// Scales [0, 1]. Skills whose prereqs are weak will naturally score lower.
    /// </summary>
    private double PrereqReadiness(Skill skill, List<Skill> allSkills)
    {
        if (skill.PrereqIds.Count == 0)
            return 1.0;

        var masteryById = allSkills.ToDictionary(s => s.Id, s => s.CurrentMastery);

        double sum   = 0.0;
        int    count = 0;
        foreach (var prereqId in skill.PrereqIds)
        {
            if (masteryById.TryGetValue(prereqId, out double m))
            {
                sum += m;
                count++;
            }
        }

        return count == 0 ? 0.0 : sum / count;
    }

    /// <summary>
    /// recencyBoost(n): penalises skills that were seen very recently (within the
    /// recency window) to encourage spacing. Returns a value in (0, 1].
    /// </summary>
    private static double RecencyBoost(Skill skill)
    {
        if (skill.LastSeen is null)
            return 1.0;

        double hoursSinceSeen = (DateTime.UtcNow - skill.LastSeen.Value).TotalHours;
        if (hoursSinceSeen < RecencyWindowHours)
            return RecencyPenalty;

        return 1.0;
    }

    /// <summary>
    /// interleaveBonus(n): a small constant bonus for Solid skills during non-interleave
    /// turns, to keep them occasionally surfaced for spaced review.
    /// </summary>
    private static double InterleaveBonus(Skill skill)
        => skill.CurrentMastery >= SolidThreshold ? InterleaveBonusValue : 0.0;

    // -----------------------------------------------------------------------
    // Interleave selection
    // -----------------------------------------------------------------------

    /// <summary>
    /// For an interleave turn: picks the Solid skill that was seen longest ago
    /// (maximum staleness), prioritising skills not visited this session.
    /// Returns null if no Solid skills exist.
    /// </summary>
    private Skill? SelectInterleaveSkill(List<Skill> allSkills)
    {
        var solidSkills = allSkills
            .Where(s => s.IsUnlocked && s.CurrentMastery >= SolidThreshold)
            .ToList();

        if (solidSkills.Count == 0)
            return null;

        // Prefer skills not visited in this session.
        var notInSession = solidSkills
            .Where(s => !_sessionSkillHistory.Contains(s.Id))
            .ToList();

        var candidates = notInSession.Count > 0 ? notInSession : solidSkills;

        // Among candidates, pick the one with the oldest (or null) LastSeen.
        return candidates
            .OrderBy(s => s.LastSeen.HasValue ? s.LastSeen.Value : DateTime.MinValue)
            .First();
    }
}
