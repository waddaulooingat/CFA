using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Manages the skill DAG: seeding, querying, mastery updates, decay, and unlock gating.
/// </summary>
public class SkillGraphService
{
    private readonly DatabaseService _db;

    // Mastery thresholds
    private const double UnlockThreshold  = 0.70;
    private const double BandPromoteScore = 0.80;  // promote difficulty band if mastery >= this
    private const double BandDemoteScore  = 0.40;  // demote difficulty band if mastery <= this
    private const double DecayFactor      = 0.97;  // weekly decay multiplier for unseen skills

    // How much a single correct / incorrect attempt shifts mastery
    private const double CorrectDelta   =  0.08;
    private const double IncorrectDelta = -0.06;

    public SkillGraphService(DatabaseService db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // -----------------------------------------------------------------------
    // Seeding
    // -----------------------------------------------------------------------

    /// <summary>
    /// Inserts all skill nodes if gt_skills is empty. Idempotent.
    /// </summary>
    public void SeedIfEmpty()
    {
        var conn = _db.GetConnection();

        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM gt_skills;";
            var count = Convert.ToInt64(check.ExecuteScalar()!);
            if (count > 0)
                return;
        }

        var skills = BuildSkillSeed();

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var s in skills)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT OR IGNORE INTO gt_skills
                        (id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked)
                    VALUES
                        (@id, @name, @unit, @prereq_ids, @diff, @mastery, NULL, @unlocked);
                    """;
                cmd.Parameters.AddWithValue("@id",        s.Id);
                cmd.Parameters.AddWithValue("@name",      s.Name);
                cmd.Parameters.AddWithValue("@unit",      s.Unit);
                cmd.Parameters.AddWithValue("@prereq_ids", JsonSerializer.Serialize(s.PrereqIds));
                cmd.Parameters.AddWithValue("@diff",      s.DifficultyBand);
                cmd.Parameters.AddWithValue("@mastery",   s.CurrentMastery);
                // Nodes with no prereqs are immediately unlocked
                cmd.Parameters.AddWithValue("@unlocked",  s.PrereqIds.Count == 0 ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Queries
    // -----------------------------------------------------------------------

    public List<Skill> GetAllSkills()
    {
        var conn = _db.GetConnection();
        var result = new List<Skill>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked FROM gt_skills ORDER BY unit, id;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadSkill(reader));

        return result;
    }

    public Skill? GetSkill(string id)
    {
        var conn = _db.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked FROM gt_skills WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadSkill(reader) : null;
    }

    /// <summary>
    /// Returns all unlocked skills whose current mastery is strictly below maxMastery.
    /// Ordered by mastery ascending so the least-mastered items surface first.
    /// </summary>
    public List<Skill> GetUnlockedSkillsBelowMastery(double maxMastery)
    {
        var conn = _db.GetConnection();
        var result = new List<Skill>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked
            FROM gt_skills
            WHERE is_unlocked = 1 AND current_mastery < @max
            ORDER BY current_mastery ASC;
            """;
        cmd.Parameters.AddWithValue("@max", maxMastery);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadSkill(reader));

        return result;
    }

    // -----------------------------------------------------------------------
    // Mastery mutation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies a mastery delta based on whether the attempt was correct, then writes
    /// a mastery-log row and recalculates difficulty band and unlock state.
    /// </summary>
    public void UpdateMastery(string skillId, bool correct, long timeMs)
    {
        var skill = GetSkill(skillId);
        if (skill is null)
            throw new InvalidOperationException($"Skill '{skillId}' not found.");

        double delta = correct ? CorrectDelta : IncorrectDelta;

        // Apply a small speed bonus: if the answer was correct and fast (< 15 s), nudge up.
        if (correct && timeMs is > 0 and < 15_000)
            delta += 0.01;

        double newMastery = Math.Clamp(skill.CurrentMastery + delta, 0.0, 1.0);

        // Difficulty band adjustment
        int newBand = skill.DifficultyBand;
        if (newMastery >= BandPromoteScore && skill.DifficultyBand < 5)
            newBand = skill.DifficultyBand + 1;
        else if (newMastery <= BandDemoteScore && skill.DifficultyBand > 1)
            newBand = skill.DifficultyBand - 1;

        string nowUtc = DateTime.UtcNow.ToString("O");

        var conn = _db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            // Update skill row
            using (var update = conn.CreateCommand())
            {
                update.CommandText = """
                    UPDATE gt_skills
                    SET current_mastery = @mastery,
                        difficulty_band = @band,
                        last_seen       = @ts
                    WHERE id = @id;
                    """;
                update.Parameters.AddWithValue("@mastery", newMastery);
                update.Parameters.AddWithValue("@band",    newBand);
                update.Parameters.AddWithValue("@ts",      nowUtc);
                update.Parameters.AddWithValue("@id",      skillId);
                update.ExecuteNonQuery();
            }

            // Append mastery log entry
            using (var log = conn.CreateCommand())
            {
                log.CommandText = """
                    INSERT INTO gt_mastery_log (skill_id, ts, mastery_score, difficulty_band, event)
                    VALUES (@skill_id, @ts, @score, @band, @event);
                    """;
                log.Parameters.AddWithValue("@skill_id", skillId);
                log.Parameters.AddWithValue("@ts",       nowUtc);
                log.Parameters.AddWithValue("@score",    newMastery);
                log.Parameters.AddWithValue("@band",     newBand);
                log.Parameters.AddWithValue("@event",    correct ? "attempt_correct" : "attempt_incorrect");
                log.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        // Recalculate unlock gates after mastery changes.
        RecalculateUnlocked();
    }

    /// <summary>
    /// Applies weekly decay (m *= 0.97) to every skill that has not been seen
    /// within the past 7 days. Logs each decayed skill to gt_mastery_log.
    /// </summary>
    public void ApplyWeeklyDecay()
    {
        var allSkills = GetAllSkills();
        var cutoff    = DateTime.UtcNow.AddDays(-7);
        string nowUtc = DateTime.UtcNow.ToString("O");

        var conn = _db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var skill in allSkills)
            {
                bool unseen = skill.LastSeen is null || skill.LastSeen < cutoff;
                if (!unseen || skill.CurrentMastery <= 0.0)
                    continue;

                double decayed = Math.Clamp(skill.CurrentMastery * DecayFactor, 0.0, 1.0);

                using (var update = conn.CreateCommand())
                {
                    update.CommandText = "UPDATE gt_skills SET current_mastery = @m WHERE id = @id;";
                    update.Parameters.AddWithValue("@m",  decayed);
                    update.Parameters.AddWithValue("@id", skill.Id);
                    update.ExecuteNonQuery();
                }

                using (var log = conn.CreateCommand())
                {
                    log.CommandText = """
                        INSERT INTO gt_mastery_log (skill_id, ts, mastery_score, difficulty_band, event)
                        VALUES (@skill_id, @ts, @score, @band, 'decay');
                        """;
                    log.Parameters.AddWithValue("@skill_id", skill.Id);
                    log.Parameters.AddWithValue("@ts",       nowUtc);
                    log.Parameters.AddWithValue("@score",    decayed);
                    log.Parameters.AddWithValue("@band",     skill.DifficultyBand);
                    log.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        RecalculateUnlocked();
    }

    /// <summary>
    /// Re-evaluates is_unlocked for every skill: a skill is unlocked when ALL of its
    /// prerequisite skills have current_mastery >= 0.70 (or have no prereqs).
    /// </summary>
    public void RecalculateUnlocked()
    {
        var allSkills   = GetAllSkills();
        var masteryById = allSkills.ToDictionary(s => s.Id, s => s.CurrentMastery);

        var conn = _db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var skill in allSkills)
            {
                bool unlocked;
                if (skill.PrereqIds.Count == 0)
                {
                    unlocked = true;
                }
                else
                {
                    unlocked = skill.PrereqIds.All(prereqId =>
                        masteryById.TryGetValue(prereqId, out double m) && m >= UnlockThreshold);
                }

                using var update = conn.CreateCommand();
                update.CommandText = "UPDATE gt_skills SET is_unlocked = @u WHERE id = @id;";
                update.Parameters.AddWithValue("@u",  unlocked ? 1 : 0);
                update.Parameters.AddWithValue("@id", skill.Id);
                update.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static Skill ReadSkill(SqliteDataReader r)
    {
        var prereqJson = r.GetString(3);
        List<string> prereqs;
        try   { prereqs = JsonSerializer.Deserialize<List<string>>(prereqJson) ?? []; }
        catch { prereqs = []; }

        DateTime? lastSeen = null;
        if (!r.IsDBNull(6))
        {
            var raw = r.GetString(6);
            if (DateTime.TryParse(raw, out var dt))
                lastSeen = dt.ToUniversalTime();
        }

        return new Skill
        {
            Id              = r.GetString(0),
            Name            = r.GetString(1),
            Unit            = r.GetInt32(2),
            PrereqIds       = prereqs,
            DifficultyBand  = r.GetInt32(4),
            CurrentMastery  = r.GetDouble(5),
            LastSeen        = lastSeen,
            IsUnlocked      = r.GetInt32(7) != 0
        };
    }

    // -----------------------------------------------------------------------
    // Skill seed data — 80 geo + 8 algebra prerequisite skills
    // -----------------------------------------------------------------------

    // -----------------------------------------------------------------------
    // Migration: replace old geo-u1-* placeholder IDs with content-pack IDs
    // -----------------------------------------------------------------------

    /// <summary>
    /// Idempotent migration that replaces the five old Unit 1 skill IDs with
    /// the canonical IDs from unit-01-skills.json.  Safe to call on every startup.
    /// </summary>
    public void MigrateUnit1Skills()
    {
        var conn = _db.GetConnection();

        // Old IDs that are no longer used.
        var obsolete = new[]
        {
            "geo-u1-definitions",
            "geo-u1-angles",
            "geo-u1-logic-ifthen",
            "geo-u1-converse",
            "geo-u1-counterexample",
        };

        // New IDs from unit-01-skills.json.
        var newSkills = BuildSkillSeed()
            .Where(s => s.Id.StartsWith("geo-u1-", StringComparison.Ordinal))
            .ToList();

        using var tx = conn.BeginTransaction();
        try
        {
            // Remove old rows (won't exist on a fresh install — no-op then).
            foreach (var id in obsolete)
            {
                using var del = conn.CreateCommand();
                del.Transaction  = tx;
                del.CommandText  = "DELETE FROM gt_skills WHERE id = @id;";
                del.Parameters.AddWithValue("@id", id);
                del.ExecuteNonQuery();
            }

            // Upsert new rows — update metadata if row already exists, but
            // never touch current_mastery or last_seen (those track student progress).
            foreach (var s in newSkills)
            {
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO gt_skills
                        (id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked)
                    VALUES
                        (@id, @name, @unit, @prereq_ids, @diff, @mastery, NULL, @unlocked)
                    ON CONFLICT(id) DO UPDATE SET
                        name           = excluded.name,
                        unit           = excluded.unit,
                        prereq_ids     = excluded.prereq_ids,
                        difficulty_band = excluded.difficulty_band;
                    """;
                ins.Parameters.AddWithValue("@id",        s.Id);
                ins.Parameters.AddWithValue("@name",      s.Name);
                ins.Parameters.AddWithValue("@unit",      s.Unit);
                ins.Parameters.AddWithValue("@prereq_ids", JsonSerializer.Serialize(s.PrereqIds));
                ins.Parameters.AddWithValue("@diff",      s.DifficultyBand);
                ins.Parameters.AddWithValue("@mastery",   s.CurrentMastery);
                ins.Parameters.AddWithValue("@unlocked",  s.PrereqIds.Count == 0 ? 1 : 0);
                ins.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Idempotent migration that upserts all five Unit 2 skill nodes from
    /// unit-02-skills.json into gt_skills.  Safe to call on every startup.
    /// </summary>
    public void MigrateUnit2Skills()
    {
        var conn = _db.GetConnection();

        var unit2Skills = BuildSkillSeed()
            .Where(s => s.Id.StartsWith("geo-u2-", StringComparison.Ordinal))
            .ToList();

        // Skill IDs that were seeded by an earlier version and must be removed.
        string[] obsoleteUnit2Ids =
        [
            "geo-u2-parallel-identify", "geo-u2-parallel-proofs",
            "geo-u2-perpendicular",     "geo-u2-angle-relationships",
        ];

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var obsoleteId in obsoleteUnit2Ids)
            {
                using var del = conn.CreateCommand();
                del.Transaction = tx;
                del.CommandText = "DELETE FROM gt_skills WHERE id = @id;";
                del.Parameters.AddWithValue("@id", obsoleteId);
                del.ExecuteNonQuery();
            }

            foreach (var s in unit2Skills)
            {
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO gt_skills
                        (id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked)
                    VALUES
                        (@id, @name, @unit, @prereq_ids, @diff, @mastery, NULL, @unlocked)
                    ON CONFLICT(id) DO UPDATE SET
                        name            = excluded.name,
                        unit            = excluded.unit,
                        prereq_ids      = excluded.prereq_ids,
                        difficulty_band = excluded.difficulty_band;
                    """;
                ins.Parameters.AddWithValue("@id",        s.Id);
                ins.Parameters.AddWithValue("@name",      s.Name);
                ins.Parameters.AddWithValue("@unit",      s.Unit);
                ins.Parameters.AddWithValue("@prereq_ids", JsonSerializer.Serialize(s.PrereqIds));
                ins.Parameters.AddWithValue("@diff",      s.DifficultyBand);
                ins.Parameters.AddWithValue("@mastery",   s.CurrentMastery);
                ins.Parameters.AddWithValue("@unlocked",  s.PrereqIds.Count == 0 ? 1 : 0);
                ins.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static List<Skill> BuildSkillSeed()
    {
        // Helper: unit 0 = algebra prereq pseudo-unit rendered before geo work begins
        static Skill S(string id, string name, int unit, int band = 1, params string[] prereqs)
            => new()
            {
                Id             = id,
                Name           = name,
                Unit           = unit,
                DifficultyBand = band,
                PrereqIds      = [.. prereqs]
            };

        return
        [
            // ----------------------------------------------------------------
            // Unit 0 — Algebra prerequisites (8 nodes, no prereqs, always unlocked)
            // ----------------------------------------------------------------
            S("alg-linear-eq",         "Solving linear equations",            0),
            S("alg-systems",           "Systems of linear equations",         0, 1, "alg-linear-eq"),
            S("alg-factoring",         "Factoring polynomials",               0),
            S("alg-quadratic-formula", "Quadratic formula",                   0, 1, "alg-factoring"),
            S("alg-exponents-radicals","Exponents and radicals",              0),
            S("alg-function-notation", "Function notation",                   0),
            S("alg-slope-lines",       "Slope and linear equations",          0, 1, "alg-linear-eq"),
            S("alg-distance-midpoint", "Distance and midpoint formulas",      0, 1, "alg-slope-lines"),

            // ----------------------------------------------------------------
            // Unit 1 — Foundations & Logical Reasoning (5 nodes, from unit-01-skills.json)
            // ----------------------------------------------------------------
            S("geo-u1-point-line-plane",  "Points, Lines, and Planes",  1),
            S("geo-u1-segments-and-rays", "Segments and Rays",          1, 1, "geo-u1-point-line-plane"),
            S("geo-u1-angle-measure",     "Angle Measure and Types",    1, 1, "geo-u1-point-line-plane"),
            S("geo-u1-if-then-logic",     "If-Then Logic",              1, 1, "geo-u1-point-line-plane"),
            S("geo-u1-counterexamples",   "Counterexamples",            1, 1, "geo-u1-if-then-logic"),

            // ----------------------------------------------------------------
            // Unit 2 — Parallel Lines & Transversals (5 nodes)
            // ----------------------------------------------------------------
            S("geo-u2-parallel-basics",     "Parallel and Perpendicular Lines",                2, 1,
                "geo-u1-point-line-plane"),
            S("geo-u2-transversal-angles",  "Angles Formed by a Transversal",                  2, 1,
                "geo-u2-parallel-basics", "geo-u1-angle-measure"),
            S("geo-u2-parallel-theorems",   "Parallel Line Theorems",                          2, 1,
                "geo-u2-transversal-angles"),
            S("geo-u2-proving-parallel",    "Proving Lines Parallel",                          2, 2,
                "geo-u2-parallel-theorems", "geo-u1-if-then-logic"),
            S("geo-u2-coordinate-parallel", "Parallel & Perpendicular in the Coordinate Plane", 2, 2,
                "geo-u2-parallel-basics"),

            // ----------------------------------------------------------------
            // Unit 3 — Triangle Congruence (7 nodes)
            // ----------------------------------------------------------------
            S("geo-u3-sss",               "SSS congruence postulate",          3, 1,
                "geo-u1-angle-measure", "geo-u2-transversal-angles"),
            S("geo-u3-sas",               "SAS congruence postulate",          3, 1,
                "geo-u3-sss"),
            S("geo-u3-asa",               "ASA congruence postulate",          3, 1,
                "geo-u3-sss"),
            S("geo-u3-aas",               "AAS congruence theorem",            3, 2,
                "geo-u3-asa"),
            S("geo-u3-hl",                "HL theorem for right triangles",    3, 2,
                "geo-u3-sas", "geo-u2-parallel-basics"),
            S("geo-u3-cpctc",             "CPCTC",                             3, 2,
                "geo-u3-sss", "geo-u3-sas", "geo-u3-asa"),
            S("geo-u3-congruence-proofs", "Triangle congruence proofs",        3, 3,
                "geo-u3-cpctc", "geo-u1-if-then-logic"),

            // ----------------------------------------------------------------
            // Unit 4 — Triangle Properties (7 nodes)
            // ----------------------------------------------------------------
            S("geo-u4-midsegment",          "Triangle midsegment theorem",         4, 2,
                "geo-u3-sss"),
            S("geo-u4-perpbisector",        "Perpendicular bisector and circumcenter", 4, 2,
                "geo-u3-congruence-proofs", "geo-u2-parallel-basics"),
            S("geo-u4-angbisector",         "Angle bisector and incenter",         4, 2,
                "geo-u3-congruence-proofs"),
            S("geo-u4-median-centroid",     "Medians and centroid",                4, 2,
                "geo-u4-midsegment"),
            S("geo-u4-altitude-orthocenter","Altitudes and orthocenter",           4, 2,
                "geo-u4-median-centroid"),
            S("geo-u4-triangle-inequality", "Triangle inequality theorem",         4, 1,
                "geo-u3-sss"),
            S("geo-u4-isosceles-theorem",   "Isosceles triangle theorem and converse", 4, 2,
                "geo-u3-cpctc"),

            // ----------------------------------------------------------------
            // Unit 5 — Similarity & Proportionality (6 nodes)
            // ----------------------------------------------------------------
            S("geo-u5-similar-triangles",     "AA, SAS, SSS similarity",                   5, 2,
                "geo-u3-sas", "geo-u4-midsegment", "alg-linear-eq"),
            S("geo-u5-scale-factor",           "Scale factor and proportionality",           5, 1,
                "geo-u5-similar-triangles", "alg-linear-eq"),
            S("geo-u5-side-splitter",          "Side-splitter theorem",                     5, 2,
                "geo-u5-similar-triangles"),
            S("geo-u5-angle-bisector-prop",    "Angle bisector proportionality theorem",    5, 2,
                "geo-u4-angbisector", "geo-u5-similar-triangles"),
            S("geo-u5-similar-proofs",         "Proofs using similarity",                   5, 3,
                "geo-u5-similar-triangles", "geo-u3-congruence-proofs"),
            S("geo-u5-perimeter-area-ratio",   "Ratios of perimeters and areas",            5, 2,
                "geo-u5-scale-factor"),

            // ----------------------------------------------------------------
            // Unit 6 — Right Triangle Trigonometry (6 nodes)
            // ----------------------------------------------------------------
            S("geo-u6-pythagorean",      "Pythagorean theorem and its converse",  6, 1,
                "geo-u3-sss", "alg-exponents-radicals"),
            S("geo-u6-special-triangles","30-60-90 and 45-45-90 triangles",       6, 2,
                "geo-u6-pythagorean"),
            S("geo-u6-trig-ratios",      "Sine, cosine, tangent ratios",          6, 2,
                "geo-u6-pythagorean", "alg-slope-lines"),
            S("geo-u6-solving-triangles","Solving for missing sides/angles",      6, 3,
                "geo-u6-trig-ratios", "alg-linear-eq"),
            S("geo-u6-inverse-trig",     "Inverse trig functions",                6, 3,
                "geo-u6-solving-triangles", "alg-function-notation"),
            S("geo-u6-applications",     "Real-world trig applications",          6, 3,
                "geo-u6-inverse-trig"),

            // ----------------------------------------------------------------
            // Unit 7 — Quadrilaterals & Polygons (7 nodes)
            // ----------------------------------------------------------------
            S("geo-u7-parallelogram",          "Properties of parallelograms",           7, 2,
                "geo-u3-congruence-proofs", "geo-u2-proving-parallel"),
            S("geo-u7-rectangle-rhombus-square","Special parallelograms",               7, 2,
                "geo-u7-parallelogram"),
            S("geo-u7-trapezoid",               "Trapezoids and kites",                  7, 2,
                "geo-u7-parallelogram"),
            S("geo-u7-polygon-angles",          "Interior and exterior angle sums",      7, 2,
                "geo-u2-transversal-angles"),
            S("geo-u7-regular-polygons",        "Regular polygons and their properties", 7, 2,
                "geo-u7-polygon-angles"),
            S("geo-u7-quad-proofs",             "Proving quadrilateral types",           7, 3,
                "geo-u7-parallelogram", "geo-u7-rectangle-rhombus-square"),
            S("geo-u7-coordinate-quad",         "Quadrilaterals in the coordinate plane",7, 3,
                "geo-u7-parallelogram", "alg-slope-lines", "alg-distance-midpoint"),

            // ----------------------------------------------------------------
            // Unit 8 — Circles (8 nodes)
            // ----------------------------------------------------------------
            S("geo-u8-circle-basics",      "Center, radius, diameter, chord, arc",    8, 1,
                "geo-u1-point-line-plane"),
            S("geo-u8-arc-measure",        "Arc measure and arc length",              8, 2,
                "geo-u8-circle-basics", "alg-linear-eq"),
            S("geo-u8-chord-relationships","Chord-chord angle, intersecting chords",  8, 2,
                "geo-u8-arc-measure"),
            S("geo-u8-tangent-line",       "Tangent lines and tangent-radius",        8, 2,
                "geo-u8-circle-basics", "geo-u6-pythagorean"),
            S("geo-u8-inscribed-angle",    "Inscribed angle theorem",                 8, 2,
                "geo-u8-arc-measure"),
            S("geo-u8-secant-tangent",     "Secant-tangent angle relationships",      8, 3,
                "geo-u8-inscribed-angle", "geo-u8-tangent-line"),
            S("geo-u8-circle-equations",   "Standard equation of a circle",           8, 3,
                "geo-u8-circle-basics", "alg-quadratic-formula", "alg-distance-midpoint"),
            S("geo-u8-inscribed-polygon",  "Inscribed polygons and cyclic quadrilaterals", 8, 3,
                "geo-u8-inscribed-angle", "geo-u7-regular-polygons"),

            // ----------------------------------------------------------------
            // Unit 9 — Area & Volume (7 nodes)
            // ----------------------------------------------------------------
            S("geo-u9-polygon-area",            "Area of polygons",                    9, 1,
                "geo-u7-polygon-angles", "alg-linear-eq"),
            S("geo-u9-circle-area",             "Area and circumference of circles",   9, 1,
                "geo-u8-circle-basics", "alg-linear-eq"),
            S("geo-u9-sector-area",             "Sector and segment area",             9, 2,
                "geo-u9-circle-area", "geo-u8-arc-measure"),
            S("geo-u9-surface-area",            "Surface area of 3D solids",           9, 2,
                "geo-u9-polygon-area"),
            S("geo-u9-volume-prism-cylinder",   "Volume of prisms and cylinders",      9, 2,
                "geo-u9-polygon-area", "geo-u9-circle-area"),
            S("geo-u9-volume-pyramid-cone",     "Volume of pyramids and cones",        9, 3,
                "geo-u9-volume-prism-cylinder"),
            S("geo-u9-volume-sphere",           "Volume and surface area of spheres",  9, 3,
                "geo-u9-volume-pyramid-cone", "geo-u8-circle-basics"),

            // ----------------------------------------------------------------
            // Unit 10 — Transformations (6 nodes)
            // ----------------------------------------------------------------
            S("geo-u10-translation", "Translations",                        10, 1,
                "geo-u1-point-line-plane", "alg-slope-lines"),
            S("geo-u10-reflection",  "Reflections",                         10, 1,
                "geo-u10-translation"),
            S("geo-u10-rotation",    "Rotations",                           10, 2,
                "geo-u10-reflection"),
            S("geo-u10-composition", "Composition of transformations",      10, 3,
                "geo-u10-rotation"),
            S("geo-u10-dilation",    "Dilations and scale factor",          10, 2,
                "geo-u10-translation", "geo-u5-scale-factor"),
            S("geo-u10-symmetry",    "Line symmetry and rotational symmetry",10, 2,
                "geo-u10-rotation"),

            // ----------------------------------------------------------------
            // Unit 11 — Coordinate Geometry Proofs (5 nodes)
            // ----------------------------------------------------------------
            S("geo-u11-midpoint-distance",  "Midpoint and distance formulas",               11, 1,
                "alg-distance-midpoint"),
            S("geo-u11-slope-parallel-perp","Slope for parallel and perpendicular lines",   11, 2,
                "alg-slope-lines", "geo-u2-parallel-basics"),
            S("geo-u11-classify-triangles", "Classifying triangles using coordinates",      11, 2,
                "geo-u11-midpoint-distance", "geo-u11-slope-parallel-perp"),
            S("geo-u11-classify-quads",     "Classifying quadrilaterals using coordinates", 11, 2,
                "geo-u11-classify-triangles", "geo-u7-parallelogram"),
            S("geo-u11-coord-proofs",       "Coordinate geometry proofs",                   11, 3,
                "geo-u11-classify-quads", "geo-u3-congruence-proofs"),

            // ----------------------------------------------------------------
            // Unit 12 — Constructions (5 nodes)
            // ----------------------------------------------------------------
            S("geo-u12-bisect-segment",     "Perpendicular bisector construction",    12, 1,
                "geo-u4-perpbisector"),
            S("geo-u12-bisect-angle",       "Angle bisector construction",            12, 1,
                "geo-u4-angbisector"),
            S("geo-u12-parallel-line",      "Constructing parallel lines",            12, 2,
                "geo-u12-bisect-segment", "geo-u2-parallel-basics"),
            S("geo-u12-equilateral-triangle","Constructing equilateral triangles",    12, 2,
                "geo-u12-bisect-segment", "geo-u3-sss"),
            S("geo-u12-inscribed-circle",   "Inscribed and circumscribed circles",    12, 3,
                "geo-u12-bisect-angle", "geo-u8-inscribed-polygon"),

            // ----------------------------------------------------------------
            // Unit 13 — Two-Column Proofs (4 nodes)
            // ----------------------------------------------------------------
            S("geo-u13-proof-structure", "Proof structure: given, prove, statements, reasons", 13, 1,
                "geo-u1-if-then-logic"),
            S("geo-u13-segment-proofs",  "Segment addition, midpoint proofs",                  13, 2,
                "geo-u13-proof-structure", "geo-u1-point-line-plane"),
            S("geo-u13-angle-proofs",    "Angle addition, supplementary/complementary proofs", 13, 2,
                "geo-u13-proof-structure", "geo-u1-angle-measure"),
            S("geo-u13-triangle-proofs", "Triangle congruence two-column proofs",              13, 3,
                "geo-u13-segment-proofs", "geo-u13-angle-proofs", "geo-u3-congruence-proofs"),

            // ----------------------------------------------------------------
            // Unit 14 — Capstone (2 nodes)
            // ----------------------------------------------------------------
            S("geo-u14-mixed-review-a", "Mixed review: Units 1–7", 14, 3,
                "geo-u7-quad-proofs", "geo-u6-applications", "geo-u5-similar-proofs"),
            S("geo-u14-mixed-review-b", "Mixed review: Units 8–13", 14, 4,
                "geo-u8-inscribed-polygon", "geo-u9-volume-sphere", "geo-u11-coord-proofs",
                "geo-u13-triangle-proofs", "geo-u14-mixed-review-a"),
        ];
    }
}
