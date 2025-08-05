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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
#if !ROBOTRACONTEUR_H5
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
#endif
using System.Reflection;

#pragma warning disable 1591

namespace RobotRaconteurWeb
{

    public abstract class AsyncStreamTransport : ITransportConnection
    {
        public uint LocalEndpoint { get { return m_LocalEndpoint; } }
        protected uint m_LocalEndpoint = 0;
        public uint RemoteEndpoint { get { return m_RemoteEndpoint; } }
        protected uint m_RemoteEndpoint = 0;

        public NodeID RemoteNodeID { get { lock (this) { return m_RemoteNodeID; } } }
        protected NodeID m_RemoteNodeID = NodeID.Any;

        protected NodeID target_nodeid = null;
        protected string target_nodename = null;
        protected bool is_server = false;

        protected readonly RobotRaconteurNode node;

        protected AsyncStreamTransportParent parent;

        protected AsyncStreamTransport(RobotRaconteurNode node, AsyncStreamTransportParent parent)
        {
            this.node = node;
            this.parent = parent;
        }


        protected bool m_Connected = false;
        public bool Connected
        {
            get
            {
                lock (this)
                {
                    return m_Connected;
                }
            }
        }


        protected CancellationTokenSource cancellationToken = new CancellationTokenSource();

        protected MemoryStream mwrite;
        protected MemoryStream mread;
        private byte[] recbuf;
        private byte[] sendbuf;

        protected ArrayBinaryWriter swriter;
        protected ArrayBinaryReader sreader;



        protected Stream basestream;


        protected bool m_RequireTls = false;
        public bool RequireTls { get { return m_RequireTls; } }

        protected bool m_IsTls = false;
        public bool IsTls { get { return m_IsTls; } }

        protected internal bool disable_message4 = false;
        protected internal bool send_version4 = false;
        protected internal uint active_capabilities_message2_basic;
        protected internal uint active_capabilities_message4_basic;
        protected internal uint max_message_size = 12 * 1024 * 1024;



        protected async Task ConnectStream(Stream s, bool is_server, NodeID target_nodeid, string target_nodename, bool starttls, bool requiretls, int heartbeat_period, CancellationToken cancel)
        {
            m_HeartbeatPeriod = heartbeat_period;
            this.m_RequireTls = requiretls;
            sendbuf = new byte[100000];
            mwrite = new MemoryStream(sendbuf);

            recbuf = new byte[100000];
            mread = new MemoryStream(recbuf);

            swriter = new ArrayBinaryWriter(mwrite, sendbuf, sendbuf.Length);
            sreader = new ArrayBinaryReader(mread, recbuf, recbuf.Length);

            this.is_server = is_server;
            this.target_nodename = target_nodename;
            this.target_nodeid = target_nodeid;

            if (this.target_nodename == null) this.target_nodename = "";
            if (this.target_nodeid == null) this.target_nodeid = NodeID.Any;

            basestream = s;

            tlastsend = DateTime.UtcNow;
            tlastrec = DateTime.UtcNow;
            tlastrec_mes = DateTime.UtcNow;

            Task recvtask = DoReceive();

            if (!is_server)
            {
                if (starttls)
                {
#if !ROBOTRACONTEUR_H5
                await DoClientTlsHandshake(cancel).ConfigureAwait(false);
#else
                    throw new NotImplementedException("TLS support not implemented");
#endif
                }

                NodeID rid = (NodeID)await StreamOp("CreateConnection", Tuple.Create(this.target_nodeid, this.target_nodename), cancel).ConfigureAwait(false);
                lock (this)
                {
                    m_RemoteNodeID = rid;
                }
            }

            var noop = DoHeartbeat();


        }

        bool request_receive_pause = false;
        TaskCompletionSource<int> request_receive_pause_task = null;
        TaskCompletionSource<int> pause_receive_task = null;

        protected async Task RequestReceivePause()
        {
            TaskCompletionSource<int> request_pause_task1 = new TaskCompletionSource<int>();
            lock (this)
            {
                if (request_receive_pause)
                {
                    if (request_receive_pause_task != null)
                    {
                        request_pause_task1 = request_receive_pause_task;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    request_receive_pause = true;
                    request_receive_pause_task = request_pause_task1;
                }
            }

            await request_pause_task1.Task.ConfigureAwait(false);
        }

        protected Task ResumeReceivePause()
        {
            TaskCompletionSource<int> pause_task1 = null;
            lock (this)
            {
                request_receive_pause = false;
                pause_task1 = pause_receive_task;
            }
            if (pause_task1 != null)
            {
                pause_task1.TrySetResult(0);
            }

            return Task.FromResult(0);

        }

        protected async Task DoReceive()
        {
            try
            {
                while (this.Connected)
                {
                    TaskCompletionSource<int> request_pause_task1 = null;
                    lock (this)
                    {
                        if (request_receive_pause)
                        {
                            pause_receive_task = new TaskCompletionSource<int>();
                            request_pause_task1 = request_receive_pause_task;
                            request_receive_pause_task = null;
                        }
                    }

                    if (pause_receive_task != null && request_pause_task1 != null)
                    {
                        request_pause_task1.TrySetResult(0);
                        await pause_receive_task.Task.ConfigureAwait(false);
                    }

                    lock (this)
                    {
                        pause_receive_task = null;
                    }

                    int mempos = 0;
                    while (mempos < 16)
                    {
                        int n = await basestream.ReadAsync(recbuf, mempos, 16 - mempos).ConfigureAwait(false);
                        if (n == 0)
                        {
                            Close();
                            return;
                        }
                        mempos += n;
                    }

                    string seed = ASCIIEncoding.ASCII.GetString(recbuf, 0, 4);
                    if (seed != "RRAC") throw new IOException("Invalid magic");

                    int meslength = (int)BitConverter.ToUInt32(recbuf, 4);
                    ushort message_version = BitConverter.ToUInt16(recbuf, 8);

                    if (recbuf.Length < meslength)
                    {
                        byte[] newbuf = new byte[(int)(meslength * 1.2)];
                        Buffer.BlockCopy(recbuf, 0, newbuf, 0, mempos);
                        recbuf = newbuf;
                        mread = new MemoryStream(recbuf, 0, recbuf.Length, true);
                        sreader = new ArrayBinaryReader(mread, recbuf, recbuf.Length);
                    }

                    if (meslength < 8)
                    {
                        RRLogFuncs.LogDebug("Received too small a message", node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);
                        throw new ProtocolException("Received too small a message");
                    }

                    if (meslength > (max_message_size))
                    {
                        RRLogFuncs.LogDebug(string.Format("Received too large a message {0} but max allowed {1}", meslength, max_message_size),
                            node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);

                        throw new ProtocolException("Received too large a message");
                    }

                    while (mempos < meslength)
                    {
                        int n = await basestream.ReadAsync(recbuf, mempos, meslength - mempos).ConfigureAwait(false);
                        if (n == 0)
                        {
                            Close();
                            return;
                        }
                        mempos += n;
                    }

                    if (mempos == meslength)
                    {
                        try
                        {

                            mread.Position = 0;
                            sreader.Reset(meslength);
                            Message mes = new Message();
                            if (message_version == 4)
                            {
                                mes.Read4(sreader);

                                var flags = mes.header.MessageFlags_;
                                if ((flags & (byte)MessageFlags.RoutingInfo) == 0)
                                {
                                    mes.header.SenderNodeID = RemoteNodeID;
                                    mes.header.ReceiverNodeID = node.NodeID;
                                }

                                if ((flags & (byte)MessageFlags.EndpointInfo) == 0)
                                {
                                    mes.header.SenderEndpoint = RemoteEndpoint;
                                    mes.header.ReceiverEndpoint = LocalEndpoint;
                                }
                            }
                            else
                            {
                                mes.Read(sreader);
                            }

                            if ((mes.entries.Count == 1) && (mes.entries[0].EntryType == MessageEntryType.StreamOp || mes.entries[0].EntryType == MessageEntryType.StreamOpRet))
                            {
#if !ROBOTRACONTEUR_H5
                            if (mes.entries[0].EntryType == MessageEntryType.StreamOp && mes.entries[0].MemberName == "STARTTLS")
                            {
                                //TODO: enforce direction of handshake
                                await DoServerTlsHandshake(mes).ConfigureAwait(false);
                                    continue;
                            }
                            else if (mes.entries[0].EntryType == MessageEntryType.StreamOpRet && mes.entries[0].MemberName == "STARTTLS")
                            {
                                //TODO: enforce direction of handshake

                                TaskCompletionSource<Message> ret;
                                TaskCompletionSource<int> wait;
                                lock (this)
                                {
                                    ret = clienthandshake_recv_task;
                                    wait = clienthandshake_recv_done_task;
                                }

                                if (ret == null || wait == null)
                                {
                                    if (ret != null) ret.TrySetCanceled();
                                    Close();
                                }
                                else
                                {
                                    ret.TrySetResult(mes);
                                    await wait.Task.ConfigureAwait(false);
                                }
                                    continue;
                            }
#endif
                                await StreamOpMessageReceived(mes).ConfigureAwait(false);

                                continue;

                            }

                            Task noop = ProcessMessage(mes).IgnoreResult();

                        }
                        catch (Exception)
                        {
                            Close();
                        }
                    }
                }
            }
            catch (Exception)
            {
                Close();
            }

        }

        protected virtual async Task ProcessMessage(Message mes)
        {
            try
            {
                NodeID RemoteNodeID1;
                uint local_ep;
                uint remote_ep;
                lock (this)
                {
                    RemoteNodeID1 = m_RemoteNodeID;
                    local_ep = m_LocalEndpoint;
                    remote_ep = m_RemoteEndpoint;
                }

                if (RequireTls && !IsTls)
                {
                    bool bad_message = true;
                    if (mes.entries.Count == 1)
                    {
                        if (mes.entries[0].EntryType == MessageEntryType.StreamOp && mes.entries[0].MemberName == "STARTTLS")
                        {
                            bad_message = false;
                        }
                    }
                    if (bad_message)
                    {
                        Close();
                        return;
                    }
                }

                if (IsTls)
                {
                    if (!RemoteNodeID1.IsAnyNode)
                    {

                        if (RemoteNodeID1 != mes.header.SenderNodeID)
                        {
                            var ret1 = node.GenerateErrorReturnMessage(mes, MessageErrorType.NodeNotFound, "RobotRaconteurNode.NodeNotFound", "Invalid sender node");
                            if (ret1.entries.Count > 0)
                            {
                                Task noop = SendMessage(ret1, default(CancellationToken)).IgnoreResult();
                                return;
                            }
                        }
                    }

                    if (local_ep != 0 && remote_ep != 0)
                    {
                        if (local_ep != mes.header.ReceiverEndpoint || remote_ep != mes.header.SenderEndpoint)
                        {
                            var ret1 = node.GenerateErrorReturnMessage(mes, MessageErrorType.InvalidEndpoint, "RobotRaconteurNode.InvalidEndpoint", "Invalid sender endpoint");
                            if (ret1.entries.Count > 0)
                            {
                                Task noop = SendMessage(ret1, default(CancellationToken)).IgnoreResult();
                                return;
                            }
                        }
                    }
                }




                Message ret = await parent.SpecialRequest(mes).ConfigureAwait(false);
                if (ret != null)
                {
                    try
                    {
                        if ((mes.entries[0].EntryType == MessageEntryType.ConnectionTest || mes.entries[0].EntryType == MessageEntryType.ConnectionTestRet))
                        {
                            if (mes.entries[0].Error != MessageErrorType.None)
                            {
                                Close();
                                return;
                            }
                        }

                        if ((ret.entries[0].EntryType == MessageEntryType.ConnectClientRet || ret.entries[0].EntryType == MessageEntryType.ConnectClientCombinedRet || ret.entries[0].EntryType == MessageEntryType.ReconnectClient) && ret.entries[0].Error == MessageErrorType.None)
                        {
                            if (ret.header.SenderNodeID == node.NodeID)
                            {

                                m_RemoteEndpoint = ret.header.ReceiverEndpoint;
                                m_LocalEndpoint = ret.header.SenderEndpoint;
                                parent.AddTransportConnection(ret.header.SenderEndpoint, this);
                            }
                            else
                            {
                                //TODO: Handle this better
                                Close();
                            }
                        }

                        //if (mes.entries[0].EntryType != MessageEntryType.ConnectionTest && mes.entries[0].EntryType != MessageEntryType.ConnectionTestRet)
                        {
                            tlastrec = DateTime.UtcNow;
                        }


                        Task noop = SendMessage(ret, default(CancellationToken)).IgnoreResult();
                    }
                    catch (Exception)
                    {
                        Close();
                    }

                    return;
                }


                tlastrec = DateTime.UtcNow;

                if ((mes.entries.Count == 1) && (mes.entries[0].EntryType == MessageEntryType.StreamOp || mes.entries[0].EntryType == MessageEntryType.StreamOpRet))
                {
#if !ROBOTRACONTEUR_H5
                if (mes.entries[0].EntryType == MessageEntryType.StreamOp && mes.entries[0].MemberName == "STARTTLS")
                {
                    await DoServerTlsHandshake(mes).ConfigureAwait(false);
                }
#endif
                    await StreamOpMessageReceived(mes).ConfigureAwait(false);
                    return;

                }

                if (mes.entries.Count == 1 && (mes.entries[0].EntryType == MessageEntryType.StreamCheckCapability || mes.entries[0].EntryType == MessageEntryType.StreamCheckCapabilityRet))
                {
                    CheckStreamCapability_MessageReceived(mes);
                    return;
                }

                if (mes.entries.Count == 1)
                {
                    if ((mes.entries[0].EntryType == MessageEntryType.ConnectClientRet || mes.entries[0].EntryType == MessageEntryType.ConnectClientCombinedRet) && remote_ep == 0)
                    {
                        lock (this)
                        {
                            if (m_RemoteEndpoint == 0)
                            {
                                m_RemoteEndpoint = mes.header.SenderEndpoint;
                            }
                            remote_ep = m_RemoteEndpoint;
                        }
                    }

                }

                if (IsTls)
                {
                    if (local_ep == 0 || remote_ep == 0)
                    {
                        if (mes.entries.Count != 1)
                        {
                            Close();
                            return;
                        }
                    }

                    var command = mes.entries[0].EntryType;
                    if ((ushort)command > 500)
                    {
                        Close();
                        return;
                    }
                }

                if (!((mes.entries.Count == 1) && ((mes.entries[0].EntryType == MessageEntryType.ConnectionTest) || (mes.entries[0].EntryType == MessageEntryType.ConnectionTestRet))))
                {
                    tlastrec_mes = DateTime.UtcNow;
                    await ProcessMessage2(mes).ConfigureAwait(false);
                }

            }
            catch (Exception)
            {
                Close();
            }

        }


        public virtual async Task ProcessMessage2(Message m)
        {
            try
            {
                Transport.m_CurrentThreadTransportConnectionURL = GetConnectionURL();
                Transport.m_CurrentThreadTransport = this;
                await MessageReceived(m).ConfigureAwait(false);
            }
            catch
            {
                Close();
            }
            finally
            {
                Transport.m_CurrentThreadTransportConnectionURL = null;
                Transport.m_CurrentThreadTransport = null;
            }
        }

        public virtual async Task MessageReceived(Message m)
        {
            await parent.MessageReceived(m).ConfigureAwait(false);
        }

        public abstract string GetConnectionURL();


        private bool sending = false;
        private List<Tuple<TaskCompletionSource<int>, Message>> send_queue = new List<Tuple<TaskCompletionSource<int>, Message>>();

        bool request_send_pause = false;
        TaskCompletionSource<int> request_send_pause_task = null;
        TaskCompletionSource<int> pause_send_task = null;


        protected async Task RequestSendPause()
        {
            TaskCompletionSource<int> request_pause_task1 = null;

            lock (this)
            {
                if (!sending)
                {
                    sending = true;

                }
                else
                {
                    request_pause_task1 = new TaskCompletionSource<int>();
                    if (request_send_pause)
                    {
                        if (request_send_pause_task != null)
                        {
                            request_pause_task1 = request_send_pause_task;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        request_send_pause = true;
                        request_send_pause_task = request_pause_task1;

                        if (!sending)
                        {
                            pause_send_task = new TaskCompletionSource<int>();

                        }

                    }
                }
            }

            if (request_pause_task1 != null)
            {
                await request_pause_task1.Task.ConfigureAwait(false);
            }

        }

        protected Task ResumeSendPause()
        {
            TaskCompletionSource<int> pause_task1 = null;
            lock (this)
            {
                request_send_pause = false;
                pause_task1 = pause_send_task;

                if (pause_task1 == null)
                {
                    if (send_queue.Count > 0)
                    {
                        var t = send_queue[0];
                        send_queue.RemoveAt(0);
                        t.Item1.TrySetResult(0);
                    }
                    else
                    {
                        sending = false;
                    }
                }

            }

            if (pause_task1 != null)
            {
                pause_task1.TrySetResult(0);
            }



            return Task.FromResult(0);
        }

        public virtual async Task SendMessage(Message m, CancellationToken cancel)
        {

            TaskCompletionSource<int> waiter = null;

            lock (this)
            {
                if (!m_Connected) throw new ConnectionException("Transport connection closed");
                if (sending)
                {
                    waiter = new TaskCompletionSource<int>();
                    waiter.AttachCancellationToken(cancel, new TimeoutException("Timed out"));

                    bool replaced = false;

                    for (int i = 0; i < send_queue.Count;)
                    {
                        bool remove = false;

                        var h1 = m.header;
                        var h2 = send_queue[i].Item2.header;
                        if (h1.ReceiverNodeName == h2.ReceiverNodeName
                            && h1.SenderNodeName == h2.SenderNodeName
                            && h1.ReceiverNodeID == h2.ReceiverNodeID
                            && h1.SenderNodeID == h2.SenderNodeID
                            && h1.ReceiverEndpoint == h2.ReceiverEndpoint
                            && h1.SenderEndpoint == h2.SenderEndpoint)
                        {
                            if (send_queue[i].Item2.entries.Count == m.entries.Count && m.entries.Count == 1)
                            {
                                var mm1 = send_queue[i].Item2.entries[0];
                                var mm2 = m.entries[0];
                                if (mm1.EntryType == MessageEntryType.ConnectionTest
                                    && mm2.EntryType == MessageEntryType.ConnectionTest) return;

                                if (mm1.EntryType == MessageEntryType.WirePacket
                                    && mm2.EntryType == MessageEntryType.WirePacket)
                                {
                                    if (mm1.ServicePath == mm2.ServicePath
                                        && mm1.MemberName == mm2.MemberName)
                                    {
                                        remove = true;
                                    }
                                }
                            }
                        }

                        if (remove)
                        {

                            var c = send_queue[i].Item1;

                            if (replaced)
                            {
                                send_queue.RemoveAt(i);
                            }
                            else
                            {
                                send_queue[i] = Tuple.Create(waiter, m);
                                i++;
                            }
                            //Tell this send message to return immediately
                            c.TrySetResult(1);
                        }
                        else
                        {
                            i++;
                        }
                    }


                    if (!replaced)
                    {
                        send_queue.Add(Tuple.Create(waiter, m));
                    }
                }
                else
                {
                    sending = true;
                }

            }

            if (waiter != null)
            {
                if (await waiter.Task.ConfigureAwait(false) != 0) return; ;
                if (!Connected) throw new ConnectionException("Transport connection closed");
            }

            TaskCompletionSource<int> request_pause_task1 = null;
            lock (this)
            {
                if (request_send_pause)
                {

                    pause_send_task = new TaskCompletionSource<int>();
                    request_pause_task1 = request_send_pause_task;
                    request_send_pause_task = null;
                }
            }

            if (pause_send_task != null && request_pause_task1 != null)
            {
                request_pause_task1.TrySetResult(0);
                await pause_send_task.Task.ConfigureAwait(false);
            }

            lock (this)
            {
                pause_send_task = null;
            }

            try
            {
                if (!send_version4)
                {
                    uint meslength = m.ComputeSize();

                    if (meslength > (max_message_size - 100))
                    {
                        RRLogFuncs.LogDebug(string.Format("Attempt to send message size {0} when max is {1}",
                                                 meslength, max_message_size - 100), node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);
                        throw new ProtocolException("Message larger than maximum message size");
                    }

                    if (meslength > sendbuf.Length)
                    {
                        sendbuf = new byte[(int)(meslength * 1.2)];
                        mwrite = new MemoryStream(sendbuf, 0, sendbuf.Length, true);
                        swriter = new ArrayBinaryWriter(mwrite, sendbuf, sendbuf.Length);
                    }

                    mwrite.Position = 0;
                    swriter.Reset((int)meslength);
                    m.Write(swriter);
                }
                else
                {
                    if (!RemoteNodeID.IsAnyNode && RemoteEndpoint != 0)
                    {
                        if (m.header.SenderNodeID == node.NodeID && m.header.ReceiverNodeID == RemoteNodeID &&
                            m.header.SenderEndpoint == LocalEndpoint && m.header.ReceiverEndpoint == RemoteEndpoint)
                        {
                            if (!(m.entries.Count == 1 && ((uint)m.entries[0].EntryType) < 500))
                            {
                                m.header.MessageFlags_ &= (byte)~(MessageFlags.RoutingInfo | MessageFlags.EndpointInfo);
                            }
                        }
                    }

                    uint meslength = m.ComputeSize4();

                    if (meslength > (max_message_size - 100))
                    {
                        RRLogFuncs.LogDebug(string.Format("Attempt to send message size {0} when max is {1}",
                                                 meslength, max_message_size - 100), node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);
                        throw new ProtocolException("Message larger than maximum message size");
                    }

                    if (meslength > sendbuf.Length)
                    {
                        sendbuf = new byte[(int)(meslength * 1.2)];
                        mwrite = new MemoryStream(sendbuf, 0, sendbuf.Length, true);
                        swriter = new ArrayBinaryWriter(mwrite, sendbuf, sendbuf.Length);
                    }

                    mwrite.Position = 0;
                    swriter.Reset((int)meslength);
                    m.Write4(swriter);
                }

                tlastsend = DateTime.UtcNow;
                await basestream.WriteAsync(sendbuf, 0, (int)mwrite.Position, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Close();
                throw e;
            }
            finally
            {
                TaskCompletionSource<int> next_send = null;
                lock (this)
                {
                    if (send_queue.Count > 0)
                    {
                        var t = send_queue[0];
                        send_queue.RemoveAt(0);
                        next_send = t.Item1;
                    }
                    else
                    {
                        sending = false;
                    }
                }

                if (next_send != null)
                {
                    next_send.TrySetResult(0);
                }


            }


        }

        private DateTime tlastsend;
        private DateTime tlastrec;
        private DateTime tlastrec_mes;

        public int ReceiveTimeout = 2500;


        protected int m_HeartbeatPeriod = 5000;

        private async Task DoHeartbeat()
        {

            try
            {
                while (m_Connected)
                {
                    await Task.Delay(500, cancellationToken.Token).ConfigureAwait(false);


                    if ((DateTime.UtcNow - tlastsend).TotalMilliseconds > m_HeartbeatPeriod)
                    {
                        Message m = new Message();
                        m.header = new MessageHeader();
                        m.header.SenderNodeID = node.NodeID;
                        MessageEntry mm = new MessageEntry(MessageEntryType.ConnectionTest, "");
                        m.entries.Add(mm);

                        await SendMessage(m, default(CancellationToken)).ConfigureAwait(false);
                    }

                    if ((tlastsend - tlastrec).TotalMilliseconds > ReceiveTimeout)
                    {
                        var diff = (tlastsend - tlastrec).TotalMilliseconds;
                        Close();
                    }
                    else if ((DateTime.UtcNow - tlastrec_mes).TotalMilliseconds > node.TransportInactivityTimeout)
                    {
                        Close();
                    }
                }
            }
            catch (Exception)
            {
                Close();
            }

        }

        public virtual void Close()
        {
            TaskCompletionSource<Message> r1 = null;
            TaskCompletionSource<Message> r2 = null;

            List<TaskCompletionSource<int>> q1 = new List<TaskCompletionSource<int>>();
            List<TaskCompletionSource<int>> q2 = new List<TaskCompletionSource<int>>();

            TaskCompletionSource<Message> h1 = null;
            TaskCompletionSource<int> h2 = null;


            lock (this)
            {
                if (!m_Connected) return;
                m_Connected = false;

                try
                {
                    parent.RemoveTransportConnection(this);
                }
                catch (Exception) { }


                foreach (var t in send_queue)
                {
                    t.Item1.TrySetException(new ConnectionException("Transport connection closed"));
                }

                send_queue.Clear();

                foreach (var t in streamop_queue)
                {
                    q1.Add(t);
                    //t.TrySetException(new ConnectionException("Transport connection closed"));
                }

                streamop_queue.Clear();

                r1 = streamop_ret;
                streamop_ret = null;

                if (streamop_ret != null)
                {
                    streamop_ret.TrySetException(new ConnectionException("Transport connection closed"));
                    streamop_ret = null;
                }

                foreach (var t in CheckStreamCapability_queue)
                {
                    //t.TrySetException(new ConnectionException("Transport connection closed"));
                    q2.Add(t);
                }

                CheckStreamCapability_queue.Clear();

                //CheckStreamCapability_ret.TrySetException(new ConnectionException("Transport connection closed"));
                r2 = CheckStreamCapability_ret;
                CheckStreamCapability_ret = null;
#if !ROBOTRACONTEUR_H5
            h1 = clienthandshake_recv_task;
            h2 = clienthandshake_recv_done_task;
#endif
            }

            if (r1 != null) r1.TrySetException(new ConnectionException("Transport connection closed"));
            if (r2 != null) r2.TrySetException(new ConnectionException("Transport connection closed"));

            foreach (var q in q1) q.TrySetException(new ConnectionException("Transport connection closed"));
            foreach (var q in q2) q.TrySetException(new ConnectionException("Transport connection closed"));

            if (h1 != null) h1.TrySetCanceled();
            if (h2 != null) h2.TrySetCanceled();

            cancellationToken.Cancel();

            try
            {
                basestream.Close();
            }
            catch (Exception) { }

        }


        public virtual void CheckConnection(uint endpoint)
        {
            if (endpoint != LocalEndpoint) throw new InvalidEndpointException("Incorrect Transportendpoint");

            if (!m_Connected) throw new ConnectionException("Transport not connected");
        }


        protected virtual Task<MessageEntry> PackStreamOpRequest(string command, object args)
        {
            var mm = new MessageEntry(MessageEntryType.StreamOp, command);

            if (command == "GetRemoteNodeID")
            {

            }
            else
            {
                throw new InvalidOperationException("Unknown StreamOp command");
            }

            return Task.FromResult(mm);

        }

        protected virtual Task<object> UnpackStreamOpResponse(MessageEntry response, MessageHeader header)
        {
            var command = response.MemberName;
            switch (command)
            {
                case "GetRemoteNodeID":
                    {
                        NodeID n = header.SenderNodeID;
                        return Task.FromResult<object>(n);
                    }
                case "CreateConnection":
                    {
                        lock (this)
                        {

                            if (response.Error != MessageErrorType.None && response.Error != MessageErrorType.ProtocolError)
                            {
                                throw RobotRaconteurExceptionUtil.MessageEntryToException(response);
                            }

                            if (!m_RemoteNodeID.IsAnyNode)
                            {
                                if (header.SenderNodeID != m_RemoteNodeID)
                                {
                                    throw new ConnectionException("Invalid node connection");
                                }
                            }
                            else
                            {
                                if (target_nodename != "")
                                {
                                    if (target_nodename != header.SenderNodeName)
                                    {
                                        throw new ConnectionException("Invalid node connection");
                                    }
                                }

                                if (!target_nodeid.IsAnyNode)
                                {
                                    if (target_nodeid != header.SenderNodeID)
                                    {
                                        throw new ConnectionException("Invalid node connection");
                                    }
                                }
                            }
                        }

                        if (response.TryFindElement("capabilities", out var elem_caps))
                        {
                            uint message2_basic_caps = (uint)TransportCapabilityCode.Message2BasicEnable;
                            uint message4_basic_caps = 0;

                            var caps_arrays = elem_caps.CastData<uint[]>();

                            for (int i = 0; i < caps_arrays.Length; i++)
                            {
                                uint cap = caps_arrays[i];
                                uint cap_page = cap & (uint)TransportCapabilityCode.PageMask;
                                uint cap_value = cap & (uint)~TransportCapabilityCode.PageMask;
                                if (cap_page == (uint)TransportCapabilityCode.Message2BasicPage)
                                {
                                    if ((cap_value & (uint)TransportCapabilityCode.Message2BasicEnable) == 0)
                                    {
                                        RRLogFuncs.LogDebug("CreateConnection server transport must support message version 2",
                                            node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint
                                            );
                                        throw new ProtocolException("Transport must support Message Version 2");
                                    }

                                    if ((cap_value & (uint)~(TransportCapabilityCode.Message2BasicEnable | TransportCapabilityCode.Message2BasicConnectCombined)) != 0)
                                    {
                                        RRLogFuncs.LogDebug("CreateConnection invalid version 2 message caps returned by server",
                                            node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint
                                            );
                                        throw new ProtocolException("Invalid Message Version 2 capabilities");
                                    }

                                    message2_basic_caps = cap_value;
                                }

                                if (cap_page == (uint)TransportCapabilityCode.Message4BasicPage)
                                {
                                    if (disable_message4)
                                    {
                                        if (cap_value != 0)
                                        {
                                            RRLogFuncs.LogDebug("CreateConnection invalid version 4 message caps returned by server",
                                                node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);
                                            throw new ProtocolException("Invalid Message Version 4 capabilities");
                                        }
                                    }
                                    else
                                    {
                                        if ((cap_value & (uint)TransportCapabilityCode.Message4BasicEnable) == 0)
                                        {
                                            if (cap_value != 0)
                                            {
                                                RRLogFuncs.LogDebug("CreateConnection invalid version 4 message caps returned by server",
                                                    node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);

                                                throw new ProtocolException("Invalid Message Version 4 capabilities");
                                            }
                                        }
                                        else
                                        {
                                            if ((cap_value & (uint)~(TransportCapabilityCode.Message4BasicEnable | TransportCapabilityCode.Message4BasicConnectCombined)) != 0)
                                            {
                                                RRLogFuncs.LogDebug("CreateConnection invalid version 4 message caps returned by server",
                                                    node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint);
                                                throw new ProtocolException("Invalid Message Version 4 capabilities");
                                            }

                                            message4_basic_caps = cap_value;
                                        }
                                    }
                                }
                            }
                            active_capabilities_message2_basic = message2_basic_caps | (uint)TransportCapabilityCode.Message2BasicPage;

                            if (message4_basic_caps != 0)
                            {
                                active_capabilities_message4_basic = message4_basic_caps | (uint)TransportCapabilityCode.Message4BasicPage;
                                send_version4 = true;
                            }
                        }


                        NodeID n = header.SenderNodeID;
                        return Task.FromResult<object>(n);
                    }
                default:
                    throw new MemberNotFoundException("Unknown command");
            }

        }

        protected virtual Task<MessageEntry> ProcessStreamOpRequest(MessageEntry request, MessageHeader header)
        {
            var command = request.MemberName;
            var mmret = new MessageEntry(MessageEntryType.StreamOpRet, command);

            try
            {
                switch (command)
                {
                    case "GetRemoteNodeID":
                        break;
                    case "CreateConnection":
                        {
                            lock (this)
                            {

                                if (request.TryFindElement("capabilities", out var elem_caps))
                                {
                                    uint message2_basic_caps = (uint)TransportCapabilityCode.Message2BasicEnable;
                                    uint message4_basic_caps = 0;

                                    List<uint> ret_caps = new List<uint>();

                                    var caps_array = elem_caps.CastData<uint[]>();
                                    for (int i = 0; i < caps_array.Length; i++)
                                    {
                                        uint cap = caps_array[i];
                                        uint cap_page = cap & (uint)TransportCapabilityCode.PageMask;
                                        uint cap_value = cap & (uint)~TransportCapabilityCode.PageMask;
                                        if (cap_page == (uint)TransportCapabilityCode.Message2BasicPage)
                                        {
                                            message2_basic_caps = cap_value & (uint)(TransportCapabilityCode.Message2BasicEnable | TransportCapabilityCode.Message2BasicConnectCombined);
                                        }

                                        if (cap_page == (uint)TransportCapabilityCode.Message4BasicPage)
                                        {
                                            message4_basic_caps = cap_value & (uint)(TransportCapabilityCode.Message4BasicEnable | TransportCapabilityCode.Message4BasicConnectCombined);
                                        }
                                    }

                                    if ((message2_basic_caps & (uint)TransportCapabilityCode.Message2BasicEnable) == 0)
                                    {
                                        RRLogFuncs.LogDebug("CreateConnection client transport must support message version 2",
                                            node, RobotRaconteur_LogComponent.Transport, endpoint: LocalEndpoint
                                            );
                                        throw new ProtocolException("Transport must support Message Version 2");
                                    }
                                    else
                                    {
                                        message2_basic_caps |= (uint)TransportCapabilityCode.Message2BasicPage;
                                        ret_caps.Add(message2_basic_caps);
                                        active_capabilities_message2_basic = message2_basic_caps;
                                    }

                                    if ((message4_basic_caps & (uint)TransportCapabilityCode.Message4BasicEnable) != 0 && !disable_message4)
                                    {
                                        send_version4 = true;
                                        message4_basic_caps |= (uint)TransportCapabilityCode.Message4BasicPage;
                                        ret_caps.Add(message4_basic_caps);
                                        active_capabilities_message4_basic = message4_basic_caps;

                                    }

                                    mmret.AddElement("capabilities", ret_caps.ToArray());
                                }

                                if (header.ReceiverNodeID.IsAnyNode && header.ReceiverNodeName == "" || header.ReceiverNodeName == node.NodeName)
                                {
                                    m_RemoteNodeID = header.SenderNodeID;
                                    return Task.FromResult(mmret);
                                }

                                if (header.ReceiverNodeID == node.NodeID)
                                {
                                    if (header.ReceiverNodeName == "" || header.ReceiverNodeName == node.NodeName)
                                    {
                                        m_RemoteNodeID = header.SenderNodeID;
                                        return Task.FromResult(mmret);
                                    }
                                }

                            }

                            mmret.Error = MessageErrorType.NodeNotFound;
                            mmret.AddElement("errorname", "RobotRaconteur.NodeNotFound");
                            mmret.AddElement("errorstring", "Node not found");

                            break;
                        }


                    default:
                        throw new ProtocolException("Unknown StreamOp Command");
                }

            }
            catch (Exception)
            {
                mmret.Error = MessageErrorType.ProtocolError;
                mmret.AddElement("errorname", "RobotRaconteur.ProtocolError");
                mmret.AddElement("errorstring", "Invalid Stream Operation");
            }

            return Task.FromResult(mmret);

        }

        private Queue<TaskCompletionSource<int>> streamop_queue = new Queue<TaskCompletionSource<int>>();
        private TaskCompletionSource<Message> streamop_ret = null;

        protected virtual async Task<object> StreamOp(string command, object args = null, CancellationToken cancel = default(CancellationToken))
        {
            TaskCompletionSource<int> t = null;
            lock (this)
            {
                if (!m_Connected)
                {
                    throw new ConnectionException("Transport connection closed");
                }

                if (streamop_ret == null)
                {
                    streamop_ret = new TaskCompletionSource<Message>();
                    streamop_ret.AttachCancellationToken(cancel, new TimeoutException("Timed out"));
                }
                else
                {
                    t = new TaskCompletionSource<int>();
                    t.AttachCancellationToken(cancel, new TimeoutException("Timed out"));
                    streamop_queue.Enqueue(t);
                }
            }

            if (t != null)
            {
                await t.Task.ConfigureAwait(false);
            }

            try
            {
                Message m = new Message();
                m.header = new MessageHeader();
                m.header.ReceiverNodeName = "";
                m.header.SenderNodeName = node.NodeName;
                m.header.SenderNodeID = node.NodeID;
                m.header.ReceiverNodeID = RemoteNodeID;


                if (command == "CreateConnection")
                {
                    var a = (Tuple<NodeID, string>)args;
                    m.header.ReceiverNodeID = a.Item1;
                    m.header.ReceiverNodeName = a.Item2;
                    MessageEntry mm = new MessageEntry(MessageEntryType.StreamOp, command);
                    var caps = new List<uint>();
                    caps.Add((uint)(TransportCapabilityCode.Message2BasicPage | TransportCapabilityCode.Message2BasicEnable | TransportCapabilityCode.Message2BasicConnectCombined));
                    if (!disable_message4)
                    {
                        caps.Add((uint)(TransportCapabilityCode.Message4BasicPage | TransportCapabilityCode.Message4BasicEnable | TransportCapabilityCode.Message4BasicConnectCombined));
                    }
                    mm.AddElement("capabilities", caps.ToArray());
                    m.entries.Add(mm);
                }
                else
                {
                    MessageEntry mm = await PackStreamOpRequest(command, args).ConfigureAwait(false);
                    m.entries.Add(mm);
                }

                await SendMessage(m, cancel).ConfigureAwait(false);

                Message streamop_ret1 = await streamop_ret.Task.AwaitWithTimeout(10000).ConfigureAwait(false);

                return await UnpackStreamOpResponse(streamop_ret1.entries[0], streamop_ret1.header).ConfigureAwait(false);
            }
            finally
            {
                lock (this)
                {
                    if (streamop_queue.Count == 0)
                    {
                        streamop_ret = null;
                    }
                    else
                    {
                        streamop_ret = new TaskCompletionSource<Message>();
                        TaskCompletionSource<int> t2 = streamop_queue.Dequeue();
                        t2.TrySetResult(0);
                    }
                }
            }




        }

        protected virtual async Task StreamOpMessageReceived(Message m)
        {
            MessageEntry mm;

            try
            {
                mm = m.entries[0];
            }
            catch { return; }
            if (mm.EntryType == MessageEntryType.StreamOp)
            {
                string command = mm.MemberName;
                Message mret = new Message();
                mret.header = new MessageHeader();
                mret.header.SenderNodeName = node.NodeName;
                mret.header.ReceiverNodeName = m.header.SenderNodeName;
                mret.header.SenderNodeID = node.NodeID;
                mret.header.ReceiverNodeID = m.header.SenderNodeID;
                MessageEntry mmret = await ProcessStreamOpRequest(mm, m.header).ConfigureAwait(false);

                if (mmret != null)
                {
                    mret.entries.Add(mmret);
                    await SendMessage(mret, default(CancellationToken)).IgnoreResult().ConfigureAwait(false);
                }
            }
            else
            {
                TaskCompletionSource<Message> r = null;
                lock (this)
                {
                    if (streamop_ret == null) return;
                    r = streamop_ret;
                }
                r.TrySetResult(m);
            }

        }

        TaskCompletionSource<Message> CheckStreamCapability_ret = null;
        Queue<TaskCompletionSource<int>> CheckStreamCapability_queue = new Queue<TaskCompletionSource<int>>();

        public async Task<uint> CheckStreamCapability(string name, CancellationToken cancel = default(CancellationToken))
        {
            TaskCompletionSource<int> t = null;
            lock (this)
            {
                if (!m_Connected)
                {
                    throw new ConnectionException("Transport connection closed");
                }

                if (CheckStreamCapability_ret == null)
                {
                    CheckStreamCapability_ret = new TaskCompletionSource<Message>();
                    CheckStreamCapability_ret.AttachCancellationToken(cancel, new TimeoutException("Timed out"));
                }
                else
                {
                    t = new TaskCompletionSource<int>();
                    t.AttachCancellationToken(cancel, new TimeoutException("Timed out"));
                    CheckStreamCapability_queue.Enqueue(t);
                }
            }

            if (t != null)
            {
                await t.Task.ConfigureAwait(false);
            }

            try
            {
                Message m = new Message();

                m.header.SenderNodeID = node.NodeID;

                m.header.ReceiverNodeID = RemoteNodeID;
                MessageEntry mm = new MessageEntry(MessageEntryType.StreamCheckCapability, name);
                m.entries.Add(mm);

                await SendMessage(m, cancel).ConfigureAwait(false);

                Message mret = await CheckStreamCapability_ret.Task.AwaitWithTimeout<Message>((int)node.RequestTimeout).ConfigureAwait(false);
                return mret.entries[0].FindElement("return").CastData<uint>();
            }
            finally
            {
                lock (this)
                {
                    if (CheckStreamCapability_queue.Count == 0)
                    {
                        CheckStreamCapability_ret = null;
                    }
                    else
                    {
                        CheckStreamCapability_ret = new TaskCompletionSource<Message>();
                        TaskCompletionSource<int> t2 = CheckStreamCapability_queue.Dequeue();
                        t2.TrySetResult(0);
                    }
                }
            }

        }

        protected void CheckStreamCapability_MessageReceived(Message m)
        {
            try
            {
                if (m.entries[0].EntryType == MessageEntryType.StreamCheckCapability)
                {
                    Message ret = new Message();
                    ret.header = new MessageHeader();
                    ret.header.SenderNodeID = node.NodeID;
                    ret.header.ReceiverNodeID = m.header.SenderNodeID;
                    MessageEntry mret = new MessageEntry(MessageEntryType.StreamCheckCapabilityRet, m.entries[0].MemberName);
                    mret.ServicePath = m.entries[0].ServicePath;
                    mret.AddElement("return", StreamCapabilities(m.entries[0].MemberName));
                    ret.entries.Add(mret);
                    SendMessage(ret, default(CancellationToken)).IgnoreResult();
                }
                else if (m.entries[0].EntryType == MessageEntryType.StreamCheckCapabilityRet)
                {
                    TaskCompletionSource<Message> r = null;
                    lock (this)
                    {
                        if (CheckStreamCapability_ret == null)
                        {
                            return;
                        }
                        r = CheckStreamCapability_ret;

                    }
                    r.TrySetResult(m);
                }
            }
            catch { }
        }

        public virtual uint StreamCapabilities(string name)
        {
            return 0;
        }

        public bool CheckCapabilityActive(uint cap)
        {
            uint cap_page = cap & (uint)TransportCapabilityCode.PageMask;
            uint cap_value = cap & (uint)(~TransportCapabilityCode.PageMask);

            if (cap_page == (uint)TransportCapabilityCode.Message2BasicPage)
            {
                return (cap_value & (active_capabilities_message2_basic & (uint)(~TransportCapabilityCode.PageMask))) != 0;
            }

            if (cap_page == (uint)TransportCapabilityCode.Message4BasicPage)
            {
                return (cap_value & (active_capabilities_message4_basic & (uint)(~TransportCapabilityCode.PageMask))) != 0;
            }

            return false;
        }
#if !ROBOTRACONTEUR_H5
    protected async Task DoServerTlsHandshake(Message m)
    {
        Message mret = new Message();
        mret.header = new MessageHeader();
        mret.header.SenderNodeName = node.NodeName;
        mret.header.ReceiverNodeName = m.header.SenderNodeName;
        mret.header.SenderNodeID = node.NodeID;
        mret.header.ReceiverNodeID = m.header.SenderNodeID;
        MessageEntry mmret = new MessageEntry(MessageEntryType.StreamOpRet, "STARTTLS");
        mret.entries.Add(mmret);

        var cert = GetTlsCertificate();

        if (cert == null)
        {
            mmret.AddElement("errorname", "RobotRaconteur.ConnectionError");
            mmret.AddElement("errorstring", "Server certificate not loaded");
            await SendMessage(mret, default(CancellationToken)).ConfigureAwait(false);
            Close();
            return;
        }

        bool good = false;
        lock (this)
        {
            if (m.header.ReceiverNodeID.IsAnyNode && m.header.ReceiverNodeName == "" || m.header.ReceiverNodeName == node.NodeName)
            {
                m_RemoteNodeID = m.header.SenderNodeID;
                good = true;
            }

            if (m.header.ReceiverNodeID == node.NodeID && !good)
            {
                if (m.header.ReceiverNodeName == "" || m.header.ReceiverNodeName == node.NodeName)
                {
                    m_RemoteNodeID = m.header.SenderNodeID;
                    good = true;
                }
            }

        }

        if (!good)
        {
            mmret.AddElement("errorname", "RobotRaconteur.NodeNotFound");
            mmret.AddElement("errorstring", "Node not found");
            await SendMessage(mret, default(CancellationToken)).ConfigureAwait(false);
            Close();
            return;
        }

        bool mutualauth=false;
        var mutualauth1 = m.entries[0].elements.FirstOrDefault(x => x.ElementName == "mutualauth");
        if (mutualauth1!=null)
        {
            if(mutualauth1.CastData<string>().ToLower()=="true")
            {
                mutualauth=true;
            }
        }

        try
        {
            await SendMessage(mret, default(CancellationToken)).ConfigureAwait(false);

            await RequestSendPause().ConfigureAwait(false);

            var ssl = new SslStream(basestream, false, RemoteCertificateValidationCallback);
            await ssl.AuthenticateAsServerAsync(cert.Item1, mutualauth, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false).AwaitWithTimeout(5000).ConfigureAwait(false);
            basestream = ssl;

            await ResumeSendPause().ConfigureAwait(false);

            return;

        }
        catch (Exception)
        {
            Close();
        }

    }

    System.Security.Cryptography.AsymmetricAlgorithm OnPVKSelection (X509Certificate certificate, string targetHost)
    {
            return ((X509Certificate2)GetTlsCertificate().Item1).PrivateKey;
    }

    TaskCompletionSource<Message> clienthandshake_recv_task = null;
    TaskCompletionSource<int> clienthandshake_recv_done_task = null;

    string handshake_host=null;
    protected virtual async Task DoClientTlsHandshake(CancellationToken cancel)
    {
        lock (this)
        {
            clienthandshake_recv_task = new TaskCompletionSource<Message>();
            clienthandshake_recv_done_task = new TaskCompletionSource<int>();
        }

        await RequestSendPause().ConfigureAwait(false);

        try
        {

            var m = new Message();
            m.header = new MessageHeader();
            m.header.ReceiverNodeID = target_nodeid;
            m.header.ReceiverNodeName = target_nodename;
            var mm = new MessageEntry(MessageEntryType.StreamOp, "STARTTLS");
            var cert = GetTlsCertificate();
            bool mutualauth = cert != null;
            if (mutualauth)
            {
                mm.AddElement("mutualauth", "true");
            }
            m.entries.Add(mm);


            byte[] m_mstream_buf=new byte[m.ComputeSize()];
            var m_mstream = new MemoryStream();
            var mstream = new ArrayBinaryWriter(m_mstream,m_mstream_buf, m_mstream_buf.Length);
            m.Write(mstream);

            var mstream_buf = m_mstream.ToArray();
            await basestream.WriteAsync(mstream_buf, 0, mstream_buf.Length).ConfigureAwait(false);

            var mret = await clienthandshake_recv_task.Task.ConfigureAwait(false);
            NodeID recv_nodeid = mret.header.SenderNodeID;
            string recv_nodename = mret.header.SenderNodeName;
            if (!target_nodeid.IsAnyNode && target_nodeid != recv_nodeid)
                throw new ConnectionException("Could not validate remote node");
            if (!String.IsNullOrEmpty(target_nodename) && target_nodename!=recv_nodename)
                throw new ConnectionException("Could not validate remote node");
            if (mret.entries.Count!=1)
                throw new ConnectionException("TLS handshake error");
            if(mret.entries[0].EntryType!=MessageEntryType.StreamOpRet
                || mret.entries[0].MemberName!="STARTTLS")
                throw new ConnectionException("TLS handshake error");
            if (mutualauth && mret.entries[0].elements.Count(x => x.ElementName == "mutualauth" && x.CastData<string>().ToLower() == "true")==0)
                throw new ConnectionException("TLS handshake error");

			var host="Robot Raconteur Node " + recv_nodeid.ToString();
            handshake_host=host;

            X509CertificateCollection certs = null;
            if (mutualauth)
            {
                certs = new X509CertificateCollection(new[] { cert.Item1 });
            }

            var ssl = new SslStream(basestream, false, RemoteCertificateValidationCallback);
            await ssl.AuthenticateAsClientAsync(host, certs, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false).AwaitWithTimeout(5000).ConfigureAwait(false);
            basestream = ssl;

            await ResumeSendPause().ConfigureAwait(false);
        }
        catch (Exception)
        {
            Close();
            throw;
        }
        finally
        {
            TaskCompletionSource<int> done_task = null;
            lock(this)
            {
                done_task = clienthandshake_recv_done_task;
                clienthandshake_recv_done_task = null;
                clienthandshake_recv_task = null;
            }
            done_task.TrySetResult(0);
        }


    }

        protected virtual Tuple<X509Certificate,X509CertificateCollection> GetTlsCertificate()
    {
        return parent.GetTlsCertificate();
    }

        bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (this.is_server && certificate == null) return true;
            if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors)
            {
                return RemoteCertificateValidationCallback2(chain, false);
            }

            var chain0 = new X509Chain();

            if (sender is SslStream)
            {
                chain0.ChainPolicy.RevocationMode = ((SslStream)sender).CheckCertRevocationStatus ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            }
            else
            {
                chain0.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            }

            List<X509Certificate2> rootcerts = new List<X509Certificate2>();
            string[] root_cert_names = new string[] { "Robot_Raconteur_Node_Root_CA.cer", "Robot_Raconteur_Node_Root_CA2.cer" };

            foreach (var root_cert_name in root_cert_names)
            {
                using (var root_cert_stream1 = typeof(AsyncStreamTransport).Assembly.GetManifestResourceStream("RobotRaconteurWeb.Data." + root_cert_name))
                using (var root_cert_stream = new MemoryStream())
                {
                    root_cert_stream1.CopyTo(root_cert_stream);
                    rootcerts.Add(new X509Certificate2(root_cert_stream.ToArray()));
                }
            }

            foreach (var rootcert in rootcerts)
            {
                chain0.ChainPolicy.ExtraStore.Add(rootcert);
            }
            for (int i = 1; i < chain.ChainElements.Count; i++)
            {
                chain0.ChainPolicy.ExtraStore.Add(chain.ChainElements[i].Certificate);
            }
            bool isValid = chain0.Build((X509Certificate2)certificate);

            if (chain0.ChainStatus.Any(x => x.Status == X509ChainStatusFlags.UntrustedRoot))
            {
                if (!rootcerts.Any(x=> x.Thumbprint == chain0.ChainElements[chain0.ChainElements.Count - 1].Certificate.Thumbprint))
                {
                    return false;
                }
            }

            return RemoteCertificateValidationCallback2(chain0, true);
        }

    bool RemoteCertificateValidationCallback2(X509Chain chain, bool embedded_root)
    {


        if (chain.ChainElements[0].Certificate.Subject != "CN=" + handshake_host)
        {
            return false;
        }

        int CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT = 0x08000000;

        for (int i=0; i<chain.ChainElements.Count; i++)
        {
            var element = chain.ChainElements[i];
            bool found_ext = false;
            bool found_crit_ext = false;
            foreach (var s in element.ChainElementStatus)
            {

                if (s.Status == X509ChainStatusFlags.InvalidExtension)
                {
                    found_ext = true;
                    continue;
                }

                int v1 = (int)s.Status;
                int v2 = (int)X509ChainStatusFlags.NoError;

                if ((int)s.Status == CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT)
                {
                    found_crit_ext = true;
                    continue;
                }

            }

            if (!found_ext || !found_crit_ext)
            {
                //TODO: re-enable this oid check
                //return false;
            }


            bool found_rr_oid = false;

            foreach (var e in element.Certificate.Extensions)
            {
                if (!e.Critical) continue;

                string oid = e.Oid.Value;
                if (oid == "2.5.29.15" || oid == "2.5.29.14" || oid == "2.5.29.19" || oid == "2.5.29.35" || oid == "2.5.29.32")
                {
                    continue;
                }

                string rr_oid;

                if (i == 0)
                {
                    rr_oid="1.3.6.1.4.1.45455.1.1.3.3";
                }
                else if (i == chain.ChainElements.Count-1)
                {
                    rr_oid = "1.3.6.1.4.1.45455.1.1.3.1";
                }
                else
                {
                    rr_oid = "1.3.6.1.4.1.45455.1.1.3.2";
                }

                if (oid != rr_oid)
                {
                    //TODO: re-enable this oid check
                    //return false;
                }
                else
                {
                    found_rr_oid = true;
                }
            }

            //TODO: re-enable this oid check
            //if (!found_rr_oid) return false;

        }

        foreach (var s in chain.ChainStatus)
        {
            if (s.Status != X509ChainStatusFlags.InvalidExtension && (int)s.Status != CERT_TRUST_HAS_NOT_SUPPORTED_CRITICAL_EXT)
            {
                if (!(s.Status == X509ChainStatusFlags.UntrustedRoot && embedded_root))
                    return false;
            }
        }

        return true;
    }

    public bool IsSecure
    {
        get
        {
            if (basestream is SslStream)
            {
                return true;
            }
            return false;
        }
    }

    public bool IsSecurePeerIdentityVerified
    {
        get
        {
            try
            {
                var s = basestream as SslStream;
                if (s != null)
                {
                    var cert = s.RemoteCertificate;
                    if (cert != null)
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public string GetSecurePeerIdentity()
    {
        try
        {
            var s = basestream as SslStream;
            if (s != null)
            {
                var cert = s.RemoteCertificate;
                if (cert == null) throw new AuthenticationException("Peer identity is not verified");
                return cert.Subject.ReplaceFirst("CN=Robot Raconteur Node ", "");
            }
            throw new AuthenticationException("Connection is not seccure");
        }
        catch
        {
            throw new AuthenticationException("Connection is not seccure");
        }
    }

#endif
    }

    public interface AsyncStreamTransportParent
    {
        Task<Message> SpecialRequest(Message m);

        Task MessageReceived(Message m);

        void AddTransportConnection(uint endpoint, AsyncStreamTransport transport);

        void RemoveTransportConnection(AsyncStreamTransport transport);

#if !ROBOTRACONTEUR_H5
    Tuple<X509Certificate, X509CertificateCollection> GetTlsCertificate();
#endif
    }

}
