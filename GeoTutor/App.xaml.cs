using System;
using System.Collections.Generic;
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

    /// <summary>Shared services kept alive for the process lifetime.</summary>
    public static DatabaseService?      Database       { get; private set; }
    public static SkillGraphService?    Skills         { get; private set; }
    public static SessionLoggerService? SessionLogger  { get; private set; }
    public static LessonEngineService?  LessonEngine   { get; private set; }
    public static DialogueEngineService? DialogueEngine { get; private set; }

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
            SeedUnit1Items(db);
            SeedUnit2Items(db);

            // Wire offline-safe lesson services (LLM/TTS gracefully return null when key is empty).
            var logger    = new SessionLoggerService(db);
            var llm       = new LlmGatewayService(db, "");    // offline: returns null on all calls
            var tts       = new TtsService(db, "", "eastus"); // offline: no audio synthesis
            var dialogue  = new DialogueEngineService(llm, tts, logger, db);
            var lesson    = new LessonEngineService(llm, skills, logger, db);

            // Keep alive for the duration of the app.
            Database       = db;
            Skills         = skills;
            SessionLogger  = logger;
            LessonEngine   = lesson;
            DialogueEngine = dialogue;
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
    /// Inserts the 15 Unit 1 library items (difficulties 1–3 for each of the
    /// 5 Foundations &amp; Logical Reasoning skills) into gt_items.
    /// </summary>
    private static void SeedUnit1Items(DatabaseService db) =>
        SeedItemList(db, Unit1Items.All);

    /// <summary>
    /// Inserts the 15 Unit 2 library items (difficulties 1–3 for each of the
    /// 5 Parallel Lines &amp; Transversals skills) into gt_items.
    /// </summary>
    private static void SeedUnit2Items(DatabaseService db) =>
        SeedItemList(db, Unit2Items.All);

    /// <summary>
    /// Inserts the 15 hard-coded geometry prerequisite items into gt_items
    /// if they are not already present (idempotent via INSERT OR IGNORE).
    /// </summary>
    private static void SeedGeometryPrereqItems(DatabaseService db) =>
        SeedItemList(db, GeometryPrereqItems.All);

    /// <summary>
    /// Inserts the 24 hard-coded algebra prerequisite items into gt_items.
    /// </summary>
    private static void SeedAlgebraItems(DatabaseService db) =>
        SeedItemList(db, AlgebraItems.All);

    /// <summary>
    /// Shared item seeder — INSERT OR IGNORE for every item in the list.
    /// </summary>
    private static void SeedItemList(DatabaseService db, IEnumerable<Item> source)
    {
        var conn = db.GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var item in source)
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
