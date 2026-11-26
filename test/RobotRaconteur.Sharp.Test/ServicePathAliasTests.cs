using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    // svc.o3[1] and svc.o3[%31] name the same object, so a lock on one covers the other
    public static class ServicePathAliasTests
    {
        public static async Task RunTestServicePathAliasLock()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var cred1 = new Dictionary<string, object>() { { "password", "testpass1" } };
                var cred2 = new Dictionary<string, object>() { { "password", "testpass2" } };
                var r1 = (testroot)await nodes.client_node.ConnectService(url, "testuser1", cred1);
                var r2 = (testroot)await nodes.client_node.ConnectService(url, "testuser2", cred2);

                var r1_o = await r1.get_o3(1);
                await nodes.client_node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.USER_LOCK);

                var m = new MessageEntry(MessageEntryType.PropertyGetReq, "d1");
                m.ServicePath = "RobotRaconteurTestService_auth.o3[%31]";
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await ((ServiceStub)r2).RRContext.ProcessRequest(m, default(CancellationToken)));

                await nodes.client_node.ReleaseObjectLock(r1_o);
                await nodes.client_node.DisconnectService(r1);
                await nodes.client_node.DisconnectService(r2);
            }
        }
    }
}
