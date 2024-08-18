using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class ServiceTests
    {
        List<string> urls;
        List<string> auth_urls;
        List<string> urls2;
        RobotRaconteurNode node;
        public ServiceTests(RobotRaconteurNode node, string[] urls, string[] auth_urls, string[] urls2)
        {
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
            else
            {
                this.node = node;
            }

            this.urls = urls.ToList();
            this.auth_urls = auth_urls.ToList();
            this.urls2 = urls2.ToList();

        }
        public async Task LoopbackTest()
        {
            ServiceTestClient serviceTestClient = new ServiceTestClient(node);
            await serviceTestClient.RunFullTest(urls.ToArray(), auth_urls.ToArray());
        }

        public async Task LoopbackTest2()
        {
            ServiceTestClient2 serviceTestClient = new ServiceTestClient2(node);
            await serviceTestClient.RunFullTest(urls2[0]);
        }

        static public async Task RunTcpLoopback()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var auth_urls = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var urls2 = nodes.GetServiceUrl("RobotRaconteurTestService2");

                var test = new ServiceTests(nodes.client_node, urls, auth_urls, urls2);
                await test.LoopbackTest();
                await test.LoopbackTest2();
            }
        }

        static public async Task RunLocalLoopback()
        {
            using (var nodes = new TestNodeConfig("testprog", false, true, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var auth_urls = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var urls2 = nodes.GetServiceUrl("RobotRaconteurTestService2");

                var test = new ServiceTests(nodes.client_node, urls, auth_urls, urls2);
                await test.LoopbackTest();
                await test.LoopbackTest2();
            }
        }

        static public async Task RunIntraLoopback()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var auth_urls = nodes.GetServiceUrl("RobotRaconteurTestService_auth");
                var urls2 = nodes.GetServiceUrl("RobotRaconteurTestService2");

#if ROBOTRACONTEUR_H5
                auth_urls = null;
#endif

                var test = new ServiceTests(nodes.client_node, urls, auth_urls, urls2);
                await test.LoopbackTest();
                await test.LoopbackTest2();
            }
        }

        static public async Task RunClientTests(string endpoint_url)
        {
            string join_char = endpoint_url.Contains("?") ? "&" : "?";
            var urls = new string[] { endpoint_url + join_char + "service=RobotRaconteurTestService" };
            var auth_urls = new string[] { endpoint_url + join_char + "service=RobotRaconteurTestService_auth" };
            var urls2 = new string[] { endpoint_url + join_char + "service=RobotRaconteurTestService2" };

#if ROBOTRACONTEUR_H5
            auth_urls = null;
#endif
            using (var nodes = new TestNodeConfig("testprog", true, true, true, false))
            {
                var test = new ServiceTests(nodes.client_node, urls, auth_urls, urls2);
                await test.LoopbackTest();
                await test.LoopbackTest2();
            }

        }

        public static async Task RunServiceTest(string[] args)
        {
            switch (args[0])
            {
                case "loopback":
                case "tcploopback":
                    await RunTcpLoopback();
                    break;
                case "intraloopback":
                    await RunIntraLoopback();
                    break;
                case "localloopback":
                    await RunLocalLoopback();
                    break;
                case "client":
                    await RunClientTests(args[1]);
                    break;
                default:
                    throw new ArgumentException("Invalid test command");
            }
        }
    }

    public class TestServer : IDisposable
    {
        public TestNodeConfig node_config { get; set; }
        public TestServer(string nodename = "testprog", bool enable_tcp_transport = true, bool enable_local_transport = true, bool enable_intra_transport = true, int tcp_port = 0)
        {
            node_config = new TestNodeConfig(nodename, enable_tcp_transport, enable_local_transport, enable_intra_transport, true, tcp_port);
        }

        public TestServer(string nodename = "testprog", bool enable_tcp_transport = true, bool enable_local_transport = true, bool enable_intra_transport = true, string tcp_port = null)
        {
            node_config = new TestNodeConfig(nodename, enable_tcp_transport, enable_local_transport, enable_intra_transport, true, tcp_port);
        }
        public void Dispose()
        {
            node_config.Dispose();
        }

        public static async Task RunServer(string nodename = "testprog", string tcp_port = null)
        {
            using (var server = new TestServer(nodename, tcp_port: tcp_port))
            {
                RRWebTest.WriteLine("Server started, press enter to quit");
                await Task.Run(() => Console.ReadLine());
            }
        }
    }
}
