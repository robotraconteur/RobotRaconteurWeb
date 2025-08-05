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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using static RobotRaconteurWeb.RRLogFuncs;

#pragma warning disable 1591

namespace RobotRaconteurWeb
{


    public abstract class ServiceSkel
    {

        public ServiceSkel(string s, Object o, ServerContext c)
        {
            m_ServicePath = s;
            m_context = c;
            uncastobj = o;
            if (o == null) throw new NullReferenceException();

            rr_node = c.node;

            string object_type_q = GetObjectType();

            var object_type_q_s = ServiceDefinitionUtil.SplitQualifiedName(object_type_q);
            var object_def = object_type_q_s.Item1;
            var object_type = object_type_q_s.Item2;

            RegisterEvents(o);
            InitPipeServers(o);
            InitCallbackServers(o);

            var d = rr_node.GetServiceType(object_def).ServiceDef();
            object_type_ver.Add(Tuple.Create(d.StdVer, object_type_q));
            if ((bool)d.StdVer)
            {

                if (d.Objects.TryGetValue(object_type, out var e))
                {
                    var found_defs = new HashSet<string>();
                    var found_versions = new HashSet<RobotRaconteurVersion>();
                    foreach (var s1 in e.Implements)
                    {
                        if (!s1.Contains("."))
                            continue;


                        var implement_def = ServiceDefinitionUtil.SplitQualifiedName(s1).Item1;
                        var implement_def_b = found_defs.Add(implement_def);
                        if (!implement_def_b)
                            continue;

                        var d2 = rr_node.GetServiceType(implement_def).ServiceDef();

                        var version_b = found_versions.Add(d2.StdVer);
                        if (!version_b)
                            continue;

                        object_type_ver.Add(Tuple.Create(d2.StdVer, s));

                        if (!(bool)d2.StdVer)
                            break;
                    }
                }
            }

            var init_obj = o as IRRServiceObject;
            if (init_obj != null)
            {
                init_obj.RRServiceObjectInit(c, s);
            }
        }

        List<Tuple<RobotRaconteurVersion, string>> object_type_ver = new List<Tuple<RobotRaconteurVersion, string>>();

        protected internal RobotRaconteurNode rr_node;
        protected internal readonly ClientContext rr_context = null;

        public virtual void InitCallbackServers(object o)
        { }



        public string ServicePath { get { return m_ServicePath; } }
        protected string m_ServicePath;

        public ServerContext RRContext { get { return m_context; } }
        protected ServerContext m_context;

        protected internal object uncastobj;

        public object UncastObject { get { return uncastobj; } }

        public abstract Task<MessageEntry> CallGetProperty(MessageEntry m);


        public abstract Task<MessageEntry> CallSetProperty(MessageEntry m);

        public abstract Task<MessageEntry> CallFunction(MessageEntry m);

        public abstract Task<object> GetSubObj(string name, string ind);

        public Task<object> GetSubObj(string name)
        {
            string[] s1 = name.Split(new char[] { '[' });
            if (s1.Length == 1)
            {
                return GetSubObj(name, "");
            }
            else
            {
                string ind = RRUriExtensions.UnescapeDataString(s1[1].Replace("]", ""));
                return GetSubObj(s1[0], ind);
            }
        }

        public virtual void RegisterEvents(Object obj1)
        {

            if (obj1 is IRobotRaconteurServiceObject)
            {
                IRobotRaconteurServiceObject obj2 = (IRobotRaconteurServiceObject)obj1;
                obj2.RobotRaconteurObjRefChanged += ObjRefChanged;
            }

        }

        public virtual void UnregisterEvents(Object obj1)
        {
            if (obj1 is IRobotRaconteurServiceObject)
            {
                IRobotRaconteurServiceObject obj2 = (IRobotRaconteurServiceObject)obj1;
                obj2.RobotRaconteurObjRefChanged -= ObjRefChanged;
            }
        }

        public virtual void InitPipeServers(Object obj1) { }

        public void ObjRefChanged(string name)
        {
            string path = ServicePath + "." + name;
            RRContext.ReplaceObject(path);
        }

        public void SendEvent(MessageEntry m)
        {
            m.ServicePath = ServicePath;
            RRContext.SendEvent(m);
        }

        public void ReleaseObject()
        {
            UnregisterEvents(uncastobj);
            uncastobj = null;
            m_context = null;
        }

        public abstract void ReleaseCastObject();

        public async Task SendPipeMessage(MessageEntry m, Endpoint e, CancellationToken cancel)
        {
            m.ServicePath = ServicePath;
            await RRContext.SendMessage(m, e, cancel).ConfigureAwait(false);
        }

        public async Task SendWireMessage(MessageEntry m, Endpoint e, CancellationToken cancel)
        {
            m.ServicePath = ServicePath;
            await RRContext.SendMessage(m, e, cancel).ConfigureAwait(false);
        }

        public virtual void DispatchPipeMessage(MessageEntry m, Endpoint e) { }

        public virtual void DispatchWireMessage(MessageEntry m, Endpoint e) { }

        public virtual Task<MessageEntry> CallPipeFunction(MessageEntry m, Endpoint e) { throw new MemberNotFoundException("Pipe " + m.MemberName + " not found"); }

        public virtual Task<MessageEntry> CallWireFunction(MessageEntry m, Endpoint e) { throw new MemberNotFoundException("Wire " + m.MemberName + " not found"); }

        public virtual object GetCallbackFunction(uint endpoint, string membername) { throw new MemberNotFoundException("Callback " + membername + " not found"); }

        public virtual Task<MessageEntry> CallMemoryFunction(MessageEntry m, Endpoint e) { throw new MemberNotFoundException("Memory " + m.MemberName + " not found"); }

        internal ServerContext.ObjectLock objectlock;

        internal ServerContext.MonitorObjectSkel monitorlock;

        internal Dictionary<uint, ServerContext.MonitorObjectSkel> monitorlocks = new Dictionary<uint, ServerContext.MonitorObjectSkel>();

        public bool IsLocked
        {
            get
            {
                if (objectlock == null) return false;
                return objectlock.IsLocked;
            }
        }

        public virtual bool IsRequestNoLock(MessageEntry m)
        {
            return false;
        }

        public bool IsMonitorLocked
        {
            get
            {
                if (monitorlock == null) return false;
                return monitorlock.IsLocked;
            }
        }

        protected Dictionary<int, GeneratorServerBase> generators = new Dictionary<int, GeneratorServerBase>();

        protected int GetNewGeneratorIndex()
        {
            int index;
            Random r = new Random();
            do
            {
                index = r.Next();
            }
            while (generators.ContainsKey(index));
            return index;
        }

        public virtual async Task<MessageEntry> CallGeneratorNext(MessageEntry m, Endpoint ep)
        {
            int index = m.FindElement("index").CastData<int[]>()[0];
            GeneratorServerBase gen;
            lock (generators)
            {
                if (!generators.TryGetValue(index, out gen))
                {
                    throw new InvalidOperationException("Invalid generator");
                }
                gen.last_access_time = DateTime.UtcNow;
            }

            if (gen.Endpoint != ep.LocalEndpoint)
            {
                throw new InvalidOperationException("Invalid generator");
            }
            return await gen.CallNext(m).ConfigureAwait(false);
        }

        public virtual string GetObjectType()
        {
            return Regex.Replace(GetType().ToString(), "_skel", "");
        }

        public string GetObjectType(RobotRaconteurVersion client_version)
        {
            if (!(bool)client_version)
            {
                return GetObjectType();
            }


            foreach (var e in object_type_ver)
            {
                if (!(bool)e.Item1)
                {
                    return e.Item2;
                }

                if (e.Item1 <= client_version)
                {

                    return e.Item2;
                }
            }

            throw new ObjectNotFoundException("Service requires newer client version");
        }
    }

    /**
    <summary>
    Context for services registered in a node for use by clients
    </summary>
    <remarks>
    <para>
    Services are registered using the RobotRaconteurNode.RegisterService() family of
    functions.
    The ServerContext manages the services, and dispatches requests and packets to the
    appropriate
    service object members. Services may expose more than one object. The root object is
    specified
    when the service is registered. Other objects are specified through ObjRef members. A name
    for the service is also specified when the service is registered. This name forms the root
    of the service path namespace. Other objects in the service have a unique service path
    based on the ObjRef used to access the object.
    </para>
    <para>
    Services may handle multiple connected clients concurrently. Each client is assigned
    a ServerEndpoint. The ServerEndpoint is unique to the client connection,
    and interacts with ServerContext to complete requests and dispatch packets. When
    the service needs to address a specific client, the ServerEndpoint or the
    ServerEndpoint.GetCurrentEndpoint() is used. (ServerEndpoint.GetCurrentEndpoint() returns
    the
    int local client ID.)
    </para>
    <para>
    Service attributes are a varvalue{string} types dictionary that is made available to
    clients during service discovery. These attributes are used to help clients determine
    which service should be selected for use. Because the attributes are passed to the clients
    as part of the discovery process, they should be as concise as possible, and should
    not use user defined types. Use ServerContext.SetAttributes() to set the service
    attributes
    after registering the service.
    </para>
    <para>
    Security for the service is specified using a ServiceSecurityPolicy instance. This policy
    is specified by passing as a parameter to RobotRaconteurNode.RegisterService(), or passing
    the policy to the constructor.
    </para>
    <para>
    ServerContext implements authentication and object locking.
    Server side functions are exposed by ServerContext for authentication, object locking,
    and client management.
    </para>
    <para> Clients using dynamic typing such as Python and MATLAB will only pull service types
    explicitly imported by the root object and objref objects that have been requested.
    Clients
    will not pull service types of user-defined named types if that service type is not
    explicitly
    imported. This can be problematic if new `struct`, `pod`, and/or `namedarray` types are
    introduced
    that do not have corresponding objects. Extra imports is used to specify extra service
    definitions
    the client should pull. Use ServerContext.AddExtraImport(),
    ServerContext.RemoveExtraImport(),
    and ServerContext.GetExtraImports() to manage the extra imports passed to the client.
    </para>
    </remarks>
    */

    [PublicApi]
    public class ServerContext
    {
        /**
        <summary>
        Get/Set the service attributes
        </summary>
        <remarks>
        Sets the service attributes. Attributes are made available to clients during
        service discovery. Attributes should be concise and not use any user defined
        types.
        </remarks>
        */

        [PublicApi]
        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public ServiceFactory ServiceDef { get { return m_ServiceDef; } }

        public async Task<ServiceFactory> GetRootObjectServiceDef(RobotRaconteurVersion client_version)
        {
            string root_object_type = await GetRootObjectType(client_version);

            var root_object_def = ServiceDefinitionUtil.SplitQualifiedName(root_object_type).Item1;
            return node.GetServiceType(root_object_def);
        }

        protected ServiceFactory m_ServiceDef;

        public string ServiceName { get { return m_ServiceName; } }

        protected string m_ServiceName;


        protected Dictionary<string, ServiceSkel> skels = new Dictionary<string, ServiceSkel>();

        protected Dictionary<uint, ServerEndpoint> client_endpoints = new Dictionary<uint, ServerEndpoint>();

        public string RootObjectType { get { return m_RootObjectType; } }

        protected string m_RootObjectType = "";





        /*public Message SendRequest(Message m)
        {
            return null;
        }*/


        protected internal readonly RobotRaconteurNode node;

        protected CancellationTokenSource cancel_source = new CancellationTokenSource();

        public ServerContext(ServiceFactory f, RobotRaconteurNode node = null)
        {
            m_ServiceDef = f;
            if (node != null)
            {
                this.node = node;
            }
            else
            {
                this.node = RobotRaconteurNode.s;
            }

            var noop = PeriodicTask.Run(PeriodicCleanupTask, TimeSpan.FromSeconds(5), cancel_source.Token);
        }

        public virtual Task SendEvent(MessageEntry m)
        {


            Message mm = new Message();

            ServerEndpoint[] cc;
            lock (client_endpoints)
            {
                cc = client_endpoints.Values.ToArray();
            }

            foreach (ServerEndpoint c in cc)
            {

                if (RequireValidUser)
                {
                    if (String.IsNullOrWhiteSpace(c.AuthenticatedUsername))
                        continue;
                }

                try
                {
                    node.CheckConnection(c.LocalEndpoint);
                    SendMessage(m, c, default(CancellationToken)).ContinueWith(delegate (Task t)
                    {
                        var e = t.Exception;
                        if (e != null)
                        {
                            try
                            {
                                RemoveClient(c);
                            }
                            catch (Exception)
                            { }
                        }
                    });
                }
                catch (Exception)
                {
                    try
                    {
                        RemoveClient(c);
                    }
                    catch (Exception)
                    { }
                }
            }

            return Task.FromResult(0);

        }

        public async virtual Task SendMessage(MessageEntry m, Endpoint e, CancellationToken cancel)
        {


            //m.ServicePath = ServiceName;

            Message mm = new Message();
            mm.header = new MessageHeader();
            //mm.header.ReceiverEndpoint = RemoteEndpoint;
            mm.entries.Add(m);

            await e.SendMessage(mm, cancel).ConfigureAwait(false);


        }

        private object rec_sync = new object();



        bool base_object_set = false;

        protected void SetSecurityPolicy(ServiceSecurityPolicy policy)
        {
            user_authenticator = policy.Authenticator;
            security_policies = policy.Policies;

            if (security_policies.Keys.Contains("requirevaliduser"))
            {
                if (security_policies["requirevaliduser"].ToLower() == "true") RequireValidUser = true;
            }

            if (security_policies.Keys.Contains("allowobjectlock"))
            {
                if (security_policies["allowobjectlock"].ToLower() == "true") AllowObjectLock = true;
            }


        }


        public virtual void SetBaseObject(string name, object o, ServiceSecurityPolicy policy = null)
        {
            if (base_object_set) throw new InvalidOperationException("Base object already set");

            if (policy != null)
            {
                SetSecurityPolicy(policy);
            }

            m_ServiceName = name;
            ServiceSkel s = ServiceDef.CreateSkel(name, o, this);

            m_RootObjectType = s.GetType().ToString().Replace("_skel", "");
            base_object_set = true;
            lock (skels)
            {
                skels[name] = s;
            }

        }

        readonly Dictionary<string, Task<ServiceSkel>> get_sub_obj_tasks = new Dictionary<string, Task<ServiceSkel>>();

        public virtual async Task<ServiceSkel> GetObjectSkel(string servicepath)
        {

            //object obj = null;
            string[] p = servicepath.Split(new char[] { '.' });

            string ppath = p[0];

            ServiceSkel skel;
            lock (skels)
            {
                skel = skels[ppath];
            }
            //obj = skel.uncastobj;

            ServiceSkel skel1 = skel;

            for (int i = 1; i < p.Length; i++)
            {

                string ppath1 = ppath;
                ppath = String.Join(".", new string[] { ppath, p[i] });

                skel1 = null;
                lock (skels)
                {
                    if (skels.Keys.Contains(ppath))
                    {
                        skel1 = skels[ppath];
                    }
                }

                if (skel1 == null)
                {
                    try
                    {
                        Task<ServiceSkel> t;
                        lock (get_sub_obj_tasks)
                        {
                            if (get_sub_obj_tasks.ContainsKey(ppath))
                            {
                                t = get_sub_obj_tasks[ppath];
                            }
                            else
                            {
                                t = GetObjectSkel2(ppath1, p[i], skel);
                                get_sub_obj_tasks.Add(ppath, t);
                            }

                        }

                        skel1 = await t.ConfigureAwait(false);
                    }
                    finally
                    {
                        lock (get_sub_obj_tasks)
                        {
                            if (get_sub_obj_tasks.ContainsKey(ppath))
                            {
                                get_sub_obj_tasks.Remove(ppath);
                            }
                        }
                    }
                }

                skel = skel1;
            }

            return skel;
        }

        async Task<ServiceSkel> GetObjectSkel2(string ppath1, string objname, ServiceSkel skel)
        {
            m_CurrentServicePath = ppath1;
            m_CurrentServerContext = this;
            object obj1 = await skel.GetSubObj(objname).ConfigureAwait(false);
            m_CurrentServicePath = null;
            m_CurrentServerContext = null;

            var ppath = String.Join(".", new string[] { ppath1, objname });

            var skel1 = ServiceDef.CreateSkel(ppath, obj1, this);
            if (skel.objectlock != null)
            {
                skel.objectlock.AddSkel(skel1);
            }
            skels[ppath] = skel1;
            return skel1;
        }


        public virtual void ReplaceObject(string path)
        {

            ReleaseServicePath(path);


        }

        /**
        <summary>
        Get the current ServerContext
        </summary>
        <remarks>
        Returns the current server context during a request or packet event.
        This is a thread-specific value and only
        valid during the initial request or packet event invocation.
        </remarks>
        */

        [PublicApi]
        public static ServerContext CurrentServerContext { get { return m_CurrentServerContext; } }
        [ThreadStatic()]
        private static ServerContext m_CurrentServerContext;
        /**
        <summary>
        Get the current object service path
        </summary>
        <remarks>
        Returns the service path of the current object during a request or
        packet event.
        This is a thread-specific value and only
        valid during the initial request or packet event invocation.
        </remarks>
        <returns>The current object service path</returns>
        */

        [PublicApi]
        public static string CurrentServicePath { get { return m_CurrentServicePath; } }
        [ThreadStatic()]
        private static string m_CurrentServicePath;


        public virtual async Task<MessageEntry> ProcessMessageEntry(MessageEntry m, ServerEndpoint c)
        {
            //lock (rec_sync)
            //{

            bool noreturn = false;
            MessageEntry ret = null;


            if (m.EntryType == MessageEntryType.ServicePathReleasedRet) return null;

            try
            {
                //ClientSessionOp methods
                if (m.EntryType == MessageEntryType.ClientSessionOpReq)
                {
                    return await ClientSessionOp(m, c).ConfigureAwait(false);

                }

                if (m.EntryType == MessageEntryType.ClientKeepAliveReq)
                {
                    ret = new MessageEntry(MessageEntryType.ClientKeepAliveRet, m.MemberName);
                    ret.RequestID = m.RequestID;
                    ret.ServicePath = m.ServicePath;
                    return ret;
                }

                if (m.EntryType == MessageEntryType.ServiceCheckCapabilityReq)
                {
                    ret = CheckServiceCapability(m, c);
                }



                if (RequireValidUser)
                {
                    if (ServerEndpoint.CurrentAuthenticatedUser == null)
                        throw new AuthenticationException("User must authenticate before accessing this service");
                }

                if (m.EntryType == MessageEntryType.PipePacket || m.EntryType == MessageEntryType.PipePacketRet)
                {
                    (await GetObjectSkel(m.ServicePath).ConfigureAwait(false)).DispatchPipeMessage(m, c);
                    ret = null;
                    noreturn = true;
                }

                if (m.EntryType == MessageEntryType.WirePacket)
                {
                    (await GetObjectSkel(m.ServicePath).ConfigureAwait(false)).DispatchWireMessage(m, c);
                    ret = null;
                    noreturn = true;
                }





                m_CurrentServicePath = m.ServicePath;
                m_CurrentServerContext = this;

                //Object member methods

                if (m.EntryType == MessageEntryType.PropertyGetReq)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallGetProperty(m).ConfigureAwait(false);
                }

                if (m.EntryType == MessageEntryType.PropertySetReq)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallSetProperty(m).ConfigureAwait(false);
                }

                if (m.EntryType == MessageEntryType.FunctionCallReq)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallFunction(m).ConfigureAwait(false);
                }

                if (m.EntryType == MessageEntryType.PipeConnectReq || m.EntryType == MessageEntryType.PipeDisconnectReq)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallPipeFunction(m, c).ConfigureAwait(false);
                }

                if (m.EntryType == MessageEntryType.WireConnectReq || m.EntryType == MessageEntryType.WireDisconnectReq || m.EntryType == MessageEntryType.WirePeekInValueReq || m.EntryType == MessageEntryType.WirePeekOutValueReq || m.EntryType == MessageEntryType.WirePokeOutValueReq)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallWireFunction(m, c).ConfigureAwait(false);
                }



                if (m.EntryType == MessageEntryType.MemoryWrite || m.EntryType == MessageEntryType.MemoryRead || m.EntryType == MessageEntryType.MemoryGetParam)
                {
                    ServiceSkel skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallMemoryFunction(m, c).ConfigureAwait(false);
                }

                else if (m.EntryType == MessageEntryType.CallbackCallRet)
                {
                    // Console.WriteLine("Got " + m.TransactionID + " " + m.EntryType + " " + m.MemberName);
                    TaskCompletionSource<MessageEntry> r = null;
                    lock (rec_wait)
                    {
                        uint t_id = m.RequestID;
                        if (rec_wait.ContainsKey(t_id))
                        {
                            r = rec_wait[t_id].Item1;
                        }
                    }

                    if (r != null) r.TrySetResult(m);
                    noreturn = true;

                }
                else if (m.EntryType == MessageEntryType.GeneratorNextReq)
                {
                    var skel = await GetObjectSkel(m.ServicePath).ConfigureAwait(false);
                    check_lock(skel, m);
                    ret = await skel.CallGeneratorNext(m, c).ConfigureAwait(false);
                    noreturn = true;
                }
                if (m.EntryType == MessageEntryType.ObjectTypeName)
                {
                    RobotRaconteurVersion v = default;
                    if (m.TryFindElement("clientversion", out var m_ver))
                    {
                        v.FromString(m_ver.CastDataToString());

                    }

                    ret = new MessageEntry(MessageEntryType.ObjectTypeNameRet, m.MemberName);
                    string objtype = await GetObjectType(m.ServicePath, v);
                    ret.AddElement("objecttype", objtype);
                }

            }
            catch (Exception e)
            {
#if RR_LOG_DEBUG
                LogDebug(string.Format("Error processing service entry: {0}", e.ToString()), node,
                    RobotRaconteur_LogComponent.Service, service_path: m.ServicePath, member: m.MemberName, endpoint: c.LocalEndpoint);
#endif
                ret = new MessageEntry(m.EntryType + 1, m.MemberName);
                RobotRaconteurExceptionUtil.ExceptionToMessageEntry(e, ret);

            }

            m_CurrentServicePath = null;
            m_CurrentServerContext = null;

            if (ret == null && !noreturn && (int)m.EntryType % 2 == 1)
            {
                ret = new MessageEntry(m.EntryType + 1, m.MemberName);
                ret.Error = MessageErrorType.ProtocolError;
                ret.AddElement("errorname", "RobotRaconteur.ProtocolError");
                ret.AddElement("errorstring", "Unknown transaction type");

            }

            if (!noreturn)
            {
                ret.ServicePath = m.ServicePath;
                ret.RequestID = m.RequestID;
            }

            return ret;


            //}



        }

        public virtual void Close()
        {
            cancel_source.Cancel();
            try
            {
                MessageEntry e = new MessageEntry(MessageEntryType.ServiceClosed, "");
                SendEvent(e).IgnoreResult();
            }
            catch { }

            ServerEndpoint[] eea = client_endpoints.Values.ToArray();

            foreach (ServerEndpoint ee in eea)
            {
                try
                {
                    node.DeleteEndpoint(ee);
                }
                catch { }
            }

            client_endpoints.Clear();

            foreach (ServiceSkel s in skels.Values)
            {
                try
                {
                    s.ReleaseObject();
                }
                catch { }
            }

            try
            {
                if (ServerServiceListener != null)
                    ServerServiceListener(this, ServerServiceListenerEventType.ServiceClosed, null);
            }
            catch { }
        }



        public virtual void MessageReceived(Message m, ServerEndpoint e)
        {
            MessageReceived2(m, e).IgnoreResult();

        }

        public virtual async Task MessageReceived2(Message m, ServerEndpoint e)
        {
            Message mret = new Message();
            mret.header = new MessageHeader();

            foreach (MessageEntry mm in m.entries)
            {
                if (mm.Error == MessageErrorType.InvalidEndpoint)
                {
                    this.RemoveClient(e);
                    return;
                }

                MessageEntry mmret = await ProcessMessageEntry(mm, e).ConfigureAwait(false);
                if (mmret != null)
                    mret.entries.Add(mmret);
            }
            if (mret.entries.Count > 0)
                await e.SendMessage(mret, default(CancellationToken)).ConfigureAwait(false);
        }

        public virtual void AddClient(ServerEndpoint cendpoint)
        {
            lock (client_endpoints)
            {
                client_endpoints.Add(cendpoint.LocalEndpoint, cendpoint);
            }

            try
            {
                if (ServerServiceListener != null)
                    ServerServiceListener(this, ServerServiceListenerEventType.ClientConnected, cendpoint.LocalEndpoint);
            }
            catch { }

        }

        public virtual void RemoveClient(ServerEndpoint cendpoint)
        {

            //TODO: possible deadlock

            string cusername = cendpoint.AuthenticatedUsername;
            uint ce = cendpoint.LocalEndpoint;

            lock (client_endpoints)
            {
                client_endpoints.Remove(cendpoint.LocalEndpoint);
            }
            node.DeleteEndpoint(cendpoint);


            lock (ClientLockOp_lockobj)
            {
                KeyValuePair<string, ObjectLock>[] oo = active_object_locks.ToArray();
                foreach (KeyValuePair<string, ObjectLock> o in oo)
                {
                    try
                    {
                        if (o.Value.Username == cusername)
                        {
                            if (o.Value.Endpoint == ce)
                            {
                                o.Value.ReleaseLock();
                                active_object_locks.Remove(o.Key);
                            }
                            else
                            {
                                lock (client_endpoints)
                                {
                                    if (!client_endpoints.Any(x => x.Value.AuthenticatedUsername == cusername))
                                    {
                                        o.Value.ReleaseLock();
                                        active_object_locks.Remove(o.Key);
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                }
            }



            try
            {
                if (ServerServiceListener != null)
                    ServerServiceListener(this, ServerServiceListenerEventType.ClientDisconnected, cendpoint.LocalEndpoint);
            }
            catch { }
        }

        public virtual MessageElementNestedElementList PackStructure(Object s)
        {
            return ServiceDef.PackStructure(s); ;
        }

        public virtual T UnpackStructure<T>(MessageElementNestedElementList l)
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
            return ServiceDef.PackAnyType(ref p);
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

        private UserAuthenticator user_authenticator;
        private Dictionary<string, string> security_policies;

        public bool RequireValidUser { get; private set; } = false;
        public bool AllowObjectLock { get; private set; } = false;

        private async Task<MessageEntry> ClientSessionOp(MessageEntry m, ServerEndpoint e)
        {
            if (user_authenticator == null && !m.MemberName.StartsWith("Monitor")) throw new InvalidOperationException("User authentication not activated for this service");

            MessageEntry ret = new MessageEntry(MessageEntryType.ClientSessionOpRet, m.MemberName);
            ret.RequestID = m.RequestID;
            ret.ServicePath = m.ServicePath;

            string command = m.MemberName;
            switch (command)
            {
                case "AuthenticateUser":
                    {
                        string username = m.FindElement("username").CastData<string>();
                        Dictionary<string, object> credentials = (Dictionary<string, object>)UnpackMapType<string, object>(m.FindElement("credentials").Data);
                        e.AuthenticateUser(username, credentials);
                        ret.AddElement("return", "OK");
                        return ret;
                    }
                case "LogoutUser":
                    {
                        e.LogoutUser();
                        ret.AddElement("return", "OK");
                        return ret;
                    }

                case "RequestObjectLock":
                case "ReleaseObjectLock":
                case "RequestClientObjectLock":
                case "ReleaseClientObjectLock":
                case "MonitorEnter":
                case "MonitorContinueEnter":
                case "MonitorExit":
                    {

                        await ClientLockOp(m, ret).ConfigureAwait(false);
                        ret.AddElement("return", "OK");
                        return ret;
                    }

                default:
                    throw new ProtocolException("Invalid ClientSessionOp command");

            }


            throw new ProtocolException("Error evaluating ClientSessionOp command");
        }

        public AuthenticatedUser AuthenticateUser(string username, Dictionary<string, object> credentials)
        {
            return user_authenticator.AuthenticateUser(username, credentials);

        }



        private object ClientLockOp_lockobj = new object();

        private async Task ClientLockOp(MessageEntry m, MessageEntry ret)
        {


            // if (m.ServicePath != ServiceName) throw new Exception("Only locking of root object currently supported");

            string[] priv = null;
            string username = null;
            if (!m.MemberName.StartsWith("Monitor"))
            {
                if (ServerEndpoint.CurrentAuthenticatedUser == null) throw new AuthenticationException("User must be authenticated to lock object");
                priv = ServerEndpoint.CurrentAuthenticatedUser.Privileges;
                if (!(priv.Contains("objectlock") || priv.Contains("objectlockoverride"))) throw new ObjectLockedException("User does not have object locking privileges");
                username = ServerEndpoint.CurrentAuthenticatedUser.Username;
            }
            else
            {
                if (this.RequireValidUser)
                {
                    if (ServerEndpoint.CurrentAuthenticatedUser == null)
                    {
                        throw new AuthenticationException("User is not authenticated");
                    }
                }
            }



            string servicepath = m.ServicePath;

            ServiceSkel skel = await GetObjectSkel(servicepath).ConfigureAwait(false);

            switch (m.MemberName)
            {
                case "RequestObjectLock":
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.IsLocked) throw new ObjectLockedException("Object already locked");
                            KeyValuePair<string, ServiceSkel>[] sskels = (skels.Where(x => x.Key.StartsWith(servicepath)).ToArray());
                            if (sskels.Any(x => x.Value.IsLocked)) throw new ObjectLockedException("Object already locked");

                            ObjectLock o = new ObjectLock(ServerEndpoint.CurrentAuthenticatedUser.Username, skel);
                            foreach (KeyValuePair<string, ServiceSkel> s in sskels)
                            {
                                o.AddSkel(s.Value);
                            }
                            active_object_locks.Add(o.RootServicePath, o);
                            ret.AddElement("return", "OK");
                        }
                        break;

                    }
                case "RequestClientObjectLock":
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.IsLocked) throw new ObjectLockedException("Object already locked");
                            KeyValuePair<string, ServiceSkel>[] sskels = (skels.Where(x => x.Key.StartsWith(servicepath)).ToArray());
                            if (sskels.Any(x => x.Value.IsLocked)) throw new ObjectLockedException("Object already locked");

                            ObjectLock o = new ObjectLock(ServerEndpoint.CurrentAuthenticatedUser.Username, skel, ServerEndpoint.CurrentEndpoint.LocalEndpoint);
                            foreach (KeyValuePair<string, ServiceSkel> s in sskels)
                            {
                                s.Value.objectlock = o;
                            }
                            active_object_locks.Add(o.RootServicePath, o);
                            ret.AddElement("return", "OK");
                        }
                        break;

                    }

                case "ReleaseObjectLock":
                    lock (ClientLockOp_lockobj)
                    {
                        if (!skel.IsLocked) return;
                        if (skel.objectlock.RootServicePath != servicepath) throw new ObjectLockedException("Cannot release inherited lock");
                        if (username != skel.objectlock.Username && !priv.Contains("objectlockoverride")) throw new ObjectLockedException("Service locked by user " + skel.objectlock.Username);
                        if (skel.objectlock.Endpoint != 0)
                        {
                            if (ServerEndpoint.CurrentEndpoint.LocalEndpoint != skel.objectlock.Endpoint && !priv.Contains("objectlockoverride")) if (username != skel.objectlock.Username && !priv.Contains("objectlockoverride")) throw new Exception("Service locked by other session");
                        }

                        active_object_locks.Remove(skel.ServicePath);
                        skel.objectlock.ReleaseLock();

                        ret.AddElement("return", "OK");
                    }
                    break;
                case "MonitorEnter":
                    {
                        int timeout = 0;
                        MonitorObjectSkel s;
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.monitorlocks.ContainsKey(ServerEndpoint.CurrentEndpoint.LocalEndpoint)) throw new InvalidOperationException("Already acquiring or acquired monitor lock");
                            s = new MonitorObjectSkel(skel);
                            timeout = m.FindElement("timeout").CastData<int[]>()[0];
                        }
                        string retcode = await s.MonitorEnter(ServerEndpoint.CurrentEndpoint.LocalEndpoint, timeout).ConfigureAwait(false);
                        ret.AddElement("return", retcode);

                        break;
                    }
                case "MonitorContinueEnter":
                    {
                        MonitorObjectSkel s;
                        lock (ClientLockOp_lockobj)
                        {
                            if (!skel.monitorlocks.ContainsKey(ServerEndpoint.CurrentEndpoint.LocalEndpoint)) throw new InvalidOperationException("Not acquiring monitor lock");
                            s = skel.monitorlocks[ServerEndpoint.CurrentEndpoint.LocalEndpoint];
                        }
                        string retcode = await s.MonitorContinueEnter(ServerEndpoint.CurrentEndpoint.LocalEndpoint).ConfigureAwait(false);
                        ret.AddElement("return", retcode);
                        break;
                    }

                case "MonitorExit":
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.monitorlock.LocalEndpoint != (ServerEndpoint.CurrentEndpoint.LocalEndpoint)) throw new InvalidOperationException("Not monitor locked");
                        }
                        string retcode = await skel.monitorlock.MonitorExit(ServerEndpoint.CurrentEndpoint.LocalEndpoint).ConfigureAwait(false);
                        ret.AddElement("return", retcode);
                        break;

                    }

                default:
                    throw new Exception("Invalid command");


            }

        }

        protected void check_lock(ServiceSkel skel, MessageEntry m)
        {
            check_monitor_lock(skel);
            if (skel.IsLocked)
            {
                if (skel.IsRequestNoLock(m))
                {
                    return;
                }
                if (skel.objectlock.Username == ServerEndpoint.CurrentAuthenticatedUser.Username && skel.objectlock.Endpoint == 0)
                    return;
                if (skel.objectlock.Username == ServerEndpoint.CurrentAuthenticatedUser.Username && skel.objectlock.Endpoint == ServerEndpoint.CurrentEndpoint.LocalEndpoint)
                    return;
                throw new ObjectLockedException("Object locked by " + skel.objectlock.Username);

            }
        }

        protected void check_monitor_lock(ServiceSkel skel)
        {
            if (skel.IsMonitorLocked)
            {
                if (skel.monitorlock.LocalEndpoint == ServerEndpoint.CurrentEndpoint.LocalEndpoint)
                {
                    skel.monitorlock.MonitorRefresh(ServerEndpoint.CurrentEndpoint.LocalEndpoint);
                }
                else
                {
                    throw new Exception("Object is currently monitor locked. Use MonitorEnter to obtain monitor lock");
                }

            }

        }

        protected Dictionary<string, ObjectLock> active_object_locks = new Dictionary<string, ObjectLock>();

        public class ObjectLock
        {

            bool m_Locked = true;
            string m_Username = null;
            List<ServiceSkel> skels = new List<ServiceSkel>();
            ServiceSkel m_RootSkel;
            uint m_Endpoint = 0;


            string m_RootServicePath = null;

            public ObjectLock(string username, ServiceSkel root_skel, uint endpoint = 0)
            {
                lock (skels)
                {
                    m_Locked = true;
                    m_RootSkel = root_skel;
                    m_Username = username;
                    m_RootServicePath = root_skel.ServicePath;
                    m_Endpoint = endpoint;
                }
            }

            public string Username { get { return m_Username; } }
            public bool IsLocked { get { return m_Locked; } }
            public string RootServicePath { get { return m_RootServicePath; } }
            public uint Endpoint { get { return m_Endpoint; } }

            public void AddSkel(ServiceSkel skel)
            {
                lock (skels)
                {
                    skel.objectlock = this;
                    string sp = skel.ServicePath;
                    if (sp == m_RootServicePath)
                    {

                        if (!Object.ReferenceEquals(skel, m_RootSkel)) return;

                        try
                        {
                            if (m_RootSkel != null) m_RootSkel.objectlock = this;
                        }
                        catch { }
                        m_RootSkel = skel;
                        return;
                    }


                    skels.Add(skel);
                }





            }

            public void ReleaseSkel(ServiceSkel skel)
            {
                lock (skels)
                {
                    try
                    {
                        skel.objectlock = null;
                    }
                    catch { }

                    string sp = skel.ServicePath;
                    if (sp == m_RootServicePath)
                    {
                        try
                        {
                            if (m_RootSkel != null) m_RootSkel.objectlock = null;
                        }
                        catch { }
                        m_RootSkel = null;
                        return;
                    }

                    skels.Remove(skel);


                }
            }

            public void ReleaseLock()
            {
                lock (skels)
                {
                    m_Locked = false;

                    try
                    {
                        m_RootSkel.objectlock = null;
                    }
                    catch { }

                    foreach (ServiceSkel s in skels)
                    {
                        try
                        {
                            s.objectlock = null;
                        }
                        catch { }

                    }

                    skels.Clear();

                }



            }


        }

        public virtual void PeriodicCleanupTask()
        {
            lock (client_endpoints)
            {
                foreach (var c in client_endpoints.Values)
                {
                    c.PeriodicCleanupTask();
                }

            }


        }

        protected MessageEntry CheckServiceCapability(MessageEntry m, ServerEndpoint c)
        {
            MessageEntry ret = new MessageEntry(MessageEntryType.ServiceCheckCapabilityRet, m.MemberName);
            ret.ServicePath = m.ServicePath;
            ret.RequestID = m.RequestID;
            ret.AddElement("return", (uint)0);
            return ret;

        }

        /// <summary>
        ///  Server service listener event type
        /// </summary>
        /// <param name="service">The context that generated the event</param>
        /// <param name="ev">The event type</param>
        /// <param name="parameter">The event parameter</param>
        [PublicApi]
        public delegate void ServerServiceListenerDelegate(ServerContext service, ServerServiceListenerEventType ev, object parameter);

        /// <summary>
        /// Server service listener event
        /// </summary>
        [PublicApi]
        public event ServerServiceListenerDelegate ServerServiceListener;
        /**
        <summary> Release the specified service path and all sub objects Services take ownership of
        objects returned by objrefs, and will only request the object once. Subsequent requests will
        return the cached object. If the objref has changed, the service must call
        ReleaseServicePath() to tell the service to request the object again. Release service path
        will release the object specified by the service path and all sub objects. This overload
        will notify all clients that the objref has been released. If the service path contains a
        session key, use ReleaseServicePath(string, uint[]) to only
        notify the client that owns the session.
        </summary>
        <remarks>None</remarks>
        <param name="path">The service path to release</param>
        */

        [PublicApi]
        public void ReleaseServicePath(string path)
        {

            if (path == ServiceName) throw new ServiceException("Root object cannot be released");

            lock (skels)
            {
                string[] objkeys = skels.Keys.Where(x => x.StartsWith(path)).ToArray();
                if (objkeys.Count() == 0) throw new ServiceException("Unknown service path");

                foreach (string path1 in objkeys)
                {
                    ServiceSkel s = skels[path1];

                    if (s.IsLocked)
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (s.objectlock.RootServicePath == path1)
                            {
                                active_object_locks.Remove(s.objectlock.Username);
                                s.objectlock.ReleaseLock();

                            }
                            else
                            {
                                s.objectlock.ReleaseSkel(s);
                            }
                        }
                    }

                    s.ReleaseObject();
                    s.ReleaseCastObject();
                    skels.Remove(path1);

                }
            }

            MessageEntry m = new MessageEntry(MessageEntryType.ServicePathReleasedReq, "");
            m.ServicePath = path;

            SendEvent(m);


        }
        /**
        <summary>
        Release the specified service path and all sub objects
        </summary>
        <remarks>
        <para>
        Services take ownership of objects returned by objrefs, and will only request the object
        once. Subsequent requests will return the cached object. If the objref has changed,
        the service must call ReleaseServicePath() to tell the service to request the object
        again.
        Release service path will release the object specified by the service path
        and all sub objects.
        </para>
        <para> This overload will notify the specified that the objref has been released. If the
        service
        path contains a session key, this overload should be used so the session key is not
        leaked.
        </para>
        </remarks>
        <param name="path">The service path to release</param>
        <param name="endpoints">The client endpoint IDs to notify of the released service path</param>
        */

        [PublicApi]
        public void ReleaseServicePath(string path, List<uint> endpoints)
        {

            if (path == ServiceName) throw new ServiceException("Root object cannot be released");

            lock (skels)
            {
                string[] objkeys = skels.Keys.Where(x => x.StartsWith(path)).ToArray();
                if (objkeys.Count() == 0) throw new ServiceException("Unknown service path");

                foreach (string path1 in objkeys)
                {
                    ServiceSkel s = skels[path1];

                    if (s.IsLocked)
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (s.objectlock.RootServicePath == path1)
                            {
                                active_object_locks.Remove(s.objectlock.Username);
                                s.objectlock.ReleaseLock();

                            }
                            else
                            {
                                s.objectlock.ReleaseSkel(s);
                            }
                        }
                    }

                    s.ReleaseObject();
                    s.ReleaseCastObject();
                    skels.Remove(path1);

                }
            }

            MessageEntry m = new MessageEntry(MessageEntryType.ServicePathReleasedReq, "");
            m.ServicePath = path;

            var cc = new List<ServerEndpoint>();

            lock (client_endpoints)
            {
                foreach (var e in endpoints)
                {
                    if (client_endpoints.ContainsKey(e))
                    {
                        cc.Add(client_endpoints[e]);
                    }
                }

            }

            foreach (var c in cc)
            {
                if (RequireValidUser)
                {
                    try
                    {
                        if (String.IsNullOrWhiteSpace(c.AuthenticatedUsername))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }


                }

                try
                {
                    node.CheckConnection(c.LocalEndpoint);
                    SendMessage(m, c, default(CancellationToken)).ContinueWith(delegate (Task t)
                    {
                        var e = t.Exception;
                        if (e != null)
                        {
                            try
                            {
                                RemoveClient(c);
                            }
                            catch (Exception)
                            { }
                        }
                    });
                }
                catch (Exception)
                {
                    try
                    {
                        RemoveClient(c);
                    }
                    catch (Exception)
                    { }
                }
            }

        }


        private Dictionary<uint, Tuple<TaskCompletionSource<MessageEntry>, ServerEndpoint>> rec_wait = new Dictionary<uint, Tuple<TaskCompletionSource<MessageEntry>, ServerEndpoint>>();
        uint request_number = 0;

        public async Task<MessageEntry> ProcessCallbackRequest(MessageEntry m, uint endpointid, CancellationToken cancel)
        {

            ServerEndpoint e = client_endpoints[endpointid];

            DateTime request_start = DateTime.UtcNow;
            uint request_timeout = node.RequestTimeout;

            TaskCompletionSource<MessageEntry> rec_source = new TaskCompletionSource<MessageEntry>();
            uint t_id;
            lock (rec_wait)
            {
                request_number++;
                m.RequestID = request_number;
                t_id = request_number;
                rec_wait.Add(t_id, Tuple.Create(rec_source, e));
                if (ProcessCallbackRequest_checkconnection_current == null)
                {
                    ProcessCallbackRequest_checkconnection_current = ProcessCallbackRequest_checkconnection();
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
                    await SendMessage(m, e, cancel).ConfigureAwait(false);
                    rec_message = await rec_source.Task.ConfigureAwait(false);
                };

                await r().AwaitWithTimeout((int)node.RequestTimeout).ConfigureAwait(false);
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
                Exception e1 = RobotRaconteurExceptionUtil.MessageEntryToException(rec_message);
                RobotRaconteurRemoteException e2 = e1 as RobotRaconteurRemoteException;
                if (e2 != null)
                {
                    Exception e3 = ServiceDef.DownCastException(e2);
                    if (e3 != null) e1 = e3;
                }
                throw e1;
            }

            return rec_message;

        }

        Task ProcessCallbackRequest_checkconnection_current = null;
        private async Task ProcessCallbackRequest_checkconnection()
        {
            lock (rec_wait)
            {
                if (rec_wait.Count < 0)
                {
                    ProcessCallbackRequest_checkconnection_current = null;
                    return;
                }
            }

            foreach (var c in rec_wait.Values.Distinct())
            {
                try
                {
                    node.CheckConnection(c.Item2.LocalEndpoint);
                }
                catch (Exception)
                {
                    var rec_wait2 = new List<TaskCompletionSource<MessageEntry>>();
                    lock (rec_wait)
                    {
                        foreach (var r in rec_wait.Values)
                        {
                            if (r.Item2.LocalEndpoint == c.Item2.LocalEndpoint)
                            {
                                rec_wait2.Add(r.Item1);
                            }
                        }
                    }

                    foreach (var t in rec_wait2)
                    {
                        t.TrySetException(new ConnectionException("Connection closed"));
                    }
                    return;
                }
            }
            try
            {
                await Task.Delay(500).ConfigureAwait(false);
            }
            catch { }


        }

        public class MonitorObjectSkel
        {
            Task wait_thread;
            TaskCompletionSource<int> wait_event;
            IRobotRaconteurMonitorObject obj;
            uint local_endpoint = 0;
            int timeout = 0;
            Exception monitor_acquire_exception = null;
            bool monitor_acquired = false;
            TaskCompletionSource<int> monitor_thread_event;
            bool maintain_lock = false;
            ServiceSkel skel;

            public uint LocalEndpoint { get { return local_endpoint; } }

            public bool IsLocked { get { return monitor_acquired; } }

            public MonitorObjectSkel(ServiceSkel skel)
            {
                object obj = skel.UncastObject;
                if (!(obj is IRobotRaconteurMonitorObject))
                {
                    throw new InvalidOperationException("Object is not monitor lockable");
                }

                this.obj = (IRobotRaconteurMonitorObject)obj;
                this.skel = skel;
            }

            public async Task<string> MonitorEnter(uint local_endpoint, int timeout)
            {
                this.timeout = timeout;
                this.local_endpoint = local_endpoint;
                wait_event = new TaskCompletionSource<int>();
                monitor_thread_event = new TaskCompletionSource<int>();
                maintain_lock = true;

                lock (this)
                {
                    last_refreshed = DateTime.UtcNow;
                }

                lock (skel.monitorlocks)
                {
                    skel.monitorlocks.Add(local_endpoint, this);
                }

                wait_thread = thread_func().IgnoreResult();

                try
                {
                    await wait_event.Task.AwaitWithTimeout(5000).ConfigureAwait(false);
                }
                catch { }

                if (monitor_acquire_exception != null)
                {
                    maintain_lock = false;
                    throw monitor_acquire_exception;
                }

                return (monitor_acquired ? "OK" : "Continue");

            }

            DateTime last_refreshed = DateTime.UtcNow;

            public async Task<string> MonitorContinueEnter(uint localendpoint)
            {
                lock (this)
                {
                    last_refreshed = DateTime.UtcNow;
                }

                if (monitor_acquired) return "OK";

                if (monitor_acquire_exception != null)
                {
                    maintain_lock = false;
                    throw monitor_acquire_exception;
                }

                //wait_event.WaitOne(5000);
                try
                {
                    await wait_event.Task.AwaitWithTimeout(5000).ConfigureAwait(false);
                }
                catch { }

                if (monitor_acquire_exception != null)
                {
                    maintain_lock = false;
                    throw monitor_acquire_exception;
                }

                return (monitor_acquired ? "OK" : "Continue");
            }

            public Task MonitorRefresh(uint localendpoint)
            {
                lock (this)
                {
                    last_refreshed = DateTime.UtcNow;
                }
                return Task.FromResult(0);
            }

            public Task<string> MonitorExit(uint local_endpoint)
            {

                maintain_lock = false;
                monitor_thread_event.TrySetResult(0);

                return Task.FromResult("OK");
            }

            private async Task thread_func()
            {

                IDisposable l = null;
                try
                {
                    l = await obj.RobotRaconteurMonitorEnter(timeout).ConfigureAwait(false);
                    monitor_acquired = true;
                    skel.monitorlock = this;
                }
                catch (Exception e)
                {
                    monitor_acquire_exception = e;
                    wait_event.TrySetResult(0);

                    lock (skel.monitorlocks)
                    {
                        skel.monitorlocks.Remove(local_endpoint);
                    }

                    return;
                }

                try
                {
                    wait_event.TrySetResult(0);

                    while (maintain_lock)
                    {
                        try
                        {
                            await monitor_thread_event.Task.AwaitWithTimeout(30000).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                        lock (this)
                            if (DateTime.UtcNow > last_refreshed.AddSeconds(30))
                            {
                                maintain_lock = false;
                            }
                    }

                }
                catch { }
                finally
                {
                    lock (skel.monitorlocks)
                    {
                        skel.monitorlocks.Remove(local_endpoint);
                    }

                    l.Dispose();

                    //obj.RobotRaconteurMonitorExit();
                    monitor_acquired = false;
                    wait_event.TrySetResult(0);

                }

            }

        }
#pragma warning disable 1591
        public async Task<string> GetRootObjectType(RobotRaconteurVersion client_version)
        {
            return await GetObjectType(m_ServiceName, client_version);
        }

        public async Task<string> GetObjectType(string servicepath, RobotRaconteurVersion client_version)
        {
            try
            {
                // TODO: check client_version
                if (servicepath != ServiceName)
                {
                    if (RequireValidUser)
                    {
                        if (ServerEndpoint.CurrentAuthenticatedUser == null)
                            throw new PermissionDeniedException("User must authenticate before accessing this service");
                    }
                }

                var s = await GetObjectSkel(servicepath);

                return s.GetObjectType(client_version);
            }
            catch (Exception exp)
            {
#if RR_LOG_DEBUG
                RRLogFuncs.LogDebug("GetObjectType failed: " + exp.Message, node, RobotRaconteur_LogComponent.Service, service_path: ServiceName);
#endif
                throw;
            }
        }
#pragma warning restore 1591

        private List<string> extra_imports = new List<string>();

        /// <summary>
        /// Get the current extra service definition imports
        /// </summary>
        [PublicApi]
        public string[] ExtraImports
        {
            get
            {
                return extra_imports.ToArray();
            }
        }

        /** <summary>
            Add an extra service definition import
            </summary>
            <remarks>
            Clients using dynamic typing will not automatically pull service definitions unless
            imported by the root object or an objref. If new `struct`, `pod`, or `namedarray` types
            are introduced in a new service definition type without a corresponding object, an error will
            occur. Use AddExtraImport() to add the name of the new service definition to add it to the
            list of service definitions the client will pull.
            Service definition must have been registered using RobotRaconteurNode::RegisterServiceType()
            </remarks>
            <param name="import_">The service type to add</param>
        */
        [PublicApi]
        public void AddExtraImport(string import_)
        {
            node.GetServiceType(import_);

            lock (extra_imports)
            {

                if (extra_imports.Contains(import_))
                {

                    RRLogFuncs.LogDebug("Extra import \"" + import_ + "\" already added", node, RobotRaconteur_LogComponent.Service, endpoint: -1, service_path: ServiceName);
                    throw new ArgumentException("Extra import already added");
                }

                extra_imports.Add(import_);
            }
        }

        /// <summary>
        /// Removes an extra import service definition registered with AddExtraImport()
        /// </summary>
        /// <param name="import_">The service type to remove</param>
        [PublicApi]
        public bool RemoveExtraImport(string import_)
        {
            lock (extra_imports)
            {
                extra_imports.Remove(import_);

                return true;
            }
        }


    }

    /// <summary>
    /// Enum of service listener events
    /// </summary>
    [PublicApi]
    public enum ServerServiceListenerEventType
    {
        /// <summary>
        /// service has been closed
        /// </summary>
        [PublicApi]
        ServiceClosed = 1,

        /// <summary>
        /// client has connected
        /// </summary>
        [PublicApi]
        ClientConnected,

        /// <summary>
        /// client has disconnected
        /// </summary>
        [PublicApi]
        ClientDisconnected
    }


    /**
    <summary>
    Server endpoint representing a client connection
    </summary>
    <remarks>
    <para>
    Robot Raconteur creates endpoint pairs between a client and service. For clients, this
    endpoint
    is a ClientContext. For services, the endpoint becomes a ServerEndpoint. ServerEndpoints
    are used
    to address a specific client connected to a service, since services may have multiple
    clients
    connected concurrently. ServerEndpoints also provide client authentication information.
    </para>
    <para>Use ServerEndpoint.GetCurrentEndpoint() to retrieve the int32
    current endpoint ID. Use ServerEndpoint.GetCurrentAuthenticatedUser() to retrieve
    the current user authentication information.
    </para>
    </remarks>
    */

    [PublicApi]
    public class ServerEndpoint : Endpoint
    {
        protected internal readonly ServerContext service;

        [ThreadStatic]
        private static ServerEndpoint m_CurrentEndpoint;
        /**
        <summary>
        Returns the current server endpoint
        </summary>
        <remarks>
        <para>
        Returns the current server endpoint during a request or packet event.
        This is a thread-specific value and only valid during the initial
        request or packet event invocation.
        </para>
        <para>Throws InvalidOperationException if not during a request or packet event
        </para>
        </remarks>
        <returns>The current server endpoint id</returns>
        */

        [PublicApi]
        public static ServerEndpoint CurrentEndpoint { get { return m_CurrentEndpoint; } }

        [ThreadStatic]
        private static AuthenticatedUser m_CurrentAuthenticatedUser;
        /**
        <summary>
        Returns the current authenticated user
        </summary>
        <remarks>
        <para>
        Users that have been authenticated have a corresponding
        AuthenticatedUser object associated with the ServerEndpoint.
        CurrentAuthenticatedUser returns the AuthenticatedUser
        associated with the current ServerEndpoint during a request
        or packet event. This is a thread-specific value and only valid during
        the initial request or packet event invocation.
        </para>
        <para>Throws PermissionDeniedException or AuthenticationException
        if there is no AuthenticatedUser set in the current thread.
        </para>
        </remarks>
        */

        [PublicApi]
        public static AuthenticatedUser CurrentAuthenticatedUser { get { return m_CurrentAuthenticatedUser; } }

        private AuthenticatedUser endpoint_authenticated_user = null;


        public ServerEndpoint(ServerContext service, RobotRaconteurNode node)
            : base(node)
        {
            this.service = service;
        }


        public string AuthenticatedUsername
        {
            get
            {
                if (endpoint_authenticated_user == null) return null;

                return endpoint_authenticated_user.Username;
            }

        }

        public override void MessageReceived(Message m)
        {
            if (m.entries.Count >= 0)
            {
                if (m.entries[0].EntryType == MessageEntryType.EndpointCheckCapability)
                {
                    CheckEndpointCapabilityMessage(m);
                    return;
                }
            }

            LastMessageReceivedTime = DateTime.UtcNow;
            m_CurrentEndpoint = this;
            m_CurrentAuthenticatedUser = endpoint_authenticated_user;
            if (endpoint_authenticated_user != null) endpoint_authenticated_user.UpdateLastAccess();
            service.MessageReceived(m, this);
            m_CurrentEndpoint = null;
            m_CurrentAuthenticatedUser = null;
        }

        public void AuthenticateUser(string username, Dictionary<string, object> credentials)
        {
            AuthenticatedUser u = service.AuthenticateUser(username, credentials);
            endpoint_authenticated_user = u;
            m_CurrentAuthenticatedUser = u;
        }

        public void LogoutUser()
        {
            endpoint_authenticated_user = null;
            m_CurrentAuthenticatedUser = null;
        }


        public void PeriodicCleanupTask()
        {
            if ((DateTime.UtcNow - LastMessageReceivedTime).TotalMilliseconds > node.EndpointInactivityTimeout)
            {
                service.RemoveClient(this);
            }
        }


    }

    public delegate void RobotRaconteurObjRefChangedEvent(string objref);

    public interface IRobotRaconteurServiceObject
    {
        event RobotRaconteurObjRefChangedEvent RobotRaconteurObjRefChanged;
    }

    public class RobotRaconteurServiceObjectInterface : Attribute
    {
        private string rrtype;
        public RobotRaconteurServiceObjectInterface(string rrtype)
        {
            this.rrtype = rrtype;
        }

        public string RRType
        {
            get
            {
                return rrtype;
            }
        }

    }

    public class RobotRaconteurServiceStruct : Attribute
    {
        private string rrtype;
        public RobotRaconteurServiceStruct(string rrtype)
        {
            this.rrtype = rrtype;
        }

        public string RRType
        {
            get
            {
                return rrtype;
            }
        }

    }
    /**
    <summary>
    Service object monitor lock notification
    </summary>
    <remarks>
    Service objects must implement IRobotRaconteurMonitorObject for
    monitor locking to function. Services call RobotRaconteurMonitorEnter()
    with an optional timeout to request the lock, and call RobotRaconteurMonitorExit()
    to release the monitor lock. RobotRaconteurMonitorEnter() should block
    until a thread-exclusive lock can be established.
    </remarks>
    */

    [PublicApi]
    public interface IRobotRaconteurMonitorObject
    {
        /**
        <summary>
        Request a thread-exclusive lock without timeout. May block until lock can be established
        </summary>
        <remarks>Dispose of the returned object to release</remarks>
        */

        [PublicApi]
        Task<IDisposable> RobotRaconteurMonitorEnter();
        /**
<summary>
Request a thread-exclusive lock with timeout. May block until lock can be established,
up to the specified timeout.
</summary>
<remarks>Dispose of the returned object to release</remarks>
<param name="timeout">Lock request timeout in milliseconds</param>
*/

        [PublicApi]
        Task<IDisposable> RobotRaconteurMonitorEnter(int timeout);
    }

    /// <summary>
    /// Interface for service objects to receive service notifications.
    /// Service objects are passed to the service, either when the service is registered
    /// or using objrefs. The service initializes the object by configuring events,
    /// pipes, callbacks, and wires for use. The object may implement IRRServiceObject
    /// to receive notification of when this process is complete, and to receive
    /// a ServerContextPtr and the service path of the object.
    /// IRRServiceObject.RRServiceObjectInit() is called after the object has been
    /// initialized to provide this information.
    /// </summary>
    [PublicApi]
    public interface IRRServiceObject
    {
        /// <summary>
        /// Function called after service object has been initialized.
        /// Override in the service object to receive notification the service object has
        /// been initialized, a ServerContextPtr, and the service path.
        /// </summary>
        /// <param name="context">The ServerContextPtr owning the object.</param>
        /// <param name="servicePath">The object service path.</param>
        [PublicApi]
        void RRServiceObjectInit(ServerContext context, string servicePath);
    }

    /// <summary>
    /// Service path segment containing a name and an optional index
    /// </summary>
    public class ServicePathSegment
    {
        /// <summary>
        /// The name of the service path segment
        /// </summary>
        public string name;
        /// <summary>
        /// The index of the service path segment or null if the segment has no index
        /// </summary>
        public string index;
    }

    /// <summary>
    /// Service path utility functions
    /// </summary>
    public static class ServicePathUtil
    {
        /// <summary>
        /// Encode a service path index for use in a Robot Raconteur service path
        /// </summary>
        /// <param name="index">The index to encode</param>
        /// <returns>The encoded index</returns>
        public static string EncodeServicePathIndex(string index)
        {
            return RRUriExtensions.EscapeDataString(index).Replace(".", "%2e");
        }

        /// <summary>
        /// Decode a service path index from a Robot Raconteur service path
        /// </summary>
        /// <param name="index">The index to decode</param>
        /// <returns>The decoded index</returns>
        public static string DecodeServicePathIndex(string index)
        {
            return RRUriExtensions.UnescapeDataString(index);
        }

        /// <summary>
        /// Parse a Robot Raconteur service path into segments
        /// </summary>
        /// <param name="path">The service path to parse</param>
        /// <returns>The parsed service path segments</returns>
        public static ServicePathSegment[] ParseServicePathIndex(string path)
        {
            string segment_regex = "^([a-zA-Z0-9_]+)(?:\\[([a-zA-Z0-9_%]+)\\])?$";
            List<ServicePathSegment> segments = new List<ServicePathSegment>();
            string[] segments1 = path.Split('.');
            foreach (string segment in segments1)
            {
                Match match = Regex.Match(segment, segment_regex);
                if (match.Success)
                {
                    if (match.Groups[2].Success)
                    {
                        segments.Add(new ServicePathSegment() { name = match.Groups[1].Value, index = DecodeServicePathIndex(match.Groups[2].Value) });
                    }
                    else
                    {
                        segments.Add(new ServicePathSegment() { name = match.Groups[1].Value, index = null });
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid service path segment");
                }
            }
            return segments.ToArray();
        }

        /// <summary>
        /// Build a Robot Raconteur service path from segments
        /// </summary>
        /// <param name="segments">The segments to build the service path from</param>
        /// <returns>The built service path</returns>
        public static string BuildServicePath(ServicePathSegment[] segments)
        {
            string segment_name_regex = "^[a-zA-Z](?:\\w*[a-zA-Z0-9])?$";
            List<string> segments1 = new List<string>();
            bool first = true;
            foreach (ServicePathSegment segment in segments)
            {
                if (!Regex.IsMatch(segment.name, segment_name_regex))
                {
                    if (!(first && segment.name == "*"))
                    {
                        throw new ArgumentException("Invalid service path segment name");
                    }
                }

                first = false;
                if (segment.index != null)
                {
                    segments1.Add(segment.name + "[" + EncodeServicePathIndex(segment.index) + "]");
                }
                else
                {
                    segments1.Add(segment.name);
                }
            }
            return string.Join(".", segments1);
        }
    }
}
