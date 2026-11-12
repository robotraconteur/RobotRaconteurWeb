#if !ROBOTRACONTEUR_H5
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class WebSocketTests
    {
        const int test_port = 47232;

        // Returns true if the server closed the connection within timeout_ms
        static async Task<bool> WaitForServerClose(NetworkStream stream, int timeout_ms)
        {
            var buf = new byte[1024];
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout_ms);
            while (true)
            {
                var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remaining <= 0)
                    return false;
                var read_task = stream.ReadAsync(buf, 0, buf.Length);
                if (await Task.WhenAny(read_task, Task.Delay(remaining)) != read_task)
                    return false;
                int n;
                try
                {
                    n = await read_task;
                }
                catch (Exception)
                {
                    return true;
                }
                if (n == 0)
                    return true;
            }
        }

        // Regression test: WebSocket control frames with a payload > 125 bytes must be rejected
        // instead of allocating a buffer of the length claimed by the peer
        public static async Task TestWebSocketOversizedControlFrame()
        {
            var node = new RobotRaconteurNode();
            node.SetLogLevelFromString("ERROR");
            var t = new TcpTransport(node);
            node.RegisterTransport(t);
            t.StartServer(test_port, true);
            try
            {
                using (var c = new TcpClient())
                {
                    await c.ConnectAsync("127.0.0.1", test_port);
                    var stream = c.GetStream();
                    var req = "GET / HTTP/1.1\r\n"
                        + "Host: 127.0.0.1\r\n"
                        + "Upgrade: websocket\r\n"
                        + "Connection: Upgrade\r\n"
                        + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"
                        + "Sec-WebSocket-Version: 13\r\n"
                        + "Sec-WebSocket-Protocol: robotraconteur.robotraconteur.com\r\n"
                        + "\r\n";
                    var b = Encoding.ASCII.GetBytes(req);
                    await stream.WriteAsync(b, 0, b.Length);

                    // Read the handshake response
                    var resp = new StringBuilder();
                    var rb = new byte[1];
                    while (!resp.ToString().EndsWith("\r\n\r\n"))
                    {
                        var read_task = stream.ReadAsync(rb, 0, 1);
                        RRAssert.IsTrue(await Task.WhenAny(read_task, Task.Delay(5000)) == read_task);
                        RRAssert.IsTrue(await read_task == 1);
                        resp.Append((char)rb[0]);
                    }
                    RRAssert.IsTrue(resp.ToString().StartsWith("HTTP/1.1 101"));

                    // Masked ping frame header claiming a 1 GiB payload, payload never sent
                    var frame = new byte[] { 0x89, 0x80 | 127, 0, 0, 0, 0, 0x40, 0, 0, 0, 1, 2, 3, 4 };
                    await stream.WriteAsync(frame, 0, frame.Length);

                    RRAssert.IsTrue(await WaitForServerClose(stream, 5000));
                }
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
#endif
