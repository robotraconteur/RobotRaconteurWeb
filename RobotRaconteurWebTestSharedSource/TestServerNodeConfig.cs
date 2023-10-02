using RobotRaconteurWeb;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RobotRaconteurTest
{
    public class TestNodeConfig : IDisposable
    {
        public string node_endpoint_url { get; set; }
        public RobotRaconteurNode node { get; set; }

        public Transport tcp_transport { get; set; }

        public Transport local_transport { get; set; }

        public Transport intra_transport { get; set; }

        public RobotRaconteurNode client_node { get; set; }
        public Transport client_tcp_transport;
        public Transport client_local_transport;
        public Transport client_intra_transport;

        public TestNodeConfig(string nodename, bool enable_tcp_transport = true, bool enable_local_transport = false, bool enable_intra_transport = true, bool start_server=true, uint tcp_port=0)
        {
            if (start_server)
            {
                string nodenv = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_NODEID");
                if (nodenv != null)
                {
                    node_endpoint_url = nodenv;
                }
                else
                {

                    node = new RobotRaconteurNode();
                    node.SetLogLevelFromEnvVariable();

                    node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                    node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                    node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

#if !ROBOTRACONTEUR_H5
                // Create TcpTransport using reflection and create dynamic reference

                
                
                if (enable_local_transport)
                {
                    local_transport = CreateTransport("LocalTransport", node);
                    InvokeMethod(local_transport, "StartServerAsNodeName", "nodename");
                    node.RegisterTransport(local_transport);
                }

                if (enable_tcp_transport)
                {
                    tcp_transport = CreateTransport("TcpTransport", node);
                    InvokeMethod(tcp_transport, "StartServer", tcp_port);
                    InvokeMethod(tcp_transport, "EnableNodeAnnounce");
                    InvokeMethod(tcp_transport, "EnableNodeDiscoveryListening");
                    node.RegisterTransport(tcp_transport);
                }


#endif

                    var s1 = new RobotRaconteurTestServiceSupport();
                    s1.RegisterServices(tcp_transport);

                    var s2 = new RobotRaconteurTestServiceSupport2();
                    s2.RegisterServices();

                    var t1 = new IntraTransport(node);
                    node.RegisterTransport(t1);

                    t1.StartServer();
                    intra_transport = t1;
                }
            }

            client_node = new RobotRaconteurNode();
            client_node.SetLogLevelFromEnvVariable();

            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

            if (enable_local_transport)
            {
                client_local_transport = CreateTransport("LocalTransport", client_node);
                
                client_node.RegisterTransport(client_local_transport);
            }

            if (enable_tcp_transport)
            {
                client_tcp_transport = CreateTransport("TcpTransport", node);
                InvokeMethod(client_tcp_transport, "EnableNodeDiscoveryListening");
                client_node.RegisterTransport(client_tcp_transport);
            }

        }

        public string[] GetServiceUrl(string service_name, string scheme = null)
        {
            if (node == null)
            {
                if (node_endpoint_url.Contains("?"))
                {
                    return new string[] { node_endpoint_url + "&service=" + service_name };
                }
                else
                {
                    return new string[] { node_endpoint_url + "?service=" + service_name };
                }
            }
            else
            {
                switch (scheme)
                {
                    case null:
                        if (intra_transport != null)
                        {
                            return intra_transport.ServerListenUrls;
                        }
                        if (tcp_transport != null)
                        {
                            return tcp_transport.ServerListenUrls;
                        }
                        if (local_transport != null)
                        {
                            return local_transport.ServerListenUrls;
                        }
                        throw new ArgumentException("Could not find service url for testing");
                    case "rr+intra":
                        return intra_transport.ServerListenUrls;
                    case "rr+tcp":
                        return tcp_transport.ServerListenUrls;
                    case "rr+local":
                        return local_transport.ServerListenUrls;
                    default:
                        throw new ArgumentException("Invalid transport type for service URL");
                }
            }
        }

        private static Transport CreateTransport(string typeName, RobotRaconteurNode node)
        {
            // Get the assembly and type using reflection
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type type = assembly.GetType("RobotRaconteurWeb." + typeName);

            // Create an instance of the type
            return (Transport)Activator.CreateInstance(type, new object[] { node });
        }

        private static void InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            // Get the type of the object
            Type type = obj.GetType();

            // Get the method with the specified name
            MethodInfo methodInfo = type.GetMethod(methodName);

            // Check if the method exists
            if (methodInfo != null)
            {
                // Invoke the method on the object
                methodInfo.Invoke(obj, parameters);
            }
            else
            {
                RRWebTest.WriteLine($"Method '{methodName}' not found in type '{type.FullName}'.");
            }
        }

        public void Dispose()
        {
            client_node?.Shutdown();
            node?.Shutdown();
        }
    }
}
