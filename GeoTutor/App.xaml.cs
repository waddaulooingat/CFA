using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using GeoTutor.Core.Models;
using GeoTutor.Core.Services;
using GeoTutor.Data;
using Microsoft.Data.Sqlite;

namespace GeoTutor;

/// <summary>
/// Startup: initialises the SQLite schema, seeds the skill graph and the
/// algebra prerequisite item bank, then shows MainWindow.
/// No API keys needed for Phase 1 / baseline assessment.
/// </summary>
public partial class App : Application
{
    public static AudioPlayerService AudioPlayer { get; } = new();

    /// <summary>Shared database service kept alive for the process lifetime.</summary>
    public static DatabaseService? Database { get; private set; }
    public static SkillGraphService? Skills { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        InitDatabase();

        var win = new MainWindow();
        MainWindow = win;
        win.Show();
    }

    private static void InitDatabase()
    {
        try
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GeoTutor", "geotutor.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var db = new DatabaseService(dbPath);
            db.EnsureMigrated();

            var skills = new SkillGraphService(db);
            skills.SeedIfEmpty();

            SeedAlgebraItems(db);
            SeedGeometryPrereqSkills(db);
            SeedGeometryPrereqItems(db);

            // Keep alive for the duration of the app.
            Database = db;
            Skills   = skills;
        }
        catch
        {
            // Non-fatal — renderer works without DB.
        }
    }

    /// <summary>
    /// Inserts the 3 new unit-0 geometry prereq skill nodes that are not part
    /// of the main 88-node skill graph seed.  Uses INSERT OR IGNORE so it is
    /// safe to call on every startup.
    /// </summary>
    private static void SeedGeometryPrereqSkills(DatabaseService db)
    {
        // (id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked)
        var newSkills = new[]
        {
            ("geo-prereq-triangle-basics", "Triangle angle sum and classification", 0, "[]", 1, 0.0, 1),
            ("geo-prereq-coord-plane",     "Coordinate plane fluency",              0, "[]", 1, 0.0, 1),
            ("geo-prereq-area-perimeter",  "Area and perimeter of basic shapes",    0, "[]", 1, 0.0, 1),
        };

        var conn = db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var (id, name, unit, prereqs, band, mastery, unlocked) in newSkills)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT OR IGNORE INTO gt_skills
                        (id, name, unit, prereq_ids, difficulty_band, current_mastery, last_seen, is_unlocked)
                    VALUES
                        (@id, @name, @unit, @prereqs, @band, @mastery, NULL, @unlocked);
                    """;
                cmd.Parameters.AddWithValue("@id",      id);
                cmd.Parameters.AddWithValue("@name",    name);
                cmd.Parameters.AddWithValue("@unit",    unit);
                cmd.Parameters.AddWithValue("@prereqs", prereqs);
                cmd.Parameters.AddWithValue("@band",    band);
                cmd.Parameters.AddWithValue("@mastery", mastery);
                cmd.Parameters.AddWithValue("@unlocked", unlocked);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
        }
    }

    /// <summary>
    /// Inserts the 15 hard-coded geometry prerequisite items into gt_items
    /// if they are not already present (idempotent via INSERT OR IGNORE).
    /// </summary>
    private static void SeedGeometryPrereqItems(DatabaseService db)
    {
        var conn = db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var item in GeometryPrereqItems.All)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT OR IGNORE INTO gt_items
                        (id, template, params_json, prompt, answer_json,
                         hints_json, solution_steps_json, skill_id, difficulty, source, item_type)
                    VALUES
                        (@id, @tpl, @params, @prompt, @answer,
                         @hints, @steps, @skill, @diff, @src, @type);
                    """;
                cmd.Parameters.AddWithValue("@id",     item.Id);
                cmd.Parameters.AddWithValue("@tpl",    item.Template);
                cmd.Parameters.AddWithValue("@params", item.ParamsJson);
                cmd.Parameters.AddWithValue("@prompt", item.Prompt);
                cmd.Parameters.AddWithValue("@answer", item.AnswerJson);
                cmd.Parameters.AddWithValue("@hints",  JsonSerializer.Serialize(item.Hints));
                cmd.Parameters.AddWithValue("@steps",  JsonSerializer.Serialize(item.SolutionSteps));
                cmd.Parameters.AddWithValue("@skill",  item.SkillId);
                cmd.Parameters.AddWithValue("@diff",   item.Difficulty);
                cmd.Parameters.AddWithValue("@src",    item.Source);
                cmd.Parameters.AddWithValue("@type",   item.Type.ToString());
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
        }
    }

    /// <summary>
    /// Inserts the 24 hard-coded algebra prerequisite items into gt_items
    /// if they are not already present (idempotent via INSERT OR IGNORE).
    /// </summary>
    private static void SeedAlgebraItems(DatabaseService db)
    {
        var conn = db.GetConnection();

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var item in AlgebraItems.All)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT OR IGNORE INTO gt_items
                        (id, template, params_json, prompt, answer_json,
                         hints_json, solution_steps_json, skill_id, difficulty, source, item_type)
                    VALUES
                        (@id, @tpl, @params, @prompt, @answer,
                         @hints, @steps, @skill, @diff, @src, @type);
                    """;

                cmd.Parameters.AddWithValue("@id",     item.Id);
                cmd.Parameters.AddWithValue("@tpl",    item.Template);
                cmd.Parameters.AddWithValue("@params", item.ParamsJson);
                cmd.Parameters.AddWithValue("@prompt", item.Prompt);
                cmd.Parameters.AddWithValue("@answer", item.AnswerJson);
                cmd.Parameters.AddWithValue("@hints",  JsonSerializer.Serialize(item.Hints));
                cmd.Parameters.AddWithValue("@steps",  JsonSerializer.Serialize(item.SolutionSteps));
                cmd.Parameters.AddWithValue("@skill",  item.SkillId);
                cmd.Parameters.AddWithValue("@diff",   item.Difficulty);
                cmd.Parameters.AddWithValue("@src",    item.Source);
                cmd.Parameters.AddWithValue("@type",   item.Type.ToString());

                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            // Non-fatal — baseline will fall back to synthesised items.
        }
    }
}
