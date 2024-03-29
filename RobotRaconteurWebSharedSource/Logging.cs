using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace RobotRaconteurWeb
{
    /// <summary>
    /// Log level enum.
    /// </summary>
    /// <remarks>
    /// Enum of possible log levels. Set log level using
    /// RobotRaconteurNode::SetLogLevel(),
    /// `ROBOTRACONTEUR_LOG_LEVEL` environmental variable, or
    /// `--robotraconteur-log-level` node setup command line option
    /// </remarks>
    [PublicApi]
    public enum RobotRaconteur_LogLevel
    {
        /// <summary>
        /// Trace log level.
        /// </summary>
        Trace,

        /// <summary>
        /// Debug log level.
        /// </summary>
        Debug,

        /// <summary>
        /// Info log level.
        /// </summary>
        Info,

        /// <summary>
        /// Warning log level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error log level.
        /// </summary>
        Error,

        /// <summary>
        /// Fatal log level.
        /// </summary>
        Fatal,

        /// <summary>
        /// Disabled log level.
        /// </summary>
        Disable = 1000
    }

    /// <summary>
    /// Log component enum.
    /// </summary>
    /// <remarks>
    /// Log records contain the code of the component where
    /// the log record was generated.
    /// </remarks>
    public enum RobotRaconteur_LogComponent
    {
        /// <summary>
        /// Default component.
        /// </summary>
        Default,

        /// <summary>
        /// Robot Raconteur Node component.
        /// </summary>
        Node,

        /// <summary>
        /// Transport component.
        /// </summary>
        Transport,

        /// <summary>
        /// Message or message serialization component.
        /// </summary>
        Message,

        /// <summary>
        /// Client component.
        /// </summary>
        Client,

        /// <summary>
        /// Service component.
        /// </summary>
        Service,

        /// <summary>
        /// Member component.
        /// </summary>
        Member,

        /// <summary>
        /// Data message packing component.
        /// </summary>
        Pack,

        /// <summary>
        /// Data message unpacking component.
        /// </summary>
        Unpack,

        /// <summary>
        /// Service definition parser component.
        /// </summary>
        ServiceDefinition,

        /// <summary>
        /// Node or service discovery component.
        /// </summary>
        Discovery,

        /// <summary>
        /// Subscription component.
        /// </summary>
        Subscription,

        /// <summary>
        /// Node setup component.
        /// </summary>
        NodeSetup,

        /// <summary>
        /// Utility component.
        /// </summary>
        Utility,

        /// <summary>
        /// Service definition standard library component (external).
        /// </summary>
        RobDefLib,

        /// <summary>
        /// User component (external).
        /// </summary>
        User,

        /// <summary>
        /// User client component (external).
        /// </summary>
        UserClient,

        /// <summary>
        /// User service component (external).
        /// </summary>
        UserService,

        /// <summary>
        /// Third party library component (external).
        /// </summary>
        ThirdParty
    }

    /**
    <summary>
    Robot Raconteur log record
    </summary>
    <remarks>
    <para>
    Records information about a logging event
    </para>
    <para> See logging for more information.
    </para>
    </remarks>
    */
        [PublicApi]
    public class RRLogRecord
    {
        /**
        <summary>
        The source node
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public RobotRaconteurNode Node;
        /**
        <summary>
        The log level
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public RobotRaconteur_LogLevel Level = RobotRaconteur_LogLevel.Warning;
        /**
        <summary>
        The source component
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public RobotRaconteur_LogComponent Component = RobotRaconteur_LogComponent.Default;
        /**
        <summary>
        The source component name
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string ComponentName;
        /**
        <summary>
        The source component object ID
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string ComponentObjectID;
        /**
        <summary>
        The source endpoint
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public long Endpoint;
        /**
        <summary>
        The service path of the source object
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string ServicePath;
        /**
        <summary>
        The source member
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string Member;
        /**
        <summary>
        Human readable log message
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string Message;
        /**
        <summary>
        Time of logging event
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public DateTime Time;
        /**
        <summary>
        The sourcecode filename
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string SourceFile;
        /**
        <summary>
        The line within the sourcecode file
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public int SourceLine;
        /**
        <summary>
        The source thread
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string ThreadID;
    }

    public static class RRLogFuncs
    {
        public static string LogLevel_ToString(RobotRaconteur_LogLevel logLevel)
        {
            switch (logLevel)
            {
                case RobotRaconteur_LogLevel.Trace:
                    return "trace";
                case RobotRaconteur_LogLevel.Debug:
                    return "debug";
                case RobotRaconteur_LogLevel.Info:
                    return "info";
                case RobotRaconteur_LogLevel.Warning:
                    return "warning";
                case RobotRaconteur_LogLevel.Error:
                    return "error";
                case RobotRaconteur_LogLevel.Fatal:
                    return "fatal";
                default:
                    return "unknown";
            }
        }

        public static string LogComponent_ToString(RobotRaconteur_LogComponent component)
        {
            switch (component)
            {
                case RobotRaconteur_LogComponent.Default:
                    return "default";
                case RobotRaconteur_LogComponent.Node:
                    return "node";
                case RobotRaconteur_LogComponent.Transport:
                    return "transport";
                case RobotRaconteur_LogComponent.Message:
                    return "message";
                case RobotRaconteur_LogComponent.Client:
                    return "client";
                case RobotRaconteur_LogComponent.Service:
                    return "service";
                case RobotRaconteur_LogComponent.Member:
                    return "member";
                case RobotRaconteur_LogComponent.Pack:
                    return "pack";
                case RobotRaconteur_LogComponent.Unpack:
                    return "unpack";
                case RobotRaconteur_LogComponent.ServiceDefinition:
                    return "service_definition";
                case RobotRaconteur_LogComponent.Discovery:
                    return "discovery";
                case RobotRaconteur_LogComponent.Subscription:
                    return "subscription";
                case RobotRaconteur_LogComponent.NodeSetup:
                    return "node_setup";
                case RobotRaconteur_LogComponent.Utility:
                    return "utility";
                case RobotRaconteur_LogComponent.RobDefLib:
                    return "robdeflib";
                case RobotRaconteur_LogComponent.User:
                    return "user";
                case RobotRaconteur_LogComponent.UserClient:
                    return "user_client";
                case RobotRaconteur_LogComponent.UserService:
                    return "user_service";
                case RobotRaconteur_LogComponent.ThirdParty:
                    return "third_party";
                default:
                    return "unknown";
            }
        }

        public static string Node_ToString(RobotRaconteurNode node)
        {
            if (node == null)
            {
                return "unknown";
            }

            if(!node.TryGetNodeID(out var id))
            {
                return "unknown";
            }

            if (!node.TryGetNodeName(out var name) || string.IsNullOrEmpty(name))
            {
                return id.ToString("B");
            }

            return id.ToString("B") + "," + name;
        }

        private static string to_iso_extended_string(DateTime time)
        {
            return time.ToString("o");
        }
        public static void WriteLogRecord(TextWriter writer, RRLogRecord record)
        {
            writer.Write("[{0}] [{1}] [{2}] [{3}]", 
                to_iso_extended_string(record.Time), LogLevel_ToString(record.Level), record.ThreadID,
                Node_ToString(record.Node));
            if(!string.IsNullOrEmpty(record.ComponentName) || !string.IsNullOrEmpty(record.ComponentObjectID))
            {
                writer.Write(" [{0},{1},{2}]", LogComponent_ToString(record.Component), record.ComponentName ?? "", record.ComponentObjectID ?? "");
            }
            else
            {
                writer.Write(" [{0}]", LogComponent_ToString(record.Component));
            }

            if (!string.IsNullOrEmpty(record.ServicePath) && !string.IsNullOrEmpty(record.Member))
            {
                writer.Write(" [{0},{1},{2}]", record.Endpoint, record.ServicePath, record.Member);
            }
            else if (!string.IsNullOrEmpty(record.ServicePath))
            {
                writer.Write(" [{0},{1}]", record.Endpoint, record.ServicePath);
            }
            else
            {
                writer.Write(" [{0}]", record.Endpoint);
            }

            if (!string.IsNullOrEmpty(record.SourceFile))
            {
                writer.Write(" [{0}:{1}]", record.SourceFile, record.SourceLine);
            }

            writer.Write(" {0}", record.Message);
        }

        public static string LogRecordToString(RRLogRecord record)
        {
            var s = new StringWriter();
            WriteLogRecord(s, record);
            return s.ToString();
        }

        public static void Log(RRLogRecord record)
        {
            record?.Node?.LogRecord(record);
        }

        public static void Log(string message, RobotRaconteurNode node=null, RobotRaconteur_LogLevel level = RobotRaconteur_LogLevel.Warning, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "", 
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            if (time == DateTime.MinValue) time = DateTime.UtcNow;
            var r = new RRLogRecord()
            {
                Node = node,
                Level = level,
                Component = component,
                ComponentName = component_name,
                ComponentObjectID = component_object_id,
                Endpoint = endpoint,
                ServicePath = service_path,
                Member = member,
                Message = message,
                Time = time,
                SourceFile = source_file,
                SourceLine = source_line,
                ThreadID = thread_id
            };

            Log(r);
        }

        public static void LogFatal(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "", 
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Fatal, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        public static void LogError(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "", 
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Error, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        public static void LogWarning(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "", 
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Warning, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        public static void LogInfo(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Info, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        public static void LogDebug(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Debug, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        public static void LogTrace(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Trace, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }
    }

    public interface ILogRecordHandler
    {
        void Log(RRLogRecord record);
    }
}