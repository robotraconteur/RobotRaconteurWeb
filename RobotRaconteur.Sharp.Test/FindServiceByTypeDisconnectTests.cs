using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSubTest
{
    // Regression test: FindServiceByType must disconnect from the service index of each node it queries
    public class FindServiceByTypeDisconnectTests
    {
        static int ClientEndpointCount(RobotRaconteurNode node)
        {
            var f = typeof(RobotRaconteurNode).GetField("endpoints", BindingFlags.NonPublic | BindingFlags.Instance);
            var d = (ICollection)f.GetValue(node);
            lock (d)
            {
                return d.Count;
            }
        }

        public static async Task RunTestFindServiceByTypeDisconnects()
        {
            var server_id = new NodeID("3c2a51a2-8bd0-4c43-a2b3-6a0c1f7e5d10");

            var client_node = new RobotRaconteurNode();
            var client_node_setup = new RobotRaconteurNodeSetup(client_node, new ServiceFactory[] { new experimental__sub_testFactory() },
                false, null, 0, testservice_impl.intra_client_flags);

            var server = new testservice_impl("find_disconnect_server", server_id);

            using (client_node_setup)
            using (server)
            {
                for (int i = 0; i < 50 && !client_node.DiscoveredNodes.ContainsKey(server_id.ToString()); i++)
                {
                    await Task.Delay(100);
                }
                RRAssert.IsTrue(client_node.DiscoveredNodes.ContainsKey(server_id.ToString()));

                for (int i = 0; i < 3; i++)
                {
                    var r = await client_node.FindServiceByType("experimental.sub_test.testobj", new string[] { "rr+intra" });
                    RRAssert.IsTrue(r.Any(x => x.NodeID == server_id));
                }

                int count = ClientEndpointCount(client_node);
                for (int i = 0; i < 50 && count != 0; i++)
                {
                    await Task.Delay(100);
                    count = ClientEndpointCount(client_node);
                }
                RRAssert.AreEqual(count, 0);
            }
        }
    }
}
