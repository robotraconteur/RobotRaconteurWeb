using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    static class ReleaseServicePathTest
    {
        static public async Task TestReleaseServicePathSeparator()
        {
            // ReleaseServicePath("svc.o1") must release svc.o1 and its sub-objects, not svc.o10
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);
                await c.get_o1();

                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                var skels = (Dictionary<string, ServiceSkel>)typeof(ServerContext)
                    .GetField("skels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ctx);
                string p = ctx.ServiceName + ".o1";
                // Stand-in for an object whose name starts with "o1"
                lock (skels)
                {
                    RRAssert.IsTrue(skels.ContainsKey(p));
                    skels[p + "0"] = skels[p];
                }

                ctx.ReleaseServicePath(p);
                lock (skels)
                {
                    RRAssert.IsFalse(skels.ContainsKey(p));
                    RRAssert.IsTrue(skels.ContainsKey(p + "0"));
                    skels.Remove(p + "0");
                }
            }
            Console.WriteLine("ReleaseServicePathTest.TestReleaseServicePathSeparator passed");
        }
    }
}
