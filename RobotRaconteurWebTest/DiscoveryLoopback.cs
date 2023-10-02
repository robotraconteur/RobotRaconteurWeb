using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    public static class DiscoveryLoopbackTest
    {        
        public static async Task RunDiscoveryLoopbackTest()
        {
            var client_node = new RobotRaconteurNode();
            var client_node_setup = new ClientNodeSetup();

            var server_flags = RobotRaconteurNodeSetupFlags.ServerDefault;
            server_flags &= ~RobotRaconteurNodeSetupFlags.LocalTransportStartServer;
            var node_setup = new ServerNodeSetup("discovery_test_server_node", 0, server_flags);

            var s1 = new RobotRaconteurTestServiceSupport();
            s1.RegisterServices(node_setup.TcpTransport);

            await Task.Delay(5000);

            var discovered_services = await client_node.FindServiceByType("com.robotraconteur.testing.TestService1.testroot", new string[] { "rr+tcp" });

            RRWebTest.WriteLine("Found " + discovered_services.Length + " services");

            var expected_service_names = new HashSet<string>();
            expected_service_names.Add("RobotRaconteurTestService");
            expected_service_names.Add("RobotRaconteurTestService_auth");

            RRAssert.IsTrue(discovered_services.Length >= 2);

            var found_services = 0;
            foreach(var s in discovered_services)
            {
                RRAssert.IsTrue(expected_service_names.Remove(s.Name));
                if (s.NodeName != "discovery_test_server_node")
                    continue;
                RRAssert.AreEqual(s.RootObjectType, "com.robotraconteur.testing.TestService1.testroot");
                RRAssert.AreEqual(s.RootObjectImplements.Length, 1);
                RRAssert.AreEqual(s.RootObjectImplements[0], "com.robotraconteur.testing.TestService2.baseobj");
                if (s.Name == "RobotRaconteurTestService")
                {
                    com.robotraconteur.testing.TestService1.testroot c = null;
                    c = (com.robotraconteur.testing.TestService1.testroot)await client_node.ConnectService(s.ConnectionURL);
                    await c.get_d1();
                    await client_node.DisconnectService(c);
                }
                found_services++;
            }

            RRAssert.IsTrue(found_services >= 2);
            RRAssert.AreEqual(expected_service_names.Count, 0);
        }
    }
}
