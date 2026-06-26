using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using WinResMonitor.Core;

namespace WinResMonitor.Service
{
    public class WinResMonitorService : ServiceBase
    {
        private ProxyEngine _proxy;
        private Timer _watchdog;
        private const int WatchdogIntervalMs = 15000; // check every 15s

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
            _watchdog = new Timer(WatchdogTick, null, WatchdogIntervalMs, WatchdogIntervalMs);
        }

        protected override void OnStop()
        {
            _watchdog?.Dispose();
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

        public static void Main(string[] args)
        {
            // Allow running as console for debugging
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
