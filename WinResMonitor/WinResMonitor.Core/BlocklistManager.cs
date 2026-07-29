using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WinResMonitor.Core
{
    public class BlocklistManager
    {
        private HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _whitelistedDomains = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _timeLimitSites = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _keywords = new();
        private readonly string _configPath;
        private readonly string _connString;
        private string _giphyApiKey = "";

        public BlocklistManager()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WinResMonitor");

            _configPath = Path.Combine(dir, "blocklist.json");
            _connString = $"Data Source={Path.Combine(dir, "requests.db")}";

            EnsureConfigDirectory();
            LoadFromDisk();
            LoadBuiltInAdultDomains();
            LoadBuiltInEducationWhitelist();
            LoadBuiltInTimeLimitSites();
        }

        public void ReloadExternalBlocklist() { }

        // Returns true if the request should be blocked.
        public bool IsBlocked(string host, string url)
        {
            var cleanHost = StripWww(host);

            // Whitelist always wins
            if (_whitelistedDomains.Contains(cleanHost) || _whitelistedDomains.Contains(host))
                return false;

            // Temp-blocked (time limit exceeded)
            var tracker = new TimeTracker(_connString);
            if (tracker.IsTempBlocked(cleanHost) || tracker.IsTempBlocked(host))
                return true;

            if (_blockedDomains.Contains(cleanHost) || _blockedDomains.Contains(host)) return true;
            if (IsBlockedByExternalList(cleanHost) || IsBlockedByExternalList(host)) return true;

            var lowerUrl = url.ToLowerInvariant();
            return _keywords.Any(k => lowerUrl.Contains(k.ToLowerInvariant()));
        }

        // Returns seconds spent today on this domain if it's a time-limit site, else -1.
        public int TrackIfTimeLimitSite(string host)
        {
            var cleanHost = StripWww(host);
            if (!_timeLimitSites.Contains(cleanHost) && !_timeLimitSites.Contains(host))
                return -1;

            var tracker = new TimeTracker(_connString);
            return tracker.RecordVisit(cleanHost);
        }

        private bool IsBlockedByExternalList(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            try
            {
                using var conn = new SqliteConnection(_connString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM ExternalBlocklist WHERE Domain = $d LIMIT 1";
                cmd.Parameters.AddWithValue("$d", host.ToLowerInvariant());
                return cmd.ExecuteScalar() != null;
            }
            catch { return false; }
        }

        // Blocklist
        public void AddDomain(string domain) { _blockedDomains.Add(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public void RemoveDomain(string domain) { _blockedDomains.Remove(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public IReadOnlyList<string> GetDomains() => _blockedDomains.OrderBy(d => d).ToList();

        // Keywords
        public void AddKeyword(string keyword)
        {
            if (!_keywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            { _keywords.Add(keyword.Trim()); SaveToDisk(); }
        }
        public void RemoveKeyword(string keyword) { _keywords.RemoveAll(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase)); SaveToDisk(); }
        public IReadOnlyList<string> GetKeywords() => _keywords.AsReadOnly();

        // Whitelist
        public void AddWhitelistedDomain(string domain) { _whitelistedDomains.Add(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public void RemoveWhitelistedDomain(string domain) { _whitelistedDomains.Remove(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public IReadOnlyList<string> GetWhitelistedDomains() => _whitelistedDomains.OrderBy(d => d).ToList();

        // Time limit sites
        public void AddTimeLimitSite(string domain) { _timeLimitSites.Add(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public void RemoveTimeLimitSite(string domain) { _timeLimitSites.Remove(domain.Trim().ToLowerInvariant()); SaveToDisk(); }
        public IReadOnlyList<string> GetTimeLimitSites() => _timeLimitSites.OrderBy(d => d).ToList();

        // Giphy
        public string GiphyApiKey => _giphyApiKey;
        public void SetGiphyApiKey(string key) { _giphyApiKey = key?.Trim() ?? ""; SaveToDisk(); }

        public string ConnString => _connString;

        private static string StripWww(string host) =>
            host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

        private void LoadFromDisk()
        {
            if (!File.Exists(_configPath)) return;
            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<BlocklistConfig>(json);
                if (config == null) return;

                foreach (var d in config.Domains ?? new()) _blockedDomains.Add(d);
                _keywords = config.Keywords ?? new();
                _giphyApiKey = config.GiphyApiKey ?? "";
                foreach (var d in config.WhitelistedDomains ?? new()) _whitelistedDomains.Add(d);
                foreach (var d in config.TimeLimitSites ?? new()) _timeLimitSites.Add(d);
            }
            catch { }
        }

        private void SaveToDisk()
        {
            try
            {
                var config = new BlocklistConfig
                {
                    Domains = _blockedDomains.ToList(),
                    Keywords = _keywords,
                    GiphyApiKey = _giphyApiKey,
                    WhitelistedDomains = _whitelistedDomains.ToList(),
                    TimeLimitSites = _timeLimitSites.ToList()
                };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private void EnsureConfigDirectory()
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        }

        private void LoadBuiltInAdultDomains()
        {
            var builtIn = new[]
            {
                "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com", "youporn.com",
                "tube8.com", "xhamster.com", "spankbang.com", "eporner.com", "beeg.com",
                "tnaflix.com", "drtuber.com", "slutload.com", "porntrex.com", "txxx.com",
                "hclips.com", "faphouse.com", "hdporn.net", "pornone.com", "vporn.com",
                "onlyfans.com", "fansly.com", "chaturbate.com", "myfreecams.com",
                "cam4.com", "livejasmin.com", "stripchat.com", "bongacams.com",
                "brazzers.com", "bangbros.com", "naughtyamerica.com", "realitykings.com",
                "mofos.com", "babes.com", "digitalplayground.com", "fakehub.com",
                "kink.com", "adultfriendfinder.com", "ashleymadison.com"
            };
            foreach (var d in builtIn) _blockedDomains.Add(d);
        }

        private void LoadBuiltInEducationWhitelist()
        {
            var builtIn = new[]
            {
                "khanacademy.org", "coursera.org", "edx.org", "udemy.com", "udacity.com",
                "duolingo.com", "quizlet.com", "wolframalpha.com", "wikipedia.org",
                "britannica.com", "scholastic.com", "ixl.com", "mathway.com",
                "desmos.com", "geogebra.org", "purplemath.com", "mathisfun.com",
                "grammar.com", "grammarly.com", "merriam-webster.com", "dictionary.com",
                "github.com", "stackoverflow.com", "w3schools.com", "mdn.org",
                "docs.microsoft.com", "learn.microsoft.com", "google.com",
                "docs.google.com", "classroom.google.com", "drive.google.com",
                "scholar.google.com", "jstor.org", "pubmed.ncbi.nlm.nih.gov",
                "ted.com", "archive.org", "gutenberg.org", "librivox.org",
                "nasa.gov", "nationalgeographic.com", "bbc.co.uk", "pbs.org"
            };
            foreach (var d in builtIn) _whitelistedDomains.Add(d);
        }

        private void LoadBuiltInTimeLimitSites()
        {
            var builtIn = new[]
            {
                "youtube.com", "discord.com", "tiktok.com", "netflix.com",
                "twitch.tv", "reddit.com", "instagram.com", "twitter.com",
                "x.com", "facebook.com", "snapchat.com", "pinterest.com",
                "tumblr.com", "9gag.com", "imgur.com", "steam.com", "roblox.com"
            };
            foreach (var d in builtIn) _timeLimitSites.Add(d);
        }
    }

    public class BlocklistConfig
    {
        public List<string> Domains { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public string GiphyApiKey { get; set; } = "";
        public List<string> WhitelistedDomains { get; set; } = new();
        public List<string> TimeLimitSites { get; set; } = new();
    }
}
