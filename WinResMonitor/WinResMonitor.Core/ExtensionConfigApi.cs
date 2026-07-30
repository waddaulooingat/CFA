using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace WinResMonitor.Core
{
    public class ExtensionConfigApi
    {
        private readonly BlocklistManager _blocklist;
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private const int Port = 8878;

        public ExtensionConfigApi(BlocklistManager blocklist)
        {
            _blocklist = blocklist;
        }

        public void Start()
        {
            if (_running) return;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _running = true;
            _thread = new Thread(Listen) { IsBackground = true, Name = "ExtensionConfigApi" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
        }

        private void Listen()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
                }
                catch { if (!_running) break; }
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            try
            {
                // CORS so the extension can call us
                ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
                ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
                ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    ctx.Response.StatusCode = 204;
                    ctx.Response.Close();
                    return;
                }

                if (ctx.Request.HttpMethod == "GET" &&
                    ctx.Request.Url.AbsolutePath.TrimEnd('/') == "/config")
                {
                    var payload = new
                    {
                        timeLimitSites = _blocklist.GetTimeLimitSites(),
                        whitelist      = _blocklist.GetWhitelistedDomains(),
                    };

                    var json  = JsonSerializer.Serialize(payload);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    ctx.Response.ContentType     = "application/json";
                    ctx.Response.ContentLength64 = bytes.Length;
                    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    ctx.Response.StatusCode = 200;
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                }
            }
            catch { }
            finally
            {
                try { ctx.Response.Close(); } catch { }
            }
        }
    }
}
