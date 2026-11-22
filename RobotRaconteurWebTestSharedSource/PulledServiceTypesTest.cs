#if !ROBOTRACONTEUR_H5
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class PulledServiceTypesTest
    {
        // Returns the node's generated factories for the definitions it is given, and records the text
        class RecordingDynamicServiceFactory : DynamicServiceFactory
        {
            RobotRaconteurNode node;
            public List<string> defs = new List<string>();
            public RecordingDynamicServiceFactory(RobotRaconteurNode node) { this.node = node; }
            public ServiceFactory CreateServiceFactory(string def, ClientContext context)
            {
                return CreateServiceFactories(new string[] { def }, context)[0];
            }
            public ServiceFactory[] CreateServiceFactories(string[] def, ClientContext context)
            {
                lock (defs)
                {
                    defs.AddRange(def);
                }
                return def.Select(x =>
                {
                    var d = new ServiceDefinition();
                    d.FromString(x);
                    return node.GetServiceType(d.Name);
                }).ToArray();
            }
        }

        static public async Task TestPulledServiceTypesDefinitionText()
        {
            // A DynamicServiceFactory must receive the pulled definition text (TCP: combined connect path)
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var f = new RecordingDynamicServiceFactory(nodes.client_node);
                nodes.client_node.RegisterDynamicServiceFactory(f);
                var c = await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));
                lock (f.defs)
                {
                    RRAssert.IsTrue(f.defs.Count > 0);
                    RRAssert.IsTrue(f.defs.All(x => !x.StartsWith("[")));
                }
                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("PulledServiceTypesTest.TestPulledServiceTypesDefinitionText passed");
        }
    }
}
#endif
