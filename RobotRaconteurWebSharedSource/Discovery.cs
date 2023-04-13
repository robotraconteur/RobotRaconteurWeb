// Implement Discovery class in csharp based on RobotRaconteurCore cpp file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace RobotRaconteurWeb
{
    public class ServiceInfo2
    {
        public string Name;
        public string RootObjectType;
        public string[] RootObjectImplements;
        public string[] ConnectionURL;
        public Dictionary<string, object> Attributes;
        public NodeID NodeID;
        public string NodeName;

        public ServiceInfo2() { }

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

    public class NodeInfo2
    {
        public NodeID NodeID;
        public string NodeName;
        public string[] ConnectionURL;
    }


    namespace detail
    {

        class Discovery_nodestorage
        {
            public NodeDiscoveryInfo info;
            public ServiceInfo2[] services;
            public string last_update_nonce;
            public DateTime last_update_time;
            //public Discovery_updateserviceinfo updater;
            public Queue<string> recent_service_nonce;
            public DateTime retry_window_start;

        }

    }

    // Use Discovery_private.h and Discovery.cpp as reference

    public class Discovery
    {
        internal Dictionary<string, NodeDiscoveryInfo> m_DiscoveredNodes = new Dictionary<string, NodeDiscoveryInfo>();

        public Dictionary<string, NodeDiscoveryInfo> DiscoveredNodes { get { return m_DiscoveredNodes; } }

        internal RobotRaconteurNode node;
        internal Task cleandiscoverednodes_task;
        private CancellationTokenSource shutdown_token = new CancellationTokenSource();

        public Discovery(RobotRaconteurNode node)
        {
            this.node = node;
            cleandiscoverednodes_task =  PeriodicTask.Run(CleanDiscoveredNodes, TimeSpan.FromSeconds(5), shutdown_token.Token);
        }

        public void Shutdown()
        {
            shutdown_token.Cancel();
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


                        if (m_DiscoveredNodes.Keys.Contains(nodeid.ToString()))
                        {
                            NodeDiscoveryInfo i = m_DiscoveredNodes[nodeid.ToString()];
                            i.NodeName = nodename;
                            if (!i.URLs.Any(x => x.URL == url))
                            {
                                NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                                u.URL = url;
                                u.LastAnnounceTime = DateTime.UtcNow;
                                i.URLs.Add(u);
                                //Console.WriteLine(url);
                            }
                            else
                            {
                                i.URLs.First(x => x.URL == url).LastAnnounceTime = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            NodeDiscoveryInfo i = new NodeDiscoveryInfo();
                            i.NodeID = nodeid;
                            i.NodeName = nodename;
                            NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                            u.URL = url;
                            u.LastAnnounceTime = DateTime.UtcNow;
                            i.URLs.Add(u);
                            m_DiscoveredNodes.Add(nodeid.ToString(), i);
                        }
                    }

                    //RobotRaconteurNode.s.NodeAnnouncePacketReceived(packet);
                }

            }
            catch { };

            //Console.WriteLine(packet);
        }

        internal void NodeDetected(NodeDiscoveryInfo n)
        {
            try
            {
                lock (m_DiscoveredNodes)
                {
                    if (m_DiscoveredNodes.Keys.Contains(n.NodeID.ToString()))
                    {
                        NodeDiscoveryInfo i = m_DiscoveredNodes[n.NodeID.ToString()];
                        i.NodeName = n.NodeName;
                        foreach (var url in n.URLs)
                        {
                            if (!i.URLs.Any(x => x.URL == url.URL))
                            {
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
                    }
                    else
                    {
                        foreach (var u in n.URLs)
                        {
                            u.LastAnnounceTime = DateTime.UtcNow;
                        }
                        m_DiscoveredNodes.Add(n.NodeID.ToString(), n);
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
                        NodeDiscoveryInfoURL[] urls = m_DiscoveredNodes[key].URLs.ToArray();
                        foreach (NodeDiscoveryInfoURL u in urls)
                        {

                            double time = (now - u.LastAnnounceTime).TotalMilliseconds;
                            if (time < 60000)
                            {
                                newurls.Add(u);
                            }
                        }

                        m_DiscoveredNodes[key].URLs = newurls;

                        if (newurls.Count == 0)
                        {
                            m_DiscoveredNodes.Remove(key);
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

            await Task.WhenAll(tasks);

            foreach (var t2 in tasks)
            {
                try
                {
                    var info = await t2;
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
            RobotRaconteurServiceIndex.ServiceIndex ind = (RobotRaconteurServiceIndex.ServiceIndex)await node.ConnectService(urls.ToArray(), cancel: cancel);
            var NodeID = ((ServiceStub)ind).RRContext.RemoteNodeID.ToByteArray();
            var NodeName = ((ServiceStub)ind).RRContext.RemoteNodeName;
            var inf = await ind.GetLocalNodeServices(cancel);
            return Tuple.Create(NodeID, NodeName, inf);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindServiceByType(servicetype, transportschemes, cancel.Token);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes, CancellationToken cancel)
        {

            try
            {
                await UpdateDetectedNodes(cancel);
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
                        foreach (NodeDiscoveryInfoURL url in m_DiscoveredNodes[nodeids[i]].URLs)
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

                await waittask;
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

                    var inf = await info_wait[i];


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
            return await FindNodeByID(id, schemes, cancel.Token);
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel);
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
                    n.NodeName = ni.NodeName;

                    var c = new List<string>();

                    foreach (var url in ni.URLs)
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
            return await FindNodeByName(name, schemes, cancel.Token);
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel);
            }
            catch { };

            var o = new List<NodeInfo2>();

            lock (m_DiscoveredNodes)
            {
                foreach (var e in m_DiscoveredNodes)
                {
                    if (e.Value.NodeName == name)
                    {
                        var n = new NodeInfo2();
                        var nodeid_str = e.Value.NodeID.ToString();
                        n.NodeID = new NodeID(nodeid_str);
                        n.NodeName = e.Value.NodeName;

                        var c = new List<string>();

                        foreach (var url in e.Value.URLs)
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


    }

}
