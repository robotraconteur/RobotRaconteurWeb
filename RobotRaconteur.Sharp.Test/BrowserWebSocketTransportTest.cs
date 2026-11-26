using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    public class BrowserWebSocketTransportTest
    {
        // A client node with only BrowserWebSocketTransport (as in Blazor WebAssembly) connects to a
        // TcpTransport server over rr+ws, calls a member and disconnects
        public static async Task TestBrowserWebSocketConnect()
        {
            using (var nodes = new TestNodeConfig("browserwstest", true, false, false))
            {
                var client_node = new RobotRaconteurNode();
                try
                {
                    client_node.RegisterServiceType(new com__robotraconteur__testing__TestService1Factory(client_node));
                    client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory(client_node));
                    client_node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory(client_node));
                    var t = new BrowserWebSocketTransport(client_node);
                    client_node.RegisterTransport(t);

                    // IPv4 and IPv6 listeners have different ports; use an IPv4 one
                    var tcp_url = nodes.GetServiceUrl("RobotRaconteurTestService", "rr+tcp").First(x => !x.Contains("["));
                    var host_port = System.Text.RegularExpressions.Regex.Match(tcp_url, "://([^/]+)/").Groups[1].Value;
                    var url = "rr+ws://" + host_port + "/?nodeid=" + nodes.node.NodeID.ToString("D") + "&service=RobotRaconteurTestService";

                    RRAssert.IsTrue(t.CanConnectService(url));
                    RRAssert.IsFalse(t.CanConnectService(tcp_url));
                    RRAssert.IsFalse(t.IsServer);

                    var cancel = new CancellationTokenSource(30000);
                    var r = (testroot)await client_node.ConnectService(url, cancel: cancel.Token);
                    await r.set_d1(3.456);
                    RRAssert.AreEqual(await r.get_d1(), 12.345);
                    await client_node.DisconnectService(r);
                }
                finally
                {
                    client_node.Shutdown();
                }
            }
            Console.WriteLine("BrowserWebSocketTransportTest.TestBrowserWebSocketConnect passed");
        }
    }
}
