using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WinResMonitor.Core
{
    public class Logger
    {
        private readonly string _dbPath;
        private readonly string _connString;

        public Logger()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WinResMonitor");

            Directory.CreateDirectory(dir);
            _dbPath = Path.Combine(dir, "requests.db");
            _connString = $"Data Source={_dbPath}";
            InitDb();
        }

        private void InitDb()
        {
            using var conn = new SqliteConnection(_connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS RequestLog (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp   TEXT NOT NULL,
                    Url         TEXT NOT NULL,
                    Host        TEXT NOT NULL,
                    Blocked     INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_timestamp ON RequestLog(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_blocked ON RequestLog(Blocked);";
            cmd.ExecuteNonQuery();
        }

        public void LogRequest(string url, string host, bool blocked)
        {
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO RequestLog (Timestamp, Url, Host, Blocked)
                    VALUES ($ts, $url, $host, $blocked)";
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("$url", url);
                cmd.Parameters.AddWithValue("$host", host);
                cmd.Parameters.AddWithValue("$blocked", blocked ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void LogError(string message)
        {
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO RequestLog (Timestamp, Url, Host, Blocked)
                    VALUES ($ts, $url, $host, 0)";
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("$url", $"[ERROR] {message}");
                cmd.Parameters.AddWithValue("$host", "system");
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public List<RequestEntry> GetRecentRequests(int limit = 200, bool? blocked = null)
        {
            var results = new List<RequestEntry>();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();

                var filter = blocked.HasValue ? $"WHERE Blocked = {(blocked.Value ? 1 : 0)}" : "";
                cmd.CommandText = $@"
                    SELECT Id, Timestamp, Url, Host, Blocked
                    FROM RequestLog {filter}
                    ORDER BY Id DESC LIMIT {limit}";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new RequestEntry
                    {
                        Id = reader.GetInt64(0),
                        Timestamp = DateTime.Parse(reader.GetString(1)).ToLocalTime(),
                        Url = reader.GetString(2),
                        Host = reader.GetString(3),
                        Blocked = reader.GetInt32(4) == 1
                    });
                }
            }
            catch { }
            return results;
        }

        public void PurgeOlderThan(int days)
        {
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM RequestLog WHERE Timestamp < $cutoff";
                cmd.Parameters.AddWithValue("$cutoff",
                    DateTime.UtcNow.AddDays(-days).ToString("o"));
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public string DbPath => _dbPath;
    }

    public class RequestEntry
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = "";
        public string Host { get; set; } = "";
        public bool Blocked { get; set; }
    }
}
