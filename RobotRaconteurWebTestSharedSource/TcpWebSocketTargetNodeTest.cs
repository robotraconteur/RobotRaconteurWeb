#if !ROBOTRACONTEUR_H5
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class TcpWebSocketTargetNodeTest
    {

        // Regression test: a WebSocket connect must check the NodeID in the URL against the node that
        // answers, as the plain TCP connect does. Previously ConnectWebsocketTransport passed no target to
        // ConnectStream, so the stream connection to the wrong node was set up and the connect only failed
        // later at the node level ("Could not find route to remote node").
        public static async Task TestWebSocketConnectChecksNodeID()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                // IPv4 and IPv6 listeners have different ports; use an IPv4 one
                var tcp_url = System.Linq.Enumerable.First(nodes.GetServiceUrl("RobotRaconteurTestService", "rr+tcp"), x => !x.Contains("["));
                var host_port = System.Text.RegularExpressions.Regex.Match(tcp_url, "://([^/]+)/").Groups[1].Value;
                string url_base = "rr+ws://" + host_port + "/?nodeid=";
                var cancel = new CancellationTokenSource(30000);

                var good_url = url_base + nodes.node.NodeID.ToString("D") + "&service=RobotRaconteurTestService";
                var r = await nodes.client_node.ConnectService(good_url, cancel: cancel.Token);
                await nodes.client_node.DisconnectService(r);

                var bad_url = url_base + NodeID.NewUniqueID().ToString("D") + "&service=RobotRaconteurTestService";
                Exception err = null;
                try
                {
                    var r2 = await nodes.client_node.ConnectService(bad_url, cancel: cancel.Token);
                    await nodes.client_node.DisconnectService(r2);
                }
                catch (Exception e)
                {
                    err = e;
                }
                RRAssert.IsTrue(err != null);
                Console.WriteLine("Wrong NodeID connect error: " + err.GetType().Name + ": " + err.Message);
                // The stream handshake (CreateConnection) rejects the node: the server answers "Node not found",
                // or (server accepting any NodeID, see asyncstream-nodematch-precedence) the client finds
                // that the answering node is not the requested one
                RRAssert.IsTrue((err is NodeNotFoundException && err.Message.Contains("Node not found"))
                    || (err is ConnectionException && err.Message.Contains("Invalid node connection")));
            }
            Console.WriteLine("TcpWebSocketTargetNodeTest.TestWebSocketConnectChecksNodeID passed");
        }
    }
}
#endif
