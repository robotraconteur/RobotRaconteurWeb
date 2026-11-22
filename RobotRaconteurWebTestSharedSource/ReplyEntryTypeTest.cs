#if !ROBOTRACONTEUR_H5
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class ReplyEntryTypeTest
    {
        static public async Task TestUnhandledReplyEntryType()
        {
            // An unhandled or failing reply-type (even) entry must not throw out of ProcessMessageEntry
            // (which drops the replies to the other entries of the message) and must not be answered
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (testroot)await nodes.client_node.ConnectService(urls[0]);

                var ctx = nodes.node.GetService("RobotRaconteurTestService");
                var eps = (Dictionary<uint, ServerEndpoint>)typeof(ServerContext)
                    .GetField("client_endpoints", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ctx);
                ServerEndpoint ep;
                lock (eps)
                {
                    ep = eps.Values.First();
                }

                var m1 = new MessageEntry(MessageEntryType.PropertyGetRes, "d1");
                m1.ServicePath = ctx.ServiceName;
                RRAssert.IsTrue(await ctx.ProcessMessageEntry(m1, ep) == null);

                var m2 = new MessageEntry(MessageEntryType.PipePacketRet, "p1");
                m2.ServicePath = ctx.ServiceName + ".nonexistent";
                RRAssert.IsTrue(await ctx.ProcessMessageEntry(m2, ep) == null);

                // An unknown request is still answered with an error
                var m3 = new MessageEntry(MessageEntryType.PropertyGetReq, "d1");
                m3.ServicePath = ctx.ServiceName + ".nonexistent";
                var r3 = await ctx.ProcessMessageEntry(m3, ep);
                RRAssert.IsTrue(r3 != null && r3.EntryType == MessageEntryType.PropertyGetRes && r3.Error != MessageErrorType.None);

                await nodes.client_node.DisconnectService(c);
            }
            Console.WriteLine("ReplyEntryTypeTest.TestUnhandledReplyEntryType passed");
        }
    }
}
#endif
