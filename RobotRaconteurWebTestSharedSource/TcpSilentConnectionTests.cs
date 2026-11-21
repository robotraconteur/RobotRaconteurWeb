using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
#if !ROBOTRACONTEUR_H5
    // An accepted TCP connection that never sends data is closed after the receive timeout
    public static class TcpSilentConnectionTests
    {
        public static async Task RunTestSilentConnectionClosed()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var t = new TcpTransport(node);
                t.DefaultReceiveTimeout = 1000;
                t.StartServer(0);
                node.RegisterTransport(t);

                var u = TransportUtil.ParseConnectionUrl(t.ServerListenUrls.First(x => !x.Contains("[")) + "&service=RobotRaconteurServiceIndex");

                using (var c = new TcpClient())
                {
                    await c.ConnectAsync("127.0.0.1", u.port);
                    var stream = c.GetStream();
                    var buf = new byte[16];
                    var read_task = stream.ReadAsync(buf, 0, buf.Length);
                    var done = await Task.WhenAny(read_task, Task.Delay(10000));
                    RRAssert.IsTrue(done == read_task);
                    int n = 0;
                    try
                    {
                        n = await read_task;
                    }
                    catch (Exception)
                    { }
                    RRAssert.AreEqual(n, 0);
                }
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
#endif
}
