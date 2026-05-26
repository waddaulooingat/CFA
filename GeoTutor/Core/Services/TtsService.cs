using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GeoTutor.Core.Models;

namespace GeoTutor.Core.Services;

/// <summary>
/// Azure Neural TTS integration (§9.3).
/// Synthesizes speech for Coach (en-US-DavisNeural) and Peer (en-US-AshleyNeural),
/// caches audio files under %APPDATA%\GeoTutor\tts_cache\, and records each
/// generated file in the gt_tts_cache table.
/// </summary>
public class TtsService
{
    // -----------------------------------------------------------------------
    // Voice constants
    // -----------------------------------------------------------------------

    private const string CoachVoice = "en-US-DavisNeural";
    private const string PeerVoice  = "en-US-AshleyNeural";

    // -----------------------------------------------------------------------
    // HTTP
    // -----------------------------------------------------------------------

    // Static shared client — TtsService may be instantiated once and reused
    // for the application lifetime.
    private static readonly HttpClient _http = new();

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private readonly DatabaseService _db;
    private readonly string          _subscriptionKey;
    private readonly string          _region;
    private readonly string          _cacheDirectory;

    /// <param name="db">Shared database service for gt_tts_cache writes.</param>
    /// <param name="azureSubscriptionKey">Azure Cognitive Services subscription key.</param>
    /// <param name="azureRegion">Azure region slug, e.g. "eastus".</param>
    public TtsService(DatabaseService db, string azureSubscriptionKey, string azureRegion)
    {
        _db              = db              ?? throw new ArgumentNullException(nameof(db));
        _subscriptionKey = azureSubscriptionKey
                           ?? throw new ArgumentNullException(nameof(azureSubscriptionKey));
        _region          = azureRegion
                           ?? throw new ArgumentNullException(nameof(azureRegion));

        // Resolve cache directory — create on first use.
        string appData   = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheDirectory  = Path.Combine(appData, "GeoTutor", "tts_cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Synthesize audio for a dialogue line and cache it.
    /// Cache key: SHA-256 of (voice + text). Returns the local .mp3 file path.
    /// If the audio is already cached, returns the cached path immediately
    /// without contacting Azure.
    /// Returns <c>null</c> if synthesis fails.
    /// </summary>
    public async Task<string?> SynthesizeAsync(Speaker speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("text must not be empty.", nameof(text));

        string voice    = VoiceName(speaker);
        string hash     = ComputeHash(voice, text);
        string filePath = Path.Combine(_cacheDirectory, $"{hash}.mp3");

        // 1. Already on disk?
        if (File.Exists(filePath))
            return filePath;

        // 2. Check the database cache (file might exist from a previous install path).
        string? cachedPath = LookupCacheDb(hash);
        if (cachedPath is not null && File.Exists(cachedPath))
            return cachedPath;

        // 3. Call Azure TTS REST API.
        byte[]? audioBytes = await CallAzureTtsAsync(voice, text).ConfigureAwait(false);
        if (audioBytes is null)
            return null;

        // 4. Persist to disk.
        await File.WriteAllBytesAsync(filePath, audioBytes).ConfigureAwait(false);

        // 5. Record in database.
        InsertCacheDb(hash, filePath);

        return filePath;
    }

    /// <summary>
    /// Pre-generates all lines in a script concurrently (latency budget: &lt;600 ms to first audio).
    /// Populates <see cref="DialogueLine.AudioFilePath"/> for every line.
    /// Lines whose synthesis fails have their AudioFilePath left as <c>null</c>.
    /// </summary>
    public async Task PreGenerateScriptAsync(DialogueScript script)
    {
        if (script is null) throw new ArgumentNullException(nameof(script));
        if (script.Lines.Count == 0) return;

        // Fan-out all synthesis requests concurrently.
        Task[] tasks = new Task[script.Lines.Count];
        for (int i = 0; i < script.Lines.Count; i++)
        {
            // Capture loop variable.
            DialogueLine line = script.Lines[i];
            tasks[i] = Task.Run(async () =>
            {
                string? path = await SynthesizeAsync(line.Speaker, line.Text)
                                   .ConfigureAwait(false);
                line.AudioFilePath = path;
            });
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>Returns the Azure Neural voice name string for a speaker.</summary>
    private static string VoiceName(Speaker speaker) =>
        speaker == Speaker.Coach ? CoachVoice : PeerVoice;

    /// <summary>Computes the SHA-256 cache key for a (voice, text) pair.</summary>
    private static string ComputeHash(string voice, string text)
    {
        byte[] input  = Encoding.UTF8.GetBytes(voice + "\x00" + text);
        byte[] digest = SHA256.HashData(input);
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>Builds the SSML document for a given voice and plain text.</summary>
    private static string BuildSsml(string voice, string text)
    {
        // Escape XML special characters in the user-supplied text.
        string escaped = text
            .Replace("&",  "&amp;",  StringComparison.Ordinal)
            .Replace("<",  "&lt;",   StringComparison.Ordinal)
            .Replace(">",  "&gt;",   StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'",  "&apos;", StringComparison.Ordinal);

        return $"""
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">
              <voice name="{voice}">{escaped}</voice>
            </speak>
            """;
    }

    /// <summary>
    /// Sends the SSML to the Azure TTS REST endpoint and returns the raw MP3 bytes,
    /// or <c>null</c> on any error.
    /// Endpoint: https://{region}.tts.speech.microsoft.com/cognitiveservices/v1
    /// </summary>
    private async Task<byte[]?> CallAzureTtsAsync(string voice, string text)
    {
        string endpoint = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/v1";
        string ssml     = BuildSsml(voice, text);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
        request.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");

        request.Content = new StringContent(ssml, Encoding.UTF8);
        request.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/ssml+xml");

        try
        {
            using HttpResponseMessage response =
                await _http.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Database cache helpers (gt_tts_cache)
    // -----------------------------------------------------------------------

    private string? LookupCacheDb(string hash)
    {
        try
        {
            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT audio_file_path FROM gt_tts_cache WHERE hash = @hash LIMIT 1;";
            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? reader.GetString(0) : null;
        }
        catch
        {
            return null;
        }
    }

    private void InsertCacheDb(string hash, string filePath)
    {
        try
        {
            var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO gt_tts_cache (hash, audio_file_path, created_at)
                VALUES (@hash, @path, @ts)
                ON CONFLICT(hash) DO NOTHING;
                """;
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@path", filePath);
            cmd.Parameters.AddWithValue("@ts",   DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Cache DB write failure is non-fatal — file is still usable.
        }
    }
}
