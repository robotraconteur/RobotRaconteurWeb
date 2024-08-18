using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    public static class HttpServerTest
    {

        public static async Task RunHttpServer()
        {
            var cancel = new CancellationTokenSource();
            Task.Run(() => DoHttp(cancel.Token)).ConfigureAwait(false);
            RRWebTest.WriteLine("Server started, press enter to quit");
            await Task.Run(() => Console.ReadLine());
            cancel.Cancel();
        }

        public static async Task DoHttp(CancellationToken cancel)
        {

            var t = new TcpTransport();
            RobotRaconteurNode.s.RegisterTransport(t);

            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

            var s = new RobotRaconteurTestServiceSupport();
            s.RegisterServices(t);

            var s2 = new RobotRaconteurTestServiceSupport2();
            s2.RegisterServices();


            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:22280/robotraconteurtest/");
            listener.Start();

            cancel.Register(() => listener.Stop());

            while (true)
            {
                var c = await listener.GetContextAsync().ConfigureAwait(false);
                if (c.Request.IsWebSocketRequest)
                {
                    var ws_context = await c.AcceptWebSocketAsync("robotraconteur.robotraconteur.com").ConfigureAwait(false);

                    t.AcceptAndProcessServerWebSocket(ws_context.WebSocket, ws_context.RequestUri.ToString()).ContinueWith(x => { });
                }
                else
                {
                    c.Response.StatusCode = 400;
                    c.Response.Close();
                }
            }
        }
    }
}
