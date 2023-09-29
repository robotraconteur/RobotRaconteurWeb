using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Mono.Options;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    [Flags]
    public enum RobotRaconteurNodeSetupFlags
    {
        None = 0x0,
        EnableNodeDiscoveryListening = 0x1,
        EnableNodeAnnounce = 0x2,
        EnableLocalTransport = 0x4,
        EnableTcpTransport = 0x8,
        // EnableHardwareTransport = 0x10,
        LocalTransportStartServer = 0x20,
        LocalTransportStartClient = 0x40,
        TcpTransportStartServer = 0x80,
        // TcpTransportStartServerPortSharer = 0x100,
        // DisableMessage4 = 0x200,
        // DisableStringTable = 0x400,
        // DisableTimeouts = 0x800,
        LoadTlsCert = 0x1000,
        RequireTls = 0x2000,
        LocalTransportServerPublic = 0x4000,
        TcpTransportListenLocalHost = 0x8000,
        NodeNameOverride = 0x10000,
        NodeIdOverride = 0x20000,
        TcpPortOverride = 0x40000,
        TcpWebSocketOriginOverride = 0x80000,
        EnableIntraTransport = 0x100000,
        IntraTransportStartServer = 0x200000,
        TcpTransportIpv4Discovery = 0x400000,
        TcpTransportIpv6Discovery = 0x800000,
        LocalTapEnable = 0x1000000,
        LocalTapName = 0x2000000,
        JumboMessage = 0x4000000,

        EnableAllTransports = EnableLocalTransport 
            | EnableTcpTransport 
            //| EnableHardwareTransport 
            | EnableIntraTransport,

        ClientDefault = EnableTcpTransport
        | EnableLocalTransport
        | EnableIntraTransport
        | EnableNodeDiscoveryListening
        | TcpTransportIpv6Discovery
        | LocalTransportStartClient,

        ClientDefaultAllowedOverride = EnableAllTransports
        | EnableNodeDiscoveryListening
        | TcpTransportIpv6Discovery
        | TcpTransportIpv4Discovery
        | LocalTransportStartClient
        // | DisableMessage4
        // | DisableStringTable
        // | DisableTimeouts
        | LoadTlsCert
        | RequireTls
        | NodeNameOverride
        | NodeIdOverride
        | JumboMessage,

        ServerDefault = EnableTcpTransport
        | EnableLocalTransport
        | EnableIntraTransport
        | LocalTransportStartServer
        | TcpTransportStartServer
        | IntraTransportStartServer
        | EnableNodeAnnounce
        | EnableNodeDiscoveryListening
        // | DisableStringTable
        | TcpTransportIpv6Discovery,

        ServerDefaultAllowedOverride = EnableAllTransports
        | TcpTransportIpv6Discovery
        | TcpTransportIpv4Discovery
        | LocalTransportStartServer
        | TcpTransportStartServer
        | IntraTransportStartServer
        | EnableNodeAnnounce
        | EnableNodeDiscoveryListening
        // | DisableMessage4
        // | DisableStringTable
        // | DisableTimeouts
        | LoadTlsCert
        | RequireTls
        | NodeNameOverride
        | NodeIdOverride
        | TcpPortOverride
        | TcpWebSocketOriginOverride
        | LocalTransportServerPublic
        // | TcpTransportStartServerPortSharer
        | JumboMessage
        | TcpTransportListenLocalHost,

        SecureServerDefault = EnableTcpTransport
        | EnableLocalTransport
        | EnableIntraTransport
        | LocalTransportStartServer
        | TcpTransportStartServer
        | IntraTransportStartServer
        | EnableNodeAnnounce
        | EnableNodeDiscoveryListening
        | LoadTlsCert
        | RequireTls
        // | DisableStringTable
        | TcpTransportIpv6Discovery,

        SecureServerDefaultAllowedOverride = EnableAllTransports
        | LocalTransportStartServer
        | TcpTransportStartServer
        | IntraTransportStartServer
        | EnableNodeAnnounce
        | EnableNodeDiscoveryListening
        | TcpTransportIpv6Discovery
        | TcpTransportIpv4Discovery
        //| DisableMessage4
        //| DisableStringTable
        //| DisableTimeouts
        | NodeNameOverride
        | NodeIdOverride
        | TcpPortOverride
        | TcpWebSocketOriginOverride
        | LocalTransportServerPublic
        //| TcpTransportStartServerPortSharer
        | JumboMessage
        | TcpTransportListenLocalHost
    }

    internal class FillOptionsDescriptionAddHelper
    {
        public string Prefix { get; }
        public RobotRaconteurNodeSetupFlags AllowedOverrides { get; set; }
        private OptionSet optionSet;

        public FillOptionsDescriptionAddHelper(OptionSet optionSet, string prefix, RobotRaconteurNodeSetupFlags allowedOverrides)
        {
            this.optionSet = optionSet;
            this.Prefix = prefix;
            this.AllowedOverrides = allowedOverrides;
        }

        public void Add<T>(string name, string descr, RobotRaconteurNodeSetupFlags flag)
        {
            if ((flag & AllowedOverrides) != 0)
            {
                string combinedName = Prefix + name;
                optionSet.Add(combinedName + "=", descr, v => { /* You can add logic here to store/process 'v' */ });
            }
        }

        public void Add<T>(string name, string descr)
        {
            string combinedName = Prefix + name;
            optionSet.Add(combinedName + "=", descr, v => { /* You can add logic here to store/process 'v' */ });
        }
    }

    public class CommandLineConfigParser
    {
        private OptionSet desc = new OptionSet();
        private Dictionary<string, string> parsedOptions = new Dictionary<string, string>();
        private string prefix;

        private string default_node_name;
        private ushort default_tcp_port;
        private RobotRaconteurNodeSetupFlags default_flags;

        public CommandLineConfigParser(RobotRaconteurNodeSetupFlags allowed_overrides, string prefix = "robotraconteur-")
        {
            default_tcp_port = 48653;
            default_flags = 0;
            this.prefix = prefix;
            FillOptionsDescription(desc, allowed_overrides, prefix);
        }

        public void SetDefaults(string node_name, ushort tcp_port, RobotRaconteurNodeSetupFlags default_flags)
        {
            this.default_node_name = node_name;
            this.default_tcp_port = tcp_port;
            this.default_flags = default_flags;
        }

        public void AddStringOption(string name, string descr)
        {
            desc.Add(prefix + name + "=", descr, v => parsedOptions[name] = v);
        }

        public void AddBoolOption(string name, string descr)
        {
            desc.Add(prefix + name, descr, v => parsedOptions[name] = v != null ? "true" : "false");
        }

        public void AddIntOption(string name, string descr)
        {
            desc.Add(prefix + name + "=", descr, (int v) => parsedOptions[name] = v.ToString());
        }

        public void ParseCommandLine(string[] args)
        {
            if (args != null)
            {
                desc.Parse(args);
            }
        
        }

        public void ParseCommandLine(List<string> args)
        {
            if (args != null)
            {
                desc.Parse(args);
            }
        }

        // Note: There's no direct equivalent in Mono.Options to accept pre-parsed results as in boost::program_options. 

        public string GetOptionOrDefaultAsString(string option)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return parsedOptions[option1].ToString();
            }

            if (option == "nodename")
            {
                return this.default_node_name;
            }

            // List of options that return empty string by default
            var emptyDefaultOptions = new List<string>
            {
                "log-level",
                "tcp-ws-add-origins",
                "tcp-ws-remove-origins",
                "local-tap-name"
            };

            if (emptyDefaultOptions.Contains(option))
            {
                return string.Empty;
            }

            throw new ArgumentException($"Required option not provided: {option}");
        }

        public string GetOptionOrDefaultAsString(string option, string defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return parsedOptions[option1].ToString();
            }

            return defaultValue;
        }

        public bool GetOptionOrDefaultAsBool(string option)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return bool.Parse(parsedOptions[option1]);
            }

            Dictionary<string, RobotRaconteurNodeSetupFlags> optionFlags = new Dictionary<string, RobotRaconteurNodeSetupFlags>
            {
                {"discovery-listening-enable", RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening},
                {"discovery-announce-enable", RobotRaconteurNodeSetupFlags.EnableNodeAnnounce},
                {"local-enable", RobotRaconteurNodeSetupFlags.EnableLocalTransport},
                {"tcp-enable", RobotRaconteurNodeSetupFlags.EnableTcpTransport},
                //{"hardware-enable", RobotRaconteurNodeSetupFlags.EnableHardwareTransport},
                {"intra-enable", RobotRaconteurNodeSetupFlags.EnableIntraTransport},
                {"local-start-server", RobotRaconteurNodeSetupFlags.LocalTransportStartServer},
                {"local-start-client", RobotRaconteurNodeSetupFlags.LocalTransportStartClient},
                {"local-server-public", RobotRaconteurNodeSetupFlags.LocalTransportServerPublic},
                {"tcp-start-server", RobotRaconteurNodeSetupFlags.TcpTransportStartServer},
                //{"tcp-start-server-sharer", RobotRaconteurNodeSetupFlags.TcpTransportStartServerPortSharer},
                //{"tcp-listen-localhost", RobotRaconteurNodeSetupFlags.TcpTransportListenLocalhost},
                //{"tcp-ipv4-discovery", RobotRaconteurNodeSetupFlags.TcpTransportIPv4Discovery},
                //{"tcp-ipv6-discovery", RobotRaconteurNodeSetupFlags.TcpTransportIPv6Discovery},
                {"intra-start-server", RobotRaconteurNodeSetupFlags.IntraTransportStartServer},
                //{"disable-timeouts", RobotRaconteurNodeSetupFlags.DisableTimeouts},
                //{"disable-message4", RobotRaconteurNodeSetupFlags.DisableMessage4},
                //{"disable-stringtable", RobotRaconteurNodeSetupFlags.DisableStringtable},
                {"load-tls", RobotRaconteurNodeSetupFlags.LoadTlsCert},
                {"require-tls", RobotRaconteurNodeSetupFlags.RequireTls},
                {"local-tap-enable", RobotRaconteurNodeSetupFlags.LocalTapEnable},
                {"jumbo-message", RobotRaconteurNodeSetupFlags.JumboMessage}
            };

            if (optionFlags.ContainsKey(option))
            {
                return (this.default_flags & optionFlags[option]) != 0;
            }

            throw new ArgumentException($"Required option not provided: {option}");
        }

        public bool GetOptionOrDefaultAsBool(string option, bool defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return bool.Parse(parsedOptions[option1]);
            }

            return defaultValue;
        }

        public int GetOptionOrDefaultAsInt(string option)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return int.Parse(parsedOptions[option1].ToString());
            }

            if (option == "tcp-port")
            {
                return default_tcp_port;
            }

            throw new ArgumentException($"Required option not provided: {option}");
        }

        public int GetOptionOrDefaultAsInt(string option, int defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return int.Parse(parsedOptions[option1].ToString());
            }

            return defaultValue;
        }

        public void FillOptionsDescription(OptionSet optionSet, RobotRaconteurNodeSetupFlags allowedOverrides, string prefix)
        {
            var h = new FillOptionsDescriptionAddHelper(optionSet, prefix, allowedOverrides);

            h.Add<bool>("discovery-listening-enable", "enable node discovery listening",
                RobotRaconteurNodeSetupFlags.EnableNodeDiscoveryListening);
            h.Add<bool>("discovery-announce-enable", "enable node discovery announce",
                RobotRaconteurNodeSetupFlags.EnableNodeAnnounce);
            h.Add<bool>("local-enable", "enable Local transport", RobotRaconteurNodeSetupFlags.EnableLocalTransport);
            h.Add<bool>("tcp-enable", "enable TCP transport", RobotRaconteurNodeSetupFlags.EnableTcpTransport);
            //h.Add<bool>("hardware-enable", "enable Hardware transport", RobotRaconteurNodeSetupFlags.EnableHardwareTransport);
            h.Add<bool>("intra-enable", "enable Intra transport", RobotRaconteurNodeSetupFlags.EnableIntraTransport);
            h.Add<bool>("local-start-server", "start Local server listening",
                RobotRaconteurNodeSetupFlags.LocalTransportStartServer);
            h.Add<bool>("local-start-client", "start Local client with node name",
                RobotRaconteurNodeSetupFlags.LocalTransportStartClient);
            h.Add<bool>("local-server-public", "local server is public on system",
                RobotRaconteurNodeSetupFlags.LocalTransportServerPublic);
            h.Add<bool>("tcp-start-server", "start TCP server listening",
                RobotRaconteurNodeSetupFlags.TcpTransportStartServer);
            /*h.Add<bool>("tcp-listen-localhost", "TCP server listen on localhost only",
                RobotRaconteurNodeSetupFlags.TcpTransportListenLocalhost);*/
            h.Add<string>("tcp-ws-add-origins", "add websocket origins (comma separated)",
                RobotRaconteurNodeSetupFlags.TcpWebSocketOriginOverride);
            h.Add<string>("tcp-ws-remove-origins", "remove websocket origins (comma separated)",
                RobotRaconteurNodeSetupFlags.TcpWebSocketOriginOverride);
            /*h.Add<bool>("tcp-start-server-sharer", "start TCP server listening using port sharer",
                RobotRaconteurNodeSetupFlags.TcpTransportStartServerPortSharer);*/
            h.Add<bool>("tcp-ipv4-discovery", "use IPv4 for discovery",
                RobotRaconteurNodeSetupFlags.TcpTransportIpv4Discovery);
            h.Add<bool>("tcp-ipv6-discovery", "use IPv6 for discovery",
                RobotRaconteurNodeSetupFlags.TcpTransportIpv6Discovery);
            h.Add<bool>("intra-start-server", "start Intra server listening",
                RobotRaconteurNodeSetupFlags.IntraTransportStartServer);
            /*h.Add<bool>("disable-timeouts", "disable timeouts for debugging", RobotRaconteurNodeSetupFlags.DisableTimeouts);
            h.Add<bool>("disable-message4", "disable message v4", RobotRaconteurNodeSetupFlags.DisableMessage4);
            h.Add<bool>("disable-stringtable", "disable message v4 string table",
                RobotRaconteurNodeSetupFlags.DisableStringtable);*/
            h.Add<bool>("load-tls", "load TLS certificate", RobotRaconteurNodeSetupFlags.LoadTlsCert);
            h.Add<bool>("require-tls", "require TLS for network communication", RobotRaconteurNodeSetupFlags.RequireTls);

            h.Add<string>("nodename", "node name to use for node", RobotRaconteurNodeSetupFlags.NodeNameOverride);
            h.Add<string>("nodeid", "node id to use fore node", RobotRaconteurNodeSetupFlags.NodeIdOverride);
            h.Add<int>("tcp-port", "port to listen on for TCP server", RobotRaconteurNodeSetupFlags.TcpPortOverride);

            h.Add<string>("log-level", "log level for node");

            h.Add<bool>("local-tap-enable", "start local tap interface, must also specify tap name",
                RobotRaconteurNodeSetupFlags.LocalTapEnable);
            h.Add<string>("local-tap-name", "name of local tap", RobotRaconteurNodeSetupFlags.LocalTapName);

            h.Add<bool>("jumbo-message", "enable jumbo messages (up to 100 MB)", RobotRaconteurNodeSetupFlags.JumboMessage);
        }
    }

    public class RobotRaconteurNodeSetup : IDisposable
    {
        public RobotRaconteurNode Node => node;

        public IntraTransport IntraTransport => intra_transport;
        public TcpTransport TcpTransport => tcp_transport;

        public LocalTransport LocalTransport => local_transport;

        internal RobotRaconteurNode node = null;

        internal IntraTransport intra_transport = null;
        internal TcpTransport tcp_transport = null;

        public LocalTransport local_transport = null;

        private CommandLineConfigParser config;

        public CommandLineConfigParser Config => config;

        public void DoSetup(RobotRaconteurNode node, ServiceFactory[] serviceTypes, bool scan_assembly_types, CommandLineConfigParser config)
        {
            this.node = node;

            node.SetLogLevelFromEnvVariable();

            string logLevelStr = config.GetOptionOrDefaultAsString("log-level");
            if (!string.IsNullOrEmpty(logLevelStr))
            {
                node.SetLogLevelFromString(logLevelStr);
            }

            /*if (config.GetOptionOrDefaultAsBool("local-tap-enable"))
            {
                string tapName = config.GetOptionOrDefaultAsString("local-tap-name");
                if (!string.IsNullOrEmpty(tapName))
                {
                    try
                    {
                        LocalMessageTap localTap = new LocalMessageTap(tapName);
                        localTap.Open();
                        node.SetMessageTap(localTap);
                        LogInfo($"Local tap initialized with name \"{tapName}\"");
                    }
                    catch (Exception exp)
                    {
                        LogError($"Local tap initialization failed: {exp.Message}");
                    }
                }
                else
                {
                    LogError("Local tap name not specified, not starting tap interface");
                }
            }*/

            string nodeName = config.GetOptionOrDefaultAsString("nodename");
            string nodeId = config.GetOptionOrDefaultAsString("nodeid", "");
            ushort tcpPort = (ushort)config.GetOptionOrDefaultAsInt("tcp-port");

                    //LogInfo($"Setting up RobotRaconteurNode version {node.GetRobotRaconteurVersion()} with NodeName: \"{nodeName}\" TCP port: {tcpPort}");
                    LogInfo($"Setting up RobotRaconteurNode with NodeName: \"{nodeName}\" TCP port: {tcpPort}");

            if (serviceTypes != null)
            {
                foreach (var factory in serviceTypes)
                {
                    node.RegisterServiceType(factory);
                }
            }

            if (scan_assembly_types)
            {
                try
                {
                    var scanned_types = ScanAssembliesForServiceTypes();
                    foreach (var t in scanned_types)
                    {
                        node.RegisterServiceType(t);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("warning: assembly scanning failed: " + e.Message);
                }
            }

            bool nodeNameSet = false;
            bool nodeIdSet = false;

            if (config.GetOptionOrDefaultAsBool("local-enable"))
            {
                local_transport = new LocalTransport(node);
                if (config.GetOptionOrDefaultAsBool("local-start-server"))
                {
                    //bool publicServer = config.GetOptionOrDefaultAsBool("local-server-public");

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        //local_transport.StartServerAsNodeName(nodeName, publicServer);
                        local_transport.StartServerAsNodeName(nodeName);
                        nodeNameSet = true;
                        nodeIdSet = true;
                    }
                    else if (!string.IsNullOrEmpty(nodeId))
                    {
                                //local_transport.StartServerAsNodeID(new NodeID(nodeId), publicServer);
                                local_transport.StartServerAsNodeID(new NodeID(nodeId));
                                nodeNameSet = true;
                        nodeIdSet = true;
                    }
                    else
                    {
                        LogError("Could not start Local transport server, neither NodeName or NodeID specified");
                    }
                }
                else if (config.GetOptionOrDefaultAsBool("local-start-client") && !string.IsNullOrEmpty(nodeName))
                {
                    local_transport.StartClientAsNodeName(nodeName);
                    nodeNameSet = true;
                    nodeIdSet = true;
                }

                if (config.GetOptionOrDefaultAsBool("discovery-listening-enable"))
                {
                    local_transport.EnableNodeDiscoveryListening();
                }

                // Note: The part about "Always announces due to file watching by clients" was commented out in the original code.
                // if (config.GetOptionOrDefaultAsBool("discovery-announce-enable"))
                //{
                //  Always announces due to file watching by clients
                //}

                /*if (config.GetOptionOrDefaultAsBool("disable-message4"))
                {
                    local_transport.SetDisableMessage4(true);
                }

                if (config.GetOptionOrDefaultAsBool("disable-stringtable"))
                {
                    local_transport.SetDisableStringTable(true);
                }*/

                /*if (config.GetOptionOrDefaultAsBool("jumbo-message"))
                {
                    local_transport.MaxMessageSize = (100 * 1024 * 1024);
                }*/

                node.RegisterTransport(local_transport);
            }

            if (!string.IsNullOrEmpty(nodeId))
            {
                if (!nodeIdSet)
                {
                    node.NodeID = (new NodeID(nodeId));
                }
                else
                {
                    if (node.NodeID.ToString() != nodeId)
                    {
                        LogError($"User requested NodeID {nodeId} but node was assigned {node.NodeID.ToString()}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(nodeName))
            {
                if (!nodeNameSet)
                {
                    node.NodeName =(nodeName);
                }
                else
                {
                    if (node.NodeName != nodeName)
                    {
                        LogError($"User requested NodeName {nodeName} but node was assigned {node.NodeName}");
                    }
                }
            }

            if (config.GetOptionOrDefaultAsBool("tcp-enable"))
            {
                tcp_transport = new TcpTransport(node);
                /*if (config.GetOptionOrDefaultAsBool("tcp-start-server-sharer"))
                {
                    tcp_transport.StartServerUsingPortSharer();
                }*/
                if (config.GetOptionOrDefaultAsBool("tcp-start-server"))
                {
                            //bool localhostOnly = config.GetOptionOrDefaultAsBool("tcp-listen-localhost");
                            //tcp_transport.StartServer(tcpPort, localhostOnly);
                            tcp_transport.StartServer(tcpPort);
                        }

                /*if (config.GetOptionOrDefaultAsBool("disable-message4"))
                {
                    tcp_transport.SetDisableMessage4(true);
                }

                if (config.GetOptionOrDefaultAsBool("disable-stringtable"))
                {
                    tcp_transport.SetDisableStringTable(true);
                }*/

                /*if (config.GetOptionOrDefaultAsBool("jumbo-message"))
                {
                    tcp_transport.SetMaxMessageSize(100 * 1024 * 1024);
                }*/

                if (config.GetOptionOrDefaultAsBool("discovery-listening-enable"))
                {
                            IPNodeDiscoveryFlags listenFlags = 0;

                    if (config.GetOptionOrDefaultAsBool("tcp-ipv4-discovery"))
                    {
                        listenFlags |= IPNodeDiscoveryFlags.IPv4Broadcast;
                    }
                    if (config.GetOptionOrDefaultAsBool("tcp-ipv6-discovery"))
                    {
                        listenFlags |= IPNodeDiscoveryFlags.LinkLocal;
                    }

                    if (listenFlags != 0)
                    {
                        tcp_transport.EnableNodeDiscoveryListening(listenFlags);
                    }
                }

                if (config.GetOptionOrDefaultAsBool("discovery-announce-enable"))
                {
                            IPNodeDiscoveryFlags announceFlags = 0;

                    if (config.GetOptionOrDefaultAsBool("tcp-ipv4-discovery"))
                    {
                        announceFlags |= IPNodeDiscoveryFlags.IPv4Broadcast;
                    }
                    if (config.GetOptionOrDefaultAsBool("tcp-ipv6-discovery"))
                    {
                        announceFlags |= IPNodeDiscoveryFlags.LinkLocal;
                    }

                    if (announceFlags != 0)
                    {
                        tcp_transport.EnableNodeAnnounce(announceFlags);
                    }
                }

                if (config.GetOptionOrDefaultAsBool("load-tls"))
                {
                    tcp_transport.LoadTlsNodeCertificate();
                }

                if (config.GetOptionOrDefaultAsBool("require-tls"))
                {
                    tcp_transport.RequireTls = (true);
                }

                string wsAddOrigin = config.GetOptionOrDefaultAsString("tcp-ws-add-origins");
                if (!string.IsNullOrEmpty(wsAddOrigin))
                {
                    var wsAddOriginSplit = wsAddOrigin.Split(',');

                    foreach (var wsOrigin in wsAddOriginSplit)
                    {
                        try
                        {
                            tcp_transport.AddWebSocketAllowedOrigin(wsOrigin.Trim());
                        }
                        catch (Exception exp)
                        {
                            LogError($"Adding tcp-ws-add-origin failed {wsOrigin}: {exp.Message}");
                        }
                    }
                }

                string wsRemoveOrigin = config.GetOptionOrDefaultAsString("tcp-ws-remove-origins");
                if (!string.IsNullOrEmpty(wsRemoveOrigin))
                {
                    var wsRemoveOriginSplit = wsRemoveOrigin.Split(',');

                    foreach (var wsOrigin in wsRemoveOriginSplit)
                    {
                        try
                        {
                            tcp_transport.RemoveWebSocketAllowedOrigin(wsOrigin.Trim());
                        }
                        catch (Exception exp)
                        {
                            LogError($"Removing tcp-ws-remove-origin failed {wsOrigin}: {exp.Message}");
                        }
                    }
                }

                node.RegisterTransport(tcp_transport);
            }

            /*if (config.GetOptionOrDefaultAsBool("hardware-enable"))
            {
                HardwareTransport hardwareTransport = new HardwareTransport(node);

                if (config.GetOptionOrDefaultAsBool("disable-message4"))
                {
                    hardwareTransport.SetDisableMessage4(true);
                }

                if (config.GetOptionOrDefaultAsBool("disable-stringtable"))
                {
                    hardwareTransport.SetDisableStringTable(true);
                }

                if (config.GetOptionOrDefaultAsBool("jumbo-message"))
                {
                    hardwareTransport.SetMaxMessageSize(100 * 1024 * 1024);
                }

                node.RegisterTransport(hardwareTransport);
            }*/

            if (config.GetOptionOrDefaultAsBool("intra-enable"))
            {
                intra_transport = new IntraTransport(node);
                if (config.GetOptionOrDefaultAsBool("intra-start-server"))
                {
                    intra_transport.StartServer();
                }
                else
                {
                    intra_transport.StartClient();
                }

                node.RegisterTransport(intra_transport);
            }

            if (config.GetOptionOrDefaultAsBool("disable-timeouts"))
            {
                node.RequestTimeout=(uint.MaxValue);
                node.TransportInactivityTimeout=(uint.MaxValue);
                node.EndpointInactivityTimeout=(uint.MaxValue);

                LogDebug("Timeouts disabled");
            }

            this.config = config;

            LogTrace("Node setup complete");
        }

        bool release_node = false;
        public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags)
        {
            var c = new CommandLineConfigParser(0);
            c.SetDefaults(nodename, tcp_port, flags);
            DoSetup(node, service_types, scan_assembly_types, c);
        }

        public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags, RobotRaconteurNodeSetupFlags allowed_overrides, string[] args)
        {
            var c = new CommandLineConfigParser(allowed_overrides);
            c.SetDefaults(nodename, tcp_port, flags);
            try
            {
                c.ParseCommandLine(args);
            }
            catch (Exception ex)
            {
                LogError(string.Format("Commandline parsing error {0}", ex), node, RobotRaconteur_LogComponent.NodeSetup);
                throw;
            }
            DoSetup(node, service_types, scan_assembly_types, c);
        }

        public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, CommandLineConfigParser config)
        {            
            DoSetup(node, service_types, scan_assembly_types, config);
        }

        public void ReleaseNode()
        {
            release_node = true;
        }

        public void Dispose()
        {
            if (release_node)
            {
                return;
            }
            Node?.Shutdown();
        }

        static List<ServiceFactory> ScanAssembliesForServiceTypes()
        {
            // https://stackoverflow.com/questions/13493416/scan-assembly-for-classes-that-implement-certain-interface-and-add-them-to-a-con

            var o = new List<ServiceFactory>();

            var assignableType = typeof(ServiceFactory);

            var scanners = AppDomain.CurrentDomain.GetAssemblies().ToList()
                .SelectMany(x => x.GetTypes())
                .Where(t => assignableType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract).ToList();

            foreach (Type type in scanners)
            {
                if (type == typeof(RobotRaconteurServiceIndex.RobotRaconteurServiceIndexFactory))
                {
                    continue;
                }
                var service_factory = Activator.CreateInstance(type) as ServiceFactory;
                if (service_factory != null)
                {
                    o.Add(service_factory);
                }
            }

            return o;
        }
    }



    public class ClientNodeSetup : RobotRaconteurNodeSetup
    {
        public ClientNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(node, service_types, false, nodename, 0, flags)
        {

        }

        public ClientNodeSetup(ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, 0, flags)
        {

        }

        public ClientNodeSetup(RobotRaconteurNode node, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(node, null, true, nodename, 0, flags)
        {

        }

        public ClientNodeSetup(string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, 0, flags)
        {

        }

        public ClientNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, 0, flags, allowed_overrides, args)
        {

        }

        public ClientNodeSetup(ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, 0, flags, allowed_overrides, args)
        {

        }

        public ClientNodeSetup(RobotRaconteurNode node, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, 0, flags, allowed_overrides, args)
        {

        }

        public ClientNodeSetup(string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, 0, flags, allowed_overrides, args)
        {

        }
    }

    public class ServerNodeSetup : RobotRaconteurNodeSetup
    {
        public ServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(node, service_types, false, nodename, tcp_port, flags)
        {

        }

        public ServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags)
        {

        }

        public ServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(node, null, true, nodename, tcp_port, flags)
        {

        }

        public ServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags)
        {

        }

        public ServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public ServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public ServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public ServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }
    }

    public class SecureServerNodeSetup : RobotRaconteurNodeSetup
    {
        public SecureServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(node, service_types, false, nodename, tcp_port, flags)
        {

        }

        public SecureServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags)
        {

        }

        public SecureServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(node, null, true, nodename, tcp_port, flags)
        {

        }

        public SecureServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags)
        {

        }

        public SecureServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public SecureServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public SecureServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        public SecureServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }
    }


}
