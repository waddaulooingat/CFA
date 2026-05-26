using System;
using System.IO;
using System.Windows;
using GeoTutor.Core.Services;

namespace GeoTutor;

/// <summary>
/// Phase 1 startup: initialises the SQLite schema, seeds the skill graph,
/// then shows MainWindow (which owns Phase1ViewModel directly via XAML).
/// No API keys needed to run Phase 1.
/// </summary>
public partial class App : Application
{
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

            db.Dispose();
        }
        catch
        {
            // Non-fatal for Phase 1 — renderer works without DB.
        }
    }
}
