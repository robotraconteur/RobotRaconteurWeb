using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    static class PipeWaiterTest
    {
        static public async Task TestPipeReceiveWaitReleasesWaiter()
        {
            // TryReceivePacketWait must remove its waiter when it returns
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);
                // p1 only answers packets the client sends; broadcastpipe now broadcasts as in Core
                var ep = await c.p1.Connect(-1);

                for (int i = 0; i < 20; i++)
                {
                    var r = await ep.TryReceivePacketWait(1);
                    RRAssert.IsFalse(r.Item1);
                }

                var recv_waiter = ep.GetType().GetField("recv_waiter", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(ep);
                var wait_tasks = (ICollection)recv_waiter.GetType()
                    .GetField("wait_tasks", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(recv_waiter);
                RRAssert.AreEqual(wait_tasks.Count, 0);
                await ep.Close();
            }
            Console.WriteLine("PipeWaiterTest.TestPipeReceiveWaitReleasesWaiter passed");
        }
    }
}
