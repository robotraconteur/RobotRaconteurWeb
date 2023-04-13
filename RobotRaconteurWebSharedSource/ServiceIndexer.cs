using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;


namespace RobotRaconteurWeb
{
    internal class ServiceIndexer : RobotRaconteurServiceIndex.ServiceIndex
    {

        protected readonly  RobotRaconteurNode node;

        public ServiceIndexer(RobotRaconteurNode node)
        {
            this.node = node;
        }

        public Task<Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>> GetLocalNodeServices(CancellationToken cancel=default(CancellationToken))
        {
            if (Transport.CurrentThreadTransportConnectionURL == null)
                throw new ServiceException("GetLocalNodeServices must be called through a transport that supports node discovery");

            Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo> o = new Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>();
            int count = 0;

            ServerContext[] sc;
            lock (node.services)
            {
                sc = node.services.Values.ToArray();
            }

            foreach (ServerContext c in sc)
            {
                RobotRaconteurServiceIndex.ServiceInfo s = new RobotRaconteurServiceIndex.ServiceInfo();
                s.Attributes = c.Attributes;
                s.Name = c.ServiceName;
                s.RootObjectType = c.RootObjectType;
                s.ConnectionURL = new Dictionary<int, string>();
                s.ConnectionURL.Add(1,Transport.CurrentThreadTransportConnectionURL + "?" + ("nodeid=" + node.NodeID.ToString().Trim(new char[] {'{','}'}) + "&service=" + RRUriExtensions.EscapeDataString(s.Name)));
                s.RootObjectImplements = new Dictionary<int, string>();
                
                List<string> implements=c.ServiceDef.ServiceDef().Objects[ServiceDefinitionUtil.SplitQualifiedName(c.RootObjectType).Item2].Implements;
                for (int i = 0; i < implements.Count; i++)
                {
                    s.RootObjectImplements.Add(i, implements[i]);
                }

                
                o.Add(count, s);
                count++;
            }

            return Task.FromResult(o);
        }

        public Task<Dictionary<int, RobotRaconteurServiceIndex.NodeInfo>> GetRoutedNodes(CancellationToken cancel = default(CancellationToken))
        {
            

            Dictionary<int, RobotRaconteurServiceIndex.NodeInfo> ret = new Dictionary<int, RobotRaconteurServiceIndex.NodeInfo>();

            
            return Task.FromResult(ret);

        }

        public Task<Dictionary<int, RobotRaconteurServiceIndex.NodeInfo>> GetDetectedNodes(CancellationToken cancel = default(CancellationToken))
        {
            
            lock (node.m_Discovery.m_DiscoveredNodes)
            {
            string[] nodeids = node.m_Discovery.m_DiscoveredNodes.Keys.ToArray();
            int len = nodeids.Length;

            Dictionary<int, RobotRaconteurServiceIndex.NodeInfo> ret = new Dictionary<int, RobotRaconteurServiceIndex.NodeInfo>();

            for (int i = 0; i < len; i++)
            {
                NodeDiscoveryInfo info = node.m_Discovery.m_DiscoveredNodes[nodeids[i]].info;

                RobotRaconteurServiceIndex.NodeInfo ii = new RobotRaconteurServiceIndex.NodeInfo();
                ii.NodeID = info.NodeID.ToByteArray();
                ii.NodeName = info.NodeName;

                Dictionary<int,string> curl=new Dictionary<int,string>();
                for (int j = 0; j < info.URLs.Count; j++ )
                {
                    curl.Add(j, info.URLs[j].URL);
                }

                ii.ServiceIndexConnectionURL=curl;
                ret.Add(i, ii);

            }
            return Task.FromResult(ret);

            }
        }

        public event Action LocalNodeServicesChanged;

    }
}