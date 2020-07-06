using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.Security.AccessControl;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RobotRaconteurWeb
{

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
        public int DefaultReceiveTimeout { get; set; }
        /// <summary>
        /// The default time to wait for a connection to be made before timing out. Units in ms
        /// </summary>
        public int DefaultConnectTimeout { get; set; }

        /// <summary>
        /// The "scheme" portion of the url that this transport corresponds to ("local" in this case)
        /// </summary>
        public override string[] UrlSchemeString { get { return new string[] { "rr+local" }; } }

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

        public LocalTransport(RobotRaconteurNode node = null)
            : base(node)
        {
            DefaultReceiveTimeout = 15000;
            DefaultConnectTimeout = 2500;
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

            var my_username = LocalTransportUtil.GetLogonUserName();

            string user_path = LocalTransportUtil.GetTransportPrivateSocketPath();
            string public_user_path = LocalTransportUtil.GetTransportPublicSocketPath();
            string public_search_path = LocalTransportUtil.GetTransportPublicSearchPath();

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
            await connection.Connect(new NetworkStream(socket, true), url, e, cancel);
            return connection;
        }

        /// <inheretdoc/>
        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            if (TransportConnections.ContainsKey(e.LocalEndpoint))
                TransportConnections[e.LocalEndpoint].Close();
            return Task.FromResult(0);
        }

        LocalTransportFD f_node_lock_file = null;

        public void StartClientAsNodeName(string name)
        {
            if (!Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$"))
            {
                throw new ArgumentException("\"" + name + "\" is an invalid NodeName");
            }

            lock(this)
            {
                var p = LocalTransportUtil.GetNodeIDForNodeNameAndLock(name);

                try
                {
                    node.NodeID = p.Item1;
                }
                catch (Exception)
                {
                    if (node.NodeID != p.Item1)
                    {
                        p.Item2.Dispose();
                        throw;
                    }                        
                }

                fds.h_nodename_s = p.Item2;
            }
        }

        public bool IsLocalTransportSupported
        {
            get
            {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                        
        public void StartServerAsNodeName(string name, bool public_= false)
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
            Tuple<NodeID, LocalTransportFD> nodeid1 = null;
            LocalTransportNodeLock<NodeID> nodeid_lock = null;

            LocalTransportFD h_pid_id_s = null;
            LocalTransportFD h_pid_name_s = null;
            LocalTransportFD h_info_id_s = null;
            LocalTransportFD h_info_name_s = null;

            Socket socket = null;
            UnixDomainSocketEndPoint  ep = null;

            try
            {

                nodename_lock = LocalTransportNodeLock<string>.Lock(name);
                if (nodename_lock == null)
                {
                    throw new NodeNameAlreadyInUse();
                }

                nodeid1 = LocalTransportUtil.GetNodeIDForNodeNameAndLock(name);

                NodeID nodeid = nodeid1.Item1;

                if (nodeid.IsAnyNode) throw new InvalidOperationException("Could not initialize LocalTransport server: Invalid NodeID in settings file");

                nodeid_lock = LocalTransportNodeLock<NodeID>.Lock(nodeid);
                if (nodeid_lock == null)
                {
                    throw new NodeIDAlreadyInUse();
                }

                string socket_path;
                if (!public_)
                {
                    socket_path = LocalTransportUtil.GetTransportPrivateSocketPath();
                }
                else
                {
                    var socket_path1 = LocalTransportUtil.GetTransportPublicSearchPath();
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
                        pipename = Path.Combine(LocalTransportUtil.GetUserRunPath(), "socket");
                    }

                    pipename = Path.Combine(pipename, result + ".sock");
                                        
                    try
                    {
                        socket = null;
                        ep = new UnixDomainSocketEndPoint (pipename);
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

                h_pid_id_s = LocalTransportUtil.CreatePidFile(pid_id_fname, false);
                h_pid_name_s = LocalTransportUtil.CreatePidFile(pid_name_fname, true);
                h_info_id_s = LocalTransportUtil.CreateInfoFile(info_id_fname, info, false);
                h_info_name_s = LocalTransportUtil.CreateInfoFile(info_name_fname, info, true);

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
                fds.h_nodename_s = nodeid1.Item2;
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
                nodeid1?.Item2?.Dispose();
                nodeid_lock?.Dispose();

                h_pid_id_s?.Dispose();
                h_pid_name_s?.Dispose();
                h_info_id_s?.Dispose();
                h_info_name_s?.Dispose();

                throw;
            }
        }

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

        
        private async Task DoListen(Socket socket, string socket_fname, UnixDomainSocketEndPoint  ep)
        {
            lock (this)
            {
                transportopen = true;
            }

            await Task.Delay(10);

            try
            {
               
                while (!close_token.IsCancellationRequested)
                {                    
                    var s2 = await socket.AcceptAsync();

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
        /// <param name="url">The url to check</param>
        /// <returns>True if url has scheme "local"</returns>
        public override bool CanConnectService(string url)
        {
            Uri u = new Uri(url);
            if (u.Scheme != "rr+local") return false;

            return true;
        }

        /// <inheretdoc/>
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

        LocalTransportFDs fds = new LocalTransportFDs();

        /// <inheretdoc/>
        public override Task Close()
        {
            lock (this)
            {
                transportopen = false;
            }

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

            close_task.TrySetResult(0);
            close_token.Cancel();

            fds.Dispose();
            discovery?.Dispose();

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
            TransportConnections.Remove(e);

            FireTransportEventListener(TransportListenerEventType.TransportConnectionClosed, e);
        }


        /// <inheretdoc/>
        public override uint TransportCapability(string name)
        {
            return base.TransportCapability(name);
        }

        public override Task<List<NodeDiscoveryInfo>> GetDetectedNodes(CancellationToken token)
        {
            var now = DateTime.UtcNow;
            var o = new List<NodeDiscoveryInfo>();

            string private_search_dir = LocalTransportUtil.GetTransportPrivateSocketPath();
            string my_username = LocalTransportUtil.GetLogonUserName();

            var o1 = LocalTransportUtil.FindNodesInDirectory(private_search_dir, "rr+local", now, my_username);
            o.AddRange(o1);

            var search_path = LocalTransportUtil.GetTransportPublicSearchPath();

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

        LocalTransportDiscovery discovery;
        public void EnableNodeDiscoveryListening()
        {
            lock(this)
            {
                if (discovery != null)
                {
                    throw new InvalidOperationException("LocalTransport discovery already running");
                }

                discovery = new LocalTransportDiscovery(this, node);
                discovery.Start();
            }
        }

        public void DisableNodeDiscoveryListening()
        {
            lock(this)
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
                    parent.TransportConnections.Remove(transport.LocalEndpoint);
                }
            }

            public Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate()
            {
                return null;
            }
        }

        internal readonly AsyncStreamTransportParent parent_adapter;
    }


    /// <summary>
    /// Implementation of a Local client transport connection.  This class should not be referenced directly,
    /// but should instead by used with LocalTransport.
    /// </summary>
    sealed class LocalClientTransport : AsyncStreamTransport
    {



        private Stream socket;
        //public NetworkStream netstream;

        private LocalTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        /// <summary>
        /// Creates a LocalClientTransport with parent LocalTransport
        /// </summary>
        /// <param name="c">Parent transport</param>
        public LocalClientTransport(LocalTransport c)
            : base(c.node, c.parent_adapter)
        {
            parenttransport = c;

        }

        string connecturl;

        /// <summary>
        /// Connects this transport connection to a LocalClient socket that connected to the listening server socket
        /// </summary>
        /// <param name="s"></param>
        public async Task Connect(Stream s, string connecturl, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            //LocalEndpoint = e.LocalEndpoint;

            socket = s;
            //socket.Client.NoDelay = true;
            this.connecturl = connecturl;
            m_LocalEndpoint = e.LocalEndpoint;

            m_Connected = true;
            await ConnectStream(socket, true, null, null, false, false, parenttransport.HeartbeatPeriod, cancel);

            parenttransport.TransportConnections.Add(LocalEndpoint, this);
        }

        public override string GetConnectionURL()
        {
            return connecturl;
        }
    }


    /// <summary>
    /// Implementation of a Local server transport connection.  This class should not be referenced directly,
    /// but should instead by used with LocalTransport.
    /// </summary>
    sealed class LocalServerTransport : AsyncStreamTransport
    {

        private Stream socket;
        private LocalTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;


        /// <summary>
        /// Creates a LocalClientTransport with parent LocalTransport
        /// </summary>
        /// <param name="c">Parent transport</param>
        public LocalServerTransport(LocalTransport c)
            : base(c.node, c.parent_adapter)
        {
            parenttransport = c;

        }

        /// <summary>
        /// Connects this transport connection to a LocalClient socket that connected to the listening server socket
        /// </summary>
        /// <param name="s"></param>
        public async Task Connect(Stream s, CancellationToken cancel = default(CancellationToken))
        {
            //LocalEndpoint = e.LocalEndpoint;

            socket = s;
            //socket.Client.NoDelay = true;

            m_Connected = true;
            await ConnectStream(socket, true, null, null, false, false, parenttransport.HeartbeatPeriod, cancel);
        }


        public override string GetConnectionURL()
        {
            return "rr+local://localhost/";
        }
    }



    static class LocalTransportUtil
    {

        public static string GetLogonUserName()
        {
            return System.Environment.UserName;
        }

        public static string GetUserDataPath()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {

                    var p = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

                    var p1 = Path.Combine(p, "RobotRaconteur");
                    if (!Directory.Exists(p1))
                    {
                        Directory.CreateDirectory(p1);
                    }
                    return p1;
                }
                else
                {
                    var p1 = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config/RobotRaconteur");
                    if (!Directory.Exists(p1))
                    {
                        Directory.CreateDirectory(p1);
                    }
                    return p1;
                }
            }

            catch (Exception ee)
            {
                throw new SystemResourceException("Could not activate system for local transport: " + ee.Message);
            }
        }

        private static int check_mkdir_res(int res)
        {
            if (Mono.Unix.Native.Syscall.GetLastError() == Mono.Unix.Native.Errno.EEXIST)
            {
                return 0;
            }
            return res;
        }
        public static string GetUserRunPath()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var p = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

                    var p1 = Path.Combine(p, "RobotRaconteur", "run");
                    if (!Directory.Exists(p1))
                    {
                        Directory.CreateDirectory(p1);
                    }
                    return p1;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    uint u = Mono.Unix.Native.Syscall.getuid();

                    string path;
                    if (u == 0)
                    {
                        path = "/var/run/robotraconteur/root/";
                        if (check_mkdir_res(Mono.Unix.Native.Syscall.mkdir(path, Mono.Unix.Native.FilePermissions.S_IRUSR
                            | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR)) < 0)
                        {
                            throw new SystemResourceException("Could not create root run directory");
                        }
                    }
                    else
                    {
                        string path1 = Environment.GetEnvironmentVariable("TMPDIR");
                        if (path1 == null)
                        {
                            throw new SystemResourceException("Could not determine TMPDIR");
                        }

                        path = Path.GetDirectoryName(path1.TrimEnd(Path.DirectorySeparatorChar));
                        path = Path.Combine(path, "C");
                        if (!Directory.Exists(path))
                        {
                            throw new SystemResourceException("Could not determine user cache dir");
                        }

                        path = Path.Combine(path, "robotraconteur");
                        if (check_mkdir_res(Mono.Unix.Native.Syscall.mkdir(path, Mono.Unix.Native.FilePermissions.S_IRUSR
                            | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR)) < 0)
                        {
                            throw new SystemResourceException("Could not create user run directory");
                        }
                    }
                    return path;
                }
                else
                {
                    uint u = Mono.Unix.Native.Syscall.getuid();

                    string path;
                    if (u == 0)
                    {
                        path = "/var/run/robotraconteur/root/";
                        if (check_mkdir_res(Mono.Unix.Native.Syscall.mkdir(path, Mono.Unix.Native.FilePermissions.S_IRUSR
                            | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR)) < 0)
                        {
                            throw new SystemResourceException("Could not create root run directory");
                        }
                    }
                    else
                    {
                        path = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");

                        if (path == null)
                        {
                            path = String.Format("/var/run/user/{0}/", u);
                        }

                        path = Path.Combine(path, "robotraconteur");
                        if (check_mkdir_res(Mono.Unix.Native.Syscall.mkdir(path, Mono.Unix.Native.FilePermissions.S_IRUSR
                            | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IXUSR)) < 0)
                        {
                            throw new SystemResourceException("Could not create user run directory");
                        }
                    }
                    return path;
                }
            }
            catch (Exception ee)
            {
                throw new SystemResourceException("Could not activate system for local transport: " + ee.Message);
            }
        }

        public static string GetUserNodeIDPath()
        {
            try
            {
                var p = Path.Combine(GetUserDataPath(), "nodeids");
                if (!Directory.Exists(p))
                {
                    Directory.CreateDirectory(p);
                }

                return p;
            }
            catch (Exception ee)
            {
                throw new SystemResourceException("Could not activate system for local transport: " + ee.Message);
            }
        }

        /*public static string GetTransportPrivateSocketPath()
        {

            var p1 = GetTransportSearchPath();
            var username = GetLogonUserName();

            var p = Path.Combine(p1, "RobotRaconteur-transport-" + username);
            if (!Directory.Exists(p1))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var sid_str = WindowsIdentity.GetCurrent().User.Value;
                    var security = new DirectorySecurity();
                    security.SetSecurityDescriptorSddlForm("D:(A;OICI;FR;;;WD)(A;OICI;FA;;;CO)(A;OICI;FA;;;BA)" + "(A;OICI;FA;;;" + sid_str + ")");
                    var dir_info = new DirectoryInfo(p);
                    FileSystemAclExtensions.Create(dir_info, security);
                }
                else
                {
                    Mono.Unix.Native.Syscall.mkdir(p, (Mono.Unix.Native.FilePermissions.S_IRWXU 
                        | Mono.Unix.Native.FilePermissions.S_IRGRP | Mono.Unix.Native.FilePermissions.S_IXGRP | Mono.Unix.Native.FilePermissions.S_IROTH 
                        | Mono.Unix.Native.FilePermissions.S_IXOTH));
                }
            }
            return p;
        }*/

        public static string GetTransportPrivateSocketPath()
        {
            try
            {
                string user_run_path = GetUserRunPath();
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

        public static string GetTransportPublicSocketPath()
        {
            string path1 = GetTransportPublicSearchPath();
            if (path1 == null)
            {
                return null;
            }

            try
            {
                string username = GetLogonUserName();

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

        public static string GetTransportPublicSearchPath()
        {
            try
            {
                string path1;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var sysdata_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create);
                    string username = GetLogonUserName();

                    path1 = Path.Combine(sysdata_path, "RobotRaconteur");
                    if (!Directory.Exists(path1))
                    {
                        return null;
                    }

                    var security = FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(path1));

                    var sid = security.GetOwner(typeof(SecurityIdentifier));
                    var current_user = WindowsIdentity.GetCurrent().User;
                    var local_service = new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null);
                    if (sid != current_user && sid != local_service)
                    {
                        return null;
                    }

                    path1 = Path.Combine(path1, "run", "transport", "local");

                }
                else
                {
                    path1 = "/var/run/robotraconteur/transport/local";
                }

                if (!Directory.Exists(path1))
                {
                    return null;
                }

                return path1;

            }
            catch (Exception ee)
            {
                throw new SystemResourceException("System not activated local transport: " + ee.Message);
            }
        }

        public static bool ReadInfoFile(string fname, out Dictionary<string, string> data)
        {
            try
            {
                using (var fd = new LocalTransportFD())
                {
                    int err_code;
                    if (!fd.OpenRead(fname, out err_code))
                    {
                        data = null;
                        return false;
                    }

                    if (!fd.ReadInfo())
                    {
                        data = null;
                        return false;
                    }

                    data = fd.Info;
                    return true;
                }
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        public static Tuple<NodeID, LocalTransportFD> GetNodeIDForNodeNameAndLock(string nodename)
        {
            NodeID nodeid = null;

            if (!Regex.IsMatch(nodename, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$"))
            {
                throw new ArgumentException("\"" + nodename + "\" is an invalid NodeName");
            }

            string p = Path.Combine(GetUserNodeIDPath(), nodename);

            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);


            LocalTransportFD fd = null;
            LocalTransportFD fd_run = null;
            try
            {
                if (is_windows)
                {
                    fd = new LocalTransportFD();

                    int error_code;
                    if (!fd.OpenLockWrite(p, false, out error_code))
                    {
                        if (error_code == 32)
                        {
                            throw new NodeNameAlreadyInUse();
                        }
                        throw new SystemResourceException("Could not initialize LocalTransport server");
                    }
                }
                else
                {
                    string p_lock = Path.Combine(GetUserRunPath(), "nodeids");

                    Directory.CreateDirectory(p_lock);

                    p_lock = Path.Combine(p_lock, nodename + ".pid");

                    fd_run = new LocalTransportFD();

                    int open_run_err;
                    if (!fd_run.OpenLockWrite(p_lock, false, out open_run_err))
                    {
                        if (open_run_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                        {
                            throw new NodeNameAlreadyInUse();
                        }
                        throw new SystemResourceException("Could not initialize LocalTransport server");
                    }

                    string pid_str = Process.GetCurrentProcess().Id.ToString();
                    if (!fd_run.Write(pid_str))
                    {
                        throw new SystemResourceException("Could not initialize LocalTransport server");
                    }

                    fd = new LocalTransportFD();
                                       
                    int open_err;
                    if (!fd.OpenLockWrite(p, false, out open_err))
                    {
                        if (open_err == (int)Mono.Unix.Native.Errno.EROFS)
                        {
                            open_err = 0;
                            if (!fd.OpenRead(p, out open_err))
                            {
                                throw new InvalidOperationException("LocalTransport NodeID not set on read only filesystem");
                            }
                        }
                        else
                        {
                            throw new SystemResourceException("Could not initialize LocalTransport server");
                        }
                    }
                }
                int len = fd.FileLen;

                if (len == 0 || len == -1 || len > 16 * 1024)
                {
                    nodeid = NodeID.NewUniqueID();
                    string dat = nodeid.ToString();
                    fd.Write(dat);
                }
                else
                {
                    string nodeid_str;
                    fd.Read(out nodeid_str);
                    try
                    {
                        nodeid_str = nodeid_str.Trim();
                        nodeid = new NodeID(nodeid_str);
                    }
                    catch (Exception)
                    {
                        throw new IOException("Error in NodeID mapping settings file");
                    }
                }

                if (is_windows)
                {
                    return Tuple.Create(nodeid, fd);
                }
                else
                {
                    fd?.Dispose();
                    return Tuple.Create(nodeid, fd_run);
                }
            }
            catch (Exception)
            {
                fd?.Dispose();
                fd_run?.Dispose();
                throw;
            }
        }
    
        public static LocalTransportFD CreatePidFile(string path, bool for_name)
        {
            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string pid_str = Process.GetCurrentProcess().Id.ToString();
            var fd = new LocalTransportFD();
            try
            {
                if (is_windows)
                {
                    int open_err;
                    if (!fd.OpenLockWrite(path, true, out open_err))
                    {
                        if (!fd.OpenLockWrite(path, false, out open_err))
                        {
                            if (open_err == 32)
                            {
                                if (!for_name)
                                {
                                    throw new NodeIDAlreadyInUse();
                                }
                                else
                                {
                                    throw new NodeNameAlreadyInUse();
                                }
                            }
                            throw new SystemResourcePermissionDeniedException("Could not initialize LocalTransport server");
                        }
                    }
                }
                else
                {
                    var old_mode = Mono.Unix.Native.Syscall.umask(~(Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IRGRP));
                    try
                    {
                        int open_err;
                        if (!fd.OpenLockWrite(path, true, out open_err))
                        {
                            if (!fd.OpenLockWrite(path, false, out open_err))
                            {
                                if (open_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                                {
                                    if (!for_name)
                                    {
                                        throw new NodeIDAlreadyInUse();
                                    }
                                    else
                                    {
                                        throw new NodeNameAlreadyInUse();
                                    }
                                }
                                throw new SystemResourcePermissionDeniedException("Could not initialize LocalTransport server");
                            }
                        }
                    }
                    finally
                    {
                        Mono.Unix.Native.Syscall.umask(old_mode);
                    }
                }

                fd.Write(pid_str);
                return fd;
            }
            catch (Exception)
            {
                fd?.Dispose();
                throw;
            }

        }

        public static LocalTransportFD CreateInfoFile(string path, Dictionary<string,string> info, bool for_name)
        {
            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string pid_str = Process.GetCurrentProcess().Id.ToString();
            string username = GetLogonUserName();

            var fd = new LocalTransportFD();
            try
            {
                if (is_windows)
                {
                    int open_err;
                    if (!fd.OpenLockWrite(path, true, out open_err))
                    {
                        if (!fd.OpenLockWrite(path, false, out open_err))
                        {
                            if (open_err == 32)
                            {
                                if (!for_name)
                                {
                                    throw new NodeIDAlreadyInUse();
                                }
                                else
                                {
                                    throw new NodeNameAlreadyInUse();
                                }
                            }
                            throw new SystemResourcePermissionDeniedException("Could not initialize LocalTransport server");
                        }
                    }
                }
                else
                {
                    var old_mode = Mono.Unix.Native.Syscall.umask(~(Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IRGRP));
                    try
                    {
                        int open_err;
                        if (!fd.OpenLockWrite(path, true, out open_err))
                        {
                            if (!fd.OpenLockWrite(path, false, out open_err))
                            {
                                if (open_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                                {
                                    if (!for_name)
                                    {
                                        throw new NodeIDAlreadyInUse();
                                    }
                                    else
                                    {
                                        throw new NodeNameAlreadyInUse();
                                    }
                                }
                                throw new SystemResourcePermissionDeniedException("Could not initialize LocalTransport server");
                            }
                        }
                    }
                    finally
                    {
                        Mono.Unix.Native.Syscall.umask(old_mode);
                    }
                }

                info["pid"] = pid_str;
                info["username"] = username;

                fd.Info = info;
                if (!fd.WriteInfo())
                {
                    throw new SystemResourceException("Could not initialize server");
                }
                return fd;
            }
            catch (Exception)
            {
                fd?.Dispose();
                throw;
            }
        }

        public static void RefreshInfoFile(LocalTransportFD h_info, string service_nonce)
        {
            if (h_info == null) return;

            lock(h_info)
            {
                h_info.Info.Remove("ServiceStateNonce");
                h_info.Info.Add("ServiceStateNonce", service_nonce);
            }

            h_info.Reset();
            h_info.WriteInfo();
        }

        public static List<NodeDiscoveryInfo> FindNodesInDirectory(string path, string scheme, DateTime now, string username)
        {
            var o = new List<NodeDiscoveryInfo>();

            string search_id = Path.Combine(path, "by-nodeid");
            string search_name = Path.Combine(path, "by-nodename");
            foreach(var f in Directory.EnumerateFiles(search_id))
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
                    if (!ReadInfoFile(f, out info))
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
                    i.NodeName = info.GetValueOrDefault("nodename");
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
                    if (!ReadInfoFile(f, out info))
                    {
                        continue;
                    }

                    if (!info.ContainsKey("nodeid") || !info.ContainsKey("nodename"))
                    {
                        continue;
                    }

                    NodeID nodeid = new NodeID(info["nodeid"]);
                    string nodename1 = info["nodename"];

                    if (nodename1 != Path.ChangeExtension(Path.GetFileName(f),""))
                    {
                        continue;
                    }

                    foreach(var e1 in o)
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

                    if (!ReadInfoFile(e2, out info_data))
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
                        if (!ReadInfoFile(e3, out info_data2))
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

                    if (!ReadInfoFile(e2, out info_data))
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
                    var ep = new UnixDomainSocketEndPoint (pipename);
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

    class LocalTransportFD : IDisposable
    {
        FileStream f;

        public Dictionary<string,string> Info { get; set; }

        public LocalTransportFD()
        {

        }

        public bool OpenRead(string path, out int error_code)
        {
            try
            {
                var h = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                f = h;
                error_code = 0;
                return true;
            }
            catch (Exception ee)
            {
                error_code = 0xFFFF & ee.HResult;
                return false;
            }

        }

        public bool OpenLockWrite(string path, bool delete_on_close, out int error_code)
        {
            FileOptions file_options = default(FileOptions);
            if (delete_on_close)
            {
                file_options |= FileOptions.DeleteOnClose;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var h = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, file_options);
                    f = h;
                }
                else
                {
                    var h = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 1024, file_options);
                    h.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        h.Lock(0, 0);
                    }
                    catch (Exception)
                    {
                        h.Dispose();
                        throw;
                    }

                    f = h;
                }

                error_code = 0;
                return true;
            }
            catch (Exception ee)
            {
                error_code = 0xFFFF & ee.HResult;
                return false;
            }
        }

        public bool Read(out string data)
        {
            try
            {
                f.Seek(0, SeekOrigin.Begin);
                long len = f.Length;
                var reader = new StreamReader(f);
                data = reader.ReadToEnd();
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        public bool ReadInfo()
        {
            string in_;
            if (!Read(out in_))
            {
                return false;
            }

            var lines = in_.Split('\n');
            Info = new Dictionary<string, string>();

            var r = new Regex("^\\s*([\\w+\\.\\-]+)\\s*\\:\\s*(.*)\\s*$");

            foreach (var l in lines)
            {
                var r_match = r.Match(l);
                if (!r_match.Success)
                    continue;

                Info.Add(r_match.Groups[1].Value, r_match.Groups[2].Value);
            }

            return true;
        }

        public bool Write(string data)
        {
            try
            {
                var w = new StreamWriter(f);
                w.Write(data);
                w.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool WriteInfo()
        {
            string data = String.Join("\n", Info.Select((v) => String.Format("{0}: {1}", v.Key, v.Value)));
            try
            {
                return Write(data);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Reset()
        {
            try
            {
                f.Seek(0, SeekOrigin.Begin);
                f.SetLength(0);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int FileLen
        {
            get
            {
                return (int)f.Length;
            }
        }

        public void Dispose()
        {
            f?.Dispose();
        }
    }

    class LocalTransportNodeLock<T> : IDisposable
    {
        static HashSet<T> nodeids = new HashSet<T>();

        public static LocalTransportNodeLock<T> Lock(T id)
        {
            if(!nodeids.Add(id))
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
            lock(nodeids)
            {
                nodeids.Remove(release_id);
            }
        }
    }

    class LocalTransportFDs : IDisposable
    {
        public LocalTransportFD h_nodename_s;
        public LocalTransportFD h_pid_id_s;
        public LocalTransportFD h_info_id_s;
        public LocalTransportFD h_pid_name_s;
        public LocalTransportFD h_info_name_s;
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

        public LocalTransportDiscovery(LocalTransport transport, RobotRaconteurNode node)
        {
            this.node = node;
            this.transport = transport;
        }

        public async Task Refresh(CancellationToken token)
        {
            var n = await transport.GetDetectedNodes(token);
            foreach (var n1 in n)
            {
                node.NodeDetected(n1);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                Refresh(default(CancellationToken)).GetAwaiter().GetResult();
            }
            catch (Exception) { }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                Refresh(default(CancellationToken)).GetAwaiter().GetResult();
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
                file_watcher_private.Path = LocalTransportUtil.GetTransportPrivateSocketPath();
                file_watcher_private.EnableRaisingEvents = true;
            }
            catch (Exception) { }

            try
            {
                file_watcher_public = NewFileSystemWatcher();
                file_watcher_public.Path = LocalTransportUtil.GetTransportPublicSearchPath();
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

    public class NodeIDAlreadyInUse : IOException
    {
        public NodeIDAlreadyInUse() : base("NodeID already in use") { }
        public NodeIDAlreadyInUse(string message) : base(message) { }
    }

    public class NodeNameAlreadyInUse : IOException
    {
        public NodeNameAlreadyInUse() : base("NodeName already in use") { }
        public NodeNameAlreadyInUse(string message) : base(message) { }
    }
}
