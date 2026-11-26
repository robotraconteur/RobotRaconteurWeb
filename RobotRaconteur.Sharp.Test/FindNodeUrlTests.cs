using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: FindNodeByID / FindNodeByName must return URLs with "scheme://"
    public static class FindNodeUrlTests
    {
        public static async Task RunTestFindNodeUrls()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var id = NodeID.NewUniqueID();
                string url = "rr+tcp://127.0.0.1:48653/?nodeid=" + id.ToString("D") + "&service=RobotRaconteurServiceIndex";
                node.NodeAnnouncePacketReceived("Robot Raconteur Node Discovery Packet\n" + id.ToString() + ",find_node_url_test\n" + url + "\n");

                var expected = "rr+tcp://127.0.0.1:48653/?nodeid=" + id.ToString("D");

                var r1 = await node.FindNodeByID(id, new string[] { "rr+tcp" });
                RRAssert.AreEqual(r1.Length, 1);
                RRAssert.AreEqual(r1[0].ConnectionURL.Length, 1);
                RRAssert.AreEqual(r1[0].ConnectionURL[0], expected);

                var r2 = await node.FindNodeByName("find_node_url_test", new string[] { "rr+tcp" });
                RRAssert.AreEqual(r2.Length, 1);
                RRAssert.AreEqual(r2[0].ConnectionURL.Length, 1);
                RRAssert.AreEqual(r2[0].ConnectionURL[0], expected);
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
