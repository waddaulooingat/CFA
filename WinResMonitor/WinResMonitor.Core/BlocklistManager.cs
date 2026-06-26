using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WinResMonitor.Core
{
    public class BlocklistManager
    {
        private HashSet<string> _blockedDomains = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _keywords = new();
        private readonly string _configPath;

        public BlocklistManager()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WinResMonitor", "blocklist.json");

            EnsureConfigDirectory();
            LoadFromDisk();
            LoadBuiltInAdultDomains();
        }

        public bool IsBlocked(string host, string url)
        {
            // Strip www. prefix
            var cleanHost = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? host[4..] : host;

            if (_blockedDomains.Contains(cleanHost)) return true;
            if (_blockedDomains.Contains(host)) return true;

            // Keyword match against URL
            var lowerUrl = url.ToLowerInvariant();
            return _keywords.Any(k => lowerUrl.Contains(k.ToLowerInvariant()));
        }

        public void AddDomain(string domain)
        {
            domain = domain.Trim().ToLowerInvariant();
            _blockedDomains.Add(domain);
            SaveToDisk();
        }

        public void RemoveDomain(string domain)
        {
            _blockedDomains.Remove(domain.Trim().ToLowerInvariant());
            SaveToDisk();
        }

        public void AddKeyword(string keyword)
        {
            if (!_keywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            {
                _keywords.Add(keyword.Trim());
                SaveToDisk();
            }
        }

        public void RemoveKeyword(string keyword)
        {
            _keywords.RemoveAll(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
            SaveToDisk();
        }

        public IReadOnlyList<string> GetDomains() => _blockedDomains.OrderBy(d => d).ToList();
        public IReadOnlyList<string> GetKeywords() => _keywords.AsReadOnly();

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
                    Keywords = _keywords
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
            // Curated list of known adult domains — extend as needed
            var builtIn = new[]
            {
                "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com", "youporn.com",
                "tube8.com", "xhamster.com", "spankbang.com", "eporner.com", "beeg.com",
                "tnaflix.com", "drtuber.com", "slutload.com", "porntrex.com", "txxx.com",
                "hclips.com", "faphouse.com", "hdporn.net", "pornone.com", "vporn.com",
                "onlyfans.com", "fansly.com", "chaturbate.com", "myfreecams.com",
                "cam4.com", "livejasmin.com", "stripchat.com", "bongacams.com",
                "brazzers.com", "bangbros.com", "naughtyamerica.com", "reality kings.com",
                "mofos.com", "babes.com", "digitalplayground.com", "fakehub.com",
                "kink.com", "adultfriendfinder.com", "ashleymadison.com"
            };

            foreach (var d in builtIn) _blockedDomains.Add(d);
        }
    }

    public class BlocklistConfig
    {
        public List<string> Domains { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
    }
}
