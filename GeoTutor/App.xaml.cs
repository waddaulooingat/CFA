using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GeoTutor.Core.Services;
using GeoTutor.Core.ViewModels;

namespace GeoTutor;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var config = LoadOrCreateConfig();

        var sc = new ServiceCollection();

        // ── Infrastructure ──────────────────────────────────────────────────
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeoTutor", "geotutor.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        sc.AddSingleton(_ =>
        {
            var db = new DatabaseService(dbPath);
            db.EnsureMigrated();
            return db;
        });

        sc.AddSingleton(sp => new LlmGatewayService(
            sp.GetRequiredService<DatabaseService>(),
            config.AnthropicApiKey));

        sc.AddSingleton<SkillGraphService>(sp =>
        {
            var svc = new SkillGraphService(sp.GetRequiredService<DatabaseService>());
            svc.SeedIfEmpty();
            return svc;
        });

        sc.AddSingleton(sp => new TtsService(
            sp.GetRequiredService<DatabaseService>(),
            config.AzureTtsKey,
            config.AzureTtsRegion));

        sc.AddSingleton<SessionLoggerService>();

        sc.AddSingleton(sp => new DialogueEngineService(
            sp.GetRequiredService<LlmGatewayService>(),
            sp.GetRequiredService<TtsService>(),
            sp.GetRequiredService<SessionLoggerService>(),
            sp.GetRequiredService<DatabaseService>()));

        sc.AddSingleton(sp => new BaselineAssessmentService(
            sp.GetRequiredService<LlmGatewayService>(),
            sp.GetRequiredService<SkillGraphService>(),
            sp.GetRequiredService<DatabaseService>(),
            sp.GetRequiredService<SessionLoggerService>()));

        sc.AddSingleton(sp => new LessonEngineService(
            sp.GetRequiredService<LlmGatewayService>(),
            sp.GetRequiredService<SkillGraphService>(),
            sp.GetRequiredService<SessionLoggerService>(),
            sp.GetRequiredService<DatabaseService>()));

        sc.AddSingleton(sp => new CoordinatorService(
            sp.GetRequiredService<SkillGraphService>(),
            sp.GetRequiredService<DatabaseService>()));

        sc.AddSingleton(sp => new ProofBeatService(
            sp.GetRequiredService<LlmGatewayService>()));

        // ── ViewModels ───────────────────────────────────────────────────────
        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<BaselineViewModel>();
        sc.AddSingleton<LessonViewModel>();
        sc.AddSingleton<TestViewModel>();
        sc.AddSingleton<ParentDashboardViewModel>();
        sc.AddSingleton<ProofViewModel>();

        Services = sc.BuildServiceProvider();

        // ── Show main window ─────────────────────────────────────────────────
        var vm  = Services.GetRequiredService<MainViewModel>();
        var win = new MainWindow { DataContext = vm };
        MainWindow = win;
        win.Show();

        _ = vm.InitializeAsync();
    }

    // -------------------------------------------------------------------------
    // Config
    // -------------------------------------------------------------------------

    private record AppConfig(
        string AnthropicApiKey,
        string AzureTtsKey,
        string AzureTtsRegion);

    private static AppConfig LoadOrCreateConfig()
    {
        string dir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeoTutor");
        string path = Path.Combine(dir, "config.json");
        Directory.CreateDirectory(dir);

        if (File.Exists(path))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                string claude = root.TryGetProperty("anthropicApiKey", out var a) ? a.GetString() ?? "" : "";
                string ttsKey = root.TryGetProperty("azureTtsKey",     out var b) ? b.GetString() ?? "" : "";
                string region = root.TryGetProperty("azureTtsRegion",  out var c) ? c.GetString() ?? "eastus" : "eastus";
                return new AppConfig(claude, ttsKey, region);
            }
            catch { /* fall through */ }
        }

        // Write placeholder config on first run
        var placeholder = new
        {
            anthropicApiKey = "YOUR_ANTHROPIC_API_KEY_HERE",
            azureTtsKey     = "YOUR_AZURE_TTS_KEY_HERE",
            azureTtsRegion  = "eastus",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(placeholder,
            new JsonSerializerOptions { WriteIndented = true }));

        return new AppConfig("", "", "eastus");
    }
}
