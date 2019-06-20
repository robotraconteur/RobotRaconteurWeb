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

namespace RobotRaconteurWeb
{
   
    public abstract class Callback<T>
    {

        protected string m_MemberName;

        public Callback(string name)
        {
            m_MemberName = name;
        }

        public abstract T Function { get; set; }

        public abstract T GetClientFunction(Endpoint e);

        public abstract T GetClientFunction(uint e);


    }

    public class CallbackClient<T> : Callback<T>
    {
        public CallbackClient(string name)
            : base(name)
        {
        }

        T function = default(T);
        public override T Function
        {
            get
            {
                return function;
            }
            set
            {
                function = value;
            }
        }

        public override T GetClientFunction(Endpoint e)
        {
            throw new InvalidOperationException("Invalid for client side of callback");
        }

        public override T GetClientFunction(uint e)
        {
            throw new InvalidOperationException("Invalid for client side of callback");
        }


    }

    public class CallbackServer<T> : Callback<T>
    {
        ServiceSkel skel;

        public CallbackServer(string name, ServiceSkel skel)
            : base(name)
        {
            this.skel = skel;
        }

        public override T Function
        {
            get
            {
                throw new InvalidOperationException("Invalid for server side of callback");
            }
            set
            {
                throw new InvalidOperationException("Invalid for server side of callback");
            }
        }

        public override T GetClientFunction(Endpoint e)
        {
            return GetClientFunction(e.LocalEndpoint);
        }

        public override T GetClientFunction(uint e)
        {
            return (T)skel.GetCallbackFunction(e, m_MemberName);
        }



    }


}