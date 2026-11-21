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
    // Object locks are refused unless the security policy sets allowobjectlock
    public static class AllowObjectLockPolicyTests
    {
        public static async Task RunTestAllowObjectLockPolicy()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                string authdata = "testuser1 0b91dec4fe98266a03b136b59219d0d6 objectlock";
                var policies = new Dictionary<string, string>() { { "requirevaliduser", "true" } };
                var policy = new ServiceSecurityPolicy(new PasswordFileUserAuthenticator(authdata), policies);
                nodes.node.RegisterService("nolock_policy", "com.robotraconteur.testing.TestService1", new RobotRaconteurTest_testroot(null), policy);

                var cred1 = new Dictionary<string, object>() { { "password", "testpass1" } };
                var r = (testroot)await nodes.client_node.ConnectService(nodes.GetServiceUrl("nolock_policy"), "testuser1", cred1);
                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => await nodes.client_node.RequestObjectLock(r, RobotRaconteurObjectLockFlags.USER_LOCK));
                await nodes.client_node.DisconnectService(r);

                // allowobjectlock=true (RobotRaconteurTestService_auth): lock works
                var r2 = (testroot)await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService_auth"), "testuser1", cred1);
                await nodes.client_node.RequestObjectLock(r2, RobotRaconteurObjectLockFlags.USER_LOCK);
                await nodes.client_node.ReleaseObjectLock(r2);
                await nodes.client_node.DisconnectService(r2);
            }
        }
    }
#endif
}
