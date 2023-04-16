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
using RobotRaconteurWeb.Extensions;
using System.Linq.Expressions;

using static RobotRaconteurWeb.RRLogFuncs;
using System.IO;

namespace RobotRaconteurWeb
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

        public const string Version = "0.9.2";

        private NodeID m_NodeID;

        public NodeID NodeID
        {
            get
            {
                if (m_NodeID == null)
                {
                    m_NodeID = NodeID.NewUniqueID();
                    LogInfo(string.Format("RobotRaconteurNode NodeID configured with random UUID {0}", m_NodeID.ToString()), this, component: RobotRaconteur_LogComponent.Node);
                }
                return m_NodeID;
            }
            set
            {
                if (m_NodeID == null)
                {
                    m_NodeID = value;
                    LogInfo(string.Format("RobotRaconteurNode NodeID configured with UUID {0}", value.ToString()), this, component: RobotRaconteur_LogComponent.Node);

                }
                else
                {
#if RR_LOG_DEBUG
                    LogDebug("RobotRaconteurNode attempt to set NodeID when already set", this, component: RobotRaconteur_LogComponent.Node);
#endif
                    throw new InvalidOperationException("NodeID cannot be changed once it is set");
                }
            }
        }

        public bool TryGetNodeID(out NodeID nodeid)
        {
            if (m_NodeID == null)
            {
                nodeid = null;
                return false;
            }
            nodeid = m_NodeID;
            return true;
        }


        private string m_NodeName;
        public string NodeName
        {
            get
            {
                if (m_NodeName == null) m_NodeName = "";
                LogInfo(string.Format("RobotRaconteurNode NodeName configured with {0}", m_NodeName), this, component: RobotRaconteur_LogComponent.Node);
                return m_NodeName;
            }
            set
            {
                if (m_NodeName == null)
                {
                    if (value.Length > 1024)
                    {
#if RR_LOG_DEBUG
                        LogDebug("RobotRaconteurNode attempt to set NodeName with length > 1024", this, component: RobotRaconteur_LogComponent.Node);
#endif
                        throw new InvalidOperationException("Invalid node name, too long");
                    }


                    if (!Regex.Match(value, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$").Success)
                    {
#if RR_LOG_DEBUG
                        LogDebug("RobotRaconteurNode attempt to set NodeName with invalid characters", this, component: RobotRaconteur_LogComponent.Node);
#endif

                        throw new InvalidOperationException("Invalid node name");
                    }

                    m_NodeName = value;

                }
                else
                {
#if RR_LOG_DEBUG
                    LogDebug("RobotRaconteurNode attempt to set NodeName when already set", this, component: RobotRaconteur_LogComponent.Node);
#endif
                    throw new InvalidOperationException("NodeName cannot be changed once it is set");
                }
            }

        }

        public bool TryGetNodeName(out string nodename)
        {
            if (m_NodeName == null)
            {
                nodename = null;
                return false;
            }
            nodename = m_NodeName;
            return true;
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
#if ROBOTRACONTEUR_BRIDGE
            browser_transport = new BrowserWebSocketTransport(this);
            RegisterTransport(browser_transport);
            m_Discovery = new Discovery(this);
#endif
            LogInfo(string.Format("RobotRaconteurNode version {0} initialized", Version), this);

        }

        private ServiceFactory GetServiceFactoryForType(string type, ClientContext context)
        {
            string servicename = ServiceDefinitionUtil.SplitQualifiedName(type).Item1;

            if (context != null)
            {
                ServiceFactory f;
                if (context.TryGetPulledServiceType(servicename, out f))
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

        public MessageElementNestedElementList PackStructure(Object s, ClientContext context)
        {
            if (s == null) return null;

            return GetServiceFactoryForType(s.GetType(), context).PackStructure(s);
        }

        public T UnpackStructure<T>(MessageElementNestedElementList l, ClientContext context)
        {
            if (l == null) return default(T);
            return GetServiceFactoryForType(l.TypeName, context).UnpackStructure<T>(l);
        }

        public MessageElementNestedElementList PackPodToArray<T>(ref T s, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(s.GetType(), context).PackPodToArray(ref s);
        }

        public T UnpackPodFromArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(l.TypeName, context).UnpackPodFromArray<T>(l);
        }

        public MessageElementNestedElementList PackPodArray<T>(T[] s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.GetType(), context).PackPodArray(s);
        }

        public T[] UnpackPodArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.TypeName, context).UnpackPodArray<T>(l);
        }

        public MessageElementNestedElementList PackPodMultiDimArray<T>(PodMultiDimArray s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.pod_array.GetType(), context).PackPodMultiDimArray<T>(s);
        }

        public PodMultiDimArray UnpackPodMultiDimArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.TypeName, context).UnpackPodMultiDimArray<T>(l);
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

        public MessageElementNestedElementList PackNamedArrayToArray<T>(ref T s, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(s.GetType(), context).PackNamedArrayToArray(ref s);
        }

        public T UnpackNamedArrayFromArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            return GetServiceFactoryForType(l.TypeName, context).UnpackNamedArrayFromArray<T>(l);
        }

        public MessageElementNestedElementList PackNamedArray<T>(T[] s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.GetType(), context).PackNamedArray(s);
        }

        public T[] UnpackNamedArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.TypeName, context).UnpackNamedArray<T>(l);
        }

        public MessageElementNestedElementList PackNamedMultiDimArray<T>(NamedMultiDimArray s, ClientContext context) where T : struct
        {
            if (s == null) return null;
            return GetServiceFactoryForType(s.namedarray_array.GetType(), context).PackNamedMultiDimArray<T>(s);
        }

        public NamedMultiDimArray UnpackNamedMultiDimArray<T>(MessageElementNestedElementList l, ClientContext context) where T : struct
        {
            if (l == null) return null;
            return GetServiceFactoryForType(l.TypeName, context).UnpackNamedMultiDimArray<T>(l);
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

        public MessageElement PackAnyType<T>(string name, ref T data, ClientContext context)
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

            if (t.IsPrimitive)
            {
                return MessageElementUtil.NewMessageElement(name, new T[] { data });
            }

            if (is_array && t.GetElementType().IsPrimitive)
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
                if (t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var method = typeof(RobotRaconteurNode).GetMethod("PackMapType");
                    var dict_params = t.GetGenericArguments();
                    var generic = method.MakeGenericMethod(dict_params);
                    var packed_map = generic.Invoke(this, new object[] { data, context });
                    return MessageElementUtil.NewMessageElement(name, packed_map);
                }
                if (t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var method = typeof(RobotRaconteurNode).GetMethod("PackListType");
                    var list_params = t.GetGenericArguments();
                    var generic = method.MakeGenericMethod(list_params);
                    var packed_list = generic.Invoke(this, new object[] { data, context });
                    return MessageElementUtil.NewMessageElement(name, packed_list);
                }
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

        private MessageElement PackAnyType<T>(int num, ref T data, ClientContext context)
        {
            return PackAnyType(num.ToString(), ref data, context);
        }

        public T UnpackAnyType<T>(MessageElement e, ClientContext context = null)
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
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.multidimarray_t);
                        return (T)(object)UnpackMultiDimArray(md);
                    }
                case DataTypes.structure_t:
                    {
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.structure_t);
                        return UnpackStructure<T>(md, context);
                    }
                /*case DataTypes.pod_t:
                    using (MessageElementData md = (MessageElementData)e.Data)
                    {
                        return (T)UnpackPod(md);
                    }*/
                case DataTypes.pod_array_t:
                    {
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.pod_array_t);
                        if (typeof(T).IsValueType)
                        {
                            if (md.Elements.Count != 1) throw new DataTypeException("Invalid array size for scalar structure");
                            return ((T[])UnpackPod(md, context))[0];
                        }
                        else
                        {
                            return (T)UnpackPod(md, context);
                        }
                    }
                case DataTypes.pod_multidimarray_t:
                    {
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.pod_multidimarray_t);
                        return (T)UnpackPod(md, context);
                    }
                case DataTypes.namedarray_array_t:
                    {
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.namedarray_array_t);
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
                        MessageElementNestedElementList md = e.CastDataToNestedList(DataTypes.namedarray_multidimarray_t);
                        return (T)UnpackNamedArray(e.Data, context);
                    }
                case DataTypes.vector_t:
                case DataTypes.dictionary_t:
                    {
                        var t = typeof(T);
                        var method = typeof(RobotRaconteurNode).GetMethod("UnpackMapType");
                        var dict_params = t.GetGenericArguments();
                        var generic = method.MakeGenericMethod(dict_params);
                        return (T)generic.Invoke(this, new object[] { e.Data, context });
                    }
                case DataTypes.list_t:
                    {
                        var t = typeof(T);
                        var method = typeof(RobotRaconteurNode).GetMethod("UnpackListType");
                        var list_params = t.GetGenericArguments();
                        var generic = method.MakeGenericMethod(list_params);
                        return (T)generic.Invoke(this, new object[] { e.Data, context });
                    }
                default:
                    throw new DataTypeException("Invalid container data type");
            }
        }

        public T UnpackAnyType<T>(MessageElement e, out string name, ClientContext context)
        {
            name = e.ElementName;
            return UnpackAnyType<T>(e, context);
        }

        public T UnpackAnyType<T>(MessageElement e, out int num, ClientContext context)
        {
            num = MessageElementUtil.GetMessageElementNumber(e);
            return UnpackAnyType<T>(e, context);
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
                    MessageElementUtil.AddMessageElement(m, PackAnyType(Convert.ToInt32(d.Key), ref v, context));
                }
                return new MessageElementNestedElementList(DataTypes.vector_t, "", m);
            }

            if (typeof(Tkey) == typeof(String))
            {
                var m = new List<MessageElement>();
                Dictionary<Tkey, Tvalue> ddata = (Dictionary<Tkey, Tvalue>)data;

                foreach (KeyValuePair<Tkey, Tvalue> d in ddata)
                {
                    var v = d.Value;
                    MessageElementUtil.AddMessageElement(m, PackAnyType(d.Key.ToString(), ref v, context));
                }
                return new MessageElementNestedElementList(DataTypes.dictionary_t, "", m);
            }

            throw new DataTypeException("Indexed types can only be indexed by int32 and string");
        }


        public object UnpackMapType<Tkey, Tvalue>(object data, ClientContext context)
        {

            if (data == null) return null;

            var cdata = (MessageElementNestedElementList)data;

            if (cdata.Type == DataTypes.vector_t)
            {
                Dictionary<int, Tvalue> o = new Dictionary<int, Tvalue>();


                var cdataElements = cdata.Elements;
                {
                    foreach (MessageElement e in cdataElements)
                    {
                        int num;
                        var val = UnpackAnyType<Tvalue>(e, out num, context);
                        o.Add(num, val);

                    }
                    return o;
                }
            }
            else if (cdata.Type == DataTypes.dictionary_t)
            {
                Dictionary<string, Tvalue> o = new Dictionary<string, Tvalue>();


                var cdataElements = cdata.Elements;
                {
                    foreach (MessageElement e in cdataElements)
                    {
                        string name;
                        var val = UnpackAnyType<Tvalue>(e, out name, context);
                        o.Add(name, val);
                    }
                    return o;
                }
            }
            else
            {
                throw new DataTypeException("May types can only be keyed by int32 and string");
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
                    MessageElementUtil.AddMessageElement(m, PackAnyType(count, ref v, context));
                    count++;
                }

                return new MessageElementNestedElementList(DataTypes.list_t, "", m);
            }
        }

        public object UnpackListType<Tvalue>(object data, ClientContext context)
        {
            if (data == null) return null;
            List<Tvalue> o = new List<Tvalue>();
            int count = 0;
            MessageElementNestedElementList cdata = (MessageElementNestedElementList)data;
            var cdataElements = cdata.Elements;
            {
                foreach (MessageElement e in cdataElements)
                {
                    int num;
                    var val = UnpackAnyType<Tvalue>(e, out num, context);
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

            if (t.IsEnum)
            {
                return new int[] { (int)(data) };
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
                        MessageElementNestedElementList md = me.CastDataToNestedList();
                        return UnpackMultiDimArray(md);
                    }
                case DataTypes.structure_t:
                    {
                        MessageElementNestedElementList md = me.CastDataToNestedList();
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

        public MessageElementNestedElementList PackMultiDimArray(MultiDimArray array)
        {
            if (array == null) return null;
            List<MessageElement> l = new List<MessageElement>();
            l.Add(new MessageElement("dims", array.Dims));
            l.Add(new MessageElement("array", array.Array_));
            return new MessageElementNestedElementList(DataTypes.multidimarray_t, "", l);
        }

        public MultiDimArray UnpackMultiDimArray(MessageElementNestedElementList marray)
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
#if RR_LOG_DEBUG
                LogDebug("Message sender node ID does not match node ID", this, component: RobotRaconteur_LogComponent.Node);
#endif
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
#if RR_LOG_DEBUG
                LogDebug("Could not find transport", this, component: RobotRaconteur_LogComponent.Node);
#endif
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
#if RR_LOG_DEBUG
                LogDebug("Message receiver node ID does not match node ID", this, component: RobotRaconteur_LogComponent.Node);
#endif

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
#if RR_LOG_DEBUG
                    LogDebug(string.Format("Received message with invalid ReceiverEndpoint: {0}", m.header.ReceiverEndpoint), this, component: RobotRaconteur_LogComponent.Node);
#endif
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
                    MessageEntry eret = new MessageEntry(me.EntryType + 1, me.MemberName);
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
#if RR_LOG_TRACE
            LogTrace("Service type registered {0}", this, component: RobotRaconteur_LogComponent.Node);
#endif

        }

        public ServiceFactory GetServiceType(string servicetype)
        {
            ServiceFactory f;
            if (!TryGetServiceType(servicetype, out f))
            {
#if RR_LOG_DEBUG
                LogDebug(string.Format("Cannot unregister nonexistant service type \"{0}\"",servicetype), this, component: RobotRaconteur_LogComponent.Node);
#endif
                throw new ServiceException("Service factory not found for " + servicetype);
            }
            return f;
        }

        public bool TryGetServiceType(string servicetype, out ServiceFactory f)
        {
            lock (service_factories)
            {
                return service_factories.TryGetValue(servicetype, out f);
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
            if (this.dynamic_factory != null)
            {
#if RR_LOG_DEBUG
                LogDebug("Attempt to register dynamic service factory when already registered", this, component: RobotRaconteur_LogComponent.Node);
#endif
                throw new InvalidOperationException("Dynamic service factory already set");
            }
            this.dynamic_factory = f;
#if RR_LOG_TRACE
            LogTrace("Registered dynamic service factory", this, component: RobotRaconteur_LogComponent.Node);
#endif
        }

        public ServerContext RegisterService(string name, string servicetype, Object obj, ServiceSecurityPolicy securitypolicy = null)
        {
            lock (services)
            {

                if (services.Keys.Contains(name))
                {
                    CloseService(name);
                }

                ServerContext c = new ServerContext(GetServiceType(servicetype), this);
                c.SetBaseObject(name, obj, securitypolicy);

                //RegisterEndpoint(c);
                services.Add(name, c);

                UpdateServiceStateNonce();

                LogInfo(string.Format("Service {0} registered", name), this, RobotRaconteur_LogComponent.Node);

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
                LogInfo(string.Format("Service {0} registered", c.ServiceName), this, RobotRaconteur_LogComponent.Node);
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
            LogInfo(string.Format("Service {0} removed", sname), this, RobotRaconteur_LogComponent.Node);



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
#if RR_LOG_DEBUG
                LogDebug(string.Format("Service {0} not found", name), this, component: RobotRaconteur_LogComponent.Node);
#endif
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
#if RR_LOG_TRACE
                LogTrace(string.Format("Transport {0} registered", c.UrlSchemeString), this, component: RobotRaconteur_LogComponent.Node);
#endif
                return transport_count;
            }
        }


        public async Task<Message> SpecialRequest(Message m, uint transportid)
        {


            if (!(m.header.ReceiverNodeID == NodeID.Any && (m.header.ReceiverNodeName == "" || m.header.ReceiverNodeName == NodeName))
                && !(m.header.ReceiverNodeID == NodeID))
            {
#if RR_LOG_DEBUG
                LogDebug(string.Format("Received SpecialRequest with invalid ReceiverNodeID: {0}", m.header.ReceiverNodeID.ToString()), this, component: RobotRaconteur_LogComponent.Node);
#endif
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
#if RR_LOG_DEBUG
                LogDebug(string.Format("Special request received from {0} ep {1} to {2} ep {3} EntryType {4} Error {5}",
                    m.header.SenderNodeID.ToString(), m.header.SenderEndpoint, m.header.ReceiverNodeID,
                    m.header.ReceiverEndpoint, e.EntryType, e.Error), this, component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                    service_path: e.ServicePath, member: e.MemberName);
#endif
                MessageEntry eret = ret.AddEntry(e.EntryType + 1, e.MemberName);
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
#if RR_LOG_DEBUG
                                LogDebug("Client requested type of an invalid service path", this,
                                    component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                    service_path: e.ServicePath, member: e.MemberName);
#endif
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
                                string servicedef = "";
                                if (e.elements.Any(x => x.ElementName == "ServiceType"))
                                {
                                    name = e.FindElement("ServiceType").CastData<string>();
                                    servicedef = GetServiceType(name).DefString();
                                }
                                else if (e.elements.Any(x => x.ElementName == "servicetype"))
                                {
                                    name = e.FindElement("servicetype").CastData<string>();
                                    servicedef = GetServiceType(name).DefString();
                                }
                                else
                                {
                                    servicedef = GetService(name).ServiceDef.DefString();
                                    eret.AddElement("attributes", PackMapType<string, object>(GetService(name).Attributes, null));
                                }
                                eret.AddElement("servicedef", servicedef);
                            }
                            catch
                            {
#if RR_LOG_DEBUG
                                LogDebug(string.Format("Client requested type of an invalid service type {0}", name), this,
                                    component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                    service_path: e.ServicePath, member: e.MemberName);
#endif
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
                                ServerEndpoint se = new ServerEndpoint(c, this);

                                se.m_RemoteEndpoint = m.header.SenderEndpoint;
                                se.m_RemoteNodeID = m.header.SenderNodeID;
                                RegisterEndpoint(se);

                                se.transport = transportid;

                                c.AddClient(se);

                                ret.header.SenderEndpoint = se.LocalEndpoint;

                                // Info log client connected
                                LogInfo(string.Format("Client connected to service {0} from {1} ep {2}", name, m.header.SenderNodeID.ToString(), m.header.SenderEndpoint), this,
                                                                       component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                                                                                          service_path: e.ServicePath, member: e.MemberName);

                                //services[name].AddClient(m.header.SenderEndpoint);
                                //eret.AddElement("servicedef", servicedef);
                            }
                            catch (Exception exp)
                            {
#if RR_LOG_DEBUG
                                LogDebug(string.Format("Error connecting client: {0}",exp), this,
                                    component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                    service_path: e.ServicePath, member: e.MemberName);
#endif
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
                                // Info log client disconnected
                                LogInfo(string.Format("Client disconnected from service {0} from {1} ep {2}", name, m.header.SenderNodeID.ToString(), m.header.SenderEndpoint), this,
                                                                             component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                                                             service_path: e.ServicePath, member: e.MemberName);
                            }
                            catch (Exception exp)
                            {
#if RR_LOG_DEBUG
                                LogDebug(string.Format("Error disconnecting client: {0}", exp), this,
                                    component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                    service_path: e.ServicePath, member: e.MemberName);
#endif
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

                                Dictionary<string, object> attr = s.Attributes;
                                eret.AddElement("return", PackMapType<string, object>(attr, null));
                            }
                            catch (Exception exp)
                            {
#if RR_LOG_DEBUG
                                LogDebug(string.Format("Error returning attributes: {0}", exp), this,
                                    component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                                    service_path: e.ServicePath, member: e.MemberName);
#endif
                                eret.AddElement("errorname", "RobotRaconteur.ServiceError");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceError;
                            }
                        }
                        break;


                    default:
#if RR_LOG_DEBUG
                        LogDebug(string.Format("Invalid special request EntryType: {0}", e.EntryType), this,
                            component: RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint,
                            service_path: e.ServicePath, member: e.MemberName);
#endif
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
            try
            {
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

                            object r = await c.ConnectService(end, url, username, credentials, objecttype, cancel);

                            if (listener != null)
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
            catch (Exception exp)
            {
                //Debug log exception
                #if RR_LOG_DEBUG
                LogDebug(string.Format("Error connecting service: {0}", exp), this,
                                       component: RobotRaconteur_LogComponent.Node);
                #endif


                throw;
            }
        }

        public async Task<object> ConnectService(string[] url, string username = null, object credentials = null, ClientContext.ClientServiceListenerDelegate listener = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                var connecting_tasks = new List<Task<object>>();

                foreach (var u in url)
                {
                    connecting_tasks.Add(ConnectService(u, username, credentials, null, objecttype, cancel));

                    await Task.WhenAny(Task.WhenAny(connecting_tasks), Task.Delay(250));

                    if (connecting_tasks.Any(x => x.IsCompleted && x.Status == TaskStatus.RanToCompletion))
                    {
                        break;
                    }
                }

                while (true)
                {
                    object r = null;
                    if (connecting_tasks.Count == 1)
                    {
                        r = await connecting_tasks[0];
                    }
                    else
                    {
                        await Task.WhenAny(connecting_tasks);

                        var completed_task = connecting_tasks.First(x => x.IsCompleted);
                        connecting_tasks.Remove(completed_task);
                        if (completed_task.Status == TaskStatus.RanToCompletion)
                        {
                            r = await completed_task;
                            foreach (var t in connecting_tasks)
                            {
                                try
                                {
                                    _ = t.ContinueWith((x) =>
                                    {
                                        try
                                        {
                                            Task.Run(() => ((ClientContext)x.Result).Close().IgnoreResult());
                                        }
                                        catch { }
                                    });

                                }
                                catch { }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (listener != null)
                        ((ClientContext)r).ClientServiceListener += listener;
                    return r;
                }
            }
            catch (Exception exp)
            {
                //Debug log exception
#if RR_LOG_DEBUG
                LogDebug(string.Format("Error connecting service: {0}", exp), this,
                                                          component: RobotRaconteur_LogComponent.Node);
#endif
                throw;
            }
        }

        public async Task DisconnectService(object obj, CancellationToken cancel = default(CancellationToken))
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
                    c = transports[e.transport];
                }
                c.CloseTransportConnection(e, default(CancellationToken)).IgnoreResult();
            }
            catch { }

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
            m_Discovery?.Shutdown();

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



        public Dictionary<string, NodeDiscoveryInfo> DiscoveredNodes { get { return m_Discovery.DiscoveredNodes; } }

        internal Discovery m_Discovery;

        public void NodeAnnouncePacketReceived(string packet)
        {
            m_Discovery.NodeAnnouncePacketReceived(packet);
        }

        internal void NodeDetected(NodeDiscoveryInfo n)
        {
            m_Discovery.NodeDetected(n);
        }


        protected void CleanDiscoveredNodes()
        {
            m_Discovery.CleanDiscoveredNodes();
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

            return urls.First();
        }


        /*public async Task<object> ConnectService(string[] urls, string username = null, object credentials = null, ClientContext.ClientServiceListenerDelegate listener = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
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

        }*/



        private async Task UpdateDetectedNodes(CancellationToken cancel)
        {
            await m_Discovery.UpdateDetectedNodes(cancel);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes)
        {
            return await m_Discovery.FindServiceByType(servicetype, transportschemes);
        }

        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes, CancellationToken cancel)
        {

            return await m_Discovery.FindServiceByType(servicetype, transportschemes, cancel);
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes)
        {
            return await m_Discovery.FindNodeByID(id, schemes);
        }

        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes, CancellationToken cancel)
        {
            return await m_Discovery.FindNodeByID(id, schemes, cancel);
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes)
        {
            return await m_Discovery.FindNodeByName(name, schemes);
        }

        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes, CancellationToken cancel)
        {
            return await m_Discovery.FindNodeByName(name, schemes, cancel);
        }

        public async Task<string> RequestObjectLock(object obj, RobotRaconteurObjectLockFlags flags, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only lock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.RequestObjectLock(obj, flags, cancel);


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

            return await s.RRContext.MonitorEnter(obj, timeout, cancel);
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
            string o = "";
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
                lock (this)
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

        public ServiceInfo2Subscription SubscribeServiceInfo2(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            return m_Discovery.SubscribeServiceInfo2(service_types, filter);
        }

        public ServiceSubscription SubscribeServiceByType(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            return m_Discovery.SubscribeServiceByType(service_types, filter);
        }

        public ServiceSubscription SubscribeService(string[] url, string username = null, Dictionary<string, object> credentials = null, string objecttype = null)
        {
            return m_Discovery.SubscribeService(url, username, credentials, objecttype);
        }

        ILogRecordHandler m_LogRecordHandler;

        public RobotRaconteur_LogLevel LogLevel
        {
            get; set;
        }

        public bool CompareLogLevel(RobotRaconteur_LogLevel level)
        {
            return (int)level >= (int)LogLevel;
        }

        public void LogMessage(RobotRaconteur_LogLevel level, string message)
        {
            LogRecord(new RRLogRecord() { Node = this, Level = level, Message = message });
        }

        public void LogRecord(RRLogRecord record)
        {
            if ((int)record.Level < (int)LogLevel)
                return;
            if (m_LogRecordHandler != null)
            {
                m_LogRecordHandler.Log(record);
                return;
            }

#if !ROBOTRACONTEUR_BRIDGE
            RRLogFuncs.WriteLogRecord(Console.Error, record);
            Console.Error.WriteLine();
#else
            var t = new StringWriter();
            RRLogFuncs.WriteLogRecord(t, record);
            Console.WriteLine(t.ToString());
#endif
        }

        public RobotRaconteur_LogLevel SetLogLevelFromString(string loglevel)
        {

            if (loglevel == "DISABLE")
            {
                LogLevel = RobotRaconteur_LogLevel.Disable;
                return RobotRaconteur_LogLevel.Disable;
            }

            if (loglevel == "FATAL")
            {
                LogLevel = RobotRaconteur_LogLevel.Fatal;
                return RobotRaconteur_LogLevel.Fatal;
            }

            if (loglevel == "ERROR")
            {
                LogLevel = RobotRaconteur_LogLevel.Error;
                return RobotRaconteur_LogLevel.Error;
            }

            if (loglevel == "WARNING")
            {
                LogLevel = RobotRaconteur_LogLevel.Warning;
                return RobotRaconteur_LogLevel.Warning;
            }

            if (loglevel == "INFO")
            {
                LogLevel = RobotRaconteur_LogLevel.Info;
                return RobotRaconteur_LogLevel.Info;
            }

            if (loglevel == "DEBUG")
            {
                LogLevel = RobotRaconteur_LogLevel.Debug;
                return RobotRaconteur_LogLevel.Debug;
            }

            if (loglevel == "TRACE")
            {
                LogLevel = RobotRaconteur_LogLevel.Trace;
                return RobotRaconteur_LogLevel.Trace;
            }

            return LogLevel;
        }

        public RobotRaconteur_LogLevel SetLogLevelFromEnvVariable(string env_variable_name = "ROBOTRACONTEUR_LOG_LEVEL")
        {
            var loglevel = System.Environment.GetEnvironmentVariable(env_variable_name);
            if (loglevel != null)
            {
                SetLogLevelFromString(loglevel);
            }
            return LogLevel;
        }



        public void SetLogRecordHandler(ILogRecordHandler handler)
        {
            m_LogRecordHandler = handler;
        }

        public ILogRecordHandler GetLogRecordHandler()
        {
            return m_LogRecordHandler;
        }


    }

    public enum RobotRaconteurObjectLockFlags
    {
        USER_LOCK = 0,
        CLIENT_LOCK
    }
}
