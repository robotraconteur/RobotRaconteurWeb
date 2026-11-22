#if !ROBOTRACONTEUR_H5
using System;
using System.IO;
using System.Linq;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: a LocalTransport server start that fails after the socket was bound must
    // release the socket and its file, and must not leave the transport marked as started
    public static class LocalStartFailTests
    {
        static string[] SocketFiles(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return new string[0];
            }
            return Directory.GetFiles(dir, "*.sock");
        }

        public static void RunTestLocalStartFailureCleanup()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var t = new LocalTransport(node);
                if (!t.IsLocalTransportSupported)
                {
                    return;
                }

                // A NodeID that differs from the one stored for the name makes the start fail after the bind
                node.NodeID = NodeID.NewUniqueID();

                string socket_dir = Path.Combine(node.NodeDirectories.user_run_dir, "socket");
                var before = SocketFiles(socket_dir);

                for (int i = 0; i < 2; i++)
                {
                    Exception err = null;
                    try
                    {
                        t.StartServerAsNodeName("local_start_fail_server");
                    }
                    catch (Exception e)
                    {
                        err = e;
                    }
                    RRAssert.IsTrue(err != null);
                    RRAssert.AreNotEqual(err.Message, "Server already started");
                    RRAssert.AreEqual(SocketFiles(socket_dir).Except(before).Count(), 0);
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
