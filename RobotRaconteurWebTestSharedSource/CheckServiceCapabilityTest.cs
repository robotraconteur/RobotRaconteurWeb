#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class CheckServiceCapabilityTest
    {
        static public async Task TestCheckServiceCapability()
        {
            // CheckServiceCapability must return the service's answer and leave the connection open
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);
                var cancel = new CancellationTokenSource(10000);
                uint res = await ((ServiceStub)c).RRContext.CheckServiceCapability("test_capability", cancel.Token);
                RRAssert.AreEqual(res, (uint)0);
                // The connection must still work
                await c.get_o1(cancel.Token);
                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("CheckServiceCapabilityTest.TestCheckServiceCapability passed");
        }
    }
}
#endif
