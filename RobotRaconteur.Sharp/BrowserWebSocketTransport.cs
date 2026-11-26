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
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /// <summary>
    /// Client-only transport that connects to Robot Raconteur services over WebSockets
    /// </summary>
    /// <remarks>
    /// <para>
    /// BrowserWebSocketTransport only uses System.Net.WebSockets.ClientWebSocket, so it works in hosts
    /// that do not support raw sockets, such as Blazor WebAssembly. It connects using "rr+ws" and
    /// "rr+wss" URLs. It cannot listen for connections and does not support discovery.
    /// </para>
    /// <para>
    /// The server must accept WebSocket connections, for example a TcpTransport server or an ASP.NET
    /// server using WebSockets.
    /// </para>
    /// </remarks>
    [PublicApi]
    public sealed class BrowserWebSocketTransport : Transport
    {
        private CancellationTokenSource transportcancel = new CancellationTokenSource();

        /// <inheritdoc/>
        public override bool IsServer { get { return false; } }
        /// <inheritdoc/>
        public override bool IsClient { get { return true; } }

        internal Dictionary<uint, AsyncStreamTransport> TransportConnections = new Dictionary<uint, AsyncStreamTransport>();

        /// <summary>
        /// The default receive timeout in milliseconds
        /// </summary>
        [PublicApi]
        public int DefaultReceiveTimeout { get; set; }
        /// <summary>
        /// The default connect timeout in milliseconds
        /// </summary>
        [PublicApi]
        public int DefaultConnectTimeout { get; set; }

        /// <inheritdoc/>
        public override string[] UrlSchemeString { get { return new string[] { "rr+ws", "rr+wss" }; } }

        private int m_HeartbeatPeriod = 5000;

        /// <summary>
        /// The heartbeat period in milliseconds. Must be at least 500.
        /// </summary>
        [PublicApi]
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

        /// <summary>
        /// Disable message format version 4
        /// </summary>
        [PublicApi]
        public bool DisableMessage4 { get; set; }

        /// <summary>
        /// The maximum message size in bytes
        /// </summary>
        [PublicApi]
        public uint MaxMessageSize { get; set; } = 12 * 1024 * 1024;

        /// <summary>
        /// Construct a new BrowserWebSocketTransport
        /// </summary>
        /// <param name="node">The node to use, or null for the default node</param>
        [PublicApi]
        public BrowserWebSocketTransport(RobotRaconteurNode node = null) : base(node)
        {
            DefaultReceiveTimeout = 15000;
            DefaultConnectTimeout = 2500;
            parent_adapter = new AsyncStreamTransportParentImpl(this);
        }

        /// <inheritdoc/>
        public override async Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel)
        {
            BrowserWebSocketClientTransport p = new BrowserWebSocketClientTransport(this);
            p.ReceiveTimeout = DefaultReceiveTimeout;
            await p.ConnectTransport(url, e, cancel).ConfigureAwait(false);

            return p;
        }

        /// <inheritdoc/>
        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            AsyncStreamTransport t = null;
            lock (this)
            {
                TransportConnections.TryGetValue(e.LocalEndpoint, out t);
            }
            t?.Close();
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public override bool CanConnectService(string url)
        {
            var u = TransportUtil.ParseConnectionUrl(url);
            return UrlSchemeString.Contains(u.scheme);
        }

        /// <inheritdoc/>
        public override async Task SendMessage(Message m, CancellationToken cancel)
        {
            if (m.header.SenderNodeID != node.NodeID)
            {
                throw new NodeNotFoundException("Invalid sender node");
            }

            AsyncStreamTransport t;
            lock (this)
            {
                if (!TransportConnections.TryGetValue(m.header.SenderEndpoint, out t))
                {
                    throw new ConnectionException("Connection to remote node has been closed");
                }
            }
            await t.SendMessage(m, cancel).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected internal override void MessageReceived(Message m)
        {
            node.MessageReceived(m);
        }

        /// <inheritdoc/>
        public override Task Close()
        {
            transportcancel.Cancel();

            AsyncStreamTransport[] cc;
            lock (this)
            {
                cc = TransportConnections.Values.ToArray();
            }

            foreach (AsyncStreamTransport c in cc)
            {
                try
                {
                    c.Close();
                }
                catch { }
            }

            lock (this)
            {
                TransportConnections.Clear();
            }

            base.Close();

            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public override void CheckConnection(uint endpoint)
        {
            AsyncStreamTransport t;
            lock (this)
            {
                if (!TransportConnections.TryGetValue(endpoint, out t))
                {
                    throw new ConnectionException("Transport not connected");
                }
            }
            t.CheckConnection(endpoint);
        }

        internal void RemoveTransportConnection(uint e)
        {
            lock (this)
            {
                TransportConnections.Remove(e);
            }

            FireTransportEventListener(TransportListenerEventType.TransportConnectionClosed, e);
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
                parent.RemoveTransportConnection(transport.LocalEndpoint);
            }

            public Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate()
            {
                // Client only, and TLS is provided by wss
                return null;
            }
        }

        internal readonly AsyncStreamTransportParent parent_adapter;

        /// <inheritdoc/>
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

        private BrowserWebSocketTransport parenttransport;

        public BrowserWebSocketClientTransport(BrowserWebSocketTransport c) : base(c.node, c.parent_adapter)
        {
            parenttransport = c;
            disable_message4 = parenttransport.DisableMessage4;
            max_message_size = parenttransport.MaxMessageSize;
        }

        private string connecturl = null;

        public async Task ConnectTransport(string url, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            this.connecturl = url;

            var u = TransportUtil.ParseConnectionUrl(url);

            if (u.host == "") throw new ConnectionException("Invalid connection URL for WebSocket");

            string http_scheme = "ws";
            if (u.scheme.EndsWith("wss"))
            {
                http_scheme = "wss";
            }

            var u2 = new Uri(url.ReplaceFirst(u.scheme + "://", http_scheme + "://"));

            NodeID target_nodeid = u.nodeid;
            string target_nodename = u.nodename;

            m_LocalEndpoint = e.LocalEndpoint;

            ClientWebSocket socket1 = new ClientWebSocket();
            socket1.Options.AddSubProtocol("robotraconteur.robotraconteur.com");
            await socket1.ConnectAsync(u2, cancel).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);

            websocket = socket1;

            m_Connected = true;
            var webstream = new WebSocketStreamWrapper(websocket);
            await ConnectStream(webstream, false, target_nodeid, target_nodename, false, false, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);

            lock (parenttransport)
            {
                parenttransport.TransportConnections.Add(LocalEndpoint, this);
            }
        }

        public override string GetConnectionURL()
        {
            return connecturl;
        }
    }
}
