// Copyright 2011-2019 Wason Technology, LLC
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
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using System.Text.RegularExpressions;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Transport for Transport Control Protocol Internet Protocol (TCP/IP) networks
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
    The TcpTransport implements transport connections over TCP/IP networks. TCP/IP is the
    most common protocol used for Internet and Local Area Network (LAN) communication,
    including
    Ethernet and WiFi connections. The Transport Control Protocol (TCP) is a reliable stream
    protocol that establishes connections between devices using IP address and port pairs.
    Each adapter has an assigned address, and applications create connections on different
    ports.
    TcpTransport listens to the port specified in StartServer(), and the client uses
    a URL containing the IP address and port of the listening transport. The TcpTransport
    uses the established connection to pass messages between nodes.
    </para>
    <para>
    The IP protocol is available in two major versions, IPv4 and IPv6. The most common
    is IPv4, and its 32 bit address is typically written as four numbers,
    ie 172.17.12.174. IPv4 has a number of critical limitations, the greatest being
    its 2^32 address limit (approximately 4 billion). This is a problem when there are
    tens of billions of internet connected devices already present. IPv6 introduces a 128
    bit address space, which allows for approximately 3.4x10^38 possible addresses. The major
    advantage for Robot Raconteur is the introduction of "link-local" addresses. These
    addresses
    begin with "FE80::" and finish with an "EUI-64" address, which is tied to the MAC
    address
    of the adaptor. IPv4 addresses need to be assigned to devices locally, and have a tendency
    to change. IPv6 addresses are permanently assigned to the adapter itself, meaning that
    network configuration for LAN communication is essentially automatic. Robot Raconteur
    will prefer IPv6 connections when possible for this reason.
    </para>
    <para>
    The TcpTransport is capable of using "raw" streams that implement the Robot Raconteur
    message protocols, or to use HTTP WebSockets. HTTP WebSockets allow Robot Raconteur
    to communicate seamlessly with browsers and HTTP servers without requiring
    additional plugins. WebSockets provide additional security using "origins". See
    AddWebSocketAllowedOrigin() for more information.
    </para>
    <para>
    The TcpTransport supports TLS encryption using certificates. See tls_security for
    more information on TLS. The TcpTransport supports four modes of TLS encryption:
    </para>
    <list type="table">
    <listheader>
    <term>Scheme</term>
    <term>Description</term>
    <term>Direction</term>
    </listheader>
    <item>
    <term>rrs+tcp</term>
    <term>"Raw" protocol with TLS</term>
    <term>Both</term>
    </item>
    <item>
    <term>rr+wss</term>
    <term>Websocket over HTTPS</term>
    <term>Client Only</term>
    </item>
    <item>
    <term>rrs+ws</term>
    <term>Websocket with RobotRaconteur TLS over HTTP</term>
    <term>Both</term>
    </item>
    <item>
    <term>rrs+wss</term>
    <term>Websocket with RobotRaconteur TLS over HTTPS</term>
    <term>Client Only</term>
    </item>
    </list>
    
    <para>
    The different combinations of TLS and HTTPS for websockets are used for different
    scenarios.
    Robot Raconteur Core can initiate HTTPS connections, but cannot accept them. Accepting
    HTTPS connections requires a certificate issued by an authority like GoDaddy or Digicert,
    and is typically used with an HTTP server running RobotRaconteurWeb.
    </para>
    <para>
    TLS certificates for Robot Raconteur nodes are issued by Wason Technology, LLC using
    a root certificate that is "burned in" to Robot Raconteur Core. All devices running
    Robot Raconteur will support this certificate chain.
    </para>
    <para>
    Discovery for the TcpTransport is accomplished using User Defined Protocol (UDP) multicast
    and/or broadcast packets. Broadcast packets are sent to all connected devices, while
    multicast is sent to devices that have registered to receive them. Unlike TCP, the packets
    sent to broadcast or multicast are sent to the entire network. This allows for devices
    to find each other on the network.
    </para>
    <para>
    For IPv4, the broadcast address 255.255.255.255 on port 48653
    is used for discovery. By default, IPv4 is disabled in favor of IPv6. IPv6 uses the
    multicast
    following multicast addresses:
    </para>
    <list type="table">
    <listheader>
    <term>Address</term>
    <term>Scope</term>
    <term>Port</term>
    <term>Default?</term>
    </listheader>
    <item>
    <term>FF01::BA86</term>
    <term>Node-Local</term>
    <term>48653</term>
    <term>Disabled</term>
    </item>
    <item>
    <term>FF02::BA86</term>
    <term>Link-Local</term>
    <term>48653</term>
    <term>Enabled</term>
    </item>
    <item>
    <term>FF05::BA86</term>
    <term>Site-Local</term>
    <term>48653</term>
    <term>Disabled</term>
    </item>
    </list>
    
    <para>
    By default, discovery will only occur on the link-local IPv6 scope. This will
    find nodes on the local subnet, but will not attempt to pass through any routers.
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
    public sealed class TcpTransport : Transport
    {
        
        //protected int Port {get {return m_Port;}}
        private int m_Port;

        private bool transportopen = false;
        private CancellationTokenSource transportcancel = new CancellationTokenSource();

        /// <summary>
        /// Checks if the transport is configured to accept incoming connections
        /// </summary>
        [PublicApi] 
        public override bool IsServer { get { return true; } }
        /// <summary>
        /// Checks if the transport is configured to initiate client connections
        /// </summary>
        [PublicApi] 
        public override bool IsClient { get { return true; } }

        internal Dictionary<uint, AsyncStreamTransport> TransportConnections = new Dictionary<uint, AsyncStreamTransport>();
        
        /// <summary>
        /// Default message receive timeout in milliseconds
        /// </summary>
        /// <remarks>
        /// If a message is not received in the specified timeout
        /// period the connection is closed
        /// </remarks>
        [PublicApi] 
        public int DefaultReceiveTimeout { get; set; }
        /// <summary>
        /// Default connect timeout in milliseconds
        /// </summary>
        [PublicApi]
        public int DefaultConnectTimeout { get; set; }

        /// <summary>
        /// Allow incoming Web Socket connections from HTTP clients
        /// </summary>
        [PublicApi]  
        public bool AcceptWebSockets { get; set; }

        /// <summary>
        /// The supported URL transport schemes
        /// </summary>
        [PublicApi] 
        public override string[] UrlSchemeString { get { return new string[] {"tcp", "rr+tcp", "rrs+tcp", "rr+ws", "rrs+ws", "rr+wss", "rrs+wss"}; } }

        private int m_HeartbeatPeriod = 5000;

        /// <summary>
        /// The transport connection heartbeat period in milliseconds
        /// </summary>
        /// <remarks>
        /// The transport will send a connection test heartbeat message
        /// if a message has not been sent within the specified interval
        /// </remarks>
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
        /// The IPEndPoints that the transport is listening on for incoming connections
        /// </summary>
        [PublicApi] 
        public List<IPEndPoint> ListeningEndpoints
        {            
            get
            {                
                List<IPEndPoint> eps = new List<IPEndPoint>();
                foreach (TcpListener l in listeners)
                {
                    eps.Add((IPEndPoint)l.LocalEndpoint);
                }
                return eps;                
            }
        }

        /**
        <summary>
        Construct a new LocalTransport for a non-default node. Must be registered with node using
        node.RegisterTransport()
        </summary>
        <remarks>None</remarks>
        <param name="node">The node to use with the transport. Defaults to RobotRaconteurNode.s</param>
        */
        [PublicApi]
        public TcpTransport(RobotRaconteurNode node=null) : base(node)
        {
            DefaultReceiveTimeout = 15000;
            DefaultConnectTimeout = 2500;
            parent_adapter = new AsyncStreamTransportParentImpl(this);
            AcceptWebSockets = true;

            allowed_websocket_origins.Add("null"); //Why firefox??
            allowed_websocket_origins.Add("file://");
            allowed_websocket_origins.Add("chrome-extension://");
            allowed_websocket_origins.Add("http://robotraconteur.com");
            allowed_websocket_origins.Add("http://robotraconteur.com:80");
            allowed_websocket_origins.Add("http://*.robotraconteur.com");
            allowed_websocket_origins.Add("http://*.robotraconteur.com:80");
            allowed_websocket_origins.Add("https://robotraconteur.com");
            allowed_websocket_origins.Add("https://robotraconteur.com:443");
            allowed_websocket_origins.Add("https://*.robotraconteur.com");
            allowed_websocket_origins.Add("https://*.robotraconteur.com:443");
        }

#pragma warning disable 1591
        public override async  Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel)
        {
            TcpClientTransport p = new TcpClientTransport(this);
            p.ReceiveTimeout = DefaultReceiveTimeout;
            await p.ConnectTransport(url, e, cancel).ConfigureAwait(false);

            return p;
        }
        
        public override Task CloseTransportConnection(Endpoint e, CancellationToken cancel)
        {
            if (TransportConnections.ContainsKey(e.LocalEndpoint))
            TransportConnections[e.LocalEndpoint].Close();
            return Task.FromResult(0);
        }

        bool listen_started = false;
#pragma warning restore 1591

        /// <summary>
        /// Start the TcpTransport service listening on the specified TCP port
        /// </summary>
        /// <param name="porte">The TCP port to listen on</param>
        [PublicApi]
        public void StartServer(int porte)
        {
            //sProgramName = progname;
            lock (this)
            {
                if (listen_started) throw new InvalidOperationException("TcpTransport server already started");
                listen_started = true;
                m_Port = porte;
                transportopen = true;
            }
            //conthread = new Thread(WaitForConnections);
            //conthread.Start();

            StartWaitForConnections();
        }
        private List<TcpListener> listeners=new List<TcpListener>();

       
        private void StartWaitForConnections()
        {
              List<IPEndPoint> listener_endpoints=new List<IPEndPoint>();

            listener_endpoints.Add(new IPEndPoint(IPAddress.Any, m_Port));
            listener_endpoints.Add(new IPEndPoint(IPAddress.IPv6Any, m_Port));
                        
            int count = 0;

            foreach (IPEndPoint e in listener_endpoints)
            {
                //Console.WriteLine(e);
                try
                {
                    TcpListener listen = new TcpListener(e);

                    try
                    {
                        listen.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                    }
                    catch (Exception) { }
                    if (e.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        try
                        {
                            listen.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);
                        }
                        catch (Exception) { }
                    }
                    listen.Start();
                    listeners.Add(listen);

                    listen.BeginAcceptTcpClient(ClientConnected, listen);
                    //Console.WriteLine("Success");
                    count++;
                }
                catch (Exception ee)
                {
                    Console.WriteLine(ee.ToString());
                }
               
            }

            if (count == 0) throw new IOException("Could not bind to any adapters to listen for connections");
        }
                                   
        private void ClientConnected(IAsyncResult a)
        {
            TcpListener listen = (TcpListener)a.AsyncState;
            try
            {
                if (!transportopen) return;
                TcpClient tcpc = listen.EndAcceptTcpClient(a);

                ClientConnected2(tcpc).IgnoreResult();
                
            }
            catch (Exception)
            {
            }


            if (!transportopen) return;
            try
            {
                listen.BeginAcceptTcpClient(ClientConnected, listen);

            }
            catch (Exception)
            {
            }

        }

        private async Task ClientConnected2(TcpClient tcpc)
        {
            try
            {

                var s = tcpc.Client;
                var b=new byte[1024];

                int i=0;
                int trycount = 0;
                while (true)
                {
                    i = await Task<int>.Factory.FromAsync(delegate(AsyncCallback cb, object state)
                    {
                        return s.BeginReceive(b, 0, b.Length, SocketFlags.Peek, cb, state);
                    }, s.EndReceive, s).ConfigureAwait(false);

                    if (i > 4) break;
                    
                    trycount++;
                    if (trycount > 100)
                    {
                        tcpc.Close();
                        return;
                    }
                    await Task.Delay(10).ConfigureAwait(false);
                }


                string seed = ASCIIEncoding.ASCII.GetString(b, 0, 4);
                if (seed == "RRAC")
                {
                    //Hurray, we have a normal Robot Raconteur connection
                    TcpServerTransport c = new TcpServerTransport(this);
                    c.ReceiveTimeout = DefaultReceiveTimeout;
                    await c.Connect(tcpc).ConfigureAwait(false);
                    return;
                }

                if (seed == "GET " || seed == "GET\t")
                {
                    if (!AcceptWebSockets)
                    {
                        tcpc.Close();
                        return;
                    }

                    bool firstline = true;

                    int trycount2 = 0;

                    var request = new Dictionary<string, string>();
                    string path = null;

                    while (true)
                    {
                        i = 0;
                        //Hurray, we have a potential HTTP websocket request
                        trycount = 0;
                        while (b.Take(i).Count(x => x == 0x0A) == 0)
                        {
                            i = await Task<int>.Factory.FromAsync(delegate(AsyncCallback cb, object state)
                            {
                                return s.BeginReceive(b, 0, b.Length, SocketFlags.Peek, cb, state);
                            }, s.EndReceive, s).ConfigureAwait(false);
                            trycount++;
                            if (trycount > 100)
                            {
                                tcpc.Close();
                                return;
                            }
                            await Task.Delay(10).ConfigureAwait(false);
                        }

                        int endpos = 0;
                        for (int j = 0; j < i; j++)
                        {
                            if (b[j] == 0x0A)
                            {
                                endpos = j+1;
                                break;
                            }
                        }

                        byte[] b2 = new byte[endpos];
                        int lineread = s.Receive(b2);
                        if (lineread != endpos)
                        {
                            tcpc.Close();
                            return;
                        }

                        string line = UTF8Encoding.UTF8.GetString(b2).Trim();

                        if (line == "")
                        {
                            break;
                        }

                        
                        if (firstline)
                        {
                            var f = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            if (f[0] != "GET" || !f[2].StartsWith("HTTP"))
                            {
                                tcpc.Close();
                                return;
                            }

                            path = f[1];

                            firstline = false;
                        }
                        else
                        {
                            var f = line.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if ((f[0] == "Sec-WebSocket-Protocol" || f[0] == "Sec-WebSocket-Version") && request.ContainsKey(f[0]))
                            {
                                request[f[0]] = request[f[0]] + ", " + f[1].Trim();
                            }
                            else
                            {
                                request.Add(f[0].Trim(), f[1].Trim());
                            }

                        }



                        if (trycount2 > 100)
                        {
                            tcpc.Close();
                            return;
                        }
                    }

                    string error = null;

                    if (!request.ContainsKey("Upgrade") || !request.ContainsKey("Sec-WebSocket-Key") || !request.ContainsKey("Sec-WebSocket-Version"))
                    {
                        error = "426 Upgrade Required";
                    }

                    if (error == null && request.ContainsKey("Sec-WebSocket-Protocol"))
                    {
                        var p = request["Sec-WebSocket-Protocol"].Split(new char[] { ',' }).Select(x => x.Trim());
                        if (!p.Contains("robotraconteur.robotraconteur.com"))
                        {
                            error = "405 Invalid Protocol";
                        }
                    }

                    if (error == null && !request["Sec-WebSocket-Version"].Split(new char[] {','}).Select(x=>x.Trim()).Contains("13"))
                    {
                        error = "426 Upgrade Required";
                    }

                    if (error == null && request.ContainsKey("Origin"))
                    {
                        string origin1 = request["Origin"];
                        bool good_origin = false;

                        var res = Regex.Match(origin1, "(?:^([^:\\s]+://[^/]*).*$)|(?:null)");
                        if (res.Success)
                        {
                            string origin;
                            if (res.Groups[1].Success)
                            {
                                origin = res.Groups[1].Value;
                            }
                            else
                            {
                                origin = "null";
                            }

                            lock (this)
                            {
                                foreach (var e in allowed_websocket_origins)
                                {
                                    if (!e.Contains("*"))
                                    {
                                        if (e == origin)
                                        {
                                            good_origin = true;
                                            break;
                                        }

                                        if (e.EndsWith("://"))
                                        {
                                            if (origin.StartsWith(e))
                                            {
                                                good_origin = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var scheme_pos = e.IndexOf("://");
                                        if (scheme_pos == -1) continue;
                                        var test_scheme = e.Substring(0, scheme_pos) + "://";
                                        if (!origin.StartsWith(test_scheme)) continue;

                                        var origin2 = origin.ReplaceFirst(test_scheme, "");
                                        var e2 = e.ReplaceFirst(test_scheme, "").ReplaceFirst("*", "");
                                        if (origin2.EndsWith(e2))
                                        {
                                            good_origin = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (!good_origin)
                        {
                            error = "403 Forbidden Origin";
                        }
                    }

                    string accept = "";

                    if (error == null)
                    {
                        string key1 = request["Sec-WebSocket-Key"] + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

                        using (var sha1 = new SHA1Managed())
                        {
                            accept = Convert.ToBase64String(sha1.ComputeHash(ASCIIEncoding.ASCII.GetBytes(key1)));
                        }
                    }

                    string path2 = path.Split(new char[] { '?' })[0];

                    if (path2 != "/" && path2 != "*")
                    {
                        error = "404 File not found";  
                    }

                    var stream = tcpc.GetStream();
                    
                    if (error != null)
                    {
                        string response1 = "HTTP/1.1 " + error + "\r\n"
                            + "Upgrade: websocket\r\n"
                            + "Sec-WebSocket-Protocol: robotraconteur.robotraconteur.com\r\n"
                            + "Sec-WebSocket-Version: 13\r\n"
                            + "Connection: close\r\n"
                            + "\r\n";
                        
                        var bresponse1 = UTF8Encoding.UTF8.GetBytes(response1);
                        await stream.WriteAsync(bresponse1, 0, bresponse1.Length).ConfigureAwait(false);
                        tcpc.Close();
                        return;
                    }

                    string response = "HTTP/1.1 101 Switching Protocols\r\n"
                        + "Upgrade: websocket\r\n"
                        + "Connection: Upgrade\r\n"
                        + "Sec-WebSocket-Accept: " + accept + "\r\n"
                        + "Sec-WebSocket-Protocol: robotraconteur.robotraconteur.com\r\n"
                        + "Sec-WebSocket-Version: 13\r\n"
                        + "\r\n";

                    var bresponse = UTF8Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(bresponse, 0, bresponse.Length).ConfigureAwait(false);

                    var ws = new WebSocketStream(stream);
                    
                    string connecturl;

                    IPEndPoint ep = (IPEndPoint)tcpc.Client.LocalEndPoint;
                    if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        connecturl = "rr+ws://[" + ep.Address.ToString() + "]:" + ep.Port + "/";
                    }
                    else
                    {
                        connecturl = "rr+ws://" + ep.ToString() + "/";
                    }

                    TcpServerTransport c = new TcpServerTransport(this);
                    c.ReceiveTimeout = DefaultReceiveTimeout;

                    await c.Connect(ws, connecturl).ConfigureAwait(false);
                    return;
                                        
                }

                //Ug, we have an invalid request. Assume it is HTTP and send a generic 404 error response.

                var stream2 = tcpc.GetStream();
                string response2 = "HTTP/1.1 404 File Not Found\r\n";
                
                var bresponse2 = UTF8Encoding.UTF8.GetBytes(response2);
                await stream2.WriteAsync(bresponse2, 0, bresponse2.Length).ConfigureAwait(false);

                tcpc.Close();
                return;

                
                
            }
            catch (Exception exp) {
                // LogDebug transport connection lost
#if RR_LOG_DEBUG
                LogDebug("Transport connection lost: " + exp.Message, node, component: RobotRaconteur_LogComponent.Transport);
#endif

                tcpc.Close();
            }


        }

        /// <summary>
        /// Accept an incoming WebSocket previously accepted by a web server
        /// </summary>
        /// <remarks>
        /// Use this function to embed RobotRaconteurWeb in an ASP.NET server.
        /// Receive WebSockets as normal and pass to this function. To not
        /// use StartServer()
        /// </remarks>
        /// <param name="s">The WebSocket to accept</param>
        /// <param name="url">The URL used to connect the WebSocket</param>
        /// <returns></returns>
        [PublicApi]
        public async Task AcceptAndProcessServerWebSocket(WebSocket s, string url)
        {
            TcpServerTransport c = new TcpServerTransport(this);
            c.ReceiveTimeout = DefaultReceiveTimeout;
            await c.ProccessWebSocket(s, url).ConfigureAwait(false);
        }
#pragma warning disable 1591
        public override bool CanConnectService(string url)
        {
            Uri u = new Uri(url);
            if (UrlSchemeString.Contains(u.Scheme)) 
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
                await TransportConnections[m.header.SenderEndpoint].SendMessage(m, cancel).ConfigureAwait(false);
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
#pragma warning restore 1591

        /// <summary>
        /// Close the TcpTransport and all active connections
        /// </summary>
        /// <returns></returns>
        [PublicApi]
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

            foreach (TcpListener listen in listeners)
            {
                try
                {
                    listen.Stop();
                }
                catch { };
            }


            try
            {

                DisableNodeDiscoveryListening();
            }
            catch { };

            try
            {
                DisableNodeAnnounce();
            }
            catch { };

            base.Close();

            // LogTrace connection closed
            #if RR_LOG_TRACE
            LogTrace("Connection closed", node, component: RobotRaconteur_LogComponent.Transport);
            #endif

            return Task.FromResult(0);
        }
#pragma warning disable 1591
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
#pragma warning restore 1591

        private IPNodeDiscovery node_discovery;
        /**
        <summary>
        Enable node discovery listening
        </summary>
        <remarks>
        <para>
        By default enables listening on IPv6 link-local scope
        </para>
        <para> See IPNodeDiscoveryFlags constants
        </para>
        </remarks>
        <param name="flags">The flags specifying the scope</param>
        */
        [PublicApi]
        public void EnableNodeDiscoveryListening(IPNodeDiscoveryFlags flags=IPNodeDiscoveryFlags.LinkLocal)
        {
            if (node_discovery == null) node_discovery = new IPNodeDiscovery(this);
            node_discovery.StartListeningForNodes(flags);

        }
        /**
        <summary>
        Disable node discovery listening
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public void DisableNodeDiscoveryListening()
        {
            if (node_discovery == null) return;
            node_discovery.StopListeningForNodes();
        }
        /**
        <summary>
        Enable node discovery announce
        </summary>
        <remarks>
        <para>
        By default enables announce on IPv6 link-local scope
        </para>
        <para> See IPNodeDiscoveryFlags constants
        </para>
        </remarks>
        <param name="flags">The flags specifying the scope</param>
        */
        [PublicApi]
        public void EnableNodeAnnounce(IPNodeDiscoveryFlags flags=IPNodeDiscoveryFlags.LinkLocal)
        {
            if (node_discovery == null) node_discovery = new IPNodeDiscovery(this);
            node_discovery.StartAnnouncingNode(flags);
        }
        /**
        <summary>
        Disable node discovery announce
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public void DisableNodeAnnounce()
        {
            if (node_discovery == null) return;
            node_discovery.StopAnnouncingNode();
        }
                

        internal void RemoveTransportConnection(uint e)
        {
            TransportConnections.Remove(e);

            FireTransportEventListener(TransportListenerEventType.TransportConnectionClosed, e);
        }

#pragma warning disable 1591
        public override uint TransportCapability(string name)
        {
            return base.TransportCapability(name);
        }
#pragma warning restore 1591

        internal Tuple<X509Certificate, X509CertificateCollection> nodecertificate = null;

        internal Tuple<X509Certificate, X509CertificateCollection> TlsNodeCertificate
        {
            get
            {
                lock (this)
                {
                    return nodecertificate;
                }
            }
        }

        /// <summary>
        /// Check if a TLS node certificate has been loaded
        /// </summary>
        [PublicApi]
        public bool IsTlsNodeCertificateLoaded
        {
            get
            {
                lock (this)
                {
                    return nodecertificate != null;
                }
            }
        }
        /// <summary>
        /// Load a TLS node certificate. This is done automatically
        /// by SecureServerNodeSetup
        /// </summary>
        [PublicApi]
        public void LoadTlsNodeCertificate()
        {
            lock (this)
            {               
                if (nodecertificate != null)
                    throw new InvalidOperationException("Certificate already loaded");

                X509Certificate cert;
                X509CertificateCollection collection = new X509CertificateCollection();

                var store = new X509Store("My");
                store.Open(OpenFlags.ReadOnly);
                foreach (X509Certificate2 mCert in store.Certificates)
                {
                    if (mCert.SubjectName.Name == "CN=Robot Raconteur Node " + node.NodeID.ToString())
                    {
                        cert = mCert;
                        nodecertificate = Tuple.Create(cert, collection);

                    }
                }

                if (nodecertificate == null)
                    throw new Exception("Certificate not found");
            }
        }

        /// <summary>
        /// Set to require all connections to use TLS. Automatically configured by
        /// SecureServerNodeSetup
        /// </summary>
        [PublicApi]
        public bool RequireTls { get; set; }

        private class AsyncStreamTransportParentImpl : AsyncStreamTransportParent
        {
            TcpTransport parent;

            public AsyncStreamTransportParentImpl(TcpTransport parent)
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
                return parent.TlsNodeCertificate;
            }
        }

        internal readonly AsyncStreamTransportParent parent_adapter;

        /// <summary>
        /// Check if an active TcpTransportConnection is secure
        /// </summary>
        /// <param name="endpoint">The LocalEndpoint ID of the connection</param>
        /// <returns>True if secured with TLS</returns>
        [PublicApi]
        public bool IsTransportConnectionSecure(uint endpoint)
        { 
            try
            {
                AsyncStreamTransport t=null;
                lock(TransportConnections)
                {
                    if (!TransportConnections.ContainsKey(endpoint)) throw new ConnectionException("Transport connection not found");
                    t = TransportConnections[endpoint];
                }

                return t.IsSecure;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if an active TcpTransportConnection is secure
        /// </summary>
        /// <param name="endpoint">The Endpoint of the connection</param>
        /// <returns>True if secured with TLS</returns>
        [PublicApi]
        public bool IsTransportConnectionSecure(Endpoint endpoint)
        {
            return IsTransportConnectionSecure(endpoint.LocalEndpoint);
        }

        /// <summary>
        /// Check if an active TcpTransportConnection is secure
        /// </summary>
        /// <param name="obj">An object reference of a connected client</param>
        /// <returns>True if secured with TLS</returns>
        [PublicApi]
        public bool IsTransportConnectionSecure(object obj)
        {
            ServiceStub s = obj as ServiceStub;
            if (s == null) throw new InvalidOperationException("Object must be a service stub");
            return IsTransportConnectionSecure(s.RRContext.LocalEndpoint);
        }

        /// <summary>
        /// Check if an active TcpTransportConnection is secure
        /// </summary>
        /// <param name="transport">The transport connection</param>
        /// <returns>True if secured with TLS</returns>
        [PublicApi]
        public bool IsTransportConnectionSecure(ITransportConnection transport)
        {
            AsyncStreamTransport s = transport as AsyncStreamTransport;
            if (s == null) throw new ConnectionException("Transport connection not found");
            return s.IsSecure;
        }

        /// <summary>
        /// Check if the identity of the peer has been verified using TLS certificates
        /// </summary>
        /// <param name="endpoint">The LocalEndpoint ID of the connection</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsSecurePeerIdentityVerified(uint endpoint)
        {
            try
            {
                AsyncStreamTransport t = null;
                lock (TransportConnections)
                {
                    if (!TransportConnections.ContainsKey(endpoint)) throw new ConnectionException("Transport connection not found");
                    t = TransportConnections[endpoint];
                }

                return t.IsSecurePeerIdentityVerified;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the identity of the peer has been verified using TLS certificates
        /// </summary>
        /// <param name="endpoint">The Endpoint of the connection</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsSecurePeerIdentityVerified(Endpoint endpoint)
        {
            return IsSecurePeerIdentityVerified(endpoint.LocalEndpoint);
        }

        /// <summary>
        /// Check if the identity of the peer has been verified using TLS certificates
        /// </summary>
        /// <param name="obj">An object reference of a connected client</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsSecurePeerIdentityVerified(object obj)
        {
            ServiceStub s = obj as ServiceStub;
            if (s == null) throw new InvalidOperationException("Object must be a service stub");
            return IsSecurePeerIdentityVerified(s.RRContext.LocalEndpoint);
        }

        /// <summary>
        /// Check if the identity of the peer has been verified using TLS certificates
        /// </summary>
        /// <param name="transport">The transport connection</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsSecurePeerIdentityVerified(ITransportConnection transport)
        {
            AsyncStreamTransport s = transport as AsyncStreamTransport;
            if (s == null) throw new ConnectionException("Transport connection not found");
            return s.IsSecurePeerIdentityVerified;
        }

        /// <summary>
        /// Get the NodeID of the peer if verified using TLS certificates
        /// </summary>
        /// <param name="endpoint">The LocalEndpoint ID of the connection</param>
        /// <returns>The NodeID UUID as a string</returns>
        [PublicApi]
        public string GetSecurePeerIdentity(uint endpoint)
        {
            AsyncStreamTransport t = null;
            lock (TransportConnections)
            {
                if (!TransportConnections.ContainsKey(endpoint)) throw new ConnectionException("Transport connection not found");
                t = TransportConnections[endpoint];
            }

            return t.GetSecurePeerIdentity();
        }
        /// <summary>
        /// Get the NodeID of the peer if verified using TLS certificates
        /// </summary>
        /// <param name="endpoint">The Endpoint of the connection</param>
        /// <returns>The NodeID UUID as a string</returns>
        [PublicApi]
        public string GetSecurePeerIdentity(Endpoint endpoint)
        {
            return GetSecurePeerIdentity(endpoint.LocalEndpoint);
        }
        /// <summary>
        /// Get the NodeID of the peer if verified using TLS certificates
        /// </summary>
        /// <param name="obj">An object reference of a connected client</param>
        /// <returns>The NodeID UUID as a string</returns>
        [PublicApi]
        public string GetSecurePeerIdentity(object obj)
        {
            ServiceStub s = obj as ServiceStub;
            if (s == null) throw new InvalidOperationException("Object must be a service stub");
            return GetSecurePeerIdentity(s.RRContext.LocalEndpoint);
        }
        /// <summary>
        /// Get the NodeID of the peer if verified using TLS certificates
        /// </summary>
        /// <param name="transport">The transport connection</param>
        /// <returns>The NodeID UUID as a string</returns>
        [PublicApi]
        public string GetSecurePeerIdentity(ITransportConnection transport)
        {
            AsyncStreamTransport s = transport as AsyncStreamTransport;
            if (s == null) throw new ConnectionException("Transport connection not found");
            return s.GetSecurePeerIdentity();
        }

        List<string> allowed_websocket_origins=new List<string>();

        /**
         * <summary>Get the currently configured WebSocket origins</summary>
         * <remarks>
         * See AddWebSocketAllowedOrigin()
         * </remarks>
         * <returns>The currently configured WebSocket origins</returns>
         */
        [PublicApi] 
        public string[] GetWebSocketAllowedOrigins()
        {
            lock (this)
            {
                return allowed_websocket_origins.ToArray();
            }
        }
        /**
         * <summayr>brief Add a WebSocket allowed origin</summayr>
         * 
         * <remarks>
         * WebSockets are vulnerable to an attack method called "cross-site scripting" (XSS). In
         * XSS, a malicious website will attempt to create a connection to an arbitrary website or local
         * device and attempt to hack or otherwise interact with it. WebSockets protect against this attack
         * using an "Origin" HTTP header field specified in the header upon connection. This header
         * field contains the domain name, the IP address, and/or the port of the web page that is
         * attempting to initiate the connection. This header field is filled by the browser, and cannot
         * be modified by the web page. If the web page is loaded from the local filesystem, the origin
         * will be the empty string ("") or null ("null"). The Robot Raconteur Core library uses the
         * empty string origin when initiating WebSocket connections. By default, it accepts
         * the following origins:
         * 
         * * (empty string)
         * * "null"
         * * "file://"
         * * "http://robotraconteur.com"
         * * "http://robotraconteur.com:80"
         * * "http://(*).robotraconteur.com"
         * * "http://(*).robotraconteur.com:80"
         * * "https://robotraconteur.com"
         * * "https://robotraconteur.com:443"
         * * "https://(*).robotraconteur.com"
         * * "https://(*).robotraconteur.com:443"
         *
         * <para>
         *    Note: forward-slash-star is not a valid comment, assume (*) is *
         * </para>
         * <para>
         * The star symbol can be used for a subdomain wildcard when matching origins.
         * </para>
         * <para>
         * Additional allowed origins can be added using this function, or the
         * `--robotraconteur-tcp-ws-add-origin=` command line option if
         * the RobotRaconteurNodeSetup classes are being used.
         * </para>
         * </remarks>
         *
         * <param name="origin">he origin to add</param>
         */
        [PublicApi]
        public void AddWebSocketAllowedOrigin(string origin)
        {
            lock (this)
            {
                var res=Regex.Match(origin, "^([^:\\s]+)://(?:((?:\\[[A-Fa-f0-9\\:]+(?:\\%\\w*)?\\])|(?:[^\\[\\]\\:/\\?\\s]+))(?::([^\\:/\\?\\s]+))?)?$");
                if (!res.Success) throw new InvalidOperationException("Invalid WebSocket origin");

                if (res.Groups.Count < 3) throw new InvalidOperationException("Invalid WebSocket origin");

                string host = "";
                string port = "";
                string scheme = res.Groups[1].Value;

                if (!String.IsNullOrEmpty(res.Groups[2].Value))
                {
                    host = res.Groups[2].Value;
                    if (host.StartsWith("*"))
                    {
                        string host2 = host.Substring(1);
                        if (host2.Contains("*")) throw new InvalidOperationException("Invalid WebSocket origin");
                        if (host2.Length != 0)
                        {
                            if (!host2.StartsWith(".")) throw new InvalidOperationException("Invalid WebSocket origin");
                        }
                    }
                    else
                    {
                        if (host.Contains("*")) throw new InvalidOperationException("Invalid WebSocket origin");                        
                    }

                    port = res.Groups[3].Value;
                    if (!String.IsNullOrEmpty(port))
                    {
                        int v;
                        if (!int.TryParse(port, out v)) throw new InvalidOperationException("Invalid WebSocket origin");
                    }
                }
                else if (!String.IsNullOrEmpty(res.Groups[3].Value)) throw new InvalidOperationException("Invalid WebSocket origin");

                allowed_websocket_origins.Add(origin);

                if (scheme == "http" && String.IsNullOrEmpty(port))
                {
                    allowed_websocket_origins.Add(origin + ":80");
                }

                if (scheme == "https" && String.IsNullOrEmpty(port))
                {
                    allowed_websocket_origins.Add(origin + ":443");
                }

                if (scheme == "http" && port == "80")
                {
                    allowed_websocket_origins.Add(origin.Replace(":80", ""));
                }

                if (scheme == "https" && port == "443")
                {
                    allowed_websocket_origins.Add(origin.Replace(":443", ""));
                }

            }
        }
        /**
         * <summary>Remove a previously added WebSocket origin</summary>
         * <remarks>
         * See AddWebSocketAllowedOrigin()
         * </remarks>
         * <param name="origin">The origin to remove</param>
         */
        [PublicApi]
        public void RemoveWebSocketAllowedOrigin(string origin)
        {
            lock (this)
            {
                allowed_websocket_origins.Remove(origin);
            }
        }
#pragma warning disable 1591
        public override void LocalNodeServicesChanged()
        {
            if (node_discovery == null) return;
            node_discovery.SendAnnounceNow();
        }

        public override void SendDiscoveryRequest()
        {
            if (node_discovery == null) return;
            node_discovery.SendDiscoveryRequestNow();
        }

        public override async Task<List<NodeDiscoveryInfo>> GetDetectedNodes(CancellationToken token)
        {
            if (node_discovery != null)
            {
                node_discovery.SendDiscoveryRequestNow();
                await Task.Delay(1000).ConfigureAwait(false);
            }
            return new List<NodeDiscoveryInfo>();
        }
#pragma warning restore 1591
        /**
         * <summary>Get the TCP endpoints the server is listening with "all" addresses resolved to specific addresses</summary>
         *
         * 
         */
        [PublicApi] 
        public IPEndPoint[] ResolvedListenerEndpoints
        {
            get
            {
                var socket_listen_endpoints = ListeningEndpoints;

                if (socket_listen_endpoints.Count == 0)
                {
                    return new IPEndPoint[] { };
                }
                         
                HashSet<IPEndPoint> listener_endpoints = new HashSet<IPEndPoint>();
                foreach (var s in socket_listen_endpoints)
                {
                    listener_endpoints.Add(s);
                }

                bool ipv4_any = false;
                bool ipv6_any = false;
                int ipv4_any_port = 0;
                int ipv6_any_port = 0;

                var erase = new HashSet<IPEndPoint>();

                foreach (var it in listener_endpoints)
                {
                    if (IPAddress.Any.Equals(it.Address))
                    {
                        ipv4_any_port = it.Port;
                        ipv4_any = true;
                        erase.Add(it);                    
                    }
                    if (IPAddress.IPv6Any.Equals(it.Address))
                    {
                        ipv6_any_port = it.Port;
                        ipv6_any = true;
                        erase.Add(it);
                    }
                }

                foreach (var it in erase)
                {
                    listener_endpoints.Remove(it);
                }

                if (ipv4_any || ipv6_any)
                {
                    NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in adapters)
                    {
                        if (adapter.OperationalStatus == OperationalStatus.Up || adapter.OperationalStatus == OperationalStatus.Unknown)
                        {
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                            {
                                if (ipv4_any && uniCast.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    listener_endpoints.Add(new IPEndPoint(uniCast.Address, (int)ipv4_any_port));
                                }

                                if (ipv6_any && uniCast.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    listener_endpoints.Add(new IPEndPoint(uniCast.Address, (int)ipv6_any_port));
                                }
                            }
                        }
                    }
                }
                return listener_endpoints.ToArray();                
            }
        }

        /**
         * <summary>Get the TCP endpoints the server is listening on as Robot Raconteur candidate URLs</summary>
         *
         * <returns>Candidate connections urls for the node, without service specified</returns>
         */
         [PublicApi] 
        public override string[] ServerListenUrls
        {
            get
            {
                var o = new List<string>();
                var endpoints = this.ResolvedListenerEndpoints;
                foreach (var ep in endpoints)
                {
                    o.Add(string.Format("rr+tcp://{0}/?nodeid={1}", ep.ToString(), node.NodeID.ToString("D")));
                }
                return o.ToArray();
            }
        }

    }   


    
    sealed class TcpClientTransport : AsyncStreamTransport
    {



        private TcpClient socket;
        private WebSocket websocket;
        //public NetworkStream netstream;

        private TcpTransport parenttransport;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        public TcpClientTransport(TcpTransport c) : base(c.node, c.parent_adapter)
        {
            parenttransport = c;           
        }

        private string connecturl = null;

        public async Task ConnectTransport(string url, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            this.connecturl = url;

            var u = TransportUtil.ParseConnectionUrl(url);

            if (u.host == "") throw new ConnectionException("Invalid connection URL for TCP");

            if (u.scheme == "rr+ws" || u.scheme == "rrs+ws" || u.scheme == "rr+wss" || u.scheme == "rrs+wss")
            {
                await ConnectWebsocketTransport(url, e, cancel).ConfigureAwait(false);
                return;
            }



            /*Uri u = new Uri(url);
            //string[] s = u.Segments;

            if (u.Scheme == "ws")
            {
                await ConnectWebsocketTransport(url, e, cancel);
                return;
            }

            string ap = Uri.UnescapeDataString(u.AbsolutePath);
            if (ap[0] == '/')
                ap = ap.Remove(0, 1);

            string[] s = ap.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            
            string noden = s[s.Length-2];

            NodeID target_nodeid = null;
            string target_nodename = null;

            if (noden.Contains('[') || noden.Contains('{'))
            {
                NodeID remid = new NodeID(s[0]);
                target_nodeid = remid;
            }
            else
            {
                if (!Regex.Match(s[0], "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$").Success)
                {
                    throw new NodeNotFoundException("Invalid node name");
                }
                target_nodename = s[0];
            }*/

            var target_nodeid = u.nodeid;
            var target_nodename = u.nodename;

            if (!(u.path == "" || u.path == "/")) throw new ConnectionException("Invalid Connection URL");

            m_LocalEndpoint = e.LocalEndpoint;
            
            IPAddress addr;
            if (IPAddress.TryParse(u.host, out addr))
            {
                //addr.ScopeId = 10;

                IPEndPoint en = new IPEndPoint(addr, u.port);
                
                if (!(addr.IsIPv6LinkLocal && addr.ScopeId==0))
                {
                    //socket = new TcpClient(addr.AddressFamily);
                    //socket.Connect(en);
                    //socket = new TcpClient(u.Host, u.Port);

                    socket = null;
                    AutoResetEvent ev = new AutoResetEvent(false);
                    TcpClient socket1 = new TcpClient(addr.AddressFamily);

                    await socket1.ConnectAsync(addr, u.port).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);

                    socket = socket1;

                }
                else
                {
                    //We need to figure out the scope id for this connection.  This is a really
                    //annoying property of ipv6 link-local addresses.


                    //Start finding all the valid scope ids on this computer
                    List<long> scopeids = new List<long>();
                    NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in adapters)
                    {

                        if (adapter.OperationalStatus == OperationalStatus.Up)
                        {
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                            {
                                if (uniCast.Address.AddressFamily == AddressFamily.InterNetworkV6  && uniCast.Address.IsIPv6LinkLocal)
                                {
                                    if (uniCast.Address.ScopeId != 0)
                                    {
                                        scopeids.Add(uniCast.Address.ScopeId);
                                    }

                                }

                            }
                        }

                    }
                    //Finish finding all the scope ids

                    //Now just try all of them to see if we can get a connection



                    socket = null;
                    var wait_tasks = new List<Tuple<Task, TcpClient>>();
                    AutoResetEvent ev=new AutoResetEvent(false);
                    foreach (long sid in scopeids)
                    {
                        try
                        {
                            addr.ScopeId = sid;
                            en = new IPEndPoint(addr, u.port);
                            TcpClient socket1 = new TcpClient(addr.AddressFamily);
                            Task task1=socket1.ConnectAsync(addr,u.port);
                            wait_tasks.Add(Tuple.Create(task1, socket1));
                            
                        }
                        catch (Exception) { };                     

                    }

                    var active_wait_tasks = new List<Tuple<Task, TcpClient>>(wait_tasks);

                    Tuple<Task, TcpClient> found = null;
                    while (active_wait_tasks.Count > 0 && found == null)
                    {
                        try
                        {
                            await Task.WhenAny(active_wait_tasks.Select(x => x.Item1)).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);
                        }
                        catch { }

                        active_wait_tasks.Clear();

                        foreach (var t in wait_tasks)
                        {
                            if (t.Item1.IsCompleted && !t.Item1.IsFaulted && !t.Item1.IsCanceled && found == null)
                            {
                                found = t;
                                socket = t.Item2;
                                break;
                            }

                            if (!t.Item1.IsCompleted)
                            {
                                active_wait_tasks.Add(t);
                            }
                        }
                    }

                    foreach (var t in wait_tasks)
                    {
                        if (!Object.ReferenceEquals(t, found))
                        {
                            try
                            {
                                t.Item2.Close();
                            }
                            catch { }
                            var noop = t.Item1.IgnoreResult();
                        }

                    }

                    if (found==null)
                    {
                        throw new System.Exception("Could not connect to remote service " + url);
                    }                    
                }

            }
            else
            {
                //socket = new TcpClient(u.Host, u.Port);
                socket = null;
                AutoResetEvent ev = new AutoResetEvent(false);
                TcpClient socket1 = new TcpClient();

                await socket1.ConnectAsync(u.host, u.port).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);

                socket = socket1;

            }
            //socket = new TcpClient(u.Host, u.Port);
            //socket.ReceiveBufferSize = 100000;
            //socket.SendBufferSize = 100000;
            //socket.Client.NoDelay = true;
            //socket.Connect();
            /*if (socket.Available > 0)
            {
                byte[] rbuf = new byte[socket.Available + 10];
                socket.GetStream().Read(rbuf, 0, socket.Available);

            }*/
            
            m_Connected = true;

            bool tls = u.scheme == "rrs+tcp";

            await ConnectStream(socket.GetStream(), false, target_nodeid, target_nodename, tls, parenttransport.RequireTls, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);
            
                
            parenttransport.TransportConnections.Add(LocalEndpoint, this);
           

        }

        private async Task ConnectWebsocketTransport(string url, Endpoint e, CancellationToken cancel = default(CancellationToken))
        {
            var u = TransportUtil.ParseConnectionUrl(url);
            /*Uri u = new Uri(url);
                        
            string ap = Uri.UnescapeDataString(u.AbsolutePath);
            if (ap[0] == '/')
                ap = ap.Remove(0, 1);

            string[] s = ap.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);*/

            string http_scheme="ws";
            if (u.scheme.EndsWith("wss"))
            {
                http_scheme="wss";
            }
           
            Uri u2 = new Uri(url.ReplaceFirst(u.scheme + "://",http_scheme + "://"));
            

            NodeID target_nodeid = null;
            string target_nodename = null;

            /*if (noden.Contains('[') || noden.Contains('{'))
            {
                NodeID remid = new NodeID(s[s.Length-2]);
                target_nodeid = remid;
            }
            else
            {
                if (!Regex.Match(s[s.Length-2], "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$").Success)
                {
                    throw new NodeNotFoundException("Invalid node name");
                }
                target_nodename = s[s.Length-2];
            }*/

            m_LocalEndpoint = e.LocalEndpoint;
                        
            IPAddress addr;
            if (IPAddress.TryParse(u.host, out addr))
            {
                //addr.ScopeId = 10;

                IPEndPoint en = new IPEndPoint(addr, u.port);

                if (!(addr.IsIPv6LinkLocal && addr.ScopeId==0))
                {
                    //socket = new TcpClient(addr.AddressFamily);
                    //socket.Connect(en);
                    //socket = new TcpClient(u.Host, u.Port);

                    websocket = null;
                    AutoResetEvent ev = new AutoResetEvent(false);
                    ClientWebSocket socket1 = new ClientWebSocket();
                    socket1.Options.AddSubProtocol("robotraconteur.robotraconteur.com");
                     
                    await socket1.ConnectAsync(u2, cancel).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);

                    websocket = socket1;

                }
                else
                {
                    //We need to figure out the scope id for this connection.  This is a really
                    //annoying property of ipv6 link-local addresses.


                    //Start finding all the valid scope ids on this computer
                    List<long> scopeids = new List<long>();
                    NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in adapters)
                    {

                        if (adapter.OperationalStatus == OperationalStatus.Up)
                        {
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                            {
                                if (uniCast.Address.AddressFamily == AddressFamily.InterNetworkV6 && uniCast.Address.IsIPv6LinkLocal)
                                {
                                    if (uniCast.Address.ScopeId != 0)
                                    {
                                        scopeids.Add(uniCast.Address.ScopeId);
                                    }

                                }

                            }
                        }

                    }
                    //Finish finding all the scope ids

                    //Now just try all of them to see if we can get a connection



                    socket = null;
                    var wait_tasks = new List<Tuple<Task, ClientWebSocket>>();
                    
                    foreach (long sid in scopeids)
                    {
                        try
                        {
                            addr.ScopeId = sid;
                            en = new IPEndPoint(addr, u.port);
                            ClientWebSocket socket1 = new ClientWebSocket();
                            socket1.Options.AddSubProtocol("robotraconteur.robotraconteur.com");
                            Uri uu = new Uri(url);
                            Uri u3 = new Uri(http_scheme + "://" + en.ToString() +  "/" + uu.PathAndQuery);
                            Task task1 = socket1.ConnectAsync(u3, cancel);
                            wait_tasks.Add(Tuple.Create(task1, socket1));

                        }
                        catch (Exception) { };

                    }

                    try
                    {
                        await Task.WhenAny(wait_tasks.Select(x => x.Item1)).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);
                    }
                    catch { }

                    Tuple<Task, ClientWebSocket> found = null;
                    foreach (var t in wait_tasks)
                    {
                        if (t.Item1.IsCompleted && found == null)
                        {
                            found = t;
                            websocket = t.Item2;
                        }

                    }

                    foreach (var t in wait_tasks)
                    {
                        if (!Object.ReferenceEquals(t, found))
                        {
                            try
                            {
                                t.Item2.Abort();
                            }
                            catch { }
                            var noop = t.Item1.IgnoreResult();
                        }

                    }

                    if (found == null)
                    {
                        throw new System.Exception("Could not connect to remote service " + url);
                    }
                }

            }
            else
            {
                //socket = new TcpClient(u.Host, u.Port);
                websocket = null;
                
                ClientWebSocket socket1 = new ClientWebSocket();
                socket1.Options.AddSubProtocol("robotraconteur.robotraconteur.com");
                await socket1.ConnectAsync(u2, cancel).AwaitWithTimeout(parenttransport.DefaultConnectTimeout).ConfigureAwait(false);

                websocket = socket1;

            }
            
            m_Connected = true;

            bool tls = u.scheme == "rrs+ws" || u.scheme=="rrs+wss";

            var webstream = new WebSocketStreamWrapper(websocket);
            await ConnectStream(webstream, false, target_nodeid, target_nodename, tls, parenttransport.RequireTls, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);
            
            parenttransport.TransportConnections.Add(LocalEndpoint, this);


        }
        
        public override string GetConnectionURL()
        {
            return connecturl;
        }
    }


    sealed class TcpServerTransport : AsyncStreamTransport
    {

        private string connecturl;

        private TcpClient socket=null;
        private WebSocket websocket = null;
        private Stream websocket_stream = null;
        private TcpTransport parenttransport;
        private Stream socketstream;

        private DateTime LastMessageReceivedTime = DateTime.UtcNow;

        
        public TcpServerTransport(TcpTransport c) : base(c.node,c.parent_adapter)
        {            
            parenttransport = c;            
        }

        public async Task Connect(TcpClient s, CancellationToken cancel = default(CancellationToken))
        {
            this.m_RequireTls = parenttransport.RequireTls;
            //LocalEndpoint = e.LocalEndpoint;

            socket = s;
            //socket.ReceiveBufferSize = 10000;
            //socket.SendBufferSize = 10000;
            //socket.Client.NoDelay = true;

            m_Connected = true;
            socketstream = s.GetStream();

            await ConnectStream(socketstream, true, null, null, false, parenttransport.RequireTls, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);
        }

        internal async Task Connect(WebSocketStream s, string connecturl, CancellationToken cancel = default(CancellationToken))
        {
            this.m_RequireTls = parenttransport.RequireTls;
            this.websocket_stream = s;
            this.connecturl = connecturl;
            
            m_Connected = true;            

            await ConnectStream(s, true, null, null, false, parenttransport.RequireTls, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);
        }

        TaskCompletionSource<int> on_close_task = new TaskCompletionSource<int>();

        public async Task ProccessWebSocket(WebSocket s, string connecturl, CancellationToken cancel = default(CancellationToken))
        {


            this.connecturl = connecturl;

            if (!this.parenttransport.AcceptWebSockets) throw new InvalidOperationException("Transport not accepting websockets");
            
            /*IPEndPoint ep = (IPEndPoint)socket.Client.LocalEndPoint;
            if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                connecturl = "ws://[" + ep.Address.ToString() + "]:" + ep.Port + "/";
            }
            else
            {
                connecturl = "ws://" + ep.ToString() + "/";
            }*/

            m_Connected = true;
            this.websocket_stream = new WebSocketStreamWrapper(s);

            await ConnectStream(this.websocket_stream, true, null, null, false, parenttransport.RequireTls, parenttransport.HeartbeatPeriod, cancel).ConfigureAwait(false);
            await on_close_task.Task.ConfigureAwait(false);
        }
        
        /**
        <summary>
        Close the transport. Done automatically by node shutdown.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public override void Close()
        {
            base.Close();

            on_close_task.TrySetResult(0);
        }


        public override string GetConnectionURL()
        {
            if (socket != null && websocket==null)
            {
                string scheme = "rr+tcp";
                if (IsTls)
                {
                    scheme = "rrs+tcp";
                }

                string connecturl;
                IPEndPoint ep = (IPEndPoint)socket.Client.LocalEndPoint;
                if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    var addr2 = IPAddress.Parse(ep.Address.ToString());
                    addr2.ScopeId = 0;

                    connecturl = scheme + "://[" + addr2.ToString() + "]:" + ep.Port + "/";
                }
                else
                {
                    connecturl = scheme + "://" + ep.ToString() + "/";
                }

                return connecturl;
            }

            if ( websocket_stream != null)
            {
                if (IsTls)
                {
                    if (this.connecturl.StartsWith("rr+ws://"))
                        return this.connecturl.Replace("rr+ws://", "rrs+ws://");

                    if (this.connecturl.StartsWith("rr+wss://"))
                        return this.connecturl.Replace("rr+wss://", "rrs+wss://");
                    else
                        throw new ApplicationException("Internal error");
                }

                return this.connecturl;
            }
            

            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Configuration flags for IP network node discovery
    /// </summary>
    [Flags, PublicApi]
    public enum  IPNodeDiscoveryFlags
    {
        /// <summary>
        /// Use IPv6 FF01:: node local  multicast addresses
        /// </summary>
        [PublicApi]
        NodeLocal=0x1,
        /// <summary>
        /// Use IPv6 FF02:: link local  multicast addresses
        /// </summary>
        [PublicApi]
        LinkLocal=0x2,
        /// <summary>
        /// Use IPv6 FF05:: site local  multicast addresses
        /// </summary>
        [PublicApi]
        SiteLocal=0x4,
        /// <summary>
        /// Use IPv4 255.255.255.255 broadcast addresses
        /// </summary>
        [PublicApi]
        IPv4Broadcast = 0x8

    }

    sealed class IPNodeDiscovery
    {
        
        
        private const int ANNOUNCE_PORT=48653;

        private Socket recvsock;
        private Socket recvsockV6;

        byte[] recvbuf=new byte[4096];
        byte[] recvbufV6 = new byte[4096];
                
        private bool listening = false;
        private bool broadcasting = false;

        TcpTransport parent;

        IPNodeDiscoveryFlags broadcast_flags;
        IPNodeDiscoveryFlags listen_flags;
        IPNodeDiscoveryFlags listen_socket_flags;

        public IPNodeDiscovery(TcpTransport parent)
        {
            this.parent = parent;
            random = new Random();
        }

        Random random;

        public void StartListeningForNodes(IPNodeDiscoveryFlags flags)
        {
            if (listening) throw new InvalidOperationException("Already listening for nodes");
                                                
            this_request_id = NodeID.NewUniqueID();
            listen_flags = flags;

            InitUDPRecvSockets();

            listening = true;

            SendDiscoveryRequestNow();
        }

        private List<IPAddress> GetIPv6MulticastAddresses(IPNodeDiscoveryFlags flags)
        {
            var IPv6MulticastListenAddresses = new List<IPAddress>();

            if (flags.HasFlag(IPNodeDiscoveryFlags.NodeLocal))
            {
                IPv6MulticastListenAddresses.Add(IPAddress.Parse("FF01::BA86"));
            }

            if (flags.HasFlag(IPNodeDiscoveryFlags.LinkLocal))
            {
                IPv6MulticastListenAddresses.Add(IPAddress.Parse("FF02::BA86"));
            }

            if (flags.HasFlag(IPNodeDiscoveryFlags.SiteLocal))
            {
                IPv6MulticastListenAddresses.Add(IPAddress.Parse("FF05::BA86"));
            }

            return IPv6MulticastListenAddresses;
        }



        private bool GetUseIPv4(IPNodeDiscoveryFlags flags) => (flags.HasFlag(IPNodeDiscoveryFlags.IPv4Broadcast));

        private void InitUDPRecvSockets()
        {
            //Find the scope ids to add to the multicast groups.  Without including every possible
            //ip multicast range and scopeid combination this won't work.

            IPNodeDiscoveryFlags flags = broadcast_flags | listen_flags;

            if (flags == listen_socket_flags)
                return;

            listen_socket_flags = flags;

            try
            {
                recvsock?.Dispose();
            }
            catch { }

            try
            {
                recvsockV6?.Dispose();
            }
            catch { }

            List<long> scopeids = new List<long>();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up || adapter.OperationalStatus == OperationalStatus.Unknown)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                    {
                        if (uniCast.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            if (!scopeids.Contains(uniCast.Address.ScopeId))
                            {
                                scopeids.Add(uniCast.Address.ScopeId);
                            }

                    }
                }

            }




            recvsock = null;

            IPEndPoint iep = new IPEndPoint(IPAddress.Any, ANNOUNCE_PORT);
            if (GetUseIPv4(flags))
            {
                //Initialize the ipv4 socket for UDP broadcast receive
                recvsock = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);                
                //recvsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                recvsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                recvsock.Bind(iep);
            }

            IPEndPoint iepV6=null;
            recvsockV6 = null;

            var IPv6MulticastListenAddresses = GetIPv6MulticastAddresses(flags);

            if (IPv6MulticastListenAddresses.Count > 0)
            {
                try
                {
                    recvsockV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    IPAddress a = IPAddress.IPv6Any;
                    //a.ScopeId=14;
                    iepV6 = new IPEndPoint(a, ANNOUNCE_PORT);
                    recvsockV6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                    //byte[] sid = BitConverter.GetBytes(14);
                    //recvsockV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, sid);
                    //recvsockV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.Broadcast, 1);
                                        

                    // long sid2 = ((IPEndPoint)recvsock.LocalEndPoint).Address.ScopeId;
                    foreach (IPAddress ip in IPv6MulticastListenAddresses)
                    {
                        try
                        {
                            IPv6MulticastOption ipv6m = new IPv6MulticastOption(ip);
                            recvsockV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, ipv6m);

                            foreach (long sidi in scopeids)
                            {
                                IPv6MulticastOption ipv6m2 = new IPv6MulticastOption(ip, sidi);
                                try
                                {
                                    recvsockV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, ipv6m2);
                                }
                                catch (Exception)
                                {
                                };

                            }
                        }
                        catch { }

                    }
                    recvsockV6.Bind(iepV6);
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.ToString());
                }
            }

            try
            {
                recvep = (EndPoint)iep;
                recvsock?.BeginReceiveFrom(recvbuf, 0, 4096, SocketFlags.None, ref recvep, new AsyncCallback(recvsock_receivecallback), recvsock);
            }
            catch { }

            try
            {
                recvepV6 = (EndPoint)iepV6;
                recvsockV6?.BeginReceiveFrom(recvbufV6, 0, 4096, SocketFlags.None, ref recvepV6, new AsyncCallback(recvsockV6_receivecallback), recvsockV6);
            }
            catch { }


        }
        EndPoint recvep;
        EndPoint recvepV6;

        private void recvsock_receivecallback(IAsyncResult a)
        {
            try
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                int count = recvsock.EndReceiveFrom(a, ref ep);
                string stringData = Encoding.ASCII.GetString(recvbuf, 0, count);

                NodeAnnounceReceived(stringData);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }

            try
            {
                recvsock.BeginReceiveFrom(recvbuf, 0, 4096, SocketFlags.None, ref recvep, new AsyncCallback(recvsock_receivecallback), recvsock);

            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }

        }

        private void recvsockV6_receivecallback(IAsyncResult a)
        {
            try
            {
                EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
                int count = recvsockV6.EndReceiveFrom(a, ref ep);
                string stringData = Encoding.ASCII.GetString(recvbufV6, 0, count);

                NodeAnnounceReceived(stringData);
                //Console.WriteLine(stringData);
                
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }

            try
            {
                recvsockV6.BeginReceiveFrom(recvbufV6, 0, 4096, SocketFlags.None, ref recvepV6, new AsyncCallback(recvsockV6_receivecallback), recvsockV6);

            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }

        }

        private void NodeAnnounceReceived(string packet)
        {
            if (listening)
            {
                string seed = "Robot Raconteur Node Discovery Packet";
                if (packet.Substring(0, seed.Length) == seed)
                {
                    //if (!IPAddress.Parse(packet.Split(new char[] {'\n'})[1]).GetAddressBytes().SequenceEqual(node.NodeID))
                    parent.node.NodeAnnouncePacketReceived(packet);
                }
            }

            if (broadcasting)
            {
                string seed = "Robot Raconteur Discovery Request Packet";
                if (packet.Substring(0, seed.Length) == seed)
                {
                    string[] lines = packet.Split(new char[] { '\n' });
                    if (lines.Length < 3) return;
                    string[] idline = lines[1].Split(new char[] { ',' });
                    NodeID id = new NodeID(idline[0]);
                    if (id != this_request_id)
                    {
                        SendAnnounceNow();
                    }
                }
            }
        }

        public void StopListeningForNodes()
        {
            try
            {
                recvsock?.Close();
            }
            catch (Exception) { };

            try
            {
                recvsockV6.Close();
            }
            catch (Exception) { };

            listening = false;
            var t = discovery_request_timer;
            discovery_request_timer = null;
            t?.Dispose();
            
        }

        public void StartAnnouncingNode(IPNodeDiscoveryFlags flags)
        {
            if (broadcasting) throw new InvalidOperationException("Already broadcasting node");

            broadcast_flags = flags;

            broadcasting = true;
            int backoff = random.Next(100, 250);
            broadcast_timer = new Timer(x=> BroadcastAnnouncePacket().ContinueWith(y=> { }), null, backoff, 55000);
            next_broadcast = DateTime.UtcNow + TimeSpan.FromMilliseconds(100);
                        
            InitUDPRecvSockets();
        }

        DateTime next_broadcast;
        Timer broadcast_timer;

        public void StopAnnouncingNode()
        {
            if (!broadcasting) return;

            broadcasting = false;
            var t = broadcast_timer;
            broadcast_timer = null;
            t?.Dispose();
        }

                

        class BroadcastAddressInfo
        {
            public bool IsIpv6;
            public IPEndPoint AdapterEndPoint;
            public IPAddress AdapterBroadcastAddress;
            public PhysicalAddress AdapterPhysicalAddress;
        }

        private async Task BroadcastAnnouncePacket()
        {
            try
            {
                List<IPEndPoint> eps = parent.ListeningEndpoints;

                //Return if there is nothing to send
                if (eps.Count >= 0) {
                    await BroadcastPacket(delegate (string scheme, BroadcastAddressInfo binfo)
                    {
                        string nodeidstring = parent.node.NodeID.ToString();
                        string packetdata = "Robot Raconteur Node Discovery Packet\n";
                        packetdata += (parent.node.NodeName == "") ? "" + nodeidstring + "\n" : "" + nodeidstring + "," + parent.node.NodeName + "\n";
                        if (!binfo.IsIpv6)
                        {
                            IPEndPoint e = new IPEndPoint(new IPAddress(binfo.AdapterEndPoint.Address.GetAddressBytes()), binfo.AdapterEndPoint.Port);
                            packetdata += scheme + "://" + e.ToString() + "/?nodeid=" + nodeidstring + "&service=RobotRaconteurServiceIndex\n";
                        }
                        else
                        {
                            IPEndPoint e = new IPEndPoint(new IPAddress(binfo.AdapterEndPoint.Address.GetAddressBytes()), binfo.AdapterEndPoint.Port);
                            packetdata += scheme + "://[" + e.Address.ToString() + "]:" + e.Port.ToString() + "/?nodeid=" + nodeidstring + "&service=RobotRaconteurServiceIndex\n";
                        }
                        string service_data = "ServiceStateNonce: " + this.parent.node.ServiceStateNonce;

                        string packetdata2 = packetdata + service_data + "\n";
                        if (packetdata2.Length <= 2048)
                        {
                            packetdata = packetdata2;
                        }

                        return packetdata;
                    }, broadcast_flags, eps).ConfigureAwait(false);
                }
            }
            catch { }
        
            next_broadcast = DateTime.UtcNow + TimeSpan.FromMilliseconds(55000);
            if (broadcasting)
            {
                var t = broadcast_timer;
                t?.Change(55000, 55000);
            }
        }

        private async Task BroadcastPacket(Func<string,BroadcastAddressInfo,string> packet_gen, IPNodeDiscoveryFlags flags, List<IPEndPoint> eps)
        {             
            List<BroadcastAddressInfo> BroadcastAddresses = new List<BroadcastAddressInfo>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (IPEndPoint ep in eps)
            {                
                BroadcastAddressInfo binfo = new BroadcastAddressInfo();
                binfo.AdapterEndPoint = ep;
                //Console.WriteLine(uniCast.Address.ToString());
                //binfo.AdapterPhysicalAddress = adapter.GetPhysicalAddress();

                if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {

                    binfo.IsIpv6 = true;
                    //Console.WriteLine("preferred " + uniCast.AddressPreferredLifetime + " " + uniCast.Address.ToString());
                }
                else
                {
                    binfo.IsIpv6 = false;
                    binfo.AdapterBroadcastAddress = IPAddress.Broadcast;
                }
                BroadcastAddresses.Add(binfo);

                //Console.WriteLine(uniCast.Address.ToString());

            }
            foreach (BroadcastAddressInfo binfo in BroadcastAddresses)
            {
                var schemes = new List<string>();
                schemes.Add("rr+tcp");
                if (parent.IsTlsNodeCertificateLoaded)
                {
                    schemes.Add("rrs+tcp");
                }

                foreach (var scheme in schemes)
                {
                    if (!binfo.IsIpv6)
                    {
                        if (GetUseIPv4(flags))
                        {
                            try
                            {
                                string packetdata = packet_gen(scheme, binfo);
                                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                                IPEndPoint local_endpoint = new IPEndPoint(binfo.AdapterEndPoint.Address, ANNOUNCE_PORT);
                                IPEndPoint broadcast_endpoint = new IPEndPoint(binfo.AdapterBroadcastAddress, ANNOUNCE_PORT);
                                byte[] data = Encoding.ASCII.GetBytes(packetdata);
                                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                                sock.ExclusiveAddressUse = false;
                                sock.Bind(local_endpoint);
                                await Task.Factory.FromAsync<int>(delegate (AsyncCallback cb, object state)
                                {
                                    return sock.BeginSendTo(data, 0, data.Length, SocketFlags.None, broadcast_endpoint, cb, state);
                                },
                                        sock.EndSendTo, sock).ConfigureAwait(false);
                                sock.Close();
                            }
                            catch { }
                        }
                    }
                    else
                    {

                        if (!binfo.AdapterEndPoint.Address.IsIPv6LinkLocal)
                            continue;

                        try
                        {
                            var IPv6MulticastBroadcastAddresses = GetIPv6MulticastAddresses(flags);

                            string packetdata = packet_gen(scheme, binfo);
                            Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                            //sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                            IPEndPoint local_endpoint = new IPEndPoint(binfo.AdapterEndPoint.Address, ANNOUNCE_PORT);


                            sock.ExclusiveAddressUse = false;
                            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                            sock.Bind(local_endpoint);


                            byte[] sid = BitConverter.GetBytes(binfo.AdapterEndPoint.Address.ScopeId);

                            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, sid);
                            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 32);
                            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.HopLimit, 32);
                            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IpTimeToLive, 32);

                            IPAddress ips = new IPAddress(binfo.AdapterEndPoint.Address.GetAddressBytes());

                            byte[] data = Encoding.ASCII.GetBytes(packetdata);
                            foreach (IPAddress ip in IPv6MulticastBroadcastAddresses)
                            {
                                IPv6MulticastOption ipv6m = new IPv6MulticastOption(ip, binfo.AdapterEndPoint.Address.ScopeId);
                                sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, ipv6m);
                            }
                            byte[] interfaceArray = BitConverter.GetBytes((int)binfo.AdapterEndPoint.Address.ScopeId);


                            foreach (IPAddress ip in IPv6MulticastBroadcastAddresses)
                            {
                                IPEndPoint broadcast_endpoint = new IPEndPoint(ip, ANNOUNCE_PORT);
                                //broadcast_endpoint.Address.ScopeId = binfo.AdapterEndPoint.Address.ScopeId;

                                try
                                {
                                    var t = Task.Factory.FromAsync<int>(delegate(AsyncCallback cb, object state)
                                    {
                                        return sock.BeginSendTo(data, 0, data.Length, SocketFlags.None, broadcast_endpoint, cb, state);
                                    },
                                    sock.EndSendTo, sock);

                                    await t.ConfigureAwait(false);
                                }
                                catch { };
                            }


                            sock.Close();
                        }
                        catch
                        {

                        }

                    }
                }
            }
        }

        NodeID this_request_id;

        internal void SendAnnounceNow()
        {
            lock(this)
            {
                if (!broadcasting)
                    return;

                if (broadcast_timer != null)
                {
                    var fromnow = next_broadcast - DateTime.UtcNow;
                    int backoff = random.Next(500, 1000);
                    if (fromnow.Milliseconds > backoff || fromnow.Milliseconds < 0)
                    {                        
                        var t = broadcast_timer;

                        t?.Change(backoff, 55000);
                        next_broadcast = DateTime.UtcNow + TimeSpan.FromMilliseconds(500);                            
                    }
                }
            }
        }

        Timer discovery_request_timer;
        DateTime last_request_send_time;
        internal void SendDiscoveryRequestNow()
        {
            if (!listening) return;
            lock(this)
            {
                if (discovery_request_timer != null)
                    return;
                int delay = random.Next(250, 1000);
                discovery_request_timer = new Timer(HandleRequestTimer, 3, delay, Timeout.Infinite);                
            }
        }

        List<IPEndPoint> GetLocalIPEndpoints()
        {
            List<IPEndPoint> listener_endpoints = new List<IPEndPoint>();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up || adapter.OperationalStatus == OperationalStatus.Unknown)
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                    {
                        listener_endpoints.Add(new IPEndPoint(uniCast.Address, 0));

                    }
                }
            }

            return listener_endpoints;
        }


        async void HandleRequestTimer(object v)
        {
            try
            {
                var eps = GetLocalIPEndpoints();

                string packetdata = "Robot Raconteur Discovery Request Packet\n";
                packetdata += this_request_id.ToString() + "\n";

                await BroadcastPacket((x, y) => packetdata, listen_flags, eps).ConfigureAwait(false);

                lock (this)
                {
                    int c = (int)v;
                    c--;
                    if (c > 0)
                    {
                        int delay = random.Next(250, 1000);
                        var d = discovery_request_timer;
                        discovery_request_timer = new Timer(HandleRequestTimer, c, delay, Timeout.Infinite);
                        d.Dispose();
                    }
                    else
                    {
                        if ((last_request_send_time + TimeSpan.FromMilliseconds(1000)) > DateTime.UtcNow)
                        {
                            var d = discovery_request_timer;
                            discovery_request_timer = new Timer(HandleRequestTimer, c, 5, Timeout.Infinite);
                            d?.Dispose();
                        }
                        else
                        {
                            var d = discovery_request_timer;
                            discovery_request_timer = null;
                            d?.Dispose();
                        }
                    }
                }
            }
            catch
            {
                var d = discovery_request_timer;
                discovery_request_timer = null;
                d?.Dispose();
            }
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

    class WebSocketStream : Stream
    {

        Stream stream;

        Random random;

        public WebSocketStream(Stream stream)
        {
            this.stream = stream;
            random = new Random();
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
            int noop=((Task<int>)asyncResult).Result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count,default(CancellationToken)).GetAwaiter().GetResult();
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
            WriteAsync(buffer, offset, count, default(CancellationToken)).GetAwaiter().GetResult();
        }
                
        bool recv_inframe = false;
        ulong recv_framepos = 0;
        ulong recv_framelen = 0;
        byte[] recv_mask;
        bool recv_en_mask = false;

        object ping_request_lock = new object();
        bool ping_requested = false;
        byte[] ping_data = null;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!recv_inframe)
            {
                while (true)
                {
                    var h1 = new byte[2];
                    int h1_r = 0;
                    do
                    {
                        var h1_r1 = await stream.ReadAsync(h1, 0, h1.Length, cancellationToken).ConfigureAwait(false);
                        if (h1_r1 == 0) throw new IOException("Connection closed");
                        h1_r += h1_r1;
                    }
                    while (h1_r < h1.Length);

                    bool recv_fin1 = (h1[0] & 0x80) != 0;
                    byte opcode_recv1 = (byte)((h1[0] & 0xF));

                    bool recv_en_mask1 = (h1[1] & 0x80) != 0;
                    byte[] recv_mask1 = new byte[4];
                    byte payload_len1 = (byte)((h1[1] & 0x7F));

                    ulong data_len1 = 0;

                    if (payload_len1 < 126)
                    {
                        data_len1 = payload_len1;
                    }
                    else if (payload_len1 == 126)
                    {
                        var h2 = new byte[2];
                        int h2_r = 0;
                        do
                        {
                            var h2_r1 = await stream.ReadAsync(h2, 0, h2.Length, cancellationToken).ConfigureAwait(false);
                            if (h2_r1 == 0) throw new IOException("Connection closed");
                            h2_r += h2_r1;
                        }
                        while (h2_r < h2.Length);
                        data_len1 = BitConverter.ToUInt16(h2.Reverse().ToArray(), 0);
                    }
                    else
                    {
                        var h2 = new byte[8];
                        int h2_r = 0;
                        do
                        {
                            var h2_r1 = await stream.ReadAsync(h2, 0, h2.Length, cancellationToken).ConfigureAwait(false);
                            if (h2_r1 == 0) throw new IOException("Connection closed");
                            h2_r += h2_r1;
                        }
                        while (h2_r < h2.Length);
                        data_len1 = BitConverter.ToUInt64(h2.Reverse().ToArray(), 0);
                    }

                    if (recv_en_mask1)
                    {

                        var h3 = new byte[4];
                        int h3_r = 0;
                        do
                        {
                            var h3_r1 = await stream.ReadAsync(h3, 0, h3.Length, cancellationToken).ConfigureAwait(false);
                            if (h3_r1 == 0) throw new IOException("Connection closed");
                            h3_r += h3_r1;
                        }
                        while (h3_r < h3.Length);
                        Buffer.BlockCopy(h3, 0, recv_mask1, 0, 4);
                    }
                    switch (opcode_recv1)
                    {
                        case (byte)WebSocketOpcode.continuation:
                        case (byte)WebSocketOpcode.binary:
                            {
                                if ((ulong)count > data_len1) count = (int)data_len1;
                                var r = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                                if (r == 0)
                                {
                                    throw new IOException("Connection closed");
                                }
                                if (recv_en_mask1)
                                {
                                    for (int i = 0; i < r; i++)
                                    {
                                        buffer[i+offset] = (byte)(buffer[i+offset] ^ recv_mask1[i % 4]);
                                    }
                                }

                                if ((ulong)r < data_len1)
                                {
                                    recv_inframe = true;
                                    recv_framepos = (ulong)r;
                                    recv_framelen = data_len1;
                                    recv_mask = recv_mask1;
                                    recv_en_mask = recv_en_mask1;
                                }
                                else
                                {
                                    recv_inframe = false;
                                    recv_framepos = 0;
                                    recv_framelen = 0;
                                    recv_en_mask = false;
                                }

                                return r;
                            }
                        case (byte)WebSocketOpcode.text:
                            {
                                stream.Close();
                                throw new IOException("Invalid data type");
                            }
                        case (byte)WebSocketOpcode.close:
                        case (byte)WebSocketOpcode.ping:
                        case (byte)WebSocketOpcode.pong:
                            {
                                var h4 = new byte[data_len1];
                                int h4_r = 0;
                                do
                                {
                                    var h4_r1 = await stream.ReadAsync(h4, 0, h4.Length, cancellationToken).ConfigureAwait(false);
                                    if (h4_r1 == 0) throw new IOException("Connection closed");
                                    h4_r += h4_r1;
                                }
                                while (h4_r < h4.Length);

                                if (recv_en_mask1)
                                {
                                    for (int i = 0; i < h4.Length; i++)
                                    {
                                        h4[i] = (byte)(h4[i] ^ recv_mask1[i % 4]);
                                    }
                                }

                                switch (opcode_recv1)
                                {
                                    case (byte)WebSocketOpcode.close:
                                        throw new EndOfStreamException("Connection closed");
                                    case (byte)WebSocketOpcode.ping:
                                        {
                                            lock (ping_request_lock)
                                            {
                                                ping_requested = true;
                                                ping_data = h4;
                                            }
                                            var noop=SendPong().IgnoreResult();
                                        }
                                        break;
                                    case (byte)WebSocketOpcode.pong:
                                        break;

                                }
                                break;
                            }
                        default:
                            throw new IOException("Invalid message format");
                    }
                }
            }
            else
            {
                if ((ulong)count > recv_framelen-recv_framepos) count = (int)(recv_framelen-recv_framepos);
                var r = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                if (r == 0)
                {
                    throw new IOException("Connection closed");
                }
                if (recv_en_mask)
                {
                    for (int i = 0; i < r; i++)
                    {
                        buffer[i+offset] = (byte)(buffer[i+offset] ^ recv_mask[(recv_framepos + (ulong)i) % 4]);
                    }
                }

                ulong recv_framenewpos = recv_framepos + (ulong)r;

                if (recv_framenewpos < recv_framelen)
                {
                    recv_inframe = true;
                    recv_framepos = (ulong)recv_framenewpos;                    
                }
                else
                {
                    recv_inframe = false;
                    recv_framepos = 0;
                    recv_framelen = 0;
                    recv_en_mask = false;
                }

                return r;
            }
        }

        bool send_en_mask = false;

        public bool EnableSendMask
        {
            get
            {
                return send_en_mask;
            }
            set
            {
                send_en_mask = value;
            }
        }

        

        private async Task WriteAsync2(byte[] buffer, int offset, int count, byte command, CancellationToken cancellationToken)
        {
            var mask = new byte[4];
            random.NextBytes(mask);

            var headerlen = (count <= 125) ? 2 : 4;
            if (send_en_mask) headerlen += 4;
            var header = new byte[headerlen];
            header[0] = (byte)(0x80 | (command & 0xF));
            if (count <= 125)
            {
                header[1] = (byte)count;
                if (send_en_mask)
                {
                    Buffer.BlockCopy(mask, 0, header, 2, 4);
                }
            }
            else
            {
                header[1] =126;
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)count).Reverse().ToArray(), 0, header, 2, 2);
                if (send_en_mask)
                {
                    Buffer.BlockCopy(mask, 0, header, 4, 4);
                }
            }

            if (send_en_mask)
            {
                header[1] |= 0x80;
            }

            await stream.WriteAsync(header, 0, headerlen).ConfigureAwait(false);

            if (send_en_mask)
            {
                byte[] write_async_2_buf = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    write_async_2_buf[i] = (byte)(buffer[i + offset] ^ mask[i % 4]);
                }
                await stream.WriteAsync(write_async_2_buf, 0, count).ConfigureAwait(false);
            }
            else
            {
                await stream.WriteAsync(buffer, offset, count).ConfigureAwait(false);
            }
        }


        AsyncMutex write_mutex = new AsyncMutex();

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count <= 4096)
            {
                using (var t=await write_mutex.Lock().ConfigureAwait(false))
                {
                    await WriteAsync2(buffer, offset, count, (byte)WebSocketOpcode.binary, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            int pos = 0;
            while (pos < count)
            {
                int c = 4096;
                if (pos + 4096 > count)
                {
                    c = count - pos;
                }
                using (var t = await write_mutex.Lock().ConfigureAwait(false))
                {
                    await WriteAsync2(buffer, offset + pos, c, (byte)WebSocketOpcode.binary, cancellationToken).ConfigureAwait(false);
                    pos += c;
                }
            }

        }

        private async Task SendPong()
        {
            using (var t = await write_mutex.Lock().ConfigureAwait(false))
            {
                lock (ping_request_lock)
                {
                    if (!ping_requested) return;
                    ping_requested = false;
                }
                await WriteAsync2(ping_data, 0, ping_data.Length, (byte)WebSocketOpcode.pong, default(CancellationToken)).ConfigureAwait(false);
            }


        }

        public override void Close()
        {
            try
            {
                stream.Close();
            }
            catch (Exception) { }
        }

    }

    enum WebSocketOpcode : byte
    {
        continuation=0x0,
        text=0x1,
        binary=0x2,
        close=0x8,
        ping=0x9,
        pong=0xA
    }


}