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
    /**
    <summary>
    "callback" member type interface
    </summary>
    <remarks>
    <para>
    The Callback class implements the `callback` member type. Callbacks are declared in service definition
    files using the `callback` keyword within object declarations. They provide functionality similar to the
    `function` member, but the direction is reversed, allowing the service to call a function on a specified
    client. The desired client is specified using the Robot Raconteur endpoint identifier. Clients must
    configure the callback to use using SetFunction().
    </para>
    <para>
    On the client side,
    the client specifies a function for the callback using the SetFunction() function.
    On the service side, the function GetFunction(uint e) is used to retrieve
    the proxy function to call a client callback.
    </para>
    <para>
    This class is instantiated by the node. It should not be instantiated by the user.
    </para>
    </remarks>
    <typeparam name="T">The type of the callback function. This is determined by the thunk source generator.</typeparam>
    */
    [PublicApi]
    public abstract class Callback<T>
    {

        protected string m_MemberName;

        public Callback(string name)
        {
            m_MemberName = name;
        }

        /**
        <summary>
        Get or set the currently configured callback function on client side
        </summary>
        <remarks>
        The callback function set will be made available to be called by
        the service using a function proxy.
        </remarks>
        */
        [PublicApi]
        public abstract T Function { get; set; }
        /**
        <summary>
        Get the proxy function to call the callback for the specified client on
        the service side
        </summary>
        <remarks>
        <para>
        This function returns a proxy to the callback on a specified client. The proxy
        operates as a reverse function, sending parameters, executing the callback,
        and receiving the results.
        </para>
        <para>
        Because services can have multiple clients, it is necessary to specify which client
        to call. This is done by passing the endpoint of the client connection to the
        endpoint parameter.
        </para>
        <para>
        The endpoint of a client can be determined using the ServerEndpoint.CurrentEndpoint()
        function during a `function` or `property` member call. The service can store this
        value, and use it to retrieve the callback proxy.
        </para>
        </remarks>
        <param name="e">The endpoint of the client connection</param>
        <returns>The callback proxy function</returns>
        */
        [PublicApi]
        public abstract T GetClientFunction(Endpoint e);
        /**
        <summary>
        Get the proxy function to call the callback for the specified client on
        the service side
        </summary>
        <remarks>
        <para>
        This function returns a proxy to the callback on a specified client. The proxy
        operates as a reverse function, sending parameters, executing the callback,
        and receiving the results.
        </para>
        <para>
        Because services can have multiple clients, it is necessary to specify which client
        to call. This is done by passing the endpoint of the client connection to the
        endpoint parameter.
        </para>
        <para>
        The endpoint of a client can be determined using the ServerEndpoint.CurrentEndpoint()
        function during a `function` or `property` member call. The service can store this
        value, and use it to retrieve the callback proxy.
        </para>
        </remarks>
        <param name="e">The endpoint of the client connection</param>
        <returns>The callback proxy function</returns>
        */
        [PublicApi]
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