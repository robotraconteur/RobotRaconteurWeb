// Implement Discovery class in csharp based on RobotRaconteurCore cpp file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Contains information about a service found using discovery
    </summary>
    <remarks>
    <para>
    ServiceInfo2 contains information about a service required to
    connect to the service, metadata, and the service attributes
    </para>
    <para>
    ServiceInfo2 structures are returned by RobotRaconteurNode::FindServiceByType()
    and ServiceInfo2Subscription
    </para>
    </remarks>
    */
    [PublicApi]
    public class ServiceInfo2
    {
        /**
        <summary>
        The name of the service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string Name;
        /**
        <summary>
        The fully qualified type of the root object in the service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string RootObjectType;
        /**
        <summary>
        The fully qualified types the root object implements
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string[] RootObjectImplements;
        /**
        <summary>
        Candidate URLs to connect to the service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string[] ConnectionURL;
        /**
        <summary>
        Service attributes
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public Dictionary<string, object> Attributes;
        /**
        <summary>
        The NodeID of the node that owns the service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public NodeID NodeID;
        /**
        <summary>
        The NodeName of the node that owns the service
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string NodeName;

        /// <summary>
        /// Construct an empty ServiceInfo2
        /// </summary>
        /// <remarks>None</remarks>
        [PublicApi] 
        public ServiceInfo2() { }

        /// <summary>
        /// Construct a ServiceInfo2 using information returned from discovery
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="info">ServiceInfo structure returned by node service index</param>
        /// <param name="ninfo">NodeInfo from discovery</param>
        [PublicApi] 
        public ServiceInfo2(RobotRaconteurServiceIndex.ServiceInfo info, RobotRaconteurServiceIndex.NodeInfo ninfo)
        {
            Name = info.Name;
            RootObjectType = info.RootObjectType;
            RootObjectImplements = info.RootObjectImplements.Values.ToArray();
            ConnectionURL = info.ConnectionURL.Values.ToArray();
            Attributes = info.Attributes;
            NodeID = new NodeID(ninfo.NodeID);
            NodeName = ninfo.NodeName;

        }

    }
    /**
    <summary>
    Contains information about a node detected using discovery
    </summary>
    <remarks>
    <para>
    NodeInfo2 contains information about a node detected using discovery.
    Node information is typically not verified, and is used as a first
    step to detect available services.
    </para>
    <para>
    NodeInfo2 structures are returned by RobotRaconteurNode.FindNodeByName()
    and RobotRaconteurNode.FindNodeByID()
    </para>
    </remarks>
    */
    [PublicApi]
    public class NodeInfo2
    {
        /**
        <summary>
        The NodeID of the detected node
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public NodeID NodeID;
        /**
        <summary>
        The NodeName of the detected node
        </summary>
        <remarks>None</remarks>
        */
        [PublicApi]
        public string NodeName;
        /**
        <summary>
        Candidate URLs to connect to the node
        </summary>
        <remarks>
        The URLs for the node typically contain the node transport endpoint
        and the nodeid. A URL service parameter must be appended
        to connect to a service.
        </remarks>
        */
        [PublicApi]
        public string[] ConnectionURL;
    }


    

    class Discovery_nodestorage
    {
        public NodeDiscoveryInfo info;
        public ServiceInfo2[] services;
        public string last_update_nonce;
        public DateTime last_update_time;
        //public Discovery_updateserviceinfo updater;
        public Queue<string> recent_service_nonce = new Queue<string>();
        public DateTime retry_window_start;
        public uint retry_count;

        public bool service_info_updating;

    }

    /**
     * <summary>Raw information used to announce and detect nodes</summary>
     * <remarks>
     * For TCP/IP and QUIC/IP, this information is transmitted using
     * UDP multicast packets. For local transports, the filesystem is used.
     *
     * The data contained in NodeDiscoveryInfo is unverified and unfiltered.
     *
     * NodeDiscoveryInfo is used with RobotRaconteurNode::GetDetectedNodes(),
     * RobotRaconteurNode::AddNodeServicesDetectedListener(),
     * and RobotRaconteurNode::AddNodeDetectionLostListener()
     * </remarks>
     */
    [PublicApi]
    public class NodeDiscoveryInfo
    {
        /** <summary>The detected NodeID</summary> */
        [PublicApi]
        public NodeID NodeID;
        /** <summary>The detected NodeName</summary> */
        [PublicApi]
        public string NodeName = "";
        /** <summary>Candidate URLs to connect to the node</summary> */
        [PublicApi]
        public List<NodeDiscoveryInfoURL> URLs = new List<NodeDiscoveryInfoURL>();
        /** <summary>The current nonce for the node's services</summary>
         *
         * <remarks>
         * The ServiceStateNonce is a random string that represents the current
         * state of the nodes services. If the services change, the nonce will
         * change to a new random string, indicating that the client should
         * reinterrogate the node.
         * </remarks>
         */
        [PublicApi]
        public string ServiceStateNonce;
    }

    /** <summary>A candidate node connection URL and its timestamp</summary>
     *  <remarks>None</remarks>
     */
    [PublicApi]
    public class NodeDiscoveryInfoURL
    {
        /** <summary>Candidate node connection URL</summary> */
        [PublicApi]
        public string URL;
        /**
         * <summary>Last time that this URL announce was received</summary>
         * <remarks>
         * Candidate URLs typically expire after one minute. If all
         * candidate URLs expire, the node is considered lost.
         * </remarks> 
        */
        [PublicApi]
        public DateTime LastAnnounceTime;
    }

    
    #pragma warning disable 1591
    public class Discovery
    {
        internal Dictionary<string, Discovery_nodestorage> m_DiscoveredNodes = new Dictionary<string, Discovery_nodestorage>();

        public Dictionary<string, NodeDiscoveryInfo> DiscoveredNodes { get { return m_DiscoveredNodes.ToDictionary(x=>x.Key,x=>x.Value.info); } }

        internal RobotRaconteurNode node;
        internal Task cleandiscoverednodes_task;
        private CancellationTokenSource shutdown_token = new CancellationTokenSource();

        uint NodeDiscoveryMaxCacheCount {get; set;} = 1000;

        public Discovery(RobotRaconteurNode node)
        {
            this.node = node;
            cleandiscoverednodes_task =  PeriodicTask.Run(CleanDiscoveredNodes, TimeSpan.FromSeconds(5), shutdown_token.Token);
        }

        public void Shutdown()
        {
            shutdown_token.Cancel();

            IServiceSubscription[] subs;
            lock(subscriptions)
            {
                subs = subscriptions.ToArray();
                subscriptions.Clear();
            }

            foreach(var s in subs)
            {
                Task.Run(() => s.Close()).IgnoreResult();
            }
        }

        public void NodeAnnouncePacketReceived(string packet)
        {

            try
            {
                string seed = "Robot Raconteur Node Discovery Packet";
                if (packet.Substring(0, seed.Length) == seed)
                {
                    lock (m_DiscoveredNodes)
                    {
                        string[] lines = packet.Split(new char[] { '\n' });
                        string[] idline = lines[1].Split(new char[] { ',' });

                        NodeID nodeid = new NodeID(idline[0]);

                        string nodename = idline[1];
                        string url = lines[2];
                        //if (!IPAddress.Parse(packet.Split(new char[] {'\n'})[1]).GetAddressBytes().SequenceEqual(RobotRaconteurNode.s.NodeID))
                        
                        NodeDiscoveryInfo i = new NodeDiscoveryInfo();
                        i.NodeID = nodeid;
                        i.NodeName = nodename;
                        NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                        u.URL = url;
                        u.LastAnnounceTime = DateTime.UtcNow;
                        i.URLs.Add(u);

                        NodeDetected(i);

                    //RobotRaconteurNode.s.NodeAnnouncePacketReceived(packet);
                }

            }
            }
            catch { };

            //Console.WriteLine(packet);
        }

        internal void NodeDetected(NodeDiscoveryInfo n)
        {
            if (n.ServiceStateNonce != null && n.ServiceStateNonce.Length > 32)
            {
                //TODO: Log service state nonce invalid
                return;
            }

            try
            {
                lock (m_DiscoveredNodes)
                {
                    if (m_DiscoveredNodes.Keys.Contains(n.NodeID.ToString()))
                    {
                        var e1 = m_DiscoveredNodes[n.NodeID.ToString()];
                        NodeDiscoveryInfo i = e1.info;
                        i.NodeName = n.NodeName;
                        foreach (var url in n.URLs)
                        {
                            if (!i.URLs.Any(x => x.URL == url.URL))
                            {
                                if (i.URLs.Count > 256)
                                {
                                    continue;
                                }
                                // Parse the url and check if it is valid
                                try
                                {
                                var uu = TransportUtil.ParseConnectionUrl(url.URL);
                                if (uu.nodeid != i.NodeID)
                                {
                                    continue;
                                }
                                }
                                catch
                                {
                                    // TODO: log error
                                    continue;
                                }
                                    
                                
                                NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                                u.URL = url.URL;
                                u.LastAnnounceTime = DateTime.UtcNow;
                                i.URLs.Add(u);
                                //Console.WriteLine(url);
                            }
                            else
                            {
                                i.URLs.First(x => x.URL == url.URL).LastAnnounceTime = DateTime.UtcNow;
                            }
                        }

                        if (i.ServiceStateNonce != n.ServiceStateNonce && !string.IsNullOrEmpty(n.ServiceStateNonce))
                        {
                            i.ServiceStateNonce = n.ServiceStateNonce;
                        }

                        if (!string.IsNullOrEmpty(n.ServiceStateNonce))
                        {
                            if(e1.recent_service_nonce.Contains(n.ServiceStateNonce))
                            {
                                return;
                            }
                            else
                            {
                                e1.recent_service_nonce.Enqueue(n.ServiceStateNonce);
                                if (e1.recent_service_nonce.Count > 16)
                                {
                                    e1.recent_service_nonce.Dequeue();
                                }
                            }


                        }

                        if (subscriptions.Count > 0)
                        {
                            if ((i.ServiceStateNonce != e1.last_update_nonce) || string.IsNullOrEmpty(i.ServiceStateNonce))
                            {
                                _ = RetryUpdateServiceInfo(e1).IgnoreResult();
                            }
                        }
                    }
                    else
                    {
                        if (m_DiscoveredNodes.Count >= NodeDiscoveryMaxCacheCount)
                        {
                            // TODO: log ignored node
                            return;
                        }

                        var storage = new Discovery_nodestorage();
                        storage.info = n;
                        storage.last_update_nonce = "";
                        storage.retry_window_start = DateTime.UtcNow;
                        storage.recent_service_nonce.Enqueue(n.ServiceStateNonce);

                        foreach (var u in n.URLs)
                        {
                            u.LastAnnounceTime = DateTime.UtcNow;
                        }
                        m_DiscoveredNodes.Add(n.NodeID.ToString(), storage);

                        // TODO: check if subscriptions is empty
                        if (subscriptions.Count > 0)
                        {
                            CallUpdateServiceInfo(storage, n.ServiceStateNonce);
                        }
                    }
                }
            }
            catch (Exception) { }

        }

        protected internal void CleanDiscoveredNodes()
        {
            try
            {
                lock (m_DiscoveredNodes)
                {
                    DateTime now = DateTime.UtcNow;

                    string[] keys = m_DiscoveredNodes.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        List<NodeDiscoveryInfoURL> newurls = new List<NodeDiscoveryInfoURL>();
                        NodeDiscoveryInfoURL[] urls = m_DiscoveredNodes[key].info.URLs.ToArray();
                        foreach (NodeDiscoveryInfoURL u in urls)
                        {

                            double time = (now - u.LastAnnounceTime).TotalMilliseconds;
                            if (time < 60000)
                            {
                                newurls.Add(u);
                            }
                        }

                        m_DiscoveredNodes[key].info.URLs = newurls;

                        if (newurls.Count == 0)
                        {
                            var d = m_DiscoveredNodes[key];

                            m_DiscoveredNodes.Remove(key);

                            Task.Run(() =>
                            {
                                lock (subscriptions)
                                {
                                    foreach(var s in subscriptions)
                                    {
                                        Task.Run(() =>
                                        {
                                            s.NodeLost(d);
                                        }).IgnoreResult();
                                    }
                                }
                            });
                        }

                    }


                }
            }
            catch (Exception) { }

        }

        internal async Task UpdateDetectedNodes(CancellationToken cancel)
        {
            var tasks = new List<Task<List<NodeDiscoveryInfo>>>();
            var t = new List<Transport>();
            lock (node.transports)
            {
                foreach (var t2 in node.transports.Values) t.Add(t2);
            }

            foreach (var t2 in t)
            {
                var task1 = t2.GetDetectedNodes(cancel);
                tasks.Add(task1);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var t2 in tasks)
            {
                try
                {
                    var info = await t2.ConfigureAwait(false);
                    foreach (var i in info)
                    {
                        NodeDetected(i);
                    }
                }
                catch (Exception) { }
            }

        }

        private async Task<Tuple<byte[], string, Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>>> DoFindServiceByType(string[] urls, CancellationToken cancel)
        {
            RobotRaconteurServiceIndex.ServiceIndex ind = (RobotRaconteurServiceIndex.ServiceIndex)await node.ConnectService(urls.ToArray(), cancel: cancel).ConfigureAwait(false);
            var NodeID = ((ServiceStub)ind).RRContext.RemoteNodeID.ToByteArray();
            var NodeName = ((ServiceStub)ind).RRContext.RemoteNodeName;
            var inf = await ind.GetLocalNodeServices(cancel).ConfigureAwait(false);
            return Tuple.Create(NodeID, NodeName, inf);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindServiceByType(servicetype, transportschemes, cancel.Token).ConfigureAwait(false);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes, CancellationToken cancel)
        {

            try
            {
                await UpdateDetectedNodes(cancel).ConfigureAwait(false);
            }
            catch { };

            List<ServiceInfo2> services = new List<ServiceInfo2>();
            List<string> nodeids;
            lock (m_DiscoveredNodes)
            {

                nodeids = DiscoveredNodes.Keys.ToList();

            }

            var info_wait = new List<Task<Tuple<byte[], string, Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>>>>();

            for (int i = 0; i < nodeids.Count; i++)
            {

                try
                {
                    List<string> urls = new List<string>();
                    lock (m_DiscoveredNodes)
                    {
                        foreach (NodeDiscoveryInfoURL url in m_DiscoveredNodes[nodeids[i]].info.URLs)
                        {
                            foreach (var s in transportschemes)
                            {
                                if (url.URL.StartsWith(s + "://"))
                                {
                                    urls.Add(url.URL);
                                    break;
                                }

                            }
                        }
                    }
                    if (urls.Count > 0)
                    {
                        info_wait.Add(DoFindServiceByType(urls.ToArray(), cancel));
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        lock (m_DiscoveredNodes)
                        {
                            m_DiscoveredNodes.Remove(nodeids[i]);
                        }
                    }
                    catch { }
                }

            }

            if (info_wait.Count == 0)
            {
                return new ServiceInfo2[0];
            }

            try
            {
                Task waittask = Task.WhenAll(info_wait);

                await waittask.ConfigureAwait(false);
            }
            catch (Exception) { }

            for (int i = 0; i < nodeids.Count; i++)
            {

                try
                {

                    if (!info_wait[i].IsCompleted)
                    {
                        throw new TimeoutException("Timeout");
                    }

                    var inf = await info_wait[i].ConfigureAwait(false);


                    RobotRaconteurServiceIndex.NodeInfo n = new RobotRaconteurServiceIndex.NodeInfo();
                    n.NodeID = inf.Item1;
                    n.NodeName = inf.Item2;


                    foreach (RobotRaconteurServiceIndex.ServiceInfo ii in inf.Item3.Values)
                    {
                        if (ii.RootObjectType == servicetype)
                        {
                            services.Add(new ServiceInfo2(ii, n));
                        }
                        else
                        {
                            foreach (string impl in ii.RootObjectImplements.Values)
                            {
                                if (impl == servicetype)
                                    services.Add(new ServiceInfo2(ii, n));
                            }

                        }
                    }
                }
                catch
                {
                    try
                    {
                        lock (m_DiscoveredNodes)
                        {
                            m_DiscoveredNodes.Remove(nodeids[i]);
                        }
                    }
                    catch { }
                }

            }


            return services.ToArray();
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindNodeByID(id, schemes, cancel.Token).ConfigureAwait(false);
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel).ConfigureAwait(false);
            }
            catch { };

            var o = new List<NodeInfo2>();

            lock (m_DiscoveredNodes)
            {
                string nodeid_str = id.ToString();
                if (m_DiscoveredNodes.ContainsKey(nodeid_str))
                {
                    var ni = m_DiscoveredNodes[nodeid_str];
                    var n = new NodeInfo2();
                    n.NodeID = new NodeID(nodeid_str);
                    n.NodeName = ni.info.NodeName;

                    var c = new List<string>();

                    foreach (var url in ni.info.URLs)
                    {
                        var u = TransportUtil.ParseConnectionUrl(url.URL);
                        if (schemes.Any(x => x == u.scheme))
                        {
                            string short_url;
                            if (u.port == -1)
                            {
                                short_url = u.scheme + "//" + u.host + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                            }
                            else
                            {
                                short_url = u.scheme + "//" + u.host + ":" + u.port + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                            }

                            c.Add(short_url);
                        }

                    }

                    if (c.Count != 0)
                    {
                        n.ConnectionURL = c.ToArray();
                        o.Add(n);
                    }
                }
            }

            return o.ToArray();
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindNodeByName(name, schemes, cancel.Token).ConfigureAwait(false);
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel).ConfigureAwait(false);
            }
            catch { };

            var o = new List<NodeInfo2>();

            lock (m_DiscoveredNodes)
            {
                foreach (var e in m_DiscoveredNodes)
                {
                    if (e.Value.info.NodeName == name)
                    {
                        var n = new NodeInfo2();
                        var nodeid_str = e.Value.info.NodeID.ToString();
                        n.NodeID = new NodeID(nodeid_str);
                        n.NodeName = e.Value.info.NodeName;

                        var c = new List<string>();

                        foreach (var url in e.Value.info.URLs)
                        {
                            var u = TransportUtil.ParseConnectionUrl(url.URL);
                            if (schemes.Any(x => x == u.scheme))
                            {
                                string short_url;
                                if (u.port == -1)
                                {
                                    short_url = u.scheme + "//" + u.host + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                                }
                                else
                                {
                                    short_url = u.scheme + "//" + u.host + ":" + u.port + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                                }

                                c.Add(short_url);
                            }

                        }

                        if (c.Count != 0)
                        {
                            n.ConnectionURL = c.ToArray();
                            o.Add(n);
                        }
                    }
                }
            }

            return o.ToArray();
        }

        internal async Task<Tuple<bool,ServiceInfo2[]>> DoUpdateServiceInfo(Discovery_nodestorage storage, string nonce, int extra_backoff, CancellationToken cancel)
        {
            lock(storage)
            {
                if (storage.service_info_updating)
                {
                    return Tuple.Create(false, new ServiceInfo2[0]);
                }
                storage.service_info_updating = true;
            }

            try
            {
                var r = new Random();
                var backoff = r.Next(100, 600) + extra_backoff;

                await Task.Delay(backoff, cancel).ConfigureAwait(false);

                var urls = storage.info.URLs.Select(x => x.URL).ToArray();

                var c = (ServiceStub)await node.ConnectService(urls, cancel: cancel).ConfigureAwait(false);

                MessageEntry rr_res;
                NodeID remote_nodeid;
                string remote_nodename;
                try
                {

                    remote_nodeid = c.rr_context.RemoteNodeID;
                    remote_nodename = c.rr_context.RemoteNodeName;

                    if (remote_nodeid != storage.info.NodeID || (!String.IsNullOrEmpty(storage.info.NodeName) && 
                        remote_nodename != storage.info.NodeName))
                    {
                        throw new InvalidOperationException("NodeID or NodeName mismatch");
                    }

                    var rr_req = new MessageEntry(MessageEntryType.FunctionCallReq, "GetLocalNodeServices");
                    rr_res = await c.ProcessRequest(rr_req, cancel).ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await node.DisconnectService(c).ConfigureAwait(false);
                    }
                    catch { }
                }

                if (rr_res == null)
                {
                    throw new InvalidOperationException("GetLocalNodeServices failed");
                }

                var o = new List<ServiceInfo2>();

                var me = rr_res.FindElement("return");

                if (me.ElementSize > 64 * 1024)
                {
                    throw new InvalidOperationException("GetLocalNodeServices response too large");
                }

                var service_list = (Dictionary<int,RobotRaconteurServiceIndex.ServiceInfo>)node.UnpackMapType<int,RobotRaconteurServiceIndex.ServiceInfo>(me.CastDataToNestedList(DataTypes.vector_t), null);

                if (service_list != null)
                {
                    foreach (var e in service_list)
                    {
                        var s = new ServiceInfo2();
                        s.NodeID = remote_nodeid;
                        s.NodeName = remote_nodename;
                        s.Name = e.Value.Name;
                        s.ConnectionURL = e.Value.ConnectionURL.Values.ToArray();
                        s.RootObjectType = e.Value.RootObjectType;
                        s.RootObjectImplements = e.Value.RootObjectImplements.Values.ToArray();
                        s.Attributes = e.Value.Attributes;
                        o.Add(s);
                    }
                }

                return Tuple.Create(true, o.ToArray());

            }
            finally
            {
                lock (storage)
                {
                    storage.service_info_updating = false;
                }
            }
        }


        internal async Task RetryUpdateServiceInfo(Discovery_nodestorage storage)
        {
            if (storage.service_info_updating)
            {
                return;
            }

            var now = DateTime.UtcNow;

            if (now > storage.retry_window_start + TimeSpan.FromSeconds(60))
            {
                storage.retry_window_start = now;
                storage.retry_count = 0;
            }

            var retry_count = storage.retry_count++;

            var r = new Random();
            var backoff = r.Next(100,600);
            if (retry_count > 3)
            {
                backoff = r.Next(2000, 2500);
            }
            if (retry_count > 5)
            {
                backoff = r.Next(4500, 5500);
            }
            if (retry_count > 8)
            {
                backoff = r.Next(9000, 11000);
            }
            if (retry_count > 12)
            {
                backoff = r.Next(25000, 35000);
            }

            if (string.IsNullOrEmpty(storage.info.ServiceStateNonce) && string.IsNullOrEmpty(storage.last_update_nonce)
                && storage.last_update_time != null)
            {
                backoff += 15000;
            }

            // TODO: log

            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(backoff + 10000);

            var ret = await DoUpdateServiceInfo(storage, storage.info.ServiceStateNonce, backoff, cancel.Token).ConfigureAwait(false);
            if (!ret.Item1)
            {
                return;
            }

            EndUpdateServiceInfo(storage, storage.info.ServiceStateNonce, ret.Item2);


        }

        internal void EndUpdateServiceInfo(Discovery_nodestorage storage, string nonce, ServiceInfo2[] service_info)
        {
            lock(m_DiscoveredNodes)
            {
                lock(storage)
                {
                    storage.services = service_info;
                    storage.last_update_time = DateTime.UtcNow;
                    storage.last_update_nonce = nonce;

                    if(storage.last_update_nonce != storage.info.ServiceStateNonce)
                    {
                        // We missed an update, do another refresh but delay 5 seconds to prevent flooding
                        Task.Run(async () =>
                        {
                            await Task.Delay(5000).ConfigureAwait(false);
                            _ = RetryUpdateServiceInfo(storage).IgnoreResult();
                        });
                    }
                    else
                    {
                        storage.retry_count = 0;
                    }
                }                

                // TODO: RobotRaconteurNode.FireNodeDetected
            }

            lock(subscriptions)
            {
                foreach(var s in subscriptions)
                {
                    Task.Run(() => s.NodeUpdated(storage)).IgnoreResult();
                }
            }

            
        }

        void CallUpdateServiceInfo(Discovery_nodestorage storage, string nonce)
        {
            Task.Run(async () =>
            {
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(10000);
                var ret = await DoUpdateServiceInfo(storage, nonce, 0, cancel.Token).ConfigureAwait(false);
                if (!ret.Item1)
                {
                    return;
                }

                EndUpdateServiceInfo(storage, nonce, ret.Item2);
            });
        }

        internal void SubscriptionClosed(IServiceSubscription sub)
        {
            lock(subscriptions)
            {
                subscriptions.Remove(sub);
            }
        }

        List<IServiceSubscription> subscriptions = new List<IServiceSubscription>();

        internal ServiceSubscription SubscribeService(string[] url, string username = null, Dictionary<string,object> credentials = null, string objecttype=null)
        {
            var s = new ServiceSubscription(this);
            s.InitServiceURL(url, username, credentials, objecttype);
            return s;
        }

        internal ServiceSubscription SubscribeServiceByType(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            var s = new ServiceSubscription(this);
            DoSubscribe(service_types, filter, s);
            return s;
        }

        internal ServiceInfo2Subscription SubscribeServiceInfo2(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            var s = new ServiceInfo2Subscription(this);
            DoSubscribe(service_types, filter, s);
            return s;
        }

        void DoSubscribe(string[] service_types, ServiceSubscriptionFilter filter, IServiceSubscription s)
        {

            lock (subscriptions)
            {
                subscriptions.Add(s);
                s.Init(service_types, filter);
            }

            DoUpdateAllDetectedServices(s);
        }

        internal void DoUpdateAllDetectedServices(IServiceSubscription s)
        {
            Discovery_nodestorage[] d;
            lock(m_DiscoveredNodes)
            {
                d = m_DiscoveredNodes.Values.ToArray();
            }

            foreach(Discovery_nodestorage n in d)
            {
                if (n.last_update_nonce != n.info.ServiceStateNonce || string.IsNullOrEmpty(n.info.ServiceStateNonce))
                {
                    RetryUpdateServiceInfo(n).IgnoreResult();
                }

                s.NodeUpdated(n);
            }
        }
    }
}
