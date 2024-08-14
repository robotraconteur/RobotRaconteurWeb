// Copyright 2011-2024 Wason Technology, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Security policy for Robot Raconteur service
    </summary>
    <remarks>
    <para>
    The security policy sets an authenticator, and a set of policies.
    PasswordFileUserAuthenticator is
    an example of an authenticator. The valid options for Policies are as follows:
    </para>
    <list type="table">
    <listheader>
    <term>Policy name</term>
    <term>Possible Values</term>
    <term>Default</term>
    <term>Description</term>
    </listheader>
    <item>
    <term>requirevaliduser</term>
    <term>true,false</term>
    <term>false</term>
    <term>Set to "true" to require a user be authenticated before accessing
    service</term>
    </item>
    <item>
    <term>allowobjectlock</term>
    <term>true,false</term>
    <term>false</term>
    <term>If "true" allow users to request object locks. requirevaliduser must
    also be "true"</term>
    </item>
    </list>

    <para>
    The security policy is passed as a parameter to RobotRaconteurNode.RegisterService().
    </para>
    <para>See security for more information.
    </para>
    </remarks>
    */

    [PublicApi]
    public class ServiceSecurityPolicy
    {
        /**
         * <summary>The user authentication</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public UserAuthenticator Authenticator;
        /**
         * <summary>The security policies</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public Dictionary<string, string> Policies;

        /**
         * <summary>Construct a new empty security policy</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public ServiceSecurityPolicy()
        {
            Authenticator = null;
            Policies = null;
        }
        /**
        <summary>Construct a new security policy</summary>
        <remarks>None</remarks>
        <param name="Authenticator">The user authenticator</param>
        <param name="Policies">The security policies</param>
        */

        [PublicApi]
        public ServiceSecurityPolicy(UserAuthenticator Authenticator, Dictionary<string, string> Policies)
        {
            this.Authenticator = Authenticator;
            this.Policies = Policies;
        }

    }


    /**
    <summary>
    Class representing an authenticated user
    </summary>
    <remarks>
    <para>
    Use ServerEndpoint.GetCurrentAuthenticatedUser() to retrieve the
    authenticated user making a request
    </para>
    <para>See security for more information.
    </para>
    </remarks>
    */

    [PublicApi]
    public class AuthenticatedUser
    {
        private string m_Username;
        private string[] m_Privileges;
        private DateTime m_LoginTime;
        private DateTime m_LastAccessTime;
        /**
        <summary>
        The authenticated username
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public string Username { get { return m_Username; } }
        /**
        <summary>
        The user privileges
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public string[] Privileges { get { return m_Privileges; } }
        /**
        <summary>
        The user login time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime LoginTime { get { return m_LoginTime; } }
        /**
        <summary>
        The user last access time
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public DateTime LastAccessTime { get { return m_LastAccessTime; } }
#pragma warning disable 1591
        public AuthenticatedUser(string username, string[] privileges)
        {
            this.m_Username = username;
            this.m_Privileges = privileges;
            m_LoginTime = DateTime.UtcNow;
            m_LastAccessTime = DateTime.UtcNow;
        }

        public void UpdateLastAccess()
        {
            m_LastAccessTime = DateTime.UtcNow;
        }
#pragma warning restore 1591

    }

    /// <summary>
    /// Interface for service user authenticators
    /// </summary>
    [PublicApi]
    public interface UserAuthenticator
    {
        /// <summary>
        /// Function called by service to authenticate user.
        /// </summary>
        /// <remarks>
        /// Throw AuthenticationException if authentication fails. Return a populated AuthenticatedUser
        /// on success.
        ///
        /// Authenticators may use any combination of credentials. Example credentials
        /// include passwords, tokens, OTP, etc.
        ///
        /// AuthenticationException may contain additional fields to return a challenge to the
        /// client.
        /// </remarks>
        /// <param name="username">The username as a string</param>
        /// <param name="credentials">Dictionary containing credentials</param>
        /// <returns></returns>
        [PublicApi]
        AuthenticatedUser AuthenticateUser(string username, Dictionary<string, object> credentials);
    }

#if !ROBOTRACONTEUR_H5
    /**
    <summary>

    Simple authenticator using a list of username, password hash, and privileges stored in a
    file or string
    </summary>
    <remarks>
    <para>
    The password user authenticator expects a string containing a list of users,
    one per line. Each line contains the username, password as md5 hash, and privileges,
    separated by white spaces.
    An example of authentication string contents:
    </para>
    <code>
    user1 79e262a81dd19d40ae008f74eb59edce objectlock
    user2 309825a0951b3cf1f25e27b61cee8243 objectlock
    superuser1 11e5dfc68422e697563a4253ba360615 objectlock,objectlockoverride
    </code>


    <para>
    The password is md5 hashed. This hash can be generated using the ``--md5passwordhash``
    command in the "RobotRaconteurGen" utility.
    The privileges are comma separated. Valid privileges are as follows:
    </para>
    <list type="table">
    <listheader>
    <term>Policy name</term>
    <term>Possible Values</term>
    <term>Default</term>
    <term>Description</term>
    </listheader>
    <item>
    <term>requirevaliduser</term>
    <term>true,false</term>
    <term>false</term>
    <term>Set to "true" to require a user be authenticated before accessing
    service</term>
    </item>
    <item>
    <term>allowobjectlock</term>
    <term>true,false</term>
    <term>false</term>
    <term>If "true" allow users to request object locks. requirevaliduser must
    also be "true"</term>
    </item>
    </list>
    </remarks>
    */

        [PublicApi]
    public class PasswordFileUserAuthenticator : UserAuthenticator
    {

        private class User
        {
            public string username;
            public string passwordhash;
            public string[] privileges;

        }

        private Dictionary<string, User> validusers = new Dictionary<string, User>();


        /**
        <summary>
        Construct a new PasswordFileUserAuthenticator
        </summary>
        <summary name="data">A file stream</summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public PasswordFileUserAuthenticator(StreamReader file)
        {
            string d = file.ReadToEnd();
            load(d);
        }

        /**
        <summary>
        Construct a new PasswordFileUserAuthenticator
        </summary>
        <summary name="data">The file text</summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public PasswordFileUserAuthenticator(string data)
        {
            load(data);
        }

        private void load(string data)
        {
            string[] lines = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string l in lines)
            {
                string[] g = l.Split(null);
                User u = new User();
                u.username = g[0];
                u.passwordhash = g[1];
                u.privileges = g[2].Split(new char[] { ',' });

                validusers.Add(u.username, u);

            }
        }
#pragma warning disable 1591
        public AuthenticatedUser AuthenticateUser(string username, Dictionary<string, object> credentials)
        {
            if (!validusers.Keys.Contains(username)) throw new AuthenticationException("Invalid username or password");
            string password;
            try
            {
                password = (string)credentials["password"];
            }
            catch { throw new AuthenticationException("Password not supplied in credentials"); }

            string passwordhash = MD5Hash(password);

            if (validusers[username].passwordhash != passwordhash)
                throw new AuthenticationException("Invalid username or password");

            return new AuthenticatedUser(username, validusers[username].privileges);

        }


        public static string MD5Hash(string text)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

            //compute hash from the bytes of text
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }
#pragma warning restore 1591

    }
#endif

}
