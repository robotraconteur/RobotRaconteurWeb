#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public static class WireWaitValidTest
    {
        // WaitInValueValid / WaitOutValueValid must return at once when the value is already valid,
        // not wait for the next value
        public static async Task TestWaitValueValidAlreadyValid()
        {
            var server_id = new NodeID("6b0c3f3e-2d2b-4d6a-8f57-3a51b0f2e9a4");
            var client_node = new RobotRaconteurNode();
            using (var client_node_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags))
            using (var server = new TestServiceImpl("server1", server_id))
            {
                var cancel = new CancellationTokenSource(10000);
                var c = (testobj)await client_node.ConnectService("rr+intra:///?nodeid=" + server_id.ToString("D") + "&service=test_service", cancel: cancel.Token);

                // Client InValue (WireConnection)
                var w1 = await c.testwire1.Connect(cancel.Token);
                server.obj.TestWire1.OutValue = 5.0;
                RRAssert.IsTrue(await w1.WaitInValueValid(-1, cancel.Token));
                RRAssert.IsTrue(await w1.WaitInValueValid(-1, cancel.Token));

                // Client OutValue (WireConnection) and service InValue (WireUnicastReceiver)
                var w2 = await c.testwire2.Connect(cancel.Token);
                w2.OutValue = 6.0;
                RRAssert.IsTrue(await w2.WaitOutValueValid(-1, cancel.Token));
                RRAssert.IsTrue(await server.obj.TestWire2.WaitInValueValid(-1, cancel.Token));
                RRAssert.IsTrue(await server.obj.TestWire2.WaitInValueValid(-1, cancel.Token));

                await client_node.DisconnectService(c);
            }
            Console.WriteLine("WireWaitValidTest.TestWaitValueValidAlreadyValid passed");
        }
    }
}
#endif
