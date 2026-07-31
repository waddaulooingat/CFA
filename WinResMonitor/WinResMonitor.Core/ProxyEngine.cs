using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinResMonitor.Core
{
    public class ProxyEngine
    {
        private TcpListener _listener;
        private readonly BlocklistManager _blocklist;
        private readonly Logger _logger;
        private readonly int _port;
        private volatile bool _running;
        private CancellationTokenSource _cts;

        private string? _cachedGifUrl;
        private Timer? _gifRefreshTimer;
        private const int GifRefreshMinutes = 10;

        // Shared HttpClient — reusing avoids socket exhaustion and connection overhead
        private static readonly HttpClient _httpClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = true, UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public event Action<string, bool>? OnRequestEvaluated;

        public ProxyEngine(int port = 8877)
        {
            _port      = port;
            _blocklist = new BlocklistManager();
            _logger    = new Logger();
        }

        public void Start()
        {
            _cts      = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _running  = true;

            Task.Run(() => AcceptLoopAsync(_cts.Token));

            _gifRefreshTimer = new Timer(_ => _ = RefreshGifAsync(), null,
                TimeSpan.Zero, TimeSpan.FromMinutes(GifRefreshMinutes));
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            _gifRefreshTimer?.Dispose();
        }

        // ── Accept loop ───────────────────────────────────────────────────────
        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (_running && !ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(ct);
                    client.ReceiveTimeout = 30_000;
                    client.SendTimeout    = 30_000;
                    _ = Task.Run(() => HandleClientAsync(client), ct);
                }
                catch when (!_running) { break; }
                catch (Exception ex) when (_running)
                {
                    _logger.LogError($"Accept loop: {ex.Message}");
                }
            }
        }

        // ── Per-connection handler ────────────────────────────────────────────
        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var headerBytes = await ReadUntilBlankLineAsync(stream);
                    if (headerBytes == null || headerBytes.Length == 0) return;

                    var headerText = Encoding.ASCII.GetString(headerBytes);
                    var firstLine  = headerText.Split(new[] { "\r\n" }, 2, StringSplitOptions.None)[0];
                    var parts      = firstLine.Split(' ');
                    if (parts.Length < 2) return;

                    var method = parts[0].ToUpperInvariant();
                    var target = parts[1];

                    if (method == "CONNECT")
                        await HandleConnectAsync(stream, target);
                    else
                        await HandleHttpAsync(stream, method, target, headerText);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Client handler: {ex.Message}");
                }
            }
        }

        // Domains Windows uses for connectivity checks — pass through silently, never log
        private static readonly HashSet<string> _systemDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "msftncsi.com", "ipv6.msftncsi.com", "ipv4.msftncsi.com",
            "msftconnecttest.com", "www.msftconnecttest.com",
            "dns.msft.net", "ctldl.windowsupdate.com"
        };

        // Domain suffixes that should always tunnel silently (system/Microsoft services)
        private static readonly string[] _systemSuffixes = new[]
        {
            ".msftncsi.com", ".live.com", ".microsoft.com", ".microsoftonline.com",
            ".windows.com", ".windowsupdate.com", ".office.com", ".office365.com",
            ".teams.microsoft.com", ".skype.com", ".azure.com", ".azureedge.net"
        };

        private static bool IsSystemDomain(string host) =>
            _systemDomains.Contains(host) ||
            _systemSuffixes.Any(s => host.EndsWith(s, StringComparison.OrdinalIgnoreCase));

        // ── HTTPS tunnel (CONNECT) ────────────────────────────────────────────
        private async Task HandleConnectAsync(NetworkStream clientStream, string target)
        {
            var (host, port) = ParseHostPort(target, 443);

            // Pass Windows system connectivity checks through without logging
            if (IsSystemDomain(host))
            {
                await TunnelAsync(clientStream, host, port);
                return;
            }

            var url     = $"https://{host}/";
            var blocked = _blocklist.IsBlocked(host, url);

            _logger.LogRequest(url, host, blocked);
            OnRequestEvaluated?.Invoke(url, blocked);

            if (blocked)
            {
                // Browser will show a connection error — enough to stop access
                var deny = "HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(deny));
                return;
            }

            await TunnelAsync(clientStream, host, port);
        }

        private async Task TunnelAsync(NetworkStream clientStream, string host, int port)
        {
            try
            {
                using var remote = new TcpClient();
                await remote.ConnectAsync(host, port);
                var ok = "HTTP/1.1 200 Connection Established\r\n\r\n";
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(ok));
                using var remoteStream = remote.GetStream();
                await Task.WhenAny(
                    PipeAsync(clientStream, remoteStream),
                    PipeAsync(remoteStream, clientStream)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"CONNECT tunnel {host}: {ex.Message}");
            }
        }

        // ── Plain HTTP forward ────────────────────────────────────────────────
        private async Task HandleHttpAsync(NetworkStream clientStream, string method, string target, string headerText)
        {
            var host = ExtractHost(target, headerText);
            var url  = target.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                       ? target : $"http://{host}{target}";

            var blocked = _blocklist.IsBlocked(host, url);
            _logger.LogRequest(url, host, blocked);
            OnRequestEvaluated?.Invoke(url, blocked);

            if (blocked)
            {
                await ServeBlockPageAsync(clientStream);
                return;
            }

            try
            {
                var req = new HttpRequestMessage(new HttpMethod(method), url);
                foreach (var line in headerText.Split(new[] { "\r\n" }, StringSplitOptions.None).Skip(1))
                {
                    var idx = line.IndexOf(':');
                    if (idx < 1) continue;
                    var name  = line[..idx].Trim();
                    var value = line[(idx + 1)..].Trim();
                    if (IsHopByHop(name)) continue;
                    try { req.Headers.TryAddWithoutValidation(name, value); } catch { }
                }

                var resp      = await _httpClient.SendAsync(req);
                var bodyBytes = await resp.Content.ReadAsByteArrayAsync();
                var ct        = resp.Content.Headers.ContentType?.ToString() ?? "";

                var sb = new StringBuilder();
                sb.Append($"HTTP/1.1 {(int)resp.StatusCode} {resp.ReasonPhrase}\r\n");
                foreach (var h in resp.Headers)
                    foreach (var v in h.Value)
                        sb.Append($"{h.Key}: {v}\r\n");
                sb.Append($"Content-Type: {ct}\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n");

                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(sb.ToString()));
                await clientStream.WriteAsync(bodyBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"HTTP forward: {ex.Message}");
                var err = "HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                try { await clientStream.WriteAsync(Encoding.ASCII.GetBytes(err)); } catch { }
            }
        }

        // ── Block page ────────────────────────────────────────────────────────
        private async Task ServeBlockPageAsync(NetworkStream stream)
        {
            try
            {
                var gifSection = _cachedGifUrl != null
                    ? $"<img src='{_cachedGifUrl}' alt='Stop' style='max-width:320px;border-radius:8px;margin:16px 0'/>"
                    : "";

                var html = $@"<!DOCTYPE html>
<html><head><title>Access Blocked</title><style>
  body{{font-family:Arial,sans-serif;text-align:center;padding:80px;background:#f0f0f0}}
  .box{{background:white;padding:40px;border-radius:8px;display:inline-block;box-shadow:0 2px 8px rgba(0,0,0,.1)}}
  h1{{color:#c0392b}}p{{color:#555}}
</style></head><body><div class='box'>
  <h1>&#128683; Access Blocked</h1>
  {gifSection}
  <p>This website has been blocked by Windows Resource Monitor.</p>
  <p>If you believe this is an error, please contact your administrator.</p>
</div></body></html>";

                var body   = Encoding.UTF8.GetBytes(html);
                var header = $"HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                await stream.WriteAsync(body);
            }
            catch { }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task RefreshGifAsync()
        {
            var key = _blocklist.GiphyApiKey;
            if (string.IsNullOrWhiteSpace(key)) return;
            var gif = await new GiphyClient(key).GetRandomGifUrlAsync();
            if (gif != null) _cachedGifUrl = gif;
        }

        private static async Task<byte[]?> ReadUntilBlankLineAsync(NetworkStream stream)
        {
            var ms  = new MemoryStream();
            var buf = new byte[4096];

            while (ms.Length < 65_536)
            {
                // Only read what's available to avoid blocking waiting for more data
                int toRead = stream.DataAvailable ? buf.Length : 1;
                int n = await stream.ReadAsync(buf, 0, toRead);
                if (n == 0) break;
                ms.Write(buf, 0, n);

                // Scan for \r\n\r\n
                var arr = ms.GetBuffer();
                var len = (int)ms.Length;
                for (int i = 0; i <= len - 4; i++)
                {
                    if (arr[i] == '\r' && arr[i+1] == '\n' && arr[i+2] == '\r' && arr[i+3] == '\n')
                        return ms.ToArray()[..(i + 4)];
                }
            }
            return ms.Length > 0 ? ms.ToArray() : null;
        }

        private static async Task PipeAsync(Stream from, Stream to)
        {
            try
            {
                var buf = new byte[65_536];
                int n;
                while ((n = await from.ReadAsync(buf, 0, buf.Length)) > 0)
                    await to.WriteAsync(buf, 0, n);
            }
            catch { }
        }

        private static (string host, int port) ParseHostPort(string target, int defaultPort)
        {
            var idx = target.LastIndexOf(':');
            if (idx > 0 && int.TryParse(target[(idx + 1)..], out int p))
                return (target[..idx], p);
            return (target, defaultPort);
        }

        private static string ExtractHost(string target, string headers)
        {
            try
            {
                if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return new Uri(target).Host;
            }
            catch { }

            foreach (var line in headers.Split(new[] { "\r\n" }, StringSplitOptions.None))
                if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                    return line[5..].Trim().Split(':')[0];

            return "";
        }

        private static bool IsHopByHop(string name) =>
            name.Equals("Connection",          StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Keep-Alive",          StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Transfer-Encoding",   StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Proxy-Connection",    StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Proxy-Authenticate",  StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("TE",                  StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Trailers",            StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Upgrade",             StringComparison.OrdinalIgnoreCase);

        public bool IsRunning => _running;
        public int Port => _port;
        public BlocklistManager Blocklist => _blocklist;
    }
}
