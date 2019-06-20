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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurWeb;
using RobotRaconteurTest;

namespace RobotRaconteurWebTestNETStandard
{
    class Program
    {
       
        async void DoHttp()
        {

            var t = new TcpTransport();
            RobotRaconteurNode.s.RegisterTransport(t);

            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

            var s = new RobotRaconteurTestServiceSupport();
            s.RegisterServices(t);

            var s2 = new RobotRaconteurTestServiceSupport2();
            s2.RegisterServices(t);


            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:60080/robotraconteurtest/");
            listener.Start();

            while (true)
            {
                var c = await listener.GetContextAsync();
                if (c.Request.IsWebSocketRequest)
                {
                    var ws_context = await c.AcceptWebSocketAsync("robotraconteur.robotraconteur.com");

                    t.AcceptAndProcessServerWebSocket(ws_context.WebSocket, ws_context.RequestUri.ToString()).ContinueWith(x => { });
                }
                else
                {
                    c.Response.StatusCode = 400;
                    c.Response.Close();
                }
            }
        }


        static void Main(string[] args)
        {
            var p = new Program();
            p.DoHttp();
            Console.WriteLine("HTTP test server started on port 60080");
            Console.ReadLine();
        }
    }
}
