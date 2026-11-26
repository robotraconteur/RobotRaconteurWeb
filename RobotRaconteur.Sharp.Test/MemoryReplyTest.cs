using System;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    static class MemoryReplyTest
    {
        static public async Task TestMemoryWriteReplyType()
        {
            // MemoryWrite requests must be answered with MemoryWriteRet
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (ServiceStub)await nodes.client_node.ConnectService(urls[0]);

                // Read element 0 of array memory m1 and write the same value back
                var rd = new MessageEntry(MessageEntryType.MemoryRead, "m1");
                rd.AddElement("memorypos", new ulong[] { 0 });
                rd.AddElement("count", new ulong[] { 1 });
                var rd_ret = await c.ProcessRequest(rd, CancellationToken.None);

                var wr = new MessageEntry(MessageEntryType.MemoryWrite, "m1");
                wr.AddElement("memorypos", new ulong[] { 0 });
                wr.AddElement("count", new ulong[] { 1 });
                wr.AddElement("data", rd_ret.FindElement("data").Data);
                var wr_ret = await c.ProcessRequest(wr, CancellationToken.None);
                RRAssert.AreEqual(wr_ret.EntryType, MessageEntryType.MemoryWriteRet);
            }
            Console.WriteLine("MemoryReplyTest.TestMemoryWriteReplyType passed");
        }
    }
}
