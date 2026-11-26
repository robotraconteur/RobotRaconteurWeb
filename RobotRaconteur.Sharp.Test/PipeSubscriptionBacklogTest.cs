using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSubTest.Pipes
{
    public static class PipeSubscriptionBacklogTest
    {
        public static async Task TestPipeSubscriptionMaxBacklog()
        {
            // SubscribePipe(max_backlog: 2) must keep only the newest two received packets
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(30000);
            var clientNode = new RobotRaconteurNode();
            var clientNodeSetup = new RobotRaconteurNodeSetup(clientNode, null, true, null, 0, testservice_impl.intra_client_flags);
            var server1 = new TestServiceImpl("server1", new NodeID("7c1d2e3f-4a5b-4c6d-8e9f-0a1b2c3d4e5f"));
            using (clientNodeSetup)
            using (server1)
            {
                var sub = clientNode.SubscribeServiceByType(new string[] { "experimental.pipe_sub_test.testobj" });
                var pipeSub = sub.SubscribePipe<double>("testpipe1", null, 2);
                await sub.GetDefaultClientWait<object>(cancel.Token);

                var sw = Stopwatch.StartNew();
                while (pipeSub.ActivePipeEndpointCount != 1 && sw.ElapsedMilliseconds < 10000)
                {
                    await Task.Delay(10);
                }
                RRAssert.AreEqual((int)pipeSub.ActivePipeEndpointCount, 1);

                for (int i = 1; i <= 5; i++)
                {
                    await server1.obj.TestPipe1SendPacket(i);
                }

                // Packets arrive in order: wait (bounded) until the oldest kept packet is 4
                sw = Stopwatch.StartNew();
                double oldest = 0;
                while (sw.ElapsedMilliseconds < 10000)
                {
                    // Peek only when a packet is queued: a peek that has to wait also dequeues on master (W26)
                    var r = pipeSub.Available > 0 ? await pipeSub.TryReceivePacketWait(0, true) : null;
                    if (r != null && r.Item1)
                    {
                        oldest = r.Item2;
                        if (oldest == 4.0)
                            break;
                    }
                    await Task.Delay(10);
                }
                RRAssert.AreEqual(oldest, 4.0);
                RRAssert.AreEqual((int)pipeSub.Available, 2);
                RRAssert.AreEqual(pipeSub.ReceivePacket(), 4.0);
                RRAssert.AreEqual(pipeSub.ReceivePacket(), 5.0);
                sub.Close();
            }
            Console.WriteLine("PipeSubscriptionBacklogTest.TestPipeSubscriptionMaxBacklog passed");
        }
    }
}
