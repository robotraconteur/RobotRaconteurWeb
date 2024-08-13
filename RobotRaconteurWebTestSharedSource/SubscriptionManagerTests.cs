using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Manager
{

    public class testobj_impl : testobj
    {
        public Task<double> add_two_numbers(double a, double b, CancellationToken rr_cancel = default)
        {
            return Task.FromResult(a + b);
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

            var service_types = new ServiceFactory[] { new experimental__sub_testFactory() };

            node_setup = new RobotRaconteurNodeSetup(node, service_types, false, nodename, 0, intra_server_flags);
            node.RegisterService("test_service", "experimental.sub_test", _obj);
        }

        public void Dispose()
        {
            node_setup?.Dispose();
        }
    }

    public class SubscriptionManagerTests
    {
        public static async Task RunTestSubManagerSubscribeByType()
        {
            var test_servers = new Dictionary<string, NodeID>()
            {
                { "server1", new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451") },
                { "server2", new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") },
                { "server3", new NodeID("cb71939a-6c6c-43cc-b6be-070a76acec74") }
            };

            var client_node = new RobotRaconteurNode();
            var service_types = new ServiceFactory[] { new experimental__sub_testFactory() };

            var client_node_setup = new RobotRaconteurNodeSetup(client_node, service_types, false, null, 0, testservice_impl.intra_client_flags);

            var server1 = new testservice_impl("server1", test_servers["server1"]);

            using (client_node_setup)
            using (server1)
            {
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(10000);
                var sub_manager = new ServiceSubscriptionManager(client_node);

                var sub1_details = new ServiceSubscriptionManagerDetails()
                {
                    Name = "sub1",
                    ServiceTypes = new string[] { "experimental.sub_test.testobj" }
                };
                sub_manager.AddSubscription(sub1_details);

                var sub = sub_manager.GetSubscription("sub1");
                var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);
                RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);

                sub_manager.DisableSubscription("sub1", true);
                await Task.Delay(100);
                bool res = sub.TryGetDefaultClient<object>(out var a);
                RRAssert.IsFalse(res);

                sub_manager.RemoveSubscription("sub1", true);
                await Task.Delay(100);
                res = sub.TryGetDefaultClient<object>(out var a2);
                RRAssert.IsFalse(res);

                RRAssert.ThrowsException<ArgumentException>(() =>
                {
                    sub_manager.GetSubscription("sub1");
                });

                sub1_details.Enabled = true;
                sub_manager.AddSubscription(sub1_details);

                var sub3 = sub_manager.GetSubscription("sub1");
                var c3 = await sub3.GetDefaultClientWait<testobj>(cancel.Token);
                RRAssert.AreEqual(await c3.add_two_numbers(1,2),3);

                Console.WriteLine(string.Join(",", sub_manager.SubscriptionNames));

                sub_manager.Close();

            }
        }

        public static async Task RunTestSubManagerSubscribeByUrl()
        {
            var test_servers = new Dictionary<string, NodeID>()
            {
                { "server1", new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451") },
                { "server2", new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") },
                { "server3", new NodeID("cb71939a-6c6c-43cc-b6be-070a76acec74") }
            };

            var client_node = new RobotRaconteurNode();
            var service_types = new ServiceFactory[] { new experimental__sub_testFactory() };

            var client_node_setup = new RobotRaconteurNodeSetup(client_node, service_types, false, null, 0, testservice_impl.intra_client_flags);

            var server1 = new testservice_impl("server1", test_servers["server1"]);

            using (client_node_setup)
            using (server1)
            {
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(10000);
                var sub_manager = new ServiceSubscriptionManager(client_node);

                var sub1_details = new ServiceSubscriptionManagerDetails()
                {
                    Name = "sub1",
                    Urls = new string[] { "rr+intra:///?nodename=server1&service=test_service" }
                };
                sub_manager.AddSubscription(sub1_details);

                var sub = sub_manager.GetSubscription("sub1");
                var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);
                RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);

                sub_manager.DisableSubscription("sub1", true);
                await Task.Delay(100);
                bool res = sub.TryGetDefaultClient<object>(out var a);
                RRAssert.IsFalse(res);

                sub_manager.RemoveSubscription("sub1", true);
                await Task.Delay(100);
                res = sub.TryGetDefaultClient<object>(out var a2);
                RRAssert.IsFalse(res);

                RRAssert.ThrowsException<ArgumentException>(() =>
                {
                    sub_manager.GetSubscription("sub1");
                });

                sub1_details.Enabled = true;
                sub_manager.AddSubscription(sub1_details);

                var sub3 = sub_manager.GetSubscription("sub1");
                var c3 = await sub3.GetDefaultClientWait<testobj>(cancel.Token);
                RRAssert.AreEqual(await c3.add_two_numbers(1, 2), 3);

                Console.WriteLine(string.Join(",", sub_manager.SubscriptionNames));

                sub_manager.Close();

            }
        }
    }
}