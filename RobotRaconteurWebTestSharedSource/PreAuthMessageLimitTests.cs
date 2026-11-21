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
    // Messages over 512 KiB are refused before an auth-required service has authenticated the client
    public static class PreAuthMessageLimitTests
    {
        public static async Task RunTestPreAuthMessageLimit()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService_auth");

                // Authenticated: large message accepted
                var cred1 = new Dictionary<string, object>() { { "password", "testpass1" } };
                var r1 = (testroot)await nodes.client_node.ConnectService(url, "testuser1", cred1);
                var r1_o = await r1.get_o1();
                await r1_o.set_d1(new double[100000]);
                await nodes.client_node.DisconnectService(r1);

                // Not authenticated: a connect request over 512 KiB (huge credentials) must be refused by the
                // transport before it reaches the authenticator
                var cred_big = new Dictionary<string, object>() { { "password", new string('a', 600000) } };
                Exception err = null;
                try
                {
                    await nodes.client_node.ConnectService(url, "testuser1", cred_big);
                }
                catch (Exception e)
                {
                    err = e;
                }
                RRAssert.IsTrue(err != null);
                RRAssert.IsFalse(err is AuthenticationException || err.ToString().Contains("AuthenticationError"));
            }
        }
    }
#endif
}
