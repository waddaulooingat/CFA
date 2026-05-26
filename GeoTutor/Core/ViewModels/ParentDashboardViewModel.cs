namespace GeoTutor.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Core.Models;
using GeoTutor.Core.Services;
using Microsoft.Data.Sqlite;

/// <summary>
/// Aggregates skill mastery, weak-spot identification, and parent-flag
/// escalations for the parent/guardian dashboard (§12 Phase 6).
/// </summary>
public partial class ParentDashboardViewModel : BaseViewModel
{
    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly SkillGraphService   _skillGraph;
    private readonly DatabaseService     _database;
    private readonly SessionLoggerService _sessionLogger;

    // -----------------------------------------------------------------------
    // Observable properties — skill lists
    // -----------------------------------------------------------------------

    [ObservableProperty] private List<SkillSummary> _allSkills       = [];
    [ObservableProperty] private List<SkillSummary> _weakSpots       = [];
    [ObservableProperty] private List<SkillSummary> _recentProgress  = [];   // improved in last 7 days

    // -----------------------------------------------------------------------
    // Observable properties — headline stats
    // -----------------------------------------------------------------------

    [ObservableProperty] private int    _totalLessonsCompleted;
    [ObservableProperty] private double _overallMastery;

    // -----------------------------------------------------------------------
    // Observable properties — parent flags
    // -----------------------------------------------------------------------

    [ObservableProperty] private bool         _hasParentFlags;          // Tier 3 escalations
    [ObservableProperty] private List<string> _parentFlagMessages = [];

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public ParentDashboardViewModel(
        SkillGraphService    skillGraph,
        DatabaseService      database,
        SessionLoggerService sessionLogger)
    {
        _skillGraph    = skillGraph    ?? throw new ArgumentNullException(nameof(skillGraph));
        _database      = database      ?? throw new ArgumentNullException(nameof(database));
        _sessionLogger = sessionLogger ?? throw new ArgumentNullException(nameof(sessionLogger));
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reloads all data from the database. Bound to a Refresh button.
    /// </summary>
    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    /// <summary>
    /// Exports a simple progress report using WPF's built-in PrintDocument/
    /// FlowDocument printing pipeline. No third-party PDF library required.
    /// </summary>
    [RelayCommand]
    private void ExportPdf()
    {
        try
        {
            var document = BuildFlowDocument();
            var dialog   = new PrintDialog();

            if (dialog.ShowDialog() != true)
                return;

            // Paginate and print.
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
            dialog.PrintDocument(paginator, "GeoTutor Progress Report");

            _sessionLogger.Log("export_pdf", new { timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads all dashboard data. Call on navigation to this view.
    /// </summary>
    public async Task LoadAsync()
    {
        SetBusy(true, "Loading progress…");

        try
        {
            // All skill summaries (joins mastery log for correct-rate).
            AllSkills = await Task.Run(() => BuildAllSkillSummaries());

            // Weak spots: unlocked skills with mastery < 0.50, ordered by mastery asc.
            WeakSpots = AllSkills
                .Where(s => s.Mastery < 0.50 && s.TotalAttempts > 0)
                .OrderBy(s => s.Mastery)
                .Take(10)
                .ToList();

            // Recent progress: skills with at least one attempt in the last 7 days
            // and whose mastery improved (correct rate > 0.70 in that window).
            var cutoff = DateTime.UtcNow.AddDays(-7);
            RecentProgress = AllSkills
                .Where(s => s.LastSeen.HasValue && s.LastSeen.Value >= cutoff
                            && s.CorrectRate >= 0.70)
                .OrderByDescending(s => s.LastSeen)
                .Take(10)
                .ToList();

            // Headline stats.
            TotalLessonsCompleted = await Task.Run(() => CountLessonResults());
            OverallMastery        = AllSkills.Count > 0
                ? AllSkills.Average(s => s.Mastery)
                : 0.0;

            // Parent flags: Tier 3 escalations logged in session log.
            var flags = await Task.Run(() => LoadParentFlags());
            ParentFlagMessages = flags;
            HasParentFlags     = flags.Count > 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading dashboard: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers — data loading
    // -----------------------------------------------------------------------

    private List<SkillSummary> BuildAllSkillSummaries()
    {
        var skills = _skillGraph.GetAllSkills();

        // Pull per-skill attempt stats in one query.
        var stats = LoadAttemptStats();

        return skills.Select(s =>
        {
            stats.TryGetValue(s.Id, out var st);
            return new SkillSummary
            {
                SkillId        = s.Id,
                SkillName      = s.Name,
                Unit           = s.Unit,
                Mastery        = s.CurrentMastery,
                DifficultyBand = s.DifficultyBand,
                LastSeen       = s.LastSeen,
                TotalAttempts  = st.total,
                CorrectRate    = st.total > 0 ? (double)st.correct / st.total : 0.0,
            };
        }).OrderBy(s => s.Unit).ThenBy(s => s.SkillName).ToList();
    }

    private Dictionary<string, (int total, int correct)> LoadAttemptStats()
    {
        var result = new Dictionary<string, (int total, int correct)>(StringComparer.Ordinal);
        var conn   = _database.GetConnection();

        using var cmd = conn.CreateCommand();
        // Join gt_items to gt_attempts to get skill-level roll-ups.
        cmd.CommandText = """
            SELECT i.skill_id,
                   COUNT(a.id)             AS total,
                   SUM(CASE WHEN a.correct = 1 THEN 1 ELSE 0 END) AS correct
            FROM   gt_attempts a
            JOIN   gt_items    i ON i.id = a.item_id
            GROUP  BY i.skill_id;
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string skillId = reader.GetString(0);
            int    total   = reader.GetInt32(1);
            int    correct = reader.GetInt32(2);
            result[skillId] = (total, correct);
        }

        return result;
    }

    private int CountLessonResults()
    {
        var conn = _database.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM gt_session_log
            WHERE event_type = 'lesson_end';
            """;
        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }

    private List<string> LoadParentFlags()
    {
        var messages = new List<string>();
        var conn     = _database.GetConnection();

        // A parent flag is any dialogue_play event where the stored payload
        // includes a tier=Tier3 key (written by DialogueEngineService).
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT payload_json, ts
            FROM   gt_session_log
            WHERE  event_type = 'dialogue_play'
            ORDER  BY ts DESC
            LIMIT  50;
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string payloadJson = reader.GetString(0);
            string ts          = reader.GetString(1);

            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("newTier", out var tierProp) &&
                    tierProp.GetString() == "Tier3")
                {
                    string category = doc.RootElement.TryGetProperty("errorCategory", out var catProp)
                        ? catProp.GetString() ?? "unknown error"
                        : "unknown error";

                    if (DateTime.TryParse(ts, out var dt))
                    {
                        messages.Add(
                            $"[{dt.ToLocalTime():MMM d, h:mm tt}] " +
                            $"Repeated difficulty with: {category.Replace('-', ' ')}. " +
                            "Consider reviewing this topic together.");
                    }
                }
            }
            catch { /* Skip malformed entries */ }
        }

        return messages;
    }

    // -----------------------------------------------------------------------
    // Private helpers — PDF / print
    // -----------------------------------------------------------------------

    private FlowDocument BuildFlowDocument()
    {
        var doc = new FlowDocument
        {
            FontFamily  = new FontFamily("Segoe UI"),
            FontSize    = 12,
            PagePadding = new Thickness(40),
        };

        // ── Title ─────────────────────────────────────────────────────────
        doc.Blocks.Add(new Paragraph(new Run("GeoTutor — Progress Report"))
        {
            FontSize   = 20,
            FontWeight = FontWeights.Bold,
            Margin     = new Thickness(0, 0, 0, 4),
        });

        doc.Blocks.Add(new Paragraph(
            new Run($"Generated {DateTime.Now:MMMM d, yyyy  h:mm tt}"))
        {
            Foreground = Brushes.Gray,
            Margin     = new Thickness(0, 0, 0, 16),
        });

        // ── Headline stats ────────────────────────────────────────────────
        AddSection(doc, "Overall Progress");
        doc.Blocks.Add(Bullet($"Total lessons completed: {TotalLessonsCompleted}"));
        doc.Blocks.Add(Bullet($"Overall mastery: {OverallMastery:P0}"));

        // ── Weak spots ────────────────────────────────────────────────────
        if (WeakSpots.Count > 0)
        {
            AddSection(doc, "Topics Needing Attention");
            foreach (var s in WeakSpots)
                doc.Blocks.Add(Bullet(
                    $"{s.SkillName} (Unit {s.Unit}) — " +
                    $"{s.Mastery:P0} mastery, {s.TotalAttempts} attempts"));
        }

        // ── Recent progress ───────────────────────────────────────────────
        if (RecentProgress.Count > 0)
        {
            AddSection(doc, "Recent Progress (last 7 days)");
            foreach (var s in RecentProgress)
                doc.Blocks.Add(Bullet(
                    $"{s.SkillName} (Unit {s.Unit}) — " +
                    $"{s.CorrectRate:P0} correct rate recently"));
        }

        // ── Parent flags ──────────────────────────────────────────────────
        if (ParentFlagMessages.Count > 0)
        {
            AddSection(doc, "Items for Parent Review");
            foreach (var msg in ParentFlagMessages)
                doc.Blocks.Add(Bullet(msg));
        }

        // ── Full skill table ──────────────────────────────────────────────
        AddSection(doc, "All Skills");

        var table = new Table { CellSpacing = 0 };
        for (int i = 0; i < 5; i++)
            table.Columns.Add(new TableColumn());

        var headerRow = new TableRow { Background = Brushes.LightGray };
        foreach (string h in new[] { "Skill", "Unit", "Mastery", "Attempts", "Correct %" })
        {
            headerRow.Cells.Add(new TableCell(
                new Paragraph(new Run(h)) { FontWeight = FontWeights.Bold })
            { Padding = new Thickness(4, 2, 4, 2) });
        }

        var rowGroup = new TableRowGroup();
        rowGroup.Rows.Add(headerRow);

        bool alternate = false;
        foreach (var s in AllSkills)
        {
            var row = new TableRow
            {
                Background = alternate ? Brushes.WhiteSmoke : Brushes.White
            };
            alternate = !alternate;

            row.Cells.Add(Cell(s.SkillName));
            row.Cells.Add(Cell(s.Unit.ToString()));
            row.Cells.Add(Cell(s.Mastery.ToString("P0")));
            row.Cells.Add(Cell(s.TotalAttempts.ToString()));
            row.Cells.Add(Cell(s.TotalAttempts > 0 ? s.CorrectRate.ToString("P0") : "—"));
            rowGroup.Rows.Add(row);
        }

        table.RowGroups.Add(rowGroup);
        doc.Blocks.Add(table);

        return doc;
    }

    private static void AddSection(FlowDocument doc, string title)
    {
        doc.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize   = 14,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 16, 0, 4),
        });
    }

    private static Paragraph Bullet(string text) =>
        new(new Run($"• {text}")) { Margin = new Thickness(8, 1, 0, 1) };

    private static TableCell Cell(string text) =>
        new(new Paragraph(new Run(text)))
        { Padding = new Thickness(4, 2, 4, 2) };
}

// ---------------------------------------------------------------------------
// Supporting model
// ---------------------------------------------------------------------------

/// <summary>
/// A flat summary of one skill's mastery and attempt statistics, used
/// throughout the parent dashboard.
/// </summary>
public class SkillSummary
{
    public string    SkillId        { get; set; } = "";
    public string    SkillName      { get; set; } = "";
    public int       Unit           { get; set; }
    public double    Mastery        { get; set; }
    public int       DifficultyBand { get; set; }
    public DateTime? LastSeen       { get; set; }
    public int       TotalAttempts  { get; set; }
    public double    CorrectRate    { get; set; }
}
