using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public class testobj_impl : testobj_default_impl
    {
        public testobj2_impl subobj = new testobj2_impl();
        public List<double> recv_packets = new List<double>();

        public override Task<testobj2> get_subobj(CancellationToken cancel = default)
        {
            return Task.FromResult((testobj2)subobj);
        }

        public WireBroadcaster<double> TestWire1 => rrvar_testwire1;

        public WireUnicastReceiver<double> TestWire2 => rrvar_testwire2;


    }

    public class testobj2_impl : testobj2_default_impl
    {
        public WireBroadcaster<double> TestWire3 => rrvar_testwire3;
    }

    public class TestServiceImpl : IDisposable
    {
        public testobj_impl obj;
        private RobotRaconteurNode node;
        private RobotRaconteurNodeSetup nodeSetup;

        public TestServiceImpl(string nodeName, NodeID nodeId)
        {
            obj = new testobj_impl();
            node = new RobotRaconteurNode();
            node.NodeID = nodeId;

            nodeSetup = new RobotRaconteurNodeSetup(node, null, true, nodeName, 0, RobotRaconteurSubTest.testservice_impl.intra_server_flags);
            node.RegisterService("test_service", "experimental.wire_sub_test", obj);
        }

        public void Dispose()
        {
            nodeSetup?.Dispose();

            nodeSetup = null;
            node = null;
        }
    }

    public class SubscriptionWireTests
    {
        public static async Task TestWireSubscription()
        {
            var testServers = new Dictionary<string, NodeID>()
    {
        { "server1", new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451") },
        { "server2", new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") },
        { "server3", new NodeID("cb71939a-6c6c-43cc-b6be-070a76acec74") }
    };

            var cancel = new CancellationTokenSource(30000);

            var clientNode = new RobotRaconteurNode();

            var clientNodeSetup = new RobotRaconteurNodeSetup(clientNode, null, true, null, 0, testservice_impl.intra_client_flags);
            using (var server1 = new TestServiceImpl("server1", testServers["server1"]))
            {

                int valueChangedCount = 0;

                void ValueChangedFunc(WireSubscription<double> wireSub, double value, TimeSpec time)
                {
                    valueChangedCount++;
                }

                using (clientNodeSetup)
                {
                    var sub = clientNode.SubscribeServiceByType(new string[] { "experimental.wire_sub_test.testobj" });
                    var wireSub = sub.SubscribeWire<double>("testwire1");
                    wireSub.WireValueChanged += ValueChangedFunc;
                    await sub.GetDefaultClientWait<object>(cancel.Token);

                    await Task.Delay(500);

                    RRAssert.AreEqual<int>((int)wireSub.ActiveWireConnectionCount, 1);

                    try
                    {
                        var inValue = wireSub.InValue;
                        RRAssert.Fail("Expected ValueNotSetException");
                    }
                    catch (ValueNotSetException)
                    {
                    }

                    RRAssert.IsFalse(wireSub.TryGetInValue(out _));

                    server1.obj.TestWire1.OutValue = 5.0;
                    await Task.Delay(1);
                    server1.obj.TestWire1.OutValue = 5.0;

                    await wireSub.WaitInValueValid(1000);

                    RRAssert.AreEqual(wireSub.InValue, 5.0);
                    RRAssert.AreEqual(wireSub.TryGetInValue(out var value), true);
                    RRAssert.AreEqual(value, 5.0);

                    RRAssert.IsTrue(valueChangedCount > 0);

                    wireSub.InValueLifespan = 100;
                    RRAssert.AreEqual(wireSub.InValueLifespan, 100);

                    await Task.Delay(200);

                    RRAssert.IsFalse(wireSub.TryGetInValue(out _));

                    wireSub.InValueLifespan = 5000;
                    server1.obj.TestWire1.OutValue = 6.0;

                    await Task.Delay(150);

                    RRAssert.AreEqual(wireSub.InValue, 6.0);

                    var wireSub2 = sub.SubscribeWire<double>("testwire3", "*.subobj");

                    await Task.Delay(200);
                    RRAssert.AreEqual<int>((int)wireSub2.ActiveWireConnectionCount, 1);

                    server1.obj.subobj.TestWire3.OutValue = 12.345;

                    await wireSub2.WaitInValueValid(1000);
                    RRAssert.AreEqual(wireSub2.InValue, 12.345);

                    wireSub2.Close();

                    var wireSub3 = sub.SubscribeWire<double>("testwire2");
                    await Task.Delay(50);
                    RRAssert.AreEqual((int)wireSub3.ActiveWireConnectionCount, 1);
                    wireSub3.SetOutValueAll(17.0);

                    await Task.Delay(50);
                    RRAssert.AreEqual(server1.obj.TestWire2.GetInValue(out _, out _), 17.0);
                    var result = server1.obj.TestWire2.TryGetInValue(out var val1, out _, out _);
                    RRAssert.IsTrue(result && val1 == 17.0);
                    server1.obj.TestWire2.InValueLifespan = 100;
                    await Task.Delay(200);
                    RRAssert.IsFalse(server1.obj.TestWire2.TryGetInValue(out _, out _, out _));
                }
            }
        }

    }

}
