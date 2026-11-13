#if !ROBOTRACONTEUR_H5
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public class WireMemberTests
    {
        // Regression test: setting OutValue must wake WaitOutValueValid()
        public static async Task TestWaitOutValueValid()
        {
            var server_id = new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451");
            var client_node = new RobotRaconteurNode();
            using (var client_node_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags))
            using (var server = new TestServiceImpl("server1", server_id))
            {
                var cancel = new CancellationTokenSource(10000);
                var c = (testobj)await client_node.ConnectService("rr+intra:///?nodeid=" + server_id.ToString("D") + "&service=test_service", cancel: cancel.Token);
                var w = await c.testwire2.Connect(cancel.Token);

                var sw = Stopwatch.StartNew();
                var wait_task = w.WaitOutValueValid(5000);
                await Task.Delay(100);
                w.OutValue = 10.0;
                RRAssert.IsTrue(await wait_task);
                RRAssert.IsTrue(sw.ElapsedMilliseconds < 2000);

                await client_node.DisconnectService(c);
            }
        }
    }
}
#endif
