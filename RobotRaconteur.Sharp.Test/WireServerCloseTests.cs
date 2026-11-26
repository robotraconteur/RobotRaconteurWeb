using System;
using System.Threading;
using System.Threading.Tasks;
using experimental.wire_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurSubTest.Wires
{
    public class wire_close_testobj : testobj
    {
        public TaskCompletionSource<Wire<double>.WireConnection> connected =
            new TaskCompletionSource<Wire<double>.WireConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
        Wire<double> w1;
        public Wire<double> testwire1
        {
            get { return w1; }
            set
            {
                w1 = value;
                w1.WireConnectCallback = (w, c) => connected.TrySetResult(c);
            }
        }
        public Wire<double> testwire2 { get; set; }
        public Task<testobj2> get_subobj(CancellationToken rr_cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }

    public class WireServerCloseTests
    {
        // Regression test: closing a wire connection from the service side must send WireClosed
        // to the client endpoint instead of throwing
        public static async Task TestServiceSideWireClose()
        {
            var server_id = new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451");
            var server_node = new RobotRaconteurNode();
            server_node.NodeID = server_id;
            var obj = new wire_close_testobj();
            var client_node = new RobotRaconteurNode();
            using (var server_setup = new RobotRaconteurNodeSetup(server_node, null, true, "server1", 0, testservice_impl.intra_server_flags))
            using (var client_setup = new RobotRaconteurNodeSetup(client_node, null, true, null, 0, testservice_impl.intra_client_flags))
            {
                server_node.RegisterService("test_service", "experimental.wire_sub_test", obj);
                var cancel = new CancellationTokenSource(10000);
                var c = (testobj)await client_node.ConnectService("rr+intra:///?nodeid=" + server_id.ToString("D") + "&service=test_service", cancel: cancel.Token);
                var w = await c.testwire1.Connect(cancel.Token);

                RRAssert.IsTrue(await Task.WhenAny(obj.connected.Task, Task.Delay(5000)) == obj.connected.Task);
                var server_conn = await obj.connected.Task;

                bool closed_ok = false;
                try
                {
                    await server_conn.Close();
                    closed_ok = true;
                }
                catch (Exception) { }
                RRAssert.IsTrue(closed_ok);

                await client_node.DisconnectService(c);
            }
        }
    }
}
