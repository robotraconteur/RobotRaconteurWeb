using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    public class DiscoveryTests
    {
        public static async Task FindServiceByTypeProgram(string servicetype, string[] schemes)
        {
            using (var config = new TestNodeConfig("", true, true, true, false))
            {
                await Task.Delay(5000);

                var r = await config.client_node.FindServiceByType(servicetype, schemes);

                foreach (var e in r)
                {
                    RRWebTest.WriteLine("Name: " + e.Name);
                    RRWebTest.WriteLine("RootObjectType: " + e.RootObjectType);
                    RRWebTest.WriteLine("RootObjectImplements: " + e.ConnectionURL);
                    RRWebTest.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    RRWebTest.WriteLine("NodeID: " + e.NodeID.ToString());
                    RRWebTest.WriteLine("NodeName: " + e.NodeName);
                    RRWebTest.WriteLine("");
                }
            }
        }

        public static async Task FindNodeByNameProgram(string nodename, string[] schemes)
        {
            using (var config = new TestNodeConfig("", true, true, true, false))
            {
                await Task.Delay(5000);

                var r = await config.client_node.FindNodeByName(nodename, schemes);

                foreach (var e in r)
                {
                    RRWebTest.WriteLine("NodeID: " + e.NodeID);
                    RRWebTest.WriteLine("NodeName: " + e.NodeName);
                    RRWebTest.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    RRWebTest.WriteLine("");
                }

            }
        }

        public static async Task FindNodeByIdProgram(NodeID nodeid, string[] schemes)
        {
            using (var config = new TestNodeConfig("", true, true, true, false))
            {
                await Task.Delay(5000);

                var r = await config.client_node.FindNodeByID(nodeid, schemes);

                foreach (var e in r)
                {
                    RRWebTest.WriteLine("NodeID: " + e.NodeID);
                    RRWebTest.WriteLine("NodeName: " + e.NodeName);
                    RRWebTest.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    RRWebTest.WriteLine("");
                }

            }
        }

        public static async Task RunDiscoveryProgram(string[] args)
        {
            switch (args[0])
            {
                case "findservicebytype":
                    {
                        string servicetype = args[1];
                        string[] schemes = args[2].Split(new char[] { ',' });
                        await FindServiceByTypeProgram(servicetype, schemes);
                    }
                    break;
                case "findnodebyid":
                    {
                        var nodeid = new NodeID(args[1]);
                        string[] schemes = args[2].Split(new char[] { ',' });
                        await FindNodeByIdProgram(nodeid, schemes);
                    }
                    break;
                case "findnodebyname":
                    {
                        var nodename = args[1];
                        string[] schemes = args[2].Split(new char[] { ',' });
                        await FindNodeByNameProgram(nodename, schemes);
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid test command");
            }
        }
    }
}
