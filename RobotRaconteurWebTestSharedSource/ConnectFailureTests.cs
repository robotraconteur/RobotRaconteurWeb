using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
{
    public class ConnectFailureTests
    {
        static int EndpointCount(RobotRaconteurNode node)
        {
            var f = typeof(RobotRaconteurNode).GetField("endpoints", BindingFlags.NonPublic | BindingFlags.Instance);
            var d = (ICollection)f.GetValue(node);
            lock (d)
            {
                return d.Count;
            }
        }

        // Regression test: a failed ConnectService must not leave the client endpoint registered
        public static async Task TestFailedConnectDoesNotLeakEndpoint()
        {
            var client_node = new RobotRaconteurNode();
            using (var client_setup = new RobotRaconteurNodeSetup(client_node, new ServiceFactory[] { new experimental__sub_testFactory() },
                false, null, 0, testservice_impl.intra_client_flags))
            {
                int before = EndpointCount(client_node);
                for (int i = 0; i < 5; i++)
                {
                    bool failed = false;
                    try
                    {
                        var cancel = new CancellationTokenSource(5000);
                        await client_node.ConnectService("rr+intra:///?nodeid=12345678-1234-1234-1234-123456789abc&service=test_service", cancel: cancel.Token);
                    }
                    catch (Exception)
                    {
                        failed = true;
                    }
                    RRAssert.IsTrue(failed);
                }
                RRAssert.AreEqual(EndpointCount(client_node), before);
            }
        }
    }
}
