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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    //TODO: Add threading locks

    public abstract class Pipe<T>
    {        
        public const int ANY_INDEX = -1;

        public class PipeEndpoint
        {
            private uint send_packet_number = 0;
            private uint recv_packet_number = 0;

            private Pipe<T> parent;
            private int index;
            private Endpoint endpoint;
                        
            public int Index { get { return index; } }

            public uint Endpoint { get { return endpoint.LocalEndpoint; } }

            public bool RequestPacketAck = false;

            public PipeEndpoint(Pipe<T> parent, int index, Endpoint endpoint = null)
            {
                this.parent = parent;
                this.index = index;
                this.endpoint = endpoint;
            }

            private AsyncMutex send_mutex = new AsyncMutex();

            public async Task<uint> SendPacket(T packet, CancellationToken cancel = default(CancellationToken))
            {
                Task mutex = send_mutex.Enter();
                try
                {
                    await mutex;
                    send_packet_number = (send_packet_number < UInt32.MaxValue)
                        ? send_packet_number + 1 : 0;

                    await parent.SendPipePacket(packet, index, send_packet_number, RequestPacketAck, endpoint, cancel);
                    return send_packet_number;
                }
                finally
                {
                    send_mutex.Exit(mutex);
                }

            }
            
            public async Task Close()
            {
                await parent.Close(this);
            }

            private uint increment_packet_number(uint packetnum)
            {
                return (packetnum < UInt32.MaxValue) ? packetnum + 1 : 0;

            }

            private Dictionary<uint, T> out_of_order_packets = new Dictionary<uint, T>();
            private object recv_lock = new object();

            public event PipePacketReceivedCallbackFunction PacketReceivedEvent;

            AsyncValueWaiter<bool> recv_waiter = new AsyncValueWaiter<bool>();

            internal protected void PipePacketReceived(T packet, uint packetnum)
            {
                if (IgnoreInValue) return;
                lock (recv_lock)
                {
                    if (packetnum == increment_packet_number(recv_packet_number))
                    {
                        recv_packets.Enqueue(packet);
                        recv_packet_number = increment_packet_number(recv_packet_number);
                        if (out_of_order_packets.Count > 0)
                        {
                            while (out_of_order_packets.Keys.Contains(increment_packet_number(recv_packet_number)))
                            {
                                recv_packet_number = increment_packet_number(recv_packet_number);
                                T opacket = out_of_order_packets[recv_packet_number];
                                recv_packets.Enqueue(opacket);
                                out_of_order_packets.Remove(recv_packet_number);
                            }
                        }

                        recv_waiter.NotifyAll(true);

                        if (PacketReceivedEvent != null)
                        {
                            try
                            {
                                PacketReceivedEvent(this);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        out_of_order_packets.Add(packetnum, packet);
                    }
                }
            }

            public event PipePacketAckReceivedCallbackFunction PacketAckReceivedEvent;

            internal void PipePacketAckReceived(uint packetnum)
            {
                PacketAckReceivedEvent(this, packetnum);
            }

            private Queue<T> recv_packets = new Queue<T>();

            public int Available
            {
                get
                {
                    lock (recv_lock)
                    {
                        return recv_packets.Count;
                    }
                }
            }

            public T PeekNextPacket()
            {
                lock (recv_lock)
                {
                    return recv_packets.Peek();
                }
            }

            public T ReceivePacket()
            {
                lock (recv_lock)
                {
                    return recv_packets.Dequeue();
                }
            }

            public async Task<T> ReceivePacketWait(int timeout = -1, CancellationToken cancel = default(CancellationToken))
            {
                var ret = await TryReceivePacketWait(timeout, false, cancel);
                if (!ret.Item1)
                {
                    throw new InvalidOperationException("Receive queue empty");
                }
                return ret.Item2;
            }

            public async Task<T> PeekPacketWait(int timeout = -1, CancellationToken cancel = default(CancellationToken))
            {
                var ret = await TryReceivePacketWait(timeout, true, cancel);
                if (!ret.Item1)
                {
                    throw new InvalidOperationException("Receive queue empty");
                }
                return ret.Item2;
            }

            public async Task<Tuple<bool, T>> TryReceivePacketWait(int timeout = -1, bool peek = false, CancellationToken cancel = default)
            {
                AsyncValueWaiter<bool>.AsyncValueWaiterTask waiter;
                lock (recv_lock)
                {              
                    if (recv_packets.Count > 0)
                    {
                        if (!peek)
                        {
                            return Tuple.Create(true, recv_packets.Dequeue());
                        }
                        else
                        {
                            return Tuple.Create(true, recv_packets.Peek());
                        }

                    }
                    else if (timeout == 0)
                    {
                        return Tuple.Create(false, default(T));
                    }

                    waiter = recv_waiter.CreateWaiterTask(timeout, cancel);
                }

                await waiter.Task.ConfigureAwait(false);

                lock (recv_lock)
                {
                    if (recv_packets.Count > 0)
                    {
                        if (!peek)
                        {
                            return Tuple.Create(true, recv_packets.Dequeue());
                        }
                        else
                        {
                            return Tuple.Create(true, recv_packets.Peek());
                        }

                    }                    
                    return Tuple.Create(false, default(T));                    
                }
            }

            public bool IgnoreInValue { get; set; }

            private PipeDisconnectCallbackFunction close_callback;

            public PipeDisconnectCallbackFunction PipeCloseCallback
            {
                get { return close_callback; }
                set { close_callback = value; }
            }
            internal void RemoteClose()
            {
                if (close_callback != null)
                    try
                    {
                        close_callback(this);
                    }
                    catch { };
                try
                {
                    Close().IgnoreResult();
                }
                catch { }
            }
        }

        private bool rawelements = false;

        public Pipe()
        {
            if (typeof(T) == typeof(MessageElement))
                rawelements = true;

        }

        public abstract string MemberName { get; }

        public delegate void PipeConnectCallbackFunction(PipeEndpoint newpipe);

        public delegate void PipeDisconnectCallbackFunction(PipeEndpoint closedpipe);

        public delegate void PipePacketReceivedCallbackFunction(PipeEndpoint e);

        public delegate void PipePacketAckReceivedCallbackFunction(PipeEndpoint e, uint packetnum);

        protected abstract Task SendPipePacket(T packet, int index, uint packetnumber, bool requestack, Endpoint endpoint, CancellationToken cancel = default(CancellationToken));
        
        public abstract Task<PipeEndpoint> Connect(int index, CancellationToken cancel = default(CancellationToken));

        public abstract PipeConnectCallbackFunction PipeConnectCallback { get; set; }

        public abstract void PipePacketReceived(MessageEntry m, Endpoint e = null);

        public abstract void Shutdown();

        protected abstract Task Close(PipeEndpoint e, Endpoint ee = null, CancellationToken cancel = default(CancellationToken));

        protected void DispatchPacketAck(MessageElement me, PipeEndpoint e)
        {
            uint pnum = me.CastData<uint[]>()[0];
            e.PipePacketAckReceived(pnum);
        }

        protected bool DispatchPacket(MessageElement me, PipeEndpoint e, out uint packetnumber)
        {
            int index = Int32.Parse(me.ElementName);
            List<MessageElement> elems = (me.CastDataToNestedList()).Elements;
            packetnumber = (MessageElement.FindElement(elems, "packetnumber").CastData<uint[]>())[0];
            object data;
            if (!rawelements)
                data = UnpackAnyType(MessageElement.FindElement(elems, "packet"));
            else
                data = MessageElement.FindElement(elems, "packet");
            e.PipePacketReceived((T)data, packetnumber);

            bool requestack=(elems.Any(x => x.ElementName == "requestack"));
            return requestack;
        }
        
        protected MessageElement PackPacket(T data, int index, uint packetnumber, bool requestack)
        {
            List<MessageElement> elems = new List<MessageElement>();
            elems.Add(new MessageElement("packetnumber", packetnumber));
            if (!rawelements)
            {
                object pdata = PackAnyType(ref data);
                elems.Add(new MessageElement("packet", pdata));
            }
            else
            {
                MessageElement pme = ((MessageElement)(object)data);
                pme.ElementName = "packet";
                elems.Add(pme);
            }

            if (requestack)
            {
                elems.Add(new MessageElement("requestack", new int[] { 1 }));
            }

            var delems = new MessageElementNestedElementList(DataTypes.dictionary_t, "", elems);
            
            MessageElement me = new MessageElement(index.ToString(), delems);

            return me;
        }
        
        protected abstract void DeleteEndpoint(PipeEndpoint e);

        protected abstract object PackAnyType(ref T o);

        protected abstract T UnpackAnyType(MessageElement o);

    }

    public sealed class PipeClient<T> : Pipe<T>
    {

        private Dictionary<int, PipeEndpoint> pipeendpoints = new Dictionary<int, PipeEndpoint>();

        private string m_Name;
        
        public override string MemberName { get { return m_Name; } }

        private ServiceStub stub;

        public PipeClient(string name, ServiceStub stub)
        {
            m_Name = name;
            this.stub = stub;
            stub.RRContext.ClientServiceListener += ClientContextListener;
        }
        
        protected override async Task SendPipePacket(T data, int index, uint packetnumber, bool requestack, Endpoint e = null, CancellationToken cancel = default(CancellationToken))
        {
            MessageElement me = PackPacket(data, index, packetnumber, requestack);
            MessageEntry m = new MessageEntry(MessageEntryType.PipePacket, MemberName);
            m.AddElement(me);
            await stub.SendPipeMessage(m, cancel);
        }

        List<Tuple<int,object>> connecting = new List<Tuple<int,object>>();
        Dictionary<int,PipeEndpoint> early_endpoints = new Dictionary<int,PipeEndpoint>();

        public override async Task<PipeEndpoint> Connect(int index, CancellationToken cancel = default(CancellationToken))
        {
            object connecting_key = new object();
            connecting.Add(Tuple.Create(index,connecting_key));
            int rindex=-1;
            try
            {
                MessageEntry m = new MessageEntry(MessageEntryType.PipeConnectReq, MemberName);
                m.AddElement("index", index);
                MessageEntry ret = await stub.ProcessRequest(m, cancel);

                rindex = (ret.FindElement("index").CastData<int[]>())[0];

                PipeEndpoint e;
                if (early_endpoints.ContainsKey(rindex))
                {
                    e = early_endpoints[rindex];
                    early_endpoints.Remove(rindex);
                }
                else
                {
                    e= new PipeEndpoint(this, rindex);
                }
                pipeendpoints.Add(rindex, e);
                return e;
            }
            finally
            {
                connecting.RemoveAll(x => Object.ReferenceEquals(x.Item2, connecting_key));
                if (connecting.Count == 0)
                {
                    early_endpoints.Clear();
                }
            }
        }
        
        protected override async Task Close(PipeEndpoint e, Endpoint ee = null, CancellationToken cancel = default(CancellationToken))
        {
            MessageEntry m = new MessageEntry(MessageEntryType.PipeDisconnectReq, MemberName);
            m.AddElement("index", e.Index);
            MessageEntry ret = await stub.ProcessRequest(m, cancel);
        }
        
        public override PipeConnectCallbackFunction PipeConnectCallback
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }
        
        public override void PipePacketReceived(MessageEntry m, Endpoint e = null)
        {
            if (m.EntryType == MessageEntryType.PipeClosed)
            {
                try
                {

                    int index = (m.FindElement("index").CastData<int[]>())[0];
                    pipeendpoints[index].RemoteClose();
                }
                catch { };
            }
            else if (m.EntryType == MessageEntryType.PipePacket)
            {
                List<MessageElement> ack = new List<MessageElement>();
                foreach (MessageElement me in m.elements)
                {
                    try
                    {

                        int index = Int32.Parse(me.ElementName);
                        uint pnum;

                        PipeEndpoint p=null;
                        if (pipeendpoints.ContainsKey(index))
                        {
                            p = pipeendpoints[index];
                        }
                        else
                        {
                            if (early_endpoints.ContainsKey(index))
                            {
                                p = early_endpoints[index];
                            }
                            else
                            if (connecting.Count > 0)
                            {
                                if (connecting.Any(x => x.Item1 == -1 || x.Item1 == index))
                                {
                                    p = new PipeEndpoint(this, index);
                                    early_endpoints.Add(index, p);
                                }
                            }
                        }

                        if (p == null) continue;
                        if (DispatchPacket(me, p, out pnum))
                        {
                            ack.Add(new MessageElement(me.ElementName, new uint[] { pnum }));
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                try
                {
                    if (ack.Count > 0)
                    {
                        MessageEntry mack = new MessageEntry(MessageEntryType.PipePacketRet, m.MemberName);
                        mack.elements = ack;
                        stub.SendPipeMessage(mack, default(CancellationToken)).IgnoreResult();
                    }
                }
                catch { }
            }
            else if (m.EntryType == MessageEntryType.PipePacketRet)
            {
                try
                {
                    foreach (MessageElement me in m.elements)
                    {
                        int index = Int32.Parse(me.ElementName);
                        DispatchPacketAck(me, pipeendpoints[index]);
                    }
                }
                catch { }
            }
        }
        
        public override void Shutdown()
        {
            Pipe<T>.PipeEndpoint[] endps = pipeendpoints.Values.ToArray();
            foreach (Pipe<T>.PipeEndpoint e in endps)
            {
                try
                {
                    e.RemoteClose();
                }
                catch { }
            }
        }
        
        protected override void DeleteEndpoint(PipeEndpoint e)
        {
            pipeendpoints.Remove(e.Index);
        }

        internal void ClientContextListener(ClientContext context, ClientServiceListenerEventType event_, object param)
        {
            if (event_ == ClientServiceListenerEventType.ClientClosed)
            {
                Shutdown();
            }
        }

        protected override object PackAnyType(ref T o)
        {
            return stub.RRContext.PackAnyType<T>(ref o);
        }

        protected override T UnpackAnyType(MessageElement o)
        {
            return stub.RRContext.UnpackAnyType<T>(o);
        }
    }
    
    public sealed class PipeServer<T> : Pipe<T>
    {

        private Dictionary<uint, Dictionary<int, PipeEndpoint>> pipeendpoints = new Dictionary<uint, Dictionary<int, PipeEndpoint>>();

        private PipeConnectCallbackFunction callback;        

        private string m_Name;
        
        public override string MemberName { get { return m_Name; } }

        private ServiceSkel skel;

        public PipeServer(string name, ServiceSkel skel)
        {
            m_Name = name;
            this.skel = skel;
        }
        
        protected override async Task SendPipePacket(T data, int index, uint packetnumber, bool requestack, Endpoint e = null, CancellationToken cancel = default(CancellationToken))
        {
            if (!pipeendpoints.ContainsKey(e.LocalEndpoint)) throw new Exception("Pipe has been disconnect");
            if (!pipeendpoints[e.LocalEndpoint].ContainsKey(index)) throw new Exception("Pipe has been disconnected");

            MessageElement me = PackPacket(data, index, packetnumber, requestack);
            MessageEntry m = new MessageEntry(MessageEntryType.PipePacket, MemberName);
            m.AddElement(me);

            await skel.SendPipeMessage(m, e, cancel);
        }
        
        public override Task<PipeEndpoint> Connect(int endpoint, CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException();
        }
        
        protected override async Task Close(PipeEndpoint e, Endpoint ee, CancellationToken cancel = default(CancellationToken))
        {
            MessageEntry m = new MessageEntry(MessageEntryType.PipeClosed, MemberName);
            m.AddElement("index", e.Index);
            await skel.SendPipeMessage(m, ee, cancel);

            DeleteEndpoint(e);            
        }
        
        public override PipeConnectCallbackFunction PipeConnectCallback
        {
            get { return callback; }
            set { callback = value; }
        }
        
        public override void PipePacketReceived(MessageEntry m, Endpoint e = null)
        {

            if (m.EntryType == MessageEntryType.PipePacket)
            {
                List<MessageElement> ack = new List<MessageElement>();
                foreach (MessageElement me in m.elements)
                {
                    try
                    {
                        int index = Int32.Parse(me.ElementName);
                        uint pnum;
                        if (DispatchPacket(me, pipeendpoints[e.LocalEndpoint][index], out pnum))
                        {
                            ack.Add(new MessageElement(me.ElementName, new uint[] { pnum }));
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                try
                {
                    if (ack.Count > 0)
                    {
                        MessageEntry mack = new MessageEntry(MessageEntryType.PipePacketRet, m.MemberName);
                        mack.elements = ack;
                        skel.SendPipeMessage(mack, e, default(CancellationToken)).IgnoreResult();

                    }
                }
                catch { }
            }
            else if (m.EntryType == MessageEntryType.PipePacketRet)
            {

                try
                {
                    foreach (MessageElement me in m.elements)
                    {
                        int index = Int32.Parse(me.ElementName);
                        DispatchPacketAck(me, pipeendpoints[e.LocalEndpoint][index]);
                    }
                }
                catch { }

            }

        }

        object pipeendpointlock = new object();
        
        public Task<MessageEntry> PipeCommand(MessageEntry m, Endpoint e)
        {
            lock (pipeendpointlock)
            {
                switch (m.EntryType)
                {
                    case MessageEntryType.PipeConnectReq:
                        {
                            if (!pipeendpoints.Keys.Contains(e.LocalEndpoint))
                                pipeendpoints.Add(e.LocalEndpoint, new Dictionary<int, PipeEndpoint>());

                            Dictionary<int, PipeEndpoint> ep = pipeendpoints[e.LocalEndpoint];

                            int index = (m.FindElement("index").CastData<int[]>())[0];
                            if (index == -1)
                                index = (ep.Count == 0) ? 1 : (ep.Keys.Max() + 1);

                            if (ep.Keys.Contains(index)) throw new Exception("Pipe endpoint index in use");

                            PipeEndpoint p = new PipeEndpoint(this, index, e);
                            ep.Add(index, p);
                            try
                            {
                                if (callback != null) callback(p);
                            }
                            catch { }

                            MessageEntry ret = new MessageEntry(MessageEntryType.PipeConnectRet, MemberName);
                            ret.AddElement("index", index);
                            return Task.FromResult(ret);
                        }

                    case MessageEntryType.PipeDisconnectReq:
                        {
                            if (!pipeendpoints.Keys.Contains(e.LocalEndpoint)) throw new Exception("Invalid pipe");
                            Dictionary<int, PipeEndpoint> ep = pipeendpoints[e.LocalEndpoint];

                            int index = (m.FindElement("index").CastData<int[]>())[0];
                            if (!ep.Keys.Contains(index)) throw new Exception("Invalid pipe");


                            ep.Remove(index);

                            return Task.FromResult(new MessageEntry(MessageEntryType.PipeDisconnectReq, MemberName));
                        }
                    default:
                        throw new Exception("Invalid Command");

                }
            }
        }
        
        public override void Shutdown()
        {
            lock (pipeendpointlock)
            {
                //Cycle through and close all endpoints
                Dictionary<int, Pipe<T>.PipeEndpoint>[] endpoints1 = pipeendpoints.Values.ToArray();
                foreach (Dictionary<int, Pipe<T>.PipeEndpoint> endpoints2 in endpoints1)
                {
                    Pipe<T>.PipeEndpoint[] endpoints3 = endpoints2.Values.ToArray();
                    foreach (Pipe<T>.PipeEndpoint endpoint in endpoints3)
                    {
                        try
                        {
                            endpoint.Close().IgnoreResult();
                        }
                        catch { }
                    }

                    endpoints2.Clear();

                }

                pipeendpoints.Clear();
            }
        }
        
        protected override void DeleteEndpoint(PipeEndpoint e)
        {
            lock (pipeendpointlock)
            {
                pipeendpoints[e.Endpoint].Remove(e.Index);
            }
        }

        protected override object PackAnyType(ref T o)
        {
            return skel.RRContext.PackAnyType<T>(ref o);
        }

        protected override T UnpackAnyType(MessageElement o)
        {
            return skel.RRContext.UnpackAnyType<T>(o);
        }
    }

    public class PipeBroadcaster<T>
    {
        protected Pipe<T> pipe;
        protected List<connected_endpoint> endpoints = new List<connected_endpoint>();
        protected int maximum_backlog;

        protected class connected_endpoint
        {
            public Pipe<T>.PipeEndpoint ep;
            public List<uint> backlog = new List<uint>();
            public List<uint> forward_backlog = new List<uint>();
            public bool sending = false;
            public List<object> active_sends = new List<object>();

            public connected_endpoint(Pipe<T>.PipeEndpoint ep)
            {
                this.ep = ep;
            }
        }

        public Pipe<T> Pipe { get => (Pipe<T>)pipe; }

        public PipeBroadcaster(Pipe<T> pipe, int maximum_backlog = -1)
        {
            this.pipe = pipe;
            this.maximum_backlog = maximum_backlog;
            pipe.PipeConnectCallback = EndpointConnected;
        }

        protected void EndpointConnected(Pipe<T>.PipeEndpoint ep)
        {
            lock (endpoints)
            {
                connected_endpoint cep = new connected_endpoint(ep);
                ep.PipeCloseCallback = delegate(Pipe<T>.PipeEndpoint ep1) { EndpointClosed(cep); };
                ep.PacketReceivedEvent += PacketReceived;
                ep.PacketAckReceivedEvent += delegate(Pipe<T>.PipeEndpoint ep1, uint pnum) { PacketAckReceived(cep, pnum); };
                ep.RequestPacketAck = true;
                endpoints.Add(cep);
            }
        }

        protected void EndpointClosed(connected_endpoint ep)
        {
            lock (endpoints)
            {

                try
                {
                    endpoints.Remove(ep);
                }
                catch { }
            }
        }

        protected void PacketReceived(Pipe<T>.PipeEndpoint ep)
        {
            //Receive packets and discard.
            lock (endpoints)
            {
                try
                {
                    while (ep.Available > 0)
                    {
                        ep.ReceivePacket();
                    }
                }
                catch { }
            }
        }

        protected void PacketAckReceived(connected_endpoint ep, uint pnum)
        {
            lock (endpoints)
            {
                try
                {
                    if (ep.backlog.Count(x => x == pnum) == 0)
                    {
                        ep.forward_backlog.Add(pnum);
                    }
                    else
                    {
                        ep.backlog.Remove(pnum);
                    }
                }
                catch { }
            }
        }

        public async Task AsyncSendPacket(T packet, CancellationToken cancel=default(CancellationToken))
        {
            List<connected_endpoint> endpoints1 = new List<connected_endpoint>();
            lock (endpoints)
            {
                endpoints.ForEach(item => endpoints1.Add(item));
            }

            var eps=new List<Tuple<Task<uint>,connected_endpoint,object>>();

            foreach (connected_endpoint cep in endpoints1)
            {
                try
                {
                    Pipe<T>.PipeEndpoint ep = cep.ep as Pipe<T>.PipeEndpoint;
                    if (ep == null)
                    {
                        lock (endpoints)
                        {
                            endpoints.Remove(cep);

                        }
                        continue;
                    }

                    if(Predicate != null && !Predicate(this, ep.Endpoint, ep.Index))
                    {
                        continue;
                    }
                    lock (endpoints)
                    {
                        if (maximum_backlog != -1 && cep.backlog.Count + cep.active_sends.Count > maximum_backlog)
                        {
                            continue;
                        }
                    }
                    var t = ep.SendPacket(packet, cancel);
                    var send_key = new object();

                    lock (endpoints)
                    {
                        cep.active_sends.Add(send_key);
                    }
                    eps.Add(Tuple.Create(t,cep,send_key));
                }
                catch { }
            }

            while (eps.Count > 0)
            {
                await Task.WhenAny(eps.Select(x => x.Item1).ToArray());

                for (int i = 0; i < eps.Count; )
                {
                    var t=eps[i].Item1;
                    var cep1 = eps[i].Item2;
                    if (t.IsCompleted || t.IsFaulted || t.IsCanceled)
                    {
                        try
                        {
                            uint pnum = await t;
                            lock (endpoints)
                            {
                                cep1.active_sends.Remove(eps[i].Item3);
                                if (maximum_backlog != -1)
                                {                                
                                    if (cep1.forward_backlog.Count(x => x == pnum) != 0)
                                    {
                                        cep1.forward_backlog.Remove(pnum);
                                    }
                                    else
                                    {
                                        cep1.backlog.Add(pnum);
                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {
                            lock (endpoints)
                            {
                                endpoints.Remove(cep1);
                            }
                        }

                        eps.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public int MaximumBacklog
        {
            get => maximum_backlog;
            set
            {
                lock (endpoints)
                {
                    if (endpoints.Count > 0)
                    {
                        throw new InvalidOperationException("Cannot change MaximumBacklog while endpoints are connected");
                    }
                    maximum_backlog = value;
                }
            }
        }

        public Func<object, uint, int, bool> Predicate { get; set; }
    }
}