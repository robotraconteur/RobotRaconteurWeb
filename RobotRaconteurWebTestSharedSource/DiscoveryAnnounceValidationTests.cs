using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Node announce packets are validated before a first-seen node is stored
    public static class DiscoveryAnnounceValidationTests
    {
        static string Packet(NodeID id, string name, string url)
        {
            return "Robot Raconteur Node Discovery Packet\n" + id.ToString() + "," + name + "\n" + url + "\n";
        }

        static string Url(NodeID id)
        {
            return "rr+tcp://127.0.0.1:48653/?nodeid=" + id.ToString("D") + "&service=RobotRaconteurServiceIndex";
        }

        public static void RunTestAnnounceValidation()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var good = NodeID.NewUniqueID();
                node.NodeAnnouncePacketReceived(Packet(good, "good_node", Url(good)));
                RRAssert.IsTrue(node.DiscoveredNodes.ContainsKey(good.ToString()));

                // URL names another NodeID
                var bad_id = NodeID.NewUniqueID();
                node.NodeAnnouncePacketReceived(Packet(bad_id, "bad_node", Url(NodeID.NewUniqueID())));
                RRAssert.IsFalse(node.DiscoveredNodes.ContainsKey(bad_id.ToString()));

                // URL too long
                var long_url = NodeID.NewUniqueID();
                node.NodeAnnouncePacketReceived(Packet(long_url, "long_url", Url(long_url) + "&x=" + new string('a', 300)));
                RRAssert.IsFalse(node.DiscoveredNodes.ContainsKey(long_url.ToString()));

                // NodeName too long
                var long_name = NodeID.NewUniqueID();
                node.NodeAnnouncePacketReceived(Packet(long_name, new string('n', 200), Url(long_name)));
                RRAssert.IsFalse(node.DiscoveredNodes.ContainsKey(long_name.ToString()));
            }
            finally
            {
                node.Shutdown();
            }
        }

        public static void RunTestAnnounceLoopbackSender()
        {
            // A loopback URL must only be accepted from a loopback sender
            var t = typeof(TcpTransport).Assembly.GetType("RobotRaconteurWeb.IPNodeDiscovery");
            var m = t.GetMethod("NodeAnnounceAddressValid", BindingFlags.NonPublic | BindingFlags.Static);
            RRAssert.IsTrue(m != null);
            Func<string, string, bool> valid = (a, s) => (bool)m.Invoke(null, new object[] { IPAddress.Parse(a), IPAddress.Parse(s) });

            RRAssert.IsFalse(valid("127.0.0.1", "192.168.1.5"));
            RRAssert.IsFalse(valid("::1", "fe80::1"));
            RRAssert.IsFalse(valid("::ffff:127.0.0.1", "192.168.1.5"));
            RRAssert.IsTrue(valid("127.0.0.1", "127.0.0.1"));
            RRAssert.IsTrue(valid("::1", "::1"));
            RRAssert.IsTrue(valid("192.168.1.7", "192.168.1.5"));
            RRAssert.IsFalse(valid("10.0.0.1", "192.168.1.5"));
        }
    }
}
