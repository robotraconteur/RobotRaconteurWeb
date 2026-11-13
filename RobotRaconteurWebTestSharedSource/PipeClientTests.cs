#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.pipe_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Pipes
{
    public class PipeClientTests
    {
        // Regression test: closing a client pipe endpoint must remove it, so the pipe can be
        // connected again
        public static async Task TestPipeReconnectAfterClose()
        {
            var server_id = new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451");
            var client_node = new RobotRaconteurNode();
            using (var client_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags))
            using (var server = new TestServiceImpl("server1", server_id))
            {
                var cancel = new CancellationTokenSource(10000);
                var c = (testobj)await client_node.ConnectService("rr+intra:///?nodeid=" + server_id.ToString("D") + "&service=test_service", cancel: cancel.Token);

                var ep1 = await c.testpipe2.Connect(-1, cancel.Token);
                await ep1.Close();

                bool reconnected = false;
                try
                {
                    var ep2 = await c.testpipe2.Connect(-1, cancel.Token);
                    reconnected = true;
                    await ep2.Close();
                }
                catch (Exception) { }
                RRAssert.IsTrue(reconnected);

                await client_node.DisconnectService(c);
            }
        }
    }
}
#endif
