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
    /**
    <summary>
    `wire` member type interface
    </summary>
    <remarks>
    <para>
    The Wire class implements the `wire` member type. Wires are declared in service definition files
    using the `wire` keyword within object declarations. Wires provide "most recent" value streaming
    between clients and services. They work by creating "connection" pairs between the client and service.
    The wire streams the current value between the wire connection pairs using packets. Wires
    are unreliable only the most recent value is of interest, and any older values
    will be dropped. Wire connections have an InValue and an OutValue. Users set the OutValue on the
    connection. The new OutValue is transmitted to the peer wire connection, and becomes the peer's
    InValue. The peer can then read the InValue. The client and service have their own InValue
    and OutValue, meaning that each direction, client to service or service to client, has its own
    value.
    </para>
    <para>
    Wire connections are created using the Connect() function. Services receive
    incoming connection requests through a callback function. Thes callback is configured using
    the WireConnectCallback property. Services may also use the WireBroadcaster class
    or WireUnicastReceiver class to automate managing wire connection lifecycles. WireBroadcaster
    is used to send values to all connected clients. WireUnicastReceiver is used to receive the
    value from the most recent wire connection. See WireConnection for details on sending
    and receiving streaming values.
    </para>
    <para>
    Wire clients may also optionally "peek" and "poke" the wire without forming a streaming
    connection. This is useful if the client needs to read the InValue or set the OutValue
    instantaniously, but does not need continuous updating. PeekInValue() 
    will retrieve the client's current InValue. PokeOutValue()
    will send a new client OutValue to the service.
    PeekOutValue() or will retrieve the last client OutValue received by
    the service.
    </para>
    <para>
    "Peek" and "poke" operations initiated by the client are received on the service using
    callbacks. Use PeekInValueCallback, PeekOutValueCallback,
    and PokeOutValueCallback to configure the callbacks to handle these requests.
    WireBroadcaster and WireUnicastReceiver configure these callbacks automatically, so
    the user does not need to configure the callbacks when these classes are used.
    </para>
    <para>
    Wires can be declared*readonly* or*writeonly*. If neither is specified, the wire is assumed
    to be full duplex.*readonly* pipes may only send values from service to client, ie OutValue
    on service side and InValue on client side.*writeonly* pipes may only send values from
    client to service, ie OutValue on client side and InValue on service side. Use Direction()
    to determine the direction of the wire.
    </para>
    <para>
    Unlike pipes, wire connections are not indexed, so only one connection pair can be
    created per client connection.
    </para>
    <para>
    WireBroadcaster or WireUnicastReceiver are typically used to simplify using wires.
    See WireBroadcaster and WireUnicastReceiver for more information.
    </para>
    <para>
    This class is instantiated by the node. It should not be instantiated by the user.
    </para>
    </remarks>
    <typeparam name="T">The value data type</typeparam>
    */

        [PublicApi]
    public abstract class Wire<T>
    {

            /**
            <summary>
            Wire connection used to transmit "most recent" values
            </summary>
            <remarks>
            <para>
            Wire connections are used to transmit "most recent" values between connected
            wire members. See Wire for more information on wire members.
            </para>
            <para>
            Wire connections are created by clients using the Wire::Connect() or Wire::AsyncConnect()
            functions. Services receive incoming wire connection requests through a
            callback function specified using the Wire.WireConnectCallback property. Services
            may also use the WireBroadcaster class to automate managing wire connection lifecycles and
            sending values to all connected clients, or use WireUnicastReceiver to receive an incoming
            value from the most recently connected client.
            </para>
            <para>
            Wire connections are used to transmit "most recent" values between clients and services. Connection
            the wire creates a connection pair, one in the client, and one in the service. Each wire connection
            object has an InValue and an OutValue. Setting the OutValue of one will cause the specified value to
            be transmitted to the InValue of the peer. See Wire for more information.
            </para>
            <para>
            Values can optionally be specified to have a finite lifespan using InValueLifespan and
            OutValueLifespan. Lifespans can be used to prevent using old values that have
            not been recently updated.
            </para>
            <para>
            This class is instantiated by the Wire class. It should not be instantiated
            by the user.
            </para>
            </remarks>
            */


        [PublicApi]
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
            /**
            <summary>
            Get the current InValue
            </summary>
            <remarks>
            Gets the current InValue that was transmitted from the peer. Throws
            ValueNotSetException if no value has been received, or the most
            recent value lifespan has expired.
            </remarks>
            */

        [PublicApi]
            public virtual T InValue
            {
                get
                {
                    if (!inval_valid) throw new ValueNotSetException("Value not set");
                    if (IsValueExpired(lasttime_recv_local, InValueLifespan))
                    {
                        throw new ValueNotSetException("Value not set");
                    }
                    return inval;
                }               
            }
            /**
            <summary>
            Try getting the InValue, returning true on success or false on failure
            </summary>
            <remarks>
            Get the current InValue and InValue timestamp. Return true or false on
            success or failure instead of throwing exception.
            </remarks>
            <param name="value">[out] The current InValue</param>
            <returns>true if the value is valid, otherwise false</returns>
            */

        [PublicApi]
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
            /**
            <summary>
            Get or set the current OutValue
            </summary>
            <remarks>
            <para>
            Gets the current OutValue that was transmitted to the peer. Throws
            ValueNotSetException if no value has been received, or the most
            recent value lifespan has expired.
            </para>
            <para>
            Setting the OutValue for the wire connection. The specified value will be
            transmitted to the peer, and will become the peers InValue. The transmission
            is unreliable, meaning that values may be dropped if newer values arrive.
            </para>
            </remarks>
            */

        [PublicApi]
            public virtual T OutValue
            {
                get
                {
                    if (!outval_valid) throw new ValueNotSetException("Value not set");
                    if (IsValueExpired(lasttime_send_local, OutValueLifespan))
                    {
                        throw new ValueNotSetException("Value not set");
                    }
                    return outval;
                }
                set
                {
                    SendOutValue(value).IgnoreResult();
                }

            }
            /**
            <summary>
            Try getting the OutValue, returning true on success or false on failure
            </summary>
            <remarks>
            Get the current OutValue and OutValue timestamp. Return true or false on
            success and failure instead of throwing exception.
            </remarks>
            <param name="value">[out] The current OutValue</param>
            <returns>true if the value is valid, otherwise false</returns>
            */

        [PublicApi]
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
            /**
            <summary>
            Get the timestamp of the last received value
            </summary>
            <remarks>
            Returns the timestamp of the value in the///senders* clock
            </remarks>
            */

        [PublicApi]
            public virtual TimeSpec LastValueReceivedTime
            {
                get
                {
                    if (!inval_valid) throw new Exception("No value received");
                    return lasttime_recv;
                }
            }
            /**
            <summary>
            Get the timestamp of the last sent value
            </summary>
            <remarks>
            Returns the timestamp of the last sent value in the///local* clock
            </remarks>
            */

        [PublicApi]
            public virtual TimeSpec LastValueSentTime
            {
                get
                {
                    if (!outval_valid) throw new Exception("No value sent");
                    return lasttime_send;
                }
            }

            /**
            <summary>
            Close the wire connection
            </summary>
            <remarks>
            Close the wire connection. Blocks until close complete. The peer wire connection
            is destroyed automatically.
            </remarks>
            */

        [PublicApi]
            public virtual Task Close()
            {
                lock(sendlock)
                {
                    send_closed = true;
                }

                return parent.Close(this);
            }
            /**
            <summary>
            Event invoked when the InValue is changed
            </summary>
            <remarks>
            Callback function must accept three arguments, receiving the WireConnection that
            received a packet, the new value, and the value's TimeSpec timestamp
            </remarks>
            */

        [PublicApi]
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
            /**
            <summary>
            Get or set the connection closed callback function
            </summary>
            <remarks>
            <para>
            Sets a function to invoke when the wire connection has been closed.
            </para>
            <para>
            Callback function must accept one argument, receiving the WireConnection that
            was closed.
            </para>
            </remarks>
            */

        [PublicApi]
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
            /**
            <summary>
            Get or set whether wire connection should ignore incoming values
            </summary>
            <remarks>
            Wire connections may optionally desire to ignore incoming values. This is useful if the connection
            is only being used to send out values, and received values may create a potential memory . If ignore is
            true, incoming values will be discarded.
            </remarks>
            */

        [PublicApi]
            public bool IgnoreInValue { get; set; } = false;
            /**
            <summary>
            Get or set the lifespan of InValue
            </summary>
            <remarks>
            <para>
            InValue may optionally have a finite lifespan specified in milliseconds. Once
            the lifespan after reception has expired, the InValue is cleared and becomes invalid.
            Attempts to access InValue will result in ValueNotSetException.
            </para>
            <para>
            InValue lifespans may be used to avoid using a stale value received by the wire. If
            the lifespan is not set, the wire will continue to return the last received value, even
            if the value is old.
            </para>
            <para>
            The lifespan in millisecond, or RR_VALUE_LIFESPAN_INFINITE for infinite lifespan
            </para>
            </remarks>
            */

        [PublicApi]
            public int InValueLifespan { get; set; } = -1;
            /**
            <summary>
            Get or set the lifespan of OutValue
            </summary>
            <remarks>
            <para>
            OutValue may optionally have a finite lifespan specified in milliseconds. Once
            the lifespan after sending has expired, the OutValue is cleared and becomes invalid.
            Attempts to access OutValue will result in ValueNotSetException.
            </para>
            <para>
            OutValue lifespans may be used to avoid using a stale value sent by the wire. If
            the lifespan is not set, the wire will continue to return the last sent value, even
            if the value is old.
            </para>
            <para>
            The lifespan in millisecond, or RR_VALUE_LIFESPAN_INFINITE for infinite lifespan
            </para>
            </remarks>
            */

        [PublicApi]
            public int OutValueLifespan { get; set; } = -1;

            public const int RR_VALUE_LIFESPAN_INFINITE = -1;

            internal static bool IsValueExpired(DateTime recv_time,int lifespan)
            {
                if (lifespan< 0)
                {
                    return false;
                }
                var expire_time = recv_time.AddMilliseconds(lifespan);
                var now_time = DateTime.UtcNow;
                 
                if ( expire_time < now_time)
                {
                    return true;
                }
                return false;
            }

            AsyncValueWaiter<bool> inval_waiter = new AsyncValueWaiter<bool>();
            AsyncValueWaiter<bool> outval_waiter = new AsyncValueWaiter<bool>();

            /**
            <summary>
            Waits for InValue to be valid
            </summary>
            <remarks>
            Blocks the current thread until InValue is valid,
            with an optional timeout. Returns true if InValue is valid,
            or false if timeout occurred.
            </remarks>
            <param name="timeout">Timeout in milliseconds, or RR_TIMEOUT_INFINITE for no timeout</param>
            <returns>true if InValue is valid, otherwise false</returns>
            */

        [PublicApi]
            public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken token = default)
            {
                var waiter = inval_waiter.CreateWaiterTask(timeout, token);
                using (waiter)
                {
                    await waiter.Task.ConfigureAwait(false);
                }

                return inval_valid && !IsValueExpired(lasttime_recv_local, InValueLifespan);
            }
            /**
            <summary>
            Waits for OutValue to be valid
            </summary>
            <remarks>
            Blocks the current thread until OutValue is valid,
            with an optional timeout. Returns true if OutValue is valid,
            or false if timeout occurred.
            </remarks>
            <param name="timeout">Timeout in milliseconds, or RR_TIMEOUT_INFINITE for no timeout</param>
            <returns>true if InValue is valid, otherwise false</returns>
            */

        [PublicApi]
            public async Task<bool> WaitOutValueValid(int timeout = -1, CancellationToken token = default)
            {
                var waiter = outval_waiter.CreateWaiterTask(timeout, token);
                using (waiter)
                {
                    await waiter.Task.ConfigureAwait(false);
                }

                return outval_valid && !IsValueExpired(lasttime_send_local, OutValueLifespan);
            }
            /**
            <summary>
            Get if the InValue is valid
            </summary>
            <remarks>
            The InValue is valid if a value has been received and
            the value has not expired
            </remarks>
            */

        [PublicApi]
            public bool InValueValid {  get { return inval_valid && !IsValueExpired(lasttime_recv_local, InValueLifespan); } }
            /**
            <summary>
            Get if the OutValue is valid
            </summary>
            <remarks>
            The OutValue is valid if a value has been
            set using OutValue
            </remarks>
            */

        [PublicApi]
            public bool OutValueValid { get { return outval_valid && !IsValueExpired(lasttime_send_local, OutValueLifespan); } }
        }

        private bool rawelements = false;
        /**
        <summary>
        Connect the wire
        </summary>
        <remarks>
        <para>
        Creates a connection between the wire, returning the client connection. Used to create
        a "most recent" value streaming connection to the service.
        </para>
        <para>
        Only valid on clients. Will throw InvalidOperationException on the service side.
        </para>
        <para>
        Note: If a streaming connection is not required, use PeekInValue(), PeekOutValue(),
        or PokeOutValue() instead of creating a connection.
        </para>
        </remarks>
        <returns>The wire connection</returns>
        */

        [PublicApi]
        public abstract Task<WireConnection> Connect(CancellationToken cancel = default(CancellationToken));

        public delegate void WireConnectCallbackFunction(Wire<T> wire, WireConnection connection);

        public delegate void WireDisconnectCallbackFunction(WireConnection wire);
        /**
        <summary>
        Set wire connected callback function
        </summary>
        <remarks>
        <para>
        Callback function invoked when a client attempts to connect a the wire. The callback
        will receive the incoming wire connection as a parameter. The service must maintain a
        reference to the wire connection, but the wire will retain ownership of the wire connection
        until it is closed. Using  boost::weak_ptr to store the reference to the connection
        is recommended.
        </para>
        <para>
        The callback may throw an exception to reject incoming connect request.
        </para>
        <para>
        Note: Connect callback is configured automatically by WireBroadcaster or
        WireUnicastReceiver
        </para>
        <para>
        Only valid for services. Will throw InvalidOperationException on the client side.
        </para>
        </remarks>
        */

        [PublicApi]
        public abstract WireConnectCallbackFunction WireConnectCallback { get; set; }

        public delegate void WireValueChangedFunction(WireConnection connection, T value, TimeSpec time);
        /**
        <summary>
        Get the member name of the wire
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public abstract string MemberName { get; }

        protected MemberDefinition_Direction direction = MemberDefinition_Direction.both;
        /**
        <summary>
        Get the direction of the wire
        </summary>
        <remarks>
        Wires may be declared*readonly* or*writeonly* in the service definition file. (If neither
        is specified, the wire is assumed to be full duplex.)*readonly* wire may only send out values from
        service to client.*writeonly* wires may only send out values from client to service.
        </remarks>
        */

        [PublicApi]
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
        /**
        <summary>
        Peek the current InValue
        </summary>
        <remarks>
        <para>
        Peeks the current InValue using a "request" instead of a streaming value. Use
        if only the instantanouse value is required.
        </para>
        <para>
        Peek and poke are similar to `property` members. Unlike streaming,
        peek and poke are reliable operations.
        </para>
        <para>
        Throws ValueNotSetException if InValue is not valid.
        </para>
        <para>
        Only valid on clients. Will throw InvalidOperationException on the service side.
        </para>
        </remarks>
        <returns>The current InValue and timestamp</returns>
        */

        [PublicApi]
        public abstract Task<Tuple<T, TimeSpec>> PeekInValue(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Peek the current OutValue
        </summary>
        <remarks>
        <para>
        Peeks the current OutValue using a "request" instead of a streaming value. Use
        if only the instantanouse value is required.
        </para>
        <para>
        Peek and poke are similar to `property` members. Unlike streaming,
        peek and poke are reliable operations.
        </para>
        <para>
        Throws ValueNotSetException if OutValue is not valid.
        </para>
        <para>
        Only valid on clients. Will throw InvalidOperationException on the service side.
        </para>
        </remarks>
        <returns>The current OutValue and timestamp</returns>
        */

        [PublicApi]
        public abstract Task<Tuple<T, TimeSpec>> PeekOutValue(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Poke the OutValue
        </summary>
        <remarks>
        <para>
        Pokes the OutValue using a "request" instead of a streaming value. Use
        to update the OutValue if the value is updated infrequently.
        </para>
        <para>
        Peek and poke are similar to `property` members. Unlike streaming,
        peek and poke are reliable operations.
        </para>
        <para>
        Only valid on clients. Will throw InvalidOperationException on the service side.
        </para>
        </remarks>
        <param name="value">The new OutValue</param>
        */

        [PublicApi]
        public abstract Task PokeOutValue(T value, CancellationToken cancel = default(CancellationToken));

        /**
        <summary>
        Set the PeekInValue callback function
        </summary>
        <remarks>
        <para>
        Peek and poke operations are used when a streaming connection of the most recent value
        is not required. Clients initiate peek and poke operations using PeekInValue(), PeekOutValue(),
        PokeOutValue(), or their asynchronous equivalents. Services receive the peek and poke
        requests through callbacks.
        </para>
        <para>
        PeekInValueCallback configures the service callback for PeekInValue() requests.
        </para>
        <para>
        The specified callback function should have the following signature:
        </para>
        <para>
        T peek_invalue_callback(uint client_endpoint)
        </para>
        <para>
        The function receives the client endpoint ID, and returns the current InValue.
        </para>
        <para>
        Note: Callback is configured automatically by WireBroadcaster or
        WireUnicastReceiver
        </para>
        <para>
        Only valid for services. Will throw InvalidOperationException on the client side.
        </para>
        </remarks>
        */

        [PublicApi]
        public abstract Func<uint, T> PeekInValueCallback { get; set; }
        /**
        <summary>
        Set the PeekOutValue callback function
        </summary>
        <remarks>
        <para>
        Peek and poke operations are used when a streaming connection of the most recent value
        is not required. Clients initiate peek and poke operations using PeekInValue(), PeekOutValue(),
        PokeOutValue(), or their asynchronous equivalents. Services receive the peek and poke
        requests through callbacks.
        </para>
        <para>
        PeekOutValueCallback configures the service callback for PeekOutValue() requests.
        </para>
        <para>
        The specified callback function should have the following signature:
        </para>
        <para>
        T peek_outvalue_callback(uint client_endpoint)
        </para>
        <para>
        The function receives the client endpoint ID, and returns the current OutValue.
        </para>
        <para>
        Note: Callback is configured automatically by WireBroadcaster or
        WireUnicastReceiver
        </para>
        <para>
        Only valid for services. Will throw InvalidOperationException on the client side.
        </para>
        </remarks>
        */

        [PublicApi]
        public abstract Func<uint, T> PeekOutValueCallback { get; set; }
        /**
        <summary>
        Set the PokeOutValue callback function
        </summary>
        <remarks>
        <para>
        Peek and poke operations are used when a streaming connection of the most recent value
        is not required. Clients initiate peek and poke operations using PeekInValue(), PeekOutValue(),
        PokeOutValue(), or their asynchronous equivalents. Services receive the peek and poke
        requests through callbacks.
        </para>
        <para>
        PokeOutValueCallback configures the service callback for PokeOutValue() requests.
        </para>
        <para>
        The specified callback function should have the following signature:
        </para>
        <para>
        void poke_outvalue_callback( T, TimeSpec timestamp, uint client_endpoint)
        </para>
        <para>
        The function receives the new out value, the new out value timestamp in the client's clock,
        and the client endpoint ID.
        </para>
        <para>
        Note: Callback is configured automatically by WireBroadcaster or
        WireUnicastReceiver
        </para>
        <para>
        Only valid for services. Will throw InvalidOperationException on the client side.
        </para>
        </remarks>
        */

        [PublicApi]
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
                    WireConnection c1;
                    lock (connectionslock)
                    {
                        c1 = connections[e.LocalEndpoint];
                    }
                    DispatchPacket(m.elements, c1); 
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
            lock (connectionslock)
            {
                if (!connections.ContainsKey(e.LocalEndpoint)) throw new Exception("Wire has been disconnected");
            }
            List<MessageElement> el = PackPacket(packet, time);
            MessageEntry m = new MessageEntry(MessageEntryType.WirePacket, MemberName);
            m.elements = el;

            return skel.SendWireMessage(m, e, default(CancellationToken));
        }

        public override void Shutdown()
        {
            Wire<T>.WireConnection[] cons;
            lock (connectionslock)
            {
                cons = connections.Values.ToArray();
                connections.Clear();
            }
            cons = connections.Values.ToArray();
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
    /**
    <summary>
    Broadcaster to send values to all connected clients
    </summary>
    <remarks>
    <para>
    WireBroadcaster is used by services to send values to all
    connected client endpoints. It attaches to the wire on the service
    side, and manages the lifecycle of connections. WireBroadcaster
    should only we used with wires that are declared*readonly*, since
    it has no provisions for receiving incoming values from clients.
    </para>
    <para>
    WireBroadcaster is initialized by the user, or by default implementation
    classes generated by RobotRaconteurGen (*_default_impl). Default
    implementation classes will automatically instantiate broadcasters for
    wires marked*readonly*. If default implementation classes are
    not used, the broadcaster must be instantiated manually. It is recommended this
    be done using the IRRServiceObject interface in the overridden
    IRRServiceObject.RRServiceObjectInit() function. This function is called after
    the wires have been instantiated by the service.
    </para>
    <para>
    Set the OutValue property to broadcast values to all connected clients.
    </para>
    <para>
    The rate that packets are sent can be regulated using a callback function configured
    with the Predicate property, or using the BroadcastDownsampler class.
    </para>
    </remarks>
    <typeparam name="T">The value data type</typeparam>
    */

        [PublicApi]
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
        /**
        <summary>
        Get the assosciated wire
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
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
        /**
        <summary>
        Construct a new WireBroadcaster
        </summary>
        <remarks>None</remarks>
        <param name="wire">The wire to use for broadcasting. Must be a wire from a service object.
        Specifying a client wire will result in an exception.</param>
        */

        [PublicApi]
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
        /**
        <summary>
        Set the OutValue for all connections
        </summary>
        <remarks>
        <para>
        Sets the OutValue for all connections. This will transmit the value
        to all connected clients using packets. The value will become the clients'
        InValue.
        </para>
        <para>
        The value will be returned when clients call Wire.PeekInValue() or
        Wire.AsyncPeekInValue()
        </para>
        </remarks>
        */

        [PublicApi]
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
#if !ROBOTRACONTEUR_H5
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
        /**
        <summary>
        Set the predicate callback function
        </summary>
        <remarks>
        <para>
        A predicate is optionally used to regulate when values are sent to clients. This is used by the
        BroadcastDownsampler to regulate update rates of values sent to clients.
        </para>
        <para>
        The predicate callback is invoked before the broadcaster sets the OutValue of a connection. If the predicate
        returns true, the OutValue packet will be sent. If it is false, the OutValue packet will not be sent to that
        endpoint. The predicate callback must have the following signature:
        </para>
        <para>
        bool broadcaster_predicate(WireBroadcaster broadcaster, uint client_endpoint)
        </para>
        <para>
        It receives the broadcaster and the client endpoint ID. It returns true to send the OutValue packet,
        or false to not send the OutValue packet.
        </para>
        </remarks>
        */

        [PublicApi]
        public Func<object, uint, bool> Predicate { get; set; }
    }
    /**
    <summary>
    Receive the InValue from the most recent connection
    </summary>
    <remarks>
    <para>
    WireUnicastReceiver is used by services to receive a value from a single client.
    When a client sets its OutValue, this value is transmitted to the service using
    packets, and becomes the service's InValue for that connection. Service wires
    can have multiple active clients, so the service needs to choose which connection
    is "active". The WireUnicastReceiver selects the "most recent" connection, and
    returns that connection's InValue. Any existing connections are closed.
    WireUnicastReceiver should only be used with wires that are declared*writeonly*.
    It is recommended that object locks be used to protect from concurrent
    access when unicast receivers are used.
    </para>
    <para>
    WireUnicastReceiver is initialized by the user, or by default implementation
    classes generated by RobotRaconteurGen (*_default_impl). Default
    implementation classes will automatically instantiate unicast receivers for
    wires marked*writeonly*. If default implementation classes are
    not used, the unicast receiver must be instantiated manually. It is recommended this
    be done using the IRRServiceObject interface in the overridden
    IRRServiceObject.RRServiceObjectInit() function. This function is called after
    the wires have been instantiated by the service.
    </para>
    <para>
    The current InValue is received using GetInValue() or TryGetInValue(). The
    InValueChanged signal can be used to monitor for changes to the InValue.
    </para>
    <para>
    Clients may also use PokeOutValue() or AsyncPokeOutValue() to update the
    unicast receiver's value.
    </para>
    </remarks>
    <typeparam name="T">The value type</typeparam>
    */

        [PublicApi]
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
        /**
        <summary>
        Get the associated wire
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
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
        /**
        <summary>
        Construct a new WireUnicastReceiverBase
        </summary>
        <remarks>None</remarks>
        <param name="wire">The wire to use for broadcasting. Must be a wire from a service object.
        Specifying a client wire will result in an exception.</param>
        */

        [PublicApi]
        public WireUnicastReceiver(Wire<T> wire)
        {
            this.wire = wire;
            wire.WireConnectCallback = ConnectionConnected;
            wire.PeekInValueCallback = ClientPeekInValue;
            wire.PeekOutValueCallback = ClientPeekOutValue;
            wire.PokeOutValueCallback = ClientPokeOutValue;
        }
        /**
        <summary>
        Get the current InValue
        </summary>
        <remarks>
        Gets the current InValue that was received from the active connection.
        Throws ValueNotSetException if no value has been received, or
        the most recent value lifespan has expired.
        </remarks>
        <param name="ts">[out] The current InValue timestamp</param>
        <param name="ep">[out] The client endpoint ID of the InValue</param>
        <returns>The current InValue</returns>
        */

        [PublicApi]
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
        /**
        <summary>
        Try getting the current InValue, returning true on success or false on failure
        </summary>
        <remarks>
        Gets the current InValue, its timestamp, and the client endpoint ID. Returns true if
        value is valid, or false if value is invalid. Value will be invalid if no value has
        been received, or the value lifespan has expired.
        </remarks>
        <param name="value">[out] The current InValue</param>
        <param name="time">[out] The current InValue timestamp</param>
        <param name="client">[out] The client endpoint ID of the InValue</param>
        <returns>true if value is valid, otherwise false</returns>
        */

        [PublicApi]
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
        /**
        <summary>
        Event fired when InValue has changed.
        </summary>
        <remarks>
        Callback function must accept three arguments, receiving the new value,
        value's TimeSpec timestamp, and the client endpoint ID.
        </remarks>
        */

        [PublicApi]
        public event Action<T, TimeSpec, uint>  InValueChanged;
        /// <summary>
        /// Wait for the InValue to be valid
        /// </summary>
        /// <remarks>None</remarks>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>True if valid at timeout</returns>
        public async Task<bool> WaitInValueValid(int timeout = -1, CancellationToken token = default)
        {
            var waiter = inval_waiter.CreateWaiterTask(timeout, token);
            using (waiter)
            {
                await waiter.Task.ConfigureAwait(false);
            }

            return in_value_valid && !Wire<T>.WireConnection.IsValueExpired(lasttime_recv_local, InValueLifespan);
        }
        /**
        <summary>
        Get or set the lifespan of InValue
        </summary>
        <remarks>
        <para>
        InValue may optionally have a finite lifespan specified in milliseconds. Once
        the lifespan after reception has expired, the InValue is cleared and becomes invalid.
        Attempts to access InValue will result in ValueNotSetException.
        </para>
        <para>
        InValue lifespans may be used to avoid using a stale value received by the wire. If
        the lifespan is not set, the wire will continue to return the last received value, even
        if the value is old.
        </para>
        <para>
        The lifespan in millisecond, or RR_VALUE_LIFESPAN_INFINITE for infinite lifespan
        </para>
        </remarks>
        */

        [PublicApi]
        public int InValueLifespan { get; set; } = -1;

        AsyncValueWaiter<bool> inval_waiter = new AsyncValueWaiter<bool>();
    }

}