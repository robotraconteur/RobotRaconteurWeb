using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{

    static class AttributesTest
    {
        static public async Task RunAttributesTest()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");

                var c = (com.robotraconteur.testing.TestService1.testroot)await nodes.client_node.ConnectService(urls[0]);
                var attr = nodes.client_node.GetServiceAttributes(c);

                RRAssert.IsTrue(attr != null);
                RRAssert.Equals(attr.Count, 2);
                RRAssert.Equals((string)attr["test"], "This is a test attribute");
                RRAssert.Equals((int[])attr["test2"], new int[] { 42 });
            }
        }

        public static void TestPasswordHashUtf8()
        {
            // Passwords are hashed from their UTF-8 bytes, as in Core
            RRAssert.AreEqual(PasswordFileUserAuthenticator.MD5Hash("password"), "5f4dcc3b5aa765d61d8327deb882cf99");
            RRAssert.AreEqual(PasswordFileUserAuthenticator.MD5Hash("pässword"), "8e1843033a0f6ee52e2f618aa8ebbef4");

            var auth = new PasswordFileUserAuthenticator("user1 8e1843033a0f6ee52e2f618aa8ebbef4 objectlock\n");
            auth.AuthenticateUser("user1", new Dictionary<string, object> { { "password", "pässword" } });
            RRAssert.ThrowsException<AuthenticationException>(() =>
                auth.AuthenticateUser("user1", new Dictionary<string, object> { { "password", "p?ssword" } }));
            RRAssert.ThrowsException<AuthenticationException>(() =>
                auth.AuthenticateUser("user1", new Dictionary<string, object> { { "password", "pössword" } }));

            // Legacy files hashed from ASCII bytes (non-ASCII replaced by '?') are still accepted
            var auth2 = new PasswordFileUserAuthenticator("user1 c0060859e24d230671220d23e4b22cb7 objectlock\n");
            auth2.AuthenticateUser("user1", new Dictionary<string, object> { { "password", "pässword" } });
            Console.WriteLine("AttributesTest.TestPasswordHashUtf8 passed");
        }

        public static void TestCloseServiceUpdatesNonce()
        {
            // Closing a service must change the service state nonce so discovery clients see the removal
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var nonce1 = nodes.node.ServiceStateNonce;
                nodes.node.CloseService("RobotRaconteurTestService2");
                RRAssert.IsTrue(nodes.node.ServiceStateNonce != nonce1);
            }
            Console.WriteLine("AttributesTest.TestCloseServiceUpdatesNonce passed");
        }

        public static void TestSetAttributesUpdatesNonce()
        {
            // ServerContext.SetAttributes must replace the attributes and change the service state nonce
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var nonce1 = nodes.node.ServiceStateNonce;
                var ctx = nodes.node.GetService("RobotRaconteurTestService2");
                var attr = new Dictionary<string, object> { { "test3", "value3" } };
                ctx.SetAttributes(attr);
                RRAssert.IsTrue(ctx.Attributes == attr);
                RRAssert.IsTrue(nodes.node.ServiceStateNonce != nonce1);
            }
            Console.WriteLine("AttributesTest.TestSetAttributesUpdatesNonce passed");
        }
    }
}
