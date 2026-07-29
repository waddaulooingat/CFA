using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace WinResMonitor.Core
{
    public class ReportGenerator
    {
        private readonly string _connString;
        private readonly string _reportDir;

        public ReportGenerator(string connString)
        {
            _connString = connString;
            _reportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WinResMonitor", "reports");
            Directory.CreateDirectory(_reportDir);
        }

        public string GenerateWeeklyReport()
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-6);

            var blockedStats = GetBlockedStats(startDate, endDate);
            var allowedStats = GetAllowedStats(startDate, endDate);
            var dailyBreakdown = GetDailyBreakdown(startDate, endDate);
            var timeStats = GetTimeStats(startDate, endDate);

            int totalRequests = blockedStats.Sum(x => x.Count) + allowedStats.Sum(x => x.Count);
            int totalBlocked = blockedStats.Sum(x => x.Count);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='en'><head><meta charset='UTF-8'>");
            html.AppendLine("<title>WinResMonitor Weekly Report</title>");
            html.AppendLine(@"<style>
body { font-family: Segoe UI, Arial, sans-serif; margin: 0; background: #f4f6f9; color: #333; }
.header { background: #2C3E50; color: white; padding: 32px 40px; }
.header h1 { margin: 0; font-size: 26px; }
.header p { margin: 6px 0 0; opacity: 0.7; font-size: 14px; }
.content { padding: 32px 40px; max-width: 1100px; margin: 0 auto; }
.stats-row { display: flex; gap: 20px; margin-bottom: 32px; }
.stat-card { flex: 1; background: white; border-radius: 8px; padding: 24px; box-shadow: 0 1px 4px rgba(0,0,0,0.08); text-align: center; }
.stat-card .number { font-size: 36px; font-weight: bold; color: #2C3E50; }
.stat-card .label { font-size: 13px; color: #888; margin-top: 4px; }
.stat-card.red .number { color: #c0392b; }
.stat-card.green .number { color: #27ae60; }
.section { background: white; border-radius: 8px; padding: 24px; margin-bottom: 24px; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
.section h2 { margin: 0 0 16px; font-size: 16px; color: #2C3E50; border-bottom: 2px solid #eee; padding-bottom: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th { text-align: left; padding: 8px 12px; background: #f8f9fa; color: #555; font-weight: 600; border-bottom: 2px solid #eee; }
td { padding: 8px 12px; border-bottom: 1px solid #f0f0f0; }
tr:last-child td { border-bottom: none; }
.blocked { color: #c0392b; font-weight: 600; }
.bar { background: #eee; border-radius: 4px; height: 8px; margin-top: 4px; }
.bar-fill { background: #3498db; border-radius: 4px; height: 8px; }
.bar-fill.red { background: #c0392b; }
.footer { text-align: center; color: #aaa; font-size: 12px; padding: 24px; }
</style></head><body>");

            html.AppendLine($"<div class='header'><h1>&#128202; WinResMonitor Weekly Report</h1>");
            html.AppendLine($"<p>{startDate:MMMM d} – {endDate:MMMM d, yyyy}</p></div>");
            html.AppendLine("<div class='content'>");

            // Summary stats
            html.AppendLine("<div class='stats-row'>");
            html.AppendLine($"<div class='stat-card'><div class='number'>{totalRequests:N0}</div><div class='label'>Total Requests</div></div>");
            html.AppendLine($"<div class='stat-card red'><div class='number'>{totalBlocked:N0}</div><div class='label'>Blocked Requests</div></div>");
            html.AppendLine($"<div class='stat-card green'><div class='number'>{totalRequests - totalBlocked:N0}</div><div class='label'>Allowed Requests</div></div>");
            double pct = totalRequests > 0 ? (double)totalBlocked / totalRequests * 100 : 0;
            html.AppendLine($"<div class='stat-card red'><div class='number'>{pct:F1}%</div><div class='label'>Block Rate</div></div>");
            html.AppendLine("</div>");

            // Daily breakdown
            html.AppendLine("<div class='section'><h2>Daily Breakdown</h2><table>");
            html.AppendLine("<tr><th>Date</th><th>Total</th><th>Allowed</th><th>Blocked</th><th>Block Rate</th></tr>");
            foreach (var day in dailyBreakdown.OrderByDescending(d => d.Date))
            {
                double dayPct = day.Total > 0 ? (double)day.Blocked / day.Total * 100 : 0;
                html.AppendLine($"<tr><td>{DateTime.Parse(day.Date):ddd MMM d}</td><td>{day.Total:N0}</td><td>{day.Total - day.Blocked:N0}</td><td class='blocked'>{day.Blocked:N0}</td><td>{dayPct:F1}%</td></tr>");
            }
            html.AppendLine("</table></div>");

            // Top blocked domains
            if (blockedStats.Count > 0)
            {
                int maxBlocked = blockedStats[0].Count;
                html.AppendLine("<div class='section'><h2>Top Blocked Domains</h2><table>");
                html.AppendLine("<tr><th>Domain</th><th>Attempts</th><th></th></tr>");
                foreach (var (domain, count) in blockedStats.Take(20))
                {
                    int width = maxBlocked > 0 ? count * 200 / maxBlocked : 0;
                    html.AppendLine($"<tr><td>{Escape(domain)}</td><td class='blocked'>{count:N0}</td><td style='width:220px'><div class='bar'><div class='bar-fill red' style='width:{width}px'></div></div></td></tr>");
                }
                html.AppendLine("</table></div>");
            }

            // Time spent on monitored sites
            if (timeStats.Count > 0)
            {
                html.AppendLine("<div class='section'><h2>Time Spent on Monitored Sites</h2><table>");
                html.AppendLine("<tr><th>Domain</th><th>Time This Week</th></tr>");
                foreach (var (domain, seconds) in timeStats.OrderByDescending(x => x.Seconds))
                {
                    html.AppendLine($"<tr><td>{Escape(domain)}</td><td>{FormatTime(seconds)}</td></tr>");
                }
                html.AppendLine("</table></div>");
            }

            // Top allowed domains
            if (allowedStats.Count > 0)
            {
                int maxAllowed = allowedStats[0].Count;
                html.AppendLine("<div class='section'><h2>Most Visited Domains (Allowed)</h2><table>");
                html.AppendLine("<tr><th>Domain</th><th>Requests</th><th></th></tr>");
                foreach (var (domain, count) in allowedStats.Take(20))
                {
                    int width = maxAllowed > 0 ? count * 200 / maxAllowed : 0;
                    html.AppendLine($"<tr><td>{Escape(domain)}</td><td>{count:N0}</td><td style='width:220px'><div class='bar'><div class='bar-fill' style='width:{width}px'></div></div></td></tr>");
                }
                html.AppendLine("</table></div>");
            }

            html.AppendLine($"<div class='footer'>Generated by WinResMonitor on {DateTime.Now:f}</div>");
            html.AppendLine("</div></body></html>");

            var fileName = $"report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html";
            var path = Path.Combine(_reportDir, fileName);
            File.WriteAllText(path, html.ToString(), Encoding.UTF8);
            return path;
        }

        private List<(string Domain, int Count)> GetBlockedStats(DateTime start, DateTime end)
        {
            return GetDomainStats(start, end, blocked: true);
        }

        private List<(string Domain, int Count)> GetAllowedStats(DateTime start, DateTime end)
        {
            return GetDomainStats(start, end, blocked: false);
        }

        private List<(string Domain, int Count)> GetDomainStats(DateTime start, DateTime end, bool blocked)
        {
            var results = new List<(string, int)>();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Host, COUNT(*) as cnt FROM RequestLog
                    WHERE Blocked = $b AND Timestamp >= $start AND Timestamp < $end
                      AND Host != 'system'
                    GROUP BY Host ORDER BY cnt DESC LIMIT 50";
                cmd.Parameters.AddWithValue("$b", blocked ? 1 : 0);
                cmd.Parameters.AddWithValue("$start", start.ToUniversalTime().ToString("o"));
                cmd.Parameters.AddWithValue("$end", end.AddDays(1).ToUniversalTime().ToString("o"));
                using var r = cmd.ExecuteReader();
                while (r.Read()) results.Add((r.GetString(0), r.GetInt32(1)));
            }
            catch { }
            return results;
        }

        private List<(string Date, int Total, int Blocked)> GetDailyBreakdown(DateTime start, DateTime end)
        {
            var results = new List<(string, int, int)>();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT substr(Timestamp, 1, 10) as day, COUNT(*), SUM(Blocked)
                    FROM RequestLog
                    WHERE Timestamp >= $start AND Timestamp < $end AND Host != 'system'
                    GROUP BY day ORDER BY day DESC";
                cmd.Parameters.AddWithValue("$start", start.ToUniversalTime().ToString("o"));
                cmd.Parameters.AddWithValue("$end", end.AddDays(1).ToUniversalTime().ToString("o"));
                using var r = cmd.ExecuteReader();
                while (r.Read()) results.Add((r.GetString(0), r.GetInt32(1), r.GetInt32(2)));
            }
            catch { }
            return results;
        }

        private List<(string Domain, int Seconds)> GetTimeStats(DateTime start, DateTime end)
        {
            var results = new List<(string, int)>();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Domain, SUM(Seconds) FROM TimeTracking
                    WHERE Date >= $start AND Date <= $end
                    GROUP BY Domain ORDER BY SUM(Seconds) DESC";
                cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));
                using var r = cmd.ExecuteReader();
                while (r.Read()) results.Add((r.GetString(0), r.GetInt32(1)));
            }
            catch { }
            return results;
        }

        private static string FormatTime(int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        private static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        public string ReportDirectory => _reportDir;
    }
}
