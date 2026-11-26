using System;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService3;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // The first request on a sub-object that is created for it must run with
    // ServerContext.CurrentServicePath / CurrentServerContext set
    public static class SubObjectCurrentPathTest
    {
        class obj4_current_path : obj4_impl, obj4
        {
            public string seen_path;
            public ServerContext seen_context;

            public new Task<int> get_i_ind(CancellationToken cancel = default(CancellationToken))
            {
                seen_path = ServerContext.CurrentServicePath;
                seen_context = ServerContext.CurrentServerContext;
                return Task.FromResult(0);
            }
        }

        class testroot3_current_path : testroot3_impl
        {
            public obj4_current_path o4 = new obj4_current_path();
            public override Task<obj4> get_o4(CancellationToken cancel = default(CancellationToken))
            {
                return Task.FromResult((obj4)o4);
            }
        }

        public static async Task TestFirstSubObjectRequestCurrentPath()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var obj = new testroot3_current_path();
                var ctx = nodes.node.RegisterService("subobj_path1", "com.robotraconteur.testing.TestService3", obj);
                var r = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("subobj_path1"));
                var m = new MessageEntry(MessageEntryType.PropertyGetReq, "i_ind");
                m.ServicePath = "subobj_path1.o4";
                await r.RRContext.ProcessRequest(m, default(CancellationToken));
                RRAssert.AreEqual(obj.o4.seen_path, "subobj_path1.o4");
                RRAssert.IsTrue(obj.o4.seen_context == ctx);
                await nodes.client_node.DisconnectService(r);
            }
            Console.WriteLine("SubObjectCurrentPathTest.TestFirstSubObjectRequestCurrentPath passed");
        }
    }
}
