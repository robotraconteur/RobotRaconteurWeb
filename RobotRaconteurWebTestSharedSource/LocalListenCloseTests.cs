using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: closing a LocalTransport server must close the listening socket and delete
    // its socket file without waiting for another incoming connection
    public static class LocalListenCloseTests
    {
        static string[] SocketFiles(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return new string[0];
            }
            return Directory.GetFiles(dir, "*.sock");
        }

        public static async Task RunTestLocalCloseRemovesSocket()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var t = new LocalTransport(node);
                if (!t.IsLocalTransportSupported)
                {
                    return;
                }

                string socket_dir = Path.Combine(node.NodeDirectories.user_run_dir, "socket");
                var before = SocketFiles(socket_dir);

                t.StartServerAsNodeName("local_listen_close_server");
                node.RegisterTransport(t);

                var created = SocketFiles(socket_dir).Except(before).ToArray();
                RRAssert.AreEqual(created.Length, 1);

                // Let DoListen (which starts after a 10 ms delay) block in AcceptAsync. Only the
                // without-fix failure depends on this; with the fix Close ends the wait either way
                await Task.Delay(500);

                await t.Close();

                for (int i = 0; i < 50 && File.Exists(created[0]); i++)
                {
                    await Task.Delay(100);
                }
                RRAssert.IsFalse(File.Exists(created[0]));
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
