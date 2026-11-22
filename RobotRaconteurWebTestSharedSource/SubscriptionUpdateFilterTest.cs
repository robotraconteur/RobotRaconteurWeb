using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public static class SubscriptionUpdateFilterTest
    {
#if !ROBOTRACONTEUR_H5
        public static async Task TestUpdateServiceByTypeDisconnectsFiltered()
        {
            // UpdateServiceByType with a filter that excludes a connected service must disconnect it
            var cancel = new CancellationTokenSource(30000);
            var clientNode = new RobotRaconteurNode();
            var clientNodeSetup = new RobotRaconteurNodeSetup(clientNode, null, true, null, 0, testservice_impl.intra_client_flags);
            using (var server1 = new TestServiceImpl("server1", new NodeID("9d2e64a1-6f43-4a77-a2f7-0f0a8d1c5b33")))
            using (clientNodeSetup)
            {
                var types = new string[] { "experimental.wire_sub_test.testobj" };
                var sub = clientNode.SubscribeServiceByType(types);
                await sub.GetDefaultClientWait<object>(cancel.Token);
                RRAssert.AreEqual(sub.GetConnectedClients().Count, 1);

                var filter = new ServiceSubscriptionFilter();
                var n = new ServiceSubscriptionFilterNode();
                n.NodeName = "no_such_node";
                filter.Nodes = new ServiceSubscriptionFilterNode[] { n };
                sub.UpdateServiceByType(types, filter);

                var sw = Stopwatch.StartNew();
                while (sub.GetConnectedClients().Count != 0 && sw.ElapsedMilliseconds < 10000)
                {
                    await Task.Delay(10);
                }
                RRAssert.AreEqual(sub.GetConnectedClients().Count, 0);
                sub.Close();
            }
            Console.WriteLine("SubscriptionUpdateFilterTest.TestUpdateServiceByTypeDisconnectsFiltered passed");
        }
#endif
    }
}
