using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class PullServiceDefsCycleTest
    {
        // Service types that only provide their definition text. They have the (node, context) constructor
        // that NodeSetup assembly scanning uses, so scanning registers them like generated factories.
        public abstract class DefOnlyServiceFactory : ServiceFactory
        {
            protected DefOnlyServiceFactory(RobotRaconteurNode node, ClientContext context) : base(node, context) { }
            public override IStructureStub FindStructureStub(string objecttype) { throw new NotImplementedException(); }
            public override IPodStub FindPodStub(string objecttype) { throw new NotImplementedException(); }
            public override INamedArrayStub FindNamedArrayStub(string objecttype) { throw new NotImplementedException(); }
        }

        public class CycleAFactory : DefOnlyServiceFactory
        {
            public CycleAFactory(RobotRaconteurNode node, ClientContext context) : base(node, context) { }
            public override string GetServiceName() { return "experimental.cyclea"; }
            public override string DefString()
            {
                return "service experimental.cyclea\n\nimport experimental.cycleb\n\nstruct sa\n    field int32 a\nend\n";
            }
        }

        public class CycleBFactory : DefOnlyServiceFactory
        {
            public CycleBFactory(RobotRaconteurNode node, ClientContext context) : base(node, context) { }
            public override string GetServiceName() { return "experimental.cycleb"; }
            public override string DefString()
            {
                return "service experimental.cycleb\n\nimport experimental.cyclea\n\nstruct sb\n    field int32 b\nend\n";
            }
        }

        static void RegisterIfMissing(RobotRaconteurNode node, ServiceFactory f)
        {
            try
            {
                node.GetServiceType(f.GetServiceName());
            }
            catch (Exception)
            {
                node.RegisterServiceType(f);
            }
        }

        static public async Task TestPullServiceDefinitionImportCycle()
        {
            // Definitions that import each other must be pulled once, not endlessly
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                RegisterIfMissing(nodes.node, new CycleAFactory(nodes.node, null));
                RegisterIfMissing(nodes.node, new CycleBFactory(nodes.node, null));

                var c = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));
                var cancel = new CancellationTokenSource(10000);
                var task = (Task<ServiceDefinition[]>)typeof(ClientContext)
                    .GetMethod("PullServiceDefinitionAndImports", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(c.RRContext, new object[] { "experimental.cyclea", cancel.Token });
                var defs = await task;
                RRAssert.AreEqual(defs.Length, 2);
                RRAssert.AreEqual(defs[0].Name, "experimental.cyclea");
                RRAssert.AreEqual(defs[1].Name, "experimental.cycleb");
                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("PullServiceDefsCycleTest.TestPullServiceDefinitionImportCycle passed");
        }
    }
}
