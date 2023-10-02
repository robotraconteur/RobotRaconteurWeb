using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
#if !ROBOTRACONTEUR_H5
using Mono.Options;
#endif
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    [Flags,PublicApi]
    public enum RobotRaconteurNodeSetupFlags
    {
        /**
        <summary>No options enabled</summary>
        */
        None = 0x0,
        /**
        <summary>Enable node discovery listening on all transports</summary>
        */
        EnableNodeDiscoveryListening = 0x1,
        /**
        <summary>Enable node announce on all transports</summary>
        */
        EnableNodeAnnounce = 0x2,
        /**
        <summary>Enable LocalTransport</summary>
        */        
        EnableLocalTransport = 0x4,
        /**
        <summary>Enable TcpTransport</summary>
        */
        EnableTcpTransport = 0x8,
        // EnableHardwareTransport = 0x10,
        /**
        <summary>Start the LocalTransport server to listen for incoming clients</summary>
        */
        LocalTransportStartServer = 0x20,
        /**
        <summary>Start the LocalTransport client with specified node name</summary>
        */
        LocalTransportStartClient = 0x40,
        /**
        <summary>Start the TcpTransport server to listen for incoming clients on the specified port</summary>
        */
        TcpTransportStartServer = 0x80,
        // TcpTransportStartServerPortSharer = 0x100,
        // DisableMessage4 = 0x200,
        // DisableStringTable = 0x400,
        // DisableTimeouts = 0x800,
        LoadTlsCert = 0x1000,
        /**
        <summary>Load the TLS certificate for TcpTransport</summary>
        */
        RequireTls = 0x2000,
        /**
        <summary>Require TLS for all clients on TcpTransport</summary>
        */
        LocalTransportServerPublic = 0x4000,
        /**
        <summary>Make LocalTransport server listen for incoming clients from all users</summary>
        */
        TcpTransportListenLocalHost = 0x8000,
        NodeNameOverride = 0x10000,
        NodeIdOverride = 0x20000,
        TcpPortOverride = 0x40000,
        TcpWebSocketOriginOverride = 0x80000,
        /**
        <summary>Enable IntraTransport</summary>
        */
        EnableIntraTransport = 0x100000,
        /**
        <summary>Start the IntraTransport server to listen for incoming clients</summary>
        */
        IntraTransportStartServer = 0x200000,
        TcpTransportIpv4Discovery = 0x400000,
        TcpTransportIpv6Discovery = 0x800000,
        /**
        <summary>Enable the LocalTap debug logging system</summary>
        */
        LocalTapEnable = 0x1000000,
        /**
        <summary>Allow the user to set the LocalTap name</summary>
        */
        LocalTapName = 0x2000000,
        JumboMessage = 0x4000000,
        /**
        <summary>Convenience flag to enable all transports</summary>
        */
        EnableAllTransports = EnableLocalTransport 
            | EnableTcpTransport 
            //| EnableHardwareTransport 
            | EnableIntraTransport,
        /**
        <summary>Default configuration for client nodes (See ClientNodeSetup)</summary>
        */
        ClientDefault = EnableTcpTransport
        | EnableLocalTransport
        | EnableIntraTransport
        | EnableNodeDiscoveryListening
        | TcpTransportIpv6Discovery
        | LocalTransportStartClient,

        /**
        <summary>Default allowed overrides for client nodes (See ClientNodeSetup)</summary>
        */
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

        /**
        <summary>Default configuration for server nodes</summary>
        */
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

        /**
        <summary>Default allowed overrides for server nodes</summary>
        */
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

        /**
        <summary>Default configuration for server nodes requiring TLS network transports</summary>
        */
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

        /**
        <summary>Default allowed overrides for server nodes requiring TLS network transports</summary>
        */
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

#if !ROBOTRACONTEUR_H5
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
#endif
    /**
    <summary>
            Command line parser for node setup classes
            </summary>
            <remarks>
            <para>
                The CommandLineConfigParser is used to parse command line options specified
                when a program is launched. These options allow for the node configuration to be
                changed without recompiling the software. See command_line_options for
                a table of the standard command line options.
            </para>
            <para>
                ClientNodeSetup, ServerNodeSetup, and SecureServerNodeSetup use this class to parse
                the `sys.argv` parameters. The RobotRaconteurNodeSetup constructors will accept
                either `sys.argv`, or will accept an initialize CommandLineConfigParser.
            </para>
            <para>
                The CommandLineConfig() constructor takes the "allowed override" flags, and the option
                prefix.
                The "allowed override" specifies which options can be overridden using the command line.
                The
                prefix option allows the command line flag prefix to be changed. By default it expects
                all options to begin with `--robotraconteur-` followed by the name of the option. If there
                are
                multiple nodes, it is necessary to change the prefix to be unique for each node. For
                instance,
                "robotraconteur1-" for the first node and "RobotRaconteur-" for the second node.
            </para>
            <para> Users may add additional options to the parser. Use AddStringOption(),
                AddBoolOption(), or AddIntOption() to add additional options.
            </para>
            </remarks>
    */
    [PublicApi]
    public class CommandLineConfigParser
    {
#if !ROBOTRACONTEUR_H5
        private OptionSet desc = new OptionSet();
#endif
        private Dictionary<string, string> parsedOptions = new Dictionary<string, string>();
        private string prefix;

        private string default_node_name;
        private ushort default_tcp_port;
        private RobotRaconteurNodeSetupFlags default_flags;

        /**
        <summary>
        Construct a new CommandLineConfigParser
        </summary>
        <remarks>None</remarks>
        <param name="allowed_overrides">The allowed overrides flags</param>
        <param name="prefix">The prefix to use for the options</param>
        */
        [PublicApi]
        public CommandLineConfigParser(RobotRaconteurNodeSetupFlags allowed_overrides, string prefix = "robotraconteur-")
        {
            default_tcp_port = 48653;
            default_flags = 0;
            this.prefix = prefix;
#if !ROBOTRACONTEUR_H5
            FillOptionsDescription(desc, allowed_overrides, prefix);
#endif
        }

        /**
        <summary>
                Set the default NodeName, TCP port, and flags
              </summary>
              <remarks>
                The command line options will be allowed to override the options
                specified in allowed_overrides passed to CommandLineConfigParser().
              </remarks>
              <param name="node_name">The default NodeName</param>
              <param name="tcp_port">The default TCP port</param>
              <param name="default_flags">The default flags</param>
        */
        [PublicApi]
        public void SetDefaults(string node_name, ushort tcp_port, RobotRaconteurNodeSetupFlags default_flags)
        {
            this.default_node_name = node_name;
            this.default_tcp_port = tcp_port;
            this.default_flags = default_flags;
        }

        /**
        <summary>
        Add a new string option
        </summary>
        <remarks>None</remarks>
        <param name="name">The name of the option</param>
        <param name="descr">Description of the option</param>
        */
        [PublicApi]
        public void AddStringOption(string name, string descr)
        {
#if !ROBOTRACONTEUR_H5
            desc.Add(prefix + name + "=", descr, v => parsedOptions[name] = v);
#endif
        }
        /**
        <summary>
        Add a new bool option
        </summary>
        <remarks>None</remarks>
        <param name="name">The name of the option</param>
        <param name="descr">Description of the option</param>
        */
        [PublicApi]
        public void AddBoolOption(string name, string descr)
        {
#if !ROBOTRACONTEUR_H5
            desc.Add(prefix + name, descr, v => parsedOptions[name] = v != null ? "true" : "false");
#endif
        }

        /**
        <summary>
        Add a new int option
        </summary>
        <remarks>None</remarks>
        <param name="name">The name of the option</param>
        <param name="descr">Description of the option</param>
        */
        [PublicApi]
        public void AddIntOption(string name, string descr)
        {
#if !ROBOTRACONTEUR_H5
            desc.Add(prefix + name + "=", descr, (int v) => parsedOptions[name] = v.ToString());
#endif
        }

        /**
        <summary>
        Parse a specified string vector containing the options
        </summary>
        <remarks>
        Results are stored in the instance
        </remarks>
        <param name="args">The options as a string array</param>
        */
        [PublicApi]
        public void ParseCommandLine(string[] args)
        {
#if !ROBOTRACONTEUR_H5
            if (args != null)
            {
                desc.Parse(args);
            }
#endif
        
        }
        /**
        <summary>
        Parse a specified string vector containing the options
        </summary>
        <remarks>
        Results are stored in the instance
        </remarks>
        <param name="args">The options as a string list</param>
        */
        [PublicApi]
        public void ParseCommandLine(List<string> args)
        {
#if !ROBOTRACONTEUR_H5
            if (args != null)
            {
                desc.Parse(args);
            }
#endif
        }

        /**
        <summary>
        Get the option value as a string
        </summary>
        <remarks>
        Returns empty string if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <returns>The option value, or an empty string</returns>
        */
        [PublicApi]
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

        /**
        <summary>
        Get the option value as a string
        </summary>
        <remarks>
        Returns default_value if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <param name="default_value">The default option value</param>
        <returns>The option value, or default_value if not specified on command line</returns>
        */
        [PublicApi]
        public string GetOptionOrDefaultAsString(string option, string defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return parsedOptions[option1].ToString();
            }

            return defaultValue;
        }
        /**
        <summary>
        Get the option value as a bool
        </summary>
        <remarks>
        Returns false if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <returns>The option value, or false</returns>
        */
        [PublicApi]
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
        /**
        <summary>
        Get the option value as a bool
        </summary>
        <remarks>
        Returns default_value if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <param name="default_value">The default option value</param>
        <returns>The option value, or default_value if not specified on command line</returns>
        */
        [PublicApi]
        public bool GetOptionOrDefaultAsBool(string option, bool defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return bool.Parse(parsedOptions[option1]);
            }

            return defaultValue;
        }
        /**
        <summary>
        Get the option value as an int
        </summary>
        <remarks>
        Returns -1 if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <returns>The option value, or -1</returns>
        */
        [PublicApi]
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

        /**
        <summary>
        Get the option value as an int
        </summary>
        <remarks>
        Returns default_value if option not specified on command line
        </remarks>
        <param name="option">The name of the option</param>
        <param name="default_value">The default option value</param>
        <returns>The option value, or default_value if not specified on command line</returns>
        */
        [PublicApi]
        public int GetOptionOrDefaultAsInt(string option, int defaultValue)
        {
            string option1 = prefix + option;
            if (parsedOptions.ContainsKey(option1))
            {
                return int.Parse(parsedOptions[option1].ToString());
            }

            return defaultValue;
        }
#if !ROBOTRACONTEUR_H5
        internal void FillOptionsDescription(OptionSet optionSet, RobotRaconteurNodeSetupFlags allowedOverrides, string prefix)
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
#endif
}

    /**
    <summary>
    Setup a node using specified options and manage node lifecycle
    </summary>
    <remarks>
    <para>
    RobotRaconteurNodeSetup and its subclasses ClientNodeSetup, ServerNodeSetup,
    and SecureServerNodeSetup are designed to help configure nodes and manage
    node lifecycles. The node setup classes use Dispose() to configure the node
    on construction, and call RobotRaconteurNode.Shutdown() when the instance
    is destroyed.
    </para>
    <para>
    The node setup classes execute the following operations to configure the node:
    </para>
    <para>
    1. Set log level and tap options from flags, command line options, or environmental variables
    </para>
    <para>
    2. Register specified service factory types
    </para>
    <para>
    3. Initialize transports using flags specified in flags or from command line options
    </para>
    <para>
    4. Configure timeouts
    </para>
    <para>
    See command_line_options for more information on available command line options.
    </para>
    <para>
    Logging level is configured using the environmental variable `ROBOTRACONTEUR_LOG_LEVEL`
    or the command line option `--robotraconteur-log-level`. See logging for more information.
    </para>
    <para>
    See taps for more information on using taps.
    </para>
    <para>
    The node setup classes optionally initialize LocalTransport,
    TcpTransport, HardwareTransport, and/or IntraTransport.
    transports for more information.
    </para>
    <para>
    The LocalTransport.StartServerAsNodeName() or
    LocalTransport.StartClientAsNodeName() are used to load the NodeID.
    See LocalTransport for more information on this procedure.
    </para>
    </remarks>
    */
    [PublicApi]
    public class RobotRaconteurNodeSetup : IDisposable
    {
        public RobotRaconteurNode Node => node;
        /**
        <summary>
        Get the IntraTransport
        </summary>
        <remarks>
        Will be null if IntraTransport is not specified in flags
        </remarks>
        */
        [PublicApi]
        public IntraTransport IntraTransport => intra_transport;
#if !ROBOTRACONTEUR_H5
        /**
        <summary>
        Get the TcpTransport
        </summary>
        <remarks>
        Will be null if TcpTransport is not specified in flags
        </remarks>
        */
        [PublicApi]
        public TcpTransport TcpTransport => tcp_transport;
        /**
        <summary>
        Get the LocalTransport
        </summary>
        <remarks>
        Will be null if LocalTransport is not specified in flags
        </remarks>
        */
        [PublicApi]
        public LocalTransport LocalTransport => local_transport;
#endif
        internal RobotRaconteurNode node = null;

        internal IntraTransport intra_transport = null;
#if !ROBOTRACONTEUR_H5
        internal TcpTransport tcp_transport = null;

        public LocalTransport local_transport = null;

        private CommandLineConfigParser config;

        /**
        <summary>
        Get the command line config parser object used to configure node
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public CommandLineConfigParser Config => config;
#endif

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
#if !ROBOTRACONTEUR_H5
                    Console.Error.WriteLine("warning: assembly scanning failed: " + e.Message);
#else
                    Console.WriteLine("warning: assembly scanning failed: " + e.Message);
#endif
                }
            }

            bool nodeNameSet = false;
            bool nodeIdSet = false;
#if !ROBOTRACONTEUR_H5
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
#endif
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
#if !ROBOTRACONTEUR_H5
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
#endif
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

            /*if (config.GetOptionOrDefaultAsBool("disable-timeouts"))
            {
                node.RequestTimeout=(uint.MaxValue);
                node.TransportInactivityTimeout=(uint.MaxValue);
                node.EndpointInactivityTimeout=(uint.MaxValue);

                LogDebug("Timeouts disabled");
            }*/
#if !ROBOTRACONTEUR_H5
            this.config = config;
#endif

            LogTrace("Node setup complete");
        }

        bool release_node = false;
        /**
        <summary>
        Construct a new RobotRaconteurNodeSetup with default node, NodeName, TCP port, and flags
        </summary>
        <remarks>
        <para>
        Construct node setup and configure the specified node. Use this overload if no command line options
        are provided.
        </para>
        </remarks>
        <param name="node">The node to setup</param>
        <param name="service_types">The service types to register</param>
        <param name="scan_assembly_types">If true, scan assemblies for service types</param>
        <param name="node_name">The NodeName</param>
        <param name="tcp_port">The port to listen for incoming TCP clients</param>
        <param name="flags">The configuration flags</param>
        */    
        [PublicApi]    
        public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags)
        {
            var c = new CommandLineConfigParser(0);
            c.SetDefaults(nodename, tcp_port, flags);
            DoSetup(node, service_types, scan_assembly_types, c);
        }
        /**
        <summary>
        Construct a new RobotRaconteurNodeSetup with default node, NodeName, TCP port, and flags
        </summary>
        <remarks>
        <para>
        Construct node setup and configure the specified node. Use this overload if no command line options
        are provided.
        </para>
        </remarks>
        <param name="node">The node to setup</param>
        <param name="service_types">The service types to register</param>
        <param name="scan_assembly_types">If true, scan assemblies for service types</param>
        <param name="node_name">The NodeName</param>
        <param name="tcp_port">The port to listen for incoming TCP clients</param>
        <param name="flags">The configuration flags</param>
        <param name="allowed_overrides">The allowed command line overrides</param>
        <param name="args">The command line arguments</param>        
        */ 
        [PublicApi]
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
        /**
        <summary>
            Setup a RobotRaconteurNode using a pre-configured CommandLineConfigParser.
        </summary>
        <remarks>None</remarks>
        <param name="node">The node to setup</param>
        <param name="service_types">The service types to register</param>
        <param name="scan_assembly_types">If true, scan assemblies for service types</param>
        <param name="config">The CommandLineConfigParser to use</param>
        */
        [PublicApi]
        public RobotRaconteurNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, bool scan_assembly_types, CommandLineConfigParser config)
        {            
            DoSetup(node, service_types, scan_assembly_types, config);
        }
        /**
        <summary>
        Release the node from lifecycle management
        </summary>
        <remarks>
        If called, RobotRaconteurNode.Shutdown() will not be called when the node setup instance is destroyed
        </remarks>
        */
        [PublicApi]
        public void ReleaseNode()
        {
            release_node = true;
        }
        /**
        <summary>
        Dispose the node setup instance and shutdown the node
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
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


    /**
    <summary>
    Initializes a RobotRaconteurNode instance to default configuration for a client only node
    </summary>
    <remarks>
    <para>
    ClientNodeSetup is a subclass of RobotRaconteurNodeSetup providing default configuration for a
    RobotRaconteurNode instance that is used only to create outgoing client connections.
    </para>
    <para>
    See command_line_options for more information on available command line options.
    </para>
    <para>
    Note: String table and HardwareTransport are disabled by default. They can be enabled
    using command line options.
    </para>
    <para>
    By default, the configuration will do the following:
    </para>
    <para>
    1. Configure logging level from environmental variable or command line options. Defaults to `INFO` if
    not specified
    </para>
    <para>
    2. Configure tap if specified in command line options
    </para>
    <para>
    3. Register service types passed to service_types
    </para>
    <para>
    4. Start LocalTransport (default enabled)
    1. If `RobotRaconteurNodeSetupFlags_LOCAL_TRANSPORT_START_CLIENT` flag is specified, call
    LocalTransport::StartServerAsNodeName() with the specified node_name
    2. Start LocalTransport discovery listening if specified in flags or on command line (default enabled)
    3. Disable Message Format Version 4 (default enabled) and/or String Table (default disabled) if
    specified on command line
    </para>
    <para>
    5. Start TcpTransport (default enabled)
    1. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    2. Start TcpTransport discovery listening (default enabled)
    3. Load TLS certificate and set if TLS is specified on command line (default disabled)
    4. Process WebSocket origin command line options
    </para>
    <para>
    6. Start HardwareTransport (default disabled)
    1. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    7. Start IntraTransport (default disabled)
    1. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    8. Disable timeouts if specified in flags or command line (default timeouts normal)
    </para>
    <para>
    Most users will not need to be concerned with these details, and can simply
    use the default configuration
    </para>
    </remarks>
    */
    [PublicApi]
    public class ClientNodeSetup : RobotRaconteurNodeSetup
    {
        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the provided node, service types, node name, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        [PublicApi]
        public ClientNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(node, service_types, false, nodename, 0, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the default node, provided service types, node name, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        [PublicApi]
        public ClientNodeSetup(ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, 0, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the provided node, node name, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        [PublicApi]
        public ClientNodeSetup(RobotRaconteurNode node, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(node, null, true, nodename, 0, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the default node, node name, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        [PublicApi]
        public ClientNodeSetup(string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, 0, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the provided node, service types, node name, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ClientNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, 0, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the default node, provided service types, node name, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ClientNodeSetup(ServiceFactory[] service_types, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, 0, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the provided node, node name, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ClientNodeSetup(RobotRaconteurNode node, string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, 0, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ClientNodeSetup class with the default node, node name, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">Optional node name.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ClientDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ClientNodeSetup(string nodename = null, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ClientDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ClientDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, 0, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initialize client node with default options
        /// </summary>
        public ClientNodeSetup()
            :base(RobotRaconteurNode.s, null, true, null, 0, RobotRaconteurNodeSetupFlags.ClientDefault)
        {

        }
    }
    /**
    <summary>
    Initializes a RobotRaconteurNode instance to default configuration for a server and client node
    </summary>
    <remarks>
    <para>
    ServerNodeSetup is a subclass of RobotRaconteurNodeSetup providing default configuration for a
    RobotRaconteurNode instance that is used as a server to accept incoming client connections
    and to initiate client connections.
    </para>
    <para>
    ServerNodeSetup requires a NodeName, and a TCP port if LocalTransport and TcpTransport
    are enabled (default behavior).
    </para>
    <para>
    See command_line_options for more information on available command line options.
    </para>
    <para>
    Note: String table and HardwareTransport are disabled by default. They can be enabled
    using command line options.
    </para>
    <para>
    By default, the configuration will do the following:
    </para>
    <para>
    1. Configure logging level from environmental variable or command line options. Defaults to `INFO` if
    not specified
    </para>
    <para>
    2. Configure tap if specified in command line options
    </para>
    <para>
    3. Register service types passed to service_types
    </para>
    <para>
    4. Start LocalTransport (default enabled)
    1. Configure the node to use the specified NodeName, and load the NodeID from the filesystem based
    based on the NodeName. NodeID will be automatically generated if not previously used.
    1. If "public" option is set, the transport will listen for all local users (default disabled)
    2. Start the LocalTransport server to listen for incoming connections with the specified NodeName and NodeID
    3. Start LocalTransport discovery announce and listening (default enabled)
    4. Disable Message Format Version 4 (default enabled) and/or String Table (default disabled) if
    specified on command line
    </para>
    <para>
    5. Start TcpTransport (default enabled)
    1. Start the TcpTransport server to listen for incoming connections on specified port
    or using the port sharer (default enabled using specified port)
    2. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    3. Start TcpTranport discovery announce and listening (default enabled)
    4. Load TLS certificate and set if TLS is specified on command line (default disabled)
    5. Process WebSocket origin command line options
    </para>
    <para>
    6. Start HardwareTransport (default disabled)
    1. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    7. Start IntraTransport (default enabled)
    1. Enable IntraTransport server to listen for incoming clients (default enabled)
    2. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    8. Disable timeouts if specified in flags or command line (default timeouts normal)
    </para>
    <para>
    Most users will not need to be concerned with these details, and can simply
    use the default configuration.
    </para>
    </remarks>
    */
    [PublicApi]
    public class ServerNodeSetup : RobotRaconteurNodeSetup
    {
        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the provided node, service types, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks> 
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        [PublicApi]
        public ServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(node, service_types, false, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the default node, provided service types, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        [PublicApi]
        public ServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the provided node, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        [PublicApi]
        public ServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(node, null, true, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the default node, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        [PublicApi]
        public ServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the provided node, service types, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the default node, provided service types, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the provided node, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ServerNodeSetup class with the default node, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.ServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public ServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.ServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }
    }

    /**
    <summary>
    Initializes a RobotRaconteurNode instance to default configuration for a secure server and client node
    </summary>
    <remarks>
    <para>
    SecureServerNodeSetup is a subclass of RobotRaconteurNodeSetup providing default configuration for a
    secure RobotRaconteurNode instance that is used as a server to accept incoming client connections
    and to initiate client connections. SecureServerNodeSetup is identical to ServerNodeSetup,
    except that it requires TLS for all network communication.
    </para>
    <para>
    ServerNodeSetup requires a NodeName, and a TCP port if LocalTransport and TcpTransport
    are enabled (default behavior).
    </para>
    <para>
    See command_line_options for more information on available command line options.
    </para>
    <para>
    Note: String table and HardwareTransport are disabled by default. They can be enabled
    using command line options.
    </para>
    <para>
    By default, the configuration will do the following:
    </para>
    <para>
    1. Configure logging level from environmental variable or command line options. Defaults to `INFO` if
    not specified
    </para>
    <para>
    2. Configure tap if specified in command line options
    </para>
    <para>
    3. Register service types passed to service_types
    </para>
    <para>
    4. Start LocalTransport (default enabled)
    1. Configure the node to use the specified NodeName, and load the NodeID from the filesystem based
    based on the NodeName. NodeID will be automatically generated if not previously used.
    1. If "public" option is set, the transport will listen for all local users (default disabled)
    2. Start the LocalTransport server to listen for incoming connections with the specified NodeName and NodeID
    3. Start LocalTransport discovery announce and listening (default enabled)
    3. Disable Message Format Version 4 (default enabled) and/or String Table (default disabled) if
    specified on command line
    </para>
    <para>
    5. Start TcpTransport (default enabled)
    1. Start the TcpTransport server to listen for incoming connections on specified port
    or using the port sharer (default enabled using specified port)
    2. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    3. Start TcpTranport discovery announce and listening (default enabled)
    4. Load TLS certificate and set if TLS is specified on command line (default enabled, required)
    5. Process WebSocket origin command line options
    </para>
    <para>
    6. Start HardwareTransport (default disabled)
    1. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    7. Start IntraTransport (default disabled)
    1. Enable IntraTransport server to listen for incoming clients (default enabled)
    2. Disable Message Format Version 4 (default enabled) and/or String Table
    (default disabled) if specified in flags or command line
    </para>
    <para>
    8. Disable timeouts if specified in flags or command line (default timeouts normal)
    </para>
    <para>
    Most users will not need to be concerned with these details, and can simply
    use the default configuration.
    </para>
    </remarks>
    */
    [PublicApi]
    public class SecureServerNodeSetup : RobotRaconteurNodeSetup
    {
         /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the provided node, service types, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        [PublicApi]
        public SecureServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(node, service_types, false, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the default node, provided service types, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        [PublicApi]
        public SecureServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the provided node, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        [PublicApi]
        public SecureServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(node, null, true, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the default node, node name, TCP port, and flags.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        [PublicApi]
        public SecureServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the provided node, service types, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public SecureServerNodeSetup(RobotRaconteurNode node, ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(node, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the default node, provided service types, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">Array of ServiceFactory types to register.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public SecureServerNodeSetup(ServiceFactory[] service_types, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, service_types, false, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the provided node, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="node">The RobotRaconteurNode instance.</param>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public SecureServerNodeSetup(RobotRaconteurNode node, string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(node, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }

        /// <summary>
        /// Initializes a new instance of the SecureServerNodeSetup class with the default node, node name, TCP port, flags, allowed overrides, and args.
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="nodename">The node name.</param>
        /// <param name="tcp_port">The TCP port number.</param>
        /// <param name="flags">Optional setup flags. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefault.</param>
        /// <param name="allowed_overrides">Optional flags for allowed overrides. Defaults to RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride.</param>
        /// <param name="args">Optional string arguments.</param>
        [PublicApi]
        public SecureServerNodeSetup(string nodename, ushort tcp_port, RobotRaconteurNodeSetupFlags flags = RobotRaconteurNodeSetupFlags.SecureServerDefault, RobotRaconteurNodeSetupFlags allowed_overrides = RobotRaconteurNodeSetupFlags.SecureServerDefaultAllowedOverride, string[] args = null)
            : base(RobotRaconteurNode.s, null, true, nodename, tcp_port, flags, allowed_overrides, args)
        {

        }
    }


}
