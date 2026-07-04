using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WinResMonitor.Core
{
    public class GiphyClient
    {
        private const string BaseUrl = "https://api.giphy.com/v1/gifs/random";
        private const string SearchTag = "stop sign";
        private const string Rating = "g";

        private readonly string _apiKey;

        public GiphyClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string?> GetRandomGifUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return null;

            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(10);

                var url = $"{BaseUrl}?api_key={Uri.EscapeDataString(_apiKey)}&tag={Uri.EscapeDataString(SearchTag)}&rating={Rating}";
                var json = await http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("data")
                    .GetProperty("images")
                    .GetProperty("original")
                    .GetProperty("url")
                    .GetString();
            }
            catch
            {
                return null;
            }
        }
    }
}
