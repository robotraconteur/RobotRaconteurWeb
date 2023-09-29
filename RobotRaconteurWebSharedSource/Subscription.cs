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

    public class ServiceSubscriptionFilterNode
    {
        public NodeID NodeID;
        public string NodeName;
        public string Username;
        public Dictionary<string, object> Credentials;
    }

    public class ServiceSubscriptionFilter
    {
        public ServiceSubscriptionFilterNode[] Nodes;
        public string[] ServiceNames;
        public string[] TransportSchemes;
        public Dictionary<string, ServiceSubscriptionFilterAttributeGroup> Attributes;
        public ServiceSubscriptionFilterAttributeGroupOperation AttributesMatchOperation = ServiceSubscriptionFilterAttributeGroupOperation.AND;
        public Func<ServiceInfo2, bool> Predicate;
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

    public struct ServiceSubscriptionClientID
    {
        public NodeID NodeID;
        public string ServiceName;

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

        public event Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> ServiceDetected;

        public event Action<ServiceInfo2Subscription, ServiceSubscriptionClientID, ServiceInfo2> ServiceLost;

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

    // Implement class using reference https://github.com/robotraconteur/robotraconteur/blob/master/RobotRaconteurCore/src/Subscription.cpp
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

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientConnected;

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, object> ClientDisconnected;

        public event Action<ServiceSubscription, ServiceSubscriptionClientID, string[], Exception> ClientConnectFailed;

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

        public uint ConnectRetryDelay { get; set; } = 2500;

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

        public string[] GetServiceURL()
        {
            if (!use_service_url)
            {
                throw new InvalidOperationException("Subscription not using service url");
            }

            return service_url;
        }

        public void UpdateServiceURL(string url, string username = null, Dictionary<string, object> credentials = null, string object_type = null, bool close_connected = false)
        {
            UpdateServiceURL(new string[] { url }, username, credentials, object_type, close_connected);
        }

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

        public uint ActiveWireConnectionCount { get { return 0; } }

        public bool IgnoreInValue { get; set; }

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

        public uint Available { get { return 0; } }

        public uint ActivePipeEndpointCount { get { return 0; } }

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
    public class PipeSubscription<T> : PipeSubscriptionBase
    {
        protected internal PipeSubscription(ServiceSubscription parent, string membername, string servicepath = "", int max_recv_packets = -1, int max_send_backlog = 5) 
            : base(parent, membername, servicepath, max_recv_packets, max_send_backlog)
        {
        }

        public T ReceivePacket()
        {
            return (T)ReceivePacketBase();
        }

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

        public async Task<Tuple<bool,T, Pipe<T>.PipeEndpoint>> TryReceivePacketWait(int timeout= -1, bool peek=false)
        {
            var r = await TryReceivedPacketWaitBase(timeout, peek).ConfigureAwait(false);
            if (!r.Item1)
            {
                return Tuple.Create(false, default(T), default(Pipe<T>.PipeEndpoint));
            }

            return Tuple.Create(true, (T)r.Item2, (Pipe<T>.PipeEndpoint)r.Item3);
        }

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

        public bool IgnoreInValue { get; set; }

        
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
                        if (IgnoreInValue)
                        {
                            
                            // TODO: ignore in value
                        }

                        c.endpoint = cc;

                        wait_task.AttachCancellationToken(c.cancel.Token);

                        Pipe<T>.PipePacketReceivedCallbackFunction pipe_changed_ev = delegate (Pipe<T>.PipeEndpoint ev_ep)
                        {
                            lock (this)
                            {
                                if (IgnoreInValue)
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

        public event Action<PipeSubscription<T>> PipePacketReceived;
    }
}