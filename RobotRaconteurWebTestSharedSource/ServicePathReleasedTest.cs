#if !ROBOTRACONTEUR_H5
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class ServicePathReleasedTest
    {
        static public async Task TestServicePathReleasedSeparator()
        {
            // A ServicePathReleased event for svc.o1 must drop the client stubs of svc.o1 and its sub-objects,
            // not svc.o10
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);
                await c.get_o1();

                var client_ctx = ((ServiceStub)c).RRContext;
                var stubs = (Dictionary<string, ServiceStub>)typeof(ClientContext)
                    .GetField("stubs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client_ctx);
                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                string p = ctx.ServiceName + ".o1";
                // Stand-in for a stub of an object whose name starts with "o1"
                lock (stubs)
                {
                    RRAssert.IsTrue(stubs.ContainsKey(p));
                    stubs[p + "0"] = stubs[p];
                }

                ctx.ReleaseServicePath(p);

                // The event is delivered asynchronously: wait (bounded) for svc.o1 to be dropped
                var sw = Stopwatch.StartNew();
                while (true)
                {
                    lock (stubs)
                    {
                        if (!stubs.ContainsKey(p))
                            break;
                    }
                    RRAssert.IsTrue(sw.ElapsedMilliseconds < 10000);
                    await Task.Delay(10);
                }
                lock (stubs)
                {
                    RRAssert.IsTrue(stubs.ContainsKey(p + "0"));
                    stubs.Remove(p + "0");
                }
            }
            Console.WriteLine("ServicePathReleasedTest.TestServicePathReleasedSeparator passed");
        }
    }
}
#endif
