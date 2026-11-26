using System;
using System.Threading.Tasks;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.BlazorTest
{
    // Runs the standard client service tests (the same ServiceTestClient and ServiceTestClient2 used by
    // RobotRaconteur.Sharp.Test) from inside the browser, against the "browsertestserver" test command.
    // The node uses only BrowserWebSocketTransport and registers the service factories explicitly, as a
    // Blazor WebAssembly application would.
    public static class BrowserServiceTests
    {
        public static async Task Run(string host_port, Action<string> log)
        {
            RRWebTest.WriteLineFunc = (format, args) => log(args == null || args.Length == 0 ? format : string.Format(format, args));

            string url_base = "rr+ws://" + host_port + "/?service=";
            var node = new RobotRaconteurNode();
            try
            {
                node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory(node));
                node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory(node));
                node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory(node));
                node.RegisterTransport(new BrowserWebSocketTransport(node));

                log("ServiceTestClient");
                var client = new ServiceTestClient(node);
                await client.RunFullTest(new[] { url_base + "RobotRaconteurTestService" },
                    new[] { url_base + "RobotRaconteurTestService_auth" });

                log("ServiceTestClient2");
                var client2 = new ServiceTestClient2(node);
                await client2.RunFullTest(url_base + "RobotRaconteurTestService2");
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
