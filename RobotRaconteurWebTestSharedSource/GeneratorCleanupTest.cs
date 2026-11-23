using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService3;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
#if !ROBOTRACONTEUR_H5
    // Service-side generators must be destroyed when they finish, fail, are closed/aborted, or stay idle
    public static class GeneratorCleanupTest
    {
        static IDictionary GetGenerators(ServerContext ctx)
        {
            var skels = (Dictionary<string, ServiceSkel>)typeof(ServerContext)
                .GetField("skels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ctx);
            ServiceSkel root;
            lock (skels)
            {
                root = skels[ctx.ServiceName];
            }
            return (IDictionary)typeof(ServiceSkel)
                .GetField("generators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(root);
        }

        static void SetLastAccessTime(IDictionary generators, DateTime t)
        {
            lock (generators)
            {
                foreach (var g in generators.Values)
                {
                    typeof(GeneratorServerBase)
                        .GetField("last_access_time", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(g, t);
                }
            }
        }

        static int Count(IDictionary generators)
        {
            lock (generators)
            {
                return generators.Count;
            }
        }

        public static async Task TestGeneratorCleanup()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var ctx = nodes.node.RegisterService("gen_cleanup1", "com.robotraconteur.testing.TestService3", new testroot3_impl());
                var r = (testroot3)await nodes.client_node.ConnectService(nodes.GetServiceUrl("gen_cleanup1"));
                var generators = GetGenerators(ctx);

                // Runs to the end: destroyed by StopIteration
                var g1 = await r.gen_func1();
                RRAssert.AreEqual(Count(generators), 1);
                await RRAssert.ThrowsExceptionAsync<StopIterationException>(async () =>
                {
                    for (int i = 0; i < 20; i++)
                        await g1.Next();
                });
                RRAssert.AreEqual(Count(generators), 0);

                // Aborted: kept briefly so a following call gets the generator's own error, then destroyed
                var g2 = await r.gen_func1();
                await g2.Next();
                await g2.Abort();
                RRAssert.AreEqual(Count(generators), 1);
                await RRAssert.ThrowsExceptionAsync<OperationAbortedException>(() => g2.Next());
                RRAssert.AreEqual(Count(generators), 0);

                // Closed and never used again: destroyed by the periodic cleanup within 30 seconds
                var g3 = await r.gen_func1();
                await g3.Next();
                await g3.Close();
                SetLastAccessTime(generators, DateTime.UtcNow - TimeSpan.FromSeconds(31) - TimeSpan.FromMinutes(9.5));
                ctx.PeriodicCleanupTask();
                RRAssert.AreEqual(Count(generators), 0);

                // Idle: destroyed after 10 minutes without access, but not before
                var g4 = await r.gen_func1();
                ctx.PeriodicCleanupTask();
                RRAssert.AreEqual(Count(generators), 1);
                SetLastAccessTime(generators, DateTime.UtcNow - TimeSpan.FromMinutes(11));
                ctx.PeriodicCleanupTask();
                RRAssert.AreEqual(Count(generators), 0);

                await nodes.client_node.DisconnectService(r);
            }
            Console.WriteLine("GeneratorCleanupTest.TestGeneratorCleanup passed");
        }
    }
#endif
}
