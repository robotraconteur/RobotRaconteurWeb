#if !ROBOTRACONTEUR_H5
using System;
using System.Collections.Generic;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: a null user name or null password must fail with AuthenticationException
    public static class PasswordFileNullCredentialTests
    {
        static Exception Auth(PasswordFileUserAuthenticator a, string username, Dictionary<string, object> credentials)
        {
            try
            {
                a.AuthenticateUser(username, credentials);
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        public static void RunTestNullCredentials()
        {
            var a = new PasswordFileUserAuthenticator("user1 " + PasswordFileUserAuthenticator.MD5Hash("password1") + " objectlock\n");

            var e1 = Auth(a, null, new Dictionary<string, object> { { "password", "password1" } });
            RRAssert.IsTrue(e1 is AuthenticationException);

            var e2 = Auth(a, "user1", new Dictionary<string, object> { { "password", null } });
            RRAssert.IsTrue(e2 is AuthenticationException);

            var e3 = Auth(a, "user1", new Dictionary<string, object> { { "password", "wrong" } });
            RRAssert.IsTrue(e3 is AuthenticationException);

            RRAssert.IsTrue(Auth(a, "user1", new Dictionary<string, object> { { "password", "password1" } }) == null);
        }
    }
}
#endif
