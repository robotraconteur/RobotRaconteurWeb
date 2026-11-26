using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // Regression test: LocalTransport discovery listening must detect a local server started
    // after listening was enabled (the info files are in subdirectories of the watched paths)
    public static class LocalDiscoveryWatchTests
    {
        public static async Task RunTestLocalDiscoveryDetectsNewServer()
        {
            var client_node = new RobotRaconteurNode();
            var server_node = new RobotRaconteurNode();
            try
            {
                var client_transport = new LocalTransport(client_node);
                if (!client_transport.IsLocalTransportSupported)
                {
                    return;
                }
                client_node.RegisterTransport(client_transport);
                client_transport.EnableNodeDiscoveryListening();

                await Task.Delay(500);

                var server_transport = new LocalTransport(server_node);
                server_transport.StartServerAsNodeName("local_discovery_watch_server");
                server_node.RegisterTransport(server_transport);
                var server_id = server_node.NodeID.ToString();

                for (int i = 0; i < 100 && !client_node.DiscoveredNodes.ContainsKey(server_id); i++)
                {
                    await Task.Delay(100);
                }
                RRAssert.IsTrue(client_node.DiscoveredNodes.ContainsKey(server_id));
            }
            finally
            {
                server_node.Shutdown();
                client_node.Shutdown();
            }
        }
    }
}
