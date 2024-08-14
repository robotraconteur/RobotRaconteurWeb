using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using System.Runtime.InteropServices;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    /**
    <summary>
            Transport for intra-process communication
            </summary>
            <remarks>
            <para>
                It is recommended that ClientNodeSetup, ServerNodeSetup, or SecureServerNodeSetup
                be used to construct this class.
            </para>
            <para>
                See robotraconteur_url for more information on URLs.
            </para>
            <para>
                The IntraTransport implements transport connections between nodes running
                within the same process. This is often true for simulation environments, where
                there may be multiple simulated devices running within the simulation. The
                IntraTransport uses a singleton to keep track of the different nodes running
                in the same process, and to form connections. The singleton also implements
                discovery updates.
            </para>
            <para>
                The use of RobotRaconteurNodeSetup and subclasses is recommended to construct
                transports.
            </para>
            <para> The transport must be registered with the node using
                RobotRaconteurNode.RegisterTransport() after construction if node
                setup is not used.
            </para>
            </remarks>
    */
        [PublicApi]
    public sealed class IntraTransport : Transport
    {

        internal static List<WeakReference<IntraTransport>> peer_transports = new List<WeakReference<IntraTransport>>();

        private bool transportopen = false;
        private bool serverstarted = false;

        /// <inheretdoc/>
        public override bool IsServer { get { return serverstarted; } }
        /// <inheretdoc/>
        public override bool IsClient { get { return true; } }

        internal Dictionary<uint, IntraTransportConnection> TransportConnections = new Dictionary<uint, IntraTransportConnection>();

        /// <summary>
        /// The default time to wait for a message before closing the connection. Units in ms
        /// </summary>
        [PublicApi]
        public int DefaultReceiveTimeout { get; set; }
        /// <summary>
        /// The default time to wait for a connection to be made before timing out. Units in ms
        /// </summary>
        [PublicApi]
        public int DefaultConnectTimeout { get; set; }

        /// <summary>
        /// The "scheme" portion of the url that this transport corresponds to ("intra" in this case)
        /// </summary>
        [PublicApi]
        public override string[] UrlSchemeString { get { return new string[] { "rr+intra" }; } }

        private int m_HeartbeatPeriod = 5000;

        /// <summary>
        /// Connection test heartbeat period in milliseconds
        /// </summary>
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

        internal void Init()
        {
            lock(this)
            {
                if(transportopen)
                {
                    return;
                }
                transportopen = true;
            }

            lock(peer_transports)
            {
                peer_transports.Add(new WeakReference<IntraTransport>(this));
            }
        }
        /**
        <summary>
        Construct a new IntraTransport for a non-default node. Must be registered with node using
        node.RegisterTransport()
        </summary>
        <remarks>None</remarks>
        <param name="node">The node to use with the transport. Defaults to RobotRaconteurNode.s</param>
        */
        [PublicApi]
        public IntraTransport(RobotRaconteurNode node = null)
            : base(node)
        {
            DefaultReceiveTimeout = 15000;
            DefaultConnectTimeout = 2500;
        }

        /// <inheretdoc/>

        public override async Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel)
        {
            var url_res = TransportUtil.ParseConnectionUrl(url);

            if (string.IsNullOrEmpty(url_res.nodename) && url_res.nodeid.IsAnyNode)
            {
                throw new ConnectionException("NodeID and/or NodeName must be specified for IntraTransport");
            }

            var host = url_res.host;

            if (url_res.port != -1)
            {
                throw new ConnectionException("Port must not be specified for IntraTransport");
            }

            if (!string.IsNullOrEmpty(url_res.path) && url_res.path != "/")
            {
                throw new ConnectionException("IntraTransport must not contain a path");
            }

            if (!string.IsNullOrEmpty(url_res.host))
            {
                throw new ConnectionException("IntraTransport must not contain a hostname");
            }

            IntraTransport peer_transport = null;

            lock(peer_transports)
            {
                foreach( var peer_transport_w in peer_transports)
                {
                    if (!peer_transport_w.TryGetTarget(out var transport))
                    {
                        continue;
                    }

                    if (!transport.IsServer)
                    {
                        continue;
                    }

                    if (!transport.TryGetNodeInfo(out var p_node_id, out var p_node_name, out var peer_nonce ))
                    {
                        continue;
                    }

                    if (!url_res.nodeid.IsAnyNode && !string.IsNullOrEmpty(url_res.nodename))
                    {
                        if (url_res.nodeid == p_node_id && url_res.nodename == p_node_name)
                        {
                            peer_transport = transport;
                            break;
                        }
                    }

                    if (!url_res.nodeid.IsAnyNode )
                    {
                        if (url_res.nodeid == p_node_id)
                        {
                            peer_transport = transport;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(url_res.nodename))
                    {
                        if (url_res.nodename == p_node_name)
                        {
                            peer_transport = transport;
                            break;
                        }
                    }
                }
            }

            if (peer_transport == null)
            {
                throw new ConnectionException("Could not connect to service");
            }

            string noden;
            if (!(url_res.nodeid.IsAnyNode && !string.IsNullOrEmpty(url_res.nodename)))
            {
                noden = url_res.nodeid.ToString();
            }
            else
            {
                noden = url_res.nodename;
            }

            var local_connection = new IntraTransportConnection(this, false, e.LocalEndpoint);
            var peer_connection = new IntraTransportConnection(peer_transport, true, 0);
            local_connection.SetPeer(peer_connection);
            peer_connection.SetPeer(local_connection);

            lock (this)
            {
                TransportConnections.Add(e.LocalEndpoint, local_connection);
            }

            return local_connection;

        }

        /// <inheretdoc/>
        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            if (TransportConnections.ContainsKey(e.LocalEndpoint))
                TransportConnections[e.LocalEndpoint].Close();
            return Task.FromResult(0);
        }
        /**
        <summary>
        Start the server to listen for incoming client connections
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public void StartServer()
        {
            _ = node.NodeID;
            _ = node.NodeName;
            serverstarted = true;
            Init();
            SendNodeDiscovery();
            DiscoverAllNodes();

        }
        /**
        <summary>
        Start the transport for use by clients
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public void StartClient()
        {
            serverstarted = false;
            Init();
            DiscoverAllNodes();
        }
#pragma warning disable 1591
        protected internal bool TryGetNodeInfo(out NodeID node_id, out string node_name, out string service_nonce)
        {
            if (!node.TryGetNodeID(out node_id))
            {
                node_id = default;
                service_nonce = default;
                node_name = default;
                return false;
            }

            if (!node.TryGetNodeName(out node_name))
            {
                node_id = default;
                service_nonce = default;
                node_name = default;
                return false;
            }

            service_nonce = node.ServiceStateNonce;

            return true;
        }
#pragma warning restore 1591

        CancellationTokenSource close_token = new CancellationTokenSource();
        TaskCompletionSource<int> close_task = new TaskCompletionSource<int>();


        /// <summary>
        /// Returns true if url has scheme "rr+intra"
        /// </summary>
        /// <param name="url">The url to check</param>
        /// <returns>True if url has scheme "rr+intra"</returns>
        [PublicApi]
        public override bool CanConnectService(string url)
        {
            var u = TransportUtil.ParseConnectionUrl(url);
            if (u.scheme != "rr+intra") return false;

            return true;
        }

        /// <inheretdoc/>
        public override async Task SendMessage(Message m, CancellationToken cancel)
        {
            if (m.header.SenderNodeID != node.NodeID)
            {
                throw new NodeNotFoundException("Invalid sender node");
            }
            m.ComputeSize();
            try
            {
                await TransportConnections[m.header.SenderEndpoint].SendMessage(m, cancel).ConfigureAwait(false);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new ConnectionException("Connection to remote node has been closed");
            }
        }


        /// <inheretdoc/>
        protected internal override void MessageReceived(Message m)
        {
            node.MessageReceived(m);
        }

        /**
        <summary>
        Close the transport. Done automatically by node shutdown.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public override Task Close()
        {
            lock (this)
            {
                transportopen = false;
            }

            lock(peer_transports)
            {
                peer_transports.RemoveAll(delegate (WeakReference<IntraTransport> x)
                {
                    if (!x.TryGetTarget(out var t2))
                    {
                        return true;
                    }
                    return object.ReferenceEquals(t2,this);

                });
            }

            var cc = TransportConnections.Values.ToArray();

            foreach (var c in cc)
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

            close_task.TrySetResult(0);
            close_token.Cancel();


            base.Close();

            return Task.FromResult(0);
        }

        /// <inheretdoc/>
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
            lock (this)
            {
                TransportConnections.Remove(e);
            }
            FireTransportEventListener(TransportListenerEventType.TransportConnectionClosed, e);
        }

        internal void AddTransportConnection(uint endpoint, IntraTransportConnection transport)
        {
            lock (this)
            {
                TransportConnections.Add(endpoint, transport);
            }
        }

        internal void RemoveTransportConnection(IntraTransportConnection transport)
        {           
              RemoveTransportConnection(transport.LocalEndpoint);
        }


        /// <inheretdoc/>
        public override uint TransportCapability(string name)
        {
            return base.TransportCapability(name);
        }
#pragma warning disable 1591
        public override Task<List<NodeDiscoveryInfo>> GetDetectedNodes(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            var o = new List<NodeDiscoveryInfo>();


            return Task.FromResult(o);
        }
       
        protected void SendNodeDiscovery()
        {
            if (!serverstarted)
            {
                return;
            }

            NodeDiscoveryInfo info = new NodeDiscoveryInfo();
            if (!node.TryGetNodeID(out info.NodeID))
            {
                return;
            }

            node.TryGetNodeName(out info.NodeName);

            info.ServiceStateNonce = node.ServiceStateNonce;

            var u = new NodeDiscoveryInfoURL();
            u.URL = "rr+intra:///?nodeid=" + info.NodeID.ToString("B") + "&service=RobotRaconteurServiceIndex";
            u.LastAnnounceTime = DateTime.UtcNow;
            info.URLs.Add(u);

            lock(peer_transports)
            {
                foreach (var transport1 in peer_transports)
                {
                    if (!transport1.TryGetTarget(out var transport))
                    {
                        continue;
                    }

                    transport.NodeDetected(info);
                }
            }
        }

        protected internal void NodeDetected(NodeDiscoveryInfo info)
        {
            Task.Run(delegate ()
            {
                try
                {
                    node.NodeDetected(info);
                }
                catch { }
            }).IgnoreResult();
        }

        protected void DiscoverAllNodes()
        {
            var discovered_info = new List<NodeDiscoveryInfo>();

            lock (peer_transports)
            {
                foreach (var transport1 in peer_transports)
                {
                    if (!transport1.TryGetTarget(out var transport))
                    {
                        continue;
                    }

                    var n = new NodeDiscoveryInfo();
                    if (transport.TryGetNodeInfo(out n.NodeID, out n.NodeName, out n.ServiceStateNonce))
                    {

                        var u = new NodeDiscoveryInfoURL();
                        u.URL = "rr+intra:///?nodeid=" + n.NodeID.ToString("B") + "&service=RobotRaconteurServiceIndex";
                        u.LastAnnounceTime = DateTime.UtcNow;
                        n.URLs.Add(u);
                        discovered_info.Add(n);
                    }
                }
            }

            foreach (var n in discovered_info)
            {
                Task.Run(delegate ()
                {
                    try
                    {
                        node.NodeDetected(n);
                    }
                    catch (Exception ex)
                    {
                        LogDebug(String.Format("Error sending node discovery from IntraTransport: {0}",ex.Message), node, RobotRaconteur_LogComponent.Transport, "IntraTransport");
                    }
                });
            }
        }

        public override string[] ServerListenUrls
        {
            get
            {
                if (!serverstarted)
                {
                    return new string[0];
                }
                return new string[] { string.Format("rr+intra:///?nodeid={0}", node.NodeID.ToString("D")) };
            }
        }
#pragma warning restore 1591
    }



    sealed class IntraTransportConnection : ITransportConnection
    {
        private WeakReference<IntraTransportConnection> peer_connection;
        bool connected = false;
        List<Message> recv_queue = new List<Message>();
        bool recv_queue_post_requested = false;
        //private Stream socket;
        //public NetworkStream netstream;
        bool server = false;
        RobotRaconteurNode node;
        bool closed = false;

        private IntraTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        /// <summary>
        /// Creates a IntraClientTransport with parent IntraTransport
        /// </summary>
        /// <param name="c">Parent transport</param>
        [PublicApi]
        public IntraTransportConnection(IntraTransport parent, bool server, uint local_endpoint)
        {
            node = parent.node;
            parenttransport = parent;
            this.local_endpoint = local_endpoint;
            this.server = server;
            if (server)
            {
                this.connecturl = "rr+intra:///";
            }
        }

        void AcceptMessage(Message m)
        {
            lock(recv_queue)
            {
                recv_queue.Add(m);
                if (!recv_queue_post_requested)
                {
                    _ = Task.Run(() => { ProcessNextMessage(); });
                }
            }
        }

        void ProcessNextMessage()
        {
            Message m;

            lock(recv_queue)
            {
                if (recv_queue.Count == 0)
                {
                    recv_queue_post_requested = false;
                    return;
                }
                m = recv_queue.First();
                recv_queue.RemoveAt(0);
                if (recv_queue.Count == 0)
                {
                    recv_queue_post_requested = false;
                }
                else
                {
                    _ = Task.Run(() => { ProcessNextMessage(); });
                }
            }

            _ = ProcessMessage(m);
        }

        internal async Task ProcessMessage(Message mes)
        {
            try
            {
                NodeID RemoteNodeID1;
                uint local_ep;
                uint remote_ep;
                lock (this)
                {
                    RemoteNodeID1 = remote_node_id;
                    local_ep = this.local_endpoint;
                    remote_ep = this.remote_endpoint;
                }

                
                Message ret = await this.parenttransport.SpecialRequest(mes).ConfigureAwait(false);
                if (ret != null)
                {
                    try
                    {
                        if ((mes.entries[0].EntryType == MessageEntryType.ConnectionTest || mes.entries[0].EntryType == MessageEntryType.ConnectionTestRet))
                        {
                            if (mes.entries[0].Error != MessageErrorType.None)
                            {
                                Close();
                                return;
                            }
                        }

                        if ((ret.entries[0].EntryType == MessageEntryType.ConnectClientRet || ret.entries[0].EntryType == MessageEntryType.ReconnectClient) && ret.entries[0].Error == MessageErrorType.None)
                        {
                            if (ret.header.SenderNodeID == node.NodeID)
                            {

                                remote_endpoint = ret.header.ReceiverEndpoint;
                                local_endpoint = ret.header.SenderEndpoint;
                                parenttransport.AddTransportConnection(ret.header.SenderEndpoint, this);
                            }
                            else
                            {
                                //TODO: Handle this better
                                Close();
                            }
                        }

                        //if (mes.entries[0].EntryType != MessageEntryType.ConnectionTest && mes.entries[0].EntryType != MessageEntryType.ConnectionTestRet)
                        {
                            tlastrec = DateTime.UtcNow;
                        }


                        Task noop = SendMessage(ret, default(CancellationToken)).IgnoreResult();
                    }
                    catch (Exception)
                    {
                        Close();
                    }

                    return;
                }


                tlastrec = DateTime.UtcNow;

                
                
                if (mes.entries.Count == 1)
                {
                    if (mes.entries[0].EntryType == MessageEntryType.ConnectClientRet && remote_ep == 0)
                    {
                        lock (this)
                        {
                            if (remote_endpoint == 0)
                            {
                                remote_endpoint = mes.header.SenderEndpoint;
                            }
                            remote_ep = remote_endpoint;
                        }
                    }

                }

                
                if (!((mes.entries.Count == 1) && ((mes.entries[0].EntryType == MessageEntryType.ConnectionTest) || (mes.entries[0].EntryType == MessageEntryType.ConnectionTestRet))))
                {
                    tlastrec_mes = DateTime.UtcNow;
                    await ProcessMessage2(mes).ConfigureAwait(false);
                }

            }
            catch (Exception exp)
            {
                LogDebug(string.Format("Error receiving message in IntraTransport: {0}", exp), node, RobotRaconteur_LogComponent.Transport, "IntraTransport", endpoint: local_endpoint);
                Close();
            }

        }


        public async Task ProcessMessage2(Message m)
        {
            try
            {
                Transport.m_CurrentThreadTransportConnectionURL = GetConnectionURL();
                Transport.m_CurrentThreadTransport = this;
                await MessageReceived(m).ConfigureAwait(false);
            }
            catch
            {
                Close();
            }
            finally
            {
                Transport.m_CurrentThreadTransportConnectionURL = null;
                Transport.m_CurrentThreadTransport = null;
            }
        }

        internal async Task MessageReceived(Message m)
        {
            parenttransport.MessageReceived(m);
        }

        string connecturl;

        NodeID remote_node_id;
        public uint LocalEndpoint => local_endpoint;

        private uint local_endpoint = 0;
        private uint remote_endpoint = 0;
        private DateTime tlastrec;
        private DateTime tlastrec_mes;

        public uint RemoteEndpoint => remote_endpoint;

        public NodeID RemoteNodeID => throw new NotImplementedException();

        public string GetConnectionURL()
        {
            return connecturl;
        }

        public Task SendMessage(Message m, CancellationToken cancel)
        {
            IntraTransportConnection peer = null;
            if(!peer_connection?.TryGetTarget(out peer) ?? false || peer == null)
            {
                throw new ConnectionException("Connection lost");
            }

            peer?.AcceptMessage(m);
            return Task.CompletedTask;
        }

        public void Close()
        {
            bool connected1 = false;
            IntraTransportConnection peer1 = null;
            lock(this)
            {
                if (closed)
                    return;
                closed = true;

                peer_connection?.TryGetTarget(out peer1);
                peer_storage = null;

                peer_connection = null;
                connected1 = connected;
                connected = false;
            }

            if (!connected1)
                return;

            parenttransport.RemoveTransportConnection(this);

            if (peer1 != null)
            {
                peer1.RemoteClose();
            }
        }

        void RemoteClose()
        {
            _ = Task.Run(() => { Close(); }).IgnoreResult();
        }

        public void CheckConnection(uint endpoint)
        {
            IntraTransportConnection peer1 = null;
            lock(this)
            {
                peer_connection?.TryGetTarget(out peer1);
            }

            if (endpoint != local_endpoint || !connected || peer1 == null)
            {
                throw new ConnectionException("Connection lost");
            }
        }

        IntraTransportConnection peer_storage;
        internal void SetPeer(IntraTransportConnection peer)
        {
            lock (this)
            {
                peer_connection = new WeakReference<IntraTransportConnection>(peer);
                if (!server)
                {
                    peer_storage = peer;
                }

                remote_node_id = peer.node.NodeID;
                remote_endpoint = peer.LocalEndpoint;
                connected = true;
            }
        }

    }
    
}
