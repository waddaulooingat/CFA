using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinResMonitor.Core
{
    public class ProxyEngine
    {
        private HttpListener _listener;
        private readonly BlocklistManager _blocklist;
        private readonly Logger _logger;
        private readonly int _port;
        private bool _running;
        private CancellationTokenSource _cts;

        private string? _cachedGifUrl;
        private Timer? _gifRefreshTimer;
        private const int GifRefreshMinutes = 10;
        private const int WarnThresholdSeconds = 60 * 60;      // 1 hour
        private const int BlockThresholdSeconds = 60 * 75;     // 1h 15m

        public event Action<string, bool> OnRequestEvaluated;

        public ProxyEngine(int port = 8877)
        {
            _port = port;
            _blocklist = new BlocklistManager();
            _logger = new Logger();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Start();
            _running = true;

            Task.Run(() => AcceptLoopAsync(_cts.Token));

            _gifRefreshTimer = new Timer(_ => _ = RefreshGifAsync(), null,
                TimeSpan.Zero, TimeSpan.FromMinutes(GifRefreshMinutes));
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _listener?.Stop();
            _gifRefreshTimer?.Dispose();
        }

        private async Task RefreshGifAsync()
        {
            var key = _blocklist.GiphyApiKey;
            if (string.IsNullOrWhiteSpace(key)) return;
            var gif = await new GiphyClient(key).GetRandomGifUrlAsync();
            if (gif != null) _cachedGifUrl = gif;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (_running && !ct.IsCancellationRequested)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(ctx), ct);
                }
                catch (Exception ex) when (_running)
                {
                    _logger.LogError($"Accept loop error: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            var url = ctx.Request.Url?.ToString() ?? "";
            var host = ctx.Request.Url?.Host ?? "";

            bool blocked = _blocklist.IsBlocked(host, url);
            OnRequestEvaluated?.Invoke(url, blocked);
            _logger.LogRequest(url, host, blocked);

            if (blocked)
            {
                await ServeBlockPage(ctx);
                return;
            }

            // Track time for time-limit sites
            int secondsToday = _blocklist.TrackIfTimeLimitSite(host);

            await ForwardRequest(ctx, host, secondsToday);
        }

        private async Task ServeBlockPage(HttpListenerContext ctx)
        {
            try
            {
                var gifSection = _cachedGifUrl != null
                    ? $"<img src='{_cachedGifUrl}' alt='Stop' style='max-width:320px;border-radius:8px;margin:16px 0;'/>"
                    : "";

                string html = $@"<!DOCTYPE html>
<html>
<head><title>Access Blocked</title>
<style>
  body {{ font-family: Arial, sans-serif; text-align: center; padding: 80px; background: #f0f0f0; }}
  .box {{ background: white; padding: 40px; border-radius: 8px; display: inline-block; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
  h1 {{ color: #c0392b; }} p {{ color: #555; }}
</style></head>
<body><div class='box'>
  <h1>&#128683; Access Blocked</h1>
  {gifSection}
  <p>This website has been blocked by Windows Resource Monitor.</p>
  <p>If you believe this is an error, please contact your administrator.</p>
</div></body></html>";

                await WriteHtmlResponse(ctx, 403, html);
            }
            catch { }
        }

        private async Task ForwardRequest(HttpListenerContext ctx, string host, int secondsToday)
        {
            try
            {
                using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
                var targetUrl = ctx.Request.Url?.ToString();
                if (string.IsNullOrEmpty(targetUrl)) return;

                var reqMsg = new HttpRequestMessage(new HttpMethod(ctx.Request.HttpMethod), targetUrl);

                foreach (string header in ctx.Request.Headers)
                {
                    if (header == null) continue;
                    try { reqMsg.Headers.TryAddWithoutValidation(header, ctx.Request.Headers[header]); }
                    catch { }
                }

                if (ctx.Request.HasEntityBody)
                {
                    var body = new byte[ctx.Request.ContentLength64];
                    await ctx.Request.InputStream.ReadAsync(body, 0, body.Length);
                    reqMsg.Content = new ByteArrayContent(body);
                }

                var response = await client.SendAsync(reqMsg);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "";
                var respBytes = await response.Content.ReadAsByteArrayAsync();

                // Inject obnoxious banner if over warning threshold on a time-limit site
                if (secondsToday >= WarnThresholdSeconds && contentType.Contains("text/html"))
                {
                    var tracker = new TimeTracker(_blocklist.ConnString);
                    var message = tracker.GetObnoxiousMessage(secondsToday);
                    var mins = secondsToday / 60;
                    respBytes = InjectBanner(respBytes, message, mins);
                }

                ctx.Response.StatusCode = (int)response.StatusCode;
                ctx.Response.ContentType = contentType;
                ctx.Response.ContentLength64 = respBytes.Length;
                await ctx.Response.OutputStream.WriteAsync(respBytes, 0, respBytes.Length);
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Forward error: {ex.Message}");
                try { ctx.Response.StatusCode = 502; ctx.Response.Close(); } catch { }
            }
        }

        private static byte[] InjectBanner(byte[] htmlBytes, string message, int minutesSpent)
        {
            try
            {
                var html = Encoding.UTF8.GetString(htmlBytes);
                var banner = $@"
<div id='wrm-banner' style='position:fixed;top:0;left:0;right:0;z-index:2147483647;
  background:#c0392b;color:white;font-family:Arial,sans-serif;font-size:16px;
  padding:14px 20px;display:flex;align-items:center;justify-content:space-between;
  box-shadow:0 2px 8px rgba(0,0,0,0.3);'>
  <span>&#9888;&#65039; <strong>{EscapeHtml(message)}</strong> &nbsp;({minutesSpent} min spent here today)</span>
  <button onclick=""document.getElementById('wrm-banner').remove()""
    style='background:transparent;border:2px solid white;color:white;padding:4px 12px;
    border-radius:4px;cursor:pointer;font-size:13px;'>Dismiss</button>
</div>
<div style='height:52px'></div>";

                var bodyIdx = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                if (bodyIdx >= 0)
                {
                    var insertAt = html.IndexOf('>', bodyIdx) + 1;
                    html = html.Insert(insertAt, banner);
                }
                else
                {
                    html = banner + html;
                }
                return Encoding.UTF8.GetBytes(html);
            }
            catch { return htmlBytes; }
        }

        private static async Task WriteHtmlResponse(HttpListenerContext ctx, int statusCode, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "text/html";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static string EscapeHtml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        public bool IsRunning => _running;
        public int Port => _port;
        public BlocklistManager Blocklist => _blocklist;
    }
}
