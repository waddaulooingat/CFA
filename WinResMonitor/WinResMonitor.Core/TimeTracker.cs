using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WinResMonitor.Core
{
    public class TimeTracker
    {
        private readonly string _connString;

        private static readonly string[] ObnoxiousMessages =
        {
            "Bruh!! What are you doing??",
            "Do you think this is helping in any way, shape, or form?",
            "Bro... seriously? Still here?",
            "Your future self is judging you right now.",
            "This is definitely not on your to-do list.",
            "Time is a non-renewable resource. Think about it.",
            "Your goals called. They miss you.",
            "Netflix and procrastinate? Really?",
            "You've been here for over an hour. Just saying.",
            "Is this really how you want to spend your day?",
            "Somewhere, a version of you who studied is doing great.",
            "Every minute here is a minute stolen from your future.",
        };

        public TimeTracker(string connString)
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
                CREATE TABLE IF NOT EXISTS TimeTracking (
                    Domain      TEXT NOT NULL,
                    Date        TEXT NOT NULL,
                    Seconds     INTEGER NOT NULL DEFAULT 0,
                    LastSeen    TEXT,
                    PRIMARY KEY (Domain, Date)
                );
                CREATE TABLE IF NOT EXISTS TempBlocked (
                    Domain      TEXT PRIMARY KEY,
                    BlockedUntil TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // Returns total seconds spent on this domain today after recording the visit.
        public int RecordVisit(string domain)
        {
            domain = domain.ToLowerInvariant();
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            var now = DateTime.UtcNow;

            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();

                // Get current record
                int currentSeconds = 0;
                DateTime? lastSeen = null;

                using (var sel = conn.CreateCommand())
                {
                    sel.CommandText = "SELECT Seconds, LastSeen FROM TimeTracking WHERE Domain = $d AND Date = $dt";
                    sel.Parameters.AddWithValue("$d", domain);
                    sel.Parameters.AddWithValue("$dt", today);
                    using var r = sel.ExecuteReader();
                    if (r.Read())
                    {
                        currentSeconds = r.GetInt32(0);
                        if (!r.IsDBNull(1))
                            lastSeen = DateTime.Parse(r.GetString(1));
                    }
                }

                // Calculate seconds to add
                int toAdd;
                if (lastSeen.HasValue && (now - lastSeen.Value).TotalSeconds < 120)
                    toAdd = Math.Max(1, (int)(now - lastSeen.Value).TotalSeconds);
                else
                    toAdd = 30; // session start cost

                int newTotal = currentSeconds + toAdd;

                using (var ups = conn.CreateCommand())
                {
                    ups.CommandText = @"
                        INSERT INTO TimeTracking (Domain, Date, Seconds, LastSeen)
                        VALUES ($d, $dt, $s, $ls)
                        ON CONFLICT(Domain, Date) DO UPDATE SET Seconds = $s, LastSeen = $ls";
                    ups.Parameters.AddWithValue("$d", domain);
                    ups.Parameters.AddWithValue("$dt", today);
                    ups.Parameters.AddWithValue("$s", newTotal);
                    ups.Parameters.AddWithValue("$ls", now.ToString("o"));
                    ups.ExecuteNonQuery();
                }

                // Auto-block at 75 minutes
                if (newTotal >= 75 * 60 && !IsTempBlocked(domain))
                    AddTempBlock(domain);

                return newTotal;
            }
            catch { return 0; }
        }

        public bool IsTempBlocked(string domain)
        {
            domain = domain.ToLowerInvariant();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();

                // Clean expired blocks
                using (var del = conn.CreateCommand())
                {
                    del.CommandText = "DELETE FROM TempBlocked WHERE BlockedUntil < $now";
                    del.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
                    del.ExecuteNonQuery();
                }

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM TempBlocked WHERE Domain = $d LIMIT 1";
                cmd.Parameters.AddWithValue("$d", domain);
                return cmd.ExecuteScalar() != null;
            }
            catch { return false; }
        }

        private void AddTempBlock(string domain)
        {
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO TempBlocked (Domain, BlockedUntil) VALUES ($d, $u)";
                cmd.Parameters.AddWithValue("$d", domain);
                cmd.Parameters.AddWithValue("$u", DateTime.UtcNow.AddHours(24).ToString("o"));
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public string GetObnoxiousMessage(int totalSeconds)
        {
            int index = (totalSeconds / 300) % ObnoxiousMessages.Length;
            return ObnoxiousMessages[index];
        }

        public List<(string Domain, string Date, int Seconds)> GetWeeklyStats()
        {
            var results = new List<(string, string, int)>();
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd");
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Domain, Date, Seconds FROM TimeTracking
                    WHERE Date >= $cutoff ORDER BY Date DESC, Seconds DESC";
                cmd.Parameters.AddWithValue("$cutoff", cutoff);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    results.Add((r.GetString(0), r.GetString(1), r.GetInt32(2)));
            }
            catch { }
            return results;
        }

        public List<(string Domain, int TotalSeconds)> GetTempBlocks()
        {
            var results = new List<(string, int)>();
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Domain, BlockedUntil FROM TempBlocked WHERE BlockedUntil > $now";
                cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var remaining = (int)(DateTime.Parse(r.GetString(1)) - DateTime.UtcNow).TotalSeconds;
                    results.Add((r.GetString(0), remaining));
                }
            }
            catch { }
            return results;
        }

        public void ClearAllTempBlocks()
        {
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM TempBlocked";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
