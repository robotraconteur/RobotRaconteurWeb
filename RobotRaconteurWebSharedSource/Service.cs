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


    public abstract class ServiceSkel
    {

        public ServiceSkel(string s,Object o,ServerContext c)
        {
            m_ServicePath = s;
            m_context = c;
            uncastobj = o ;
            if (o == null) throw new NullReferenceException();

            rr_node = c.node;
            
            RegisterEvents(o);
            InitPipeServers(o);
            InitCallbackServers(o);            
        }

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
                string ind= RRUriExtensions.UnescapeDataString( s1[1].Replace("]",""));
                return GetSubObj(s1[0], ind);
            }
        }

        public virtual void RegisterEvents(Object obj1) {

            if (obj1 is IRobotRaconteurServiceObject)
            {
                IRobotRaconteurServiceObject obj2 = (IRobotRaconteurServiceObject)obj1;
                obj2.RobotRaconteurObjRefChanged += ObjRefChanged;
            }
        
        }

        public virtual void UnregisterEvents(Object obj1) {
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
            await RRContext.SendMessage(m, e, cancel);
        }

        public async Task SendWireMessage(MessageEntry m, Endpoint e, CancellationToken cancel)
        {
            m.ServicePath = ServicePath;
            await RRContext.SendMessage(m, e, cancel);
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
            lock(generators)
            {
                if(!generators.TryGetValue(index, out gen))
                {
                    throw new InvalidOperationException("Invalid generator");
                }
                gen.last_access_time = DateTime.UtcNow;                
            }

            if (gen.Endpoint != ep.LocalEndpoint)
            {
                throw new InvalidOperationException("Invalid generator");
            }
            return await gen.CallNext(m);
        }
    }

    public interface ServiceSkelDynamic
    {
        string GetObjectType();
        
    }
        
    public class ServerContext 
   {
        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public ServiceFactory ServiceDef { get { return m_ServiceDef; } }
        
        protected ServiceFactory m_ServiceDef;

        public string ServiceName { get { return m_ServiceName; } }
        
        protected string m_ServiceName;


        protected Dictionary<string, ServiceSkel> skels = new Dictionary<string, ServiceSkel>();

        protected Dictionary<uint, ServerEndpoint> client_endpoints = new Dictionary<uint, ServerEndpoint>();

        public string RootObjectType { get { return m_RootObjectType; } }

        protected string m_RootObjectType= "";
       
      

       

        /*public Message SendRequest(Message m)
        {
            return null;
        }*/


        protected internal readonly RobotRaconteurNode node;

        protected CancellationTokenSource cancel_source = new CancellationTokenSource();        

        public ServerContext(ServiceFactory f, RobotRaconteurNode node=null)
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

            var noop=PeriodicTask.Run(PeriodicCleanupTask, TimeSpan.FromSeconds(5), cancel_source.Token);
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
                        SendMessage(m, c, default(CancellationToken)).ContinueWith(delegate(Task t)
                        {
                            var e = t.Exception;
                            if (e!=null)
                            {
                                try
                                {
                                    RemoveClient(c);
                                }
                                catch (Exception)
                                { };
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
                        { };
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

            await e.SendMessage(mm, cancel);


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


        public virtual void SetBaseObject(string name, object o, ServiceSecurityPolicy policy=null)
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

                        skel1 = await t;
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
            object obj1 = await skel.GetSubObj(objname);
            m_CurrentServicePath = null;
            m_CurrentServerContext = null;

            var ppath = String.Join(".", new string[] { ppath1, objname});

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

        public virtual async Task<string> GetObjectType(string servicepath)
        {
            
            ServiceSkel s = await GetObjectSkel(servicepath);

            if (s is ServiceSkelDynamic)
            {
                return ((ServiceSkelDynamic)s).GetObjectType();
            }
            return Regex.Replace(s.GetType().ToString(), "_skel", "");
            

            
        }

        public static ServerContext CurrentServerContext {get {return m_CurrentServerContext;}}
        [ThreadStatic()]
        private static ServerContext m_CurrentServerContext;

        public static string CurrentServicePath {get {return m_CurrentServicePath;}}
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
                        return await ClientSessionOp(m, c);

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
                        (await GetObjectSkel(m.ServicePath)).DispatchPipeMessage(m, c);
                        ret = null;
                        noreturn = true;
                    }

                    if (m.EntryType == MessageEntryType.WirePacket)
                    {
                        (await GetObjectSkel(m.ServicePath)).DispatchWireMessage(m, c);
                        ret = null;
                        noreturn = true;
                    }

                    

                    

                    m_CurrentServicePath=m.ServicePath;
                    m_CurrentServerContext=this;

                    //Object member methods

                    if (m.EntryType == MessageEntryType.PropertyGetReq)
                    {
                        ServiceSkel skel=await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret = await skel.CallGetProperty(m);
                    }

                    if (m.EntryType == MessageEntryType.PropertySetReq)
                    {
                        ServiceSkel skel = await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret = await skel.CallSetProperty(m);
                    }

                    if (m.EntryType == MessageEntryType.FunctionCallReq)
                    {
                        ServiceSkel skel = await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret = await skel.CallFunction(m);
                    }

                    if (m.EntryType == MessageEntryType.PipeConnectReq || m.EntryType==MessageEntryType.PipeDisconnectReq)
                    {
                        ServiceSkel skel = await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret = await skel.CallPipeFunction(m,c);
                    }

                    if (m.EntryType == MessageEntryType.WireConnectReq || m.EntryType == MessageEntryType.WireDisconnectReq || m.EntryType == MessageEntryType.WirePeekInValueReq || m.EntryType == MessageEntryType.WirePeekOutValueReq || m.EntryType == MessageEntryType.WirePokeOutValueReq)
                    {
                        ServiceSkel skel = await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret = await skel.CallWireFunction(m, c);
                    }

                    

                    if (m.EntryType == MessageEntryType.MemoryWrite || m.EntryType == MessageEntryType.MemoryRead || m.EntryType == MessageEntryType.MemoryGetParam)
                    {
                        ServiceSkel skel = await GetObjectSkel(m.ServicePath);
                        check_lock(skel, m);
                        ret=await skel.CallMemoryFunction(m, c);
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
                    var skel = await GetObjectSkel(m.ServicePath);
                    check_lock(skel, m);
                    ret = await skel.CallGeneratorNext(m, c);
                    noreturn = true;
                }

            }
                catch (Exception e)
                {
                    ret = new MessageEntry(m.EntryType+1, m.MemberName);
                    RobotRaconteurExceptionUtil.ExceptionToMessageEntry(e, ret);

                }

                m_CurrentServicePath=null;
                m_CurrentServerContext=null;

                if (ret == null && !noreturn && (int)m.EntryType %2 ==1)
                {
                    ret = new MessageEntry(m.EntryType+1, m.MemberName);
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
            catch { };

            ServerEndpoint[] eea = client_endpoints.Values.ToArray();

            foreach (ServerEndpoint ee in eea)
            {
                try
                {
                    node.DeleteEndpoint(ee);
                }
                catch { };
            }

            client_endpoints.Clear();

            foreach (ServiceSkel s in skels.Values)
            {
                try
                {
                    s.ReleaseObject();
                }
                catch { };
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

                MessageEntry mmret=await ProcessMessageEntry(mm,e);
                if (mmret!=null)
                mret.entries.Add(mmret);
            }
            if (mret.entries.Count > 0)
            await e.SendMessage(mret,default(CancellationToken));
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

            string cusername=cendpoint.AuthenticatedUsername;
            uint ce=cendpoint.LocalEndpoint;

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

        private bool RequireValidUser=false;
        private bool AllowObjectLock=false;

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
                        Dictionary<string, object> credentials = (Dictionary<string,object>)UnpackMapType<string, object>(m.FindElement("credentials").Data);
                        e.AuthenticateUser(username, credentials);
                        ret.AddElement("return", "OK");
                        return ret;
                    }
                case "LogoutUser":
                    {
                        e.LogoutUser();
                        ret.AddElement("return","OK");
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

                        await ClientLockOp(m, ret);
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

            ServiceSkel skel = await GetObjectSkel(servicepath);

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
                        int timeout=0;
                        MonitorObjectSkel s;
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.monitorlocks.ContainsKey(ServerEndpoint.CurrentEndpoint.LocalEndpoint)) throw new InvalidOperationException("Already acquiring or acquired monitor lock");
                            s = new MonitorObjectSkel(skel);
                            timeout = m.FindElement("timeout").CastData<int[]>()[0];
                        }
                        string retcode = await s.MonitorEnter(ServerEndpoint.CurrentEndpoint.LocalEndpoint, timeout);
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
                        string retcode = await s.MonitorContinueEnter(ServerEndpoint.CurrentEndpoint.LocalEndpoint);
                        ret.AddElement("return", retcode);
                        break;
                    }

                case "MonitorExit":
                    {
                        lock (ClientLockOp_lockobj)
                        {
                            if (skel.monitorlock.LocalEndpoint != (ServerEndpoint.CurrentEndpoint.LocalEndpoint)) throw new InvalidOperationException("Not monitor locked");
                        }
                        string retcode = await skel.monitorlock.MonitorExit(ServerEndpoint.CurrentEndpoint.LocalEndpoint);
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

            public ObjectLock(string username, ServiceSkel root_skel, uint endpoint=0)
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
                            s.objectlock=null;
                        }
                        catch {}

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

        public delegate void ServerServiceListenerDelegate(ServerContext service, ServerServiceListenerEventType ev, object parameter);

        public event ServerServiceListenerDelegate ServerServiceListener;

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
                    SendMessage(m, c, default(CancellationToken)).ContinueWith(delegate(Task t)
                    {
                        var e = t.Exception;
                        if (e != null)
                        {
                            try
                            {
                                RemoveClient(c);
                            }
                            catch (Exception)
                            { };
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
                    { };
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
                rec_wait.Add(t_id, Tuple.Create(rec_source,e));
                if (ProcessCallbackRequest_checkconnection_current == null)
                {
                    ProcessCallbackRequest_checkconnection_current = ProcessCallbackRequest_checkconnection();
                }
            }

            MessageEntry rec_message = null;
            try
            {
                cancel.Register(delegate()
                {
                    rec_source.TrySetCanceled();
                });


                Func<Task> r = async delegate()
                {
                    await SendMessage(m, e, cancel);
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
                await Task.Delay(500);
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
            Exception monitor_acquire_exception=null;
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
                    await wait_event.Task.AwaitWithTimeout(5000);
                }
                catch { }

                if (monitor_acquire_exception != null)
                {
                    maintain_lock = false;
                    throw monitor_acquire_exception;
                }

                return (monitor_acquired ?  "OK" : "Continue" );
                
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
                    await wait_event.Task.AwaitWithTimeout(5000);
                }
                catch { }

                if (monitor_acquire_exception != null)
                {
                    maintain_lock = false;
                    throw monitor_acquire_exception;
                }

                return (monitor_acquired ? "OK" : "Continue" );
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
                
                IDisposable l=null;
                try
                {
                    l=await obj.RobotRaconteurMonitorEnter(timeout);
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
                            await monitor_thread_event.Task.AwaitWithTimeout(30000);
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
    }

    public enum ServerServiceListenerEventType
    {
        ServiceClosed = 1,
        ClientConnected,
        ClientDisconnected

    }


    public class ServerEndpoint : Endpoint
    {
        protected internal readonly ServerContext service;

        [ThreadStatic]
        private static ServerEndpoint m_CurrentEndpoint;

        public static ServerEndpoint CurrentEndpoint { get { return m_CurrentEndpoint; } }

        [ThreadStatic]
        private static AuthenticatedUser m_CurrentAuthenticatedUser;

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
            if (endpoint_authenticated_user!=null) endpoint_authenticated_user.UpdateLastAccess();
            service.MessageReceived(m, this);
            m_CurrentEndpoint = null;
            m_CurrentAuthenticatedUser = null;
        }

        public void AuthenticateUser(string username, Dictionary<string, object> credentials)
        {
            AuthenticatedUser u=service.AuthenticateUser(username, credentials);
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

    public interface IRobotRaconteurMonitorObject
    {
        Task<IDisposable> RobotRaconteurMonitorEnter();

        Task<IDisposable> RobotRaconteurMonitorEnter(int timeout);

    }
}
