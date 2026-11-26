using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
{
    // Regression test: IntraTransport.GetDetectedNodes must report the intra server nodes, so that
    // FindServiceByType / FindNodeByID refresh them after the 60 s discovery expiry
    public class IntraDetectedNodesTests
    {
        public static async Task RunTestIntraGetDetectedNodes()
        {
            var server_id = new NodeID("7d1e0a52-3f6b-4b8e-9c21-5a4f0e6b2c31");

            var client_node = new RobotRaconteurNode();
            var client_transport = new IntraTransport(client_node);
            client_node.RegisterTransport(client_transport);
            client_transport.StartClient();

            var server = new testservice_impl("intra_detected_server", server_id);
            try
            {
                var n = await client_transport.GetDetectedNodes(default(CancellationToken));
                var e = n.FirstOrDefault(x => x.NodeID == server_id);
                RRAssert.IsTrue(e != null);
                RRAssert.AreEqual(e.NodeName, "intra_detected_server");
                RRAssert.IsTrue(e.URLs.Count == 1 && e.URLs[0].URL.StartsWith("rr+intra:///?nodeid="));
                RRAssert.IsFalse(n.Any(x => x.NodeID == client_node.NodeID));
            }
            finally
            {
                server.Dispose();
                client_node.Shutdown();
            }
        }
    }
}
