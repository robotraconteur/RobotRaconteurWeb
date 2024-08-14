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
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

#pragma warning disable 1591

namespace RobotRaconteurWeb
{
    public abstract class Endpoint
    {
        public uint LocalEndpoint { get { return m_LocalEndpoint; } }
        protected internal uint m_LocalEndpoint = 0;

        public uint RemoteEndpoint { get { return m_RemoteEndpoint; } }
        protected internal uint m_RemoteEndpoint = 0;

        public string RemoteNodeName { get { return m_RemoteNodeName; } }
        protected string m_RemoteNodeName = "";

        public NodeID RemoteNodeID { get { return m_RemoteNodeID; } }
        protected internal NodeID m_RemoteNodeID = NodeID.Any;

        protected internal ITransportConnection TransportConnection = null;

        protected internal uint transport;

        protected internal DateTime LastMessageReceivedTime = DateTime.UtcNow;

        protected ushort MessageNumber = 0;

        protected object MessageNumberLock = new object();
        //public Transport transport;
        //public uint transport_id;

        protected internal readonly RobotRaconteurNode node;

        public Endpoint(RobotRaconteurNode node)
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

        public virtual async Task SendMessage(Message m, CancellationToken cancel)
        {                       
            if (m.header == null) m.header = new MessageHeader();

            if (m.entries.Count == 1 && (int)m.entries[0].EntryType <= 500)
            {
                m.header.ReceiverNodeName = RemoteNodeName;
                m.header.SenderNodeName = node.NodeName;
            }
            m.header.SenderEndpoint = LocalEndpoint;
            m.header.ReceiverEndpoint = RemoteEndpoint;

            m.header.SenderNodeID = node.NodeID;
            m.header.ReceiverNodeID = RemoteNodeID;


            lock (MessageNumberLock)
            {
                m.header.MessageID = MessageNumber;

                MessageNumber = (ushort)((MessageNumber == ((ushort)UInt16.MaxValue)) ? 0 : MessageNumber + 1);
            }

            await node.SendMessage(m, cancel).ConfigureAwait(false);


        }


        public abstract void MessageReceived(Message m);
              

        protected virtual void CheckEndpointCapabilityMessage(Message m)
        {
            uint capability = 0;
            MessageEntry e;

            Message ret = new Message();
            ret.header = new MessageHeader();
            ret.header.ReceiverNodeName = m.header.SenderNodeName;
            ret.header.SenderNodeName = node.NodeName;
            ret.header.ReceiverNodeID = m.header.SenderNodeID;
            ret.header.ReceiverEndpoint = m.header.SenderEndpoint;
            ret.header.SenderEndpoint = m.header.ReceiverEndpoint;
            ret.header.SenderNodeID = node.NodeID;


            try
            {

                if (m.entries.Count == 0) throw new InvalidOperationException();

                e = m.entries[0];

                MessageEntry eret = ret.AddEntry(MessageEntryType.EndpointCheckCapabilityRet, m.entries[0].MemberName);
                eret.RequestID = e.RequestID;
                eret.ServicePath = e.ServicePath;

                if (e.EntryType != MessageEntryType.EndpointCheckCapability) throw new InvalidOperationException();
                string name = e.MemberName;
                capability = EndpointCapability(name);
                eret.AddElement("return", capability);
            }
            catch
            {



            }

            SendMessage(ret,default(CancellationToken)).IgnoreResult();


        }


        public virtual uint EndpointCapability(string name)
        {
            return (uint)0;
        }

    }


}