using System;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace GeoTutor.Core.Services;

/// <summary>
/// Appends structured events to gt_session_log.
/// Every public method serialises a typed anonymous payload to JSON before
/// writing so the log stays queryable without a full JSON library at read time.
/// </summary>
public class SessionLoggerService
{
    private readonly DatabaseService _db;

    public SessionLoggerService(DatabaseService db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // -----------------------------------------------------------------------
    // Generic entry point
    // -----------------------------------------------------------------------

    /// <summary>
    /// Appends a raw event row. <paramref name="payload"/> is serialised with
    /// System.Text.Json; use an anonymous object for convenient call sites.
    /// </summary>
    public void Log(string eventType, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        string payloadJson;
        try
        {
            payloadJson = JsonSerializer.Serialize(payload);
        }
        catch (Exception ex)
        {
            payloadJson = JsonSerializer.Serialize(new { serializationError = ex.Message });
        }

        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO gt_session_log (event_type, ts, payload_json)
            VALUES (@et, @ts, @payload);
            """;
        cmd.Parameters.AddWithValue("@et",      eventType);
        cmd.Parameters.AddWithValue("@ts",      DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@payload", payloadJson);
        cmd.ExecuteNonQuery();
    }

    // -----------------------------------------------------------------------
    // Typed convenience overloads
    // -----------------------------------------------------------------------

    /// <summary>Logged when a lesson screen is entered for a given skill.</summary>
    public void LogLessonStart(string skillId, string lessonId)
    {
        Log("lesson_start", new { skillId, lessonId });
    }

    /// <summary>Logged when the tutoring engine transitions into a new beat.</summary>
    public void LogBeatEnter(string beatType, string sceneSpecId)
    {
        Log("beat_enter", new { beatType, sceneSpecId });
    }

    /// <summary>
    /// Logged each time the student submits an answer (correct or not).
    /// Also appends an attempt row to gt_attempts for mastery computation.
    /// </summary>
    public void LogAnswer(string itemId, bool correct, long timeMs, int hintsUsed)
    {
        Log("answer", new { itemId, correct, timeMs, hintsUsed });

        // Mirror into gt_attempts so mastery queries can join without parsing JSON.
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO gt_attempts (item_id, ts, correct, time_ms, hints_used)
            VALUES (@item_id, @ts, @correct, @time_ms, @hints_used);
            """;
        cmd.Parameters.AddWithValue("@item_id",   itemId);
        cmd.Parameters.AddWithValue("@ts",        DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@correct",   correct ? 1 : 0);
        cmd.Parameters.AddWithValue("@time_ms",   timeMs);
        cmd.Parameters.AddWithValue("@hints_used",hintsUsed);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Logged each time a hint card is revealed during practice.</summary>
    public void LogHintReveal(string itemId, int hintIndex)
    {
        Log("hint_reveal", new { itemId, hintIndex });
    }

    /// <summary>Logged when a coaching dialogue is triggered after an error.</summary>
    public void LogDialoguePlay(string errorCategory, string problemId)
    {
        Log("dialogue_play", new { errorCategory, problemId });

        // Mark the most-recent attempt for this item as having played a dialogue.
        var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE gt_attempts
            SET    dialogue_played  = 1,
                   error_category   = @ec
            WHERE  id = (
                SELECT id FROM gt_attempts
                WHERE  item_id = @item_id
                ORDER  BY id DESC
                LIMIT  1
            );
            """;
        cmd.Parameters.AddWithValue("@ec",      errorCategory);
        cmd.Parameters.AddWithValue("@item_id", problemId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Logged every time the student drags a point on the interactive canvas.</summary>
    public void LogDrag(string sceneSpecId, string pointId, double x, double y)
    {
        Log("drag", new { sceneSpecId, pointId, x, y });
    }

    /// <summary>Logged when the student completes a lesson and its end-of-lesson test.</summary>
    public void LogLessonEnd(string skillId, double testScore)
    {
        Log("lesson_end", new { skillId, testScore });
    }

    /// <summary>
    /// Generic event log with a pre-built dictionary payload.
    /// Alias for <see cref="Log"/> that accepts <c>Dictionary&lt;string, object&gt;</c>
    /// so call sites can pass typed key-value pairs without anonymous types.
    /// </summary>
    public void LogEvent(string eventType, Dictionary<string, object> payload)
        => Log(eventType, payload);
}
