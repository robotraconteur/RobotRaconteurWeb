#if !ROBOTRACONTEUR_H5
using System;
using System.Collections.Generic;
using System.Linq;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: the node setup assembly scan must register every service type it finds even
    // when one of them was already passed in service_types
    public static class ScanAssembliesTests
    {
        static List<Type> FactoryTypes()
        {
            var assignableType = typeof(ServiceFactory);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x =>
                {
                    try { return x.GetTypes(); }
                    catch (Exception) { return new Type[0]; }
                })
                .Where(t => assignableType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .Where(t => t != typeof(RobotRaconteurServiceIndex.RobotRaconteurServiceIndexFactory)).ToList();
        }

        public static void RunTestScanWithPreregisteredType()
        {
            var node = new RobotRaconteurNode();
            var types = FactoryTypes();
            RRAssert.IsTrue(types.Count >= 2);

            // The first type the scan finds is also passed explicitly
            var first = (ServiceFactory)Activator.CreateInstance(types[0], new object[] { node, null });
            using (var setup = new RobotRaconteurNodeSetup(node, new ServiceFactory[] { first }, true, null, 0, 0))
            {
                foreach (var t in types)
                {
                    var f = (ServiceFactory)Activator.CreateInstance(t, new object[] { node, null });
                    ServiceFactory f2;
                    RRAssert.IsTrue(node.TryGetServiceType(f.GetServiceName(), out f2));
                }
            }
        }
    }
}
#endif
