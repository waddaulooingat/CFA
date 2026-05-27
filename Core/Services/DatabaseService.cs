using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace GeoTutor.Core.Services;

/// <summary>
/// Manages the single local SQLite database file (geotutor.db).
/// Handles connection lifetime and schema migrations via the gt_migrations table.
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;
    private bool _disposed;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    /// <summary>
    /// Returns the single shared connection, opening it if necessary.
    /// </summary>
    public SqliteConnection GetConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            // Enable WAL mode for better concurrent read performance and crash safety.
            using var wal = _connection.CreateCommand();
            wal.CommandText = "PRAGMA journal_mode=WAL;";
            wal.ExecuteNonQuery();

            using var fk = _connection.CreateCommand();
            fk.CommandText = "PRAGMA foreign_keys=ON;";
            fk.ExecuteNonQuery();
        }

        return _connection;
    }

    /// <summary>
    /// Applies all pending SQL migrations in order, tracking applied ones in gt_migrations.
    /// Safe to call on every startup — already-applied migrations are skipped.
    /// </summary>
    public void EnsureMigrated()
    {
        var conn = GetConnection();

        // Bootstrap the migrations tracking table before doing anything else.
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS gt_migrations (
                    id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    applied_at TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        ApplyMigration(conn, "001_core_tables", Migration001_CoreTables);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void ApplyMigration(SqliteConnection conn, string name, Action<SqliteConnection> action)
    {
        // Check whether this migration has already run.
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM gt_migrations WHERE name = @name;";
            check.Parameters.AddWithValue("@name", name);
            var count = Convert.ToInt64(check.ExecuteScalar()!);
            if (count > 0)
                return;
        }

        // Run the migration inside a transaction so a partial failure leaves
        // the database in a clean state.
        using var tx = conn.BeginTransaction();
        try
        {
            action(conn);

            using var insert = conn.CreateCommand();
            insert.CommandText = """
                INSERT INTO gt_migrations (name, applied_at)
                VALUES (@name, @ts);
                """;
            insert.Parameters.AddWithValue("@name", name);
            insert.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("O"));
            insert.ExecuteNonQuery();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void Migration001_CoreTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS gt_skills (
                id               TEXT    PRIMARY KEY,
                name             TEXT    NOT NULL,
                unit             INTEGER NOT NULL,
                prereq_ids       TEXT    NOT NULL DEFAULT '[]',
                difficulty_band  INTEGER NOT NULL DEFAULT 1,
                current_mastery  REAL    NOT NULL DEFAULT 0.0,
                last_seen        TEXT,
                is_unlocked      INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS gt_lessons (
                id             TEXT    PRIMARY KEY,
                skill_id       TEXT    NOT NULL,
                beat_sequence  TEXT    NOT NULL DEFAULT '[]',
                version        INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS gt_items (
                id                   TEXT    PRIMARY KEY,
                template             TEXT    NOT NULL,
                params_json          TEXT    NOT NULL DEFAULT '{}',
                prompt               TEXT    NOT NULL DEFAULT '',
                answer_json          TEXT    NOT NULL DEFAULT '{}',
                hints_json           TEXT    NOT NULL DEFAULT '[]',
                solution_steps_json  TEXT    NOT NULL DEFAULT '[]',
                skill_id             TEXT    NOT NULL,
                difficulty           INTEGER NOT NULL DEFAULT 1,
                source               TEXT    NOT NULL DEFAULT 'library',
                item_type            TEXT    NOT NULL DEFAULT 'Procedural'
            );

            CREATE TABLE IF NOT EXISTS gt_attempts (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                item_id          TEXT    NOT NULL,
                ts               TEXT    NOT NULL,
                correct          INTEGER NOT NULL,
                time_ms          INTEGER NOT NULL DEFAULT 0,
                hints_used       INTEGER NOT NULL DEFAULT 0,
                dialogue_played  INTEGER NOT NULL DEFAULT 0,
                error_category   TEXT
            );

            CREATE TABLE IF NOT EXISTS gt_mastery_log (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                skill_id         TEXT    NOT NULL,
                ts               TEXT    NOT NULL,
                mastery_score    REAL    NOT NULL,
                difficulty_band  INTEGER NOT NULL,
                event            TEXT    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS gt_scene_specs (
                id          TEXT PRIMARY KEY,
                spec_json   TEXT NOT NULL,
                created_at  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS gt_dialogue_cache (
                cache_key    TEXT PRIMARY KEY,
                script_json  TEXT NOT NULL,
                created_at   TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS gt_tts_cache (
                hash             TEXT PRIMARY KEY,
                audio_file_path  TEXT NOT NULL,
                created_at       TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS gt_session_log (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                event_type    TEXT NOT NULL,
                ts            TEXT NOT NULL,
                payload_json  TEXT NOT NULL DEFAULT '{}'
            );

            CREATE TABLE IF NOT EXISTS gt_baseline_results (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                ts                  TEXT NOT NULL,
                skill_scores_json   TEXT NOT NULL DEFAULT '{}',
                overall_readiness   TEXT NOT NULL DEFAULT 'NotReady'
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection?.Dispose();
        _connection = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
