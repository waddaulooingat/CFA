using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using WinResMonitor.Core;

namespace WinResMonitor.Service
{
    public class WinResMonitorService : ServiceBase
    {
        private ProxyEngine _proxy;
        private ExtensionConfigApi _configApi;
        private Timer _watchdog;
        private Timer _blocklistRefresh;
        private const int WatchdogIntervalMs = 15000;
        private const int OneDayMs = 24 * 60 * 60 * 1000;

        private string DbConnString =>
            $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinResMonitor", "requests.db")}";

        public WinResMonitorService()
        {
            ServiceName = "WinResMonitor";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            StartProxy();
            _configApi = new ExtensionConfigApi(_proxy.Blocklist);
            _configApi.Start();
            _watchdog = new Timer(WatchdogTick, null, WatchdogIntervalMs, WatchdogIntervalMs);
            // Fire immediately on start, then every 24 hours
            _blocklistRefresh = new Timer(BlocklistRefreshTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(OneDayMs));
        }

        protected override void OnStop()
        {
            _blocklistRefresh?.Dispose();
            _watchdog?.Dispose();
            _configApi?.Stop();
            _proxy?.Stop();
        }

        private void StartProxy()
        {
            try
            {
                _proxy = new ProxyEngine(port: 8877);
                _proxy.Start();
                EventLog.WriteEntry("WinResMonitor proxy started on port 8877.", System.Diagnostics.EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Failed to start proxy: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void WatchdogTick(object state)
        {
            if (_proxy == null || !_proxy.IsRunning)
            {
                EventLog.WriteEntry("WinResMonitor: proxy down, restarting...", System.Diagnostics.EventLogEntryType.Warning);
                StartProxy();
            }
        }

        private void BlocklistRefreshTick(object state)
        {
            try
            {
                var updater = new BlocklistUpdater(DbConnString);
                var task = updater.UpdateAsync();
                task.Wait();
                EventLog.WriteEntry($"Blocklist updated: {task.Result} domains loaded.", System.Diagnostics.EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Blocklist update failed: {ex.Message}", System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        public static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("WinResMonitor Service - Running in console mode. Press Ctrl+C to stop.");
                var svc = new WinResMonitorService();
                svc.OnStart(args);
                Console.CancelKeyPress += (s, e) => { svc.OnStop(); };
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                ServiceBase.Run(new WinResMonitorService());
            }
        }
    }
}
