using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class TcpWebSocketHeaderCaseTest
    {

        static async Task<string> WebSocketHandshakeStatus(IPAddress host, int port, string headers)
        {
            using (var c = new TcpClient(AddressFamily.InterNetwork))
            {
                await c.ConnectAsync(host, port);
                var s = c.GetStream();
                var req = System.Text.Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n" + headers + "\r\n");
                await s.WriteAsync(req, 0, req.Length);
                var buf = new byte[4096];
                int n = 0;
                var cancel = new CancellationTokenSource(10000);
                while (n < buf.Length)
                {
                    var read = s.ReadAsync(buf, n, buf.Length - n, cancel.Token);
                    if (await Task.WhenAny(read, Task.Delay(10000)) != read) break;
                    int r = await read;
                    if (r == 0) break;
                    n += r;
                    if (System.Text.Encoding.ASCII.GetString(buf, 0, n).Contains("\r\n\r\n")) break;
                }
                var resp = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                int eol = resp.IndexOf("\r\n");
                return eol >= 0 ? resp.Substring(0, eol) : resp;
            }
        }

        // Regression test: WebSocket handshake header names are case-insensitive (RFC 7230), and a
        // repeated header must not end the connection. Previously "sec-websocket-key:" was not found
        // (426 Upgrade Required) and a repeated header other than Sec-WebSocket-Protocol / -Version made
        // the dictionary insert throw and the connection was closed without a reply.
        public static async Task TestWebSocketHandshakeHeaderCase()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                // IPv4 and IPv6 listeners have different ports; use an IPv4 one
                var tcp_url = System.Linq.Enumerable.First(nodes.GetServiceUrl("RobotRaconteurTestService", "rr+tcp"), x => !x.Contains("["));
                var host_port = System.Text.RegularExpressions.Regex.Match(tcp_url, "://([^:/]+):([0-9]+)/");
                var host = IPAddress.Parse(host_port.Groups[1].Value);
                int port = int.Parse(host_port.Groups[2].Value);

                string common = "Host: 127.0.0.1\r\nSec-WebSocket-Protocol: robotraconteur.robotraconteur.com\r\n";
                // Reference: the usual header spelling is accepted
                var r1 = await WebSocketHandshakeStatus(host, port, common + "Upgrade: websocket\r\nConnection: Upgrade\r\n"
                    + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n");
                RRAssert.AreEqual(r1, "HTTP/1.1 101 Switching Protocols");

                var r2 = await WebSocketHandshakeStatus(host, port, common + "upgrade: websocket\r\nconnection: Upgrade\r\n"
                    + "sec-websocket-key: dGhlIHNhbXBsZSBub25jZQ==\r\nsec-websocket-version: 13\r\n");
                RRAssert.AreEqual(r2, "HTTP/1.1 101 Switching Protocols");

                var r3 = await WebSocketHandshakeStatus(host, port, common + "Upgrade: websocket\r\nConnection: Upgrade\r\n"
                    + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n"
                    + "Cache-Control: no-cache\r\nCache-Control: no-store\r\n");
                RRAssert.AreEqual(r3, "HTTP/1.1 101 Switching Protocols");
            }
            Console.WriteLine("TcpWebSocketHeaderCaseTest.TestWebSocketHandshakeHeaderCase passed");
        }
    }
}
