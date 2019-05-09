// Copyright 2011-2019 Wason Technology, LLC
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteur.Extensions;

namespace RobotRaconteur
{
    public class ServiceSecurityPolicy
    {
        public UserAuthenticator Authenticator;
        public Dictionary<string, string> Policies;

        public ServiceSecurityPolicy()
        {
            Authenticator = null;
            Policies = null;
        }

        public ServiceSecurityPolicy(UserAuthenticator Authenticator, Dictionary<string, string> Policies)
        {
            this.Authenticator = Authenticator;
            this.Policies = Policies;
        }

    }



    public class AuthenticatedUser
    {
        private string m_Username;
        private string[] m_Privileges;
        private DateTime m_LoginTime;
        private DateTime m_LastAccessTime;

        public string Username { get { return m_Username; } }

        public string[] Privileges { get { return m_Privileges; } }

        public DateTime LoginTime { get { return m_LoginTime; } }

        public DateTime LastAccessTime { get { return m_LastAccessTime; } }

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

    }


    public interface UserAuthenticator
    {
        AuthenticatedUser AuthenticateUser(string username, Dictionary<string, object> credentials);


    }

#if !ROBOTRACONTEUR_BRIDGE
    public class PasswordFileUserAuthenticator : UserAuthenticator
    {

        private class User
        {
            public string username;
            public string passwordhash;
            public string[] privileges;

        }

        private Dictionary<string, User> validusers = new Dictionary<string, User>();

        public PasswordFileUserAuthenticator(StreamReader file)
        {
            string d = file.ReadToEnd();
            load(d);
        }

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

    }
#endif

}