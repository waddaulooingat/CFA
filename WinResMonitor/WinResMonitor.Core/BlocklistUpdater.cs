using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace WinResMonitor.Core
{
    public class BlocklistUpdater
    {
        private const string SourceUrl = "https://blocklistproject.github.io/Lists/porn.txt";
        private readonly string _connString;

        public event Action<string>? OnProgress;

        public BlocklistUpdater(string connString)
        {
            _connString = connString;
            EnsureTable();
        }

        private void EnsureTable()
        {
            using var conn = new SqliteConnection(_connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ExternalBlocklist (
                    Domain TEXT PRIMARY KEY
                );
                CREATE TABLE IF NOT EXISTS BlocklistMeta (
                    Key   TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public async Task<int> UpdateAsync()
        {
            OnProgress?.Invoke("Downloading blocklist from blocklistproject...");

            string raw;
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(60);
                raw = await http.GetStringAsync(SourceUrl);
            }

            OnProgress?.Invoke("Parsing domains...");
            var domains = ParseHosts(raw);

            OnProgress?.Invoke($"Storing {domains.Length} domains...");
            int count = BulkInsert(domains);

            SetMeta("ExternalBlocklistUpdated", DateTime.UtcNow.ToString("o"));
            SetMeta("ExternalBlocklistCount", count.ToString());

            OnProgress?.Invoke($"Done — {count} domains in blocklist.");
            return count;
        }

        private static string[] ParseHosts(string raw)
        {
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var domains = new System.Collections.Generic.List<string>(lines.Length);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('#') || trimmed.Length == 0) continue;

                // Format: "0.0.0.0 domain.com" or just "domain.com"
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var domain = parts.Length >= 2 ? parts[1] : parts[0];

                domain = domain.Trim().ToLowerInvariant();
                if (domain.Length > 0 && domain != "0.0.0.0" && domain != "localhost")
                    domains.Add(domain);
            }
            return domains.ToArray();
        }

        private int BulkInsert(string[] domains)
        {
            using var conn = new SqliteConnection(_connString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            using (var del = conn.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = "DELETE FROM ExternalBlocklist";
                del.ExecuteNonQuery();
            }

            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = "INSERT OR IGNORE INTO ExternalBlocklist (Domain) VALUES ($d)";
            var param = ins.Parameters.Add("$d", SqliteType.Text);

            foreach (var d in domains)
            {
                param.Value = d;
                ins.ExecuteNonQuery();
            }

            tx.Commit();
            return domains.Length;
        }

        private void SetMeta(string key, string value)
        {
            using var conn = new SqliteConnection(_connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO BlocklistMeta (Key, Value) VALUES ($k, $v)";
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }

        public (DateTime? LastUpdated, int Count) GetStatus()
        {
            using var conn = new SqliteConnection(_connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM BlocklistMeta WHERE Key IN ('ExternalBlocklistUpdated','ExternalBlocklistCount')";

            DateTime? lastUpdated = null;
            int count = 0;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r.GetString(0) == "ExternalBlocklistUpdated")
                    lastUpdated = DateTime.Parse(r.GetString(1)).ToLocalTime();
                else if (r.GetString(0) == "ExternalBlocklistCount")
                    int.TryParse(r.GetString(1), out count);
            }
            return (lastUpdated, count);
        }
    }
}
