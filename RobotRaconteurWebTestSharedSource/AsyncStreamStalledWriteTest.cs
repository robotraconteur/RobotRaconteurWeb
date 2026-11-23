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
    // The receive timeout must close a connection whose writes never complete (the peer stopped reading)
    public class AsyncStreamStalledWriteTest
    {
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
            public TestTransport(RobotRaconteurNode n, int receive_timeout) : base(n, new Parent())
            {
                ReceiveTimeout = receive_timeout;
            }
            public override string GetConnectionURL() => "test://";
            public Task Start(Stream s, bool is_server, int heartbeat_period)
            {
                m_Connected = true;
                return ConnectStream(s, is_server, null, null, false, false, heartbeat_period, default(CancellationToken));
            }
        }

        // Writes pass through until Stall(); after that they never complete (until the stream is closed)
        class StallableStream : Stream
        {
            readonly Stream s;
            volatile bool stall;
            readonly TaskCompletionSource<int> stalled =
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            public StallableStream(Stream s) { this.s = s; }
            public void Stall() { stall = true; }
            public override async Task WriteAsync(byte[] b, int o, int c, CancellationToken t)
            {
                if (stall)
                {
                    await stalled.Task.ConfigureAwait(false);
                    return;
                }
                await s.WriteAsync(b, o, c, t).ConfigureAwait(false);
            }
            public override Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken t) => s.ReadAsync(b, o, c, t);
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
            public override void Close()
            {
                stalled.TrySetException(new IOException("Stream closed"));
                s.Close();
                base.Close();
            }
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

        static async Task<bool> WaitFor(Func<bool> f, int timeout_ms)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeout_ms);
            while (!f())
            {
                if (DateTime.UtcNow > end) return false;
                await Task.Delay(50).ConfigureAwait(false);
            }
            return true;
        }

        static async Task RunCase(int client_heartbeat_period, string name)
        {
            var client_node = NewNode();
            var server_node = NewNode();
            var p = await SocketPair();
            var client_stream = new StallableStream(p.Item1.GetStream());
            var client = new TestTransport(client_node, 2000);
            // The server never sends unprompted, and with the client's writes stalled it receives nothing to answer
            var server = new TestTransport(server_node, 600000);
            try
            {
                await server.Start(p.Item2.GetStream(), true, 600000);
                await client.Start(client_stream, false, client_heartbeat_period);
                RRAssert.IsTrue(client.Connected);

                client_stream.Stall();
                if (client_heartbeat_period > 10000)
                {
                    // Put a write in flight; it never completes
                    var m = new Message();
                    m.header = new MessageHeader();
                    m.header.SenderNodeID = client_node.NodeID;
                    m.entries.Add(new MessageEntry(MessageEntryType.ConnectionTest, ""));
                    client.SendMessage(m, default(CancellationToken)).ContinueWith(t => { var e = t.Exception; });
                }

                // Receive timeout 2 s, checked every 500 ms
                RRAssert.IsTrue(await WaitFor(() => !client.Connected, 10000));
            }
            finally
            {
                client.Close();
                server.Close();
                client_node.Shutdown();
                server_node.Shutdown();
            }
            Console.WriteLine("AsyncStreamStalledWriteTest." + name + " passed");
        }

        // Regression test: the heartbeat loop compared tlastsend - tlastrec (tlastsend freezes when a write stalls)
        // and awaited its own heartbeat send (which queues behind the stalled write), so the receive timeout never
        // closed the connection
        public static async Task TestStalledWriteReceiveTimeout()
        {
            await RunCase(600000, "TestStalledWriteReceiveTimeout(no heartbeat)");
            await RunCase(1000, "TestStalledWriteReceiveTimeout(heartbeat)");
        }
    }
}
#endif
