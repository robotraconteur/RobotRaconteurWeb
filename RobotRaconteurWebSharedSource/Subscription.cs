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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Subscription filter node information
    </summary>
    <remarks>
    Specify a node by NodeID and/or NodeName. Also allows specifying
    username and password.

    When using username and credentials, secure transports and specified NodeID should
    be used. Using username and credentials without a transport that verifies the
    NodeID could result in credentials being leaked.
    </remarks>
    */
    [PublicApi]

    public class ServiceSubscriptionFilterNode
    {
        /**
        <summary>
        The NodeID to match. All zero NodeID will match any NodeID.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public NodeID NodeID;
        /**
        <summary>
        The NodeName to match. Empty or null NodeName will match any NodeName.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public string NodeName;
        /**
        <summary>
        The username to use for authentication. Should only be used with secure transports and verified NodeID
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public string Username;
        /**
        <summary>
        The credentials to use for authentication. Should only be used with secure transports and verified NodeID
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public Dictionary<string, object> Credentials;
    }

    /**
    <summary>
    Subscription filter
    </summary>
    <remarks>
    The subscription filter is used with RobotRaconteurNode.SubscribeServiceByType() and
    RobotRaconteurNode::SubscribeServiceInfo2() to decide which services should
    be connected. Detected services that match the service type are checked against
    the filter before connecting.
    </remarks>
    */
    [PublicApi]

    public class ServiceSubscriptionFilter
    {
        /**
        <summary>
        Vector of nodes that should be connected. Empty means match any node.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public ServiceSubscriptionFilterNode[] Nodes;
        /**
        <summary>
        Vector service names that should be connected. Empty means match any service name.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public string[] ServiceNames;
        /**
        <summary>
        Vector of transport schemes. Empty means match any transport scheme.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public string[] TransportSchemes;
        /**
        <summary>
        Attributes to match
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public Dictionary<string, ServiceSubscriptionFilterAttributeGroup> Attributes;
        /**
        <summary>
        Operation to use to match attributes. Defaults to AND
        </summary>
        */
        [PublicApi]

        public ServiceSubscriptionFilterAttributeGroupOperation AttributesMatchOperation = ServiceSubscriptionFilterAttributeGroupOperation.And;
        /**
        <summary>
        A user specified predicate function. If nullptr, the predicate is not checked.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public Func<ServiceInfo2, bool> Predicate;
        /**
        <summary>
        The maximum number of connections the subscription will create. Zero means unlimited connections.
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public uint MaxConnection;
    }

    /**
     * <summary>Subscription filter attribute for use with ServiceSubscriptionFilter</summary>
     *
     */
    [PublicApi]
    public class ServiceSubscriptionFilterAttribute
    {
        /** <summary>The attribute name. Empty for no name</summary> */
        [PublicApi]
        public string Name = string.Empty;
        /**<summary>The string value of the attribute</summary> */
        [PublicApi]
        public string Value = string.Empty;
        /** <summary>The regex value of the attribute</summary> */
        [PublicApi]
        public Regex ValueRegex;
        /** <summary>True if ValueRegex is used, otherwise Value is matched</summary> */
        [PublicApi]
        public bool UseRegex = false;

        /**
         * <summary>Construct a new Service Subscription Filter Attribute object</summary>
         *
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttribute() { }

        /**
         * <summary>Construct a new Service Subscription Filter Attribute object</summary>
         *
         * <remarks>
         * This is a nameless attribute for use with attribute lists
         * </remarks>
         *
         * <param name="value">The attribute value</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttribute(string value)
        {
            Value = value;
        }

        /**
         * <summary>Construct a new Service Subscription Filter Attribute object</summary>
         *
         * <remarks>
         * This is a nameless attribute for use with attribute lists. The value is compared using a regex
         * </remarks>
         * <param name="valueRegex">The attribute value regex</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttribute(Regex valueRegex)
        {
            ValueRegex = valueRegex;
            UseRegex = true;
        }
        /**
         * <summary>Construct a new Service Subscription Filter Attribute object</summary>
         * <remarks>
         * This is a named attribute for use with attribute maps
         * </remarks>
         * <param name="name">The attribute name</param>
         * <param name="value"> The attribute value</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
        /**
         * <summary>Construct a new Service Subscription Filter Attribute object</summary>
         *
         * <remarks>
         * This is a named attribute for use with attribute maps. The value is compared using a regex
         * </remarks>
         * <param name="name">The attribute name</param>
         * <param name="valueRegex">The attribute value regex</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttribute(string name, Regex valueRegex)
        {
            Name = name;
            ValueRegex = valueRegex;
            UseRegex = true;
        }
        /// <summary>
        /// Compare the attribute to a value
        /// </summary>
        /// <param name="value">The value to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(string value)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return false;
            }
            if (UseRegex)
            {
                return ValueRegex.IsMatch(value);
            }
            else
            {
                return value == Value;
            }
        }

        /// <summary>
        /// Compare the attribute to a named value
        /// </summary>
        /// <param name="name">The name to compare</param>
        /// <param name="value">The value to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(string name, string value)
        {
            if (!string.IsNullOrEmpty(Name) && Name != name)
            {
                return false;
            }
            if (UseRegex)
            {
                return ValueRegex.IsMatch(value);
            }
            else
            {
                return value == Value;
            }
        }

        /// <summary>
        /// Compare the attribute to a value list using OR logic
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(List<string> values)
        {
            foreach (string e in values)
            {
                if (IsMatch(e))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compare the attribute to a value list using OR logic
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(List<object> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (object e in values)
            {
                if (e == null)
                {
                    continue;
                }

                string s = e as string;

                if (s == null)
                {
                    continue;
                }


                if (IsMatch(s))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compare the attribute to a value map using OR logic
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(Dictionary<string, object> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (KeyValuePair<string, object> e in values)
            {
                if (e.Value == null)
                {
                    continue;
                }

                string s = e.Value as string;  // Assuming RRArray<char> is somewhat like char[]

                if (s == null)
                {
                    continue;
                }


                if (IsMatch(e.Key, s))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compare the attribute to a value map using OR logic
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(Dictionary<string, string> values)
        {
            foreach (KeyValuePair<string, string> e in values)
            {
                if (IsMatch(e.Key, e.Value))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Utility factory functions for filter attributes
    /// </summary>
    [PublicApi]
    public static class ServiceSubscriptionFilterAttributeFactory
    {
        /**
         * <summary>Create a ServiceSubscriptionFilterAttribute from a regex string</summary>
         *
         * <param name="regexValue">The regex string to compile</param>
         *
         */
        [PublicApi]
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeRegex(string regexValue)
        {
            return new ServiceSubscriptionFilterAttribute(new Regex(regexValue));
        }
        /**
         * <summary>Create a ServiceSubscriptionFilterAttribute from a regex string</summary>
         *
         * <param name="name">The attribute name</param>
         * <param name="regexValue">The regex string to compile</param>
         */
        [PublicApi]
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeRegex(string name, string regexValue)
        {
            return new ServiceSubscriptionFilterAttribute(name, new Regex(regexValue));
        }

        static void IdentifierToRegexCaseInsensitiveUuidMatch(string uuidSegment, TextWriter o)
        {
            foreach (char c in uuidSegment)
            {
                if (char.IsLetter(c))
                {
                    o.Write($"[{char.ToLower(c)}{char.ToUpper(c)}]");
                }
                else
                {
                    o.Write(c);
                }
            }
        }

        static bool IdentifierToRegexUuidAllZero(string uuidSegment)
        {
            return uuidSegment.All(c => c == '0');
        }

        static Regex IdentifierToRegex(string name, string uuidString)
        {
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(uuidString))
            {
                throw new ArgumentException("Name and UUID string cannot both be empty");
            }

            const string nameRegexStr = @"(?:[a-zA-Z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?)(?:\.[a-zA-Z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?)*";
            const string uuidRegexStr = @"\{?([a-fA-F0-9]{8})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{12})\}?";

            var identO = new StringWriter();
            var nameRegex = new Regex(nameRegexStr);
            if (!string.IsNullOrEmpty(name))
            {
                if (!nameRegex.IsMatch(name))
                {
                    throw new ArgumentException("Invalid identifier name");
                }

                identO.Write(name);
            }
            else
            {
                identO.Write($"(?:{nameRegexStr}\\|)?");
            }

            var uuidRegex = new Regex(uuidRegexStr);
            bool zeroUuid = true;
            if (!string.IsNullOrEmpty(uuidString))
            {
                var uuidMatch = uuidRegex.Match(uuidString);
                if (!uuidMatch.Success)
                {
                    throw new ArgumentException("Invalid identifier UUID");
                }

                zeroUuid = IdentifierToRegexUuidAllZero(uuidMatch.Groups[1].Value) &&
                        IdentifierToRegexUuidAllZero(uuidMatch.Groups[2].Value) &&
                        IdentifierToRegexUuidAllZero(uuidMatch.Groups[3].Value) &&
                        IdentifierToRegexUuidAllZero(uuidMatch.Groups[4].Value) &&
                        IdentifierToRegexUuidAllZero(uuidMatch.Groups[5].Value);

                if (!zeroUuid)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        identO.Write("\\|");
                    }
                    identO.Write("\\{?");
                    IdentifierToRegexCaseInsensitiveUuidMatch(uuidMatch.Groups[1].Value, identO);
                    identO.Write("-?");
                    IdentifierToRegexCaseInsensitiveUuidMatch(uuidMatch.Groups[2].Value, identO);
                    identO.Write("-?");
                    IdentifierToRegexCaseInsensitiveUuidMatch(uuidMatch.Groups[3].Value, identO);
                    identO.Write("-?");
                    IdentifierToRegexCaseInsensitiveUuidMatch(uuidMatch.Groups[4].Value, identO);
                    identO.Write("-?");
                    IdentifierToRegexCaseInsensitiveUuidMatch(uuidMatch.Groups[5].Value, identO);
                    identO.Write("\\}?");
                }
            }

            if (zeroUuid)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Name and UUID string cannot both be empty");
                }
                identO.Write($"(?:\\|{uuidRegexStr})?");
            }

            return new Regex(identO.ToString());
        }


        static Regex IdentifierToRegex(string combinedString)
        {
            const string nameRegexStr =
                @"(?:[a-zA-Z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?)(?:\.[a-zA-Z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?)*";
            const string uuidRegexStr =
                @"\{?([a-fA-F0-9]{8})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{4})-?([a-fA-F0-9]{12})\}?";

            string combinedRegexStr = $"({nameRegexStr})\\|({uuidRegexStr})";

            if (string.IsNullOrEmpty(combinedString))
            {
                return IdentifierToRegex("", "");
            }

            var combinedRegex = new Regex(combinedRegexStr);
            var combinedMatch = combinedRegex.Match(combinedString);
            if (combinedMatch.Success)
            {
                string nameSub = combinedMatch.Groups[1].Value;
                string uuidSub = combinedMatch.Groups[2].Value;
                return IdentifierToRegex(nameSub, uuidSub);
            }

            var uuidRegex = new Regex(uuidRegexStr);
            if (uuidRegex.IsMatch(combinedString))
            {
                return IdentifierToRegex("", combinedString);
            }

            return IdentifierToRegex(combinedString, "");
        }
        /**
         * <summary>Create a ServiceSubscriptionFilterAttribute from a combined identifier string</summary>
         * <remarks>
         * The identifier may be a name, UUID, or a combination of both using a "|" to separate the name and UUID.
         * </remarks>
         * <param name="combinedIdentifier">The identifier as a string</param>
         */
        [PublicApi]
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeCombinedIdentifier(string combinedIdentifier)
        {
            return new ServiceSubscriptionFilterAttribute(IdentifierToRegex(combinedIdentifier));
        }
        /**
         * <summary>Create a ServiceSubscriptionFilterAttribute from an identifier</summary>
         *
         * <param name="identifierName">The identifier name</param>
         * <param name="uuidString">The identifier UUID as a string</param>
         * @return ServiceSubscriptionFilterAttribute The created attribute
         */
        [PublicApi]
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeIdentifier(string identifierName, string uuidString)
        {
            return new ServiceSubscriptionFilterAttribute(IdentifierToRegex(identifierName, uuidString));
        }
        /**
         * <summary>Create a ServiceSubscriptionFilterAttribute from an identifier</summary>
         *
         * <param name="name">The attribute name</param>
         * <param name="identifierName">The identifier name</param>
         * <param name="uuidString">The identifier UUID as a string</param>
         */
        [PublicApi]
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeIdentifier(string name, string identifierName, string uuidString)
        {
            return new ServiceSubscriptionFilterAttribute(name, IdentifierToRegex(identifierName, uuidString));
        }
    }

    /// <summary>
    /// Comparison operations for ServiceSubscriptionFilterAttributeGroup
    /// </summary>
    [PublicApi]
    public enum ServiceSubscriptionFilterAttributeGroupOperation
    {
        /// <summary>
        /// OR operation
        /// </summary>
        [PublicApi]
        Or,
        /// <summary>
        /// AND operation
        /// </summary>
        [PublicApi]
        And,
        /// <summary>
        /// NOR operation. Also used for NOT
        /// </summary>
        [PublicApi]
        Nor,
        /// <summary>
        /// NAND operation
        /// </summary>
        [PublicApi]
        Nand
    }
    /**
     * <summary>Subscription filter attribute group for use with ServiceSubscriptionFilter</summary>
     * <remarks>
     * Used to combine multiple ServiceSubscriptionFilterAttribute objects for comparison using
     * AND, OR, NOR, or NAND logic. Other groups can be nested, to allow for complex comparisons.
     * </remarks>
     */
    [PublicApi]
    public class ServiceSubscriptionFilterAttributeGroup
    {
        /** <summary>The attributes in the group</summary> */
        [PublicApi]
        public List<ServiceSubscriptionFilterAttribute> Attributes = new List<ServiceSubscriptionFilterAttribute>();
        /** <summary>The nested groups in the group</summary> */
        [PublicApi]
        public List<ServiceSubscriptionFilterAttributeGroup> Groups = new List<ServiceSubscriptionFilterAttributeGroup>();
        /** <summary>The operation to use for matching the attributes and groups</summary> */
        [PublicApi]
        public ServiceSubscriptionFilterAttributeGroupOperation Operation = ServiceSubscriptionFilterAttributeGroupOperation.Or;
        /** <summary>True if string attributes will be split into a list with delimiter (default ",")</summary> */
        [PublicApi]
        public bool SplitStringAttribute = true;
        /** <summary>Delimiter to use to split string attributes (default ",")</summary> */
        [PublicApi]
        public char SplitStringDelimiter = ',';

        /**
         * <summary>Construct a new Service Subscription Filter Attribute Group object</summary>
         *
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttributeGroup() { }
        /**
         * <summary>Construct a new Service Subscription Filter Attribute Group object</summary>
         *
         * <param name="operation">The operation to use for matching the attributes and groups</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation)
        {
            Operation = operation;
        }
        /**
         * <summary>Construct a new Service Subscription Filter Attribute Group object</summary>
         *
         * <param name="operation">The operation to use for matching the attributes and groups</param>
         * <param name="attributes">The attributes in the group</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation, List<ServiceSubscriptionFilterAttribute> attributes)
        {
            Operation = operation;
            Attributes = attributes;
        }
        /**
         * <summary>Construct a new Service Subscription Filter Attribute Group object</summary>
         *
         * <param name="operation">The operation to use for matching the attributes and groups</param>
         * <param name="groups">The nested groups in the group</param>
         */
        [PublicApi]
        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation, List<ServiceSubscriptionFilterAttributeGroup> groups)
        {
            Operation = operation;
            Groups = groups;
        }
#pragma warning disable 1591
        public static bool ServiceSubscriptionFilterAttributeGroupDoFilter<T>(
    ServiceSubscriptionFilterAttributeGroupOperation operation,
    List<ServiceSubscriptionFilterAttribute> attributes,
    List<ServiceSubscriptionFilterAttributeGroup> groups,
    List<object> values)
        {
            switch (operation)
            {
                case ServiceSubscriptionFilterAttributeGroupOperation.Or:
                    {
                        if (!attributes.Any() && !groups.Any())
                        {
                            return true;
                        }
                        foreach (var e in groups)
                        {
                            if (e.IsMatch(values))
                            {
                                return true;
                            }
                        }
                        foreach (var e in attributes)
                        {
                            if (e.IsMatch(values))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                case ServiceSubscriptionFilterAttributeGroupOperation.And:
                    {
                        if (!attributes.Any() && !groups.Any())
                        {
                            return true;
                        }
                        foreach (var e in groups)
                        {
                            if (!e.IsMatch(values))
                            {
                                return false;
                            }
                        }
                        foreach (var e in attributes)
                        {
                            if (!e.IsMatch(values))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                case ServiceSubscriptionFilterAttributeGroupOperation.Nor:
                    {
                        return !ServiceSubscriptionFilterAttributeGroupDoFilter<T>(ServiceSubscriptionFilterAttributeGroupOperation.Or, attributes, groups, values);
                    }
                case ServiceSubscriptionFilterAttributeGroupOperation.Nand:
                    {
                        return !ServiceSubscriptionFilterAttributeGroupDoFilter<T>(ServiceSubscriptionFilterAttributeGroupOperation.And, attributes, groups, values);
                    }
                default:
                    {
                        throw new ArgumentException("Invalid attribute filter operation");
                    }
            }
        }
#pragma warning restore 1591

        /// <summary>
        /// Compare the group to a value
        /// </summary>
        /// <param name="value">The value to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(string value)
        {
            if (!SplitStringAttribute)
            {
                List<string> value_v = new List<string>();
                value_v.Add(value);
                return IsMatch(value_v);
            }
            else
            {
                List<string> value_v = new List<string>(value.Split(','));
                return IsMatch(value_v);
            }
        }

        /// <summary>
        /// Compare the group to a list of values
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(List<string> values)
        {
            return ServiceSubscriptionFilterAttributeGroupDoFilter<string>(Operation, Attributes, Groups, values.Select(x => (object)x).ToList());
        }

        /// <summary>
        /// Compare the group to a list of values
        /// </summary>
        /// <param name="values">The values to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(List<object> values)
        {
            return ServiceSubscriptionFilterAttributeGroupDoFilter<object>(Operation, Attributes, Groups, values);
        }

        /*public bool IsMatch(Dictionary<string, object> values)
        {
            // TODO: Implementation
            throw new NotImplementedException();
        }

        public bool IsMatch(Dictionary<string, string> values)
        {
            // TODO: Implementation
            throw new NotImplementedException();
        }*/

        /// <summary>
        /// Compare the group to a value
        /// </summary>
        /// <param name="value">The value to compare</param>
        /// <returns></returns>
        [PublicApi]
        public bool IsMatch(object value)
        {
            if (value == null)
            {
                List<string> empty_values = new List<string>();
                return IsMatch(empty_values);
            }

            string a0 = value as string;
            if (a0 != null)
            {
                return IsMatch(a0);
            }

            List<object> a1 = value as List<object>;
            if (a1 != null)
            {
                return IsMatch(a1);
            }

            List<string> a2 = value as List<string>;
            if (a2 != null)
            {
                return IsMatch(a2);
            }

            return false;
        }
    }
    /**
    <summary>
    ClientID for use with ServiceSubscription
    </summary>
    <remarks>
    The ServiceSubscriptionClientID stores the NodeID
    and ServiceName of a connected service.
    </remarks>
    */
    [PublicApi]

    public struct ServiceSubscriptionClientID
    {
        /**
        <summary>
        The NodeID of the connected service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public NodeID NodeID;
        /**
        <summary>
        The ServiceName of the connected service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public string ServiceName;
        /**
        <summary>
        Construct a ServiceSubscriptionClientID
        </summary>
        <remarks>None</remarks>
        <param name="NodeID">The NodeID</param>
        <param name="ServiceName">The Service Name</param>
        */
        [PublicApi]

        public ServiceSubscriptionClientID(NodeID NodeID, string ServiceName)
        {
            this.NodeID = NodeID;
            this.ServiceName = ServiceName;
        }
#pragma warning disable 1591
        public override bool Equals(object obj)
        {
            if (obj is ServiceSubscriptionClientID)
            {
                ServiceSubscriptionClientID o = (ServiceSubscriptionClientID)obj;
                return NodeID.Equals(o.NodeID) && ServiceName == o.ServiceName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return NodeID.GetHashCode() ^ ServiceName.GetHashCode();
        }

        public static bool operator ==(ServiceSubscriptionClientID left, ServiceSubscriptionClientID right)
        {
            return left.NodeID == right.NodeID && left.ServiceName == right.ServiceName;
        }

        public static bool operator !=(ServiceSubscriptionClientID left, ServiceSubscriptionClientID right)
        {
            return !(left == right);
        }
#pragma warning restore 1591
    }

    static class SubscriptionFilterUtil
    {
        // Filter service using example from Subscription.cpp
        internal static bool FilterService(string[] service_types, ServiceSubscriptionFilter filter, Discovery_nodestorage storage,
            ServiceInfo2 info, out List<string> urls, out string client_service_type, out ServiceSubscriptionFilterNode filter_node)
        {
            filter_node = null;
            client_service_type = null;
            urls = new List<string>();
            if (service_types != null && service_types.Length != 0 && !service_types.Contains(info.RootObjectType))
            {
                bool implements_match = false;
                foreach (var implements in info.RootObjectImplements)
                {
                    if (service_types.Contains(implements))
                    {
                        implements_match = true;
                        client_service_type = implements;
                        break;
                    }
                }

                if (!implements_match)
                {
                    return false;
                }
            }
            else
            {
                client_service_type = info.RootObjectType;
            }

            if (filter != null)
            {
                if (filter.Nodes != null && filter.Nodes.Length > 0)
                {
                    foreach (var f1 in filter.Nodes)
                    {
                        if ((f1.NodeID == null || f1.NodeID.IsAnyNode) && string.IsNullOrEmpty(f1.NodeName))
                        {
                            // Wildcard match, most likely an error...
                            filter_node = f1;
                            break;
                        }

                        if ((f1.NodeID != null && !f1.NodeID.IsAnyNode) && !string.IsNullOrEmpty(f1.NodeName))
                        {
                            if (f1.NodeName == info.NodeName && f1.NodeID == info.NodeID)
                            {
                                filter_node = f1;
                                break;
                            }
                        }

                        if ((f1.NodeID == null || f1.NodeID.IsAnyNode) && !string.IsNullOrEmpty(f1.NodeName))
                        {
                            if (f1.NodeName == info.NodeName)
                            {
                                filter_node = f1;
                                break;
                            }
                        }

                        if ((f1.NodeID != null && !f1.NodeID.IsAnyNode) && string.IsNullOrEmpty(f1.NodeName))
                        {
                            if (f1.NodeID == info.NodeID)
                            {
                                filter_node = f1;
                                break;
                            }
                        }
                    }

                    if (filter_node == null)
                    {
                        return false;
                    }
                }

                if (filter.TransportSchemes == null || filter.TransportSchemes.Length == 0)
                {
                    urls = info.ConnectionURL.ToList();
                }

                else
                {
                    foreach (var url1 in info.ConnectionURL)
                    {
                        foreach (var scheme1 in filter.TransportSchemes)
                        {
                            if (url1.StartsWith(scheme1 + "://"))
                            {
                                urls.Add(url1);
                            }
                        }
                    }

                    if (urls.Count == 0 && storage != null)
                    {
                        // We didn't find a match with the ServiceInfo2 urls, attempt to use NodeDiscoveryInfo
                        // TODO: test this....

                        foreach (var url2 in storage.info.URLs)
                        {
                            var url1 = url2.URL;
                            foreach (var scheme1 in filter.TransportSchemes)
                            {
                                if (url1.StartsWith(scheme1 + "://"))
                                {
                                    urls.Add(url1.Replace("RobotRaconteurServiceIndex", info.Name));
                                }
                            }
                        }
                    }
                }

                if (filter.ServiceNames != null && filter.ServiceNames.Length > 0)
                {
                    if (!filter.ServiceNames.Contains(info.Name))
                    {
                        return false;
                    }
                }

                if (filter.Attributes != null && filter.Attributes.Any())
                {
                    List<bool> attrMatches = new List<bool>();

                    foreach (var e in filter.Attributes)
                    {
                        if (!info.Attributes.TryGetValue(e.Key, out var e2Value))
                        {
                            object nullValue = null;
                            attrMatches.Add(e.Value.IsMatch(nullValue));
                        }
                        else
                        {
                            attrMatches.Add(e.Value.IsMatch(e2Value));
                        }
                    }

                    switch (filter.AttributesMatchOperation)
                    {
                        case ServiceSubscriptionFilterAttributeGroupOperation.Or:
                            if (!attrMatches.Contains(true))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.Nor:
                            if (attrMatches.Contains(true))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.Nand:
                            if (!attrMatches.Contains(false))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.And:
                        default:
                            if (attrMatches.Contains(false))
                                return false;
                            break;
                    }
                }

                if (filter.Predicate != null)
                {
                    if (!filter.Predicate(info))
                    {
                        return false;
                    }
                }
            }
            else
            {
                urls = info.ConnectionURL.ToList();
            }

            return true;
        }
    }

    interface IServiceSubscription
    {
        void Init(string[] service_types, ServiceSubscriptionFilter filter);
        void NodeUpdated(Discovery_nodestorage nodestorage);
        void NodeLost(Discovery_nodestorage nodestorage);
        void Close();
    }

    class ServiceInfo2Subscription_client
    {
        internal NodeID nodeid;
        internal string service_name;
        internal ServiceInfo2 service_info2;
        internal DateTime last_node_update;
    }

    /**
    <summary>
    Subscription for information about detected services
    </summary>
    <remarks>
    <para>
    Created using RobotRaconteurNode::SubscribeServiceInfo2()
    </para>
    <para>
    The ServiceInfo2Subscription class is used to track services with a specific service type as they are
    detected on the local network and when they are lost. The currently detected services can also
    be retrieved. The service information is returned using the ServiceInfo2 structure.
    </para>
    </remarks>
    */
    [PublicApi]

    public class ServiceInfo2Subscription : IServiceSubscription
    {
        bool active;
        Dictionary<ServiceSubscriptionClientID, ServiceInfo2Subscription_client> clients = new Dictionary<ServiceSubscriptionClientID, ServiceInfo2Subscription_client>();
        uint retry_delay;

        Discovery parent;
        RobotRaconteurNode node;
#pragma warning disable 1591
        public ServiceInfo2Subscription(Discovery parent)
        {
            this.parent = parent;
            this.node = parent.node;
            active = true;
            retry_delay = 15000;
        }
#pragma warning restore 1591
        /**
        <summary>
        Close the subscription
        </summary>
        <remarks>
        Closes the subscription. Subscriptions are automatically closed when the node is shut down.
        </remarks>
        */
        [PublicApi]

        public void Close()
        {
            lock (this)
            {
                if (!active)
                {
                    return;
                }

                active = false;
                clients.Clear();
            }

            parent.SubscriptionClosed(this);
        }
        string[] service_types;
        ServiceSubscriptionFilter filter;
        void IServiceSubscription.Init(string[] service_types, ServiceSubscriptionFilter filter)
        {
            this.active = true;
            this.service_types = service_types;
            this.filter = filter;
        }

        void IServiceSubscription.NodeLost(Discovery_nodestorage storage)
        {
            lock (this)
            {
                if (storage == null)
                {
                    return;
                }

                if (storage.info == null)
                {
                    return;
                }

                var id = storage.info.NodeID;

                foreach (var k in clients.Keys.ToList())
                {
                    var v = clients[k];
                    if (k.NodeID == storage.info.NodeID)
                    {

                        var info1 = v.service_info2;
                        var id1 = k;
                        clients.Remove(k);
                        Task.Run(() => ServiceLost?.Invoke(this, id1, info1));

                    }
                }
            }
        }

        void IServiceSubscription.NodeUpdated(Discovery_nodestorage storage)
        {
            lock (this)
            {
                if (!active)
                    return;
                if (storage == null)
                    return;
                if (storage.services == null)
                    return;
                if (storage.info == null)
                    return;

                foreach (var info in storage.services)
                {
                    var k = new ServiceSubscriptionClientID(storage.info.NodeID, info.Name);
                    ServiceInfo2Subscription_client e = null;
                    if (clients.TryGetValue(k, out e))
                    {
                        var info2 = e.service_info2;
                        if (info.NodeName != info2.NodeName || info2.Name != info.Name ||
                            info2.RootObjectType != info.RootObjectType || info2.ConnectionURL != info.ConnectionURL ||
                            !new HashSet<string>(info.RootObjectImplements).SetEquals(new HashSet<string>(info2.RootObjectImplements)))
                        {
                            e.service_info2 = info;
                            Task.Run(() => ServiceDetected?.Invoke(this, k, info));
                        }
                        e.last_node_update = DateTime.UtcNow;
                        return;
                    }

                    List<string> urls;
                    string client_service_type;
                    ServiceSubscriptionFilterNode filter_node;

                    if (!SubscriptionFilterUtil.FilterService(service_types, filter, storage, info, out urls, out client_service_type, out filter_node))
                    {
                        continue;
                    }

                    if (e == null)
                    {
                        var c2 = new ServiceInfo2Subscription_client();
                        c2.nodeid = info.NodeID;
                        c2.service_name = info.Name;
                        c2.service_info2 = info;
                        c2.last_node_update = DateTime.UtcNow;

                        var noden = new ServiceSubscriptionClientID(c2.nodeid, c2.service_name);

                        clients.Add(noden, c2);

                        Task.Run(() => ServiceDetected?.Invoke(this, noden, c2.service_info2));
                    }
                }
            }

            foreach (var k in clients.Keys.ToList())
            {
                var v = clients[k];

                if (k.NodeID == storage.info.NodeID)
                {
                    bool found = false;
                    foreach (var info in storage.services)
                    {
                        if (info.Name == k.ServiceName)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var info1 = v.service_info2;
                        var id1 = k;

                        clients.Remove(k);

                        Task.Run(() => ServiceDetected?.Invoke(this, id1, info1));
                    }
                }

            }


        }
        /**
        <summary>
        Listener event that is invoked when a service is detected
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> ServiceDetected;
        /**
        <summary>
        Listener event that is invoked when a service is lost
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> ServiceLost;

        /**
        <summary>
        Returns a dictionary of detected services.
        </summary>
        <remarks>
        The returned dictionary contains the detected nodes as ServiceInfo2. The map
        is keyed with ServiceSubscriptionClientID.

        This function does not block.
        </remarks>
        <returns>The detected services</returns>
        */
        [PublicApi]

        public Dictionary<ServiceSubscriptionClientID, ServiceInfo2> GetDetectedServiceInfo2()
        {
            lock (this)
            {
                return clients.ToDictionary(x => new ServiceSubscriptionClientID(x.Value.nodeid, x.Value.service_name), x => x.Value.service_info2);
            }
        }


    }

    class ServiceSubscription_client
    {
        internal NodeID nodeid;
        internal string nodename;
        internal string service_name;
        internal string service_type;
        internal string[] urls;

        internal object client;
        internal DateTime last_node_update;
        internal bool connecting;
        internal uint error_count;

        internal string username;
        internal Dictionary<string, object> credentials;
        internal bool claimed;
        internal CancellationTokenSource cancel = new CancellationTokenSource();
        internal bool erase = false;
    }

    /**
    <summary>
    Subscription that automatically connects services and manages lifecycle of connected services
    </summary>
    <remarks>
    <para>
    Created using RobotRaconteurNode.SubscribeService() or RobotRaconteurNode.SubscribeServiceByType(). The
    ServiceSubscription class is used to automatically create and manage connections based on connection criteria.
    RobotRaconteurNode.SubscribeService() is used to create a robust connection to a service with a specific URL.
    RobotRaconteurNode.SubscribeServiceByType() is used to connect to services with a specified type, filtered with a
    ServiceSubscriptionFilter. Subscriptions will create connections to matching services, and will retry the connection
    if it fails or the connection is lost. This behavior allows subscriptions to be used to create robust connections.
    The retry delay for connections can be modified using ConnectRetryDelay.
    </para>
    <para>
    The currently connected clients can be retrieved using the GetConnectedClients() function. A single "default client"
    can be retrieved using the GetDefaultClient() function or TryGetDefaultClient() functions. Listeners for client
    connect and disconnect events can be added  using the AddClientConnectListener() and AddClientDisconnectListener()
    functions. If the user wants to claim a client, the ClaimClient() and ReleaseClient() functions will be used.
    Claimed clients will no longer have their lifecycle managed by the subscription.
    </para>
    <para>
    Subscriptions can be used to create `pipe` and `wire` subscriptions. These member subscriptions aggregate
    the packets and values being received from all services. They can also act as a "reverse broadcaster" to
    send packets and values to all services that are actively connected. See PipeSubscription and WireSubscription.
    </para>
    </remarks>
    */
    [PublicApi]

    public class ServiceSubscription : IServiceSubscription
    {
#pragma warning disable 1591
        bool active = false;
        Dictionary<ServiceSubscriptionClientID, ServiceSubscription_client> clients = new Dictionary<ServiceSubscriptionClientID, ServiceSubscription_client>();

        internal RobotRaconteurNode node;
        protected internal Discovery parent;
        protected internal string[] service_types;
        ServiceSubscriptionFilter filter;
        List<WireSubscriptionBase> wire_subscriptions = new List<WireSubscriptionBase>();
        List<PipeSubscriptionBase> pipe_subscriptions = new List<PipeSubscriptionBase>();

        protected internal bool use_service_url = false;
        protected internal string[] service_url;
        protected internal string service_url_username;
        protected internal Dictionary<string, object> service_url_credentials;

        CancellationTokenSource cancel = new CancellationTokenSource();
#pragma warning restore 1591
        /**
        <summary>
        Close the subscription
        </summary>
        <remarks>
        Close the subscription. Subscriptions are automatically closed when the node is shut down.
        </remarks>
        */
        [PublicApi]

        public void Close()
        {
            lock (this)
            {
                cancel?.Cancel();
                cancel = null;

                if (!active)
                    return;
                active = false;

                foreach (var w in wire_subscriptions)
                {
                    Task.Run(() => w.Close()).IgnoreResult();
                }

                foreach (var p in pipe_subscriptions)
                {
                    Task.Run(() => p.Close()).IgnoreResult();
                }

                foreach (var c in clients.Values)
                {
                    c.claimed = false;
                    if (c.client != null)
                    {
                        Task.Run(() => node.DisconnectService(c)).IgnoreResult();
                    }
                }

                wire_subscriptions.Clear();
                pipe_subscriptions.Clear();
                clients.Clear();

            }

            parent.SubscriptionClosed(this);
        }
#pragma warning disable 1591
        public void SoftClose()
        {

            service_url = null;
            service_url_username = null;
            service_url_credentials = null;
            use_service_url = true;
            service_types = null;
            filter = null;

            lock (this)
            {
                cancel?.Cancel();
                cancel = null;

                foreach (var c in clients.Values)
                {
                    c.claimed = false;
                    if (c.client != null)
                    {
                        Task.Run(() => node.DisconnectService(c)).IgnoreResult();
                    }
                }

                clients.Clear();

            }
        }

        public void Init(string[] service_types, ServiceSubscriptionFilter filter)
        {
            this.active = true;
            this.service_types = service_types;
            this.filter = filter;
            this.use_service_url = false;
            CancellationTokenSource old_cancel = cancel;
            cancel = new CancellationTokenSource();
            old_cancel?.Cancel();
        }

        internal void InitServiceURL(string[] url, string username, Dictionary<string, object> credentials, string objecttype)
        {
            if (url.Length == 0)
            {
                throw new ArgumentException("URL must not be empty for SubscribeService");
            }

            NodeID service_nodeid;
            string service_nodename;
            string service_name;

            var url_res = TransportUtil.ParseConnectionUrl(url[0]);
            service_nodeid = url_res.nodeid;
            service_nodename = url_res.nodename;
            service_name = url_res.service;

            CancellationTokenSource old_cancel = cancel;
            cancel = new CancellationTokenSource();
            old_cancel?.Cancel();

            for (int i = 1; i < url.Length; i++)
            {
                var url_res1 = TransportUtil.ParseConnectionUrl(url[i]);
                if (url_res1.nodeid != url_res.nodeid || url_res1.nodename != url_res.nodename || url_res1.service != url_res.service)
                {
                    throw new ArgumentException("Provided URLs do not point to the same service in SubscribeService");
                }
            }

            ConnectRetryDelay = 2500;
            active = true;
            service_url = url;
            service_url_username = username;
            service_url_credentials = credentials;
            use_service_url = true;

            var c2 = new ServiceSubscription_client()
            {
                connecting = true,
                nodeid = service_nodeid,
                nodename = service_nodename,
                service_name = service_name,
                service_type = objecttype,
                urls = url,
                last_node_update = DateTime.UtcNow,
                username = username,
                credentials = credentials,
            };

            this.cancel.Token.Register(() => c2.cancel.Cancel());

            lock (clients)
            {
                clients.Add(new ServiceSubscriptionClientID(service_nodeid, service_name), c2);
            }

            RunClient(c2).IgnoreResult();

        }

        static string ServiceSubscription_ConnectServiceType(RobotRaconteurNode node, string service_type_in)
        {
            if (node == null)
            {
                return service_type_in;
            }

            if (node.DynamicServiceFactory != null)
            {
                return "";
            }

            return service_type_in;
        }

        async Task RunClient(ServiceSubscription_client client)
        {
            CancellationTokenSource cancel;
            lock (this)
            {
                cancel = this.cancel;
            }

            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    client.connecting = true;
                    object o;
                    TaskCompletionSource<bool> wait_task;
                    if (client.erase)
                    {
                        return;
                    }
                    try
                    {
                        //ClientContext.ClientServiceListenerDelegate client_listener = delegate (ClientContext context, ClientServiceListenerEventType evt, object param) { };
                        o = await node.ConnectService(client.urls, client.username, client.credentials, null, ServiceSubscription_ConnectServiceType(node, client.service_type), cancel.Token).ConfigureAwait(false);
                        lock (client)
                        {
                            client.client = o;
                            client.connecting = false;
                            client.error_count = 0;
                            if (client.nodeid == null || client.nodeid.IsAnyNode)
                            {
                                client.nodeid = ((ServiceStub)o).RRContext.RemoteNodeID;
                            }

                            if (string.IsNullOrEmpty(client.nodename))
                            {
                                client.nodename = ((ServiceStub)o).RRContext.RemoteNodeName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            ClientConnectFailed?.Invoke(this, new ServiceSubscriptionClientID(client.nodeid, client.service_name), client.urls, ex);
                        }
                        catch (Exception ex2)
                        {
                            LogDebug(string.Format("Error in ServiceSubscription.ConnectClientFailed callback {0}", ex2), node, RobotRaconteur_LogComponent.Subscription);
                        }
                        client.error_count++;
                        if (client.error_count > 25 && !use_service_url)
                        {
                            client.connecting = false;
                            lock (this)
                            {
                                clients.Remove(new ServiceSubscriptionClientID(client.nodeid, client.service_name));
                            }
                            return;
                        }

                        await Task.Delay((int)ConnectRetryDelay, cancel.Token).IgnoreResult().ConfigureAwait(false);
                        continue;
                    }

                    wait_task = new TaskCompletionSource<bool>();
                    wait_task.AttachCancellationToken(cancel.Token);
                    bool closed_sent = false;
                    ((ServiceStub)o).RRContext.ClientServiceListener += delegate (ClientContext context, ClientServiceListenerEventType evt, object param)
                    {
                        // TODO: ClientConnectionTimeout and TransportConnectionClosed
                        if (evt == ClientServiceListenerEventType.ClientClosed
                            || evt == ClientServiceListenerEventType.ClientConnectionTimeout
                            || evt == ClientServiceListenerEventType.TransportConnectionClosed)
                        {

                            try
                            {

                                if (client.erase)
                                {
                                    lock (this)
                                    {
                                        clients.Remove(new ServiceSubscriptionClientID(client.nodeid, client.service_name));
                                    }
                                }

                                bool send_closed = false;
                                lock (this)
                                {
                                    send_closed = !closed_sent;
                                    closed_sent = true;
                                }
                                if (send_closed)
                                {
                                    ClientDisconnected?.Invoke(this, new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                                }
                            }
                            catch (Exception ex2)
                            {
                                LogDebug(string.Format("Error in ServiceSubscription.ConnectClientFailed callback {0}", ex2), node, RobotRaconteur_LogComponent.Subscription);
                            }
                            client.claimed = false;
                            wait_task.TrySetResult(true);
                        }
                    };
                    try
                    {
                        ClientConnected?.Invoke(this, new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                    }
                    catch (Exception ex)
                    {
                        LogDebug(string.Format("Error in ServiceSubscription.ClientConnected callback {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                    }

                    lock (connect_waiter)
                    {
                        connect_waiter.NotifyAll(o);
                    }
                    lock (this)
                    {
                        foreach (var p in pipe_subscriptions)
                        {
                            p.ClientConnected(new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                        }

                        foreach (var w in wire_subscriptions)
                        {
                            w.ClientConnected(new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                        }
                    }

                    try
                    {
                        await wait_task.Task.ConfigureAwait(false);
                    }
                    catch { }


                    client.client = null;

                    try
                    {
                        _ = Task.Run(delegate ()
                        {
                            try
                            {
                                _ = node.DisconnectService(o).IgnoreResult();
                            }
                            catch { }
                        }).IgnoreResult();
                    }
                    catch { }


                    lock (this)
                    {
                        foreach (var p in pipe_subscriptions)
                        {
                            p.ClientDisconnected(new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                        }

                        foreach (var w in wire_subscriptions)
                        {
                            w.ClientDisconnected(new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                        }
                    }


                    await Task.Delay((int)ConnectRetryDelay, cancel.Token).IgnoreResult().ConfigureAwait(false);
                }
            }
            finally
            {
                if (!client.claimed && client.client != null)
                {
                    _ = node.DisconnectService(client.client).IgnoreResult();
                }
            }
        }

        void IServiceSubscription.NodeLost(Discovery_nodestorage nodestorage)
        {
            if (use_service_url)
                return;

            // TODO: Not using this feature, if enough connect attempts fail client will be deleted
        }

        void IServiceSubscription.NodeUpdated(Discovery_nodestorage storage)
        {
            lock (this)
            {
                if (use_service_url)
                    return;
                if (!active)
                    return;
                if (storage == null)
                    return;
                if (storage.services == null)
                    return;
                if (storage.info == null)
                    return;

                foreach (var info in storage.services)
                {
                    var k = new ServiceSubscriptionClientID(storage.info.NodeID, info.Name);

                    if (clients.TryGetValue(k, out var e))
                    {
                        if (e.client != null)
                            // Already have connection, ignore
                            return;
                    }

                    if (!SubscriptionFilterUtil.FilterService(service_types, filter, storage, info, out var urls, out var client_service_type, out var filter_node))
                    {
                        // Filter match failure
                        continue;
                    }

                    if (!clients.TryGetValue(k, out var e2))
                    {
                        var c2 = new ServiceSubscription_client()
                        {
                            nodeid = info.NodeID,
                            nodename = info.NodeName,
                            service_name = info.Name,
                            connecting = true,
                            service_type = client_service_type,
                            urls = urls.ToArray(),
                            last_node_update = DateTime.UtcNow
                        };

                        this.cancel.Token.Register(() => c2.cancel.Cancel());

                        if (filter_node != null && !string.IsNullOrEmpty(filter_node.Username) && filter_node.Credentials != null)
                        {
                            c2.username = filter_node.Username;
                            c2.credentials = filter_node.Credentials;
                        }

                        lock (clients)
                        {
                            clients.Add(new ServiceSubscriptionClientID(c2.nodeid, c2.service_name), c2);
                        }

                        RunClient(c2).IgnoreResult();
                    }
                    else
                    {
                        e2.urls = urls.ToArray();
                        e2.last_node_update = DateTime.UtcNow;
                    }
                }
            }

        }
#pragma warning restore 1591
        /**
        <summary>
        Returns a dictionary of connected clients
        </summary>
        <remarks>
        <para>
        The returned dictionary contains the connect clients. The map
        is keyed with ServiceSubscriptionClientID.
        </para>
        <para>
        Clients must be cast to a type, similar to the client returned by
        RobotRaconteurNode.ConnectService().
        </para>
        <para>
        Clients can be "claimed" using ClaimClient(). Once claimed, the subscription
        will stop managing the lifecycle of the client.
        </para>
        <para>
        This function does not block.
        </para>
        </remarks>
        <returns>The detected services.</returns>
        */
        [PublicApi]

        public Dictionary<ServiceSubscriptionClientID, object> GetConnectedClients()
        {
            var o = new Dictionary<ServiceSubscriptionClientID, object>();
            lock (this)
            {
                foreach (var kv in clients)
                {
                    if (kv.Value.client != null)
                    {
                        o.Add(kv.Key, kv.Value.client);
                    }
                }
            }
            return o;
        }

        /**
        <summary>
        Event listener for when a client connects
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientConnected;
        /**
        <summary>
        Event listener for when a client disconnects
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientDisconnected;
        /**
        <summary>
        Event listener for when a client connection attempt fails. Use to diagnose connection problems
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, string[], Exception> ClientConnectFailed;
        /**
        <summary>
        Claim a client that was connected by the subscription
        </summary>
        <remarks>
        The subscription class will automatically manage the lifecycle of the connected clients. The clients
        will be automatically disconnected and/or reconnected as necessary. If the user wants to disable
        this behavior for a specific client connection, the client connection can be "claimed".
        </remarks>
        <param name="client">The client to be claimed</param>
        */
        [PublicApi]

        public void ClaimClient(object client)
        {
            lock (this)
            {
                if (!active)
                {
                    throw new InvalidOperationException("Service closed");
                }

                var sub = FindClient(client);
                if (sub == null)
                {
                    throw new ArgumentException("Invalid client for ClaimClient");
                }

                sub.claimed = true;
            }
        }
        /**
        <summary>
        Release a client previously claimed with ClaimClient()
        </summary>
        <remarks>
        Lifecycle management is returned to the subscription
        </remarks>
        <param name="client">The client to release claim</param>
        */
        [PublicApi]

        public void ReleaseClient(object client)
        {
            lock (this)
            {
                if (!active)
                {
                    Task.Run(() => node.DisconnectService(client)).IgnoreResult();
                }

                var sub = FindClient(client);
                if (sub == null)
                {
                    return;
                }

                sub.claimed = false;
            }
        }

        /**
        <summary>
        Get or set the connect retry delay in milliseconds
        </summary>
        <remarks>
        Default connect retry delay is 2.5 seconds
        </remarks>
        <value />
        */
        [PublicApi]
        public uint ConnectRetryDelay { get; set; } = 2500;

        /**
        <summary>
        Get the "default client" connection
        </summary>
        <remarks>
        <para>
        The "default client" is the "first" client returned from the connected clients map. This is effectively
        default, and is only useful if only a single client connection is expected. This is normally true
        for RobotRaconteurNode.SubscribeService()
        </para>
        <para>
        Clients using GetDefaultClient() should not store a reference to the client. It should instead
        call GetDefaultClient() right before using the client to make sure the most recenty connection
        is being used. If possible, SubscribePipe() or SubscribeWire() should be used so the lifecycle
        of pipes and wires can be managed automatically.
        </para>
        </remarks>
        <returns>The client connection. Cast to expected object type</returns>
        */
        [PublicApi]

        public T GetDefaultClient<T>()
        {
            lock (this)
            {
                T ret;
                if (!TryGetDefaultClient(out ret))
                {
                    throw new ConnectionException("No clients connected");
                }

                return ret;
            }
        }
        /**
        <summary>
        Try getting the "default client" connection
        </summary>
        <remarks>
        Same as GetDefaultClient(), but returns a bool success instead of throwing
        exceptions on failure.
        </remarks>
        <param name="client">[out] The client connection</param>
        <returns>true if client object is valid, false otherwise</returns>
        */
        [PublicApi]

        public bool TryGetDefaultClient<T>(out T client)
        {
            lock (this)
            {
                var client_storage = clients.Values.FirstOrDefault();
                if (client_storage == null)
                {
                    client = default;
                    return false;
                }
                var ret = client_storage.client;
                if (ret == null)
                {
                    client = default;
                    return false;
                }
                client = (T)ret;
                return true;
            }
        }

        AsyncValueWaiter<object> connect_waiter = new AsyncValueWaiter<object>();
        /**
<summary>
Get the "default client" connection, waiting with timeout if not connected
</summary>
<remarks>
<para>
The "default client" is the "first" client returned from the connected clients map. This is effectively
default, and is only useful if only a single client connection is expected. This is normally true
for RobotRaconteurNode.SubscribeService()
</para>
<para>
Clients using GetDefaultClient() should not store a reference to the client. It should instead
call GetDefaultClient() right before using the client to make sure the most recently connection
is being used. If possible, SubscribePipe() or SubscribeWire() should be used so the lifecycle
of pipes and wires can be managed automatically.
</para>
</remarks>
<param name="cancel">Cancellation token</param>
<returns>The client connection. Cast to expected object type</returns>
*/
        [PublicApi]

        public async Task<T> GetDefaultClientWait<T>(CancellationToken cancel = default)
        {
            var waiter = connect_waiter.CreateWaiterTask(-1, cancel);
            using (waiter)
            {
                if (TryGetDefaultClient<T>(out var o))
                {
                    return o;
                }
                await waiter.Task.ConfigureAwait(false);
                return GetDefaultClient<T>();
            }
        }
        /**
        <summary>
        Try getting the "default client" connection, waiting with timeout if not connected
        </summary>
        <remarks>
        Same as GetDefaultClientWait(), but returns a bool success instead of throwing
        exceptions on failure.
        </remarks>
        <param name="cancel">Cancellation token</param>
        <returns>Tuple of bool success and client object</returns>
        */
        [PublicApi]

        public async Task<Tuple<bool, T>> TryGetDefaultClientWait<T>(CancellationToken cancel = default)
        {
            var waiter = connect_waiter.CreateWaiterTask(-1, cancel);
            using (waiter)
            {
                if (TryGetDefaultClient<T>(out var o))
                {
                    return Tuple.Create(true, o);
                }
                await waiter.Task.ConfigureAwait(false);
                T client;
                bool res = TryGetDefaultClient<T>(out client);
                return Tuple.Create(res, client);
            }
        }

        /**
        <summary>
        Get the service connection URL
        </summary>
        <remarks>
        Returns the service connection URL. Only valid when subscription was created using
        RobotRaconteurNode.SubscribeService(). Will throw an exception if subscription
        was opened using RobotRaconteurNode.SubscribeServiceByType()
        </remarks>
        */
        [PublicApi]

        public string[] GetServiceURL()
        {
            if (!use_service_url)
            {
                throw new InvalidOperationException("Subscription not using service url");
            }

            return service_url;
        }
        /**
        <summary>
        Update the service connection URL
        </summary>
        <remarks>
        Updates the URL used to connect to the service. If close_connected is true,
        existing connections will be closed. If false,
        existing connections will not be closed.
        </remarks>
        <param name="url">The new URL to use to connect to service</param>
        <param name="username">(Optional) The new username</param>
        <param name="credentials">(Optional) The new credentials</param>
        <param name="object_type">(Optional) The desired root object proxy type. Optional but highly recommended.</param>
        <param name="close_connected">(Optional, default false) Close existing connections</param>
        */
        [PublicApi]

        public void UpdateServiceURL(string url, string username = null, Dictionary<string, object> credentials = null, string object_type = null, bool close_connected = false)
        {
            UpdateServiceURL(new string[] { url }, username, credentials, object_type, close_connected);
        }
        /**
        <summary>
        Update the service connection URL
        </summary>
        <remarks>
        Updates the URL used to connect to the service. If close_connected is true,
        existing connections will be closed. If false,
        existing connections will not be closed.
        </remarks>
        <param name="url">The new URL to use to connect to service</param>
        <param name="username">(Optional) The new username</param>
        <param name="credentials">(Optional) The new credentials</param>
        <param name="object_type">(Optional) The desired root object proxy type. Optional but highly recommended.</param>
        <param name="close_connected">(Optional, default false) Close existing connections</param>
        */
        [PublicApi]

        public void UpdateServiceURL(string[] url, string username = null, Dictionary<string, object> credentials = null, string object_type = null, bool close_connected = false)
        {
            if (!active)
            {
                return;
            }

            if (!use_service_url)
            {
                throw new InvalidOperationException("Subscription not using service url");
            }

            NodeID service_nodeid;
            string service_nodename;
            string service_name;

            var url_res = TransportUtil.ParseConnectionUrl(url[0]);
            service_nodeid = url_res.nodeid;
            service_nodename = url_res.nodename;
            service_name = url_res.service;

            for (int i = 1; i < url.Length; i++)
            {
                var url_res1 = TransportUtil.ParseConnectionUrl(url[i]);
                if (url_res1.nodeid != url_res.nodeid || url_res1.nodename != url_res.nodename || url_res1.service != url_res.service)
                {
                    throw new ArgumentException("Provided URLs do not point to the same service in SubscribeService");
                }
            }


            lock (this)
            {
                service_url = url;
                service_url_username = username;
                service_url_credentials = credentials;
            }

            foreach (var c in clients.Values)
            {
                c.nodeid = service_nodeid;
                c.nodename = service_nodename;
                c.service_name = service_name;
                c.service_type = object_type;
                c.urls = url;
                c.last_node_update = DateTime.UtcNow;

                c.username = username;
                c.credentials = credentials;

                if (!close_connected)
                {
                    continue;
                }

                if (c.claimed)
                {
                    continue;
                }

                if (c.client != null)
                {
                    Task.Run(() => node.DisconnectService(c.client).IgnoreResult());
                }
            }
        }
        /// <summary>
        /// Update the service type and filter for the subscription
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="service_types">n arrayof service types to listen for, ie
        /// `com.robotraconteur.robotics.robot.Robot`</param>
        /// <param name="filter">A filter to select individual services based on specified criteria</param>
        [PublicApi]
        public void UpdateServiceByType(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            if (!active)
            {
                return;
            }

            if (use_service_url)
            {
                throw new InvalidOperationException("Subscription not using service by type");
            }

            if (!(service_types?.Length > 0))
            {
                throw new ArgumentException("service_types must not be empty");
            }

            lock (this)
            {
                this.service_types = service_types;
                this.filter = filter;

                foreach (var c in clients.Values)
                {
                    try
                    {
                        ServiceInfo2 info = new ServiceInfo2();
                        info.NodeID = c.nodeid;
                        info.NodeName = c.nodename;
                        info.Name = c.service_name;
                        info.RootObjectType = c.service_type;
                        info.ConnectionURL = c.urls;
                        info.Attributes = node.GetServiceAttributes(c);

                        c.erase = true;


                        Discovery_nodestorage node_storage = new Discovery_nodestorage();

                        bool connect = SubscriptionFilterUtil.FilterService(service_types, this.filter, node_storage, info, out var filter_res_urls, out var filter_res, out var filter_node);

                        if (!connect)
                        {
                            try
                            {
                                _ = Task.Run(delegate ()
                                {
                                    try
                                    {
                                        node.DisconnectService(c).IgnoreResult();
                                    }
                                    catch { }
                                });
                            }
                            catch { }

                        }

                    }
                    catch (Exception exp)
                    {
                        LogDebug(string.Format("Error updating service by type {0}", exp), node, RobotRaconteur_LogComponent.Subscription);
                    }

                    _ = Task.Run(async delegate ()
                    {
                        await Task.Delay(250).ConfigureAwait(false);
                        var cancel = new CancellationTokenSource();
                        parent.DoUpdateAllDetectedServices(this);
                    }).IgnoreResult();
                }
            }


        }

        private ServiceSubscription_client FindClient(object client)
        {
            var c = ((ServiceStub)client).RRContext;
            var target_nodeid = c.RemoteNodeID;
            var target_servicename = c.ServiceName;
            var target_subid = new ServiceSubscriptionClientID(target_nodeid, target_servicename);
            lock (this)
            {
                if (clients.TryGetValue(target_subid, out var e))
                {
                    return e;
                }

                foreach (var ee in clients)
                {
                    if (ReferenceEquals(ee.Value.client, client))
                    {
                        return ee.Value;
                    }
                }
            }

            return null;
        }
#pragma warning disable 1591
        public ServiceSubscription(Discovery parent)
        {
            this.parent = parent;
            active = true;
            this.node = parent.node;
        }
#pragma warning restore 1591
        /**
        <summary>
        Creates a wire subscription
        </summary>
        <remarks>
        <para>
        Wire subscriptions aggregate the value received from the connected services. It can also act as a
        "reverse broadcaster" to send values to clients. See WireSubscription.
        </para>
        <para>
        The optional service path may be null to use the root object in the service. The first level of the
        service path may be "*" to match any service name. For instance, the service path "*.sub_obj" will match
        any service name, and use the "sub_obj" objref.
        </para>
        </remarks>
        <param name="wire_name">The member name of the wire</param>
        <param name="service_path">The service path of the object owning the wire member.
        Leave as null for root object</param>
        <typeparam name="T">The type of the wire value. This must be specified since the subscription doesn't
        know the wire value type</typeparam>
        <returns>The wire subscription</returns>
        */
        [PublicApi]

        public WireSubscription<T> SubscribeWire<T>(string wire_name, string service_path = null)
        {
            var o = new WireSubscription<T>(this, wire_name, service_path);
            lock (this)
            {
                if (wire_subscriptions.FirstOrDefault(x => x.membername == wire_name && x.servicepath == service_path) != null)
                {
                    throw new InvalidOperationException("Already subscribed to wire member: " + wire_name);
                }


                wire_subscriptions.Add(o);

                foreach (var c in clients.Values)
                {
                    if (c.client != null)
                    {
                        o.ClientConnected(new ServiceSubscriptionClientID(c.nodeid, c.service_name), c.client);
                    }
                }
            }
            return o;
        }
        /**
        <summary>
        Creates a pipe subscription
        </summary>
        <remarks>
        <para>
        Pipe subscriptions aggregate the packets received from the connected services. It can also act as a
        "reverse broadcaster" to send packets to clients. See PipeSubscription.
        </para>
        <para>
        The optional service path may be null to use the root object in the service. The first level of the
        service path may be "*" to match any service name. For instance, the service path "*.sub_obj" will match
        any service name, and use the "sub_obj" objref.
        </para>
        </remarks>
        <param name="pipe_name">The member name of the pipe</param>
        <param name="service_path">The service path of the object owning the pipe member.
        Leave as null for root object</param>
        <param name="max_backlog">The maximum number of packets to store in receive queue</param>
        <typeparam name="T">The type of the pipe packets. This must be specified since the subscription does not
        know the pipe packet type</typeparam>
        <returns>The pipe subscription</returns>
        */
        [PublicApi]

        public PipeSubscription<T> SubscribePipe<T>(string pipe_name, string service_path = null, int max_backlog = -1)
        {
            var o = new PipeSubscription<T>(this, pipe_name, service_path, max_backlog);
            lock (this)
            {
                if (pipe_subscriptions.FirstOrDefault(x => x.membername == pipe_name && x.servicepath == service_path) != null)
                {
                    throw new InvalidOperationException("Already subscribed to pipe member: " + pipe_name);
                }


                pipe_subscriptions.Add(o);

                foreach (var c in clients.Values)
                {
                    if (c.client != null)
                    {
                        o.ClientConnected(new ServiceSubscriptionClientID(c.nodeid, c.service_name), c.client);
                    }
                }
            }
            return o;
        }

        internal void WireSubscriptionClosed(WireSubscriptionBase s)
        {
            lock (this)
            {
                wire_subscriptions.Remove(s);
            }
        }

        internal void PipeSubscriptionClosed(PipeSubscriptionBase s)
        {
            lock (this)
            {
                pipe_subscriptions.Remove(s);
            }
        }
        /**
         * <summary>Creates a sub object subscription</summary>
         * <remarks>
         * <para>
         * Sub objects are objects within a service that are not the root object. Sub objects are typically
         * referenced using objref members, however they can also be referenced using a service path.
         * The SubObjectSubscription class is used to automatically access sub objects of the default client.
         * </para>
         * <para>
         * The service path is broken up into segments using periods. See the Robot Raconter
         * documentation for more information. <!-- The BuildServicePath() function can be used to assist
         * building service paths.--> The first level of the* service path may be "*" to match any service name.
         * For instance, the service path "*.sub_obj" will match any service name, and use the "sub_obj" objref
         * </para>
         * </remarks>
         * <param name="service_path">The service path of the sub object</param>
         * <param name="object_type"> Optional object type to use for the sub object</param>
         * <return>The sub object subscription</return>
         */
        [PublicApi]
        public SubObjectSubscription SubscribeSubObject(string service_path, string object_type = null)
        {
            var o = new SubObjectSubscription(this, service_path, object_type);
            return o;
        }
    }


    internal class WireSubscription_connection
    {
        internal WireSubscriptionBase parent;
        internal object connection;
        internal object client;
        internal bool closed;
        internal CancellationTokenSource cancel;
    }

    /// <summary>
    /// Base class for WireSubscription
    /// </summary>
    [PublicApi]
    public abstract class WireSubscriptionBase
    {
#pragma warning disable 1591
        protected internal RobotRaconteurNode node;
        protected internal ServiceSubscription parent;
        protected internal object in_value;
        protected internal TimeSpec in_value_time;
        protected internal DateTime in_value_time_local;
        protected internal bool in_value_valid;
        protected internal object in_value_connection;

        protected internal AsyncValueWaiter<object> in_value_waiter = new AsyncValueWaiter<object>();

        protected internal string membername;
        protected internal string servicepath;

        protected internal CancellationTokenSource cancel = new CancellationTokenSource();

        internal Dictionary<ServiceSubscriptionClientID, WireSubscription_connection> connections = new Dictionary<ServiceSubscriptionClientID, WireSubscription_connection>();
#pragma warning restore 1591
        /**
        <summary>
        Closes the wire subscription
        </summary>
        <remarks>
        Wire subscriptions are automatically closed when the parent ServiceSubscription is closed
        or when the node is shut down.
        </remarks>
        */
        [PublicApi]

        public void Close()
        {
            this.cancel.Cancel();
            parent.WireSubscriptionClosed(this);
        }
#pragma warning disable 1591
        public object GetInValueBase(out TimeSpec time, out object wire_connection)
        {
            if (!TryGetInValueBase(out var in_value, out time, out wire_connection))
            {
                throw new ValueNotSetException("In value not valid");
            }

            return in_value;
        }

        public bool TryGetInValueBase(out object value, out TimeSpec time, out object wire_connection)
        {
            lock (this)
            {
                if (!in_value_valid)
                {
                    value = default;
                    time = default;
                    wire_connection = default;
                    return false;
                }

                if (InValueLifespan >= 0)
                {
                    if (in_value_time_local + TimeSpan.FromMilliseconds(InValueLifespan) < DateTime.UtcNow)
                    {
                        value = default;
                        time = default;
                        wire_connection = default;
                        return false;
                    }
                }

                value = in_value;
                time = in_value_time;
                wire_connection = in_value_connection;

                return true;
            }


        }

        protected internal bool closed;
#pragma warning restore 1591

        /**
         * <summary>Wait for a valid InValue to be received from a client</summary>
         *
         * Awaitable task until value is received or timeout
         *
         * <param name="timeout">The timeout in milliseconds</param>
         * <param name="cancel">A cancellation token for the operation</param>
         * <return>true if a value was received within the specified timeout</return>
         */
        [PublicApi]
        public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken cancel = default)
        {
            AsyncValueWaiter<object>.AsyncValueWaiterTask waiter = null;
            lock (this)
            {
                if (in_value_valid)
                {
                    return true;
                }

                if (closed)
                {
                    return false;
                }

                if (timeout == 0)
                    return in_value_valid;
                waiter = in_value_waiter.CreateWaiterTask(timeout, cancel);

            }
            using (waiter)
            {
                await waiter.Task.ConfigureAwait(false); ;
                return (waiter.TaskCompleted);
            }
        }
        /**
        <summary>
        Get the number of wire connections currently connected
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public uint ActiveWireConnectionCount
        {
            get
            {
                lock (this)
                {
                    return (uint)connections.Count(x => x.Value.connection != null);
                }
            }
        }
        /**
        <summary>
        Get or Set if InValue is ignored
        </summary>
        <remarks />
        <value />
        */
        [PublicApi]
        public bool IgnoreInValue { get; set; }
        /**
        <summary>
        Get or Set the InValue lifespan in milliseconds
        </summary>
        <remarks>
        Get the lifespan of InValue in milliseconds. The value will expire after the specified
        lifespan, becoming invalid. Use -1 for infinite lifespan.
        </remarks>
        */
        [PublicApi]

        public int InValueLifespan { get; set; } = -1;

        internal WireSubscriptionBase(ServiceSubscription parent, string membernname, string servicepath)
        {
            this.parent = parent;
            this.node = parent.node;
            this.membername = membernname;
            this.servicepath = servicepath;
        }

        internal void ClientConnected(ServiceSubscriptionClientID id, object client)
        {
            RunConnection(id, client).IgnoreResult();
        }

        internal abstract Task RunConnection(ServiceSubscriptionClientID id, object client);

        internal void ClientDisconnected(ServiceSubscriptionClientID id, object client)
        {
            lock (this)
            {
                if (connections.TryGetValue(id, out var conn))
                {
                    conn.cancel?.Cancel();
                }
            }
        }

    }
    /**
    <summary>
    Subscription for wire members that aggregates the values from client wire connections
    </summary>
    <remarks>
    <para>
    Wire subscriptions are created using the ServiceSubscription.SubscribeWire() function. This function takes the
    type of the wire value, the name of the wire member, and an optional service path of the service
    object that owns the wire member.
    </para>
    <para>
    Wire subscriptions aggregate the InValue from all active wire connections. When a client connects,
    the wire subscriptions will automatically create wire connections to the wire member specified
    when the WireSubscription was created using ServiceSubscription::SubscribeWire(). The InValue of
    all the active wire connections are collected, and the most recent one is used as the current InValue
    of the wire subscription. The current value, the timespec, and the wire connection can be accessed
    using GetInValue() or TryGetInValue().
    </para>
    <para>
    The lifespan of the InValue can be configured using SetInValueLifespan(). It is recommended that
    the lifespan be configured, so that the value will expire if the subscription stops receiving
    fresh in values.
    </para>
    <para>
    The wire subscription can also be used to set the OutValue of all active wire connections. This behaves
    similar to a "reverse broadcaster", sending the same value to all connected services.
    </para>
    </remarks>
    <typeparam name="T">The value type used by the wire</typeparam>
    */
    [PublicApi]

    public class WireSubscription<T> : WireSubscriptionBase
    {
#pragma warning disable 1591
        public WireSubscription(ServiceSubscription parent, string membernname, string servicepath)
            : base(parent, membernname, servicepath)
        {
        }
#pragma warning restore 1591

        internal override async Task RunConnection(ServiceSubscriptionClientID id, object client)
        {
            var c = new WireSubscription_connection()
            {
                parent = this,
                client = client,
                closed = false,
                cancel = new CancellationTokenSource()
            };

            this.cancel.Token.Register(() => { c.cancel.Cancel(); });

            lock (this)
            {
                connections.Add(id, c);
            }
            try
            {
                var wait_task = new TaskCompletionSource<bool>();
                while (!c.cancel.IsCancellationRequested && !wait_task.Task.IsCompleted)
                {
                    try
                    {
                        object obj = client;
                        if (!string.IsNullOrEmpty(servicepath) && servicepath != "*")
                        {
                            if (servicepath.StartsWith("*."))
                            {
                                servicepath = servicepath.ReplaceFirst("*", ((ServiceStub)client).RRContext.ServiceName);
                            }
                            obj = await ((ServiceStub)client).RRContext.FindObjRef(servicepath, null, c.cancel.Token).ConfigureAwait(false);
                        }

                        var property_info = obj.GetType().GetProperty(this.membername);
                        if (property_info == null)
                        {
                            await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false);
                            continue;
                        }

                        Wire<T> w = property_info.GetValue(obj) as Wire<T>;
                        if (w == null)
                        {
                            await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false); ;
                        }

                        Wire<T>.WireConnection cc = await w.Connect().ConfigureAwait(false);
                        if (IgnoreInValue)
                        {
                            // TODO: ignore in value
                        }

                        c.connection = cc;

                        wait_task.AttachCancellationToken(c.cancel.Token);

                        Wire<T>.WireValueChangedFunction wire_changed_ev = delegate (Wire<T>.WireConnection ev_c, T ev_v, TimeSpec ev_t)
                        {
                            lock (this)
                            {
                                if (IgnoreInValue)
                                {
                                    return;
                                }

                                in_value = ev_v;
                                in_value_time = ev_t;
                                in_value_connection = ev_c;
                                in_value_valid = true;
                                in_value_time_local = DateTime.UtcNow;
                                in_value_waiter.NotifyAll(ev_v);

                                WireValueChanged?.Invoke(this, ev_v, ev_t);

                            }
                        };
                        Wire<T>.WireDisconnectCallbackFunction wire_closed_ev = delegate (Wire<T>.WireConnection ev_c)
                        {
                            wait_task.SetResult(true);
                        };

                        cc.WireCloseCallback = wire_closed_ev;
                        cc.WireValueChanged += wire_changed_ev;

                        try
                        {
                            await wait_task.Task.ConfigureAwait(false);
                        }
                        finally
                        {
                            cc.WireCloseCallback = null;
                            cc.WireValueChanged -= wire_changed_ev;
                            c.connection = null;
                            try
                            {
                                _ = cc.Close().IgnoreResult();
                            }
                            catch { }
                        }


                    }
                    catch (Exception ex)
                    {
                        LogDebug(string.Format("WireSubscription RunClient exception {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                    }
                    try
                    {
                        await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false);
                    }
                    catch { }
                }

            }
            finally
            {
                lock (this)
                {
                    connections.Remove(id);
                }
            }
        }
        /**
        <summary>
        Get the current InValue
        </summary>
        <remarks>
        Throws ValueNotSetException if no valid value is available
        </remarks>
        */
        [PublicApi]

        public T InValue
        {
            get
            {
                lock (this)
                {
                    if (!in_value_valid)
                    {
                        throw new ValueNotSetException("InValue is not valid");
                    }
                    if (Wire<T>.WireConnection.IsValueExpired(this.in_value_time_local, this.InValueLifespan))
                    {
                        throw new ValueNotSetException("InValue is expired");
                    }
                    return (T)in_value;
                }
            }
        }

        /**
        <summary>
        Get the current InValue and metadata
        </summary>
        <remarks>
        Throws ValueNotSetException if no valid value is available
        </remarks>
        <param name="ts">[out] the LastValueReceivedTime of the InValue</param>
        <param name="connection">[out] the WireConnection of the InValue</param>
        <returns>The current InValue</returns>
        */
        [PublicApi]

        public T GetInValue(out TimeSpec ts, out Wire<T>.WireConnection connection)
        {
            lock (this)
            {
                if (!in_value_valid)
                {
                    throw new ValueNotSetException("InValue is not valid");
                }
                if (Wire<T>.WireConnection.IsValueExpired(this.in_value_time_local, this.InValueLifespan))
                {
                    throw new ValueNotSetException("InValue is expired");
                }
                ts = in_value_time;
                connection = (Wire<T>.WireConnection)in_value_connection;
                return (T)in_value;
            }
        }
        /**
<summary>
Try getting the current InValue and metadata
</summary>
<remarks>
Same as GetInValue(), but returns a bool for success or failure instead of throwing
an exception.
</remarks>
<param name="val">[out] the current InValue</param>
<param name="ts">[out] the LastValueReceivedTime of the InValue</param>
<param name="connection">[out] the WireConnection of the InValue</param>
<returns>true if value is valid, otherwise false</returns>
*/
        [PublicApi]

        public bool TryGetInValue(out T val, out TimeSpec ts, out Wire<T>.WireConnection connection)
        {
            lock (this)
            {
                if (!in_value_valid || Wire<T>.WireConnection.IsValueExpired(this.in_value_time_local, this.InValueLifespan))
                {
                    val = default;
                    ts = default;
                    connection = default;
                    return false;
                }
                ts = in_value_time;
                connection = (Wire<T>.WireConnection)in_value_connection;
                val = (T)in_value;
                return true;
            }
        }

        /**
        <summary>
        Try getting the current InValue
        </summary>
        <remarks>
        Same as InValue, but returns a bool for success or failure instead of throwing
        an exception.
        </remarks>
        <param name="val">[out] the current InValue</param>
        <returns>true if value is valid, otherwise false</returns>
        */
        [PublicApi]

        public bool TryGetInValue(out T val)
        {
            lock (this)
            {
                if (!in_value_valid || Wire<T>.WireConnection.IsValueExpired(this.in_value_time_local, this.InValueLifespan))
                {
                    val = default;
                    return false;
                }
                val = (T)in_value;
                return true;
            }
        }
        /**
        <summary>
        Set the OutValue for all active wire connections
        </summary>
        <remarks>
        Behaves like a "reverse broadcaster". Calls WireConnection.SetOutValue()
        for all connected wire connections.
        </remarks>
        <param name="value">The new OutValue</param>
        */
        [PublicApi]

        public void SetOutValueAll(T value)
        {
            lock (this)
            {
                foreach (var c in connections.Values)
                {
                    try
                    {
                        var cc = (c.connection as Wire<T>.WireConnection);
                        if (cc != null)
                        {
                            cc.OutValue = value;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogDebug(string.Format("WireSubscription SetOutValueAll exception {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                    }
                }
            }
        }
        /// <summary>
        /// Event for wire value changed
        /// </summary>
        [PublicApi]
        public event Action<WireSubscription<T>, T, TimeSpec> WireValueChanged;
    }

    internal class PipeSubscription_connection
    {
        internal PipeSubscriptionBase parent;
        internal object endpoint;
        internal object client;
        internal bool closed;
        internal CancellationTokenSource cancel;
        internal uint active_send_count;
        internal List<uint> active_sends = new List<uint>();
        internal List<int> backlog = new List<int>();
        internal List<int> forward_backlog = new List<int>();

    }

    /// <summary>
    /// Base class for PipeSubscription
    /// </summary>
    [PublicApi]
    public abstract class PipeSubscriptionBase
    {
        /**
        <summary>
        Closes the pipe subscription
        </summary>
        <remarks>
        Pipe subscriptions are automatically closed when the parent ServiceSubscription is closed
        or when the node is shut down.
        </remarks>
        */
        [PublicApi]

        public void Close()
        {
            this.cancel.Cancel();
            parent.PipeSubscriptionClosed(this);
        }
#pragma warning disable 1591
        internal protected object ReceivePacketBase()
        {
            if (!TryReceivedPacketBase(out var packet))
            {
                throw new InvalidOperationException("PipeSubscription Receive Queue Empty");
            }
            return packet;
        }

        internal protected bool TryReceivedPacketBase(out object packet)
        {
            lock (this)
            {
                if (recv_packets.Count > 0)
                {
                    var q = recv_packets.Dequeue();
                    packet = q.Item1;
                    return true;
                }
                else
                {
                    packet = null;
                    return false;
                }
            }
        }

        internal protected async Task<Tuple<bool, object, object>> TryReceivedPacketWaitBase(int timeout = -1, bool peek = false)
        {
            lock (this)
            {
                if (recv_packets.Count > 0)
                {
                    Tuple<object, object> q;
                    if (!peek)
                    {
                        q = recv_packets.Dequeue();
                    }
                    else
                    {
                        q = recv_packets.Peek();
                    }
                    return Tuple.Create(true, q.Item1, q.Item2);
                }

                if (timeout == 0 || closed)
                {
                    return Tuple.Create(false, (object)null, (object)null);
                }
            }

            AsyncValueWaiter<bool>.AsyncValueWaiterTask waiter = null;
            waiter = recv_packets_waiter.CreateWaiterTask(timeout, cancel.Token);
            using (waiter)
            {
                await waiter.Task.ConfigureAwait(false);
            }

            lock (this)
            {
                if (recv_packets.Count > 0)
                {
                    var q = recv_packets.Dequeue();
                    return Tuple.Create(true, q.Item1, q.Item2);
                }
                else
                {
                    return Tuple.Create(false, (object)null, (object)null);
                }
            }

        }
#pragma warning restore 1591
        /**
        <summary>
        Get the number of packets available to receive
        </summary>
        <remarks>
        Use ReceivePacket(), TryReceivePacket(), or TryReceivePacketWait() to receive the packet
        </remarks>
        */
        [PublicApi]

        public uint Available
        {
            get
            {
                lock (this)
                {
                    return (uint)recv_packets.Count;
                }
            }
        }
        /**
        <summary>
        Get the number of pipe endpoints currently connected
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public uint ActivePipeEndpointCount
        {
            get
            {
                lock (this)
                {
                    return (uint)connections.Count(x => x.Value.endpoint != null);
                }
            }
        }
        /**
        <summary>
        Get or set if incoming packets are ignored
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public bool IgnoreReceived { get; set; }
#pragma warning disable 1591
        internal protected PipeSubscriptionBase(ServiceSubscription parent, string membername, string servicepath = "", int max_recv_packets = -1, int max_send_backlog = 5)
        {
            this.parent = parent;
            this.node = parent.node;
            this.membername = membername;
            this.servicepath = servicepath;
            this.max_recv_packets = max_recv_packets;
            this.max_send_backlog = max_send_backlog;
        }

        internal void ClientConnected(ServiceSubscriptionClientID id, object client)
        {
            RunConnection(id, client).IgnoreResult();
        }

        internal abstract Task RunConnection(ServiceSubscriptionClientID id, object client);

        internal void ClientDisconnected(ServiceSubscriptionClientID id, object client)
        {
            lock (this)
            {
                if (connections.TryGetValue(id, out var conn))
                {
                    conn.cancel?.Cancel();
                }
            }
        }

        internal Dictionary<ServiceSubscriptionClientID, PipeSubscription_connection> connections = new Dictionary<ServiceSubscriptionClientID, PipeSubscription_connection>();

        protected internal bool closed = false;

        protected internal ServiceSubscription parent;

        protected internal RobotRaconteurNode node;

        protected internal Queue<Tuple<object, object>> recv_packets = new Queue<Tuple<object, object>>();

        protected internal AsyncValueWaiter<bool> recv_packets_waiter = new AsyncValueWaiter<bool>();

        protected internal string membername;
        protected internal string servicepath;
        protected internal int max_recv_packets;
        protected internal int max_send_backlog;
        protected internal CancellationTokenSource cancel = new CancellationTokenSource();
#pragma warning restore 1591
    }

    /**
    <summary>
    Subscription for pipe members that aggregates incoming packets from client pipe endpoints
    </summary>
    <remarks>
    <para>
    Pipe subscriptions are created using the ServiceSubscription.SubscribePipe() function. This function takes the
    the type of the pipe packets, the name of the pipe member, and an optional service path of the service
    object that owns the pipe member.
    </para>
    <para>
    Pipe subscriptions collect all incoming packets from connect pipe endpoints. When a client connects,
    the pipe subscription will automatically connect a pipe endpoint the pipe endpoint specified when
    the PipeSubscription was created using ServiceSubscription.SubscribePipe(). The packets received
    from each of the collected pipes are collected and placed into a common receive queue. This queue
    is read using ReceivePacket(), TryReceivePacket(), or TryReceivePacketWait(). The number of packets
    available to receive can be checked using Available().
    </para>
    <para>
    Pipe subscriptions can also be used to send packets to all connected pipe endpoints. This is done
    with the AsyncSendPacketAll() function. This function behaves somewhat like a "reverse broadcaster",
    sending the packets to all connected services.
    </para>
    <para>
    If the pipe subscription is being used to send packets but not receive them, the SetIgnoreInValue()
    should be set to true to prevent packets from queueing.
    </para>
    </remarks>
    <typeparam name="T">The type of the pipe packets</typeparam>
    */
    [PublicApi]

    public class PipeSubscription<T> : PipeSubscriptionBase
    {
#pragma warning disable 1591
        protected internal PipeSubscription(ServiceSubscription parent, string membername, string servicepath = "", int max_recv_packets = -1, int max_send_backlog = 5)
            : base(parent, membername, servicepath, max_recv_packets, max_send_backlog)
        {
        }
#pragma warning restore 1591

        /**
        <summary>
        Dequeue a packet from the receive queue
        </summary>
        <remarks>
        If the receive queue is empty, an InvalidOperationException() is thrown
        </remarks>
        <returns>The dequeued packet</returns>
        */
        [PublicApi]

        public T ReceivePacket()
        {
            return (T)ReceivePacketBase();
        }

        /**
        <summary>
        Try dequeuing a packet from the receive queue
        </summary>
        <remarks>
        Same as ReceivePacket(), but returns a bool for success or failure instead of throwing
        an exception
        </remarks>
        <param name="packet">[out] the dequeued packet</param>
        <returns>true if packet dequeued successfully, otherwise false if queue is empty</returns>
        */
        [PublicApi]

        public bool TryReceivePacket(out T packet)
        {
            if (!TryReceivedPacketBase(out object packet1))
            {
                packet = default;
                return false;
            }

            packet = (T)packet1;
            return true;
        }
        /**
        <summary>
        Try dequeuing a packet from the receive queue, optionally waiting or peeking the packet
        </summary>
        <remarks>None</remarks>
        <param name="timeout">The time to wait for a packet to be received in milliseconds if the queue is empty, or
        RR_TIMEOUT_INFINITE to wait forever</param>
        <param name="peek">If true, the packet is returned, but not dequeued. If false, the packet is dequeued</param>
        <returns>Returns success, the packet value, and the pipe connection</returns>
        */
        [PublicApi]

        public async Task<Tuple<bool, T, Pipe<T>.PipeEndpoint>> TryReceivePacketWait(int timeout = -1, bool peek = false)
        {
            var r = await TryReceivedPacketWaitBase(timeout, peek).ConfigureAwait(false);
            if (!r.Item1)
            {
                return Tuple.Create(false, default(T), default(Pipe<T>.PipeEndpoint));
            }

            return Tuple.Create(true, (T)r.Item2, (Pipe<T>.PipeEndpoint)r.Item3);
        }
        /**
        <summary>
        Sends a packet to all connected pipe endpoints
        </summary>
        <remarks>
        Calls AsyncSendPacket() on all connected pipe endpoints with the specified value.
        Returns immediately, not waiting for transmission to complete.
        </remarks>
        <param name="packet">The packet to send</param>
        */
        [PublicApi]

        public void AsyncSendPacketAll(T packet)
        {

            lock (this)
            {
                foreach (var c in connections.Values)
                {
                    if (c.active_send_count < this.max_send_backlog)
                    {
                        var ep = c.endpoint as Pipe<T>.PipeEndpoint;
                        if (ep != null)
                        {
                            ep.SendPacket(packet, cancel.Token).ContinueWith((t) =>
                            {
                                if (t.Status == TaskStatus.RanToCompletion)
                                {
                                    lock (this)
                                    {
                                        c.active_sends.Add(t.Result);
                                        c.active_send_count = (uint)c.active_sends.Count;
                                    }
                                }
                            });
                        }
                    }
                }
            }

        }

        internal override async Task RunConnection(ServiceSubscriptionClientID id, object client)
        {
            var c = new PipeSubscription_connection()
            {
                parent = this,
                client = client,
                closed = false,
                cancel = new CancellationTokenSource()
            };

            this.cancel.Token.Register(() => { c.cancel.Cancel(); });

            lock (this)
            {
                if (connections.ContainsKey(id))
                {
                    return;
                }
                connections.Add(id, c);
            }
            try
            {
                var wait_task = new TaskCompletionSource<bool>();
                while (!c.cancel.IsCancellationRequested && !wait_task.Task.IsCompleted)
                {
                    try
                    {
                        object obj = client;
                        if (!string.IsNullOrEmpty(servicepath) && servicepath != "*")
                        {
                            if (servicepath.StartsWith("*."))
                            {
                                servicepath = servicepath.ReplaceFirst("*", ((ServiceStub)client).RRContext.ServiceName);
                            }
                            obj = await ((ServiceStub)client).RRContext.FindObjRef(servicepath, null, c.cancel.Token).ConfigureAwait(false);
                        }

                        var property_info = obj.GetType().GetProperty(this.membername);
                        if (property_info == null)
                        {
                            await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false);
                            continue;
                        }

                        Pipe<T> w = property_info.GetValue(obj) as Pipe<T>;
                        if (w == null)
                        {
                            await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false); ;
                        }

                        Pipe<T>.PipeEndpoint cc = await w.Connect(-1).ConfigureAwait(false);
                        if (IgnoreReceived)
                        {

                            // TODO: ignore in value
                        }

                        c.endpoint = cc;

                        wait_task.AttachCancellationToken(c.cancel.Token);

                        Pipe<T>.PipePacketReceivedCallbackFunction pipe_changed_ev = delegate (Pipe<T>.PipeEndpoint ev_ep)
                        {
                            lock (this)
                            {
                                if (IgnoreReceived)
                                {
                                    return;
                                }

                                while (ev_ep.Available > 0)
                                {
                                    recv_packets.Enqueue(Tuple.Create<object, object>(ev_ep.ReceivePacket(), ev_ep));
                                }

                                recv_packets_waiter.NotifyAll(true);


                                PipePacketReceived?.Invoke(this);

                            }
                        };

                        Pipe<T>.PipeDisconnectCallbackFunction pipe_closed_ev = delegate (Pipe<T>.PipeEndpoint ev_ep)
                        {
                            wait_task.SetResult(true);
                        };

                        Pipe<T>.PipePacketAckReceivedCallbackFunction pipe_ack_ev = delegate (Pipe<T>.PipeEndpoint ev_ep, uint packetnum)
                        {
                            lock (this)
                            {
                                c.active_sends.Remove(packetnum);
                                c.active_send_count = (uint)c.active_sends.Count;
                            }
                        };

                        cc.PipeCloseCallback = pipe_closed_ev;
                        cc.PacketReceivedEvent += pipe_changed_ev;
                        cc.PacketAckReceivedEvent += pipe_ack_ev;

                        try
                        {
                            await wait_task.Task.ConfigureAwait(false);
                        }
                        finally
                        {
                            cc.PipeCloseCallback = null;
                            cc.PacketReceivedEvent -= pipe_changed_ev;
                            cc.PacketAckReceivedEvent -= pipe_ack_ev;
                            c.endpoint = null;
                            try
                            {
                                _ = cc.Close().IgnoreResult();
                            }
                            catch { }
                        }


                    }
                    catch (Exception ex)
                    {
                        LogDebug(string.Format("PipeSubscription RunClient exception {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                    }
                    try
                    {
                        await Task.Delay(2500, c.cancel.Token).ConfigureAwait(false);
                    }
                    catch { }
                }

            }
            finally
            {
                lock (this)
                {
                    connections.Remove(id);
                }
            }
        }
        /**
        <summary>
        Listener event for when a pipe packet is received
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]

        public event Action<PipeSubscription<T>> PipePacketReceived;
    }

    /**
     * <summary>Subscription for sub objects of the default client</summary>
     * <remarks>
     * <para>
     * SubObjectSubscription is used to access sub objects of the default client. Sub objects are objects within a service
     * that are not the root object. Sub objects are typically referenced using objref members, however they can also be
     * referenced using a service path. The SubObjectSubscription class is used to automatically access sub objects of the
     * default client.
     * </para>
     * <para>
     * Use ServiceSubscription::SubscribeSubObject() to create a SubObjectSubscription.
     * </para>
     * <para>
     * This class should not be used to access Pipe or Wire members. Use the ServiceSubscription::SubscribePipe() and
     * ServiceSubscription::SubscribeWire() functions to access Pipe and Wire members.
     * </para>
     * </remarks>
     */
    [PublicApi]
    public class SubObjectSubscription
    {

        private async Task<T> GetObjFromRoot<T>(ServiceStub client, CancellationToken cancel)
        {
            string service_path1 = servicepath;
            if (service_path1.StartsWith("*."))
            {
                service_path1 = service_path1.ReplaceFirst("*", client.RRContext.ServiceName);
            }

            return (T)await client.RRContext.FindObjRef(service_path1, objecttype, cancel);
        }

        /**
         * <summary>Get the "default client" sub object</summary>
         * <remarks>
         * The sub object is retrieved from the default client. The default client is the first client
         * that connected to the service. If no clients are currently connected, an exception is thrown.
         *
         * Clients using GetDefaultClient() should not store a reference to the client. Call GetDefaultClient()
         * each time the client is needed.
         * </remarks>
         * <typeparam name="T">The type of the sub object</typeparam>
         * <param name="cancel">The cancellation token for the operation</param>
         * <returns>The default client</returns>
         */
        [PublicApi]
        public async Task<T> GetDefaultClient<T>(CancellationToken cancel = default)
        {
            var client = (ServiceStub)parent.GetDefaultClient<object>();

            return await GetObjFromRoot<T>(client, cancel);
        }

        /**
         * <summary>Try getting the "default client" sub object</summary>
         * <remarks>
         * Same as GetDefaultClient(), but returns a bool for success or failure instead of throwing
         * an exception on failure.
         * </remarks>
         * <typeparam name="T">The type of the sub object</typeparam>
         * <param name="cancel">The cancellation token for the operation</param>
         * <returns>Success and the default client as tuple</returns>
         */
        [PublicApi]
        public async Task<Tuple<bool, T>> TryGetDefaultClient<T>(CancellationToken cancel = default)
        {
            try
            {
                T ret = await GetDefaultClient<T>(cancel);
                return Tuple.Create(true, ret);
            }
            catch (Exception ex)
            {
                LogDebug(string.Format("TryGetDefaultClient failed {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                return Tuple.Create(false, default(T));
            }
        }
        /**
         * <summary>Get the "default client" sub object and wait if not available</summary>
         * <remarks>
         * Same as GetDefaultClient() but waits for a client to be available
         * </remarks>
         * <typeparam name="T">The type of the sub object</typeparam>
         * <param name="cancel">The cancellation token for the operation</param>
         * <returns>The default client</returns>
         */
        [PublicApi]
        public async Task<T> GetDefaultClientWait<T>(CancellationToken cancel)
        {
            var client = (ServiceStub)await parent.GetDefaultClientWait<object>(cancel);

            return await GetObjFromRoot<T>(client, cancel);
        }
        /**
         * <summary>Try getting the "default client" sub object and wait if not available</summary>
         * <remarks>
         * Same as TryGetDefaultClient() but waits for a client to be available
         * </remarks>
         * <typeparam name="T">The type of the sub object</typeparam>
         * <param name="cancel">The cancellation token for the operation</param>
         * <returns>Success and the default client as tuple</returns>
         */
        [PublicApi]
        public async Task<Tuple<bool, T>> TryGetDefaultClientWait<T>(CancellationToken cancel)
        {
            try
            {
                T ret = await GetDefaultClientWait<T>(cancel);
                return Tuple.Create(true, ret);
            }
            catch (Exception ex)
            {
                LogDebug(string.Format("TryGetDefaultClientWait failed {0}", ex), node, RobotRaconteur_LogComponent.Subscription);
                return Tuple.Create(false, default(T));
            }
        }

        /// <summary>
        /// Close the sub object subscription
        /// </summary>
        [PublicApi]
        public void Close()
        {

        }

#pragma warning disable 1591
        protected internal SubObjectSubscription(ServiceSubscription parent, string servicepath, string objecttype)
        {
            this.parent = parent;
            this.node = parent.node;
            this.servicepath = servicepath;
            this.objecttype = objecttype;
        }

        protected ServiceSubscription parent;
        protected RobotRaconteurNode node;
        string servicepath;
        string objecttype;
#pragma warning restore 1591
    }
    /**
     * <summary>Connection method for ServiceSubscriptionManager subscription</summary>
     * <remarks>
     * Select between using URLs or service types for subscription
     * </remarks>
     */
    [PublicApi]
    public enum ServiceSubscriptionManagerConnectionMethod
    {
        /** <summary>Implicitly select between URL and service types</summary> */
        [PublicApi]
        Default = 0,
        /** <summary>Use URLs types for subscription</summary> */
        [PublicApi]
        Url,
        /** <summary>Use service types for subscription</summary> */
        [PublicApi]
        Type
    }

    /**
     * <summary>ServiceSubscriptionManager subscription connection information</summary>
     * <remarks>
     * Contains the connection information for a ServiceSubscriptionManager subscription
     * and the local name of the subscription
     * </remarks>
     */
    [PublicApi]
    public class ServiceSubscriptionManagerDetails
    {
        /** <summary>The local name of the subscription</summary> */
        [PublicApi]
        public string Name;
        /** <summary>The connection method to use, URL or service type</summary> */
        [PublicApi]
        public ServiceSubscriptionManagerConnectionMethod ConnectionMethod;
        /** <summary>The URLs to use for subscription</summary> */
        [PublicApi]
        public string[] Urls;
        /** <summary>The username to use for URLs (optional)</summary> */
        [PublicApi]
        public string UrlUsername;
        /** <summary>The credentials to use for URLs (optional)</summary> */
        [PublicApi]
        public Dictionary<string, object> UrlCredentials;
        /** <summary>The service types to use for subscription</summary> */
        [PublicApi]
        public string[] ServiceTypes;
        /** <summary>The filter to use for subscription when service type is used (optional)</summary> */
        [PublicApi]
        public ServiceSubscriptionFilter Filter;
        /** <summary>If the subscription is enabled</summary> */
        [PublicApi]
        public bool Enabled = true;
    }

    /**
     * <summary>Class to manage multiple subscriptions to services</summary>
     * <remarks>
     * ServiceSubscriptionManager is used to manage multiple subscriptions to services. Subscriptions
     * are created using information contained in ServiceSubscriptionManagerDetails structures. The subscriptions
     * can connect using URLs or service types. The subscriptions can be enabled or disabled, and can be
     * closed.
     * </remarks>
     */
    [PublicApi]
    public class ServiceSubscriptionManager
    {
#pragma warning disable 1591
        protected internal RobotRaconteurNode node;
        internal Dictionary<string, ServiceSubscriptionManager_subscription> subscriptions = new Dictionary<string, ServiceSubscriptionManager_subscription>();
#pragma warning restore 1591
        /**
         * <summary>Construct a new ServiceSubscriptionManager object</summary>
         *
         * <param name="node">The node to use for the subscription manager. Defaults to RobotRaconteurNode.s</param>
         */
        [PublicApi]
        public ServiceSubscriptionManager(RobotRaconteurNode node = null)
        {
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
            else
            {
                this.node = node;
            }
        }

        internal ServiceSubscription CreateSubscription(ServiceSubscriptionManagerDetails details)
        {
            switch (details.ConnectionMethod)
            {
                case ServiceSubscriptionManagerConnectionMethod.Default:
                case ServiceSubscriptionManagerConnectionMethod.Url:
                    break;
                case ServiceSubscriptionManagerConnectionMethod.Type:
                    {
                        if (details.ServiceTypes == null || details.ServiceTypes.Length == 0)
                        {
                            throw new ArgumentException("ServiceTypes must be specified for ServiceSubscriptionManager connection method type");
                        }
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid ServiceSubscriptionManagerConnectionMethod");
            }

            var d = node.m_Discovery;

            ServiceSubscription sub;

            if ((!(details.Urls?.Length > 0) && !(details.ServiceTypes?.Length > 0)) || !details.Enabled)
            {
                sub = new ServiceSubscription(d);

                switch (details.ConnectionMethod)
                {
                    case ServiceSubscriptionManagerConnectionMethod.Default:
                        {
                            if (details.Urls?.Length > 0)
                            {
                                sub.use_service_url = true;
                            }
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Url:
                        {
                            sub.use_service_url = true;
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                switch (details.ConnectionMethod)
                {
                    case ServiceSubscriptionManagerConnectionMethod.Default:
                        {
                            if (details.Urls?.Length > 0)
                            {
                                sub = d.SubscribeService(details.Urls, details.UrlUsername, details.UrlCredentials);
                            }
                            else
                            {
                                sub = d.SubscribeServiceByType(details.ServiceTypes, details.Filter);
                            }
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Type:
                        {
                            sub = d.SubscribeServiceByType(details.ServiceTypes, details.Filter);
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Url:
                        {
                            sub = d.SubscribeService(details.Urls, details.UrlUsername, details.UrlCredentials);
                            break;
                        }
                    default:
                        throw new ArgumentException("Invalid ServiceSubscriptionManagerConnectionMethod");
                }
            }

            return sub;
        }

        internal void UpdateSubscription(ServiceSubscriptionManager_subscription sub, ServiceSubscriptionManagerDetails details, bool close)
        {
            // CALL LOCKED!

            if (string.IsNullOrEmpty(details.Name))
            {
                throw new ArgumentException("Name must be specified for ServiceSubscriptionManagerDetails");
            }

            switch (details.ConnectionMethod)
            {
                case ServiceSubscriptionManagerConnectionMethod.Default:
                case ServiceSubscriptionManagerConnectionMethod.Url:
                    break;
                case ServiceSubscriptionManagerConnectionMethod.Type:
                    {
                        if (!(details.ServiceTypes?.Length > 0))
                        {
                            throw new ArgumentException("ServiceTypes must be specified for ServiceSubscriptionManager connection method type");
                        }
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid ServiceSubscriptionManagerConnectionMethod");
            }

            Discovery d = node.m_Discovery;

            var old_details = sub.details;
            sub.details = details;

            if (sub.details.Enabled && sub.details.ConnectionMethod == ServiceSubscriptionManagerConnectionMethod.Url && !(sub.details.Urls?.Length > 0))
            {
                sub.details.Enabled = false;
            }

            if (sub.details.Enabled &&
                 ((sub.details.ConnectionMethod == ServiceSubscriptionManagerConnectionMethod.Default) &&
                 (!(sub.details.Urls?.Length > 0) && !(sub.details.ServiceTypes?.Length > 0))))
            {
                sub.details.Enabled = false;
            }

            bool sub_running = false;
            lock (this)
            {
                sub_running = !(sub.sub.use_service_url) || (sub.sub.use_service_url && (sub.sub.service_url?.Length > 0));
            }

            if (sub_running && !sub.details.Enabled)
            {
                if (close)
                {
                    sub.sub.SoftClose();
                }
                return;
            }

            if (((old_details.ConnectionMethod != sub.details.ConnectionMethod) || (old_details.ConnectionMethod == ServiceSubscriptionManagerConnectionMethod.Default
            || sub.details.ConnectionMethod == ServiceSubscriptionManagerConnectionMethod.Default)) || !sub_running)
            {
                if (sub_running)
                {
                    sub.sub.SoftClose();
                }

                switch (sub.details.ConnectionMethod)
                {
                    case ServiceSubscriptionManagerConnectionMethod.Default:
                        {
                            if (sub.details.Urls?.Length > 0)
                            {
                                sub.sub.InitServiceURL(sub.details.Urls, sub.details.UrlUsername, sub.details.UrlCredentials, null);
                            }
                            else
                            {
                                sub.sub.Init(sub.details.ServiceTypes, sub.details.Filter);
                            }
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Url:
                        {
                            sub.sub.InitServiceURL(sub.details.Urls, sub.details.UrlUsername, sub.details.UrlCredentials, null);
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Type:
                        {
                            sub.sub.Init(sub.details.ServiceTypes, sub.details.Filter);
                            break;
                        }
                    default:
                        throw new ArgumentException("Invalid ServiceSubscriptionManagerConnectionMethod");
                }
            }
            else
            {
                switch (sub.details.ConnectionMethod)
                {
                    case ServiceSubscriptionManagerConnectionMethod.Default:
                        {
                            if (sub.details.Urls?.Length > 0)
                            {
                                sub.sub.UpdateServiceURL(sub.details.Urls, sub.details.UrlUsername, sub.details.UrlCredentials, null);
                            }
                            else
                            {
                                sub.sub.UpdateServiceByType(sub.details.ServiceTypes, sub.details.Filter);
                            }
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Url:
                        {
                            sub.sub.UpdateServiceURL(sub.details.Urls, sub.details.UrlUsername, sub.details.UrlCredentials, null);
                            break;
                        }
                    case ServiceSubscriptionManagerConnectionMethod.Type:
                        {
                            sub.sub.UpdateServiceByType(sub.details.ServiceTypes, sub.details.Filter);
                            break;
                        }
                    default:
                        throw new ArgumentException("Invalid ServiceSubscriptionManagerConnectionMethod");
                }
            }

            Task.Run(() => d.DoUpdateAllDetectedServices(sub.sub)).IgnoreResult();
        }

        /**
         * <summary>Initialize the subscription manager with a list of subscriptions</summary>
         *
         * <param name="details">The list of subscriptions to initialize</param>
         */
        [PublicApi]
        public void Init(ServiceSubscriptionManagerDetails[] details)
        {
            lock (this)
            {
                foreach (var e in details)
                {
                    var s = new ServiceSubscriptionManager_subscription()
                    {
                        details = e,
                        sub = CreateSubscription(e)
                    };
                    subscriptions.Add(e.Name, s);
                }

            }
        }

        /**
         * <summary>Add a subscription to the manager</summary>
         *
         * <param name="details">The subscription to add</param>
         */
        [PublicApi]
        public void AddSubscription(ServiceSubscriptionManagerDetails details)
        {
            lock (this)
            {
                if (subscriptions.ContainsKey(details.Name))
                {
                    throw new ArgumentException("Subscription already exists");
                }

                var s = new ServiceSubscriptionManager_subscription()
                {
                    details = details,
                    sub = CreateSubscription(details)
                };
                subscriptions.Add(details.Name, s);
            }
        }

        /**
         * <summary>Remove a subscription from the manager</summary>
         *
         * <param name="name">The local name of the subscription to remove</param>
         * <param name="close">If true, close the subscription. Default true</param>
         */
        [PublicApi]
        public void RemoveSubscription(string name, bool close = true)
        {
            lock (this)
            {
                if (!subscriptions.TryGetValue(name, out var s))
                {
                    throw new ArgumentException("Subscription does not exist");
                }

                subscriptions.Remove(name);
                if (close && s.sub != null)
                {
                    try
                    {
                        s.sub.Close();
                    }
                    catch
                    {
                        LogDebug("ServiceSubscriptionManager RemoveSubscription close failed", node, RobotRaconteur_LogComponent.Subscription);
                    }
                }

            }
        }

        /**
         * <summary>Enable a subscription</summary>
         *
         * <param name="name">The local name of the subscription to enable</param>
         */
        [PublicApi]
        public void EnableSubscription(string name)
        {
            lock (this)
            {
                if (!subscriptions.TryGetValue(name, out var s))
                {
                    return;
                }
                if (s.sub == null)
                {
                    return;
                }

                s.details.Enabled = true;
                UpdateSubscription(s, s.details, false);
            }
        }

        /**
         * <summary>Disable a subscription</summary>
         *
         * <param name="name">The local name of the subscription to disable</param>
         * <param name="close">If true, close subscription if connected. Default true</param>
         */
        [PublicApi]
        public void DisableSubscription(string name, bool close = true)
        {
            lock (this)
            {
                if (!subscriptions.TryGetValue(name, out var s))
                {
                    return;
                }

                if (s.sub == null)
                {
                    return;
                }

                s.details.Enabled = false;
                UpdateSubscription(s, s.details, close);
            }
        }

        /**
         * <summary>Get a subscription by name</summary>
         *
         * <param name="name">The local name of the subscription</param>
         * <param name="force_create">If true, create the subscription if it does not exist. Default false</param>
         * <return>The subscription</return>
         */
        [PublicApi]
        public ServiceSubscription GetSubscription(string name, bool force_create = false)
        {
            lock (this)
            {
                if (subscriptions.TryGetValue(name, out var s))
                {
                    return s.sub;
                }

                if (!force_create)
                {
                    LogDebug("ServiceSubscriptionManager subscription not found " + name, node, RobotRaconteur_LogComponent.Subscription);
                    throw new ArgumentException("Subscription not found");
                }

                var details = new ServiceSubscriptionManagerDetails()
                {
                    Name = name,
                    ConnectionMethod = ServiceSubscriptionManagerConnectionMethod.Url,
                    Enabled = false
                };

                var sub = CreateSubscription(details);
                var s2 = new ServiceSubscriptionManager_subscription()
                {
                    details = details,
                    sub = sub
                };
                subscriptions.Add(name, s2);
                return sub;
            }
        }
        /**
         * <summary>Get if a subscription is connected</summary>
         *
         * <param name="name">The local name of the subscription</param>
         * <return>True if the subscription is connected</return>
         */
        [PublicApi]
        public bool IsConnected(string name)
        {
            return GetSubscription(name)?.TryGetDefaultClient<object>(out var a) ?? false;
        }

        /**
         * <summary>Get if a subscription is enabled</summary>
         *
         * <param name="name">The local name of the subscription</param>
         * <return>True if the subscription is enabled</return>
         */
        [PublicApi]
        public bool IsEnabled(string name)
        {
            lock (this)
            {
                if (!subscriptions.TryGetValue(name, out var s))
                {
                    return false;
                }

                return s.details.Enabled;
            }
        }

        /**
         * <summary>Close the subscription manager</summary>
         *
         * <param name="close_subscriptions">If true, close all subscriptions. Default true</param>
         */
        [PublicApi]
        public void Close(bool close_subscriptions = true)
        {
            Dictionary<string, ServiceSubscriptionManager_subscription> subs2;
            lock (this)
            {
                subs2 = new Dictionary<string, ServiceSubscriptionManager_subscription>(subscriptions);
                subscriptions.Clear();
            }

            if (close_subscriptions)
            {
                foreach (var s in subs2.Values)
                {
                    try
                    {
                        s.sub.Close();
                    }
                    catch (Exception e)
                    {
                        LogDebug("ServiceSubscriptionManager Close failed " + e.ToString(), node, RobotRaconteur_LogComponent.Subscription);
                    }
                }
            }

            subs2.Clear();
        }
        /// <summary>
        /// Get the names of all subscriptions
        /// </summary>
        [PublicApi]
        public string[] SubscriptionNames
        {
            get
            {
                lock (this)
                {
                    return subscriptions.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// Get the details of all subscriptions
        /// </summary>
        [PublicApi]
        public ServiceSubscriptionManagerDetails[] SubscriptionDetails
        {
            get
            {
                lock (this)
                {
                    return subscriptions.Values.Select(x => x.details).ToArray();
                }
            }
        }
    }

    class ServiceSubscriptionManager_subscription
    {
        public ServiceSubscriptionManagerDetails details;
        public ServiceSubscription sub;
    }
}
