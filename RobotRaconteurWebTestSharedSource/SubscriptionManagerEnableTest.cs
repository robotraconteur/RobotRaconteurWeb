#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Manager
{
    public static class SubscriptionManagerEnableTest
    {
        public static async Task TestEnableDisabledByType()
        {
            // A manager subscription created disabled and enabled later must connect to services that appear afterwards
            var client_node = new RobotRaconteurNode();
            var service_types = new ServiceFactory[] { new experimental__sub_testFactory() };
            var client_node_setup = new RobotRaconteurNodeSetup(client_node, service_types, false, null, 0, testservice_impl.intra_client_flags);
            using (client_node_setup)
            {
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(10000);
                var sub_manager = new ServiceSubscriptionManager(client_node);
                var details = new ServiceSubscriptionManagerDetails()
                {
                    Name = "sub1",
                    ConnectionMethod = ServiceSubscriptionManagerConnectionMethod.Type,
                    ServiceTypes = new string[] { "experimental.sub_test.testobj" },
                    Enabled = false
                };
                sub_manager.AddSubscription(details);
                sub_manager.EnableSubscription("sub1");
                var sub = sub_manager.GetSubscription("sub1");

                using (var server1 = new testservice_impl("server1", new NodeID("5e6b1f0c-8a5d-4c1e-9b4e-2f7d3c9a1b62")))
                {
                    var c = await sub.GetDefaultClientWait<testobj>(cancel.Token);
                    RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);
                }
                sub_manager.Close();
            }
            Console.WriteLine("SubscriptionManagerEnableTest.TestEnableDisabledByType passed");
        }
    }
}
#endif
