using System;
using System.Collections.Generic;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // Regression test: password file columns may be separated by any run of whitespace, and
    // whitespace-only lines are ignored
    public static class PasswordFileParseTests
    {
        public static void RunTestPasswordFileWhitespace()
        {
            string h1 = PasswordFileUserAuthenticator.MD5Hash("password1");
            string h2 = PasswordFileUserAuthenticator.MD5Hash("password2");
            string data = "user1  " + h1 + "\t objectlock,objectlockoverride\r\n"
                + "   \n"
                + "\tuser2 " + h2 + " objectlock  \n";

            var a = new PasswordFileUserAuthenticator(data);

            var u1 = a.AuthenticateUser("user1", new Dictionary<string, object> { { "password", "password1" } });
            RRAssert.AreEqual(u1.Username, "user1");
            RRAssert.AreEqual(u1.Privileges.Length, 2);
            RRAssert.AreEqual(u1.Privileges[0], "objectlock");
            RRAssert.AreEqual(u1.Privileges[1], "objectlockoverride");

            var u2 = a.AuthenticateUser("user2", new Dictionary<string, object> { { "password", "password2" } });
            RRAssert.AreEqual(u2.Username, "user2");
            RRAssert.AreEqual(u2.Privileges.Length, 1);
            RRAssert.AreEqual(u2.Privileges[0], "objectlock");
        }
    }
}
