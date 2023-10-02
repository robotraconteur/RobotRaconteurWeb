// Copyright 2011-2019 Wason Technology, LLC
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
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{

    public interface ITransportConnection
    {
        Task SendMessage(Message m, CancellationToken cancel);

        void Close();

        void CheckConnection(uint endpoint);

        uint LocalEndpoint { get; }

        uint RemoteEndpoint { get; }

        NodeID RemoteNodeID { get; }
    }


    public abstract class Transport
    {
        [ThreadStatic]
        protected internal static string m_CurrentThreadTransportConnectionURL = null;
        public static string CurrentThreadTransportConnectionURL { get { return m_CurrentThreadTransportConnectionURL; } }

        [ThreadStatic]
        protected internal static ITransportConnection m_CurrentThreadTransport = null;

        public static ITransportConnection CurrentThreadTransport { get { return m_CurrentThreadTransport; } }

        public event Action<Message> MessageReceivedEvent;

        protected internal uint TransportID;

        protected internal readonly RobotRaconteurNode node;

        protected Transport(RobotRaconteurNode node=null)
        {
            if (node != null)
            {

                this.node = node;
            }
            else
            {
                this.node = RobotRaconteurNode.s;
            }
        }

        public virtual void CheckConnection(uint endpoint)
        {

        }

        public abstract bool IsClient { get; }

        public abstract bool IsServer { get; }

        public abstract string[] UrlSchemeString { get; }

        //public abstract string Scheme { get; }

        public abstract bool CanConnectService(string url);

        
        public abstract Task<ITransportConnection> CreateTransportConnection(string url, Endpoint e, CancellationToken cancel);

        public abstract Task CloseTransportConnection(Endpoint e, CancellationToken cancel);

        

        public virtual Task SendMessage(Message m, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        protected internal virtual void MessageReceived(Message m)
        {


            MessageReceivedEvent(m);
        }

        protected internal async Task<Message> SpecialRequest(Message m)
        {
            if (m.entries.Count >= 1)
            {
                uint type = ((uint)m.entries[0].EntryType);
                if (type < 500 && (type % 2 == 1))
                {
                    Message r = await node.SpecialRequest(m, TransportID).ConfigureAwait(false);
                    return r;
                }
            }

            return null;
        }

        public virtual Task Close()
        {
            FireTransportEventListener(TransportListenerEventType.TransportClosed, null);
            return Task.FromResult(0);
        }


        public virtual uint TransportCapability(string name)
        {
            return 0;
        }

        public delegate void TransportListenerDelegate(Transport transport, TransportListenerEventType ev, object parameter);

        public event TransportListenerDelegate TransportListeners;


        protected void FireTransportEventListener(TransportListenerEventType ev, object parameter)
        {
            if (TransportListeners != null)
                try
                {
                    TransportListeners(this, ev, parameter);
                }
                catch { }
        }

        public virtual Task<List<NodeDiscoveryInfo>> GetDetectedNodes(CancellationToken token)
        {
            var o = new List<NodeDiscoveryInfo>();
            return Task.FromResult(o);
        }

        public virtual void LocalNodeServicesChanged()
        {

        }

        public virtual void SendDiscoveryRequest()
        {

        }

        public abstract string[] ServerListenUrls { get; }

    }

    public enum TransportListenerEventType
    {
        TransportClosed = 1,
        TransportConnectionClosed
    }


    public class ParseConnectionUrlResult
    {
        public string scheme;
        public string host;
        public int port = 0;
        public string path;
        public NodeID nodeid;
        public string nodename;
        public string service;
    }

    public static class TransportUtil
    {
        public static ParseConnectionUrlResult ParseConnectionUrl(string url)
        {
            var rr1 = new Regex("^([^:\\s]+)://(?:((?:\\[[A-Fa-f0-9\\:]+(?:\\%\\w*)?\\])|(?:[^\\[\\]\\:/\\?\\s]+))(?::([^:/\\?\\s]+))?|/)(?:/([^\\?\\s]*))?\\??([^\\s]*)$");
            var u1 = rr1.Match(url);

            if (!u1.Success)
                throw new ConnectionException("Invalid connection URL");

            if (!u1.Groups[1].Success)
                throw new ConnectionException("Invalid connection URL");

            var o = new ParseConnectionUrlResult();

            o.scheme = u1.Groups[1].Value;
            o.host = u1.Groups[2].Value;
            if (u1.Groups[3].Success && u1.Groups[3].Value != "")
            {
                o.port = int.Parse(u1.Groups[3].Value);
            }
            else
            {
                o.port = -1;
            }

            if (o.scheme == "tcp")
            {
                if (u1.Groups[5].Success && u1.Groups[5].Value != "")
                    throw new ConnectionException("Invalid connection URL");

                if (!(u1.Groups[4].Success && u1.Groups[4].Value != ""))
                    throw new ConnectionException("Invalid connection URL");

                var ap = u1.Groups[4].Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (ap.Length != 2) throw new ConnectionException("Invalid connection URL");
                string noden = ap[0];
                if (noden.Contains("{") || noden.Contains("["))
                {
                    o.nodeid = new NodeID(noden);
                    o.nodename = "";
                }
                else
                {
                    o.nodename = noden;
                    o.nodeid = NodeID.Any;
                }

                o.service = ap[1];
                o.path = "/";

                return o;
            }

            if (o.port == -1)
            {
                if (o.scheme == "rr+tcp" || o.scheme == "rrs+tcp")
                {
                    o.port = 48653;
                }
                if (o.scheme == "rr+ws" || o.scheme == "rrs+ws")
                {
                    o.port = 80;
                }
                if (o.scheme == "rr+wss" || o.scheme == "rrs+wss")
                {
                    o.port = 443;
                }
            }

            o.path = u1.Groups[4].Value;

            if (o.path == null || o.path == "") o.path = "/";

            var query_params = new Dictionary<string, string>();

            if (!(u1.Groups[5].Success && u1.Groups[5].Value != ""))
                throw new ConnectionException("Invalid connection URL");

            var q2 = new List<string>();
            foreach (var e in u1.Groups[5].Value.TrimStart(new char[] { '?' }).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var q3 = e.Split(new char[] { '=' });
                if (q3.Length != 2) throw new ConnectionException("Invalid Connection URL");
                if (q3[0].Length == 0 || q3[1].Length == 0) throw new ConnectionException("Invalid Connection URL");
                query_params.Add(q3[0], RRUriExtensions.UnescapeDataString(q3[1]));
            }

            if (!query_params.ContainsKey("service"))
            {
                throw new ConnectionException("Invalid Connection URL");
            }

            o.service = query_params["service"];
            if (String.IsNullOrWhiteSpace(o.service)) throw new ConnectionException("Invalid Connection URL");

            if (query_params.ContainsKey("nodeid"))
            {
                o.nodeid = new NodeID(query_params["nodeid"]);
            }
            else
            {
                o.nodeid = NodeID.Any;
            }

            if (query_params.ContainsKey("nodename"))
            {
                o.nodename = query_params["nodename"];
                var rr = new Regex("^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$");

                if (!rr.Match(o.nodename).Success)
                {
                    throw new ConnectionException("\"" + o.nodename + "\" is an invalid NodeName");
                }
            }
            else
            {
                o.nodename = "";
            }

            return o;
        }

    }

}
