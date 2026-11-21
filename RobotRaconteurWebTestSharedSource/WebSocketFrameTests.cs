#if !ROBOTRACONTEUR_H5
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class WebSocketFrameTests
    {
        const int test_port = 47236;

        static async Task WithWebSocketServer(Func<NetworkStream, Task> f)
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
                    c.NoDelay = true;
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

                    var resp = new StringBuilder();
                    var rb = new byte[1];
                    while (!resp.ToString().EndsWith("\r\n\r\n"))
                    {
                        RRAssert.AreEqual(await ReadWithTimeout(stream, rb, 1, 5000), 1);
                        resp.Append((char)rb[0]);
                    }
                    RRAssert.IsTrue(resp.ToString().StartsWith("HTTP/1.1 101"));

                    await f(stream);
                }
            }
            finally
            {
                node.Shutdown();
            }
        }

        // Reads exactly count bytes; returns the number read before the connection closed or the timeout expired
        static async Task<int> ReadWithTimeout(NetworkStream stream, byte[] buf, int count, int timeout_ms)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout_ms);
            int n = 0;
            while (n < count)
            {
                var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remaining <= 0)
                    return n;
                var read_task = stream.ReadAsync(buf, n, count - n);
                if (await Task.WhenAny(read_task, Task.Delay(remaining)) != read_task)
                    return n;
                int r;
                try
                {
                    r = await read_task;
                }
                catch (Exception)
                {
                    return n;
                }
                if (r == 0)
                    return n;
                n += r;
            }
            return n;
        }

        // Masked client frame with the given opcode and payload (mask key 1,2,3,4)
        static byte[] MaskedFrame(byte opcode, byte[] payload)
        {
            var mask = new byte[] { 1, 2, 3, 4 };
            var f = new byte[6 + payload.Length];
            f[0] = (byte)(0x80 | opcode);
            f[1] = (byte)(0x80 | payload.Length);
            Buffer.BlockCopy(mask, 0, f, 2, 4);
            for (int i = 0; i < payload.Length; i++)
            {
                f[6 + i] = (byte)(payload[i] ^ mask[i % 4]);
            }
            return f;
        }

        static async Task ExpectPong(NetworkStream stream, byte[] payload)
        {
            var h = new byte[2 + payload.Length];
            RRAssert.AreEqual(await ReadWithTimeout(stream, h, h.Length, 5000), h.Length);
            RRAssert.AreEqual(h[0], (byte)0x8A);
            RRAssert.AreEqual(h[1], (byte)payload.Length);
            for (int i = 0; i < payload.Length; i++)
            {
                RRAssert.AreEqual(h[2 + i], payload[i]);
            }
        }

        // Regression test: zero-length binary frames and control frames must not close the connection
        public static async Task TestWebSocketZeroLengthFrames()
        {
            await WithWebSocketServer(async stream =>
            {
                // Empty binary frame, then a ping with a payload: the ping must be answered
                var f1 = MaskedFrame(0x2, new byte[0]);
                var f2 = MaskedFrame(0x9, new byte[] { 0x61, 0x62 });
                await stream.WriteAsync(f1, 0, f1.Length);
                await stream.WriteAsync(f2, 0, f2.Length);
                await ExpectPong(stream, new byte[] { 0x61, 0x62 });

                // Ping with an empty payload must be answered with an empty pong
                var f3 = MaskedFrame(0x9, new byte[0]);
                await stream.WriteAsync(f3, 0, f3.Length);
                await ExpectPong(stream, new byte[0]);
            });
            Console.WriteLine("WebSocketFrameTests.TestWebSocketZeroLengthFrames passed");
        }

        // Regression test: a frame header that arrives in several TCP segments must be assembled correctly
        public static async Task TestWebSocketSplitFrameHeader()
        {
            await WithWebSocketServer(async stream =>
            {
                // Send a masked ping one byte at a time so the 2-byte header and the 4-byte mask key arrive
                // in separate reads
                var payload = new byte[] { 0x61, 0x62, 0x63 };
                var f = MaskedFrame(0x9, payload);
                for (int i = 0; i < f.Length; i++)
                {
                    await stream.WriteAsync(f, i, 1);
                    await stream.FlushAsync();
                    await Task.Delay(50);
                }
                await ExpectPong(stream, payload);
            });
            Console.WriteLine("WebSocketFrameTests.TestWebSocketSplitFrameHeader passed");
        }
    }
}
#endif
