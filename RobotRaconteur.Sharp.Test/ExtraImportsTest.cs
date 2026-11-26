using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    static class ExtraImportsTest
    {
        static public async Task TestExtraImportsElementName()
        {
            // GetServiceDesc must report extra imports in element "extraimports", as Core and TS read it (TCP, so the reply is serialized)
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                ctx.AddExtraImport("com.robotraconteur.testing.TestService3");
                var c = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));
                var m = new MessageEntry(MessageEntryType.GetServiceDesc, "");
                m.ServicePath = ctx.ServiceName;
                m.AddElement("clientversion", RobotRaconteurNode.Version);
                var ret = await c.RRContext.ProcessRequest(m, new CancellationTokenSource(10000).Token);
                RRAssert.IsTrue(ret.TryFindElement("extraimports", out var e));
                var l = (List<string>)nodes.client_node.UnpackListType<string>(e.Data, null);
                RRAssert.IsTrue(l.Contains("com.robotraconteur.testing.TestService3"));
                ctx.RemoveExtraImport("com.robotraconteur.testing.TestService3");
                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("ExtraImportsTest.TestExtraImportsElementName passed");
        }
    }
}
