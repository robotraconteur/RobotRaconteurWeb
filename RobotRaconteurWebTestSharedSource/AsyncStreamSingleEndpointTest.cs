#if !ROBOTRACONTEUR_H5
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class AsyncStreamSingleEndpointTest
    {
        // Minimal AsyncStreamTransport peers over a loopback TCP socket pair
        class Parent : AsyncStreamTransportParent
        {
            public Func<Message, Message> special_request = null;
            public int add_count = 0;
            public Tuple<X509Certificate, X509CertificateCollection> cert = null;
            public Task<Message> SpecialRequest(Message m) => Task.FromResult(special_request?.Invoke(m));
            public Task MessageReceived(Message m) => Task.FromResult(0);
            public void AddTransportConnection(uint e, AsyncStreamTransport t) { Interlocked.Increment(ref add_count); }
            public void RemoveTransportConnection(AsyncStreamTransport t) { }
            public Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate() => cert;
        }

        class TestTransport : AsyncStreamTransport
        {
            public readonly Parent test_parent;
            public Func<MessageEntry, MessageHeader, MessageEntry> streamop_override = null;
            public TestTransport(RobotRaconteurNode n, Parent p) : base(n, p) { test_parent = p; ReceiveTimeout = 600000; }
            public override string GetConnectionURL() => "test://";
            public Task Start(Stream s, bool is_server, NodeID target_nodeid = null, bool starttls = false, CancellationToken cancel = default(CancellationToken))
            {
                m_Connected = true;
                return ConnectStream(s, is_server, target_nodeid, null, starttls, false, 600000, cancel);
            }
            public Task<object> DoStreamOp(string command, CancellationToken cancel) => StreamOp(command, null, cancel);
            protected override Task<MessageEntry> ProcessStreamOpRequest(MessageEntry request, MessageHeader header)
            {
                if (streamop_override != null) return Task.FromResult(streamop_override(request, header));
                return base.ProcessStreamOpRequest(request, header);
            }
        }

        // Read side waits for the gate to open
        class GatedReadStream : Stream
        {
            readonly Stream s;
            public TaskCompletionSource<int> gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            public GatedReadStream(Stream s, bool open) { this.s = s; if (open) gate.SetResult(0); }
            public override async Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken t)
            {
                await gate.Task.ConfigureAwait(false);
                return await s.ReadAsync(b, o, c, t).ConfigureAwait(false);
            }
            public override Task WriteAsync(byte[] b, int o, int c, CancellationToken t) => s.WriteAsync(b, o, c, t);
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get => 0; set { } }
            public override void Flush() { }
            public override int Read(byte[] b, int o, int c) => s.Read(b, o, c);
            public override long Seek(long o, SeekOrigin so) => 0;
            public override void SetLength(long v) { }
            public override void Write(byte[] b, int o, int c) => s.Write(b, o, c);
            public override void Close() { s.Close(); base.Close(); }
        }

        static async Task<Tuple<TcpClient, TcpClient>> SocketPair()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            try
            {
                var c = new TcpClient(AddressFamily.InterNetwork);
                var accept = l.AcceptTcpClientAsync();
                await c.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)l.LocalEndpoint).Port).ConfigureAwait(false);
                return Tuple.Create(c, await accept.ConfigureAwait(false));
            }
            finally
            {
                l.Stop();
            }
        }

        static RobotRaconteurNode NewNode()
        {
            var node = new RobotRaconteurNode();
            node.NodeID = NodeID.NewUniqueID();
            return node;
        }

        static async Task<bool> CompletesWithin(Task t, int timeout_ms)
        {
            return await Task.WhenAny(t, Task.Delay(timeout_ms)).ConfigureAwait(false) == t;
        }

        static async Task<bool> WaitFor(Func<bool> f, int timeout_ms)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeout_ms);
            while (!f())
            {
                if (DateTime.UtcNow > end) return false;
                await Task.Delay(20).ConfigureAwait(false);
            }
            return true;
        }

        static Message ConnectClientMessage(RobotRaconteurNode node, uint endpoint)
        {
            var m = new Message();
            m.header = new MessageHeader();
            m.header.SenderNodeID = node.NodeID;
            m.header.SenderEndpoint = endpoint;
            m.entries.Add(new MessageEntry(MessageEntryType.ConnectClient, "test_service"));
            return m;
        }

        // Regression test: a server connection takes only one endpoint. Previously every successful
        // ConnectClient reply on the same connection overwrote the connection's endpoint and added another
        // TransportConnections entry; only the last one was removed when the connection closed.
        public static async Task TestSecondConnectClientClosesConnection()
        {
            var client_node = NewNode();
            var server_node = NewNode();
            var p = await SocketPair();
            var client = new TestTransport(client_node, new Parent());
            var server = new TestTransport(server_node, new Parent());
            int server_ep = 100;
            server.test_parent.special_request = m =>
            {
                if (m.entries.Count != 1 || m.entries[0].EntryType != MessageEntryType.ConnectClient) return null;
                var r = new Message();
                r.header = new MessageHeader();
                r.header.SenderNodeID = server_node.NodeID;
                r.header.ReceiverNodeID = m.header.SenderNodeID;
                r.header.SenderEndpoint = (uint)Interlocked.Increment(ref server_ep);
                r.header.ReceiverEndpoint = m.header.SenderEndpoint;
                r.entries.Add(new MessageEntry(MessageEntryType.ConnectClientRet, m.entries[0].MemberName));
                return r;
            };
            try
            {
                await server.Start(p.Item2.GetStream(), true);
                await client.Start(p.Item1.GetStream(), true);

                await client.SendMessage(ConnectClientMessage(client_node, 10), default(CancellationToken));
                RRAssert.IsTrue(await WaitFor(() => server.test_parent.add_count == 1, 5000));
                RRAssert.AreEqual(server.LocalEndpoint, 101u);
                RRAssert.IsTrue(server.Connected);

                await client.SendMessage(ConnectClientMessage(client_node, 11), default(CancellationToken));
                // The second ConnectClient either closes the connection or, with the connection endpoint check,
                // is rejected as sent from the wrong endpoint; either way it must not add an endpoint
                await WaitFor(() => !server.Connected, 2000);
                RRAssert.AreEqual(server.test_parent.add_count, 1);
                RRAssert.AreEqual(server.LocalEndpoint, 101u);
            }
            finally
            {
                client.Close();
                server.Close();
                client_node.Shutdown();
                server_node.Shutdown();
            }
            Console.WriteLine("AsyncStreamSingleEndpointTest.TestSecondConnectClientClosesConnection passed");
        }
    }
}
#endif
