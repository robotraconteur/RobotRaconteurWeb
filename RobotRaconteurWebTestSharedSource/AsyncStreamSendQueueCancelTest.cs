#if !ROBOTRACONTEUR_H5
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class AsyncStreamSendQueueCancelTest
    {
        // Stream whose first write blocks until the gate is opened; reads never complete
        class GateStream : Stream
        {
            public TaskCompletionSource<int> gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            public int writes = 0;
            public override Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken t) { return new TaskCompletionSource<int>().Task; }
            public override async Task WriteAsync(byte[] b, int o, int c, CancellationToken t)
            {
                if (Interlocked.Increment(ref writes) == 1) await gate.Task.ConfigureAwait(false);
            }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get => 0; set { } }
            public override void Flush() { }
            public override int Read(byte[] b, int o, int c) => 0;
            public override long Seek(long o, SeekOrigin s) => 0;
            public override void SetLength(long v) { }
            public override void Write(byte[] b, int o, int c) { }
        }

        class Parent : AsyncStreamTransportParent
        {
            public Task<Message> SpecialRequest(Message m) => Task.FromResult<Message>(null);
            public Task MessageReceived(Message m) => Task.FromResult(0);
            public void AddTransportConnection(uint e, AsyncStreamTransport t) { }
            public void RemoveTransportConnection(AsyncStreamTransport t) { }
            public Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate() => null;
        }

        class TestTransport : AsyncStreamTransport
        {
            public TestTransport(RobotRaconteurNode n) : base(n, new Parent()) { }
            public override string GetConnectionURL() => "test://";
            public Task Start(Stream s)
            {
                m_Connected = true;
                return ConnectStream(s, true, null, null, false, false, 600000, default(CancellationToken));
            }
        }

        static Message Msg(RobotRaconteurNode node, string name)
        {
            var m = new Message();
            m.header = new MessageHeader();
            m.header.SenderNodeID = node.NodeID;
            m.header.ReceiverNodeID = node.NodeID;
            m.entries.Add(new MessageEntry(MessageEntryType.EventReq, name));
            return m;
        }

        static async Task<bool> CompletesWithin(Task t, int timeout_ms)
        {
            return await Task.WhenAny(t, Task.Delay(timeout_ms)).ConfigureAwait(false) == t;
        }

        // Regression test: a message queued behind a running write whose cancellation token fires must
        // not stall the send queue. Previously the cancelled waiter was handed the send slot, nobody
        // continued sending and every later message on the connection (including heartbeats) queued forever.
        public static async Task TestCancelledQueuedSendDoesNotStall()
        {
            var node = new RobotRaconteurNode();
            node.NodeID = NodeID.NewUniqueID();
            var s = new GateStream();
            var t = new TestTransport(node);
            try
            {
                await t.Start(s);

                var t1 = t.SendMessage(Msg(node, "m1"), default(CancellationToken));
                // m1 is now blocked in the write
                RRAssert.IsFalse(await CompletesWithin(t1, 100));

                var cts = new CancellationTokenSource();
                var t2 = t.SendMessage(Msg(node, "m2"), cts.Token);
                cts.Cancel();
                // The queued m2 fails because of its cancelled token
                RRAssert.IsTrue(await CompletesWithin(t2, 5000));
                RRAssert.IsTrue(t2.IsFaulted || t2.IsCanceled);

                s.gate.SetResult(0);
                RRAssert.IsTrue(await CompletesWithin(t1, 5000));
                await t1;

                var t3 = t.SendMessage(Msg(node, "m3"), default(CancellationToken));
                RRAssert.IsTrue(await CompletesWithin(t3, 5000));
                await t3;
                var t4 = t.SendMessage(Msg(node, "m4"), default(CancellationToken));
                RRAssert.IsTrue(await CompletesWithin(t4, 5000));
                await t4;
                RRAssert.AreEqual(s.writes, 3);
            }
            finally
            {
                t.Close();
                node.Shutdown();
            }
            Console.WriteLine("AsyncStreamSendQueueCancelTest.TestCancelledQueuedSendDoesNotStall passed");
        }
    }
}
#endif
