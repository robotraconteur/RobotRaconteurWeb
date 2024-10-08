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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        [PublicApi]
        Trace,

        /// <summary>
        /// Debug log level.
        /// </summary>
        [PublicApi]
        Debug,

        /// <summary>
        /// Info log level.
        /// </summary>
        [PublicApi]
        Info,

        /// <summary>
        /// Warning log level.
        /// </summary>
        [PublicApi]
        Warning,

        /// <summary>
        /// Error log level.
        /// </summary>
        [PublicApi]
        Error,

        /// <summary>
        /// Fatal log level.
        /// </summary>
        [PublicApi]
        Fatal,

        /// <summary>
        /// Disabled log level.
        /// </summary>
        [PublicApi]
        Disable = 1000
    }

    /// <summary>
    /// Log component enum.
    /// </summary>
    /// <remarks>
    /// Log records contain the code of the component where
    /// the log record was generated.
    /// </remarks>
    [PublicApi]
    public enum RobotRaconteur_LogComponent
    {
        /// <summary>
        /// Default component.
        /// </summary>
        [PublicApi]
        Default,

        /// <summary>
        /// Robot Raconteur Node component.
        /// </summary>
        [PublicApi]
        Node,

        /// <summary>
        /// Transport component.
        /// </summary>
        [PublicApi]
        Transport,

        /// <summary>
        /// Message or message serialization component.
        /// </summary>
        [PublicApi]
        Message,

        /// <summary>
        /// Client component.
        /// </summary>
        [PublicApi]
        Client,

        /// <summary>
        /// Service component.
        /// </summary>
        [PublicApi]
        Service,

        /// <summary>
        /// Member component.
        /// </summary>
        [PublicApi]
        Member,

        /// <summary>
        /// Data message packing component.
        /// </summary>
        [PublicApi]
        Pack,

        /// <summary>
        /// Data message unpacking component.
        /// </summary>
        [PublicApi]
        Unpack,

        /// <summary>
        /// Service definition parser component.
        /// </summary>
        [PublicApi]
        ServiceDefinition,

        /// <summary>
        /// Node or service discovery component.
        /// </summary>
        [PublicApi]
        Discovery,

        /// <summary>
        /// Subscription component.
        /// </summary>
        [PublicApi]
        Subscription,

        /// <summary>
        /// Node setup component.
        /// </summary>
        [PublicApi]
        NodeSetup,

        /// <summary>
        /// Utility component.
        /// </summary>
        [PublicApi]
        Utility,

        /// <summary>
        /// Service definition standard library component (external).
        /// </summary>
        [PublicApi]
        RobDefLib,

        /// <summary>
        /// User component (external).
        /// </summary>
        [PublicApi]
        User,

        /// <summary>
        /// User client component (external).
        /// </summary>
        [PublicApi]
        UserClient,

        /// <summary>
        /// User service component (external).
        /// </summary>
        [PublicApi]
        UserService,

        /// <summary>
        /// Third party library component (external).
        /// </summary>
        [PublicApi]
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

    /// <summary>
    /// Logging functions for Robot Raconteur
    /// </summary>
    public static class RRLogFuncs
    {
        /**
        <summary>
        Convert a log level to a string
        </summary>
        <param name="logLevel">The log level</param>
        <returns>The log level as a string</returns>
        <remarks>None</remarks>
        */
        [PublicApi]
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

        /**
        <summary>
        Convert a log component to a string
        </summary>
        <param name="component">The log component</param>
        <returns>The log component as a string</returns>
        <remarks>None</remarks>
        */
        [PublicApi]
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

        /**
        <summary>
        Create a string representation of a node's identity
        for logging.
        </summary>
        <param name="node">The node</param>
        <returns>The node identity as a string</returns>
        <remarks>None</remarks>
        */
        [PublicApi]
        public static string Node_ToString(RobotRaconteurNode node)
        {
            if (node == null)
            {
                return "unknown";
            }

            if (!node.TryGetNodeID(out var id))
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

        /**
        <summary>
        Write a log record to a text writer
        </summary>
        <param name="writer">The text writer</param>
        <param name="record">The log record</param>
        <remarks>None</remarks>
        */
        [PublicApi]
        public static void WriteLogRecord(TextWriter writer, RRLogRecord record)
        {
            writer.Write("[{0}] [{1}] [{2}] [{3}]",
                to_iso_extended_string(record.Time), LogLevel_ToString(record.Level), record.ThreadID,
                Node_ToString(record.Node));
            if (!string.IsNullOrEmpty(record.ComponentName) || !string.IsNullOrEmpty(record.ComponentObjectID))
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

        /**
        <summary>
        Convert a log record to a string
        </summary>
        <param name="record">The log record</param>
        <returns>The log record as a string</returns>
        <remarks>None</remarks>
        */
        [PublicApi]
        public static string LogRecordToString(RRLogRecord record)
        {
            var s = new StringWriter();
            WriteLogRecord(s, record);
            return s.ToString();
        }

        /**
        <summary>
        Log a LogRecord to the node's log record handler
        </summary>
        <remarks>The node stored in the Node field of the record is used to log the record</remarks>
        */
        [PublicApi]
        public static void Log(RRLogRecord record)
        {
            record?.Node?.LogRecord(record);
        }

        /// <summary>
        /// Log a message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="level">The level of the log message</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void Log(string message, RobotRaconteurNode node = null, RobotRaconteur_LogLevel level = RobotRaconteur_LogLevel.Warning, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
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

        /// <summary>
        /// Log a "Fatal" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogFatal(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Fatal, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        /// <summary>
        /// Log an "Error" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogError(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Error, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        /// <summary>
        /// Log a "Warning" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogWarning(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Warning, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        /// <summary>
        /// Log an "Info" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogInfo(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Info, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        /// <summary>
        /// Log a "Debug" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogDebug(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Debug, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }

        /// <summary>
        /// Log a "Trace" message to the node's log record handler
        /// </summary>
        /// <param name="message">The message to log as a string</param>
        /// <param name="node">The node that produced the message and logs it</param>
        /// <param name="component">The component that produced the message</param>
        /// <param name="component_name">The component subname</param>
        /// <param name="component_object_id">An object id to help identify the source</param>
        /// <param name="endpoint">The endpoint that produced the message</param>
        /// <param name="service_path">The service path of the object that produced the message</param>
        /// <param name="member">The name of the member that produced the message</param>
        /// <param name="time">The UTC time of the message</param>
        /// <param name="source_file">The source file of the message</param>
        /// <param name="source_line">The source file line of the message</param>
        /// <param name="thread_id">The ID of the thread that produced the message</param>
        [PublicApi]
        public static void LogTrace(string message, RobotRaconteurNode node = null, RobotRaconteur_LogComponent component = RobotRaconteur_LogComponent.Default,
            string component_name = "", string component_object_id = "", long endpoint = -1, string service_path = "", string member = "",
            DateTime time = default, [CallerFilePath] string source_file = "", [CallerLineNumber] int source_line = 0, string thread_id = "")
        {
            Log(message, node, RobotRaconteur_LogLevel.Trace, component, component_name, component_object_id, endpoint, service_path, member, time, source_file, source_line, thread_id);
        }
    }

    /**
    <summary>
    Interface for log record handlers. Use this interface to implement custom log record handlers.
    </summary>
    **/
    [PublicApi]
    public interface ILogRecordHandler
    {
        /**
        <summary>
        Log a log record
        </summary>
        <param name="record">The log record</param>
        <remarks>None</remarks>
        */
        [PublicApi]
        void Log(RRLogRecord record);
    }
}
