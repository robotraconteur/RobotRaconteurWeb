using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
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

    public class SubscriptionTests
    {
        public static async Task RunTestSubscribeByType()
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
                var sub = client_node.SubscribeServiceByType(new string[] { "experimental.sub_test.testobj" });
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(TimeSpan.FromSeconds(10));
                var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);
                RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);
                var c2 = sub.GetDefaultClient<testobj>();
                RRAssert.AreEqual(await c.add_two_numbers(3, 2), 5);

                var connectCalledTcs = new TaskCompletionSource<bool>();
                var disconnectCalledTcs = new TaskCompletionSource<bool>();

                sub.ClientConnected += (subscription, client_id, client) =>
                {
                    RRAssert.AreEqual(client_id.NodeID, test_servers["server2"]);
                    RRAssert.AreEqual(client_id.ServiceName, "test_service");
                    connectCalledTcs.SetResult(true);
                };

                sub.ClientDisconnected += (subscription, client_id, client) =>
                {
                    if ((client_id.NodeID == test_servers["server2"]) &&
                    (client_id.ServiceName == "test_service"))
                    {
                        disconnectCalledTcs.SetResult(true);
                    }
                };

                using (var server2 = new testservice_impl("server2", test_servers["server2"]))
                {
                    var connectCalled = await Task.WhenAny(connectCalledTcs.Task, Task.Delay(5000)) == connectCalledTcs.Task;
                    RRAssert.IsTrue(connectCalled);
                }

                var disconnectCalled = await Task.WhenAny(disconnectCalledTcs.Task, Task.Delay(5000)) == disconnectCalledTcs.Task;
                RRAssert.IsTrue(disconnectCalled);
            }
        }

        public static async Task RunTestSubscribeByUrl()
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

            using (client_node_setup)
            {
                var sub = client_node.SubscribeService(new string[] { "rr+intra:///?nodename=server2&service=test_service" });

                var connectCalledTcs = new TaskCompletionSource<bool>();
                var disconnectCalledTcs = new TaskCompletionSource<bool>();

                sub.ClientConnected += (subscription, client_id, client) =>
                {
                    RRAssert.AreEqual(client_id.NodeID, test_servers["server2"]);
                    RRAssert.AreEqual(client_id.ServiceName, "test_service");
                    connectCalledTcs.SetResult(true);
                };

                sub.ClientDisconnected += (subscription, client_id, client) =>
                {
                    RRAssert.AreEqual(client_id.NodeID, test_servers["server2"]);
                    RRAssert.AreEqual(client_id.ServiceName, "test_service");
                    disconnectCalledTcs.SetResult(true);
                };

                using (var server2 = new testservice_impl("server2", test_servers["server2"]))
                {
                    var connectCalled = await Task.WhenAny(connectCalledTcs.Task, Task.Delay(5000)) == connectCalledTcs.Task;
                    RRAssert.IsTrue(connectCalled);

                    var c = sub.GetDefaultClient<testobj>();
                    RRAssert.AreEqual(await c.add_two_numbers(3, 2), 5);
                }

                var disconnectCalled = await Task.WhenAny(disconnectCalledTcs.Task, Task.Delay(5000)) == disconnectCalledTcs.Task;
                RRAssert.IsTrue(disconnectCalled);
            }

        }

        public static async Task RunTestSubscribeByUrlBadUrl()
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

            using (client_node_setup)
            {
                var connectErrCalledTcs = new TaskCompletionSource<bool>();

                

                // Pass an invalid URL and make sure error is called
                var sub = client_node.SubscribeService(new string[] { "rr+intra:///?nodename=server5&service=test_service" });
                Action<ServiceSubscription,ServiceSubscriptionClientID, string[], Exception> connectErrHandler =
                    delegate (ServiceSubscription sub2, ServiceSubscriptionClientID cid, string[] urls, Exception e)
                {
                    RRAssert.IsTrue(e!=null);
                    RRAssert.IsTrue(e is ConnectionException);
                    RRAssert.IsTrue(urls.Contains("rr+intra:///?nodename=server5&service=test_service"));
                    connectErrCalledTcs.SetResult(true);
                };

                sub.ClientConnectFailed += connectErrHandler;

                var connectErrCalled = await Task.WhenAny(connectErrCalledTcs.Task, Task.Delay(15000)) == connectErrCalledTcs.Task;
                RRAssert.IsTrue(connectErrCalled);

                sub.ClientConnectFailed -= connectErrHandler;
            }
        }

        public static async Task RunTestSubscribeServiceInfo2()
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

            using (client_node_setup)
            using (var server1 = new testservice_impl("server1", test_servers["server1"]))
            {
                var sub = client_node.SubscribeServiceInfo2(new string[] { "experimental.sub_test.testobj" });

                var detected = await Task.WhenAny(
                    Task.Delay(5000),
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            await Task.Delay(100);
                            if (sub.GetDetectedServiceInfo2().Count > 0)
                                return true;
                        }
                    })
                );

                if (detected == Task.Delay(5000))
                    throw new Exception("Timeout waiting for service info");

                var detected_nodes = sub.GetDetectedServiceInfo2();
                RRAssert.IsTrue(detected_nodes.Count >= 1);
                var service_info = detected_nodes[new ServiceSubscriptionClientID(test_servers["server1"], "test_service")];
                RRAssert.AreEqual(service_info.NodeID, test_servers["server1"]);
                RRAssert.AreEqual(service_info.Name, "test_service");
                RRAssert.AreEqual(service_info.RootObjectType, "experimental.sub_test.testobj");

                var connectCalledTcs = new TaskCompletionSource<bool>();

                Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> connectHandler =
                    delegate(ServiceInfo2Subscription sub2, ServiceSubscriptionClientID cid, ServiceInfo2 info)
                {
                    RRAssert.AreEqual(cid.NodeID, test_servers["server2"]);
                    RRAssert.AreEqual(cid.ServiceName, "test_service");
                    connectCalledTcs.SetResult(true);
                };

                sub.ServiceDetected += connectHandler;

                using (var server2 = new testservice_impl("server2", test_servers["server2"]))
                {
                    var connectCalled = await Task.WhenAny(connectCalledTcs.Task, Task.Delay(5000)) == connectCalledTcs.Task;
                    RRAssert.IsTrue(connectCalled);
                }

                sub.ServiceDetected -= connectHandler;
            }
        }


    }
}
