using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _listener?.Stop();
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

            await ForwardRequest(ctx);
        }

        private async Task ServeBlockPage(HttpListenerContext ctx)
        {
            try
            {
                string html = @"<!DOCTYPE html>
<html>
<head><title>Access Blocked</title>
<style>
  body { font-family: Arial, sans-serif; text-align: center; padding: 80px; background: #f0f0f0; }
  .box { background: white; padding: 40px; border-radius: 8px; display: inline-block; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
  h1 { color: #c0392b; } p { color: #555; }
</style></head>
<body><div class='box'>
  <h1>&#128683; Access Blocked</h1>
  <p>This website has been blocked by Windows Resource Monitor.</p>
  <p>If you believe this is an error, please contact your administrator.</p>
</div></body></html>";

                byte[] bytes = Encoding.UTF8.GetBytes(html);
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "text/html";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch { /* client disconnected */ }
        }

        private async Task ForwardRequest(HttpListenerContext ctx)
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
                ctx.Response.StatusCode = (int)response.StatusCode;
                ctx.Response.ContentType = response.Content.Headers.ContentType?.ToString();

                var respBytes = await response.Content.ReadAsByteArrayAsync();
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

        public bool IsRunning => _running;
        public int Port => _port;
        public BlocklistManager Blocklist => _blocklist;
    }
}
