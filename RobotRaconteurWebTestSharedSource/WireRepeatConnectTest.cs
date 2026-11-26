using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public static class WireRepeatConnectTest
    {
        public static async Task TestWireRepeatConnect()
        {
            // A repeated WireConnectReq from a connected client must not register the connection again
            var cancel = new CancellationTokenSource(30000);
            var clientNode = new RobotRaconteurNode();
            var clientNodeSetup = new RobotRaconteurNodeSetup(clientNode, null, true, null, 0, testservice_impl.intra_client_flags);
            using (var server1 = new TestServiceImpl("server1", new NodeID("3a4f0d0e-58f1-4b33-9e3c-7b1b2f4f0c11")))
            using (clientNodeSetup)
            {
                var sub = clientNode.SubscribeServiceByType(new string[] { "experimental.wire_sub_test.testobj" });
                var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);
                var stub = (ServiceStub)c;

                // WireBroadcaster (testwire1)
                var w1 = await c.testwire1.Connect(cancel.Token);
                await stub.ProcessRequest(new MessageEntry(MessageEntryType.WireConnectReq, "testwire1"), cancel.Token);
                await stub.ProcessRequest(new MessageEntry(MessageEntryType.WireConnectReq, "testwire1"), cancel.Token);
                var connected_wires = (ICollection)typeof(WireBroadcaster<double>)
                    .GetField("connected_wires", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(server1.obj.TestWire1);
                RRAssert.AreEqual(connected_wires.Count, 1);

                // WireUnicastReceiver (testwire2): the repeated connect must not close or re-register the connection
                var w2 = await c.testwire2.Connect(cancel.Token);
                await stub.ProcessRequest(new MessageEntry(MessageEntryType.WireConnectReq, "testwire2"), cancel.Token);
                await stub.ProcessRequest(new MessageEntry(MessageEntryType.WireConnectReq, "testwire2"), cancel.Token);
                var active = typeof(WireUnicastReceiver<double>)
                    .GetField("active_connection", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(server1.obj.TestWire2);
                RRAssert.IsTrue(active != null);
                var con = (Wire<double>.WireConnection)active.GetType().GetField("connection").GetValue(active);
                var handlers = (Delegate)typeof(Wire<double>.WireConnection)
                    .GetField("WireValueChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(con);
                RRAssert.AreEqual(handlers.GetInvocationList().Length, 1);

                sub.Close();
            }
            Console.WriteLine("WireRepeatConnectTest.TestWireRepeatConnect passed");
        }
    }
}
