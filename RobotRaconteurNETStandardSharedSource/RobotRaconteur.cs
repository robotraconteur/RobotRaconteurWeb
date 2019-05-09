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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using RobotRaconteur.Extensions;

namespace RobotRaconteur
{
    public class RobotRaconteurNode
    {

        private static RobotRaconteurNode sp;

        public static RobotRaconteurNode s
        {
            get
            {
                if (sp == null) sp = new RobotRaconteurNode();
                return sp;
            }
        }

        public const string Version = "0.9.0";

        private NodeID m_NodeID;

        public NodeID NodeID
        {
            get
            {
                if (m_NodeID == null) m_NodeID = NodeID.NewUniqueID();
                return m_NodeID;
            }
            set
            {
                if (m_NodeID == null)
                {
                    m_NodeID = value;

                }
                else
                {
                    throw new InvalidOperationException("NodeID cannot be changed once it is set");
                }
            }
        }


        private string m_NodeName;
        public string NodeName
        {
            get
            {
                if (m_NodeName == null) m_NodeName = "";
                return m_NodeName;
            }
            set
            {
                if (m_NodeName == null)
                {

                    if (!Regex.Match(value, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$").Success)
                    {
                        throw new InvalidOperationException("Invalid node name");
                    }

                    m_NodeName = value;

                }
                else
                {
                    throw new InvalidOperationException("NodeName cannot be changed once it is set");
                }
            }

        }





        internal Dictionary<uint, Endpoint> endpoints = new Dictionary<uint, Endpoint>();

        internal Dictionary<uint, Transport> transports = new Dictionary<uint, Transport>();

        //public Dictionary<uint, uint> endpoint_map=new Dictionary<uint,uint>;

        internal Dictionary<string, ServiceFactory> service_factories = new Dictionary<string, ServiceFactory>();

        internal DynamicServiceFactory dynamic_factory;

        public DynamicServiceFactory DynamicServiceFactory { get { return dynamic_factory; } }

        internal Dictionary<string, ServerContext> services = new Dictionary<string, ServerContext>();


        private uint transport_count = 0;
        

        public uint EndpointInactivityTimeout = 600000;
        public uint TransportInactivityTimeout = 600000;
        public uint RequestTimeout = 15000;
        private ServiceIndexer serviceindexer;


        public uint MemoryMaxTransferSize = 102400;

#if ROBOTRACONTEUR_BRIDGE
        public readonly BrowserWebSocketTransport browser_transport;
#endif

        RobotRaconteurNode()
        {
            serviceindexer = new ServiceIndexer(this);
            RegisterServiceType(new RobotRaconteurServiceIndex.RobotRaconteurServiceIndexFactory(this));
            RegisterService("RobotRaconteurServiceIndex", "RobotRaconteurServiceIndex", serviceindexer);
            cleandiscoverednodes_task =  PeriodicTask.Run(CleanDiscoveredNodes, TimeSpan.FromSeconds(5), shutdown_token.Token);
#if ROBOTRACONTEUR_BRIDGE
            browser_transport = new BrowserWebSocketTransport(this);
            RegisterTransport(browser_transport);
#endif
        }

        private ServiceFactory GetServiceFactoryForType(string type, ClientContext context)
        {
            string servicename = ServiceDefinitionUtil.SplitQualifiedName(type).Item1;

            if (context != null)
            {
                ServiceFactory f;
                if(context.TryGetPulledServiceType(servicename, out f))
                {
                    return f;
                }
            }            
            return GetServiceType(servicename);            
        }

        private ServiceFactory GetServiceFactoryForType(Type type, ClientContext context)
        {
            return GetServiceFactoryForType(ServiceDefinitionUtil.FindStructRRType(type), context);      
        }

        public MessageElementStructure PackStructure(Object s, ClientContext context)
        {
            if (s == null) return null;
            
            return GetServiceFactoryForType(s.GetType(), context).PackStructure(s);
        }
        
        public T UnpackStructure<T>(MessageElementStructure l, ClientContext context)
        {
            if (l == null) return default(T);
            return GetServiceFactoryForType(l.Type, context).UnpackStructure<T>(l);
        }

        public MessageElementPodArray PackPodToArray<T>(ref T s, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(s.GetType(), context).PackPodToArray(ref s);
        }

        public T UnpackPodFromArray<T>(MessageElementPodArray l, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(l.Type, context).UnpackPodFromArray<T>(l);
        }

        public MessageElementPodArray PackPodArray<T>(T[] s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.GetType(), context).PackPodArray(s);
        }

        public T[] UnpackPodArray<T>(MessageElementPodArray l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.Type, context).UnpackPodArray<T>(l);
        }

        public MessageElementPodMultiDimArray PackPodMultiDimArray<T>(PodMultiDimArray s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.pod_array.GetType(), context).PackPodMultiDimArray<T>(s);
        }

        public PodMultiDimArray UnpackPodMultiDimArray<T>(MessageElementPodMultiDimArray l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.Type, context).UnpackPodMultiDimArray<T>(l);
        }

        public object PackPod(object s, ClientContext context)
        {
            var t = s.GetType();

            if (t.IsValueType)
            {
                return GetServiceFactoryForType(t, context).PackPod(s);
            }
            else if (t.IsArray)
            {
                return GetServiceFactoryForType(t.GetElementType(), context).PackPod(s);
            }
            else if (t == typeof(PodMultiDimArray))
            {
                return GetServiceFactoryForType(((PodMultiDimArray)s).pod_array.GetType().GetElementType(), context).PackPod(s);
            }
            else if (t == typeof(NamedMultiDimArray))
            {
                return GetServiceFactoryForType(((NamedMultiDimArray)s).namedarray_array.GetType().GetElementType(), context).PackNamedArray(s);
            }
            throw new DataTypeException("Invalid pod object");
        }

        public object UnpackPod(object l, ClientContext context)
        {
            return GetServiceFactoryForType(MessageElementUtil.GetMessageElementDataTypeString(l), context).UnpackPod(l);
        }

        public MessageElementNamedArray PackNamedArrayToArray<T>(ref T s, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(s.GetType(), context).PackNamedArrayToArray(ref s);
        }

        public T UnpackNamedArrayFromArray<T>(MessageElementNamedArray l, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(l.Type, context).UnpackNamedArrayFromArray<T>(l);
        }

        public MessageElementNamedArray PackNamedArray<T>(T[] s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.GetType(), context).PackNamedArray(s);
        }

        public T[] UnpackNamedArray<T>(MessageElementNamedArray l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.Type, context).UnpackNamedArray<T>(l);
        }

        public MessageElementNamedMultiDimArray PackNamedMultiDimArray<T>(NamedMultiDimArray s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.namedarray_array.GetType(), context).PackNamedMultiDimArray<T>(s);
        }

        public NamedMultiDimArray UnpackNamedMultiDimArray<T>(MessageElementNamedMultiDimArray l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.Type, context).UnpackNamedMultiDimArray<T>(l);
        }

        public object PackNamedArray(object s, ClientContext context)
        {
            var t = s.GetType();

            if (t.IsValueType)
            {
                return GetServiceFactoryForType(t, context).PackNamedArray(s);
            }
            else if (t.IsArray)
            {
                return GetServiceFactoryForType(t.GetElementType(), context).PackNamedArray(s);
            }
            else if (t == typeof(NamedMultiDimArray))
            {
                return GetServiceFactoryForType(((NamedMultiDimArray)s).namedarray_array.GetType().GetElementType(), context).PackNamedArray(s);
            }
            throw new DataTypeException("Invalid pod object");
        }

        public object UnpackNamedArray(object l, ClientContext context)
        {
            return GetServiceFactoryForType(MessageElementUtil.GetMessageElementDataTypeString(l), context).UnpackNamedArray(l);
        }

        private MessageElement PackContainerValue<T>(string name, ref T data, ClientContext context)
        {
            Type t = typeof(T);

            if (t == typeof(object))
            {
                return MessageElementUtil.NewMessageElement(name, PackVarType((object)data, context));
            }

            bool is_array = t.IsArray;
            if (!(t.IsValueType || !EqualityComparer<T>.Default.Equals(data, default(T))))
            {
                return MessageElementUtil.NewMessageElement(name, null);
            }

            if (t.IsPrimitive || (is_array && t.GetElementType().IsPrimitive))
            {
                return MessageElementUtil.NewMessageElement(name, data);
            }

            if (t == typeof(string))
            {
                return MessageElementUtil.NewMessageElement(name, data);
            }

            if (t == typeof(CDouble) || t == typeof(CSingle))
            {
                return MessageElementUtil.NewMessageElement(name, data);
            }

            if (is_array)
            {
                var t2 = t.GetElementType();
                if (t2 == typeof(CDouble) || t2 == typeof(CSingle))
                {
                    return MessageElementUtil.NewMessageElement(name, data);
                }
            }

            if (t == typeof(MultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(name, PackMultiDimArray((MultiDimArray)(object)data));
            }

            if (t == typeof(PodMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(name, PackPod((object)data, context));
            }

            if (t == typeof(NamedMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(name, PackNamedArray((object)data, context));
            }

            if (t.IsGenericType)
            {
                throw new DataTypeException("Invalid Robot Raconteur container value type");
            }

            if (!t.IsValueType && !is_array && t != typeof(PodMultiDimArray) && t != typeof(NamedMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(name, PackStructure(data, context));
            }
            else
            {
                Type t2 = t;
                if (t.IsArray) t2 = t.GetElementType();
                if (t2.GetCustomAttributes(typeof(RobotRaconteurNamedArrayElementTypeAndCount), false).Length > 0)
                {
                    return MessageElementUtil.NewMessageElement(name, PackNamedArray(data, context));
                }
                else
                {
                    return MessageElementUtil.NewMessageElement(name, PackPod(data, context));
                }
            }
        }

        private MessageElement PackContainerValue<T>(int num, ref T data, ClientContext context)
        {
            return PackContainerValue(num.ToString(), ref data, context);
        }

        private T UnpackContainerValue<T>(MessageElement e, ClientContext context)
        {
            switch (e.ElementType)
            {
                case DataTypes.void_t:
                    if (typeof(T).IsValueType)
                        throw new DataTypeException("Primitive types may not be null");
                    return default(T);
                case DataTypes.double_t:
                case DataTypes.single_t:
                case DataTypes.int8_t:
                case DataTypes.uint8_t:
                case DataTypes.int16_t:
                case DataTypes.uint16_t:
                case DataTypes.int32_t:
                case DataTypes.uint32_t:
                case DataTypes.int64_t:
                case DataTypes.uint64_t:
                case DataTypes.cdouble_t:
                case DataTypes.csingle_t:
                case DataTypes.bool_t:
                    if (typeof(T).IsArray)
                    {
                        return (T)e.Data;
                    }
                    else
                    {
                        return (typeof(T) == typeof(object)) ? (T)e.Data : ((T[])e.Data)[0];
                    }
                case DataTypes.string_t:
                    return (T)e.Data;
                case DataTypes.multidimarray_t:
                    {
                        MessageElementMultiDimArray md = (MessageElementMultiDimArray)e.Data;
                        return (T)(object)UnpackMultiDimArray(md);
                    }
                case DataTypes.structure_t:
                    {
                        MessageElementStructure md = (MessageElementStructure)e.Data;
                        return UnpackStructure<T>(md, context);
                    }
                /*case DataTypes.pod_t:
                    using (MessageElementData md = (MessageElementData)e.Data)
                    {
                        return (T)UnpackPod(md);
                    }*/
                case DataTypes.pod_array_t:
                    {
                        MessageElementPodArray md = (MessageElementPodArray)e.Data;
                        if (typeof(T).IsValueType)
                        {
                            if (md.Elements.Count != 1) throw new DataTypeException("Invalid array size for scalar structure");
                            return ((T[])UnpackPod(md,context))[0];
                        }
                        else
                        {
                            return (T)UnpackPod(md,context);
                        }
                    }
                case DataTypes.pod_multidimarray_t:                    
                    {
                        return (T)UnpackPod(e.Data, context);
                    }
                case DataTypes.namedarray_array_t:
                    {
                        MessageElementNamedArray md = (MessageElementNamedArray)e.Data;
                        if (typeof(T).IsValueType)
                        {
                            if (md.Elements.Count != 1) throw new DataTypeException("Invalid array size for scalar structure");

                            return ((T[])UnpackNamedArray(md, context))[0];

                        }
                        else
                        {
                            return (T)UnpackNamedArray(md, context);
                        }
                    }
                case DataTypes.namedarray_multidimarray_t:
                    {                       
                        return (T)UnpackNamedArray(e.Data, context);
                    }
                default:
                    throw new DataTypeException("Invalid container data type");
            }
        }

        private T UnpackContainerValue<T>(MessageElement e, out string name, ClientContext context)
        {
            name = e.ElementName;
            return UnpackContainerValue<T>(e, context);
        }

        private T UnpackContainerValue<T>(MessageElement e, out int num, ClientContext context)
        {
            num = MessageElementUtil.GetMessageElementNumber(e);
            return UnpackContainerValue<T>(e, context);
        }

        public object PackMapType<Tkey, Tvalue>(object data, ClientContext context)
        {
            if (data == null) return null;

            if (typeof(Tkey) == typeof(Int32))
            {
                var m = new List<MessageElement>();

                Dictionary<Tkey, Tvalue> ddata = (Dictionary<Tkey, Tvalue>)data;

                foreach (KeyValuePair<Tkey, Tvalue> d in ddata)
                {
                    var v = d.Value;
                    MessageElementUtil.AddMessageElement(m, PackContainerValue(Convert.ToInt32(d.Key), ref v, context));
                }
                return new MessageElementMap<int>(m);
            }

            if (typeof(Tkey) == typeof(String))
            {
                var m = new List<MessageElement>();
                Dictionary<Tkey, Tvalue> ddata = (Dictionary<Tkey, Tvalue>)data;

                foreach (KeyValuePair<Tkey, Tvalue> d in ddata)
                {
                    var v = d.Value;
                    MessageElementUtil.AddMessageElement(m, PackContainerValue(d.Key.ToString(), ref v, context));
                }
                return new MessageElementMap<string>(m);
            }            

            throw new DataTypeException("Indexed types can only be indexed by int32 and string");
        }


        public object UnpackMapType<Tkey, Tvalue>(object data, ClientContext context)
        {
            if (data == null) return null;

            if (data is MessageElementMap<int>)
            {
                Dictionary<int, Tvalue> o = new Dictionary<int, Tvalue>();

                MessageElementMap<int> cdata = (MessageElementMap<int>)data;
                var cdataElements = cdata.Elements;
                {
                    foreach (MessageElement e in cdataElements)
                    {
                        int num;
                        var val = UnpackContainerValue<Tvalue>(e, out num, context);
                        o.Add(num, val);

                    }
                    return o;
                }
            }
            else if (data is MessageElementMap<string>)
            {
                Dictionary<string, Tvalue> o = new Dictionary<string, Tvalue>();

                MessageElementMap<string> cdata = (MessageElementMap<string>)data;
                var cdataElements = cdata.Elements;
                {
                    foreach (MessageElement e in cdataElements)
                    {
                        string name;
                        var val = UnpackContainerValue<Tvalue>(e, out name, context);
                        o.Add(name, val);
                    }
                    return o;
                }
            }
            else
            {
                throw new DataTypeException("Indexed types can only be indexed by int32 and string");
            }
        }

        public object PackListType<Tvalue>(object data, ClientContext context)
        {
            if (data == null) return null;

            var m = new List<MessageElement>();
            {
                List<Tvalue> ddata = (List<Tvalue>)data;

                int count = 0;
                foreach (Tvalue d in ddata)
                {
                    var v = d;
                    MessageElementUtil.AddMessageElement(m, PackContainerValue(count, ref v, context));
                    count++;
                }

                return new MessageElementList(m);
            }
        }

        public object UnpackListType<Tvalue>(object data, ClientContext context)
        {
            if (data == null) return null;
            List<Tvalue> o = new List<Tvalue>();
            int count = 0;
            MessageElementList cdata = (MessageElementList)data;
            var cdataElements = cdata.Elements;
            {
                foreach (MessageElement e in cdataElements)
                {
                    int num;
                    var val = UnpackContainerValue<Tvalue>(e, out num, context);
                    if (count != num) throw new DataTypeException("Error in list format");
                    o.Add(val);
                    count++;
                }
                return o;
            }
        }

        public object PackVarType(object data, ClientContext context)
        {
            if (data == null) return null;

            Type t = data.GetType();

            if (t == typeof(Dictionary<int, object>))
            {
                return PackMapType<int, object>(data, context);

            }
            else if (t == typeof(Dictionary<string, object>))
            {
                return PackMapType<string, object>(data, context);

            }
            else if (t == typeof(List<object>))
            {
                return PackListType<object>(data, context);

            }

            bool is_array = t.IsArray;

            if (t.IsPrimitive || (is_array && t.GetElementType().IsPrimitive))
            {
                return data;
            }

            if (t == typeof(string))
            {
                return data;
            }

            if (t == typeof(MultiDimArray))
            {
                return PackMultiDimArray((MultiDimArray)data);
            }

            if (t == typeof(PodMultiDimArray))
            {
                return PackPod(data, context);
            }

            if (t.IsGenericType)
            {
                throw new DataTypeException("Invalid Robot Raconteur varvalue type");
            }

            if (!t.IsValueType && !is_array && t != typeof(PodMultiDimArray) && t != typeof(NamedMultiDimArray))
            {
                return PackStructure(data, context);
            }
            else
            {
                Type t2 = t;
                if (t.IsArray) t2 = t.GetElementType();
                if (t2.GetCustomAttributes(typeof(RobotRaconteurNamedArrayElementTypeAndCount), false).Length > 0)
                {
                    return PackNamedArray(data, context);
                }
                else
                {
                    return PackPod(data, context);
                }
            }
        }
                
        public object UnpackVarType(MessageElement me, ClientContext context)
        {
            if (me == null) return null;

            switch (me.ElementType)
            {
                case DataTypes.void_t:
                    return null;
                case DataTypes.double_t:
                case DataTypes.single_t:
                case DataTypes.int8_t:
                case DataTypes.uint8_t:
                case DataTypes.int16_t:
                case DataTypes.uint16_t:
                case DataTypes.int32_t:
                case DataTypes.uint32_t:
                case DataTypes.int64_t:
                case DataTypes.uint64_t:
                case DataTypes.string_t:
                    return me.Data;
                case DataTypes.multidimarray_t:
                    {
                        MessageElementMultiDimArray md = (MessageElementMultiDimArray)me.Data;
                        return UnpackMultiDimArray(md);
                    }
                case DataTypes.structure_t:
                    {
                        MessageElementStructure md = (MessageElementStructure)me.Data;              
                        return UnpackStructure<object>(md, context);
                    }
                //case DataTypes.pod_t:
                case DataTypes.pod_array_t:
                case DataTypes.pod_multidimarray_t:
                    {                        
                        return UnpackPod(me.Data, context);
                    }
                case DataTypes.namedarray_array_t:
                case DataTypes.namedarray_multidimarray_t:
                    {
                        return UnpackNamedArray(me.Data, context);
                    }
                case DataTypes.vector_t:
                    { 
                        return UnpackMapType<int, object>(me.Data, context);
                    }
                case DataTypes.dictionary_t:
                    {
                        return UnpackMapType<string, object>(me.Data, context);
                    }
                case DataTypes.list_t:                    
                    {
                        return UnpackListType<object>(me.Data, context);
                    }
                default:
                    throw new DataTypeException("Invalid varvalue data type");
            }
        }
        
        public MessageElementMultiDimArray PackMultiDimArray(MultiDimArray array)
        {
            if (array == null) return null;
            List<MessageElement> l = new List<MessageElement>();
            l.Add(new MessageElement("dims",array.Dims));            
            l.Add(new MessageElement("array", array.Array_));            
            return new MessageElementMultiDimArray(l);
        }

        public MultiDimArray UnpackMultiDimArray(MessageElementMultiDimArray marray)
        {
            if (marray == null) return null;

            MultiDimArray m = new MultiDimArray();
            
            m.Dims = (MessageElement.FindElement(marray.Elements, "dims").CastData<uint[]>());
            m.Array_ = (MessageElement.FindElement(marray.Elements, "array").CastData<Array>());            
            return m;
        }

        public async Task SendMessage(Message m, CancellationToken cancel)
        {

            if (m.header.SenderNodeID != NodeID)
            {
                
                    throw new ConnectionException("Could not route message");
                
            }

            Endpoint e;
            try
            {
                lock (endpoints)
                {
                    e = endpoints[m.header.SenderEndpoint];
                }
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidEndpointException("Could not find endpoint");
            }


            Transport c;
            try
            {
                lock (transports)
                {
                    c = transports[e.transport];
                }
            }
            catch (KeyNotFoundException)
            {
                throw new ConnectionException("Could not find transport");
            }
            
            await c.SendMessage(m, cancel);

        }

        public void MessageReceived(Message m)
        {
            if (m.header.ReceiverNodeID != NodeID)
            {                
                Message eret = GenerateErrorReturnMessage(m, MessageErrorType.NodeNotFound, "RobotRaconteur.NodeNotFound", "Could not find route to remote node");
                if (eret.entries.Count > 0)
                    SendMessage(eret, default(CancellationToken)).IgnoreResult();               

            }

            else
            {
                

                    
                    try
                    {

                        Endpoint e;
                        lock (endpoints)
                        {
                            e = endpoints[m.header.ReceiverEndpoint];
                        }

                        e.MessageReceived(m);
                    }
                    catch (KeyNotFoundException)
                    {
                        Message eret = GenerateErrorReturnMessage(m, MessageErrorType.InvalidEndpoint, "RobotRaconteur.InvalidEndpoint", "Invalid destination endpoint");
                        if (eret.entries.Count > 0)
                            SendMessage(eret, default(CancellationToken)).IgnoreResult();
                    }
            }
           


        }

        public Message GenerateErrorReturnMessage(Message m, MessageErrorType err, string errname, string errdesc)
        {
            Message ret = new Message();
            ret.header = new MessageHeader();
            ret.header.ReceiverNodeName = m.header.SenderNodeName;
            ret.header.SenderNodeName = m.header.ReceiverNodeName;
            ret.header.ReceiverNodeID = m.header.SenderNodeID;
            ret.header.ReceiverEndpoint = m.header.SenderEndpoint;
            ret.header.SenderEndpoint = m.header.ReceiverEndpoint;
            ret.header.SenderNodeID = m.header.ReceiverNodeID;
            foreach (MessageEntry me in m.entries)
            {
                if (((int)me.EntryType) % 2 == 1)
                {
                    MessageEntry eret = new MessageEntry(me.EntryType+1, me.MemberName);
                    eret.RequestID = me.RequestID;
                    eret.ServicePath = me.ServicePath;
                    eret.AddElement("errorname", errname);
                    eret.AddElement("errorstring", errdesc);
                    eret.Error = err;
                    ret.entries.Add(eret);
                }
            }

            return ret;

        }

        public void RegisterServiceType(ServiceFactory f)
        {
            lock (service_factories)
            {
                service_factories.Add(f.GetServiceName(), f);
            }
        }

        public ServiceFactory GetServiceType(string servicetype)
        {
            ServiceFactory f;
            if (!TryGetServiceType(servicetype, out f))
            {
                throw new ServiceException("Service factory not found for " + servicetype);
            }
            return f;            
        }

        public bool TryGetServiceType(string servicetype, out ServiceFactory f)
        {
            lock (service_factories)
            {                
                return service_factories.TryGetValue(servicetype,out f);
            }
        }


        public string[] GetServiceTypes()
        {
            lock (service_factories)
            {
                return service_factories.Keys.ToArray();
            }
        }

        public void RegisterDynamicServiceFactory(DynamicServiceFactory f)
        {
            if (this.dynamic_factory != null) throw new InvalidOperationException("Dynamic service factory already set");
            this.dynamic_factory = f;
        }

        public ServerContext RegisterService(string name, string servicetype, Object obj,ServiceSecurityPolicy securitypolicy=null)
        {
            lock (services)
            {

                if (services.Keys.Contains(name))
                {
                    CloseService(name);
                }

                ServerContext c = new ServerContext(GetServiceType(servicetype),this);
                c.SetBaseObject(name, obj, securitypolicy);

                //RegisterEndpoint(c);
                services.Add(name, c);

                UpdateServiceStateNonce();

                return c;
            }
        }

        
        public ServerContext RegisterService(ServerContext c)
        {
            lock (services)
            {
                if (services.Keys.Contains(c.ServiceName))
                {
                    CloseService(c.ServiceName);
                }

                services.Add(c.ServiceName, c);
                return c;
            }
        }

        public void CloseService(string sname)
        {
            lock (services)
            {
                ServerContext s = GetService(sname);



                s.Close();
                //DeleteEndpoint(s);

                services.Remove(sname);
            }

            


        }

        public ServerContext GetService(string name)
        {
            try
            {
                lock (services)
                {
                    return services[name];
                }
            }
            catch (KeyNotFoundException)
            {
                throw new ServiceNotFoundException("Service " + name + " not found");
            }


        }

        public uint RegisterTransport(Transport c)
        {
            lock (transports)
            {
                transport_count++;
                c.TransportID = transport_count;
                transports.Add(transport_count, c);
                return transport_count;
            }
        }

       
        public async Task<Message> SpecialRequest(Message m, uint transportid)
        {
           

            if (!(m.header.ReceiverNodeID==NodeID.Any && (m.header.ReceiverNodeName=="" || m.header.ReceiverNodeName==NodeName))
                && !(m.header.ReceiverNodeID == NodeID))
            {                
                return GenerateErrorReturnMessage(m, MessageErrorType.NodeNotFound, "RobotRaconteur.NodeNotFound", "Could not find route to remote node");                
            }


            Message ret = new Message();
            ret.header = new MessageHeader();
            ret.header.ReceiverNodeName = m.header.SenderNodeName;
            ret.header.SenderNodeName = this.NodeName;
            ret.header.ReceiverNodeID = m.header.SenderNodeID;
            ret.header.ReceiverEndpoint = m.header.SenderEndpoint;
            ret.header.SenderEndpoint = m.header.ReceiverEndpoint;
            ret.header.SenderNodeID = this.NodeID;

            foreach (MessageEntry e in m.entries)
            {
                MessageEntry eret = ret.AddEntry(e.EntryType+1,e.MemberName);
                eret.RequestID = e.RequestID;
                eret.ServicePath = e.ServicePath;

                switch (e.EntryType)
                {
                    case MessageEntryType.GetNodeInfo:
                        break;
                    case MessageEntryType.ObjectTypeName:
                        {
                            string path = (string)e.ServicePath;
                            string[] s1 = path.Split(new char[] { '.' }, 2);
                            try
                            {
                                ServerContext s;
                                
                                    s = GetService(s1[0]);
                                
                                string objtype = await s.GetObjectType(path);
                                eret.AddElement("objecttype", objtype);
                            }
                            catch
                            {
                                eret.AddElement("errorname", "RobotRaconteur.ObjectNotFoundException");
                                eret.AddElement("errorstring", "Object not found");
                                eret.Error = MessageErrorType.ObjectNotFound;
                            }
                        }
                        break;
                    case MessageEntryType.GetServiceDesc:
                        {
                            //string name = (string)e.FindElement("servicename").Data;
                            string name = e.ServicePath;
                            try
                            {
                                string servicedef="";
                                if (e.elements.Any(x =>x.ElementName == "ServiceType"))
                                {
                                    name = e.FindElement("ServiceType").CastData<string>();
                                    servicedef=GetServiceType(name).DefString();
                                }
                                else if (e.elements.Any(x => x.ElementName == "servicetype"))
                                {
                                    name = e.FindElement("servicetype").CastData<string>();
                                    servicedef = GetServiceType(name).DefString();
                                }
                                else
                                {
                                 servicedef= GetService(name).ServiceDef.DefString();
                                 eret.AddElement("attributes", PackMapType<string,object>(GetService(name).Attributes, null));
                                }
                                eret.AddElement("servicedef", servicedef);
                            }
                            catch
                            {
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceNotFound;
                            }
                        }
                        break;
                    case MessageEntryType.ConnectClient:
                        {
                            string name = (string)e.ServicePath;

                            try
                            {
                                

                                ServerContext c = GetService(name);
                                ServerEndpoint se = new ServerEndpoint(c,this);
                                
                                se.m_RemoteEndpoint = m.header.SenderEndpoint;
                                se.m_RemoteNodeID = m.header.SenderNodeID;
                                RegisterEndpoint(se);

                                se.transport = transportid;

                                c.AddClient(se);

                                ret.header.SenderEndpoint = se.LocalEndpoint;
                                
                                
                                //services[name].AddClient(m.header.SenderEndpoint);
                                //eret.AddElement("servicedef", servicedef);
                            }
                            catch
                            {
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceNotFound;
                            }
                        }
                        break;
                    case MessageEntryType.DisconnectClient:
                        {
                            

                            try
                            {
                                string name = e.FindElement("servicename").CastData<string>();
                                ServerEndpoint se;
                                lock (endpoints)
                                {
                                    se = (ServerEndpoint)endpoints[m.header.ReceiverEndpoint];
                                }

                                lock (services)
                                {
                                    GetService(name).RemoveClient(se);
                                }
                                DeleteEndpoint(se);
                                //eret.AddElement("servicedef", servicedef);
                            }
                            catch
                            {
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceNotFound;
                            }
                        }
                        break;
                    case MessageEntryType.ConnectionTest:
                        break;
                     
                    case MessageEntryType.NodeCheckCapability:
                        eret.AddElement("return", (uint)0);
                        break;
                    case MessageEntryType.GetServiceAttributes:
                        {
                            string path = (string)e.ServicePath;
                            string[] s1 = path.Split(new char[] { '.' }, 2);
                            try
                            {
                                ServerContext s;

                                s = GetService(s1[0]);

                                Dictionary<string,object> attr = s.Attributes;
                                eret.AddElement("return", PackMapType<string,object>(attr, null));
                            }
                            catch
                            {
                                eret.AddElement("errorname", "RobotRaconteur.ServiceError");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceError;
                            }
                        }
                        break;


                    default:
                        eret.Error = MessageErrorType.ProtocolError;
                        eret.AddElement("errorname", "RobotRaconteur.ProtocolError");
                        eret.AddElement("errorstring", "Invalid Special Operation");
                        break;
                }
            }

            return ret;
            

        }

        public async Task<object> ConnectService(string url, string username = null, object credentials = null, ClientContext.ClientServiceListenerDelegate listener = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
        {

            //TODO: Specify target object type

            ClientContext c = new ClientContext(this);
            RegisterEndpoint(c);
            Transport[] atransports;
            lock (transports)
            {
                atransports = transports.Values.ToArray();
            }
            foreach (Transport end in atransports)
            {
                if (end == null) continue;
                if (end.IsClient)
                {
                    if (end.CanConnectService(url))
                    {

                        object r = await c.ConnectService(end, url,username,credentials,objecttype,cancel);
                        
                        if (listener!=null)
                        c.ClientServiceListener += listener;
                            return r;
                        
                    }
                }
            }

            try
            {
                DeleteEndpoint(c);
            }
            catch { };
           
            throw new ConnectionException("Could not connect to service");
        }

        public async Task DisconnectService(object obj, CancellationToken cancel=default(CancellationToken))
        {
            ServiceStub stub = (ServiceStub)obj;
            await stub.RRContext.Close(cancel);

        }

        public Dictionary<string, object> GetServiceAttributes(object obj)
        {
            ServiceStub stub = (ServiceStub)obj;
            return stub.RRContext.Attributes;
        }

        public uint RegisterEndpoint(Endpoint e)
        {
            lock (endpoints)
            {
                Random r = new Random();
                uint local_endpoint;
                do
                {
                    byte[] b = new byte[4];
                    r.NextBytes(b);
                    local_endpoint = BitConverter.ToUInt32(b, 0);
                } while (endpoints.ContainsKey(local_endpoint));
                
                e.m_LocalEndpoint = local_endpoint;
                endpoints.Add(local_endpoint, e);
                return local_endpoint;
            }
        }

        public void DeleteEndpoint(Endpoint e)
        {
           
            try
            {
                Transport c;
                lock (transports)
                {
                    c=transports[e.transport];
                }
                c.CloseTransportConnection(e,default(CancellationToken)).IgnoreResult();
            }
            catch {}

            try
            {
                lock (endpoints)
                {
                    endpoints.Remove(e.LocalEndpoint);
                }
            }
            catch { }
        }

        public void CheckConnection(uint endpoint)
        {
            try
            {
                Endpoint e;
                lock (endpoints)
                {
                    try
                    {
                        e = endpoints[endpoint];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new InvalidEndpointException("Invalid endpoint");
                    }
                }

                Transport c;
                lock (transports)
                {
                    try
                    {
                        c = transports[e.transport];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new ConnectionException("Transport not found");
                    }
                }
                c.CheckConnection(endpoint);
            }
            catch (KeyNotFoundException)
            {
                throw new ConnectionException("Transport not connected");
            }

        }

        private CancellationTokenSource shutdown_token = new CancellationTokenSource();

        public void Shutdown()
        {

            shutdown_token.Cancel();

            Transport[] cc;
            lock (transports)
            {
                cc = transports.Values.ToArray();
            }

            foreach (Transport c in cc)
            {
                try
                {
                   c.Close();
                }
                catch { };
            }

            ServerContext[] sc;
            lock (services)
            {
                sc = services.Values.ToArray();
            }

            foreach (ServerContext c in sc)
            {
                try
                {
                    c.Close();
                }
                catch { };
            }
            
            lock (endpoints)
            {
                try
                {
                    endpoints.Clear();
                }
                catch { }
            }

            

            //RobotRaconteurNode.sp = null;
        }


        
              

        private Dictionary<string, NodeDiscoveryInfo> m_DiscoveredNodes = new Dictionary<string, NodeDiscoveryInfo>();

        public Dictionary<string, NodeDiscoveryInfo> DiscoveredNodes { get { return m_DiscoveredNodes; } }

        public void NodeAnnouncePacketReceived(string packet)
        {
            try
            {
                string seed = "Robot Raconteur Node Discovery Packet";
                if (packet.Substring(0, seed.Length) == seed)
                {
                    lock (m_DiscoveredNodes)
                    {
                    string[] lines = packet.Split(new char[] { '\n' });
                    string[] idline = lines[1].Split(new char[] { ',' });
                       
                        NodeID nodeid = new NodeID(idline[0]);
                       
                    string nodename = idline[1];
                    string url = lines[2];
                    //if (!IPAddress.Parse(packet.Split(new char[] {'\n'})[1]).GetAddressBytes().SequenceEqual(RobotRaconteurNode.s.NodeID))

                    
                        if (m_DiscoveredNodes.Keys.Contains(nodeid.ToString()))
                        {
                            NodeDiscoveryInfo i = m_DiscoveredNodes[nodeid.ToString()];
                            i.NodeName = nodename;
                            if (!i.URLs.Any(x => x.URL == url))
                            {
                                NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                                u.URL = url;
                                u.LastAnnounceTime = DateTime.UtcNow;
                                i.URLs.Add(u);
                                //Console.WriteLine(url);
                            }
                            else
                            {
                                i.URLs.First(x => x.URL == url).LastAnnounceTime = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            NodeDiscoveryInfo i = new NodeDiscoveryInfo();
                            i.NodeID = nodeid;
                            i.NodeName = nodename;
                            NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                            u.URL = url;
                            u.LastAnnounceTime = DateTime.UtcNow;
                            i.URLs.Add(u);
                            m_DiscoveredNodes.Add(nodeid.ToString(),i);
                        }
                    }

                    //RobotRaconteurNode.s.NodeAnnouncePacketReceived(packet);
                }

            }
            catch { };

            //Console.WriteLine(packet);

        }

        private void NodeDetected(NodeDiscoveryInfo n)
        {            
            try
            {
                lock (m_DiscoveredNodes)
                {
                    if (m_DiscoveredNodes.Keys.Contains(n.NodeID.ToString()))
                    {
                        NodeDiscoveryInfo i = m_DiscoveredNodes[n.NodeID.ToString()];
                        i.NodeName = n.NodeName;
                        foreach (var url in n.URLs)
                        {
                            if (!i.URLs.Any(x => x.URL == url.URL))
                            {
                                NodeDiscoveryInfoURL u = new NodeDiscoveryInfoURL();
                                u.URL = url.URL;
                                u.LastAnnounceTime = DateTime.UtcNow;
                                i.URLs.Add(u);
                                //Console.WriteLine(url);
                            }
                            else
                            {
                                i.URLs.First(x => x.URL == url.URL).LastAnnounceTime = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        foreach (var u in n.URLs)
                        {
                            u.LastAnnounceTime = DateTime.UtcNow;
                        }
                        m_DiscoveredNodes.Add(n.NodeID.ToString(), n);
                    }
                }
            }
            catch (Exception) { }

        }

        private Task cleandiscoverednodes_task;

        protected void CleanDiscoveredNodes()
        {
            try
            {
                lock (m_DiscoveredNodes)
                {
                    DateTime now = DateTime.UtcNow;

                    string[] keys = m_DiscoveredNodes.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        List<NodeDiscoveryInfoURL> newurls = new List<NodeDiscoveryInfoURL>();
                        NodeDiscoveryInfoURL[] urls = m_DiscoveredNodes[key].URLs.ToArray();
                        foreach (NodeDiscoveryInfoURL u in urls)
                        {

                            double time = (now - u.LastAnnounceTime).TotalMilliseconds;
                            if (time < 60000)
                            {
                                newurls.Add(u);
                            }
                        }

                        m_DiscoveredNodes[key].URLs = newurls;

                        if (newurls.Count == 0)
                        {
                            m_DiscoveredNodes.Remove(key);
                        }

                    }


                }
            }
            catch (Exception) { }

        }

        private class ServiceIndexer : RobotRaconteurServiceIndex.ServiceIndex
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
                lock (node.m_DiscoveredNodes)
                {
                string[] nodeids = node.m_DiscoveredNodes.Keys.ToArray();
                int len = nodeids.Length;

                Dictionary<int, RobotRaconteurServiceIndex.NodeInfo> ret = new Dictionary<int, RobotRaconteurServiceIndex.NodeInfo>();

                for (int i = 0; i < len; i++)
                {
                    NodeDiscoveryInfo info = node.m_DiscoveredNodes[nodeids[i]];

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

        public static string SelectRemoteNodeURL(string[] urls)
        {
            var url_order = new string[] { 
                "rr+local://",
                "rr+pci://",
                "rr+usb://",
                "rr+tcp://127.0.0.1",
                "tcp://127.0.0.1",
                "rr+tcp://[::1]",
                "tcp://[::1]",
                "rr+tcp://localhost",
                "tcp://localhost",
                "rrs+tcp://[fe80",
                "rrs+wss://[fe80",
                "rrs+ws://[fe80",
                "rrs+tcp://",                
                "rrs+wss://",                
                "rrs+ws://",
                "rr+tcp://[fe80",
                "rr+wss://[fe80",
                "rr+ws://[fe80",
                "rr+tcp://",                
                "rr+wss://",                
                "rr+ws://",
            };

            foreach (var u in url_order)
            {
                var u1 = urls.FirstOrDefault(x => x.ToLower().StartsWith(u));
                if (u1 != null) return u1;
            }

            return urls.First() ;
        }


        public async Task<object> ConnectService(string[] urls, string username = null, object credentials = null, ClientContext.ClientServiceListenerDelegate listener = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
        {
            List<string> urlsl = urls.ToList();

            while (urlsl.Count > 0 && !cancel.IsCancellationRequested)
            {
                string url = SelectRemoteNodeURL(urlsl.ToArray());

                try
                {
                    return await ConnectService(url, username, credentials, listener, objecttype, cancel);
                }
                catch (Exception e)
                {
                    if (e is RobotRaconteurException)
                    {
                        if (!(e is ConnectionException || e is TimeoutException))
                            throw e;

                    }
                
                };

                urlsl.RemoveAll(x => (x == url));


            }
            throw new ConnectionException("Could not connect to service");

        }



        private async Task UpdateDetectedNodes(CancellationToken cancel)
        {
            var tasks = new List<Task<List<NodeDiscoveryInfo>>>();
            var t=new List<Transport>();
            lock(transports)
            {
                foreach (var t2 in transports.Values) t.Add(t2);
            }

            foreach (var t2 in t)
            {
                var task1 = t2.GetDetectedNodes(cancel);
                tasks.Add(task1);
            }

            await Task.WhenAll(tasks);

            foreach (var t2 in tasks)
            {
                try
                {
                    var info=await t2;
                    foreach (var i in info)
                    {
                        NodeDetected(i);
                    }
                }
                catch (Exception) { }
            }

        }

        private async Task<Tuple<byte[],string,Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>>> DoFindServiceByType(string[] urls, CancellationToken cancel)
        {
            RobotRaconteurServiceIndex.ServiceIndex ind = (RobotRaconteurServiceIndex.ServiceIndex)await this.ConnectService(urls.ToArray(),cancel: cancel);
            var NodeID = ((ServiceStub)ind).RRContext.RemoteNodeID.ToByteArray();
            var NodeName = ((ServiceStub)ind).RRContext.RemoteNodeName;
            var inf = await ind.GetLocalNodeServices(cancel);
            return Tuple.Create(NodeID, NodeName, inf);

        }


        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes)
        {
            var cancel=new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindServiceByType(servicetype, transportschemes, cancel.Token);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes, CancellationToken cancel)
        {

            try
            {
                await UpdateDetectedNodes(cancel);
            }
            catch { };

            List<ServiceInfo2> services = new List<ServiceInfo2>();
            List<string> nodeids;
            lock (m_DiscoveredNodes)
            {

                 nodeids= DiscoveredNodes.Keys.ToList();

            }

			var info_wait = new List<Task<Tuple<byte[],string,Dictionary<int, RobotRaconteurServiceIndex.ServiceInfo>>>>();

            for (int i=0; i<nodeids.Count; i++)
            {

                try
                {
                    List<string> urls = new List<string>();
                    lock (m_DiscoveredNodes)
                    {
                        foreach (NodeDiscoveryInfoURL url in m_DiscoveredNodes[nodeids[i]].URLs)
                        {
                            foreach (var s in transportschemes)
                            {
                                if (url.URL.StartsWith(s + "://"))
                                {
                                    urls.Add(url.URL);
                                    break;
                                }

                            }
                        }
                    }
					if (urls.Count > 0)
					{
						info_wait.Add(DoFindServiceByType(urls.ToArray(), cancel));
					}
                }
                catch (Exception)
                {
                    try
                    {
                        lock (m_DiscoveredNodes)
                        {
                            m_DiscoveredNodes.Remove(nodeids[i]);
                        }
                    }
                    catch { }
                }

            }

			if (info_wait.Count == 0) 
			{
                return new ServiceInfo2[0];
			}

            try
            {
                Task waittask = Task.WhenAll(info_wait);

                await waittask;
            }
            catch (Exception) { }

            for (int i=0; i<nodeids.Count; i++)
            {

                try
                {

                    if (!info_wait[i].IsCompleted)
                    {
                        throw new TimeoutException("Timeout");
                    }

                    var inf = await info_wait[i];
                    

                    RobotRaconteurServiceIndex.NodeInfo n = new RobotRaconteurServiceIndex.NodeInfo();
                    n.NodeID = inf.Item1;
                    n.NodeName = inf.Item2;


                    foreach (RobotRaconteurServiceIndex.ServiceInfo ii in inf.Item3.Values)
                    {
                        if (ii.RootObjectType == servicetype)
                        {
                            services.Add(new ServiceInfo2(ii, n));
                        }
                        else
                        {
                            foreach (string impl in ii.RootObjectImplements.Values)
                            {
                                if (impl == servicetype)
                                    services.Add(new ServiceInfo2(ii, n));
                            }

                        }
                    }
                }                
                catch
                {
                    try
                    {
                        lock (m_DiscoveredNodes)
                        {
                            m_DiscoveredNodes.Remove(nodeids[i]);
                        }
                    }
                    catch { }
                }
                
            }


            return services.ToArray() ;
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindNodeByID(id, schemes, cancel.Token);
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel);
            }
            catch { };

            var o = new List<NodeInfo2>();

            lock (m_DiscoveredNodes)
            {
                string nodeid_str=id.ToString();
                if (m_DiscoveredNodes.ContainsKey(nodeid_str))
                {
                    var ni=m_DiscoveredNodes[nodeid_str];
                    var n=new NodeInfo2();
                    n.NodeID=new NodeID(nodeid_str);
                    n.NodeName = ni.NodeName;

                    var c=new List<string>();

                    foreach (var url in ni.URLs)
                    {
                        var u = TransportUtil.ParseConnectionUrl(url.URL);
                        if (schemes.Any(x=> x==u.scheme))
                        {
                            string short_url;
                            if (u.port == -1)
                            {
                                short_url = u.scheme + "//" + u.host + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                            }
                            else
                            {
                                short_url = u.scheme + "//" + u.host + ":" + u.port + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                            }

                            c.Add(short_url);
                        }

                    }

                    if (c.Count != 0)
                    {
                        n.ConnectionURL = c.ToArray();
                        o.Add(n);
                    }
                }
            }

            return o.ToArray();
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes)
        {
            var cancel = new CancellationTokenSource();
            cancel.CancelAfter(5000);
            return await FindNodeByName(name, schemes, cancel.Token);
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes, CancellationToken cancel)
        {
            try
            {
                await UpdateDetectedNodes(cancel);
            }
            catch { };

            var o = new List<NodeInfo2>();

            lock (m_DiscoveredNodes)
            {
                foreach (var e in m_DiscoveredNodes)
                {
                    if (e.Value.NodeName == name)
                    {                        
                        var n = new NodeInfo2();
                        var nodeid_str = e.Value.NodeID.ToString();
                        n.NodeID = new NodeID(nodeid_str);
                        n.NodeName = e.Value.NodeName;

                        var c = new List<string>();

                        foreach (var url in e.Value.URLs)
                        {
                            var u = TransportUtil.ParseConnectionUrl(url.URL);
                            if (schemes.Any(x => x == u.scheme))
                            {
                                string short_url;
                                if (u.port == -1)
                                {
                                    short_url = u.scheme + "//" + u.host + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                                }
                                else
                                {
                                    short_url = u.scheme + "//" + u.host + ":" + u.port + u.path + "?nodeid=" + nodeid_str.Trim(new char[] { '{', '}' });
                                }

                                c.Add(short_url);
                            }

                        }

                        if (c.Count != 0)
                        {
                            n.ConnectionURL = c.ToArray();
                            o.Add(n);
                        }
                    }
                }
            }

            return o.ToArray();
        }

        public async Task<string> RequestObjectLock(object obj, RobotRaconteurObjectLockFlags flags, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only lock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.RequestObjectLock(obj,flags, cancel);

            
        }


        public async Task<string> ReleaseObjectLock(object obj, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only unlock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.ReleaseObjectLock(obj, cancel);
        }

        public class MonitorLock
        {
            internal IDisposable lock_;
            internal ServiceStub stub;
        }

        public async Task<MonitorLock> MonitorEnter(object obj, int timeout = -1, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can be monitored by RobotRaconteurNode");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.MonitorEnter(obj,timeout, cancel);
        }

        public async Task MonitorExit(RobotRaconteurNode.MonitorLock lock_, CancellationToken cancel = default(CancellationToken))
        {
            
            await lock_.stub.RRContext.MonitorExit(lock_, cancel);
        }

        public async Task<object> FindObjRefTyped(object obj, string objref, string objecttype, CancellationToken cancel)
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can have objrefs");
            ServiceStub s = (ServiceStub)obj;

            return await s.FindObjRefTyped(objref, objecttype, cancel);
        }

        public async Task<object> FindObjRefTyped(object obj, string objref, string index, string objecttype, CancellationToken cancel)
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can have objrefs");
            ServiceStub s = (ServiceStub)obj;

            return await s.FindObjRefTyped(objref, index, objecttype, cancel);
        }

        public DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public ITimer CreateTimer(int period, Action<TimerEvent> handler, bool oneshot = false)
        {
            var t = new WallTimer(period, handler, oneshot, this);
            return t;
        }

        public IRate CreateRate(double period)
        {
            return new WallRate(period, this);
        }

        public string GetRandomString(int count)
        {
            string o="";
            Random r = new Random();
            string strvals = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            for (int i = 0; i < count; i++)
            {
                o += strvals[r.Next(0, (strvals.Length - 1))];
            }
            return o;
        }

        protected string service_state_nonce;

        public string ServiceStateNonce
        {
            get
            {
                lock(this)
                {
                    return service_state_nonce;
                }
            }

        }

        public void UpdateServiceStateNonce()
        {
            lock (this)
            {
                string new_nonce;
                do
                {
                    Random r = new Random();
                    new_nonce = GetRandomString(16);
                } while (new_nonce == service_state_nonce);

                service_state_nonce = new_nonce;
            }

            lock (transports)
            {
                foreach (var t in transports.Values)
                {
                    t.LocalNodeServicesChanged();
                }
            }
        }
    }
        

    public class ServiceInfo2
    {
        public string Name;
        public string RootObjectType;
        public string[] RootObjectImplements;
        public string[] ConnectionURL;
        public Dictionary<string, object> Attributes;
        public NodeID NodeID;
        public string NodeName;

        public ServiceInfo2() { }

        public ServiceInfo2(RobotRaconteurServiceIndex.ServiceInfo info,RobotRaconteurServiceIndex.NodeInfo ninfo)
        {
            Name = info.Name;
            RootObjectType = info.RootObjectType;
            RootObjectImplements = info.RootObjectImplements.Values.ToArray();
            ConnectionURL = info.ConnectionURL.Values.ToArray();
            Attributes = info.Attributes;
            NodeID = new NodeID(ninfo.NodeID);
            NodeName = ninfo.NodeName;

        }

    }

    public class NodeInfo2
    {
        public NodeID NodeID;
        public string NodeName;
        public string[] ConnectionURL;
    }

    public class TimeSpec
    {
        
        private static long start_ticks;
        private static long ticks_per_second;

        private static long start_seconds;
        private static int start_nanoseconds;
        
        private static DateTime start_time;
        private static bool started = false;

        private static bool iswindows = false;
        
        public long seconds;
        public int nanoseconds;

        private static object start_lock=new object();

        public TimeSpec(long seconds, int nanoseconds)
        {
            this.seconds = seconds;
            this.nanoseconds = nanoseconds;

            lock(start_lock)
            {
            if (!started) start();
            }

            cleanup_nanosecs();
        }

        public TimeSpec()
        {
            if (!started) start();
            this.seconds = 0;
            this.nanoseconds = 0;
        }

        public static TimeSpec Now
        {
            get
            {
                TimeSpec t = new TimeSpec();
                t.timespec_now();
                return t;
            }

        }

        private void timespec_now()
        {
            this.seconds = 0;
            this.nanoseconds = 0;

            lock (start_lock)
            {
                if (!started)
                {
                    start();
                    this.seconds = start_seconds;
                    this.nanoseconds = start_nanoseconds;
                }
                else
                {
                    if (iswindows)
                    {
                        long ticks;
                        QueryPerformanceCounter(out ticks);

                        long diff_ticks = ticks - start_ticks;
                        long diff_secs = diff_ticks / ticks_per_second;
                       
                        long diff_nanosecs = ((diff_ticks * (long)1e9) / ticks_per_second) % (long)1e9;

                        

                        this.seconds = diff_secs + start_seconds;
                        this.nanoseconds = (int)diff_nanosecs + start_nanoseconds;
                    }
                    else
                    {
                        TimeSpan t = DateTime.UtcNow.ToUniversalTime() - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
						this.seconds = (long)Math.Round(t.TotalSeconds);
						this.nanoseconds = (int)Math.IEEERemainder(t.TotalMilliseconds * 1e6, 1e9) ;

                        
                    }

                }
            }

            cleanup_nanosecs();


        }


        private void start()
        {
            if (!started)
            {

#if !ROBOTRACONTEUR_BRIDGE
                if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.WinCE)
                {
                    iswindows = true;
                    QueryPerformanceCounter(out start_ticks);
                    QueryPerformanceFrequency(out ticks_per_second);
                    
                    
                }
#endif

                start_time = DateTime.UtcNow.ToUniversalTime();
                TimeSpan t = start_time - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                start_seconds = (long)Math.Round(t.TotalSeconds);
                start_nanoseconds = (int)Math.IEEERemainder(t.TotalMilliseconds * 1e6, 1e9);

                started = true;
            }

        }

        


        /*[DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);*/

        private static  bool QueryPerformanceCounter(out long lpPerformanceCount)
        {
            lpPerformanceCount=System.Diagnostics.Stopwatch.GetTimestamp();
            return true;
        }

        private static  bool QueryPerformanceFrequency(out long lpFrequency)
        {
            lpFrequency = System.Diagnostics.Stopwatch.Frequency;
            return true;
        }

        

        private void cleanup_nanosecs()
        {
            int nanoseconds1 = nanoseconds;

            int nano_div = nanoseconds / (int)(1e9);
            nanoseconds = nanoseconds % (int)(1e9);
            seconds += nano_div;

            

            if (seconds > 0 && nanoseconds < 0)
            {
                seconds = seconds - 1;
                nanoseconds = (int)1e9 + nanoseconds;
            }

            if (seconds < 0 && nanoseconds > 0)
            {
                seconds = seconds + 1;
                nanoseconds = nanoseconds - (int)1e9;
            }

            

        }

        public static bool operator ==(TimeSpec t1, TimeSpec t2)
        {
            if (((object)t1) == null && ((object)t2) == null) return true;
            if (((object)t1) == null || ((object)t2) == null) return false;

            return (t1.seconds == t2.seconds && t1.nanoseconds == t2.nanoseconds);
        }

        public static bool operator !=(TimeSpec t1, TimeSpec t2)
        {
            return !(t1 == t2);
        }

        public static TimeSpec operator -(TimeSpec t1, TimeSpec t2)
        {
            return new TimeSpec(t1.seconds - t2.seconds, t1.nanoseconds - t2.nanoseconds);
        }

        public static TimeSpec operator +(TimeSpec t1, TimeSpec t2)
        {
            return new TimeSpec(t1.seconds + t2.seconds, t1.nanoseconds + t2.nanoseconds);
        }

        public static bool operator >(TimeSpec t1, TimeSpec t2)
        {
            TimeSpec diff = t1 - t2;
            if (diff.seconds == 0) return diff.nanoseconds > 0;
            return diff.seconds > 0;
        }

        public static bool operator >=(TimeSpec t1, TimeSpec t2)
        {
            if (t1 == t2) return true;
            return t1 > t2;
        }

        public static bool operator <(TimeSpec t1, TimeSpec t2)
        {
            return t2>= t1;
        }

        public static bool operator <=(TimeSpec t1, TimeSpec t2)
        {
            return t2 > t1;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is TimeSpec)) return false;
            return this == (TimeSpec)obj;
        }

        public override int GetHashCode()
        {
            return nanoseconds+(int)seconds;
        }
    }

    public enum RobotRaconteurObjectLockFlags
    {
        USER_LOCK=0,
        CLIENT_LOCK
    }

    public class NodeDiscoveryInfo
    {
        public NodeID NodeID;
        public string NodeName = "";
        public List<NodeDiscoveryInfoURL> URLs = new List<NodeDiscoveryInfoURL>();
    }

    public class NodeDiscoveryInfoURL
    {
        public string URL;
        public DateTime LastAnnounceTime;
    }
}
