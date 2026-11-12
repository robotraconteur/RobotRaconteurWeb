#if !ROBOTRACONTEUR_H5
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class TcpTransportTests
    {
        const int test_port = 47231;

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

        // Regression test: the WebSocket handshake header line limit must be enforced
        public static async Task TestWebSocketHandshakeHeaderLineLimit()
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
                    var sb = new StringBuilder();
                    sb.Append("GET / HTTP/1.1\r\n");
                    for (int i = 0; i < 200; i++)
                    {
                        sb.Append("X-Test-Header-" + i + ": value\r\n");
                    }
                    var b = Encoding.ASCII.GetBytes(sb.ToString());
                    // Header lines are sent but the header is never terminated
                    await stream.WriteAsync(b, 0, b.Length);

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
