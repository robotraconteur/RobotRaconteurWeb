using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: FindServiceByType must charge a failed query to the node it was sent to.
    // A node without a URL for the requested scheme is not queried and must stay in the cache.
    public static class FindServiceByTypeIndexTests
    {
        static string Packet(NodeID id, string name, string url)
        {
            return "Robot Raconteur Node Discovery Packet\n" + id.ToString() + "," + name + "\n" + url + "\n";
        }

        public static async Task RunTestFindServiceByTypeSkippedNode()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var ws_only = NodeID.NewUniqueID();
                var tcp_node = NodeID.NewUniqueID();
                node.NodeAnnouncePacketReceived(Packet(ws_only, "ws_only_node",
                    "rr+ws://127.0.0.1:48653/?nodeid=" + ws_only.ToString("D") + "&service=RobotRaconteurServiceIndex"));
                node.NodeAnnouncePacketReceived(Packet(tcp_node, "tcp_node",
                    "rr+tcp://127.0.0.1:48653/?nodeid=" + tcp_node.ToString("D") + "&service=RobotRaconteurServiceIndex"));
                RRAssert.IsTrue(node.DiscoveredNodes.ContainsKey(ws_only.ToString()));
                RRAssert.IsTrue(node.DiscoveredNodes.ContainsKey(tcp_node.ToString()));

                // The node has no transports, so the query to tcp_node fails
                var r = await node.FindServiceByType("com.robotraconteur.testing.TestService1.testroot", new string[] { "rr+tcp" });
                RRAssert.AreEqual(r.Length, 0);

                RRAssert.IsTrue(node.DiscoveredNodes.ContainsKey(ws_only.ToString()));
                RRAssert.IsFalse(node.DiscoveredNodes.ContainsKey(tcp_node.ToString()));
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
