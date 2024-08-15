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
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    The central node implementation
    </summary>
    <remarks>
    <para>
    RobotRaconteurNode implements the current Robot Raconteur instance
    and acts as the central switchpoint for the instance. The user
    registers types, connects clients, registers services, and
    registers transports through this class.
    </para>
    <para>
    If the current program only needs one instance of RobotRaconteurNode,
    the singleton can be used. The singleton is accessed using:
    </para>
    <code>
    RobotRaconteurNode.s
    </code>
    <para>
    The node must be shut down before existing the program,
    or a memory leak/hard crash will occur. This can either be
    accomplished manually using the `Shutdown()` function,
    or automatically by using the ClientNodeSetup or
    ServerNodeSetup classes.
    </para>
    </remarks>
    */
    [PublicApi]
    public class RobotRaconteurNode
    {

        private static RobotRaconteurNode sp;
        /**
        <summary>
        Singleton accessor
        </summary>
        <remarks>
        The RobotRaconteurNode singleton can be used when only
        one instance of Robot Raconteur is required in a program.
        The singleton must be shut down when the program exits.
        </remarks>
        */
        [PublicApi]
        public static RobotRaconteurNode s
        {
            get
            {
                if (sp == null) sp = new RobotRaconteurNode();
                return sp;
            }
        }

        /**
         * <summary>The current version of RobotRaconteurWeb ins tring format</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public const string Version = "0.18.0";

        private NodeID m_NodeID;
        /**
        <summary>
        Get or set the current NodeID
        </summary>
        <remarks>
        Gets or setthe current NodeID. If one has not been set,
        one will be automatically generated. Cannot be set if a NodeID has been assigned.
        </remarks>
        <value />
        */
        [PublicApi]
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
        /// <summary>
        /// Try to get the NodeID. Do not automatically generate if not previously configured
        /// </summary>
        /// <param name="nodeid">The current NodeID</param>
        /// <returns>true if NodeID has been configured, otherwise false</returns>
        [PublicApi]
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
        /**
        <summary>
        Get or set the current NodeName
        </summary>
        <remarks>
        Gets or set the current NodeName. If one has not been set using,
        it will be an empty string. Cannot be set if a NodeName has been assigned.
        </remarks>
        */
        [PublicApi]
        public string NodeName
        {
            get
            {
                if (m_NodeName == null)
                {
                    m_NodeName = "";
                    LogInfo(string.Format("RobotRaconteurNode NodeName configured with {0}", m_NodeName), this, component: RobotRaconteur_LogComponent.Node);
                }
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

                    LogInfo(string.Format("RobotRaconteurNode NodeName configured with {0}", m_NodeName), this, component: RobotRaconteur_LogComponent.Node);

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

        /// <summary>
        /// Try to get the NodeName. Do not automatically generate if not previously configured
        /// </summary>
        /// <param name="nodename">The current NodeName</param>
        /// <returns>true if NodeID has been configured, otherwise false</returns>
        [PublicApi]
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

        /// <summary>
        /// Get the dynamic service factory if set
        /// </summary>
        [PublicApi]
        public DynamicServiceFactory DynamicServiceFactory { get { return dynamic_factory; } }

        internal Dictionary<string, ServerContext> services = new Dictionary<string, ServerContext>();


        private uint transport_count = 0;

        /**
        <summary>
        Get or set the timeout for endpoint activity in milliseconds
        </summary>
        <remarks>
        Sets a timeout for endpoint inactivity. If no message
        is sent or received by the endpoint for the specified time,
        the endpoint is closed. Default timeout is 10 minutes.
        </remarks>
        */
        [PublicApi]
        public uint EndpointInactivityTimeout = 600000;
        /**
        <summary>
        Get or set the timeout for transport activity in milliseconds
        </summary>
        <remarks>
        Sets a timeout for transport inactivity. If no message
        is sent or received on the transport for the specified time,
        the transport is closed. Default timeout is 10 minutes.
        </remarks>
        */
        [PublicApi]
        public uint TransportInactivityTimeout = 600000;
        /**
        <summary>
        Get or set the timeout for requests in milliseconds
        </summary>
        <remarks>
        Requests are calls to a remote node that expect a response. `function`,
        `property`, `callback`, `memory`, and setup calls in `pipe` and `wire`
        are all requests. All other Robot Raconteur functions that call the remote
        node and expect a response are requests. Default timeout is 15 seconds.
        </remarks>
        */
        [PublicApi]
        public uint RequestTimeout = 15000;
        private ServiceIndexer serviceindexer;

        /**
        <summary>
        Get or set the maximum chunk size for memory transfers in bytes
        </summary>
        <remarks>
        `memory` members break up large transfers into chunks to avoid
        sending messages larger than the transport maximum, which is normally
        approximately 10 MB. The memory max transfer size is the largest
        data chunk the memory will send, in bytes. Default is 100 kB.
        </remarks>
        */
        [PublicApi]
        public uint MemoryMaxTransferSize = 102400;

#if ROBOTRACONTEUR_H5
        public readonly BrowserWebSocketTransport browser_transport;
#endif
        /// <summary>
        /// Construct a new RobotRaconteurNode instance
        /// </summary>
        [PublicApi]
        public RobotRaconteurNode()
        {
            serviceindexer = new ServiceIndexer(this);
            RegisterServiceType(new RobotRaconteurServiceIndex.RobotRaconteurServiceIndexFactory(this));
            RegisterService("RobotRaconteurServiceIndex", "RobotRaconteurServiceIndex", serviceindexer);
#if ROBOTRACONTEUR_H5
            browser_transport = new BrowserWebSocketTransport(this);
            RegisterTransport(browser_transport);
#endif
            m_Discovery = new Discovery(this);
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

#pragma warning disable 1591
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
            return GetServiceFactoryForType(((MessageElementNestedElementList)l).TypeName, context).UnpackPod(l);
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
            return GetServiceFactoryForType(((MessageElementNestedElementList)l).TypeName, context).UnpackNamedArray(l);
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
            Type t = typeof(T);

            if (t == typeof(object))
            {
                return MessageElementUtil.NewMessageElement(num, PackVarType((object)data, context));
            }

            bool is_array = t.IsArray;
            if (!(t.IsValueType || !EqualityComparer<T>.Default.Equals(data, default(T))))
            {
                return MessageElementUtil.NewMessageElement(num, null);
            }

            if (t.IsPrimitive)
            {
                return MessageElementUtil.NewMessageElement(num, new T[] { data });
            }

            if (is_array && t.GetElementType().IsPrimitive)
            {
                return MessageElementUtil.NewMessageElement(num, data);
            }

            if (t == typeof(string))
            {
                return MessageElementUtil.NewMessageElement(num, data);
            }

            if (t == typeof(CDouble) || t == typeof(CSingle))
            {
                return MessageElementUtil.NewMessageElement(num, data);
            }

            if (is_array)
            {
                var t2 = t.GetElementType();
                if (t2 == typeof(CDouble) || t2 == typeof(CSingle))
                {
                    return MessageElementUtil.NewMessageElement(num, data);
                }
            }

            if (t == typeof(MultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(num, PackMultiDimArray((MultiDimArray)(object)data));
            }

            if (t == typeof(PodMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(num, PackPod((object)data, context));
            }

            if (t == typeof(NamedMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(num, PackNamedArray((object)data, context));
            }

            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var method = typeof(RobotRaconteurNode).GetMethod("PackMapType");
                    var dict_params = t.GetGenericArguments();
                    var generic = method.MakeGenericMethod(dict_params);
                    var packed_map = generic.Invoke(this, new object[] { data, context });
                    return MessageElementUtil.NewMessageElement(num, packed_map);
                }
                if (t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var method = typeof(RobotRaconteurNode).GetMethod("PackListType");
                    var list_params = t.GetGenericArguments();
                    var generic = method.MakeGenericMethod(list_params);
                    var packed_list = generic.Invoke(this, new object[] { data, context });
                    return MessageElementUtil.NewMessageElement(num, packed_list);
                }
                throw new DataTypeException("Invalid Robot Raconteur container value type");
            }

            if (!t.IsValueType && !is_array && t != typeof(PodMultiDimArray) && t != typeof(NamedMultiDimArray))
            {
                return MessageElementUtil.NewMessageElement(num, PackStructure(data, context));
            }
            else
            {
                Type t2 = t;
                if (t.IsArray) t2 = t.GetElementType();
                if (t2.GetCustomAttributes(typeof(RobotRaconteurNamedArrayElementTypeAndCount), false).Length > 0)
                {
                    return MessageElementUtil.NewMessageElement(num, PackNamedArray(data, context));
                }
                else
                {
                    return MessageElementUtil.NewMessageElement(num, PackPod(data, context));
                }
            }
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

            await c.SendMessage(m, cancel).ConfigureAwait(false);

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

#pragma warning restore 1591

        /**
        <summary>
        Register a service type
        </summary>
        <remarks>None</remarks>
        <param name="f">The service factory implementing the type to register</param>
        */
        [PublicApi]
        public void RegisterServiceType(ServiceFactory f)
        {
            lock (service_factories)
            {
                service_factories.Add(f.GetServiceName(), f);
            }
#if RR_LOG_TRACE
            LogTrace(string.Format("Service type registered {0}", f.GetServiceName()), this, component: RobotRaconteur_LogComponent.Node);
#endif

        }

        /**
         <summary>
        Returns a previously registered service type
        </summary>
        <remarks>None</remarks>
        <param name="servicetype">The name of the service type to retrieve</param>
        */
        [PublicApi]
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
        /**
        <summary>
       Returns a previously registered service type.
       </summary>
        <remarks>
        Same as GetServiceType() but returns false on failure instead of throwing an exception
        </remarks>
       <remarks>None</remarks>
       <param name="servicetype">The name of the service type to retrieve</param>
        <param name="f">Returns the service factory</param>
       */
        [PublicApi]
        public bool TryGetServiceType(string servicetype, out ServiceFactory f)
        {
            lock (service_factories)
            {
                return service_factories.TryGetValue(servicetype, out f);
            }
        }

        /**
        <summary>
            Return names of registered service types
        </summary>
        <remarks>None</remarks>
        <returns>The registered service types</returns>
        */
        [PublicApi]
        public string[] GetServiceTypes()
        {
            lock (service_factories)
            {
                return service_factories.Keys.ToArray();
            }
        }

        /// <summary>
        /// Register a dynamic service factory.
        /// </summary>
        /// <remarks>Dynamic service factories are used by clients to generate service factories to
        /// implement plug-and-play typing</remarks>
        /// <param name="f">The dynamic service factory</param>
        [PublicApi]
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
        /**
        <summary>
        Registers a service for clients to connect
        </summary>
        <remarks>
        <para>
        The supplied object becomes the root object in the service. Other objects may
        be accessed by clients using `objref` members. The name of the service must conform
        to the naming rules of Robot Raconteur member names. A service is closed using
        either CloseService() or when Shutdown() is called.
        </para>
        <para>
        Multiple services can be registered within the same node. Service names
        within a single node must be unique.
        </para>
        </remarks>
        <param name="name">The name of the service, must follow member naming rules</param>
        <param name="servicetype">The name of the service definition containing the object type.
        Do not include the object type.</param>
        <param name="obj">The root object of the service</param>
        <param name="securitypolicy">An optional security policy for the service to control authentication
        and other security functions</param>
        <returns>The instantiated ServerContext. This object is owned
        by the node and the return can be safely ignored.</returns>
        */
        [PublicApi]
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

        /**
        <summary>Register a service using a ServerContext instance</summary>
        <remarks>None</remarks>
        <param name="c">The ServerContext instance to register</param>
        */
        [PublicApi]
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
        /**
        <summary>
            Closes a previously registered service
        </summary>
        <remarks>
            Services are automatically closed by Shutdown, so this function
            is rarely used.
        </remarks>
        <param name="sname">The name of the service to close</param>
        */
        [PublicApi]
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

        /// <summary>
        /// Return a previously registered service
        /// </summary>
        /// <param name="name">The name of the service</param>
        /// <returns>The context of the service</returns>
        [PublicApi]
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
        /**
        <summary>
            Register a transport for use by the node
        </summary>
        <remarks>None</remarks>
        <param name="c">The transport to register</param>
        <returns>The transport internal id</returns>
        */
        [PublicApi]
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

#pragma warning disable 1591
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

            if (m.header.ReceiverEndpoint != 0 && m.entries.Count == 1 &&
            m.entries[0].EntryType == MessageEntryType.ObjectTypeName)
            {
                // Workaround for security of getting object types
                MessageReceived(m);
                return null;
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

                            RobotRaconteurVersion v = default;
                            if (e.TryFindElement("clientversion", out var m_ver))
                            {
                                v = new RobotRaconteurVersion();
                                v.FromString(m_ver.CastDataToString());
                            }

                            try
                            {
                                ServerContext s;

                                s = GetService(s1[0]);

                                var objtype = await s.GetObjectType(path, v);
                                eret.AddElement("objecttype", objtype);

                                var objtype_s = ServiceDefinitionUtil.SplitQualifiedName(objtype);

                                if (!GetServiceType(objtype_s.Item1).ServiceDef().Objects.TryGetValue(objtype_s.Item2, out var def))
                                    throw new ServiceException("Invalid service object");

                                if (def.Implements.Any())
                                {
                                    var implements = def.Implements.ToList();
                                    eret.AddElement("objectimplements", PackListType<string>(implements, null));
                                }
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
                                    var service = GetService(name);
                                    servicedef = service.ServiceDef.DefString();
                                    eret.AddElement("attributes", PackMapType<string, object>(GetService(name).Attributes, null));
                                    var extra_imports = service.ExtraImports.ToList();
                                    if (extra_imports.Count > 0)
                                    {
                                        eret.AddElement("extra_imports", PackListType<string>(extra_imports, null));
                                    }
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
                    case MessageEntryType.ServiceClosed:
                    case MessageEntryType.ServiceClosedRet:
                        return null;
                    case MessageEntryType.ConnectClientCombined:
                        {
                            var name = e.ServicePath;

                            ServerContext c;

                            var v = new RobotRaconteurVersion();

                            if (e.TryFindElement("clientversion", out var m_ver))
                            {
                                v.FromString(m_ver.CastDataToString());
                            }

                            try
                            {
                                c = GetService(name);
                                var objtype = await c.GetRootObjectType(v);
                                eret.AddElement("objecttype", objtype);

                                var objtype_s = ServiceDefinitionUtil.SplitQualifiedName(objtype);

                                if (!GetServiceType(objtype_s.Item1).ServiceDef().Objects.TryGetValue(objtype_s.Item2, out var def))
                                {
                                    throw new ServiceException("Invalid service object");
                                }

                                if (def.Implements.Any())
                                {
                                    var implements = def.Implements;
                                    eret.AddElement("objectimplements", PackListType<string>(implements, null));
                                }
                            }
                            catch (Exception exp)
                            {
                                RRLogFuncs.LogDebug(string.Format("Error connecting client: {0}", exp.Message), this, RobotRaconteur_LogComponent.Node,
                                            endpoint: m.header.ReceiverEndpoint, service_path: e.ServicePath,
                                                                        member: e.MemberName);
                                eret.elements.Clear();
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceNotFound;
                                break;
                            }

                            try
                            {
                                bool returnservicedef = true;

                                try
                                {

                                    if (e.TryFindElement("returnservicedefs", out var returnservicedefs_el))
                                    {
                                        var returnservicedef_str = returnservicedefs_el.CastDataToString();
                                        returnservicedef_str = returnservicedef_str.Trim();
                                        if (returnservicedef_str.ToLower() == "false" || returnservicedef_str == "0")
                                        {
                                            returnservicedef = false;
                                        }
                                    }
                                }
                                catch (Exception)
                                { }

                                if (returnservicedef)
                                {
                                    if (c == null)
                                    {
                                        throw new ServiceException("Service not found");
                                    }
                                    var servicedef1 = await c.GetRootObjectServiceDef(v);
                                    var defs = new Dictionary<string, ServiceFactory>();
                                    defs.Add(servicedef1.GetServiceName(), servicedef1);

                                    var extra_imports = c.ExtraImports;
                                    foreach (var e3 in extra_imports)
                                    {
                                        if (!defs.ContainsKey(e3))
                                        {
                                            defs.Add(e3, GetServiceType(e3));
                                        }
                                    }

                                    while (true)
                                    {
                                        bool new_found = false;

                                        foreach (var e3 in defs.Keys.ToArray())
                                        {
                                            var d1 = defs[e3];
                                            foreach (string e2 in d1.ServiceDef().Imports)
                                            {
                                                if (!defs.ContainsKey(e2))
                                                {
                                                    var d2 = GetServiceType(e2);
                                                    defs.Add(d2.GetServiceName(), d2);
                                                    new_found = true;
                                                }
                                            }
                                        }

                                        if (!new_found)
                                            break;
                                    }

                                    uint n = 0;

                                    var servicedef_list = new List<MessageElement>();
                                    foreach (var d in defs.Values)
                                    {
                                        var e1 = new MessageElement((int)n, d.DefString());
                                        servicedef_list.Add(e1);
                                        n++;
                                    }

                                    eret.AddElement("servicedefs", new MessageElementNestedElementList(DataTypes.list_t, "",
                                                                                                          servicedef_list));
                                }
                            }
                            catch (Exception exp)
                            {
                                RRLogFuncs.LogDebug(string.Format("Error connecting client: {0}", exp.Message), this, RobotRaconteur_LogComponent.Node,
                                            endpoint: m.header.ReceiverEndpoint, service_path: e.ServicePath,
                                                                        member: e.MemberName);
                                eret.elements.Clear();
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service factory configuraiton error");
                                eret.Error = MessageErrorType.ServiceNotFound;
                                break;
                            }

                            ServerEndpoint se = null;

                            try
                            {
                                if (c == null)
                                {
                                    throw new ServiceException("Service not found");
                                }

                                se = new ServerEndpoint(c, this);

                                se.m_RemoteEndpoint = m.header.SenderEndpoint;
                                se.m_RemoteNodeID = m.header.SenderNodeID;
                                RegisterEndpoint(se);

                                se.transport = transportid;

                                c.AddClient(se);

                                ret.header.SenderEndpoint = se.LocalEndpoint;

                            }
                            catch (Exception exp)
                            {
                                if (se != null)
                                {
                                    try
                                    {
                                        DeleteEndpoint(se);
                                    }
                                    catch
                                    { }
                                }
                                RRLogFuncs.LogDebug(string.Format("Error connecting client: {0}", exp.Message), this, RobotRaconteur_LogComponent.Node,
                                        endpoint: m.header.ReceiverEndpoint, service_path: e.ServicePath,
                                                                    member: e.MemberName);
                                eret.elements.Clear();
                                eret.AddElement("errorname", "RobotRaconteur.ServiceNotFoundException");
                                eret.AddElement("errorstring", "Service not found");
                                eret.Error = MessageErrorType.ServiceNotFound;
                                break;


                            }

                            try
                            {
                                if (c == null)
                                {
                                    throw new ServiceException("Service not found");
                                }
                                if (c.RequireValidUser)
                                {
                                    if (!e.TryFindElement("username", out var username_el))
                                    {
                                        throw new AuthenticationException("Username not provided");
                                    }

                                    if (!e.TryFindElement("credentials", out var credentials_el))
                                    {
                                        throw new AuthenticationException("Credentials not provided");
                                    }

                                    var credentials =
                                        (Dictionary<string, object>)(UnpackMapType<string, object>(
                                            credentials_el.CastDataToNestedList(DataTypes.dictionary_t), null));
                                    if (credentials == null)
                                    {
                                        throw new AuthenticationException("Credentials cannot be null");
                                    }
                                    var username = username_el.CastDataToString();

                                    se.AuthenticateUser(username, credentials);
                                }
                                else
                                {

                                    if (e.TryFindElement("username", out var username_el) && e.TryFindElement("credentials", out var credentials_el))
                                    {
                                        throw new AuthenticationException("Authentication not enabled for service");
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                RRLogFuncs.LogDebug(string.Format("Error authenticating client: {0}", exp.Message), this,
                                    RobotRaconteur_LogComponent.Node, endpoint: m.header.ReceiverEndpoint, service_path: e.ServicePath,
                                                                        member: e.MemberName);
                                try
                                {
                                    if (c != null && se != null)
                                    {
                                        c.RemoveClient(se);
                                        DeleteEndpoint(se);
                                    }
                                }
                                catch (Exception)
                                { }

                                eret.elements.Clear();
                                eret.AddElement("errorname", "RobotRaconteur.AuthenticationError");
                                eret.AddElement("errorstring", ("Authentication Failed"));
                                eret.Error = MessageErrorType.AuthenticationError;
                                break;
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
#pragma warning restore 1591
        /**
        <summary>
        Create a client connection to a remote service using a URL
        </summary>
        <remarks>
        <para>
        Creates a connection to a remote service using a URL. URLs are either provided by
        the service, or are determined using discovery functions such as FindServiceByType().
        This function is the primary way to create client connections.
        </para>
        <para>
        username and credentials can be used to specify authentication information. Credentials will
        often contain a "password" or token entry.
        </para>
        <para>
        The listener is a function that is called during various events. See ClientServiceListenerEventType
        for a description of the possible events.
        </para>
        <para>
        ConnectService will attempt to instantiate a client object reference (proxy) based on the type
        information provided by the service. The type information will contain the type of the object,
        and all the implemented types. The client will normally want a specific one of the implement types.
        Specify this desired type in objecttype to avoid future compatibility issues.
        </para>
        </remarks>
        <param name="url">The URL of the service to connect</param>
        <param name="username">An optional username for authentication</param>
        <param name="credentials">Optional credentials for authentication</param>
        <param name="listener">An optional listener callback function</param>
        <param name="objecttype">The desired root object proxy type. Optional but highly recommended.</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The root object reference of the connected service</returns>
        */
        [PublicApi]
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

                            object r = await c.ConnectService(end, url, username, credentials, objecttype, cancel).ConfigureAwait(false);

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
        /**
        <summary>
        Create a client connection to a remote service using a URL
        </summary>
        <remarks>
        <para>
        Creates a connection to a remote service using a URL. URLs are either provided by
        the service, or are determined using discovery functions such as FindServiceByType().
        This function is the primary way to create client connections.
        </para>
        <para>
        username and credentials can be used to specify authentication information. Credentials will
        often contain a "password" or token entry.
        </para>
        <para>
        The listener is a function that is called during various events. See ClientServiceListenerEventType
        for a description of the possible events.
        </para>
        <para>
        ConnectService will attempt to instantiate a client object reference (proxy) based on the type
        information provided by the service. The type information will contain the type of the object,
        and all the implemented types. The client will normally want a specific one of the implement types.
        Specify this desired type in objecttype to avoid future compatibility issues.
        </para>
        </remarks>
        <param name="url">The candidate URLs of the service to connect</param>
        <param name="username">An optional username for authentication</param>
        <param name="credentials">Optional credentials for authentication</param>
        <param name="listener">An optional listener callback function</param>
        <param name="objecttype">The desired root object proxy type. Optional but highly recommended.</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The root object reference of the connected service</returns>
        */
        [PublicApi]
        public async Task<object> ConnectService(string[] url, string username = null, object credentials = null, ClientContext.ClientServiceListenerDelegate listener = null, string objecttype = null, CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                var connecting_tasks = new List<Task<object>>();
                Exception connecting_exp = null;

                foreach (var u in url)
                {
                    try
                    {
                        connecting_tasks.Add(ConnectService(u, username, credentials, null, objecttype, cancel));
                    }
                    catch (Exception e)
                    {
                        if (connecting_exp == null)
                        {
                            connecting_exp = e;
                        }
                    }
                }

                if (connecting_tasks.Count == 0)
                {
                    if (connecting_exp != null)
                    {
                        throw connecting_exp;
                    }
                    throw new ConnectionException("Could not connect to service");
                }

                while (true)
                {
                    object r = null;
                    if (connecting_tasks.Count == 1)
                    {
                        r = await connecting_tasks[0].ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.WhenAny(connecting_tasks).ConfigureAwait(false);

                        var completed_task = connecting_tasks.First(x => x.IsCompleted);
                        connecting_tasks.Remove(completed_task);
                        if (completed_task.Status == TaskStatus.RanToCompletion)
                        {
                            r = await completed_task.ConfigureAwait(false);
                            foreach (var t in connecting_tasks)
                            {
                                try
                                {
                                    _ = t.ContinueWith((x) =>
                                    {
                                        try
                                        {
                                            Task.Run(delegate ()
                                            {
                                                try
                                                {
                                                    ((ServiceStub)x.Result).RRContext.Close().IgnoreResult();
                                                }
                                                catch { }
                                            });
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
        /**
        <summary>
        Disconnects a client connection to a service
        </summary>
        <remarks>
        <para>
        Disconnects a client connection. Client connections
        are automatically closed by Shutdown(), so this function
        is optional.
        </para>
        </remarks>
        <param name="obj">The root object of the service to disconnect</param>
        <param name="cancel">The cancellation token for the operation</param>
        */
        [PublicApi]
        public async Task DisconnectService(object obj, CancellationToken cancel = default(CancellationToken))
        {
            ServiceStub stub = (ServiceStub)obj;
            if (stub != null)
            {
                await stub.RRContext.Close(cancel).ConfigureAwait(false);
            }

        }
        /**
        <summary>
        Get the service attributes of a client connection
        </summary>
        <remarks>
        Returns the service attributes of a client connected using
        ConnectService()
        </remarks>
        <param name="obj">The root object of the client to use to retrieve service attributes</param>
        <returns>Dictionary of the service attributes</returns>
        */
        [PublicApi]
        public Dictionary<string, object> GetServiceAttributes(object obj)
        {
            ServiceStub stub = (ServiceStub)obj;
            return stub.RRContext.Attributes;
        }
#pragma warning disable 1591
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
#pragma warning restore 1591
        /**
        <summary>
            Check that the TransportConnection associated with an endpoint
            is connected
        </summary>
        <remarks>None</remarks>
        <param name="endpoint">The LocalEndpoint identifier to check</param>
        */
        [PublicApi]
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
        /**
        <summary>
        Shuts down the node. Called automatically by ClientNodeSetup and ServerNodeSetup
        </summary>
        <remarks>
        <para>
        Shutdown must be called before program exit to avoid segfaults and other undefined
        behavior. The use of ClientNodeSetup and ServerNodeSetup is recommended to automate
        the node lifecycle. Calling this function does the following:
        </para>
        <list type="number">
        <item>1. Closes all services and releases all service objects</item>
        <item>2. Closes all client connections</item>
        <item>3. Shuts down discovery</item>
        <item>4. Shuts down all transports</item>
        <item>5. Notifies all shutdown listeners</item>
        <item>6. Releases all periodic cleanup task listeners</item>
        <item>7. Shuts down and releases the thread pool</item>
        </list>
        </remarks>
        */
        [PublicApi]
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


        /// <summary>
        /// Returns the currently detected nodes from discovery
        /// </summary>
        /// <remarks>This is raw information from listening to multicast packtes.
        /// These nodes are not validated and may not be reachable</remarks>
        /// <value></value>
        [PublicApi]
        public Dictionary<string, NodeDiscoveryInfo> DiscoveredNodes { get { return m_Discovery.DiscoveredNodes; } }

        internal Discovery m_Discovery;
#pragma warning disable 1591
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
#pragma warning restore 1591
        /**
        <summary>
            Select the "best" URL from a std::vector of candidates
        </summary>
        <remarks>
            <para>
            Service discovery will often return a list of candidate URLs to
            use to connect to a node. This function uses hueristics to select
            the "best" URL to use. The selection criteria ranks URLs in roughly
            the following order, lower number being better:
            </para>
            <list type="number">
            <term>"rr+intra" for IntraTransport</term>
            <term>"rr+local" for LocalTransport</term>
            <term>"rr+pci" or "rr+usb" for HardwareTransport</term>
            <term>"rrs+tcp://127.0.0.1" for secure TcpTransport loopback</term>
            <term>"rrs+tcp://[::1]" for secure TcpTransport IPv6 loopback</term>
            <term>"rrs+tcp://localhost" for secure TcpTransport loopback</term>
            <term>"rrs+tcp://[fe80" for secure TcpTransport link-local IPv6</term>
            <term>"rrs+tcp://" for any secure TcpTransport</term>
            <term>"rr+tcp://127.0.0.1" for TcpTransport loopback</term>
            <term>"rr+tcp://[::1]" for TcpTransport IPv6 loopback</term>
            <term>"rr+tcp://localhost" for TcpTransport loopback</term>
            <term>"rr+tcp://[fe80" for TcpTransport link-local IPv6</term>
            <term>"rr+tcp://" for any TcpTransport</term>
            </list>
        </remarks>
        <param name="urls">The candidate URLs</param>
        */
        [PublicApi]
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
            await m_Discovery.UpdateDetectedNodes(cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Use discovery to find available services by service type
        </summary>
        <remarks>
        <para>
        Uses discovery to find available services based on a service type. This
        service type is the type of the root object, ie
        `com.robotraconteur.robotics.robot.Robot`. This process will update the detected
        node cache.
        </para>
        </remarks>
        <param name="servicetype">The service type to find, ie `com.robotraconteur.robotics.robot.Robot`</param>
        <param name="transportschemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        etc</param>
        <returns>The detected services</returns>
        */
        [PublicApi]
        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes)
        {
            return await m_Discovery.FindServiceByType(servicetype, transportschemes).ConfigureAwait(false);
        }
        /**
        <summary>
        Use discovery to find available services by service type
        </summary>
        <remarks>
        <para>
        Uses discovery to find available services based on a service type. This
        service type is the type of the root object, ie
        `com.robotraconteur.robotics.robot.Robot`. This process will update the detected
        node cache.
        </para>
        </remarks>
        <param name="servicetype">The service type to find, ie `com.robotraconteur.robotics.robot.Robot`</param>
        <param name="transportschemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        etc</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The detected services</returns>
        */
        [PublicApi]
        public async Task<ServiceInfo2[]> FindServiceByType(string servicetype, string[] transportschemes, CancellationToken cancel)
        {

            return await m_Discovery.FindServiceByType(servicetype, transportschemes, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Finds nodes on the network with a specified NodeID
        </summary>
        <remarks>
        <para>
        Updates the discovery cache and find nodes with the specified NodeID.
        This function returns unverified cache information.
        </para>
        </remarks>
        <param name="id">The NodeID to find</param>
        <param name="schemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        etc</param> <returns>The detected nodes</returns>
        */
        [PublicApi]
        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes)
        {
            return await m_Discovery.FindNodeByID(id, schemes).ConfigureAwait(false);
        }
        /**
        <summary>
        Finds nodes on the network with a specified NodeID
        </summary>
        <remarks>
        <para>
        Updates the discovery cache and find nodes with the specified NodeID.
        This function returns unverified cache information.
        </para>
        </remarks>
        <param name="id">The NodeID to find</param>
        <param name="schemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        <param name="cancel">The cancellation token for the operation</param>
        etc</param> <returns>The detected nodes</returns>
        */
        [PublicApi]
        public async Task<NodeInfo2[]> FindNodeByID(NodeID id, string[] schemes, CancellationToken cancel)
        {
            return await m_Discovery.FindNodeByID(id, schemes, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Finds nodes on the network with a specified NodeName
        </summary>
        <remarks>
        <para>
        Updates the discovery cache and find nodes with the specified NodeName.
        This function returns unverified cache information.
        </para>
        </remarks>
        <param name="name">The NodeName to find</param>
        <param name="schemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        etc</param>
        <returns>The detected nodes</returns>
        */
        [PublicApi]
        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes)
        {
            return await m_Discovery.FindNodeByName(name, schemes).ConfigureAwait(false);
        }
        /**
        <summary>
        Finds nodes on the network with a specified NodeName
        </summary>
        <remarks>
        <para>
        Updates the discovery cache and find nodes with the specified NodeName.
        This function returns unverified cache information.
        </para>
        </remarks>
        <param name="name">The NodeName to find</param>
        <param name="schemes">A list of transport types to search, ie `rr+tcp`, `rr+local`, `rrs+tcp`,
        etc</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The detected nodes</returns>
        */
        [PublicApi]
        public async Task<NodeInfo2[]> FindNodeByName(string name, string[] schemes, CancellationToken cancel)
        {
            return await m_Discovery.FindNodeByName(name, schemes, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Request an exclusive access lock to a service object
        </summary>
        <remarks>
        <para>
        Called by clients to request an exclusive lock on a service object and
        all subobjects (`objrefs`) in the service. The exclusive access lock will
        prevent other users ("User" lock) or client connections  ("Session" lock)
        from interacting with the objects.
        </para>
        </remarks>
        <param name="obj">The object to lock. Must be returned by ConnectService or returned by an `objref`</param>
        <param name="flags">Select either a "User" or "Session" lock</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>"OK" on success</returns>
        */
        [PublicApi]
        public async Task<string> RequestObjectLock(object obj, RobotRaconteurObjectLockFlags flags, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only lock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.RequestObjectLock(obj, flags, cancel).ConfigureAwait(false);


        }

        /**
        <summary>
        Release an excluse access lock previously locked with RequestObjectLock()
        </summary>
        <remarks>
        <para>
        Object must have previously been locked using RequestObjectLock()
        </para>
        </remarks>
        <param name="obj">The object previously locked</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>"OK" on success</returns>
        */
        [PublicApi]
        public async Task<string> ReleaseObjectLock(object obj, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Can only unlock object opened through Robot Raconteur");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.ReleaseObjectLock(obj, cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle to a monitor lock
        /// </summary>
        [PublicApi]
        public class MonitorLock
        {
            internal IDisposable lock_;
            internal ServiceStub stub;
        }
        /**
        <summary>
        Creates a monitor lock on a specified object
        </summary>
        <remarks>
        <para>
        Monitor locks are intendended for short operations that require
        guarding to prevent races, corruption, or other concurrency problems.
        Monitors emulate a single thread locking the service object.
        </para>
        <para>
        Monitor locks do not lock any sub-objects (objref)
        </para>
        </remarks>
        <param name="obj">The object to lock</param>
        <param name="timeout">The timeout in milliseconds to acquire the monitor lock, or RR_TIMEOUT_INFINITE</param>
        <param name="cancel">The cancellation token for the operation</param>
        */
        [PublicApi]
        public async Task<MonitorLock> MonitorEnter(object obj, int timeout = -1, CancellationToken cancel = default(CancellationToken))
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can be monitored by RobotRaconteurNode");
            ServiceStub s = (ServiceStub)obj;

            return await s.RRContext.MonitorEnter(obj, timeout, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Releases a monitor lock
        </summary>
        <remarks>None
        </remarks>
        <param name="lock_">The object previously locked by MonitorEnter()</param>
        <param name="cancel">The cancellation token for the operation</param>
        */
        [PublicApi]
        public async Task MonitorExit(RobotRaconteurNode.MonitorLock lock_, CancellationToken cancel = default(CancellationToken))
        {

            await lock_.stub.RRContext.MonitorExit(lock_, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Returns an objref as a specific type
        </summary>
        <remarks>
        <para>
        Robot Raconteur service object types are polymorphic using inheritence,
        meaning that an object may be represented using multiple object types.
        `objref` will attempt to return the relevant type, but it is sometimes
        necessary to request a specific type for an objref.
        </para>
        <para>
        This function will return the object from an `objref` as the specified type,
        or throw an error if the type is invalid.
        </para>
        </remarks>
        <param name="obj">The object with the desired `objref`</param>
        <param name="objref">The name of the `objref` member</param>
        <param name="objecttype">The desired service object type</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The object with the specified interface type. Must be cast to the desired type</returns>
        */
        [PublicApi]
        public async Task<object> FindObjRefTyped(object obj, string objref, string objecttype, CancellationToken cancel)
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can have objrefs");
            ServiceStub s = (ServiceStub)obj;

            return await s.FindObjRefTyped(objref, objecttype, cancel).ConfigureAwait(false);
        }
        /**
        <summary>
        Returns an indexed objref as a specified type
        </summary>
        <remarks>
        <para>
        Same as FindObjRefTyped() but includes an `objref` index
        </para>
        </remarks>
        <param name="obj">The object with the desired `objref`</param>
        <param name="objref">The name of the `objref` member</param>
        <param name="index">The index for the `objref`, convert int to string for int32 index type</param>
        <param name="objecttype">The desired service object type</param>
        <param name="cancel">The cancellation token for the operation</param>
        <returns>The object with the specified interface type. Must be cast to the desired type</returns>
        */
        [PublicApi]
        public async Task<object> FindObjRefTyped(object obj, string objref, string index, string objecttype, CancellationToken cancel)
        {
            if (!(obj is ServiceStub)) throw new InvalidOperationException("Only service stubs can have objrefs");
            ServiceStub s = (ServiceStub)obj;

            return await s.FindObjRefTyped(objref, index, objecttype, cancel).ConfigureAwait(false);
        }

        /**
        <summary>
        The current time in UTC time zone
        </summary>
        <remarks>
        Uses the internal node clock to get the current time in UTC.
        While this will normally use the system clock, this may
        use simulation time in certain circumstances
        </remarks>
        */
        [PublicApi]
        public DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }
        /**
        <summary>
        Create a Timer object
        </summary>
        <remarks>
        <para>
        This function will normally return a WallTimer instance
        </para>
        <para>
        Start() must be called after timer creation
        </para>
        </remarks>
        <param name="period">The period of the timer in milliseconds</param>
        <param name="handler">The handler function to call when timer times out</param>
        <param name="oneshot">True if timer is a one-shot timer, false for repeated timer</param>
        <returns>The new Timer object. Must call Start()</returns>
        */
        [PublicApi]
        public ITimer CreateTimer(int period, Action<TimerEvent> handler, bool oneshot = false)
        {
            var t = new WallTimer(period, handler, oneshot, this);
            return t;
        }
        /**
        <summary>
            Create a Rate object
        </summary>
        <remarks>
            <para>
            Rate is used to stabilize periodic loops to a specified frequency
            </para>
            <para> This function will normally return a WallRate instance
            </para>
        </remarks>
        <param name="frequency">Frequency of loop in Hz</param>
        <returns>The new Rate object</returns>
        */
        [PublicApi]
        public IRate CreateRate(double frequency)
        {
            return new WallRate(frequency, this);
        }

        /// <summary>
        /// Return a random alphanumeric string of the specified length
        /// </summary>
        /// <param name="count">Length of string</param>
        /// <returns>The random string</returns>
        [PublicApi]
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

#pragma warning disable 1591
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
#pragma warning restore 1591
        /**
        <summary>
        Subscribe to listen for available services information
        </summary>
        <remarks>
        A ServiceInfo2Subscription will track the availability of service types and
        inform when services become available or are lost. If connections to
        available services are also required, ServiceSubscription should be used.
        </remarks>
        <param name="service_types">An array of service types to listen for, ie
        `com.robotraconteur.robotics.robot.Robot`</param>
        <param name="filter">A filter to select individual services based on specified criteria</param>
        <returns>The active subscription</returns>
        */
        [PublicApi]
        public ServiceInfo2Subscription SubscribeServiceInfo2(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            return m_Discovery.SubscribeServiceInfo2(service_types, filter);
        }
        /**
        <summary>
        Subscribe to listen for available services and automatically connect
        </summary>
        <remarks>
        A ServiceSubscription will track the availability of service types and
        create connections when available.
        </remarks>
        <param name="service_types">An arrayof service types to listen for, ie
        `com.robotraconteur.robotics.robot.Robot`</param>
        <param name="filter">A filter to select individual services based on specified criteria</param>
        <returns>The active subscription</returns>
        */
        [PublicApi]
        public ServiceSubscription SubscribeServiceByType(string[] service_types, ServiceSubscriptionFilter filter = null)
        {
            return m_Discovery.SubscribeServiceByType(service_types, filter);
        }
        /**
        <summary>
        Subscribe to a service using one or more URL. Used to create robust connections to services
        </summary>
        <remarks>
        Creates a ServiceSubscription assigned to a service with one or more candidate connection URLs. The
        subscription will attempt to maintain a persistent connection, reconnecting if the connection is lost.
        </remarks>
        <param name="url">One or more candidate connection urls</param>
        <param name="username">An optional username for authentication</param>
        <param name="credentials">Optional credentials for authentication</param>
        <param name="objecttype">The desired root object proxy type. Optional but highly recommended.</param>
        <returns>The active subscription</returns>
        */
        [PublicApi]
        public ServiceSubscription SubscribeService(string[] url, string username = null, Dictionary<string, object> credentials = null, string objecttype = null)
        {
            return m_Discovery.SubscribeService(url, username, credentials, objecttype);
        }

        ILogRecordHandler m_LogRecordHandler;
        /**
        <summary>
            The current log level for the node
        </summary>
        <remarks>
            Default level is "warning". Set RobotRaconteur.RobotRaconteur_LogLevel_Disable to disable logging
        </remarks>
        */
        [PublicApi]
        public RobotRaconteur_LogLevel LogLevel
        {
            get; set;
        } = RobotRaconteur_LogLevel.Warning;
        /**
        <summary>
            Test if the specified log level would be accepted
        </summary>
        <remarks>None</remarks>
        <param name="level">Log level to test</param>
        <returns>true if the log would be accepted</returns>
        */
        [PublicApi]
        public bool CompareLogLevel(RobotRaconteur_LogLevel level)
        {
            return (int)level >= (int)LogLevel;
        }
        /**
        <summary>
            Log a simple message using the current node
        </summary>
        <remarks>
            <para>
            The record will be sent to the configured log handler,
            or sent to cerr if none is configured
            </para>
            <para> If the level of the message is below the current log level
            for the node, the record will be ignored
            </para>
        </remarks>
        <param name="level">The level for the log message</param>
        <param name="message">The log message</param>
        */
        [PublicApi]
        public void LogMessage(RobotRaconteur_LogLevel level, string message)
        {
            LogRecord(new RRLogRecord() { Node = this, Level = level, Message = message });
        }
        /**
        <summary>
            Log a record to the node.
        </summary>
        <remarks>
            <para>
            The record will be sent to the configured log handler,
            or sent to stderr if none is configured
            </para>
            <para> If the level of the message is below the current log level
            for the node, it will be ignored
            </para>
        </remarks>
        <param name="record">The record to log</param>
        */
        [PublicApi]
        public void LogRecord(RRLogRecord record)
        {
            if ((int)record.Level < (int)LogLevel)
                return;
            if (m_LogRecordHandler != null)
            {
                m_LogRecordHandler.Log(record);
                return;
            }

#if !ROBOTRACONTEUR_H5
            RRLogFuncs.WriteLogRecord(Console.Error, record);
            Console.Error.WriteLine();
#else
            var t = new StringWriter();
            RRLogFuncs.WriteLogRecord(t, record);
            Console.WriteLine(t.ToString());
#endif
        }
        /**
        <summary>
            Set the log level for the node from a string
        </summary>
        <remarks>
            Must be one of the following values: DISABLE, FATAL, ERROR, WARNING, INFO, DEBUG, TRACE
            Defaults to WARNING
        </remarks>
        <param name="loglevel">The desired log level</param>
        */
        [PublicApi]
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
        /**
        <summary>
            Set the log level for the node from specified environmental variable
        </summary>
        <remarks>
            <para>
            Retrieves the specified environmental variable and sets the log level based
            on one of the following values: DISABLE, FATAL, ERROR, WARNING, INFO, DEBUG, TRACE
            </para>
            <para> If an invalid value or the variable does not exist, the log level is left unchanged.
            </para>
        </remarks>
        <param name="env_variable_name">The environmental variable to use. Defaults to
            `ROBOTRACONTEUR_LOG_LEVEL`</param>
        */
        [PublicApi]
        public RobotRaconteur_LogLevel SetLogLevelFromEnvVariable(string env_variable_name = "ROBOTRACONTEUR_LOG_LEVEL")
        {
            var loglevel = System.Environment.GetEnvironmentVariable(env_variable_name);
            if (loglevel != null)
            {
                SetLogLevelFromString(loglevel);
            }
            return LogLevel;
        }


        /**
        <summary>
            Set the handler for log records
        </summary>
        <remarks>
            If handler is NULL, records are sent to stderr
        </remarks>
        <param name="handler">The log record handler function</param>
        */
        [PublicApi]
        public void SetLogRecordHandler(ILogRecordHandler handler)
        {
            m_LogRecordHandler = handler;
        }

        /**
        <summary>
            Get the currently configured log record handler
        </summary>
        <remarks>
            If null, records are sent to stderr
        </remarks>
        */
        [PublicApi]
        public ILogRecordHandler GetLogRecordHandler()
        {
            return m_LogRecordHandler;
        }

        internal NodeDirectories node_dirs;

        /// <summary>
        /// Get or set the NodeDirectories object for the node
        /// </summary>
        /// <remarks>
        /// The NodeDirectories controls where the node searches for local transport connections,
        /// stores node information, searches for configuration information,
        /// and other node specific directories. The NodeDirectories cannot be modified after
        /// it has been configured. A default configuration is used if not set.</remarks>
        /// <value></value>
        [PublicApi]
        public NodeDirectories NodeDirectories
        {
            get
            {
                lock (this)
                {
                    if (node_dirs == null)
                    {
                        node_dirs = NodeDirectoriesUtil.GetDefaultNodeDirectories(this);
                    }
                    return node_dirs;
                }
            }
            set
            {
                if (node_dirs != null)
                {
                    throw new InvalidOperationException("Node directories already set");
                }
                node_dirs = value;
            }
        }


    }

    /**
    <summary>
        The type of object lock
      </summary>
    */
    [PublicApi]
    public enum RobotRaconteurObjectLockFlags
    {
        /**
        <summary>
            User level lock
        </summary>
        <remarks>
            The object will be accesible for all client connections
            authenticated by the current user
        </remarks>
        */
        [PublicApi]
        USER_LOCK = 0,
        /**
        <summary>
            Client level lock
        </summary>
        <remarks>
            Only the current client connection will have access
            to the locked object
        </remarks>
        */
        [PublicApi]
        CLIENT_LOCK
    }

    /// <summary>
    /// Used to mark member as part of the public API
    /// </summary>
    public class PublicApiAttribute : System.Attribute { };
}
