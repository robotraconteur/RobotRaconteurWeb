using System;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService3;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // ServerEndpoint.CurrentEndpoint and ServerContext.CurrentServicePath must stay valid
    // in service code after an await
    public static class CurrentEndpointAsyncTest
    {
        class obj4_async : obj4_impl, obj4
        {
            public ServerEndpoint seen_endpoint;
            public string seen_path;

            public new async Task<int> get_i_ind(CancellationToken cancel = default(CancellationToken))
            {
                await Task.Delay(20).ConfigureAwait(false);
                seen_endpoint = ServerEndpoint.CurrentEndpoint;
                seen_path = ServerContext.CurrentServicePath;
                return 0;
            }
        }

        class testroot3_async : testroot3_impl
        {
            public obj4_async o4 = new obj4_async();
            public override async Task<obj4> get_o4(CancellationToken cancel = default(CancellationToken))
            {
                await Task.Delay(20).ConfigureAwait(false);
                return o4;
            }
        }

        public static async Task TestCurrentEndpointAfterAwait()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var obj = new testroot3_async();
                nodes.node.RegisterService("current_async1", "com.robotraconteur.testing.TestService3", obj);
                var r = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("current_async1"));
                var m = new MessageEntry(MessageEntryType.PropertyGetReq, "i_ind");
                m.ServicePath = "current_async1.o4";
                await r.RRContext.ProcessRequest(m, default(CancellationToken));
                RRAssert.IsTrue(obj.o4.seen_endpoint != null);
                RRAssert.AreEqual(obj.o4.seen_endpoint.RemoteEndpoint, r.RRContext.LocalEndpoint);
                RRAssert.AreEqual(obj.o4.seen_path, "current_async1.o4");
                await nodes.client_node.DisconnectService(r);
            }
            Console.WriteLine("CurrentEndpointAsyncTest.TestCurrentEndpointAfterAwait passed");
        }
    }
}
