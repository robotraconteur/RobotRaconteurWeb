#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public class WireSubscriptionServicePathTests
    {
        // Regression test: a "*." service path must be resolved per client, so services with
        // different service names are all connected
        public static async Task TestWildcardServicePathMultipleServiceNames()
        {
            var ids = new NodeID[] { new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451"), new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") };
            var names = new string[] { "test_service_a", "test_service_b" };
            var setups = new RobotRaconteurNodeSetup[2];
            var client_node = new RobotRaconteurNode();
            var client_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    var n = new RobotRaconteurNode();
                    n.NodeID = ids[i];
                    setups[i] = new RobotRaconteurNodeSetup(n, null, true, "server" + i, 0, testservice_impl.intra_server_flags);
                    n.RegisterService(names[i], "experimental.wire_sub_test", new testobj_impl());
                }

                var sub = client_node.SubscribeServiceByType(new string[] { "experimental.wire_sub_test.testobj" });
                var wire_sub = sub.SubscribeWire<double>("testwire3", "*.subobj");

                for (int i = 0; i < 50 && wire_sub.ActiveWireConnectionCount < 2; i++)
                {
                    await Task.Delay(100);
                }
                RRAssert.AreEqual<int>((int)wire_sub.ActiveWireConnectionCount, 2);
                sub.Close();
            }
            finally
            {
                client_setup.Dispose();
                foreach (var s in setups)
                    s?.Dispose();
            }
        }
    }
}
#endif
