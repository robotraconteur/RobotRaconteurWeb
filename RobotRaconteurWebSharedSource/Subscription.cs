using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RobotRaconteurWeb.Extensions;
using static RobotRaconteurWeb.RRLogFuncs;
using System.Text.RegularExpressions;
using System.IO;

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
    public class ServiceSubscriptionFilterNode
    {
        /**
        <summary>
        The NodeID to match. All zero NodeID will match any NodeID.
        </summary>
        <remarks>None</remarks>
        */
        public NodeID NodeID;
        /**
        <summary>
        The NodeName to match. Empty or null NodeName will match any NodeName.
        </summary>
        <remarks>None</remarks>
        */
        public string NodeName;
        /**
        <summary>
        The username to use for authentication. Should only be used with secure transports and verified NodeID
        </summary>
        <remarks>None</remarks>
        */
        public string Username;
        /**
        <summary>
        The credentials to use for authentication. Should only be used with secure transports and verified NodeID
        </summary>
        <remarks>None</remarks>
        */
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
    public class ServiceSubscriptionFilter
    {
        /**
        <summary>
        Vector of nodes that should be connected. Empty means match any node.
        </summary>
        <remarks>None</remarks>
        */
        public ServiceSubscriptionFilterNode[] Nodes;
        /**
        <summary>
        Vector service names that should be connected. Empty means match any service name.
        </summary>
        <remarks>None</remarks>
        */
        public string[] ServiceNames;
        /**
        <summary>
        Vector of transport schemes. Empty means match any transport scheme.
        </summary>
        <remarks>None</remarks>
        */
        public string[] TransportSchemes;
        /**
        <summary>
        Attributes to match
        </summary>
        <remarks>None</remarks>
        */
        public Dictionary<string, ServiceSubscriptionFilterAttributeGroup> Attributes;
        /**
        <summary>
        Operation to use to match attributes. Defaults to AND
        </summary>
        */
        public ServiceSubscriptionFilterAttributeGroupOperation AttributesMatchOperation = ServiceSubscriptionFilterAttributeGroupOperation.AND;
        /**
        <summary>
        A user specified predicate function. If nullptr, the predicate is not checked.
        </summary>
        <remarks>None</remarks>
        */
        public Func<ServiceInfo2, bool> Predicate;
        /**
        <summary>
        The maximum number of connections the subscription will create. Zero means unlimited connections.
        </summary>
        <remarks>None</remarks>
        */
        public uint MaxConnection;
    }

    public class ServiceSubscriptionFilterAttribute
    {
        public string Name = string.Empty;
        public string Value = string.Empty;
        public Regex ValueRegex;
        public bool UseRegex = false;

        public ServiceSubscriptionFilterAttribute() { }

        public ServiceSubscriptionFilterAttribute(string value)
        {
            Value = value;
        }

        public ServiceSubscriptionFilterAttribute(Regex valueRegex)
        {
            ValueRegex = valueRegex;
            UseRegex = true;
        }

        public ServiceSubscriptionFilterAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public ServiceSubscriptionFilterAttribute(string name, Regex valueRegex)
        {
            Name = name;
            ValueRegex = valueRegex;
            UseRegex = true;
        }

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

    public static class ServiceSubscriptionFilterAttributeFactory
    {
        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeRegex(string regexValue)
        {
            return new ServiceSubscriptionFilterAttribute(new Regex(regexValue));
        }

        public static ServiceSubscriptionFilterAttribute CreateServiceSubscriptionFilterAttributeRegex(string name, string regexValue)
        {
            return new ServiceSubscriptionFilterAttribute(name, new Regex(regexValue));
        }
    }

    public enum ServiceSubscriptionFilterAttributeGroupOperation
    {
        OR,
        AND,
        NOR,  // Also used for NOT
        NAND
    }

    public class ServiceSubscriptionFilterAttributeGroup
    {
        public List<ServiceSubscriptionFilterAttribute> Attributes = new List<ServiceSubscriptionFilterAttribute>();
        public List<ServiceSubscriptionFilterAttributeGroup> Groups = new List<ServiceSubscriptionFilterAttributeGroup>();
        public ServiceSubscriptionFilterAttributeGroupOperation Operation = ServiceSubscriptionFilterAttributeGroupOperation.OR;
        public bool SplitStringAttribute = true;
        public char SplitStringDelimiter = ',';

        public ServiceSubscriptionFilterAttributeGroup() { }

        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation)
        {
            Operation = operation;
        }

        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation, List<ServiceSubscriptionFilterAttribute> attributes)
        {
            Operation = operation;
            Attributes = attributes;
        }

        public ServiceSubscriptionFilterAttributeGroup(ServiceSubscriptionFilterAttributeGroupOperation operation, List<ServiceSubscriptionFilterAttributeGroup> groups)
        {
            Operation = operation;
            Groups = groups;
        }

        public static bool ServiceSubscriptionFilterAttributeGroupDoFilter<T>(
    ServiceSubscriptionFilterAttributeGroupOperation operation,
    List<ServiceSubscriptionFilterAttribute> attributes,
    List<ServiceSubscriptionFilterAttributeGroup> groups,
    List<object> values)
        {
            switch (operation)
            {
                case ServiceSubscriptionFilterAttributeGroupOperation.OR:
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
                case ServiceSubscriptionFilterAttributeGroupOperation.AND:
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
                case ServiceSubscriptionFilterAttributeGroupOperation.NOR:
                    {
                        return !ServiceSubscriptionFilterAttributeGroupDoFilter<T>(ServiceSubscriptionFilterAttributeGroupOperation.OR, attributes, groups, values);
                    }
                case ServiceSubscriptionFilterAttributeGroupOperation.NAND:
                    {
                        return !ServiceSubscriptionFilterAttributeGroupDoFilter<T>(ServiceSubscriptionFilterAttributeGroupOperation.AND, attributes, groups, values);
                    }
                default:
                    {
                        throw new ArgumentException("Invalid attribute filter operation");
                    }
            }
        }

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

       
        public bool IsMatch(List<string> values)
        {
            return ServiceSubscriptionFilterAttributeGroupDoFilter<string>(Operation, Attributes, Groups, values.Select(x => (object)x).ToList());
        }

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
    public struct ServiceSubscriptionClientID
    {
        /**
        <summary>
        The NodeID of the connected service
        </summary>
        <remarks>None</remarks>
        */
        public NodeID NodeID;
        /**
        <summary>
        The ServiceName of the connected service
        </summary>
        <remarks>None</remarks>
        */
        public string ServiceName;
        /**
        <summary>
        Construct a ServiceSubscriptionClientID
        </summary>
        <remarks>None</remarks>
        <param name="node_id">The NodeID</param>
        <param name="service_name">The Service Name</param>
        */
        public ServiceSubscriptionClientID(NodeID NodeID, string ServiceName)
        {
            this.NodeID = NodeID;
            this.ServiceName = ServiceName;
        }

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
                            if (url1.StartsWith(scheme1 + "://")) {
                                urls.Add(url1);
                            }
                        }
                    }

                    if (urls.Count == 0 && storage!=null)
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

                if (filter.Attributes!=null && filter.Attributes.Any())
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
                        case ServiceSubscriptionFilterAttributeGroupOperation.OR:
                            if (!attrMatches.Contains(true))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.NOR:
                            if (attrMatches.Contains(true))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.NAND:
                            if (!attrMatches.Contains(false))
                                return false;
                            break;

                        case ServiceSubscriptionFilterAttributeGroupOperation.AND:
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
    public class ServiceInfo2Subscription : IServiceSubscription
    {
        bool active;
        Dictionary<ServiceSubscriptionClientID, ServiceInfo2Subscription_client> clients = new Dictionary<ServiceSubscriptionClientID, ServiceInfo2Subscription_client>();
        uint retry_delay;

        Discovery parent;
        RobotRaconteurNode node;
        public ServiceInfo2Subscription(Discovery parent)
        {
            this.parent = parent;
            this.node = parent.node;
            active = true;
            retry_delay = 15000;
        }
        /**
        <summary>
        Close the subscription
        </summary>
        <remarks>
        Closes the subscription. Subscriptions are automatically closed when the node is shut down.
        </remarks>
        */
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

                    if (clients.TryGetValue(k, out var e))
                    {
                        var info2 = e.service_info2;
                        if (info.NodeName != info2.NodeName || info2.Name != info.Name ||
                            info2.RootObjectType != info.RootObjectType || info2.ConnectionURL != info.ConnectionURL ||
                            !new HashSet<string>(info.RootObjectImplements).SetEquals(new HashSet<string>(info2.RootObjectImplements)))
                        {
                            e.service_info2 = info;
                            ServiceDetected?.Invoke(this, k, info);
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
                            found = true; break;
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
        public event Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> ServiceDetected;
        /**
        <summary>
        Listener event that is invoked when a service is lost
        </summary>
        <remarks>None</remarks>
        */
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
    public class ServiceSubscription : IServiceSubscription
    {

        bool active = false;
        Dictionary<ServiceSubscriptionClientID, ServiceSubscription_client> clients = new Dictionary<ServiceSubscriptionClientID, ServiceSubscription_client>();

        internal RobotRaconteurNode node;
        Discovery parent;
        string[] service_types;
        ServiceSubscriptionFilter filter;
        List<WireSubscriptionBase> wire_subscriptions = new List<WireSubscriptionBase>();
        List<PipeSubscriptionBase> pipe_subscriptions = new List<PipeSubscriptionBase>();

        bool use_service_url = false;
        string[] service_url;
        string service_url_username;
        Dictionary<string, object> service_url_credentials;

        CancellationTokenSource cancel = new CancellationTokenSource();
        /**
        <summary>
        Close the subscription
        </summary>
        <remarks>
        Close the subscription. Subscriptions are automatically closed when the node is shut down.
        </remarks>
        */
        public void Close()
        {
            lock (this)
            {
                cancel.Cancel();

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

        void IServiceSubscription.Init(string[] service_types, ServiceSubscriptionFilter filter)
        {
            this.active = true;
            this.service_types = service_types;
            this.filter = filter;
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

        async Task RunClient(ServiceSubscription_client client)
        {


            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    client.connecting = true;
                    object o;
                    TaskCompletionSource<bool> wait_task;
                    try
                    {
                        //ClientContext.ClientServiceListenerDelegate client_listener = delegate (ClientContext context, ClientServiceListenerEventType evt, object param) { };
                        o = await node.ConnectService(client.urls, client.username, client.credentials, null, client.service_type, cancel.Token).ConfigureAwait(false);
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
                            wait_task.SetResult(true);
                        }
                    };
                    try
                    {
                        ClientConnected?.Invoke(this, new ServiceSubscriptionClientID(client.nodeid, client.service_name), o);
                    }
                    catch (Exception ex)
                    {
                        LogDebug(string.Format("Error in ServiceSubscription.ClientConnected callback {0}",ex), node, RobotRaconteur_LogComponent.Subscription);
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
                    finally
                    {


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
                if (!active)
                    return;
                if (storage == null)
                    return;
                if (storage.services == null)
                    return;
                if (storage.info == null)
                    return;

                foreach (var info in storage.services) {
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
        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientConnected;
        /**
        <summary>
        Event listener for when a client disconnects
        </summary>
        <remarks>None</remarks>
        */
        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientDisconnected;
        /**
        <summary>
        Event listener for when a client connection attempt fails. Use to diagnose connection problems
        </summary>
        <remarks>None</remarks>
        */
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
        Release a client previously clamed with ClaimClient()
        </summary>
        <remarks>
        Lifecycle management is returned to the subscription
        </remarks>
        <param name="client">The client to release claim</param>
        */
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
        <param name="obj">[out] The client connection</param>
        <returns>true if client object is valid, false otherwise</returns>
        */
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
        <param name="obj">[out] The client connection</param>
        <param name="cancel">Cancellation token</param>
        <returns>true if client object is valid, false otherwise</returns>
        */
        public async Task<Tuple<bool,T>> TryGetDefaultClientWait<T>(CancellationToken cancel = default)
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
        <param name="objecttype">(Optional) The desired root object proxy type. Optional but highly recommended.</param>
        <param name="close_connected">(Optional, default false) Close existing connections</param>
        */
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
        <param name="objecttype">(Optional) The desired root object proxy type. Optional but highly recommended.</param>
        <param name="close_connected">(Optional, default false) Close existing connections</param>
        */
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

        public ServiceSubscription(Discovery parent)
        {
            this.parent = parent;
            active = true;
            this.node = parent.node;
        }
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
        public WireSubscription<T> SubscribeWire<T>(string membername, string servicepath = null)
        {
            var o = new WireSubscription<T>(this, membername, servicepath);
            lock (this)
            {
                if (wire_subscriptions.FirstOrDefault(x => x.membername == membername && x.servicepath == servicepath) != null)
                {
                    throw new InvalidOperationException("Already subscribed to wire member: " + membername);
                }


                wire_subscriptions.Add(o);

                foreach (var c in clients.Values)
                {
                    o.ClientConnected(new ServiceSubscriptionClientID(c.nodeid, c.service_name), c);
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
        public PipeSubscription<T> SubscribePipe<T>(string membername, string servicepath = null)
        {
            var o = new PipeSubscription<T>(this, membername, servicepath);
            lock (this)
            {
                if (pipe_subscriptions.FirstOrDefault(x => x.membername == membername && x.servicepath == servicepath) != null)
                {
                    throw new InvalidOperationException("Already subscribed to pipe member: " + membername);
                }


                pipe_subscriptions.Add(o);

                foreach (var c in clients.Values)
                {
                    o.ClientConnected(new ServiceSubscriptionClientID(c.nodeid, c.service_name), c);
                }
            }
            return o;
        }

        internal void WireSubscriptionClosed(WireSubscriptionBase s)
        {
            lock(this)
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
    }


    internal class WireSubscription_connection
    {
        internal WireSubscriptionBase parent;
        internal object connection;
        internal object client;
        internal bool closed;
        internal CancellationTokenSource cancel;
    }
  
    public abstract class WireSubscriptionBase
    {

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
        /**
        <summary>
        Closes the wire subscription
        </summary>
        <remarks>
        Wire subscriptions are automatically closed when the parent ServiceSubscription is closed
        or when the node is shut down.
        </remarks>
        */
        public void Close()
        {
            this.cancel.Cancel();
            parent.WireSubscriptionClosed(this);
        }

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
        public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken cancel = default)
        {
            AsyncValueWaiter<object>.AsyncValueWaiterTask waiter = null;
            lock(this)
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
        public uint ActiveWireConnectionCount { get { return 0; } }
        /**
        <summary>
        Get or Set if InValue is ignored
        </summary>
        <remarks />
        <value />
        */
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
            lock(this)
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
    public class WireSubscription<T> : WireSubscriptionBase
    {
        public WireSubscription(ServiceSubscription parent, string membernname, string servicepath) 
            : base(parent, membernname, servicepath)
        {
        }

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
                            lock(this)
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
                        await Task.Delay(2500, c.cancel.Token);
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
        public T InValue
        {
            get
            {
                lock (this)
                {
                    if (!in_value_valid)
                    {
                        throw new InvalidOperationException("InValue is not valid");
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
        public T GetInValue(out TimeSpec ts, out Wire<T>.WireConnection connection)
        {
            lock(this)
            {
                if (!in_value_valid)
                {
                    throw new InvalidOperationException("InValue is not valid");
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
        public bool TryGetInValue(out T val, out TimeSpec ts, out Wire<T>.WireConnection connection)
        {
            lock (this)
            {
                if (!in_value_valid)
                {
                    val = default;
                    ts = default;
                    connection = default;
                    return false;
                }
                ts = in_value_time;
                connection = (Wire<T>.WireConnection)in_value_connection;
                val=(T)in_value;
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
        public bool TryGetInValue(out T val)
        {
            lock (this)
            {
                if (!in_value_valid)
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
        public void SetOutValueAll(T value)
        {
            lock(this)
            {
                foreach(var c in connections.Values)
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
        public void Close()
        {
            this.cancel.Cancel();
            parent.PipeSubscriptionClosed(this);
        }

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

        internal protected async Task<Tuple<bool,object,object>> TryReceivedPacketWaitBase(int timeout=-1, bool peek=false)
        {
            lock (this)
            {
                if (recv_packets.Count > 0)
                {
                    var q = recv_packets.Dequeue();
                    return Tuple.Create(true,q.Item1,q.Item2);
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
        /**
        <summary>
        Get the number of packets available to receive
        </summary>
        <remarks>
        Use ReceivePacket(), TryReceivePacket(), or TryReceivePacketWait() to receive the packet
        </remarks>
        */
        public uint Available { get { return 0; } }
        /**
        <summary>
        Get the number of pipe endpoints currently connected
        </summary>
        <remarks>None</remarks>
        */
        public uint ActivePipeEndpointCount { get { return 0; } }
        /**
        <summary>
        Get or set if incoming packets are ignored
        </summary>
        <remarks>None</remarks>
        */
        public bool IgnoreReceived { get; set; }

        internal protected PipeSubscriptionBase(ServiceSubscription parent, string membername, string servicepath="", int max_recv_packets = -1, int max_send_backlog = 5)
        {
            this.parent = parent;
            this.node = parent.node;
            this.membername = membername;
            this.servicepath = servicepath;
            this.max_recv_packets=max_recv_packets; 
            this.max_send_backlog=max_send_backlog;
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
    public class PipeSubscription<T> : PipeSubscriptionBase
    {
        protected internal PipeSubscription(ServiceSubscription parent, string membername, string servicepath = "", int max_recv_packets = -1, int max_send_backlog = 5) 
            : base(parent, membername, servicepath, max_recv_packets, max_send_backlog)
        {
        }

        /**
        <summary>
        Dequeue a packet from the receive queue
        </summary>
        <remarks>
        If the receive queue is empty, an InvalidOperationException() is thrown
        </remarks>
        <returns>The dequeued packet</returns>
        */
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
        public async Task<Tuple<bool,T, Pipe<T>.PipeEndpoint>> TryReceivePacketWait(int timeout= -1, bool peek=false)
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
        <param name="value">The packet to send</param>
        */
        public void AsyncSendPacketAll(T packet)
        {
            
            lock (this)
            {
                foreach (var c in connections.Values)
                {
                    if (c.active_send_count < this.max_send_backlog)
                    {
                        var ep = c.endpoint as Pipe<T>.PipeEndpoint;
                        if (ep!=null) {
                            ep.SendPacket(packet, cancel.Token).ContinueWith((t) =>
                            {
                                if (t.Status == TaskStatus.RanToCompletion)
                                {
                                    lock(this)
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
                            lock(this)
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
        public event Action<PipeSubscription<T>> PipePacketReceived;
    }
}