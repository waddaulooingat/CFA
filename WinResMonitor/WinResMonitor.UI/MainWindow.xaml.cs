using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WinResMonitor.Core;

namespace WinResMonitor.UI
{
    public partial class MainWindow : Window
    {
        private ProxyEngine _proxy;

        private string DbConnString =>
            $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinResMonitor", "requests.db")}";

        public MainWindow()
        {
            InitializeComponent();
            _proxy = new ProxyEngine(port: 8877);
            _proxy.Start();
            RefreshAll();
            RefreshBlocklistStatus();
        }

        private void RefreshAll()
        {
            RefreshLog();
            RefreshDomains();
            RefreshKeywords();
            RefreshWhitelist();
            RefreshTimeLimitSites();
            GiphyApiKeyBox.Text = _proxy.Blocklist.GiphyApiKey;
        }

        // --- Activity Log ---

        private void RefreshLog()
        {
            bool? filter = null;
            if (FilterBlocked?.IsChecked == true) filter = true;
            else if (FilterAllowed?.IsChecked == true) filter = false;

            var entries = new Logger().GetRecentRequests(300, filter);
            LogGrid.ItemsSource = entries;
            StatusBarText.Text = $"Showing {entries.Count} entries";
        }

        private void BtnRefreshLog_Click(object sender, RoutedEventArgs e) => RefreshLog();
        private void Filter_Changed(object sender, RoutedEventArgs e) => RefreshLog();

        // --- Proxy Start/Stop ---

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_proxy.IsRunning)
            {
                _proxy.Stop();
                BtnStartStop.Content = "Start Proxy";
                StatusDot.Fill = Brushes.Red;
                StatusLabel.Text = "Proxy Stopped";
            }
            else
            {
                _proxy.Start();
                BtnStartStop.Content = "Stop Proxy";
                StatusDot.Fill = Brushes.LimeGreen;
                StatusLabel.Text = "Proxy Running — Port 8877";
            }
        }

        // --- Blocked Domains ---

        private void RefreshDomains()
        {
            DomainList.ItemsSource = null;
            DomainList.ItemsSource = _proxy.Blocklist.GetDomains().ToList();
        }

        private void BtnAddDomain_Click(object sender, RoutedEventArgs e)
        {
            var domain = NewDomainBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain)) return;
            _proxy.Blocklist.AddDomain(domain);
            NewDomainBox.Clear();
            RefreshDomains();
            StatusBarText.Text = $"Added domain: {domain}";
        }

        private void BtnRemoveDomain_Click(object sender, RoutedEventArgs e)
        {
            if (DomainList.SelectedItem is string domain)
            {
                _proxy.Blocklist.RemoveDomain(domain);
                RefreshDomains();
                StatusBarText.Text = $"Removed domain: {domain}";
            }
        }

        // --- Keywords ---

        private void RefreshKeywords()
        {
            KeywordList.ItemsSource = null;
            KeywordList.ItemsSource = _proxy.Blocklist.GetKeywords().ToList();
        }

        private void BtnAddKeyword_Click(object sender, RoutedEventArgs e)
        {
            var kw = NewKeywordBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(kw)) return;
            _proxy.Blocklist.AddKeyword(kw);
            NewKeywordBox.Clear();
            RefreshKeywords();
            StatusBarText.Text = $"Added keyword: {kw}";
        }

        private void BtnRemoveKeyword_Click(object sender, RoutedEventArgs e)
        {
            if (KeywordList.SelectedItem is string kw)
            {
                _proxy.Blocklist.RemoveKeyword(kw);
                RefreshKeywords();
                StatusBarText.Text = $"Removed keyword: {kw}";
            }
        }

        // --- Education Whitelist ---

        private void RefreshWhitelist()
        {
            WhitelistDomainList.ItemsSource = null;
            WhitelistDomainList.ItemsSource = _proxy.Blocklist.GetWhitelistedDomains().ToList();
        }

        private void BtnAddWhitelist_Click(object sender, RoutedEventArgs e)
        {
            var domain = NewWhitelistBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain)) return;
            _proxy.Blocklist.AddWhitelistedDomain(domain);
            NewWhitelistBox.Clear();
            RefreshWhitelist();
            StatusBarText.Text = $"Whitelisted: {domain}";
        }

        private void BtnRemoveWhitelist_Click(object sender, RoutedEventArgs e)
        {
            if (WhitelistDomainList.SelectedItem is string domain)
            {
                _proxy.Blocklist.RemoveWhitelistedDomain(domain);
                RefreshWhitelist();
                StatusBarText.Text = $"Removed from whitelist: {domain}";
            }
        }

        // --- Time Limit Sites ---

        private void RefreshTimeLimitSites()
        {
            TimeLimitList.ItemsSource = null;
            TimeLimitList.ItemsSource = _proxy.Blocklist.GetTimeLimitSites().ToList();

            var tracker = new TimeTracker(DbConnString);
            var tempBlocks = tracker.GetTempBlocks();
            TempBlockedList.ItemsSource = tempBlocks
                .Select(t => $"{t.Domain} — unblocks in {t.TotalSeconds / 3600}h {(t.TotalSeconds % 3600) / 60}m")
                .ToList();
        }

        private void BtnAddTimeLimitSite_Click(object sender, RoutedEventArgs e)
        {
            var domain = NewTimeLimitBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain)) return;
            _proxy.Blocklist.AddTimeLimitSite(domain);
            NewTimeLimitBox.Clear();
            RefreshTimeLimitSites();
            StatusBarText.Text = $"Added time-limit site: {domain}";
        }

        private void BtnRemoveTimeLimitSite_Click(object sender, RoutedEventArgs e)
        {
            if (TimeLimitList.SelectedItem is string domain)
            {
                _proxy.Blocklist.RemoveTimeLimitSite(domain);
                RefreshTimeLimitSites();
                StatusBarText.Text = $"Removed time-limit site: {domain}";
            }
        }

        // --- Weekly Report ---

        private void BtnWeeklyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gen = new ReportGenerator(DbConnString);
                var path = gen.GenerateWeeklyReport();
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                StatusBarText.Text = $"Report saved: {path}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Settings ---

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var pm = new WinResMonitor.Core.PasswordManager();
            // Verify current password first
            var verify = new PasswordDialog(pm, isSetup: false);
            verify.Title = "Verify Current Password";
            if (verify.ShowDialog() != true) return;

            // Then set new password
            var setup = new PasswordDialog(pm, isSetup: true);
            setup.Title = "Set New Password";
            if (setup.ShowDialog() == true)
                StatusBarText.Text = "Password changed successfully.";
        }

        private void RefreshBlocklistStatus()
        {
            try
            {
                var updater = new BlocklistUpdater(DbConnString);
                var (lastUpdated, count) = updater.GetStatus();
                BlocklistStatusText.Text = lastUpdated.HasValue
                    ? $"Last updated: {lastUpdated:g} — {count:N0} domains"
                    : "Blocklist not yet downloaded.";
            }
            catch { BlocklistStatusText.Text = "Status unavailable."; }
        }

        private async void BtnUpdateBlocklist_Click(object sender, RoutedEventArgs e)
        {
            BtnUpdateBlocklist.IsEnabled = false;
            StatusBarText.Text = "Downloading blocklist...";
            try
            {
                var updater = new BlocklistUpdater(DbConnString);
                updater.OnProgress += msg => Dispatcher.Invoke(() => StatusBarText.Text = msg);
                int count = await updater.UpdateAsync();
                RefreshBlocklistStatus();
                StatusBarText.Text = $"Blocklist updated: {count:N0} domains.";
            }
            catch (Exception ex)
            {
                BlocklistStatusText.Text = "Update failed.";
                StatusBarText.Text = $"Update error: {ex.Message}";
            }
            finally { BtnUpdateBlocklist.IsEnabled = true; }
        }

        private void BtnSaveGiphyKey_Click(object sender, RoutedEventArgs e)
        {
            var key = GiphyApiKeyBox.Text.Trim();
            _proxy.Blocklist.SetGiphyApiKey(key);
            StatusBarText.Text = string.IsNullOrEmpty(key)
                ? "Giphy API key cleared."
                : "Giphy API key saved. Block page will show a new GIF within 10 minutes.";
        }

        private void BtnPurge_Click(object sender, RoutedEventArgs e)
        {
            int[] days = { 7, 30, 90 };
            int d = days[RetentionDays.SelectedIndex];
            new Logger().PurgeOlderThan(d);
            RefreshLog();
            StatusBarText.Text = $"Purged logs older than {d} days.";
        }

        private void BtnInstallService_Click(object sender, RoutedEventArgs e)
        {
            RunScCommand("create WinResMonitor binPath= \"\"%ProgramFiles%\\WinResMonitor\\WinResMonitor.Service.exe\"\" start= auto DisplayName= \"Windows Resource Monitor\"");
            RunScCommand("start WinResMonitor");
            MessageBox.Show("Service installed and started.", "WinResMonitor", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnUninstallService_Click(object sender, RoutedEventArgs e)
        {
            RunScCommand("stop WinResMonitor");
            RunScCommand("delete WinResMonitor");
            MessageBox.Show("Service removed.", "WinResMonitor", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunScCommand(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", args)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"sc.exe error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _proxy?.Stop();
        }
    }

    public class BlockedToBoolConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v ? "BLOCKED" : "allowed";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class BlockedToColorConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v ? Brushes.Red : Brushes.Green;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
