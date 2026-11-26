using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest
{
    public class ConnectServiceTests
    {
        // Regression test: ConnectService with an array of URLs and a listener must return the
        // client and attach the listener
        public static async Task TestConnectServiceUrlsWithListener()
        {
            var server_id = new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451");
            var client_node = new RobotRaconteurNode();
            using (var client_node_setup = new RobotRaconteurNodeSetup(client_node, new ServiceFactory[] { new experimental__sub_testFactory() },
                false, null, 0, testservice_impl.intra_client_flags))
            using (var server = new testservice_impl("server1", server_id))
            {
                var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                ClientContext.ClientServiceListenerDelegate listener = (ctx, ev, p) =>
                {
                    if (ev == ClientServiceListenerEventType.ClientClosed)
                        closed.TrySetResult(true);
                };

                var cancel = new CancellationTokenSource(10000);
                var urls = new string[] { "rr+intra:///?nodeid=" + server_id.ToString("D") + "&service=test_service" };
                var c = (testobj)await client_node.ConnectService(urls, listener: listener, cancel: cancel.Token);
                RRAssert.AreEqual(await c.add_two_numbers(1, 2), 3);

                await client_node.DisconnectService(c);
                RRAssert.IsTrue(await Task.WhenAny(closed.Task, Task.Delay(5000)) == closed.Task);
            }
        }
    }
}
