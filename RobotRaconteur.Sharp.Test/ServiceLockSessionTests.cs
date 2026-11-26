using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // A client (session) lock can only be released by the session that holds it
    public static class ServiceLockSessionTests
    {
        public static async Task RunTestReleaseClientLockOtherSession()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var cred1 = new Dictionary<string, object>() { { "password", "testpass1" } };
                var r1 = (testroot)await nodes.client_node.ConnectService(url, "testuser1", cred1);
                var r2 = (testroot)await nodes.client_node.ConnectService(url, "testuser1", cred1);

                var r1_o = await r1.get_o1();
                var r2_o = await r2.get_o1();
                await nodes.client_node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.CLIENT_LOCK);
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await r2_o.set_d1(new double[] { 1.0 }));

                // Same user, other session: must not release the lock
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await nodes.client_node.ReleaseObjectLock(r2_o));
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await r2_o.set_d1(new double[] { 1.0 }));

                await nodes.client_node.ReleaseObjectLock(r1_o);
                await r2_o.set_d1(new double[] { 1.0 });
                await nodes.client_node.DisconnectService(r1);
                await nodes.client_node.DisconnectService(r2);
            }
        }
    }
}
