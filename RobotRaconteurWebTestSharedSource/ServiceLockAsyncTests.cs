using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService3;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
#if !ROBOTRACONTEUR_H5
    // Lock checks must use the requesting endpoint and user even when the
    // first access of a sub-object yields in an async objref getter (the
    // thread-static current endpoint/user are not valid after an await)
    public static class ServiceLockAsyncTests
    {
        class testroot3_async_o4 : testroot3_impl
        {
            public override async Task<obj4> get_o4(CancellationToken cancel = default(CancellationToken))
            {
                await Task.Delay(20).ConfigureAwait(false);
                return new obj4_impl();
            }
        }

        static ServiceSecurityPolicy CreatePolicy()
        {
            string authdata = "testuser1 0b91dec4fe98266a03b136b59219d0d6 objectlock";
            var p = new PasswordFileUserAuthenticator(authdata);
            var policies = new Dictionary<string, string>();
            policies.Add("requirevaliduser", "true");
            policies.Add("allowobjectlock", "true");
            return new ServiceSecurityPolicy(p, policies);
        }

        public static async Task RunTestLockAfterAsyncGetter()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                nodes.node.RegisterService("lock_async1", "com.robotraconteur.testing.TestService3", new testroot3_async_o4(), CreatePolicy());
                nodes.node.RegisterService("lock_async2", "com.robotraconteur.testing.TestService3", new testroot3_async_o4(), CreatePolicy());

                var cred = new Dictionary<string, object>();
                cred.Add("password", "testpass1");

                // ClientLockOp: lock a sub-object that has not been accessed yet
                var r1 = (ServiceStub)await nodes.client_node.ConnectService(nodes.GetServiceUrl("lock_async1"), "testuser1", cred);
                var m1 = new MessageEntry(MessageEntryType.ClientSessionOpReq, "RequestObjectLock");
                m1.ServicePath = "lock_async1.o4";
                var ret1 = await r1.RRContext.ProcessRequest(m1, default(CancellationToken));
                RRAssert.AreEqual(ret1.FindElement("return").CastData<string>(), "OK");
                await nodes.client_node.DisconnectService(r1);

                // check_lock: sub-object inherits the root lock held by the same user
                var r2 = (testroot3)await nodes.client_node.ConnectService(nodes.GetServiceUrl("lock_async2"), "testuser1", cred);
                await nodes.client_node.RequestObjectLock(r2, RobotRaconteurObjectLockFlags.USER_LOCK);
                var m2 = new MessageEntry(MessageEntryType.PropertyGetReq, "i_ind");
                m2.ServicePath = "lock_async2.o4";
                await ((ServiceStub)r2).RRContext.ProcessRequest(m2, default(CancellationToken));
                await nodes.client_node.DisconnectService(r2);
            }
        }
    }
#endif
}
