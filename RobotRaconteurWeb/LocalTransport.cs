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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Transport for communication between processes using UNIX domain sockets
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
    The LocalTransport implements transport connections between processes running on the
    same host operating system using UNIX domain sockets. UNIX domain sockets are similar
    to standard networking sockets, but are used when both peers are on the same machine
    instead of connected through a network. This provides faster operation and greater
    security, since the kernel simply passes data between the processes. UNIX domain
    sockets work using Information Node (inode) files, which are special files on
    the standard filesystem. Servers "listen" on a specified inode, and clients
    use the inode as the address to connect. The LocalTransport uses UNIX sockets
    in `SOCK_STREAM` mode. This provides a reliable stream transport connection similar
    to TCP, but with significantly improved performance due the lower overhead.
    </para>
    <para>
    UNIX domain sockets were added to Windows 10 with the 1803 update. Robot Raconteur
    switch to UNIX domain sockets for the LocalTransport on Windows in version 0.9.2.
    Previous versions used Named Pipes, but these were inferior to UNIX sockets. The
    LocalTransport will not function on versions of Windows prior to Windows 10 1803 update
    due to the lack of support for UNIX sockets. A warning will be issued to the log if
    the transport is not available, and all connection attempts will fail. All other
    transports will continue to operate normally.
    </para>
    <para>
    The LocalTransport stores inode and node information files in the filesystem at various
    operator system dependent locations. See the Robot Raconteur Standards documents
    for details on where these files are stored.
    </para>
    <para>
    Discovery is implemented using file watchers. The file watchens must be activated
    using the node setup flags, or by calling EnableNodeDiscoveryListening().
    After being initialized the file watchers operate automatically.
    </para>
    <para>
    The LocalTransport can be used to dynamically assign NodeIDs to nodes based on a NodeName.
    StartServerAsNodeName() and StartClientAsNodeName() take a NodeName that will identify the
    node to clients, and manage a system-local NodeID corresponding to that NodeName. The
    generated NodeIDs are stored on the local filesystem. If LocalTransport finds a
    corresponding
    NodeID on the filesystem, it will load and use that NodeID. If it does not, a new random
    NodeID
    is automatically generated.
    </para>
    <para>
    The server can be started in "public" or "private" mode. Private servers store their
    inode and
    information in a location only the account owner can access, while "public" servers are
    placed in a location that all users with the appropriate permissions can access. By
    default,
    public LocalTransport servers are assigned to the "robotraconteur" group. Clients that
    belong to the
    "robotraconteur" group will be able to connect to these public servers.
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
    public sealed class LocalTransport : Transport
    {

        private bool transportopen = false;
        private bool serverstarted = false;

        /// <inheretdoc/>
        public override bool IsServer { get { return true; } }
        /// <inheretdoc/>
        public override bool IsClient { get { return true; } }

        internal Dictionary<uint, AsyncStreamTransport> TransportConnections = new Dictionary<uint, AsyncStreamTransport>();

        /// <summary>
        /// The default time to wait for a message before closing the connection. Units in ms
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public int DefaultReceiveTimeout { get; set; }
        /// <summary>
        /// The default time to wait for a connection to be made before timing out. Units in ms
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public int DefaultConnectTimeout { get; set; }

        /// <summary>
        /// The "scheme" portion of the url that this transport corresponds to ("local" in this case)
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public override string[] UrlSchemeString { get { return new string[] { "rr+local" }; } }

        /// <summary>
        /// Disable message version 4
        /// </summary>
        [PublicApi]
        public bool DisableMessage4 { get; set; }

        /// <summary>
        /// The maxmimum message size in bytes
        /// </summary>
        [PublicApi]
        public uint MaxMessageSize { get; set; }

        /**
        <summary>
        Construct a new LocalTransport for a non-default node. Must be registered with node using
        node.RegisterTransport()
        </summary>
        <remarks>None</remarks>
        <param name="node">The node to use with the transport. Defaults to RobotRaconteurNode.s</param>
        */
        [PublicApi]
        public LocalTransport(RobotRaconteurNode node = null)
            : base(node)
        {
            DefaultReceiveTimeout = 600000;
            DefaultConnectTimeout = 2500;
            DisableMessage4 = false;
            MaxMessageSize = 12 * 1024 * 1024;
            parent_adapter = new AsyncStreamTransportParentImpl(this);
        }

        /// <inheretdoc/>

        public override async Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel)
        {
            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var url_res = TransportUtil.ParseConnectionUrl(url);

            if (string.IsNullOrEmpty(url_res.nodename) && url_res.nodeid.IsAnyNode)
            {
                throw new ConnectionException("NodeID and/or NodeName must be specified for LocalTransport");
            }

            var my_username = NodeDirectoriesUtil.GetLogonUserName();

            var node_dirs = node.NodeDirectories;

            string user_path = LocalTransportUtil.GetTransportPrivateSocketPath(node_dirs);
            string public_user_path = LocalTransportUtil.GetTransportPublicSocketPath(node_dirs);
            string public_search_path = LocalTransportUtil.GetTransportPublicSearchPath(node_dirs);

            var search_paths = new List<string>();

            string host = url_res.host;

            if (url_res.port != -1) throw new ConnectionException("Invalid url for local transport");
            if (url_res.path != "" && url_res.path != "/") throw new ConnectionException("Invalid url for local transport");

            string username;

            var usernames = new List<string>();

            if (!host.Contains('@'))
            {
                if (host != "localhost" && host != "") throw new ConnectionException("Invalid host for local transport");
                search_paths.Add(user_path);

                if (public_user_path != null)
                {
                    search_paths.Add(public_user_path);
                }

                usernames.Add(my_username);
                if (public_search_path != null)
                {
                    string service_username;
                    if (is_windows)
                    {
                        service_username = "LocalService";
                    }
                    else
                    {
                        service_username = "root";
                    }

                    string service_path = Path.Combine(public_search_path, service_username);
                    if (Directory.Exists(service_path))
                    {
                        search_paths.Add(service_path);
                    }

                    usernames.Add(service_username);
                }
            }
            else
            {
                var v1 = host.Split(new char[] { '@' });
                if (v1.Length != 2) throw new ConnectionException("Malformed URL");
                if (v1[1] != "localhost") throw new ConnectionException("Invalid host for local transport");

                username = v1[0].Trim();

                if (!Regex.IsMatch(username, "^[a-zA-Z][a-zA-Z0-9_\\-]*$"))
                {
                    throw new ConnectionException("\"" + username + "\" is an invalid username");
                }

                if (username == my_username)
                {
                    search_paths.Add(user_path);
                    if (public_user_path != null)
                    {
                        search_paths.Add(public_user_path);
                    }
                }
                else
                {
                    if (public_search_path != null)
                    {
                        var service_path = Path.Combine(public_search_path, username);
                        if (Directory.Exists(service_path))
                        {
                            search_paths.Add(service_path);
                        }
                    }
                }
                usernames.Add(username);
            }

            var socket = LocalTransportUtil.FindAndConnectLocalSocket(url_res, search_paths.ToArray(), usernames.ToArray());
            if (socket == null) throw new ConnectionException("Could not connect to service");

            var connection = new LocalClientTransport(this);
            connection.ReceiveTimeout = DefaultReceiveTimeout;
            await connection.Connect(new NetworkStream(socket, true), url, e, cancel).ConfigureAwait(false);
            return connection;
        }

        /// <inheretdoc/>
        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            lock (this)
            {
                if (TransportConnections.ContainsKey(e.LocalEndpoint))
                    TransportConnections[e.LocalEndpoint].Close();
            }
            return Task.FromResult(0);
        }

        NodeDirectoriesFD f_node_lock_file = null;
        /**
        <summary>
        Initialize the LocalTransport by assigning a NodeID based on NodeName
        </summary>
        <remarks>
        <para>
        Assigns the specified name to be the NodeName of the node, and manages
        a corresponding NodeID. See LocalTransport for more information.
        </para>
        <para> Throws NodeNameAlreadyInUse if another node is using name
        </para>
        </remarks>
        <param name="name">The node name</param>
        */
        [PublicApi]
        public void StartClientAsNodeName(string name)
        {
            if (!Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$"))
            {
                throw new ArgumentException("\"" + name + "\" is an invalid NodeName");
            }

            var node_dirs = node.NodeDirectories;

            lock (this)
            {
                var p = NodeDirectoriesUtil.GetUuidForNameAndLock(node_dirs, name, new string[] { "nodeids" });

                try
                {
                    node.NodeID = p.uuid;
                }
                catch (Exception)
                {
                    if (node.NodeID != p.uuid)
                    {
                        p.Dispose();
                        throw;
                    }
                }

                fds.h_nodename_s = p.fd;
            }
        }

        /// <summary>
        /// Check if the Local Transport is supported
        /// </summary>
        /// <remarks>
        /// Not all versions of Windows support the UNIX socket used
        /// by the Local Transport. It will be disabled automatically
        /// if not available.
        /// </remarks>
        /// <return>true if available</return>
        [PublicApi]
        public bool IsLocalTransportSupported
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var s = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        s.Dispose();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }
        /**
        <summary>
        Start the server using the specified NodeName and assigns a NodeID
        </summary>
        <remarks>
        <para>
        The LocalTransport will listen on a UNIX domain socket for incoming clients,
        using information files and inodes on the local filesystem. Clients
        can locate the node using the NodeID and/or NodeName. The NodeName is assigned
        to the node, and the transport manages a corresponding NodeID. See
        LocalTransport for more information.
        </para>
        <para>
        Throws NodeNameAlreadyInUse if another node is using name.
        </para>
        <para> Throws NodeIDAlreadyInUse if another node is using the managed NodeID.
        </para>
        </remarks>
        <param name="name">The NodeName</param>
        <param name="public_">If True, other users can access the server. If False, only
        the account owner can access the server. Defaults to false.</param>
        */
        [PublicApi]
        public void StartServerAsNodeName(string name, bool public_ = false)
        {
            lock (this)
            {
                if (serverstarted) throw new InvalidOperationException("Server already started");
                serverstarted = true;
            }

            if (!IsLocalTransportSupported)
            {
                Console.Error.WriteLine("warning: local transport not supported on this operating system");
                StartClientAsNodeName(name);
                return;
            }

            LocalTransportNodeLock<string> nodename_lock = null;
            GetUuidForNameAndLockResult nodeid1 = null;
            LocalTransportNodeLock<NodeID> nodeid_lock = null;

            NodeDirectoriesFD h_pid_id_s = null;
            NodeDirectoriesFD h_pid_name_s = null;
            NodeDirectoriesFD h_info_id_s = null;
            NodeDirectoriesFD h_info_name_s = null;

            Socket socket = null;
            UnixDomainSocketEndPoint ep = null;

            var node_dirs = node.NodeDirectories;

            try
            {

                nodename_lock = LocalTransportNodeLock<string>.Lock(name);
                if (nodename_lock == null)
                {
                    throw new NodeNameAlreadyInUse();
                }

                nodeid1 = NodeDirectoriesUtil.GetUuidForNameAndLock(node_dirs, name, new string[] { "nodeids" });

                NodeID nodeid = nodeid1.uuid;

                if (nodeid.IsAnyNode) throw new InvalidOperationException("Could not initialize LocalTransport server: Invalid NodeID in settings file");

                nodeid_lock = LocalTransportNodeLock<NodeID>.Lock(nodeid);
                if (nodeid_lock == null)
                {
                    throw new NodeIDAlreadyInUse();
                }

                string socket_path;
                if (!public_)
                {
                    socket_path = LocalTransportUtil.GetTransportPrivateSocketPath(node_dirs);
                }
                else
                {
                    var socket_path1 = LocalTransportUtil.GetTransportPublicSearchPath(node_dirs);
                    if (socket_path1 == null) throw new SystemResourceException("Computer not initialized for public node server");
                    socket_path = socket_path1;
                }

                string pipename;
                string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                int tries = 0;
                var random = new Random(Guid.NewGuid().GetHashCode());

                do
                {

                    var result = new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());

                    if (public_)
                    {
                        pipename = Path.Combine(socket_path, "socket");
                    }
                    else
                    {
                        pipename = Path.Combine(node_dirs.user_run_dir, "socket");
                    }

                    pipename = Path.Combine(pipename, result + ".sock");

                    try
                    {
                        socket = null;
                        ep = new UnixDomainSocketEndPoint(pipename);
                        socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        socket.Bind(ep);
                        socket.Listen(16);
                        break;
                    }
                    catch (Exception)
                    {
                        socket?.Dispose();
                        tries++;
                        if (tries > 3)
                            throw;
                    }
                }
                while (true);

                string pid_id_fname = Path.Combine(socket_path, "by-nodeid", nodeid.ToString("D") + ".pid");
                string info_id_fname = Path.Combine(socket_path, "by-nodeid", nodeid.ToString("D") + ".info");
                string pid_name_fname = Path.Combine(socket_path, "by-nodename", name + ".pid");
                string info_name_fname = Path.Combine(socket_path, "by-nodename", name + ".info");

                var info = new Dictionary<string, string>();
                info.Add("nodename", name);
                info.Add("nodeid", nodeid.ToString());
                info.Add("socket", pipename);
                //TODO: info.Add("ServiceStateNonce", node.GetServiceStateNonce());

                h_pid_id_s = NodeDirectoriesUtil.CreatePidFile(pid_id_fname, false);
                h_pid_name_s = NodeDirectoriesUtil.CreatePidFile(pid_name_fname, true);
                h_info_id_s = NodeDirectoriesUtil.CreateInfoFile(info_id_fname, info, false);
                h_info_name_s = NodeDirectoriesUtil.CreateInfoFile(info_name_fname, info, true);

                try
                {
                    node.NodeID = nodeid;
                }
                catch (Exception)
                {
                    if (node.NodeID != nodeid)
                        throw;
                }

                try
                {
                    node.NodeName = name;
                }
                catch (Exception)
                {
                    if (node.NodeName != name)
                        throw;
                }

                DoListen(socket, pipename, ep).IgnoreResult();

                fds.nodename_lock = nodename_lock;
                fds.h_nodename_s = nodeid1.fd;
                fds.nodeid_lock = nodeid_lock;
                fds.h_pid_id_s = h_pid_id_s;
                fds.h_pid_name_s = h_pid_name_s;
                fds.h_info_id_s = h_info_id_s;
                fds.h_info_name_s = h_info_name_s;

                nodename_lock = null;
                nodeid1 = null;
                nodeid_lock = null;
                h_pid_id_s = null;
                h_pid_name_s = null;
                h_info_id_s = null;
                h_info_name_s = null;
                socket = null;

            }
            catch (Exception)
            {


                nodename_lock?.Dispose();
                nodeid1?.fd?.Dispose();
                nodeid_lock?.Dispose();

                h_pid_id_s?.Dispose();
                h_pid_name_s?.Dispose();
                h_info_id_s?.Dispose();
                h_info_name_s?.Dispose();

                throw;
            }
        }
        /**
        <summary>

        The LocalTransport will listen on a UNIX domain socket for incoming clients,
        using information files and inodes on the local filesystem. This function
        leaves the NodeName blank, so clients must use NodeID to identify the node.
        </summary>
        <remarks>
        Throws NodeIDAlreadyInUse if another node is using nodeid
        </remarks>
        <param name="nodeid">The NodeID</param>
        */
        [PublicApi]
        public void StartServerAsNodeID(NodeID nodeid)
        {
            lock (this)
            {
                if (serverstarted) throw new InvalidOperationException("Server already started");
                serverstarted = true;
            }
            if (nodeid.IsAnyNode) throw new InvalidOperationException("Could not initialize server: Invalid NodeID");

            throw new NotImplementedException();

        }


        private async Task DoListen(Socket socket, string socket_fname, UnixDomainSocketEndPoint ep)
        {
            lock (this)
            {
                transportopen = true;
            }

            await Task.Delay(10).ConfigureAwait(false);

            try
            {

                while (!close_token.IsCancellationRequested)
                {
                    var s2 = await socket.AcceptAsync().ConfigureAwait(false);

                    ClientConnected(new NetworkStream(s2, true));
                }
            }
            finally
            {
                try
                {
                    socket.Close();
                    File.Delete(socket_fname);
                }
                catch
                {

                }
            }

        }

        CancellationTokenSource close_token = new CancellationTokenSource();
        TaskCompletionSource<int> close_task = new TaskCompletionSource<int>();


        private void ClientConnected(Stream a)
        {

            try
            {
                if (!transportopen)
                {
                    try
                    {
                        a.Close();
                    }
                    catch (Exception) { }
                    return;
                }
                var localc = a;

                LocalServerTransport c = new LocalServerTransport(this);
                c.ReceiveTimeout = DefaultReceiveTimeout;
                c.Connect(localc).IgnoreResult();
            }
            catch (Exception)
            {
            }

        }

        /// <summary>
        /// Returns true if url has scheme "local"
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="url">The url to check</param>
        /// <returns>True if url has scheme "local"</returns>
        [PublicApi]
        public override bool CanConnectService(string url)
        {
            Uri u = new Uri(url);
            if (u.Scheme != "rr+local") return false;

            return true;
        }
#pragma warning disable 1591
        public override async Task SendMessage(Message m, CancellationToken cancel)
        {
            if (m.header.SenderNodeID != node.NodeID)
            {
                throw new NodeNotFoundException("Invalid sender node");
            }
            try
            {
                ITransportConnection tc;
                lock (this)
                {
                    tc = TransportConnections[m.header.SenderEndpoint];
                }
                await tc.SendMessage(m, cancel).ConfigureAwait(false);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new ConnectionException("Connection to remote node has been closed");
            }
        }



        protected internal override void MessageReceived(Message m)
        {
            node.MessageReceived(m);
        }

        LocalTransportFDs fds = new LocalTransportFDs();
#pragma warning restore 1591
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


            try
            {
                lock (this)
                {
                    TransportConnections.Clear();
                }
            }
            catch { }

            close_task.TrySetResult(0);
            close_token.Cancel();

            fds.Dispose();
            discovery?.Dispose();

            base.Close();

            return Task.FromResult(0);
        }

#pragma warning disable 1591
        public override void CheckConnection(uint endpoint)
        {
            try
            {
                lock (this)
                {
                    TransportConnections[endpoint].CheckConnection(endpoint);
                }
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



        public override uint TransportCapability(string name)
        {
            return base.TransportCapability(name);
        }

        public override Task<List<NodeDiscoveryInfo>> GetDetectedNodes(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            var o = new List<NodeDiscoveryInfo>();

            var node_dirs = node.NodeDirectories;

            string private_search_dir = LocalTransportUtil.GetTransportPrivateSocketPath(node_dirs);
            string my_username = NodeDirectoriesUtil.GetLogonUserName();

            var o1 = LocalTransportUtil.FindNodesInDirectory(private_search_dir, "rr+local", now, my_username);
            o.AddRange(o1);

            var search_path = LocalTransportUtil.GetTransportPublicSearchPath(node_dirs);

            if (search_path != null)
            {
                try
                {
                    foreach (var d in Directory.EnumerateDirectories(search_path))
                    {
                        try
                        {
                            string username1 = Path.GetFileName(d);
                            var o2 = LocalTransportUtil.FindNodesInDirectory(d, "rr+local", now, username1);
                            o.AddRange(o2);
                        }
                        catch (Exception)
                        { }
                    }
                }
                catch (Exception) { }
            }
            return Task.FromResult(o);
        }
#pragma warning restore 1591
        LocalTransportDiscovery discovery;

        /// <summary>
        /// Enable discovery listening for nodes using the LocalTransport
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public void EnableNodeDiscoveryListening()
        {
            lock (this)
            {
                if (discovery != null)
                {
                    throw new InvalidOperationException("LocalTransport discovery already running");
                }

                discovery = new LocalTransportDiscovery(this, node);
                discovery.Start();
            }
        }
        /// <summary>
        /// Disable discovery listening for nodes using the LocalTransport
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public void DisableNodeDiscoveryListening()
        {
            lock (this)
            {
                discovery?.Dispose();
            }
        }

        private class AsyncStreamTransportParentImpl : AsyncStreamTransportParent
        {
            LocalTransport parent;

            public AsyncStreamTransportParentImpl(LocalTransport parent)
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

            public Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate()
            {
                return null;
            }
        }

        internal readonly AsyncStreamTransportParent parent_adapter;

        /// <summary>
        /// Urls the transport is listening on
        /// </summary>
        /// <remarks>None</remarks>
        /// <value></value>
        [PublicApi]
        public override string[] ServerListenUrls
        {
            get
            {
                if (!serverstarted)
                {
                    return new string[0];
                }
                return new string[] { string.Format("rr+local:///?nodeid={0}", node.NodeID.ToString("D")) };
            }
        }
    }



    sealed class LocalClientTransport : AsyncStreamTransport
    {



        private Stream socket;
        //public NetworkStream netstream;

        private LocalTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        /// <summary>
        /// Creates a LocalClientTransport with parent LocalTransport
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="c">Parent transport</param>
        [PublicApi]
        public LocalClientTransport(LocalTransport c)
            : base(c.node, c.parent_adapter)
        {
            parenttransport = c;
            disable_message4 = parenttransport.DisableMessage4;
            max_message_size = parenttransport.MaxMessageSize;

        }

        string connecturl;


        public async Task Connect(Stream s, string connecturl, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            //LocalEndpoint = e.LocalEndpoint;

            socket = s;
            //socket.Client.NoDelay = true;
            this.connecturl = connecturl;
            m_LocalEndpoint = e.LocalEndpoint;

            m_Connected = true;
            await ConnectStream(socket, true, null, null, false, false, 30000, cancel).ConfigureAwait(false);
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



    sealed class LocalServerTransport : AsyncStreamTransport
    {

        private Stream socket;
        private LocalTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;



        public LocalServerTransport(LocalTransport c)
            : base(c.node, c.parent_adapter)
        {
            parenttransport = c;
            disable_message4 = parenttransport.DisableMessage4;
            max_message_size = parenttransport.MaxMessageSize;

        }


        public async Task Connect(Stream s, CancellationToken cancel = default(CancellationToken))
        {
            //LocalEndpoint = e.LocalEndpoint;

            socket = s;
            //socket.Client.NoDelay = true;

            m_Connected = true;
            await ConnectStream(socket, true, null, null, false, false, 30000, cancel).ConfigureAwait(false);
        }


        public override string GetConnectionURL()
        {
            return "rr+local://localhost/";
        }
    }



    static class LocalTransportUtil
    {



        public static string GetTransportPrivateSocketPath(NodeDirectories node_dirs)
        {
            try
            {
                string user_run_path = node_dirs.user_run_dir;
                string path = Path.Combine(user_run_path, "transport", "local");
                string bynodeid_path = Path.Combine(path, "by-nodeid");
                string bynodename_path = Path.Combine(path, "by-nodename");
                string socket_path1 = Path.Combine(path, "socket");
                string socket_path2 = Path.Combine(user_run_path, "socket");

                Directory.CreateDirectory(bynodeid_path);
                Directory.CreateDirectory(bynodename_path);
                Directory.CreateDirectory(socket_path1);
                Directory.CreateDirectory(socket_path2);

                return path;
            }
            catch (Exception ee)
            {
                throw new SystemResourceException("Could not activate system for local transport: " + ee.Message);
            }
        }

        public static string GetTransportPublicSocketPath(NodeDirectories node_dirs)
        {
            string path1 = GetTransportPublicSearchPath(node_dirs);
            if (path1 == null)
            {
                return null;
            }

            try
            {
                string username = NodeDirectoriesUtil.GetLogonUserName();

                string path = Path.Combine(path1, username);

                if (!Directory.Exists(path))
                {
                    throw new SystemResourceException(String.Format("RobotRaconteur public directory not configured for user {0}", username));
                }

                string bynodeid_path = Path.Combine(path, "by-nodeid");
                string bynodename_path = Path.Combine(path, "by-nodename");
                string socket_path = Path.Combine(path, "socket");

                Directory.CreateDirectory(bynodeid_path);
                Directory.CreateDirectory(bynodename_path);
                Directory.CreateDirectory(socket_path);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //TODO: file permissions?
                }
                else
                {
                    Mono.Unix.Native.Stat info;
                    if (Mono.Unix.Native.Syscall.stat(path, out info) < 0)
                    {
                        throw new SystemResourceException(String.Format("RobotRaconteur public directory not configured for user {0}", username));
                    }

                    uint my_uid = Mono.Unix.Native.Syscall.getuid();

                    Mono.Unix.Native.Syscall.chmod(bynodeid_path, Mono.Unix.Native.FilePermissions.S_ISGID | Mono.Unix.Native.FilePermissions.S_IRUSR
                        | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR | Mono.Unix.Native.FilePermissions.S_IRGRP
                        | Mono.Unix.Native.FilePermissions.S_IXGRP);
                    Mono.Unix.Native.Syscall.chown(bynodeid_path, my_uid, info.st_gid);
                    Mono.Unix.Native.Syscall.chmod(bynodename_path, Mono.Unix.Native.FilePermissions.S_ISGID | Mono.Unix.Native.FilePermissions.S_IRUSR
                        | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR | Mono.Unix.Native.FilePermissions.S_IRGRP
                        | Mono.Unix.Native.FilePermissions.S_IXGRP);
                    Mono.Unix.Native.Syscall.chown(bynodename_path, my_uid, info.st_gid);
                    Mono.Unix.Native.Syscall.chmod(socket_path, Mono.Unix.Native.FilePermissions.S_ISGID | Mono.Unix.Native.FilePermissions.S_IRUSR
                        | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR | Mono.Unix.Native.FilePermissions.S_IRGRP
                        | Mono.Unix.Native.FilePermissions.S_IXGRP);
                    Mono.Unix.Native.Syscall.chown(socket_path, my_uid, info.st_gid);
                }

                return path;
            }
            catch (Exception ee)
            {
                throw new SystemResourceException("Could not activate system for local transport: " + ee.Message);
            }
        }

        public static string GetTransportPublicSearchPath(NodeDirectories nodeDirs)
        {
            string path1;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string username = Environment.UserName;  // Get the logon username

                path1 = nodeDirs.system_run_dir;
                if (!Directory.Exists(path1))
                {
                    return null;  // Return null for no value
                }

                DirectoryInfo di = new DirectoryInfo(path1);
                DirectorySecurity ds = di.GetAccessControl();
                IdentityReference owner = ds.GetOwner(typeof(SecurityIdentifier));

                string ownerSidString = owner.ToString();

                string systemSidString = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).ToString();
                string localServiceSidString = new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null).ToString();

                if (ownerSidString != systemSidString && ownerSidString != localServiceSidString)
                {
                    return null;
                }

                path1 = Path.Combine(path1, "transport", "local");
            }
            else
            {
                path1 = Path.Combine(nodeDirs.system_run_dir, "transport", "local");
            }

            if (!Directory.Exists(path1))
            {
                return null;
            }

            return path1;
        }

        public static List<NodeDiscoveryInfo> FindNodesInDirectory(string path, string scheme, DateTime now, string username)
        {
            var o = new List<NodeDiscoveryInfo>();

            string search_id = Path.Combine(path, "by-nodeid");
            string search_name = Path.Combine(path, "by-nodename");
            foreach (var f in Directory.EnumerateFiles(search_id))
            {
                try
                {
                    if (!File.Exists(f))
                    {
                        continue;
                    }

                    if (Path.GetExtension(f) != ".info")
                    {
                        continue;
                    }

                    Dictionary<string, string> info;
                    if (!NodeDirectoriesUtil.ReadInfoFile(f, out info))
                    {
                        continue;
                    }

                    if (!info.ContainsKey("nodeid") || !info.ContainsKey("username"))
                    {
                        continue;
                    }

                    NodeID nodeid = new NodeID(info["nodeid"]);
                    string username2 = info["username"].Trim();

                    string url;
                    if (username != null)
                    {
                        if (username2 != username)
                        {
                            continue;
                        }

                        url = scheme + "://" + username + "@localhost/?nodeid=" + nodeid.ToString("D") + "&service=RobotRaconteurServiceIndex";
                    }
                    else
                    {
                        url = scheme + ":///?nodeid=" + nodeid.ToString("D") + "&service=RobotRaconteurServiceIndex";
                    }

                    var i = new NodeDiscoveryInfo();
                    i.NodeID = nodeid;
                    i.NodeName = "";
                    var iurl = new NodeDiscoveryInfoURL();
                    iurl.URL = url;
                    iurl.LastAnnounceTime = now;
                    i.URLs.Add(iurl);

                    if (info.ContainsKey("ServiceStateNonce"))
                    {
                        i.ServiceStateNonce = info["ServiceStateNonce"];
                    }

                    o.Add(i);
                }
                catch (Exception)
                {
                    continue;
                }

            }

            foreach (var f in Directory.EnumerateFiles(search_name))
            {
                try
                {
                    if (!File.Exists(f))
                    {
                        continue;
                    }

                    if (Path.GetExtension(f) != ".info")
                    {
                        continue;
                    }

                    Dictionary<string, string> info;
                    if (!NodeDirectoriesUtil.ReadInfoFile(f, out info))
                    {
                        continue;
                    }

                    if (!info.ContainsKey("nodeid") || !info.ContainsKey("nodename"))
                    {
                        continue;
                    }

                    NodeID nodeid = new NodeID(info["nodeid"]);
                    string nodename1 = info["nodename"];

                    if (nodename1 != Path.ChangeExtension(Path.GetFileName(f), ""))
                    {
                        continue;
                    }

                    foreach (var e1 in o)
                    {
                        if (e1.NodeID == nodeid && String.IsNullOrEmpty(e1.NodeName))
                        {
                            e1.NodeName = nodename1;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }

            }
            return o;
        }

        public static Socket FindAndConnectLocalSocket(ParseConnectionUrlResult url, string[] search_paths, string[] usernames)
        {
            Socket socket;

            foreach (var e in search_paths)
            {
                Dictionary<string, string> info_data;
                if (!url.nodeid.IsAnyNode)
                {
                    string e2 = Path.Combine(e, "by-nodeid", url.nodeid.ToString("D") + ".info");

                    if (!NodeDirectoriesUtil.ReadInfoFile(e2, out info_data))
                    {
                        continue;
                    }

                    if (!String.IsNullOrEmpty(url.nodename))
                    {
                        string name1;
                        if (!info_data.TryGetValue("nodename", out name1))
                        {
                            continue;
                        }

                        if (name1 != url.nodename)
                        {
                            continue;
                        }

                        string e3 = Path.Combine(e, "by-nodename", url.nodename + ".info");

                        Dictionary<string, string> info_data2;
                        if (!NodeDirectoriesUtil.ReadInfoFile(e3, out info_data2))
                        {
                            continue;
                        }

                        string socket1;
                        string socket2;
                        if (!info_data.TryGetValue("socket", out socket1)
                            || !info_data2.TryGetValue("socket", out socket2))
                        {
                            continue;
                        }

                        if (socket1 != socket2)
                        {
                            continue;
                        }
                    }

                }
                else
                {
                    string e2 = Path.Combine(e, "by-nodename", url.nodename + ".info");

                    if (!NodeDirectoriesUtil.ReadInfoFile(e2, out info_data))
                    {
                        continue;
                    }
                }

                string pipename;
                if (!info_data.TryGetValue("socket", out pipename))
                {
                    continue;
                }

                socket = null;
                try
                {
                    socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    var ep = new UnixDomainSocketEndPoint(pipename);
                    socket.Connect(ep);

                    //TODO: Check user on unix
                    return socket;
                }
                catch (Exception)
                {
                    socket?.Dispose();
                    socket = null;
                }
            }
            return null;
        }

    }

    class LocalTransportNodeLock<T> : IDisposable
    {
        static HashSet<T> nodeids = new HashSet<T>();

        public static LocalTransportNodeLock<T> Lock(T id)
        {
            if (!nodeids.Add(id))
            {
                return null;
            }
            else
            {
                var o = new LocalTransportNodeLock<T>() { release_id = id };
                return o;
            }
        }

        T release_id;

        public void Dispose()
        {
            lock (nodeids)
            {
                nodeids.Remove(release_id);
            }
        }
    }

    class LocalTransportFDs : IDisposable
    {
        public NodeDirectoriesFD h_nodename_s;
        public NodeDirectoriesFD h_pid_id_s;
        public NodeDirectoriesFD h_info_id_s;
        public NodeDirectoriesFD h_pid_name_s;
        public NodeDirectoriesFD h_info_name_s;
        public LocalTransportNodeLock<NodeID> nodeid_lock;
        public LocalTransportNodeLock<string> nodename_lock;

        public void Dispose()
        {
            h_nodename_s?.Dispose();
            h_pid_id_s?.Dispose();
            h_info_id_s?.Dispose();
            h_pid_name_s?.Dispose();
            h_info_name_s?.Dispose();
            nodeid_lock?.Dispose();
            nodename_lock?.Dispose();
        }
    };

    class LocalTransportDiscovery : IDisposable
    {
        RobotRaconteurNode node;
        LocalTransport transport;
        NodeDirectories node_dirs;

        public LocalTransportDiscovery(LocalTransport transport, RobotRaconteurNode node)
        {
            this.node = node;
            this.transport = transport;
            node_dirs = node.NodeDirectories;

        }

        public async Task Refresh(CancellationToken token)
        {
            var n = await transport.GetDetectedNodes(token).ConfigureAwait(false);
            foreach (var n1 in n)
            {
                node.NodeDetected(n1);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                _ = Refresh(default(CancellationToken)).IgnoreResult();
            }
            catch (Exception) { }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                _ = Refresh(default(CancellationToken)).IgnoreResult();
            }
            catch (Exception) { }
        }

        FileSystemWatcher file_watcher_private;
        FileSystemWatcher file_watcher_public;

        private FileSystemWatcher NewFileSystemWatcher()
        {
            var w = new FileSystemWatcher();
            w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName;
            w.Changed += OnChanged;
            w.Created += OnChanged;
            w.Deleted += OnChanged;
            w.Renamed += OnRenamed;

            return w;
        }

        public void Start()
        {
            try
            {
                file_watcher_private = NewFileSystemWatcher();
                file_watcher_private.Path = LocalTransportUtil.GetTransportPrivateSocketPath(node_dirs);
                file_watcher_private.EnableRaisingEvents = true;
            }
            catch (Exception) { }

            try
            {
                file_watcher_public = NewFileSystemWatcher();
                file_watcher_public.Path = LocalTransportUtil.GetTransportPublicSearchPath(node_dirs);
                file_watcher_public.EnableRaisingEvents = true;
            }
            catch (Exception) { }

        }

        public void Dispose()
        {
            file_watcher_public?.Dispose();
            file_watcher_private?.Dispose();
        }

    }

    /// <summary>
    /// Exception thrown when a node ID is already in use
    /// </summary>
    /// <remarks>None</remarks>
    [PublicApi]
    public class NodeIDAlreadyInUse : IOException
    {
        /// <summary>
        /// Construct a new NodeIDAlreadyInUse
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public NodeIDAlreadyInUse() : base("NodeID already in use") { }
        /// <summary>
        /// Construct a new NodeIDAlreadyInUse with a message
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="message">Message for exception</param>
        [PublicApi]
        public NodeIDAlreadyInUse(string message) : base(message) { }
    }
    /// <summary>
    /// Exception thrown when a node name is already in use
    /// </summary>
    /// <remarks>None</remarks>
    [PublicApi]
    public class NodeNameAlreadyInUse : IOException
    {
        /// <summary>
        /// Construct a new NodeNameAlreadyInUse
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi]
        public NodeNameAlreadyInUse() : base("NodeName already in use") { }
        /// <summary>
        /// Construct a new NodeNameAlreadyInUse with a message
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="message">Message for exception</param>
        [PublicApi]
        public NodeNameAlreadyInUse(string message) : base(message) { }
    }
}
