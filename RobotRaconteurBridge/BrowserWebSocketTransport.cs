using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using RobotRaconteur.Extensions;
using System.IO;

namespace RobotRaconteur
{
    public sealed class BrowserWebSocketTransport : Transport
    {

        //protected int Port {get {return m_Port;}}
        private int m_Port;

        private bool transportopen = false;
        private CancellationTokenSource transportcancel = new CancellationTokenSource();

        public override bool IsServer { get { return true; } }
        public override bool IsClient { get { return true; } }

        internal Dictionary<uint, AsyncStreamTransport> TransportConnections = new Dictionary<uint, AsyncStreamTransport>();

        public int DefaultReceiveTimeout { get; set; }
        public int DefaultConnectTimeout { get; set; }

        public bool AcceptWebSockets { get; set; }

        public override string[] UrlSchemeString { get { return new string[] { "tcp", "rr+tcp", "rr+ws", "rr+wss"}; } }

        private int m_HeartbeatPeriod = 5000;

        public int HeartbeatPeriod
        {
            get
            {
                return m_HeartbeatPeriod;
            }
            set
            {
                if (value < 500) throw new InvalidOperationException();
                m_HeartbeatPeriod = value;
            }
        }
                

        public BrowserWebSocketTransport(RobotRaconteurNode node = null) : base(node)
        {
            DefaultReceiveTimeout = 15000;
            DefaultConnectTimeout = 2500;
            parent_adapter = new AsyncStreamTransportParentImpl(this);
            
        }


        public override async Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel)
        {
            BrowserWebSocketClientTransport p = new BrowserWebSocketClientTransport(this);
            p.ReceiveTimeout = DefaultReceiveTimeout;
            await p.ConnectTransport(url, e, cancel);

            return p;
        }

        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            if (TransportConnections.ContainsKey(e.LocalEndpoint))
                TransportConnections[e.LocalEndpoint].Close();
            return Task.FromResult(0);
        }

        public override bool CanConnectService(string url)
        {
            var u = TransportUtil.ParseConnectionUrl(url);
            if (UrlSchemeString.Contains(u.scheme))
                return true;
            //if (u.Host != "localhost") return false;

            return false;
        }

        public override async Task SendMessage(Message m, CancellationToken cancel)
        {
            if (m.header.SenderNodeID != node.NodeID)
            {
                throw new NodeNotFoundException("Invalid sender node");
            }
            try
            {
                await TransportConnections[m.header.SenderEndpoint].SendMessage(m, cancel);
            }
            catch (KeyNotFoundException)
            {
                throw new ConnectionException("Connection to remote node has been closed");
            }
        }


        protected internal override void MessageReceived(Message m)
        {


            node.MessageReceived(m);

        }

        public override Task Close()
        {
            transportopen = false;
            transportcancel.Cancel();

            AsyncStreamTransport[] cc = TransportConnections.Values.ToArray();

            foreach (AsyncStreamTransport c in cc)
            {
                try
                {
                    c.Close();
                }
                catch { }
            }


            try
            {

                TransportConnections.Clear();
            }
            catch { }

            base.Close();

            return Task.FromResult(0);
        }

        public override void CheckConnection(uint endpoint)
        {
            try
            {
                TransportConnections[endpoint].CheckConnection(endpoint);
            }
            catch (KeyNotFoundException)
            {
                throw new ConnectionException("Transport not connected");
            }
        }
                
        internal void RemoveTransportConnection(uint e)
        {
            TransportConnections.Remove(e);

            FireTransportEventListener(TransportListenerEventType.TransportConnectionClosed, e);
        }


        public override uint TransportCapability(string name)
        {
            return base.TransportCapability(name);
        }

        private class AsyncStreamTransportParentImpl : AsyncStreamTransportParent
        {
            BrowserWebSocketTransport parent;

            public AsyncStreamTransportParentImpl(BrowserWebSocketTransport parent)
            {
                this.parent = parent;
            }

            public Task<Message> SpecialRequest(Message m)
            {
                return parent.SpecialRequest(m);
            }

            public Task MessageReceived(Message m)
            {
                parent.MessageReceived(m);
                return Task.FromResult(0);
            }

            public void AddTransportConnection(uint endpoint, AsyncStreamTransport transport)
            {
                lock (parent)
                {
                    parent.TransportConnections.Add(endpoint, transport);
                }
            }

            public void RemoveTransportConnection(AsyncStreamTransport transport)
            {
                lock (parent)
                {
                    parent.TransportConnections.Remove(transport.LocalEndpoint);
                }
            }           
        }

        internal readonly AsyncStreamTransportParent parent_adapter;

    }



    sealed class BrowserWebSocketClientTransport : AsyncStreamTransport
    {

        private ClientWebSocket websocket;
        //public NetworkStream netstream;

        private BrowserWebSocketTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        public BrowserWebSocketClientTransport(BrowserWebSocketTransport c) : base(c.node, c.parent_adapter)
        {
            parenttransport = c;
        }

        private string connecturl = null;

        public async Task ConnectTransport(string url, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            this.connecturl = url;

            var u = TransportUtil.ParseConnectionUrl(url);

            if (u.host == "") throw new ConnectionException("Invalid connection URL for TCP");
            
            await ConnectWebsocketTransport(url, e, cancel);
            return;
        }

        private async Task ConnectWebsocketTransport(string url, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            var u = TransportUtil.ParseConnectionUrl(url);
            /*Uri u = new Uri(url);
                        
            string ap = Uri.UnescapeDataString(u.AbsolutePath);
            if (ap[0] == '/')
                ap = ap.Remove(0, 1);

            string[] s = ap.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);*/

            string http_scheme = "ws";
            if (u.scheme.EndsWith("wss"))
            {
                http_scheme = "wss";
            }            

            var u2 = new Uri(url.ReplaceFirst(u.scheme + "://", http_scheme + "://"));


            NodeID target_nodeid = null;
            string target_nodename = null;

            m_LocalEndpoint = e.LocalEndpoint;

            {
                //socket = new TcpClient(u.Host, u.Port);
                websocket = null;

                ClientWebSocket socket1 = new ClientWebSocket();                
                socket1.Options.AddSubProtocol("robotraconteur.robotraconteur.com");
                await socket1.ConnectAsync(u2, cancel).AwaitWithTimeout(parenttransport.DefaultConnectTimeout);

                websocket = socket1;

            }

            m_Connected = true;
            var webstream = new WebSocketStreamWrapper(websocket);
            await ConnectStream(webstream, false, target_nodeid, target_nodename, false, false, parenttransport.HeartbeatPeriod, cancel);

            parenttransport.TransportConnections.Add(LocalEndpoint, this);


        }

        public override string GetConnectionURL()
        {
            return connecturl;
        }
    }

    class WebSocketStreamWrapper : Stream
    {

        WebSocket websock;

        public WebSocketStreamWrapper(WebSocket websocket)
        {
            websock = websocket;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {

        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return ReadAsync(buffer, offset, count).AsApm(callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).Result;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return WriteAsync(buffer, offset, count).AsApm(callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            int noop = ((Task<int>)asyncResult).Result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return websock.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), default(CancellationToken)).Result.Count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            websock.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, false, default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var r = await websock.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
            if (r.MessageType != WebSocketMessageType.Binary) throw new IOException("Invalid websocket message type");
            return r.Count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count <= 4096)
            {
                await websock.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                return;
            }

            int pos = 0;
            while (pos < count)
            {
                int c = 4096;
                if (pos + 4096 > count)
                {
                    c = count - pos;
                }

                await websock.SendAsync(new ArraySegment<byte>(buffer, offset + pos, c), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);

                pos += c;
            }

        }

        public override void Close()
        {
            websock.CloseAsync(WebSocketCloseStatus.NormalClosure, "", default(CancellationToken)).IgnoreResult();
        }
    }
}
