using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    public class LocalTransportDiscoveryTests
    {
        // Regression test: LocalTransport.GetDetectedNodes must report the node name of local
        // servers started with a node name
        public static async Task TestGetDetectedNodesNodeName()
        {
            var server_node = new RobotRaconteurNode();
            var server_t = new LocalTransport(server_node);
            server_node.RegisterTransport(server_t);
            var client_node = new RobotRaconteurNode();
            var client_t = new LocalTransport(client_node);
            client_node.RegisterTransport(client_t);
            try
            {
                if (!server_t.IsLocalTransportSupported)
                    return;
                server_t.StartServerAsNodeName("bughunt_local_discovery_test");
                var nodes = await client_t.GetDetectedNodes(default(CancellationToken));
                var n = nodes.FirstOrDefault(x => x.NodeID == server_node.NodeID);
                RRAssert.IsTrue(n != null);
                RRAssert.AreEqual(n.NodeName, "bughunt_local_discovery_test");
            }
            finally
            {
                client_node.Shutdown();
                server_node.Shutdown();
            }
        }
    }
}
