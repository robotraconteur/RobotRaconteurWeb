using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurSharp;

namespace RobotRaconteurTest
{
    // Server for the Blazor WebAssembly browser tests (RobotRaconteur.Sharp.BlazorTest). Runs the standard test
    // services on a TcpTransport port, allows WebSocket connections from the page origin, and serves the published
    // Blazor app over HTTP. Runs until the process is killed.
    //
    // Usage: browsertestserver <tcp_port> <http_port> <wwwroot>
    public static class BrowserTestServer
    {
        static readonly Dictionary<string, string> mime_types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html; charset=utf-8" },
            { ".js", "text/javascript" },
            { ".mjs", "text/javascript" },
            { ".css", "text/css" },
            { ".json", "application/json" },
            { ".wasm", "application/wasm" },
            { ".ico", "image/x-icon" },
            { ".svg", "image/svg+xml" },
            { ".png", "image/png" },
        };

        public static async Task RunBrowserTestServer(string[] args)
        {
            if (args.Length != 4)
            {
                throw new ArgumentException("Usage: browsertestserver <tcp_port> <http_port> <wwwroot>");
            }
            int tcp_port = int.Parse(args[1]);
            int http_port = int.Parse(args[2]);
            string wwwroot = Path.GetFullPath(args[3]);
            if (!File.Exists(Path.Combine(wwwroot, "index.html")))
            {
                throw new ArgumentException("index.html not found in " + wwwroot);
            }

            using (var nodes = new TestNodeConfig("browsertestserver", true, false, false, true, tcp_port))
            {
                var tcp = (TcpTransport)nodes.tcp_transport;
                tcp.AddWebSocketAllowedOrigin("http://localhost:" + http_port);
                tcp.AddWebSocketAllowedOrigin("http://127.0.0.1:" + http_port);

                var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:" + http_port + "/");
                listener.Prefixes.Add("http://127.0.0.1:" + http_port + "/");
                listener.Start();

                Console.WriteLine("Browser test server: services on rr+ws://localhost:{0}, app on http://localhost:{1}/",
                    tcp_port, http_port);

                while (true)
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => ServeFile(context, wwwroot));
                }
            }
        }

        static void ServeFile(HttpListenerContext context, string wwwroot)
        {
            var response = context.Response;
            try
            {
                string rel = Uri.UnescapeDataString(context.Request.Url.AbsolutePath).TrimStart('/');
                if (rel == "")
                {
                    rel = "index.html";
                }
                string path = Path.GetFullPath(Path.Combine(wwwroot, rel));
                if (!path.StartsWith(wwwroot + Path.DirectorySeparatorChar) || !File.Exists(path))
                {
                    response.StatusCode = 404;
                    return;
                }

                string content_type;
                if (!mime_types.TryGetValue(Path.GetExtension(path), out content_type))
                {
                    content_type = "application/octet-stream";
                }
                byte[] data = File.ReadAllBytes(path);
                response.ContentType = content_type;
                response.Headers["Cache-Control"] = "no-store";
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("Browser test server error: " + e.Message);
                try { response.StatusCode = 500; } catch { }
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }
    }
}
