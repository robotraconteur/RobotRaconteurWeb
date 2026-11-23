using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;
using com.robotraconteur.testing.TestService1;

namespace RobotRaconteurWebTest
{
    public static class ObjectLockUnauthenticatedTest
    {
        // Regression test: a client that has not authenticated has no AuthenticatedUser. A request it makes to a
        // locked member must fail with ObjectLockedException (as Core), not NullValue from a null dereference in
        // ServerContext.check_lock.
        public static async Task TestUnauthenticatedClientGetsObjectLocked()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);
                var o1 = await c.get_o1();

                // Lock o1 on the service side as another user, the way a service holds a lock for itself
                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                var skel = await ctx.GetObjectSkel("RobotRaconteurTestService.o1");
                var l = new ServerContext.ObjectLock("server", skel);
                l.AddSkel(skel);

                await RRAssert.ThrowsExceptionAsync<ObjectLockedException>(async () => { await o1.get_d1(); });
            }
            Console.WriteLine("ObjectLockUnauthenticatedTest.TestUnauthenticatedClientGetsObjectLocked passed");
        }
    }
}
