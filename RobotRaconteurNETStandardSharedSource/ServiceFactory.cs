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
using RobotRaconteur.Extensions;

namespace RobotRaconteur
{
    public interface DynamicServiceFactory
    {
        ServiceFactory CreateServiceFactory(string def, ClientContext context);
        ServiceFactory[] CreateServiceFactories(string[] def, ClientContext context);
    }

    public abstract class ServiceFactory
    {
        private ServiceDefinition sdef = null;
        protected readonly RobotRaconteurNode node;
        protected readonly ClientContext context;

        public ServiceFactory(RobotRaconteurNode node = null, ClientContext context = null)
        {
            if (node != null)
            {
                this.node = node;
            }
            else
            {
                this.node = RobotRaconteurNode.s;
            }

            this.context = context;
        }

        public ServiceDefinition ServiceDef()
        {
            if (sdef == null)
            {
                sdef = new ServiceDefinition();
                sdef.FromString(DefString());
            }
            return sdef;
        }

        public string RemovePath(string path)
        {
            return ServiceDefinitionUtil.SplitQualifiedName(path).Item2;
        }

        public bool CompareNamespace(string qualified_typename, out string typename)
        {
            var s = ServiceDefinitionUtil.SplitQualifiedName(qualified_typename);
            typename = s.Item2;
            return s.Item1 == GetServiceName();
        }

        public abstract string DefString();

        public virtual MessageElementStructure PackStructure(object s)
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.GetType()), out typename))
            {
                return FindStructureStub(typename).PackStructure(s);
            }
            else
            {
                return node.PackStructure(s, context);
            }                        
        }

        public virtual T UnpackStructure<T>(MessageElementStructure l)
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return (T)FindStructureStub(typename).UnpackStructure<T>(l);
            }
            else
            {
                return node.UnpackStructure<T>(l,context);
            }
        }

        public abstract IStructureStub FindStructureStub(string objecttype);
        public abstract IPodStub FindPodStub(string objecttype);
        public abstract INamedArrayStub FindNamedArrayStub(string objecttype);

        public virtual ServiceStub CreateStub(string objecttype, string path, ClientContext context)
        {
            string extdef = ServiceDefinitionUtil.SplitQualifiedName(objecttype).Item1;
            if (extdef == this.GetServiceName()) throw new InvalidOperationException("Invalid service factory request");
            if (this.context == null)
            {
                return node.GetServiceType(extdef).CreateStub(objecttype, path, context);
            }
            else
            {
                return this.context.GetPulledServiceType(extdef).CreateStub(objecttype, path, context);
            }
        }

        public MessageElementPodArray PackPodToArray<T>(ref T s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.GetType()), out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).PackPodToArray(ref s);
            }
            else
            {
                return node.PackPodToArray(ref s, context);
            }
        }

        public T UnpackPodFromArray<T>(MessageElementPodArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).UnpackPodFromArray(l);
            }
            else
            {
                return node.UnpackPodFromArray<T>(l, context);
            }
        }

        public MessageElementPodArray PackPodArray<T>(T[] s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.GetType().GetElementType()), out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).PackPodArray(s);
            }
            else
            {
                return node.PackPodArray(s, context);
            }
        }

        public T[] UnpackPodArray<T>(MessageElementPodArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).UnpackPodArray(l);
            }
            else
            {
                return node.UnpackPodArray<T>(l,context);
            }
        }

        public MessageElementPodMultiDimArray PackPodMultiDimArray<T>(PodMultiDimArray s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.pod_array.GetType().GetElementType()), out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).PackPodMultiDimArray(s);
            }
            else
            {
                return node.PackPodMultiDimArray<T>(s, context);
            }
        }

        public PodMultiDimArray UnpackPodMultiDimArray<T>(MessageElementPodMultiDimArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((PodStub<T>)FindPodStub(typename)).UnpackPodMultiDimArray(l);
            }
            else
            {
                return node.UnpackPodMultiDimArray<T>(l, context);
            }
        }

        public object PackPod(object s)
        {
            Type t;

            var s1 = s as PodMultiDimArray;
            if (s1 != null)
            {
                t = s1.pod_array.GetType().GetElementType();
            }
            else
            {
                var s2 = s as Array;
                if (s2 != null)
                {
                    t = s2.GetType().GetElementType();
                }
                else
                {
                    t = s.GetType();
                }
            }


            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(t), out typename))
            {
                return FindPodStub(typename).PackPod(s);
            }
            else
            {
                return node.PackPod(s, context);
            }
        }

        public object UnpackPod(object m)
        {
            string typename;
            if (CompareNamespace(MessageElementUtil.GetMessageElementDataTypeString(m), out typename))
            {
                return FindPodStub(typename).UnpackPod(m);
            }
            else
            {
                return node.UnpackPod(m,context);
            }
        }

        //namedarray

        public MessageElementNamedArray PackNamedArrayToArray<T>(ref T s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.GetType()), out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).PackNamedArrayStructToArray(ref s);
            }
            else
            {
                return node.PackNamedArrayToArray(ref s, context);
            }
        }

        public T UnpackNamedArrayFromArray<T>(MessageElementNamedArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).UnpackNamedArrayStructFromArray(l);
            }
            else
            {
                return node.UnpackNamedArrayFromArray<T>(l,context);
            }
        }

        public MessageElementNamedArray PackNamedArray<T>(T[] s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.GetType().GetElementType()), out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).PackNamedArray(s);
            }
            else
            {
                return node.PackNamedArray(s, context);
            }
        }

        public T[] UnpackNamedArray<T>(MessageElementNamedArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).UnpackNamedArray(l);
            }
            else
            {
                return node.UnpackNamedArray<T>(l,context);
            }
        }

        public MessageElementNamedMultiDimArray PackNamedMultiDimArray<T>(NamedMultiDimArray s) where T : struct
        {
            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(s.namedarray_array.GetType().GetElementType()), out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).PackNamedMultiDimArray(s);
            }
            else
            {
                return node.PackNamedMultiDimArray<T>(s, context);
            }
        }

        public NamedMultiDimArray UnpackNamedMultiDimArray<T>(MessageElementNamedMultiDimArray l) where T : struct
        {
            string typename;
            if (CompareNamespace(l.Type, out typename))
            {
                return ((INamedArrayStub<T>)FindNamedArrayStub(typename)).UnpackNamedMultiDimArray(l);
            }
            else
            {
                return node.UnpackNamedMultiDimArray<T>(l,context);
            }
        }

        public object PackNamedArray(object s)
        {
            Type t;

            var s1 = s as NamedMultiDimArray;
            if (s1 != null)
            {
                t = s1.namedarray_array.GetType().GetElementType();
            }
            else
            {
                var s2 = s as Array;
                if (s2 != null)
                {
                    t = s2.GetType().GetElementType();
                }
                else
                {
                    t = s.GetType();
                }
            }


            string typename;
            if (CompareNamespace(ServiceDefinitionUtil.FindStructRRType(t), out typename))
            {
                return FindNamedArrayStub(typename).PackNamedArray(s);
            }
            else
            {
                return node.PackNamedArray(s, context);
            }
        }

        public object UnpackNamedArray(object m)
        {
            string typename;
            if (CompareNamespace(MessageElementUtil.GetMessageElementDataTypeString(m), out typename))
            {
                return FindNamedArrayStub(typename).UnpackNamedArray(m);
            }
            else
            {
                return node.UnpackNamedArray(m, context);
            }
        }

        public virtual ServiceSkel CreateSkel(string path, object obj, ServerContext context)
        {
            string objecttype = ServiceDefinitionUtil.FindObjectRRType(obj);
            string extdef = ServiceDefinitionUtil.SplitQualifiedName(objecttype).Item1;
            if (extdef == this.GetServiceName()) throw new InvalidOperationException("Invalid service factory request");

            return node.GetServiceType(extdef).CreateSkel(path, obj, context);

        }

        public abstract string GetServiceName();

        public virtual RobotRaconteurException DownCastException(RobotRaconteurException exp)
        {
            try
            {
                if (!exp.Error.Contains("."))
                {
                    return exp;
                }

                ServiceFactory f = null;

                var s = ServiceDefinitionUtil.SplitQualifiedName(exp.Error);

                if (context != null)
                {
                    if (!context.TryGetPulledServiceType(s.Item1, out f))
                    {
                        f = null;
                    }
                }
                if (f == null)
                {
                    if (!node.TryGetServiceType(s.Item1, out f))
                    {
                        f = null;
                    }
                }

                if (f==null)
                {
                    return exp;
                }

                return f.DownCastException(exp);                
            }
            catch (Exception)
            {
                return null;
            }
        }

        public virtual object PackMapType<K, T>(object o)
        {
            return node.PackMapType<K, T>(o, context);
        }

        public virtual object PackListType<T>(object o)
        {
            return node.PackListType<T>(o, context);
        }

        public virtual object PackMultiDimArray(MultiDimArray multiDimArray)
        {
            return node.PackMultiDimArray(multiDimArray);
        }

        public virtual object PackVarType(object p)
        {
            return node.PackVarType(p, context);
        }

        public virtual object UnpackMapType<K, T>(object o)
        {
            return node.UnpackMapType<K, T>(o, context);
        }

        public virtual object UnpackListType<T>(object o)
        {
            return node.UnpackListType<T>(o, context);
        }

        public virtual MultiDimArray UnpackMultiDimArray(MessageElementMultiDimArray o)
        {
            return node.UnpackMultiDimArray(o);
        }

        public virtual object UnpackVarType(MessageElement o)
        {
            return node.UnpackVarType(o, context);
        }
    }


}