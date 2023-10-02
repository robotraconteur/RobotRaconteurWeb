using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using experimental.pipe_sub_test;
using RobotRaconteurTest;
using RobotRaconteurWeb;


namespace RobotRaconteurSubTest.Pipes
{
    public class testobj_impl : testobj_default_impl, IRRServiceObject
    {
        public testobj2_impl subobj = new testobj2_impl();
        public List<double> recv_packets = new List<double>();
               
        public override Task<testobj2> get_subobj(CancellationToken cancel = default)
        {
            return Task.FromResult((testobj2)subobj);
        }

        public void RRServiceObjectInit(ServerContext context, string servicePath)
        {
            testpipe2.PipeConnectCallback += delegate(Pipe<double>.PipeEndpoint pipe_ep)
            {
                pipe_ep.PacketReceivedEvent += delegate (Pipe<double>.PipeEndpoint pipe_ep2)
                {
                    while (pipe_ep2.Available > 0)
                    {
                        recv_packets.Add(pipe_ep2.ReceivePacket());
                    }
                };
            };           
        }

        public async Task TestPipe1SendPacket(double v)
        {
            await rrvar_testpipe1.SendPacket(v);
        }

        public override Pipe<double> testpipe2 { get; set; }
    }

    public class testobj2_impl : testobj2_default_impl
    {
        public async Task TestPipe3SendPacket(double v)
        {
            await rrvar_testpipe3.SendPacket(v);
        }
    }

    public class TestServiceImpl : IDisposable
    {
        public testobj_impl obj;
        private RobotRaconteurNode node;
        private RobotRaconteurNodeSetup nodeSetup;

        public TestServiceImpl(string nodeName, NodeID nodeId)
        {
            obj = new testobj_impl();
            node = new RobotRaconteurNode();
            node.NodeID = nodeId;


            nodeSetup = new RobotRaconteurNodeSetup(node, null, true, nodeName, 0, RobotRaconteurSubTest.testservice_impl.intra_server_flags);

            node.RegisterService("test_service", "experimental.pipe_sub_test", obj);
        }

        public void Dispose()
        {            
            nodeSetup?.Dispose();

            nodeSetup = null;
            node = null;
        }
    }

    public class SubscriptionPipeTests
    {
        public static async Task TestPipeSubscription()
        {
            var testServers = new Dictionary<string, NodeID>()
            {
                { "server1", new NodeID("0d694574-1ad8-4b9e-9aea-e881524fb451") },
                { "server2", new NodeID("e23ac123-4357-467e-b44b-4c9eb4ff7916") },
                { "server3", new NodeID("cb71939a-6c6c-43cc-b6be-070a76acec74") }
            };

            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(30000);

            var clientNode = new RobotRaconteurNode();

            var clientNodeSetup = new RobotRaconteurNodeSetup(clientNode, null, true, null, 0, testservice_impl.intra_client_flags);
            var server1 = new TestServiceImpl("server1", testServers["server1"]);
            using (clientNodeSetup)
            using (server1)
            {


                int packetRecvCount = 0;

                void PacketRecvFunc(PipeSubscription<double> pipe_sub2)
                {
                    packetRecvCount++;
                }

                var sub = clientNode.SubscribeServiceByType(new string[] { "experimental.pipe_sub_test.testobj" });
                var pipeSub = sub.SubscribePipe<double>("testpipe1");
                pipeSub.PipePacketReceived += PacketRecvFunc;
                await sub.GetDefaultClientWait<object>(cancel.Token);
                await Task.Delay(250);

                RRAssert.AreEqual((int)pipeSub.ActivePipeEndpointCount, (int)1);

                await server1.obj.TestPipe1SendPacket(1.0);
                await server1.obj.TestPipe1SendPacket(2.0);

                await Task.Delay(50);

                RRAssert.IsTrue(pipeSub.TryReceivePacket(out double packet) && packet == 1.0);
                RRAssert.AreEqual(pipeSub.ReceivePacket(), 2.0);

                async Task DelaySend()
                {
                    await Task.Delay(150);
                    await server1.obj.TestPipe1SendPacket(3.0);
                    await server1.obj.TestPipe1SendPacket(4.0);
                }

                var t = DelaySend();
                var res5 = await pipeSub.TryReceivePacketWait(1000);
                RRAssert.IsTrue(res5.Item1 && res5.Item2 == 3.0);

                await Task.Delay(5);

                RRAssert.AreEqual((int)pipeSub.Available, (int)1);
                var res1 = await pipeSub.TryReceivePacketWait(1000, true);
                RRAssert.IsTrue(res1.Item1 && res1.Item2 == 4.0);
                RRAssert.AreEqual((int)pipeSub.Available, (int)1);
                var res2 = await pipeSub.TryReceivePacketWait(1000);
                RRAssert.IsTrue(res2.Item1 && res2.Item2 == 4.0);

                var pipeSub2 = sub.SubscribePipe<double>("testpipe2");
                await Task.Delay(500);
                RRAssert.AreEqual((int)pipeSub2.ActivePipeEndpointCount, (int)1);

                pipeSub2.AsyncSendPacketAll(5.0);
                pipeSub2.AsyncSendPacketAll(6.0);

                await Task.Delay(50);

                RRAssert.AreEqual((int)server1.obj.recv_packets.Count, 2);

                var pipeSub3 = sub.SubscribePipe<double>("testpipe3", "*.subobj");
                await Task.Delay(100);
                RRAssert.AreEqual<int>((int)pipeSub3.ActivePipeEndpointCount, 1);

                await server1.obj.subobj.TestPipe3SendPacket(7.0);
                await server1.obj.subobj.TestPipe3SendPacket(8.0);

                var res3 = await pipeSub3.TryReceivePacketWait(1000);
                RRAssert.IsTrue(res3.Item1 && res3.Item2 == 7.0);
                var res4 = await pipeSub3.TryReceivePacketWait(1000);
                RRAssert.IsTrue(res4.Item1 && res4.Item2 == 8.0);
            }
        }

    }

}