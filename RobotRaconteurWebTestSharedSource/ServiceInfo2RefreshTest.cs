using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
{
    public static class ServiceInfo2RefreshTest
    {
#if !ROBOTRACONTEUR_H5
        public static async Task TestServiceInfo2RefreshSameUrls()
        {
            // A refresh of a node's service list with the same contents (new arrays) must not report the
            // service as changed (ServiceDetected again)
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(30000);
            var client_node = new RobotRaconteurNode();
            var client_node_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags);
            using (client_node_setup)
            using (var server1 = new testservice_impl("server1", new NodeID("2b3c4d5e-6f70-4182-93a4-b5c6d7e8f901")))
            {
                var sub = client_node.SubscribeServiceInfo2(new string[] { "experimental.sub_test.testobj" });
                var sw = Stopwatch.StartNew();
                while (sub.GetDetectedServiceInfo2().Count == 0 && sw.ElapsedMilliseconds < 10000)
                {
                    await Task.Delay(10);
                }
                RRAssert.AreEqual(sub.GetDetectedServiceInfo2().Count, 1);

                var asm = typeof(RobotRaconteurNode).Assembly;
                var disc = typeof(RobotRaconteurNode).GetField("m_Discovery", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client_node);
                var nodes = (IDictionary)disc.GetType().GetField("m_DiscoveredNodes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(disc);
                object storage = null;
                lock (nodes)
                {
                    foreach (var v in nodes.Values)
                    {
                        var services = (ServiceInfo2[])v.GetType().GetField("services").GetValue(v);
                        if (services != null && services.Any(x => x.RootObjectType == "experimental.sub_test.testobj"))
                            storage = v;
                    }
                }
                RRAssert.IsTrue(storage != null);

                // Copy the service list with new (equal) ConnectionURL arrays, as a refresh does
                var services_field = storage.GetType().GetField("services");
                var old_services = (ServiceInfo2[])services_field.GetValue(storage);
                services_field.SetValue(storage, old_services.Select(x => new ServiceInfo2()
                {
                    Name = x.Name,
                    RootObjectType = x.RootObjectType,
                    RootObjectImplements = x.RootObjectImplements?.ToArray(),
                    ConnectionURL = x.ConnectionURL?.ToArray(),
                    Attributes = x.Attributes,
                    NodeID = x.NodeID,
                    NodeName = x.NodeName
                }).ToArray());

                var clients = (IDictionary)typeof(ServiceInfo2Subscription).GetField("clients", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sub);
                object client;
                lock (sub)
                {
                    client = clients.Values.Cast<object>().First();
                }
                var info_field = client.GetType().GetField("service_info2", BindingFlags.NonPublic | BindingFlags.Instance);
                var info_before = info_field.GetValue(client);

                var node_updated = asm.GetType("RobotRaconteurWeb.IServiceSubscription").GetMethod("NodeUpdated");
                node_updated.Invoke(sub, new object[] { storage });

                // The stored info is replaced only when the service changed
                RRAssert.IsTrue(ReferenceEquals(info_field.GetValue(client), info_before));
                sub.Close();
            }
            Console.WriteLine("ServiceInfo2RefreshTest.TestServiceInfo2RefreshSameUrls passed");
        }
#endif
    }
}
