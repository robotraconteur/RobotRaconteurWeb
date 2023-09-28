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
    public abstract class Wire<T>
    {



        public class WireConnection
        {
            protected Endpoint endpoint;

            public uint Endpoint { get { return endpoint.LocalEndpoint; } }

            protected T inval;
            protected bool inval_valid = false;
            protected TimeSpec last_sendtime = new TimeSpec(0, 0);

            protected Wire<T> parent;

            public WireConnection(Wire<T> parent, Endpoint endpoint = null)
            {
                this.parent = parent;
                this.endpoint = endpoint;
            }

            protected object sendlock = new object();

            public virtual T InValue
            {
                get
                {
                    if (!inval_valid) throw new Exception("Value not set");
                    if (IsValueExpired(lasttime_recv_local, InValueLifespan))
                    {
                        throw new Exception("Value not set");
                    }
                    return inval;
                }               
            }

            public bool TryGetInValue(out T value)
            {
                value = default;
                if (!inval_valid) return false;
                if (IsValueExpired(lasttime_recv_local, InValueLifespan))
                {
                    return false;
                }
                value = inval;
                return true;
            }

            protected T outval;
            protected bool outval_valid=false;
            protected TimeSpec lasttime_send;
            protected DateTime lasttime_send_local;
            protected bool send_closed = false;

            public virtual T OutValue
            {
                get
                {
                    if (!outval_valid) throw new Exception("Value not set");
                    if (IsValueExpired(lasttime_send_local, OutValueLifespan))
                    {
                        throw new Exception("Value not set");
                    }
                    return outval;
                }
                set
                {
                    SendOutValue(value).IgnoreResult();
                }

            }

            public bool TryGetOutValue(out T value)
            {
                value = default;
                if (!outval_valid) return false;
                if (IsValueExpired(lasttime_send_local, OutValueLifespan))
                {
                    return false;
                }
                value = outval;
                return true;
            }

            public Task SendOutValue(T value)
            {
                lock (sendlock)
                {
                    if (send_closed)
                    {
                        throw new InvalidOperationException("Wire has been closed");
                    }

                    TimeSpec time = TimeSpec.Now;
                    if (time == last_sendtime)
                    {
                        time.nanoseconds += 1;
                    }


                    Task t = parent.SendWirePacket(value, time, endpoint);
                    last_sendtime = time;
                    outval = value;
                    outval_valid = true;
                    lasttime_send = TimeSpec.Now;
                    lasttime_send_local = DateTime.UtcNow;
                    inval_waiter.NotifyAll(true);
                    return t;
                }
            }

            public virtual TimeSpec LastValueReceivedTime
            {
                get
                {
                    if (!inval_valid) throw new Exception("No value received");
                    return lasttime_recv;
                }
            }

            public virtual TimeSpec LastValueSentTime
            {
                get
                {
                    if (!outval_valid) throw new Exception("No value sent");
                    return lasttime_send;
                }
            }


            public virtual Task Close()
            {
                lock(sendlock)
                {
                    send_closed = true;
                }

                return parent.Close(this);
            }

            public event WireValueChangedFunction WireValueChanged;

            private object recv_lock = new object();

            private TimeSpec lasttime_recv = null;
            private DateTime lasttime_recv_local = default;

            protected internal virtual void WirePacketReceived(TimeSpec timespec, T packet)
            {
                lock (recv_lock)
                {
                    if (IgnoreInValue) return;


                    if (lasttime_recv == null || timespec > lasttime_recv)
                    {
                        lasttime_recv = timespec;
                        lasttime_recv_local = DateTime.UtcNow;
                        inval = packet;
                        inval_valid = true;

                        inval_waiter.NotifyAll(true);

                        try
                        {
                            if (WireValueChanged != null)
                            {
                                WireValueChanged(this, packet, timespec);
                            }
                        }
                        catch { }

                    }
                }
            }

            private WireDisconnectCallbackFunction close_callback;

            public WireDisconnectCallbackFunction WireCloseCallback
            {
                get { return close_callback; }
                set { close_callback = value; }
            }


            internal protected virtual void RemoteClose()
            {
                if (close_callback != null)
                {
                    try
                    {
                        close_callback(this);
                    }
                    catch { };
                }

                Close();
            }

            public bool IgnoreInValue { get; set; } = false;

            public int InValueLifespan { get; set; } = -1;

            public int OutValueLifespan { get; set; } = -1;

            public const int RR_VALUE_LIFESPAN_INFINITE = -1;

            internal static bool IsValueExpired(DateTime recv_time,int lifespan)
            {
                if (lifespan< 0)
                {
                    return false;
                }
                 
                if (recv_time + TimeSpan.FromMilliseconds(lifespan) < DateTime.UtcNow)
                {
                    return true;
                }
                return false;
            }

            AsyncValueWaiter<bool> inval_waiter = new AsyncValueWaiter<bool>();
            AsyncValueWaiter<bool> outval_waiter = new AsyncValueWaiter<bool>();

            public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken token = default)
            {
                var waiter = inval_waiter.CreateWaiterTask(timeout, token);
                using (waiter)
                {
                    await waiter.Task.ConfigureAwait(false);
                }

                return inval_valid && !IsValueExpired(lasttime_recv_local, InValueLifespan);
            }

            public async Task<bool> WaitOutValueValid(int timeout = -1, CancellationToken token = default)
            {
                var waiter = outval_waiter.CreateWaiterTask(timeout, token);
                using (waiter)
                {
                    await waiter.Task.ConfigureAwait(false);
                }

                return outval_valid && !IsValueExpired(lasttime_send_local, OutValueLifespan);
            }

            public bool InValueValid {  get { return inval_valid && !IsValueExpired(lasttime_recv_local, InValueLifespan); } }

            public bool OutValueValid { get { return outval_valid && !IsValueExpired(lasttime_send_local, OutValueLifespan); } }
        }

        private bool rawelements = false;

        public abstract Task<WireConnection> Connect(CancellationToken cancel = default(CancellationToken));

        public delegate void WireConnectCallbackFunction(Wire<T> wire, WireConnection connection);

        public delegate void WireDisconnectCallbackFunction(WireConnection wire);

        public abstract WireConnectCallbackFunction WireConnectCallback { get; set; }

        public delegate void WireValueChangedFunction(WireConnection connection, T value, TimeSpec time);

        public abstract string MemberName { get; }

        protected MemberDefinition_Direction direction = MemberDefinition_Direction.both;

        public MemberDefinition_Direction Direction
        {
            get { return direction; }
        }

        public Wire()
        {
            if (typeof(T) == typeof(MessageElement))
                rawelements = true;
        }


        protected T UnpackPacket(List<MessageElement> me, out TimeSpec timespec)
        {
            var s = (MessageElement.FindElement(me, "packettime").CastDataToNestedList(DataTypes.structure_t));
            long seconds = MessageElement.FindElement(s.Elements, "seconds").CastData<long[]>()[0];
            int nanoseconds = MessageElement.FindElement(s.Elements, "nanoseconds").CastData<int[]>()[0];
            timespec = new TimeSpec(seconds, nanoseconds);
            object data;
            if (!rawelements)
                data = UnpackAnyType(MessageElement.FindElement(me, "packet"));
            else
                data = MessageElement.FindElement(me, "packet");

            return (T)data;
        }

        protected void DispatchPacket(List<MessageElement> me, WireConnection e)
        {            
            TimeSpec timespec;
            var data = UnpackPacket(me, out timespec);
            
            e.WirePacketReceived(timespec, (T)data);
        }
        protected List<MessageElement> PackPacket(T data, TimeSpec time)
        {

            List<MessageElement> timespec1 = new List<MessageElement>();
            timespec1.Add(new MessageElement("seconds", time.seconds));
            timespec1.Add(new MessageElement("nanoseconds", time.nanoseconds));
            var s = new MessageElementNestedElementList(DataTypes.structure_t, "RobotRaconteur.TimeSpec", timespec1);


            List<MessageElement> elems = new List<MessageElement>();
            elems.Add(new MessageElement("packettime", s));
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


            return elems;
        }

        public abstract Task SendWirePacket(T packet, TimeSpec time, Endpoint e = null);

        public abstract void WirePacketReceived(MessageEntry m, Endpoint e = null);

        protected abstract Task Close(WireConnection c, Endpoint e = null, CancellationToken cancel=default(CancellationToken));

        public abstract void Shutdown();

        protected abstract object PackAnyType(ref T o);

        protected abstract object UnpackAnyType(MessageElement o);

        public abstract Task<Tuple<T, TimeSpec>> PeekInValue(CancellationToken cancel = default(CancellationToken));
        public abstract Task<Tuple<T, TimeSpec>> PeekOutValue(CancellationToken cancel = default(CancellationToken));
        public abstract Task PokeOutValue(T value, CancellationToken cancel = default(CancellationToken));

        public abstract Func<uint, T> PeekInValueCallback { get; set; }
        public abstract Func<uint, T> PeekOutValueCallback { get; set; }
        public abstract Action<T, TimeSpec, uint> PokeOutValueCallback { get; set; }
    }


    
    public class WireClient<T> : Wire<T>
    {
        protected internal ServiceStub stub;
        protected internal WireConnection connection = null;

        protected string m_MemberName;

        public override string MemberName { get { return m_MemberName; } }

        public WireClient(string name, ServiceStub stub)
        {
            m_MemberName = name;
            this.stub = stub;
            stub.RRContext.ClientServiceListener += ClientContextListener;
        }

        protected internal object connect_lock = new object();

        public async override Task<WireConnection> Connect(CancellationToken cancel=default(CancellationToken))
        {
            
                try
                {
                    lock (connect_lock)
                    {
                        if (connection != null) throw new InvalidOperationException("Already connected");
                        connection = new WireConnection(this);
                        
                    }
                    MessageEntry m = new MessageEntry(MessageEntryType.WireConnectReq, MemberName);
                    MessageEntry ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);


                    return connection;
                }
                catch (Exception e)
                {
                    connection = null;
                    throw e;
                }
            
        }

        private AsyncMutex Close_mutex = new AsyncMutex();

        protected override async Task Close(WireConnection c, Endpoint e = null, CancellationToken cancel = default(CancellationToken))
        {
            Task mutex = Close_mutex.Enter();
            try
            {
                await mutex.ConfigureAwait(false);
                MessageEntry m = new MessageEntry(MessageEntryType.WireDisconnectReq, MemberName);
                MessageEntry ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
                connection = null;

            }
            finally
            {
                Close_mutex.Exit(mutex);
            }
        }

        public override WireConnectCallbackFunction WireConnectCallback
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public override Func<uint, T> PeekInValueCallback { get => throw new InvalidOperationException("Invalid for wire client"); set => throw new InvalidOperationException("Invalid for wire client"); }
        public override Func<uint, T> PeekOutValueCallback { get => throw new InvalidOperationException("Invalid for wire client"); set => throw new InvalidOperationException("Invalid for wire client"); }
        public override Action<T, TimeSpec, uint> PokeOutValueCallback { get => throw new InvalidOperationException("Invalid for wire client"); set => throw new InvalidOperationException("Invalid for wire client"); }

        public override void WirePacketReceived(MessageEntry m, Endpoint e = null)
        {
            if (m.EntryType == MessageEntryType.WireClosed)
            {
                try
                {
                    connection.Close();
                    connection = null;
                }
                catch { }
            }
            else if (m.EntryType == MessageEntryType.WirePacket)
            {
                try
                {
                    if (connection == null) return;
                    DispatchPacket(m.elements, connection);
                }
                catch { }
            }
        }



        public override Task SendWirePacket(T packet, TimeSpec time, Endpoint e = null)
        {
            List<MessageElement> el = PackPacket(packet, time);
            MessageEntry m = new MessageEntry(MessageEntryType.WirePacket, MemberName);
            m.elements = el;
            return stub.SendWireMessage(m, default(CancellationToken));
        }

        protected virtual void ClientContextListener(ClientContext context, ClientServiceListenerEventType event_, object param)
        {
            if (event_ == ClientServiceListenerEventType.ClientClosed)
            {
                Shutdown();
            }
        }

        public override void Shutdown()
        {
            try
            {
                if (connection != null)
                    connection.RemoteClose();
            }
            catch { }
        }

        protected override object PackAnyType(ref T o)
        {
            return stub.RRContext.PackAnyType<T>(ref o);
        }

        protected override object UnpackAnyType(MessageElement o)
        {
            return stub.RRContext.UnpackAnyType<T>(o);
        }

        public override async Task<Tuple<T, TimeSpec>> PeekInValue(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.WirePeekInValueReq, MemberName);
            var mr = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            TimeSpec ts;
            var data =  UnpackPacket(mr.elements, out ts);
            return Tuple.Create(data, ts);
        }

        public override async Task<Tuple<T, TimeSpec>> PeekOutValue(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.WirePeekOutValueReq, MemberName);
            var mr = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            TimeSpec ts;
            var data = UnpackPacket(mr.elements, out ts);
            return Tuple.Create(data, ts);
        }

        public override async Task PokeOutValue(T value, CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.WirePokeOutValueReq, MemberName);
            m.elements = PackPacket(value, TimeSpec.Now);
            await stub.ProcessRequest(m, cancel).ConfigureAwait(false);            
        }
    }

    public class WireServer<T> : Wire<T>
    {

        protected string m_MemberName;
        protected ServiceSkel skel;

        Dictionary<uint, WireConnection> connections = new Dictionary<uint, WireConnection>();

        public override string MemberName { get { return m_MemberName; } }

        public WireServer(string name, ServiceSkel skel)
        {
            m_MemberName = name;
            this.skel = skel;

        }

        public override Task<WireConnection> Connect(CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("Cannot connect from server side");
        }

        private WireConnectCallbackFunction connect_callback = null;

        public override WireConnectCallbackFunction WireConnectCallback
        {
            get { return connect_callback; }
            set { connect_callback = value; }
        }

        public override Func<uint, T> PeekInValueCallback { get; set; }
        public override Func<uint, T> PeekOutValueCallback { get; set; }
        public override Action<T, TimeSpec, uint> PokeOutValueCallback { get; set; }

        public override void WirePacketReceived(MessageEntry m, Endpoint e = null)
        {
            if (m.EntryType == MessageEntryType.WirePacket)
            {
                try
                {
                    DispatchPacket(m.elements, connections[e.LocalEndpoint]);
                }
                catch { }

            }
        }

        private object connectionslock = new object();

        public Task<MessageEntry> WireCommand(MessageEntry m, Endpoint e)
        {
            lock (connectionslock)
            {
                switch (m.EntryType)
                {
                    case MessageEntryType.WireConnectReq:
                        {
                            if (!connections.ContainsKey(e.LocalEndpoint))
                                connections.Add(e.LocalEndpoint, new WireConnection(this, e));
                            WireConnection con = connections[e.LocalEndpoint];
                            if (connect_callback != null) connect_callback(this, con);

                            MessageEntry ret = new MessageEntry(MessageEntryType.WireConnectRet, MemberName);
                            return Task.FromResult(ret);
                        }
                    case MessageEntryType.WireDisconnectReq:
                        {
                            if (!connections.ContainsKey(e.LocalEndpoint))
                                throw new ServiceException("Invalid wire connection");
                            connections.Remove(e.LocalEndpoint);
                            return Task.FromResult(new MessageEntry(MessageEntryType.WireDisconnectRet, MemberName));
                        }
                    case MessageEntryType.WirePeekInValueReq:
                        {
                            if (direction == MemberDefinition_Direction.writeonly)
                                throw new WriteOnlyMemberException("Write only member");
                            var value = PeekInValueCallback(e.LocalEndpoint);
                            var mr_dat = PackPacket(value, TimeSpec.Now);
                            var mr = new MessageEntry(MessageEntryType.WirePeekInValueRet, MemberName);
                            mr.elements = mr_dat;
                            return Task.FromResult(mr);
                        }
                    case MessageEntryType.WirePeekOutValueReq:
                        {
                            if (direction == MemberDefinition_Direction.readonly_)
                                throw new ReadOnlyMemberException("Read only member");
                            var value = PeekOutValueCallback(e.LocalEndpoint);
                            var mr_dat = PackPacket(value, TimeSpec.Now);
                            var mr = new MessageEntry(MessageEntryType.WirePeekOutValueRet, MemberName);
                            mr.elements = mr_dat;
                            return Task.FromResult(mr);
                        }
                    case MessageEntryType.WirePokeOutValueReq:
                        {
                            if (direction == MemberDefinition_Direction.readonly_)
                                throw new ReadOnlyMemberException("Read only member");
                            TimeSpec ts;
                            var value = UnpackPacket(m.elements, out ts);
                            PokeOutValueCallback(value, ts, e.LocalEndpoint);                            
                            var mr = new MessageEntry(MessageEntryType.WirePokeOutValueRet, MemberName);                            
                            return Task.FromResult(mr);
                        }
                    default:
                        throw new InvalidOperationException("Invalid Command");
                }
            }
        }

        protected override async Task Close(WireConnection c, Endpoint ee, CancellationToken cancel = default(CancellationToken))
        {
            MessageEntry m = new MessageEntry(MessageEntryType.WireClosed, MemberName);

            await skel.SendWireMessage(m, ee, cancel).ConfigureAwait(false);

            try
            {
                lock (connectionslock)
                {
                    if (connections.ContainsKey(c.Endpoint))
                        connections.Remove(c.Endpoint);
                }
            }
            catch { }
        }

        public override Task SendWirePacket(T packet, TimeSpec time, Endpoint e = null)
        {
            if (!connections.ContainsKey(e.LocalEndpoint)) throw new Exception("Wire has been disconnected");
            List<MessageElement> el = PackPacket(packet, time);
            MessageEntry m = new MessageEntry(MessageEntryType.WirePacket, MemberName);
            m.elements = el;

            return skel.SendWireMessage(m, e, default(CancellationToken));
        }

        public override void Shutdown()
        {
            Wire<T>.WireConnection[] cons = connections.Values.ToArray();
            foreach (Wire<T>.WireConnection con in cons)
            {
                try
                {
                    con.RemoteClose();
                }
                catch { }
            }
        }

        protected override object PackAnyType(ref T o)
        {
            return skel.RRContext.PackAnyType<T>(ref o);
        }

        protected override object UnpackAnyType(MessageElement o)
        {
            return skel.RRContext.UnpackAnyType<T>(o);
        }

        public override Task<Tuple<T, TimeSpec>> PeekInValue(CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("Invalid for wire server");
        }

        public override Task<Tuple<T, TimeSpec>> PeekOutValue(CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("Invalid for wire server");
        }

        public override Task PokeOutValue(T value, CancellationToken cancel = default(CancellationToken))
        {
            throw new InvalidOperationException("Invalid for wire server");
        }
    }

    public class WireBroadcaster<T>
    {
        protected class connected_connection
        {
            public Wire<T>.WireConnection connection = null;

            public connected_connection(Wire<T>.WireConnection connection)
            {
                this.connection = connection;
            }

        }

        protected List<connected_connection> connected_wires = new List<connected_connection>();
        protected object connected_wires_lock = new object();
        protected Wire<T> wire;

        public Wire<T> Wire { get => (Wire<T>)wire; }

        protected void ConnectionClosed(connected_connection ep)
        {
            lock (connected_wires_lock)
            {
                try
                {
                    connected_wires.Remove(ep);
                }
                catch (Exception e) { }
            }
        }

        protected void ConnectionConnected(Wire<T> w, Wire<T>.WireConnection ep)
        {
            lock (connected_wires_lock)
            {
                connected_connection c = new connected_connection(ep);
                ep.WireCloseCallback = delegate(Wire<T>.WireConnection w2) { ConnectionClosed(c); };
                connected_wires.Add(c);
            }

        }

        public WireBroadcaster(Wire<T> wire)
        {
            this.wire = wire;
            wire.WireConnectCallback = ConnectionConnected;
            wire.PeekInValueCallback = ClientPeekInValue;
            wire.PeekOutValueCallback = ClientPeekOutValue;
            wire.PokeOutValueCallback = ClientPokeOutValue;
        }

        protected T current_out_value = default(T);
        protected bool out_value_valid = false;

        public T OutValue
        {
            set
            {
                lock (connected_wires_lock)
                {
                    current_out_value = value;
                    out_value_valid = true;
                    List<connected_connection> ceps = new List<connected_connection>();
                    foreach (connected_connection c in connected_wires)
                    {
                        ceps.Add(c);
                    }

                    foreach (connected_connection ee in ceps)
                    {                        
                        try
                        {
                            Wire<T>.WireConnection c = ee.connection as Wire<T>.WireConnection;
                            if (c == null)
                            {
                                connected_wires.Remove(ee);
                                continue;
                            }

                            var ep_endpoint = c.Endpoint;
                            if (Predicate != null)
                            {
                                if (!Predicate(this, ep_endpoint))
                                {
                                    continue;
                                }
                            }
                            c.SendOutValue(value).ContinueWith(delegate(Task t)
                            {
                                if (t.IsFaulted)
                                {
                                    lock (connected_wires_lock)
                                    {
                                        ee.connection = null;
                                    }
                                }
                            }
#if !ROBOTRACONTEUR_BRIDGE
                            , TaskContinuationOptions.OnlyOnFaulted
#endif
                            );

                        }
                        catch (Exception exp) {
                            ee.connection = null;
                        }
                    }
                }
            }
        }

        protected T ClientPeekInValue(uint c)
        {
            lock (connected_wires_lock)
            {
                if (!out_value_valid)
                {
                    throw new ValueNotSetException("Value not set");
                }
                return current_out_value;
            }
        }

        protected T ClientPeekOutValue(uint c)
        {
            throw new ReadOnlyMemberException("Read only wire");
        }

        protected void ClientPokeOutValue(T v, TimeSpec ts, uint c)
        {
            throw new ReadOnlyMemberException("Read only wire");
        }

        public Func<object, uint, bool> Predicate { get; set; }
    }

    public class WireUnicastReceiver<T>
    {
        protected class connected_connection
        {
            public Wire<T>.WireConnection connection = null;

            public connected_connection(Wire<T>.WireConnection connection)
            {
                this.connection = connection;
            }

        }

        connected_connection active_connection = null;        
        protected Wire<T> wire;

        T in_value;
        TimeSpec in_value_ts;
        DateTime lasttime_recv_local;
        bool in_value_valid;
        uint in_value_ep;

        public Wire<T> Wire { get => (Wire<T>)wire; }

        protected void ConnectionClosed(connected_connection ep)
        {
            lock (this)
            {
                if (active_connection == ep)
                {
                    active_connection = null;
                }
            }
        }

        protected void ConnectionConnected(Wire<T> w, Wire<T>.WireConnection ep)
        {
            lock (this)
            {
                if (active_connection != null)
                {
                    try
                    {
                        ((Wire<T>.WireConnection)active_connection.connection).Close().ContinueWith(x => { });
                    }
                    catch { }
                }

                connected_connection c = new connected_connection(ep);
                ep.WireCloseCallback = delegate (Wire<T>.WireConnection w2) { ConnectionClosed(c); };
                ep.WireValueChanged += (c1,value,time) => ClientPokeOutValue(value, time, c1.Endpoint);
                active_connection = c;
            }

        }

        public WireUnicastReceiver(Wire<T> wire)
        {
            this.wire = wire;
            wire.WireConnectCallback = ConnectionConnected;
            wire.PeekInValueCallback = ClientPeekInValue;
            wire.PeekOutValueCallback = ClientPeekOutValue;
            wire.PokeOutValueCallback = ClientPokeOutValue;
        }

        public T GetInValue(out TimeSpec ts, out uint ep)
        {
            lock(this)
            {
                if (!in_value_valid || Wire<T>.WireConnection.IsValueExpired(lasttime_recv_local, InValueLifespan)) 
                    throw new InvalidOperationException("Value not set");
                ts = in_value_ts;
                ep = in_value_ep;
                return in_value;
            }
        }

        public bool TryGetInValue(out T val, out TimeSpec ts, out uint ep)
        {
            lock (this)
            {
                if (!in_value_valid || Wire<T>.WireConnection.IsValueExpired(lasttime_recv_local, InValueLifespan))
                {
                    val = default;
                    ts = default;
                    ep = default;
                    return false;
                }
                ts = in_value_ts;
                ep = in_value_ep;
                val = in_value;
                return true;
            }
        }

        protected T ClientPeekInValue(uint c)
        {
            throw new WriteOnlyMemberException("Write only wire");
        }

        protected T ClientPeekOutValue(uint c)
        {
            lock (this)
            {
                if (!in_value_valid) throw new InvalidOperationException("Value not set");               
                return in_value;
            }
        }

        protected void ClientPokeOutValue(T v, TimeSpec ts, uint c)
        {
            lock(this)
            {
                in_value = v;
                in_value_ts = ts;
                lasttime_recv_local = DateTime.UtcNow;
                in_value_valid = true;
                in_value_ep = c;
                inval_waiter.NotifyAll(true);
            }

            if (InValueChanged!=null) InValueChanged(v, ts, c);
        }

        public event Action<T, TimeSpec, uint>  InValueChanged;

        public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken token = default)
        {
            var waiter = inval_waiter.CreateWaiterTask(timeout, token);
            using (waiter)
            {
                await waiter.Task.ConfigureAwait(false);
            }

            return in_value_valid && !Wire<T>.WireConnection.IsValueExpired(lasttime_recv_local, InValueLifespan);
        }

        public int InValueLifespan { get; set; } = -1;

        AsyncValueWaiter<bool> inval_waiter = new AsyncValueWaiter<bool>();
    }

}