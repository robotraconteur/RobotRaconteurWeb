using System;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // The Web test server must behave like Core's test server for the shared test clients:
    // broadcastpipe sends packets, and a monitor lock timeout is returned as RequestTimeoutException
    static class TestServiceBroadcastMonitorTest
    {
        static public async Task RunTestServiceBroadcastMonitorTest()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService")[0];
                var r1 = (testroot)await nodes.client_node.ConnectService(url);
                var r2 = (testroot)await nodes.client_node.ConnectService(url);

                var ep = await r1.broadcastpipe.Connect(-1);
                bool received = false;
                for (int i = 0; i < 50 && !received; i++)
                {
                    received = (await ep.TryReceivePacketWait(100)).Item1;
                }
                RRAssert.IsTrue(received);
                await ep.Close();

                var o1 = await r1.get_o1();
                var o2 = await r2.get_o1();
                var l1 = await nodes.client_node.MonitorEnter(o1);
                bool timeout_caught = false;
                try
                {
                    await nodes.client_node.MonitorEnter(o2, 200);
                }
                catch (RequestTimeoutException)
                {
                    timeout_caught = true;
                }
                RRAssert.IsTrue(timeout_caught);
                await nodes.client_node.MonitorExit(l1);

                await nodes.client_node.DisconnectService(r1);
                await nodes.client_node.DisconnectService(r2);
            }
            Console.WriteLine("TestServiceBroadcastMonitorTest.RunTestServiceBroadcastMonitorTest passed");
        }
    }
}
