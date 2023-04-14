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
    public abstract class ServiceStub
    {


        public ServiceStub(string path, ClientContext c)
        {
            rr_context = c;
            rr_node = c.node;
            RRServicePath = path;
        }

        protected internal readonly string RRServicePath;

        protected internal ClientContext rr_context;
        protected internal RobotRaconteurNode rr_node;

        private object rr_context_lock = new object();

        public ClientContext RRContext
        {
            get
            {
                lock (rr_context_lock)
                {
                    return rr_context;
                }
            }
        }

        protected internal void RRReleaseContext()
        {
            lock (rr_context_lock)
            {
                rr_context = null;
            }
        }




        public async Task<MessageEntry> ProcessRequest(MessageEntry m, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");

            m.ServicePath = RRServicePath;
            return await RRContext.ProcessRequest(m, cancel);
        }

        protected internal abstract void DispatchEvent(MessageEntry m);

        public Task<object> FindObjRef(string n, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            return RRContext.FindObjRef(RRServicePath + "." + n, null, cancel);
        }

        public Task<object> FindObjRef(string n, string i, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            return RRContext.FindObjRef(RRServicePath + "." + n + "[" + RRUriExtensions.EscapeDataString(i.ToString()).Replace(".", "%2e") + "]", null, cancel);
        }

        public Task<object> FindObjRefTyped(string n, string objecttype, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            return RRContext.FindObjRef(RRServicePath + "." + n, objecttype, cancel);
        }

        public Task<object> FindObjRefTyped(string n, string i, string objecttype, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            return RRContext.FindObjRef(RRServicePath + "." + n + "[" + RRUriExtensions.EscapeDataString(i.ToString()).Replace(".", "%2e") + "]", objecttype, cancel);
        }

        protected internal async Task SendPipeMessage(MessageEntry m, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            m.ServicePath = RRServicePath;
            await RRContext.SendPipeMessage(m, cancel);
        }

        protected internal async Task SendWireMessage(MessageEntry m, CancellationToken cancel)
        {
            if (RRContext == null) throw new ServiceException("Reference has been released");
            m.ServicePath = RRServicePath;
            await RRContext.SendWireMessage(m, cancel);
        }

        protected internal virtual void DispatchPipeMessage(MessageEntry m) { }

        protected internal virtual void DispatchWireMessage(MessageEntry m) { }

        protected internal virtual Task<MessageEntry> CallbackCall(MessageEntry m)
        {
            throw new MemberNotFoundException("Member not found");
        }

        internal readonly AsyncMutex rr_async_mutex = new AsyncMutex();
    }

    public class ClientContext : Endpoint
    {
        protected internal Dictionary<string, ServiceStub> stubs;



        // public delegate void EventDispatchMessageEntry(MessageEntry e);

        protected ServiceFactory m_ServiceDef;

        public ServiceFactory ServiceDef { get { return m_ServiceDef; } }

        protected readonly bool UsePulledServiceTypes;
        protected readonly DynamicServiceFactory DynamicServiceFactory_;

        public ClientContext(RobotRaconteurNode node) : base(node)
        {


            //rec_event = new AutoResetEvent(false);
            stubs = new Dictionary<string, ServiceStub>();
            DynamicServiceFactory_ = node.DynamicServiceFactory;
            UsePulledServiceTypes = DynamicServiceFactory_ != null;
        }

        public ClientContext(ServiceFactory service_def, RobotRaconteurNode node) : base(node)
        {
            m_ServiceDef = service_def;

            //rec_event = new AutoResetEvent(false);
            stubs = new Dictionary<string, ServiceStub>();
            DynamicServiceFactory_ = node.DynamicServiceFactory;
            UsePulledServiceTypes = DynamicServiceFactory_ != null;
        }

        public async Task<object> FindObjRef(string path, string objecttype, CancellationToken cancel)
        {
            lock (stubs)
            {
                if (stubs.Keys.Contains(path))
                {
                    return stubs[path];

                }
            }

            MessageEntry e = new MessageEntry(MessageEntryType.ObjectTypeName, "");
            //MessageElement m = e.AddElement("ObjectPath", path);
            e.ServicePath = path;
            MessageEntry ret = await ProcessRequest(e, cancel);
            string objecttype2 = ret.FindElement("objecttype").CastData<string>();

            if (objecttype2 == "") throw new ObjectNotFoundException("Object type was not returned.");

            string objectdef = ServiceDefinitionUtil.SplitQualifiedName(objecttype2).Item1;

            if (UsePulledServiceTypes)
            {
                bool pull = false;
                lock (pulled_service_types)
                {
                    if (!pulled_service_types.ContainsKey(objectdef))
                    {
                        pull = true;
                    }

                }
                if (pull)
                {
                    ServiceDefinition[] d = await PullServiceDefinitionAndImports(null, cancel: cancel);

                    lock (pulled_service_defs)
                    {
                        foreach (var d2 in d)
                        {
                            if (!pulled_service_defs.ContainsKey(d2.Name))
                            {
                                pulled_service_defs.Add(d2.Name, d2);
                            }
                        }
                    }

                    var f = DynamicServiceFactory_.CreateServiceFactories(d.Select(x => x.ToString()).ToArray(), this);
                    lock (pulled_service_defs)
                    {
                        foreach (var f2 in f)
                        {
                            if (!pulled_service_types.ContainsKey(f2.GetServiceName()))
                            {
                                pulled_service_types.Add(f2.GetServiceName(), f2);
                            }
                        }
                    }
                }
            }

            if (objecttype != null)
            {
                VerifyObjectImplements(objecttype2, objecttype);
                objecttype2 = objecttype;
            }

            var stub = ServiceDef.CreateStub(objecttype2, path, this);

            lock (stubs)
            {
                if (!stubs.Keys.Contains(path))
                {
                    stubs[path] = stub;
                    return stub;
                }
                else
                {
                    return stubs[path];
                }
            }

        }




        private Dictionary<uint, TaskCompletionSource<MessageEntry>> rec_wait = new Dictionary<uint, TaskCompletionSource<MessageEntry>>();


        public async Task<MessageEntry> ProcessRequest(MessageEntry m, CancellationToken cancel)
        {
            DateTime request_start = DateTime.UtcNow;
            uint request_timeout = node.RequestTimeout;

            TaskCompletionSource<MessageEntry> rec_source = new TaskCompletionSource<MessageEntry>();
            uint t_id;
            lock (rec_wait)
            {
                request_number++;
                m.RequestID = request_number;
                t_id = request_number;
                rec_wait.Add(t_id, rec_source);
                if (ProcessRequest_checkconnection_current == null)
                {
                    ProcessRequest_checkconnection_current = ProcessRequest_checkconnection();
                }
            }

            MessageEntry rec_message = null;
            try
            {
                cancel.Register(delegate ()
                {
                    rec_source.TrySetCanceled();
                });


                Func<Task> r = async delegate ()
                {
                    await SendMessage(m, cancel);
                    rec_message = await rec_source.Task;
                };

                await r().AwaitWithTimeout((int)node.RequestTimeout);
            }
            finally
            {
                lock (rec_wait)
                {
                    rec_wait.Remove(t_id);
                }

            }

            if (rec_message.RequestID != t_id)
                throw new Exception("This should be impossible!");

            if (rec_message.Error != MessageErrorType.None)
            {
                Exception e = RobotRaconteurExceptionUtil.MessageEntryToException(rec_message);
                RobotRaconteurRemoteException e2 = e as RobotRaconteurRemoteException;
                if (e2 != null)
                {
                    Exception e3 = ServiceDef.DownCastException(e2);
                    if (e3 != null) e = e3;
                }
                throw e;
            }

            return rec_message;

        }



        Task ProcessRequest_checkconnection_current = null;
        private async Task ProcessRequest_checkconnection()
        {
            while (true)
            {
                lock (rec_wait)
                {
                    if (rec_wait.Count < 0)
                    {
                        ProcessRequest_checkconnection_current = null;
                        return;
                    }
                }

                try
                {
                    node.CheckConnection(this.LocalEndpoint);
                }
                catch (Exception)
                {
                    var rec_wait2 = new List<TaskCompletionSource<MessageEntry>>();
                    lock (rec_wait)
                    {
                        foreach (var r in rec_wait.Values)
                        {
                            rec_wait2.Add(r);
                        }
                    }

                    foreach (var t in rec_wait2)
                    {
                        t.TrySetException(new ConnectionException("Connection closed"));
                    }
                    return;
                }
                try
                {
                    await Task.Delay(500);
                }
                catch { }
            }

        }

        public async Task SendMessage(MessageEntry m, CancellationToken cancel)
        {
            //m.ServiceName = ServiceName;

            if (!Connected) throw new ConnectionException("Client has been disconnected");

            Message mm = new Message();
            mm.header = new MessageHeader();

            mm.entries.Add(m);

            LastMessageSentTime = DateTime.UtcNow;

            await SendMessage(mm, cancel);


        }

        private object rec_loc = new object();

        private uint request_number = 0;
        //private AutoResetEvent rec_event;

        public override void MessageReceived(Message m)
        {
            LastMessageReceivedTime = DateTime.UtcNow;
            if (m.entries.Count >= 1)
            {
                if (m.entries[0].EntryType == MessageEntryType.ConnectClientRet)
                {
                    m_RemoteEndpoint = m.header.SenderEndpoint;
                    m_RemoteNodeID = m.header.SenderNodeID;
                    m_RemoteNodeName = m.header.SenderNodeName;
                }

                if (m.entries[0].EntryType == MessageEntryType.EndpointCheckCapability)
                {
                    CheckEndpointCapabilityMessage(m);
                    return;
                }
            }

            foreach (MessageEntry mm in m.entries)
            {
                if (mm.Error == MessageErrorType.InvalidEndpoint)
                {
                    Close().IgnoreResult();
                    return;
                }
                MessageEntryReceived(mm);
            }
        }

        protected void MessageEntryReceived(MessageEntry m)
        {
            //lock (rec_loc)
            {
                if (m.EntryType == MessageEntryType.EventReq)
                {
                    Action a = null;

                    lock (stubs)
                        if (stubs.Keys.Contains(m.ServicePath))
                        {
                            ServiceStub stub;
                            stub = stubs[m.ServicePath];
                            a = (delegate () { stub.DispatchEvent(m); });

                            //stub.DispatchEvent(m);
                        }
                    if (a != null)
                    {
                        Task.Run(a).IgnoreResult();
                    }
                }
                else if (m.EntryType == MessageEntryType.PropertyGetRes || m.EntryType == MessageEntryType.PropertySetRes || m.EntryType == MessageEntryType.FunctionCallRes || m.EntryType == MessageEntryType.ObjectTypeNameRet || m.EntryType == MessageEntryType.ConnectClientRet || m.EntryType == MessageEntryType.DisconnectClientRet || m.EntryType == MessageEntryType.GetServiceDescRet || (m.EntryType >= MessageEntryType.PipeConnectReq && m.EntryType <= MessageEntryType.PipeDisconnectRet) || m.EntryType == MessageEntryType.ClientSessionOpRet || m.EntryType == MessageEntryType.WireConnectRet || m.EntryType == MessageEntryType.WireDisconnectRet || m.EntryType == MessageEntryType.MemoryReadRet || m.EntryType == MessageEntryType.MemoryWriteRet || m.EntryType == MessageEntryType.MemoryGetParamRet || m.EntryType == MessageEntryType.WirePeekInValueRet || m.EntryType == MessageEntryType.WirePeekOutValueRet || m.EntryType == MessageEntryType.WirePokeOutValueRet || m.EntryType == MessageEntryType.GeneratorNextRes)
                {
                    // Console.WriteLine("Got " + m.TransactionID + " " + m.EntryType + " " + m.MemberName);
                    TaskCompletionSource<MessageEntry> r = null;
                    lock (rec_wait)
                    {
                        uint t_id = m.RequestID;
                        if (rec_wait.ContainsKey(t_id))
                        {
                            r = rec_wait[t_id];
                        }
                    }

                    if (r != null) r.TrySetResult(m);


                }
                else if (m.EntryType == MessageEntryType.ServiceClosed)
                {
                    Close().IgnoreResult();
                }
                else if (m.EntryType == MessageEntryType.ClientKeepAliveRet)
                {
                }
                else if (m.EntryType == MessageEntryType.PipePacket || m.EntryType == MessageEntryType.PipeClosed || m.EntryType == MessageEntryType.PipePacketRet)
                {
                    Action a = null;
                    lock (stubs)
                        if (stubs.Keys.Contains(m.ServicePath))
                        {
                            ServiceStub stub;
                            stub = stubs[m.ServicePath];
                            a = (delegate () { stub.DispatchPipeMessage(m); });

                            //stub.DispatchEvent(m);
                        }
                    if (a != null)
                    {
                        Task.Run(a).IgnoreResult();
                    }
                }
                else if (m.EntryType == MessageEntryType.WirePacket || m.EntryType == MessageEntryType.WireClosed)
                {
                    Action a = null;
                    lock (stubs)
                        if (stubs.Keys.Contains(m.ServicePath))
                        {
                            ServiceStub stub;
                            stub = stubs[m.ServicePath];
                            a = delegate () { stub.DispatchWireMessage(m); };

                            //stub.DispatchEvent(m);
                        }

                    if (a != null)
                    {
                        Task.Run(a).IgnoreResult();
                    }
                }
                else if (m.EntryType == MessageEntryType.ServicePathReleasedReq)
                {
                    string path = m.ServicePath;
                    string[] objkeys = stubs.Keys.Where(x => (x.Length >= path.Length) && (x.Substring(0, path.Length) == path)).ToArray();
                    //if (objkeys.Count() == 0) throw new ServiceException("Unknown service path");

                    foreach (string path1 in objkeys)
                    {
                        try
                        {
                            stubs[path1].RRReleaseContext();

                            stubs.Remove(path1);
                        }
                        catch { }

                    }

                }
                else if (m.EntryType == MessageEntryType.CallbackCallReq)
                {
                    ProcessCallbackCall(m).IgnoreResult();
                }
                else
                {
                    throw new ServiceException("Unknown service command");
                }

            }
        }

        public MessageElementNestedElementList PackStructure(Object s)
        {
            return ServiceDef.PackStructure(s); ;
        }

        public T UnpackStructure<T>(MessageElementNestedElementList l)
        {
            return ServiceDef.UnpackStructure<T>(l);
        }

        public virtual object PackMapType<K, T>(object o)
        {
            return ServiceDef.PackMapType<K, T>(o);
        }

        public virtual object PackListType<T>(object o)
        {
            return ServiceDef.PackListType<T>(o);
        }

        public virtual object PackMultiDimArray(MultiDimArray multiDimArray)
        {
            return ServiceDef.PackMultiDimArray(multiDimArray);
        }

        public virtual object PackVarType(object p)
        {
            return ServiceDef.PackVarType(p);
        }

        public virtual object PackAnyType<T>(ref T p)
        {
            return ServiceDef.PackAnyType<T>(ref p);
        }

        public virtual object UnpackMapType<K, T>(object o)
        {
            return ServiceDef.UnpackMapType<K, T>(o);
        }

        public virtual object UnpackListType<T>(object o)
        {
            return ServiceDef.UnpackListType<T>(o);
        }

        public virtual MultiDimArray UnpackMultiDimArray(MessageElementNestedElementList o)
        {
            return ServiceDef.UnpackMultiDimArray(o);
        }

        public virtual object UnpackVarType(MessageElement o)
        {
            return ServiceDef.UnpackVarType(o);
        }

        public virtual T UnpackAnyType<T>(MessageElement o)
        {
            return ServiceDef.UnpackAnyType<T>(o);
        }

        protected string m_ServiceName;
        public string ServiceName { get { return m_ServiceName; } }

        public bool Connected { get { return m_Connected; } }
        protected bool m_Connected = false;

        private Transport connecttransport;
        private string connecturl;


        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public async Task<object> ConnectService(Transport c, string url, string username = null, object credentials = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
        {

            this.connecturl = url;
            this.connecttransport = c;
            var u = TransportUtil.ParseConnectionUrl(url);

            m_RemoteNodeID = u.nodeid;

            m_RemoteNodeName = u.nodename;
            m_ServiceName = u.service;

            //ProgramName = ProgramName1;
            /* try
             {*/
            //if (RobotRaconteurService.s.transports[c] == null) return null;
            if (!c.CanConnectService(url)) throw new ServiceException("Invalid transport");

            TransportConnection = await c.CreateTransportConnection(url, this, cancel);
            m_Connected = true;

            ClientServiceListener?.Invoke(this, ClientServiceListenerEventType.TransportConnectionConnected, null);

            c.TransportListeners+= delegate(Transport c, TransportListenerEventType evt, object param)
            {
                if (evt == TransportListenerEventType.TransportConnectionClosed &&  ((uint)param) == LocalEndpoint )
                {
                    Task.Run(() => ClientServiceListener?.Invoke(this, ClientServiceListenerEventType.TransportConnectionClosed, null));
                }
            };

            try
            {
                transport = c.TransportID;
                m_RemoteEndpoint = 0;

                ServiceDefinition[] d = await PullServiceDefinitionAndImports(null, cancel: cancel);

                lock (pulled_service_defs)
                {
                    foreach (var d2 in d)
                    {
                        if (!pulled_service_defs.ContainsKey(d2.Name))
                        {
                            pulled_service_defs.Add(d2.Name, d2);
                        }
                    }
                }

                if (!UsePulledServiceTypes)
                {
                    m_ServiceDef = node.GetServiceType(d[0].Name);
                }
                else
                {
                    var f = DynamicServiceFactory_.CreateServiceFactories(d.Select(x => x.ToString()).ToArray(), this);
                    lock (pulled_service_defs)
                    {
                        foreach (var f2 in f)
                        {
                            if (!pulled_service_types.ContainsKey(f2.GetServiceName()))
                            {
                                pulled_service_types.Add(f2.GetServiceName(), f2);
                            }
                        }
                    }
                    m_ServiceDef = GetPulledServiceType(d[0].Name);
                }

                MessageEntry e = new MessageEntry(MessageEntryType.ObjectTypeName, "");
                //e.AddElement("servicepath", ServiceName);
                e.ServicePath = ServiceName;

                MessageEntry ret = await ProcessRequest(e, cancel);
                if (ret.Error != MessageErrorType.None) return null;
                string type = ret.FindElement("objecttype").CastData<string>();
                if (type == "") return new ObjectNotFoundException("Could not find object type"); ;


                if (objecttype != null)
                {
                    VerifyObjectImplements(type, objecttype);
                    type = objecttype;
                    await PullServiceDefinitionAndImports(ServiceDefinitionUtil.SplitQualifiedName(type).Item2, cancel);
                }


                MessageEntry e2 = new MessageEntry();
                e2.ServicePath = ServiceName;
                e2.MemberName = "registerclient";
                e2.EntryType = MessageEntryType.ConnectClient;
                await ProcessRequest(e2, cancel);

                if (username != null)
                    await AuthenticateUser(username, credentials, cancel);

                ServiceStub stub = ServiceDef.CreateStub(type, ServiceName, this);
                stubs.Add(ServiceName, stub);
                Task noop = PeriodicTask.Run(PeriodicCleanupTask, TimeSpan.FromSeconds(5), cancel_source.Token).IgnoreResult();

                return stub;
            }
            catch (Exception e)
            {

                try
                {
                    TransportConnection.Close();
                }
                catch { }

                m_Connected = false;
                throw e;
            }


            /*}
            catch { 
                return null; 
            }*/
        }

        protected CancellationTokenSource cancel_source = new CancellationTokenSource();
        public async Task Close(CancellationToken cancel = default(CancellationToken))
        {
            cancel_source.Cancel();

            try
            {
                MessageEntry e = new MessageEntry(MessageEntryType.DisconnectClient, "");
                e.AddElement("servicename", ServiceName);
                await ProcessRequest(e, cancel);
            }
            catch (Exception)
            {
            }


            stubs.Clear();
            m_Connected = false;
            node.DeleteEndpoint(this);

            try
            {
                if (ClientServiceListener != null)
                    ClientServiceListener(this, ClientServiceListenerEventType.ClientClosed, null);
            }
            catch { }

        }

        public bool VerifyObjectImplements(string objecttype, string implementstype)
        {
            if (!VerifyObjectImplements2(objecttype, implementstype))
                throw new ServiceException("Invalid object type");
            return true;
        }

        protected bool VerifyObjectImplements2(string objecttype, string implementstype)
        {
            lock (this)
            {
                if (objecttype == implementstype) return true;
                var s1 = ServiceDefinitionUtil.SplitQualifiedName(objecttype);
                if (!pulled_service_defs.ContainsKey(s1.Item1))
                {
                    //TODO: handle this better?
                    return false;
                }

                if (!pulled_service_defs.ContainsKey(s1.Item1)) return false;

                var d = pulled_service_defs[s1.Item1];

                var o = d.Objects.FirstOrDefault(x => x.Value.Name == s1.Item2).Value;
                if (o == null) return false;
                foreach (var e in o.Implements)
                {
                    var deftype = d.Name;
                    var objtype = "";
                    if (e.Contains('.'))
                    {
                        objtype = e;
                    }
                    else
                    {
                        var s2 = ServiceDefinitionUtil.SplitQualifiedName(e);
                        deftype = s2.Item1;
                        objtype = s2.Item2;
                    }

                    if ((deftype + "." + objtype) == implementstype) return true;
                    if (VerifyObjectImplements2(deftype + "." + objtype, implementstype)) return true;
                }

                return false;
            }

        }


        public async Task SendPipeMessage(MessageEntry m, CancellationToken cancel)
        {
            //m.EntryType= MessageEntryType.PipePacket;
            await SendMessage(m, cancel);
        }


        public async Task SendWireMessage(MessageEntry m, CancellationToken cancel)
        {
            //m.EntryType= MessageEntryType.PipePacket;
            await SendMessage(m, cancel);
        }

        private bool m_UserAuthenticated = false;

        public bool UserAuthenticated { get { return m_UserAuthenticated; } }
        private string m_AuthenticatedUsername;
        public string AuthenticatedUsername { get { return m_AuthenticatedUsername; } }

        public async Task<string> AuthenticateUser(string username, object credentials, CancellationToken cancel = default(CancellationToken))
        {
            MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, "AuthenticateUser");
            m.ServicePath = ServiceName;
            m.AddElement("username", username);
            if (credentials is Dictionary<string, object>)
            {
                m.AddElement("credentials", PackMapType<string, object>(credentials));
            }
            else if (credentials is MessageElement)
            {
                MessageElement mcredentials = (MessageElement)credentials;
                mcredentials.ElementName = "credentials";
                m.AddElement(mcredentials);
            }
            MessageEntry ret = await ProcessRequest(m, cancel);
            m_AuthenticatedUsername = username;
            m_UserAuthenticated = true;
            return ret.FindElement("return").CastData<string>();
        }

        public async Task<string> LogoutUser(CancellationToken cancel = default(CancellationToken))
        {
            if (!UserAuthenticated) throw new AuthenticationException("User is not authenticated");

            m_UserAuthenticated = false;
            m_AuthenticatedUsername = null;

            MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, "LogoutUser");
            m.ServicePath = ServiceName;
            m.AddElement("username", AuthenticatedUsername);
            MessageEntry ret = await ProcessRequest(m, cancel);
            return ret.FindElement("return").CastData<string>();

        }

        public async Task<string> RequestObjectLock(object obj, RobotRaconteurObjectLockFlags flags, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only lock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            string command = "";
            if (flags == RobotRaconteurObjectLockFlags.USER_LOCK)
            {
                command = "RequestObjectLock";
            }
            else if (flags == RobotRaconteurObjectLockFlags.CLIENT_LOCK)
            {
                command = "RequestClientObjectLock";
            }
            else throw new InvalidOperationException("Unknown flags");

            MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, command);
            m.ServicePath = s.RRServicePath;

            MessageEntry ret = await ProcessRequest(m, cancel);
            return ret.FindElement("return").CastData<string>();


        }

        public async Task<string> ReleaseObjectLock(object obj, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only unlock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, "ReleaseObjectLock");
            m.ServicePath = s.RRServicePath;

            MessageEntry ret = await ProcessRequest(m, cancel);
            return ret.FindElement("return").CastData<string>();


        }


        public async Task<RobotRaconteurNode.MonitorLock> MonitorEnter(object obj, int timeout, CancellationToken cancel = default(CancellationToken))
        {
            bool iserror = true;
            IDisposable lock_ = null;
            try
            {

                if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only unlock object opened through Robot Raconteur");
                ServiceStub s = (ServiceStub)obj;
                lock_ = await s.rr_async_mutex.Lock();

                bool keep_trying = true;
                MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, "MonitorEnter");
                m.ServicePath = s.RRServicePath;
                m.AddElement("timeout", timeout);

                MessageEntry ret = await ProcessRequest(m, cancel);
                string retcode = ret.FindElement("return").CastData<string>();

                if (retcode == "OK")
                {
                    iserror = false;
                    return new RobotRaconteurNode.MonitorLock
                    {
                        lock_ = lock_,
                        stub = s
                    };


                }
                if (retcode == "Continue")
                {
                    while (keep_trying)
                    {
                        MessageEntry m1 = new MessageEntry(MessageEntryType.ClientSessionOpReq, "MonitorContinueEnter");
                        m1.ServicePath = s.RRServicePath;

                        MessageEntry ret1 = await ProcessRequest(m1, cancel);
                        string retcode1 = ret1.FindElement("return").CastData<string>();
                        if (retcode1 == "OK")
                        {
                            iserror = false;
                            return new RobotRaconteurNode.MonitorLock
                            {
                                lock_ = lock_,
                                stub = s
                            };
                        }
                        if (retcode1 != "Continue")
                        {
                            throw new ProtocolException("Unknown return code");
                        }
                    }
                }
                else
                {
                    throw new ProtocolException("Unknown return code");
                }

            }
            finally
            {
                if (iserror)
                {
                    if (lock_ != null)
                    {
                        try
                        {
                            lock_.Dispose();
                        }
                        catch { }
                    }
                    //Monitor.Exit(obj);
                }
            }

            throw new ProtocolException("Unknown return code");
        }

        public async Task MonitorExit(RobotRaconteurNode.MonitorLock lock_, CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                MessageEntry m = new MessageEntry(MessageEntryType.ClientSessionOpReq, "MonitorExit");
                m.ServicePath = lock_.stub.RRServicePath;

                MessageEntry ret = await ProcessRequest(m, cancel);
                string retcode = ret.FindElement("return").CastData<string>();
                if (retcode != "OK") throw new ProtocolException("Unknown return code");
            }
            finally
            {
                try
                {
                    lock_.lock_.Dispose();
                }
                catch { }
            }

        }


        protected DateTime LastMessageSentTime = DateTime.UtcNow;

        public void PeriodicCleanupTask()
        {
            if ((DateTime.UtcNow - LastMessageReceivedTime).TotalMilliseconds > node.EndpointInactivityTimeout)
            {
                Close().IgnoreResult();
            }

            if (m_RemoteEndpoint != 0)
            {
                if ((DateTime.UtcNow - LastMessageSentTime).TotalMilliseconds > 60000)
                {
                    MessageEntry m = new MessageEntry(MessageEntryType.ClientKeepAliveReq, "");
                    m.ServicePath = m_ServiceName;
                    m.RequestID = 0;
                    SendMessage(m, default(CancellationToken)).IgnoreResult();
                }
            }
        }

        public async Task<uint> CheckServiceCapability(string name, CancellationToken cancel = default(CancellationToken))
        {
            MessageEntry m = new MessageEntry(MessageEntryType.ServiceCheckCapabilityReq, name);
            m.ServicePath = m_ServiceName;
            MessageEntry ret = await ProcessRequest(m, cancel);
            uint res = ret.FindElement("return").CastData<uint>();
            return res;
        }

        public delegate void ClientServiceListenerDelegate(ClientContext client, ClientServiceListenerEventType ev, object parameter);

        public event ClientServiceListenerDelegate ClientServiceListener;

        protected async Task ProcessCallbackCall(MessageEntry m)
        {

            MessageEntry ret = null;
            try
            {
                ServiceStub s;
                lock (stubs)
                {
                    if (!stubs.ContainsKey(m.ServicePath)) throw new ServiceException("Stub not found");
                    s = stubs[m.ServicePath];
                }
                ret = await s.CallbackCall(m);
            }
            catch (Exception e)
            {
                ret = new MessageEntry(m.EntryType + 1, m.MemberName);
                ret.ServicePath = m.ServicePath;
                ret.RequestID = m.RequestID;
                RobotRaconteurExceptionUtil.ExceptionToMessageEntry(e, ret);

            }

            await SendMessage(ret, default(CancellationToken));

        }

        protected async Task<ServiceDefinition> PullServiceDefinition(string servicetype, CancellationToken cancel)
        {
            var e3 = new MessageEntry(MessageEntryType.GetServiceDesc, "");
            e3.ServicePath = m_ServiceName;
            if (servicetype != null)
            {
                e3.AddElement("ServiceType", servicetype);
            }

            var res = await ProcessRequest(e3, cancel);

            var def = res.FindElement("servicedef").CastData<string>();
            if (def == "") throw new ServiceNotFoundException("Could not find service definition");
            var d = new ServiceDefinition();
            d.FromString(def);

            if (servicetype == null)
            {

                if (res.elements.Count(x => x.ElementName == "attributes") != 0)
                {
                    lock (this)
                    {
                        Attributes = (Dictionary<string, object>)node.UnpackMapType<string, object>(res.FindElement("attributes").CastDataToNestedList(DataTypes.dictionary_t), null);
                    }
                }
            }

            return d;
        }

        protected async Task<ServiceDefinition[]> PullServiceDefinitionAndImports(string servicetype, CancellationToken cancel)
        {
            var defs = new List<ServiceDefinition>();
            var root = await PullServiceDefinition(servicetype, cancel);
            defs.Add(root);
            if (root.Imports.Count == 0)
            {
                return defs.ToArray();
            }

            foreach (var i in root.Imports)
            {
                if (defs.Count(x => x.Name == i) == 0)
                {
                    var defs2 = await PullServiceDefinitionAndImports(i, cancel);
                    foreach (var d in defs2)
                    {
                        if (defs.Count(x => x.Name == d.Name) == 0)
                        {
                            defs.Add(d);
                        }
                    }
                }
            }

            return defs.ToArray();
        }

        protected readonly Dictionary<string, ServiceFactory> pulled_service_types = new Dictionary<string, ServiceFactory>();

        public ServiceFactory GetPulledServiceType(string type)
        {
            ServiceFactory f;
            if (!TryGetPulledServiceType(type, out f))
            {
                throw new ServiceException("Unknown service type");
            }
            return f;
        }

        public bool TryGetPulledServiceType(string type, out ServiceFactory f)
        {
            lock (pulled_service_types)
            {
                return pulled_service_types.TryGetValue(type, out f);
            }
        }

        public string[] GetPulledServiceTypes()
        {
            lock (pulled_service_types)
            {
                return pulled_service_types.Keys.ToArray();
            }
        }

        protected readonly Dictionary<string, ServiceDefinition> pulled_service_defs = new Dictionary<string, ServiceDefinition>();

    }


    public enum ClientServiceListenerEventType
    {
        ClientClosed = 1,
        ClientConnectionTimeout,
        TransportConnectionConnected,
        TransportConnectionClosed,
        ServicePathReleased
    }

}
