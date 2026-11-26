using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSubTest
{
    public static class DefaultClientTest
    {
        public static async Task TestDefaultClientSkipsConnecting()
        {
            // TryGetDefaultClient must return a connected client even when the first entry is still connecting
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(30000);
            var client_node = new RobotRaconteurNode();
            var client_node_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags);
            using (client_node_setup)
            using (var server1 = new testservice_impl("server1", new NodeID("8e9f0a1b-2c3d-4e5f-a6b7-c8d9e0f1a2b3")))
            {
                var sub = client_node.SubscribeServiceByType(new string[] { "experimental.sub_test.testobj" });
                await sub.GetDefaultClientWait<testobj>(cancel.Token);

                // Put an entry that has no client yet (connecting) in front of the connected one
                var clients = (IDictionary)typeof(ServiceSubscription).GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sub);
                var client_type = typeof(ServiceSubscription).Assembly.GetType("RobotRaconteurSharp.ServiceSubscription_client");
                var connecting = Activator.CreateInstance(client_type, true);
                var connecting_key = new ServiceSubscriptionClientID(new NodeID("00000000-0000-0000-0000-0000000000aa"), "connecting");
                lock (sub)
                {
                    var entries = clients.Keys.Cast<object>().Select(k => Tuple.Create(k, clients[k])).ToList();
                    clients.Clear();
                    clients.Add(connecting_key, connecting);
                    foreach (var e in entries)
                        clients.Add(e.Item1, e.Item2);
                }

                RRAssert.IsTrue(sub.TryGetDefaultClient<testobj>(out var c));
                RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);
                RRAssert.AreEqual(await sub.GetDefaultClient<testobj>().add_two_numbers(2, 3), 5);

                lock (sub)
                {
                    clients.Remove(connecting_key);
                }
                sub.Close();
            }
            Console.WriteLine("DefaultClientTest.TestDefaultClientSkipsConnecting passed");
        }
    }
}
