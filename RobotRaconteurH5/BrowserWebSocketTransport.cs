// Copyright 2011-2024 Wason Technology, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
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

        public override string[] UrlSchemeString { get { return new string[] { "tcp", "rr+tcp", "rr+ws", "rr+wss" }; } }

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

        public bool DisableMessage4 { get; set; }


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
                    parent.RemoveTransportConnection(transport.LocalEndpoint);
                }
            }
        }

        internal readonly AsyncStreamTransportParent parent_adapter;

        public override string[] ServerListenUrls
        {
            get
            {
                return new string[0];
            }
        }

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
            disable_message4 = parenttransport.DisableMessage4;
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
                dynamic socket2 = socket1;
                socket2.options.addSubProtocol("robotraconteur.robotraconteur.com");
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

        ClientWebSocket websock;

        public WebSocketStreamWrapper(ClientWebSocket websocket)
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

        private async Task<int> DoBeginRead(byte[] buffer, int offset, int count)
        {
            var buffer2 = new byte[count];
            var r = await websock.ReceiveAsync(new ArraySegment<byte>(buffer2, 0, count), default(CancellationToken));
            if (r.MessageType != WebSocketMessageType.Binary) throw new IOException("Invalid websocket message type");
            Array.Copy(buffer2, 0, buffer, offset, r.Count);
            return r.Count;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return DoBeginRead(buffer, offset, count).AsApm(callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            dynamic asyncResult1 = asyncResult;
            return (int)asyncResult1.getResult();
        }

        private async Task<int> DoBeginWrite(byte[] buffer, int offset, int count)
        {
            if (count <= 65536)
            {
                await websock.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true);
            }
            else
            {
                int pos = 0;
                while (pos < count)
                {
                    if ((count - pos) <= 65536)
                    {
                        await websock.SendAsync(new ArraySegment<byte>(buffer, offset + pos, count - pos), WebSocketMessageType.Binary, true);
                        pos = count;
                    }
                    else
                    {
                        await websock.SendAsync(new ArraySegment<byte>(buffer, offset + pos, 65536), WebSocketMessageType.Binary, true);
                        pos += 65536;
                    }
                }
            }
            return count;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return DoBeginWrite(buffer, offset, count).AsApm(callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            int noop = ((Task<int>)asyncResult).Result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Invalid for browser");
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
            throw new InvalidOperationException("Invalid for browser");
        }

        public override void Close()
        {
            websock.CloseAsync(WebSocketCloseStatus.NormalClosure, "", default(CancellationToken)).IgnoreResult();
        }
    }
}
