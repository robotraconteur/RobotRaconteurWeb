using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression tests: the ServiceStateNonce must reach discovery from TCP announce packets and
    // from LocalTransport info files
    public static class ServiceStateNonceTests
    {
        public static void RunTestAnnounceNonce()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var id = NodeID.NewUniqueID();
                string url = "rr+tcp://127.0.0.1:48653/?nodeid=" + id.ToString("D") + "&service=RobotRaconteurServiceIndex";
                node.NodeAnnouncePacketReceived("Robot Raconteur Node Discovery Packet\n" + id.ToString() + ",nonce_node\n"
                    + url + "\nServiceStateNonce: abcdef0123456789\n");
                RRAssert.IsTrue(node.DiscoveredNodes.ContainsKey(id.ToString()));
                RRAssert.AreEqual(node.DiscoveredNodes[id.ToString()].ServiceStateNonce, "abcdef0123456789");
            }
            finally
            {
                node.Shutdown();
            }
        }

        static async Task<string> GetLocalNonce(LocalTransport t, NodeID id)
        {
            var n = await t.GetDetectedNodes(default(CancellationToken));
            var e = n.FirstOrDefault(x => x.NodeID == id);
            RRAssert.IsTrue(e != null);
            return e.ServiceStateNonce;
        }

        public static async Task RunTestLocalInfoFileNonce()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var t = new LocalTransport(node);
                if (!t.IsLocalTransportSupported)
                {
                    return;
                }
                t.StartServerAsNodeName("service_state_nonce_test_node");
                node.RegisterTransport(t);

                node.UpdateServiceStateNonce();
                var nonce1 = node.ServiceStateNonce;
                RRAssert.IsFalse(string.IsNullOrEmpty(nonce1));
                RRAssert.AreEqual(await GetLocalNonce(t, node.NodeID), nonce1);

                node.UpdateServiceStateNonce();
                var nonce2 = node.ServiceStateNonce;
                RRAssert.AreNotEqual(nonce1, nonce2);
                RRAssert.AreEqual(await GetLocalNonce(t, node.NodeID), nonce2);
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
