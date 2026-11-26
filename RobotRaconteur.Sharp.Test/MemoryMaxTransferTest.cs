using System;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    static class MemoryMaxTransferTest
    {
        static public async Task TestMemoryReadCountLimit()
        {
            // The service must reject a memory read larger than MemoryMaxTransferSize before allocating the reply
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var urls = nodes.GetServiceUrl("RobotRaconteurTestService");
                var c = (ServiceStub)await nodes.client_node.ConnectService(urls[0]);

                // 16M doubles (128 MiB) from array memory m1
                var rd = new MessageEntry(MessageEntryType.MemoryRead, "m1");
                rd.AddElement("memorypos", new ulong[] { 0 });
                rd.AddElement("count", new ulong[] { 16 * 1024 * 1024 });
                long before = GC.GetTotalAllocatedBytes(true);
                await RRAssert.ThrowsExceptionAsync<ArgumentException>(() => c.ProcessRequest(rd, CancellationToken.None));
                long allocated = GC.GetTotalAllocatedBytes(true) - before;
                RRAssert.IsTrue(allocated < 64L * 1024 * 1024);

                // 4096 x 4096 doubles from multidim memory m2
                var rd2 = new MessageEntry(MessageEntryType.MemoryRead, "m2");
                rd2.AddElement("memorypos", new ulong[] { 0, 0 });
                rd2.AddElement("count", new ulong[] { 4096, 4096 });
                before = GC.GetTotalAllocatedBytes(true);
                await RRAssert.ThrowsExceptionAsync<ArgumentException>(() => c.ProcessRequest(rd2, CancellationToken.None));
                allocated = GC.GetTotalAllocatedBytes(true) - before;
                RRAssert.IsTrue(allocated < 64L * 1024 * 1024);
            }
            Console.WriteLine("MemoryMaxTransferTest.TestMemoryReadCountLimit passed");
        }
    }
}
