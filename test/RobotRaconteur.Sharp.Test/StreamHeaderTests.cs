using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    public class StreamHeaderTests
    {
        const int test_port = 47234;

        // Regression test: a message header claiming a length above the maximum message size must
        // be rejected (connection closed) before the receive buffer is grown to that size.
        // The server runs in this process, so the allocation is visible in GC.GetTotalAllocatedBytes
        public static async Task TestOversizedMessageLengthRejected()
        {
            var node = new RobotRaconteurNode();
            node.SetLogLevelFromString("ERROR");
            node.NodeID = new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451");
            var t = new TcpTransport(node);
            node.RegisterTransport(t);
            t.StartServer(test_port, true);
            try
            {
                long alloc_before = GC.GetTotalAllocatedBytes(true);
                using (var c = new TcpClient())
                {
                    await c.ConnectAsync("127.0.0.1", test_port);
                    var stream = c.GetStream();
                    var h = new byte[16];
                    h[0] = (byte)'R'; h[1] = (byte)'R'; h[2] = (byte)'A'; h[3] = (byte)'C';
                    BitConverter.GetBytes((uint)0x10000000).CopyTo(h, 4);
                    BitConverter.GetBytes((ushort)2).CopyTo(h, 8);
                    await stream.WriteAsync(h, 0, h.Length);

                    var buf = new byte[1024];
                    bool closed = false;
                    var deadline = DateTime.UtcNow.AddSeconds(5);
                    while (!closed && DateTime.UtcNow < deadline)
                    {
                        var rt = stream.ReadAsync(buf, 0, buf.Length);
                        if (await Task.WhenAny(rt, Task.Delay(deadline - DateTime.UtcNow)) != rt)
                            break;
                        try { closed = await rt == 0; }
                        catch (Exception) { closed = true; }
                    }
                    RRAssert.IsTrue(closed);
                }
                long alloc = GC.GetTotalAllocatedBytes(true) - alloc_before;
                // Claimed length is 256 MiB; buffer growth would allocate ~307 MiB
                RRAssert.IsTrue(alloc < 100L * 1024 * 1024);
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
