using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.subobject_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;


namespace RobotRaconteurSubTest.SubObject
{
    public class testobj3_impl : testobj3_default_impl, IRRServiceObject
    {
        string index;
        string service_path;
        public testobj3_impl(string index = null)
        {
            this.index = index;
        }

        public override Task<double> add_two_numbers(double a, double b, CancellationToken rr_cancel = default)
        {
            return Task.FromResult(a + b);
        }

        public override Task<string> getf_service_path(CancellationToken rr_cancel = default)
        {
            return Task.FromResult(service_path);
        }

        public void RRServiceObjectInit(ServerContext context, string servicePath)
        {
            this.service_path = servicePath;
        }
    }

    public class testobj2_impl : testobj2_default_impl, IRRServiceObject
    {
        string service_path = null;

        public void RRServiceObjectInit(ServerContext context, string servicePath)
        {
            this.service_path = servicePath;
        }

        public override Task<string> getf_service_path(CancellationToken rr_cancel = default)
        {
            return Task.FromResult(service_path);
        }

        public override Task<testobj3> get_subobj3_1(CancellationToken cancel = default)
        {
            return Task.FromResult<testobj3>(new testobj3_impl());
        }

        public override Task<testobj3> get_subobj3_2(int ind, CancellationToken cancel = default)
        {
            return Task.FromResult<testobj3>(new testobj3_impl(ind.ToString()));
        }

        public override Task<testobj3> get_subobj3_3(string ind, CancellationToken cancel = default)
        {
            return Task.FromResult<testobj3>(new testobj3_impl(ind));
        }
    }

    public class testobj_impl : testobj_default_impl, IRRServiceObject
    {
        public string service_path = null;

        public void RRServiceObjectInit(ServerContext context, string servicePath)
        {
            service_path = servicePath;
        }

        public override Task<string> getf_service_path(CancellationToken rr_cancel = default)
        {
            return Task.FromResult(service_path);
        }

        public override Task<testobj2> get_subobj2(CancellationToken cancel = default)
        {
            return Task.FromResult<testobj2>(new testobj2_impl());
        }
    }

    public class testservice_impl : IDisposable
    {


        public const RobotRaconteurNodeSetupFlags intra_server_flags = RobotRaconteurNodeSetupFlags.EnableIntraTransport
            | RobotRaconteurNodeSetupFlags.IntraTransportStartServer
            | RobotRaconteurNodeSetupFlags.EnableNodeAnnounce
            | RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening;

        public const RobotRaconteurNodeSetupFlags intra_client_flags = RobotRaconteurNodeSetupFlags.EnableIntraTransport
            | RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening;


        testobj_impl _obj;
        RobotRaconteurNode node;
        RobotRaconteurNodeSetup node_setup;

        public testservice_impl(string nodename, NodeID nodeid)
        {
            _obj = new testobj_impl();
            node = new RobotRaconteurNode();
            node.NodeID = nodeid;

            var service_types = new ServiceFactory[] { new experimental__subobject_sub_testFactory() };

            node_setup = new RobotRaconteurNodeSetup(node, service_types, false, nodename, 0, intra_server_flags);
            node.RegisterService("test_service", "experimental.subobject_sub_test", _obj);
        }

        public void Dispose()
        {
            node_setup?.Dispose();
        }
    }

    public class SubObjectSubscriptionTests
    {
        public static async Task RunTestSubscribeSubObject()
        {
            var test_servers = new Dictionary<string, NodeID>()
            {
                { "server1", new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451") },
                { "server2", new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") },
                { "server3", new NodeID("cb71939a-6c6c-43cc-b6be-070a76acec74") }
            };

            var client_node = new RobotRaconteurNode();
            var service_types = new ServiceFactory[] { new experimental__subobject_sub_testFactory() };

            var client_node_setup = new RobotRaconteurNodeSetup(client_node, service_types, false, null, 0, testservice_impl.intra_client_flags);

            var server1 = new testservice_impl("server1", test_servers["server1"]);

            using (client_node_setup)
            using (server1)
            {
                var sub = client_node.SubscribeServiceByType(new string[] { "experimental.subobject_sub_test.testobj" });
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(TimeSpan.FromSeconds(10));
                var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);

                RRAssert.AreEqual(await c.getf_service_path(), "test_service");

                var sub2 = sub.SubscribeSubObject("*.subobj2");
                var c2 = await sub2.GetDefaultClient<testobj2>();
                RRAssert.AreEqual(await c2.getf_service_path(), "test_service.subobj2");

                var cancel3 = new CancellationTokenSource();
                cancel3.CancelAfter(1000);
                var c3 = await sub2.GetDefaultClientWait<testobj2>(cancel3.Token);
                RRAssert.AreEqual(await c3.getf_service_path(), "test_service.subobj2");

                var c4 = await sub2.TryGetDefaultClient<testobj2>();
                RRAssert.IsTrue(c4.Item1);
                RRAssert.AreEqual(await c4.Item2.getf_service_path(), "test_service.subobj2");

                var cancel4 = new CancellationTokenSource();
                cancel4.CancelAfter(1000);
                var c5 = await sub2.TryGetDefaultClientWait<testobj2>(cancel3.Token);
                RRAssert.IsTrue(c5.Item1);
                RRAssert.AreEqual(await c5.Item2.getf_service_path(), "test_service.subobj2");

                var sub3 = sub.SubscribeSubObject("*.subobj2.subobj3_1");
                var c6 = await sub3.GetDefaultClient<testobj3>();
                RRAssert.AreEqual(await c6.getf_service_path(), "test_service.subobj2.subobj3_1");

                var sub4 = sub.SubscribeSubObject("*.subobj2.subobj3_2[123]");
                var c7 = await sub4.GetDefaultClient<testobj3>();
                RRAssert.AreEqual(await c7.getf_service_path(), "test_service.subobj2.subobj3_2[123]");

                var sub5 = sub.SubscribeSubObject("*.subobj2.subobj3_3[someobj]");
                var c8 = await sub5.GetDefaultClient<testobj3>();
                RRAssert.AreEqual(await c8.getf_service_path(), "test_service.subobj2.subobj3_3[someobj]");
            }
        }
    }
}
