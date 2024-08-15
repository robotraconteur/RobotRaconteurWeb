using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RobotRaconteurWeb;

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


        public TestNodeConfig(string nodename, bool enable_tcp_transport = true, bool enable_local_transport = false, bool enable_intra_transport = true, bool start_server = true, object tcp_port = null)
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

                    node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory(node));
                    node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory(node));
                    node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory(node));

#if !ROBOTRACONTEUR_H5
                // Create TcpTransport using reflection and create dynamic reference



                if (enable_local_transport)
                {
                    var t1 = new LocalTransport(node);
                    t1.StartServerAsNodeName(nodename);
                    node.RegisterTransport(t1);
                    local_transport = t1;
                }

                if (enable_tcp_transport)
                {
                    var t2 = new TcpTransport(node);
                        string tcp_port_s = tcp_port as string;
                        if (tcp_port_s != null)
                        {
                            if (tcp_port_s == "sharer")
                            {
                                t2.StartServerUsingPortSharer();
                            }
                            else
                            {
                                t2.StartServer(int.Parse(tcp_port_s));
                            }
                        }
                        else
                        {
                            int tcp_port_i = (int)tcp_port;
                            t2.StartServer(tcp_port_i);
                        }


                    t2.EnableNodeAnnounce();
                    t2.EnableNodeDiscoveryListening();
                    node.RegisterTransport(t2);
                    tcp_transport = t2;
                }



#endif
                    var s1 = new RobotRaconteurTestServiceSupport(node);
                    s1.RegisterServices(tcp_transport);

                    var s2 = new RobotRaconteurTestServiceSupport2(node);
                    s2.RegisterServices();

                    if (enable_intra_transport)
                    {
                        var t1 = new IntraTransport(node);
                        node.RegisterTransport(t1);

                        t1.StartServer();
                        intra_transport = t1;
                    }
                }
            }

            client_node = new RobotRaconteurNode();
            client_node.SetLogLevelFromEnvVariable();

            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory(client_node));
            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory(client_node));
            client_node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory(client_node));

#if !ROBOTRACONTEUR_H5
            if (enable_local_transport)
            {
                client_local_transport = new LocalTransport(client_node);

                client_node.RegisterTransport(client_local_transport);
            }

            if (enable_tcp_transport)
            {
                var t2 = new TcpTransport(client_node);
                t2.EnableNodeDiscoveryListening();
                client_node.RegisterTransport(t2);
                client_tcp_transport = t2;
            }
#endif

            if (enable_intra_transport)
            {
                var t3 = new IntraTransport(client_node);
                t3.StartClient();
                client_intra_transport = t3;
                client_node.RegisterTransport(t3);
            }

        }

        string[] append_service(string[] eps, string service_name)
        {
            List<string> o = new List<string>();
            foreach (var ep in eps)
            {
                if (ep.Contains("?"))
                {
                    o.Add(ep + "&service=" + service_name);
                }
                else
                {
                    o.Add(ep + "?service=" + service_name);
                }
            }
            return o.ToArray();
        }

        public string[] GetServiceUrl(string service_name, string scheme = null)
        {
            if (node == null)
            {
                return append_service(new[] { node_endpoint_url }, service_name);
            }
            else
            {
                switch (scheme)
                {
                    case null:
                        if (intra_transport != null)
                        {
                            return append_service(intra_transport.ServerListenUrls, service_name);
                        }
                        if (tcp_transport != null)
                        {
                            return append_service(tcp_transport.ServerListenUrls, service_name);
                        }
                        if (local_transport != null)
                        {
                            return append_service(local_transport.ServerListenUrls, service_name);
                        }
                        throw new ArgumentException("Could not find service url for testing");
                    case "rr+intra":
                        return append_service(intra_transport.ServerListenUrls, service_name);
                    case "rr+tcp":
                        return append_service(tcp_transport.ServerListenUrls, service_name);
                    case "rr+local":
                        return append_service(local_transport.ServerListenUrls, service_name);
                    default:
                        throw new ArgumentException("Invalid transport type for service URL");
                }
            }
        }

        private static Transport CreateTransport(string typeName, RobotRaconteurNode node)
        {
            // Get the assembly and type using reflection
            Assembly assembly = Assembly.GetExecutingAssembly();
            //Type type = assembly.GetType("RobotRaconteurWeb." + typeName);
            Type type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName.Equals("RobotRaconteurWeb." + typeName));

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
