using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
#if !ROBOTRACONTEUR_H5
    // Locks held by a session are released when its user logs out (and when it disconnects afterwards)
    public static class LockReleaseOnLogoutTests
    {
        public static async Task RunTestLockReleaseOnLogout()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var cred1 = new Dictionary<string, object>() { { "password", "testpass1" } };
                var cred2 = new Dictionary<string, object>() { { "password", "testpass2" } };

                var r2 = (testroot)await nodes.client_node.ConnectService(url, "testuser2", cred2);
                var r2_o = await r2.get_o1();

                var r1 = (testroot)await nodes.client_node.ConnectService(url, "testuser1", cred1);
                var r1_o = await r1.get_o1();
                await nodes.client_node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.CLIENT_LOCK);
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await r2_o.set_d1(new double[] { 1.0 }));

                await ((ServiceStub)r1).RRContext.LogoutUser();
                await nodes.client_node.DisconnectService(r1);

                await r2_o.set_d1(new double[] { 1.0 });
                await nodes.client_node.DisconnectService(r2);
            }
        }
    }
#endif
}
