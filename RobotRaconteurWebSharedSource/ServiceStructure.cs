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
    public interface IStructureStub
    {
        MessageElementNestedElementList PackStructure(Object s);
                
        T UnpackStructure<T>(MessageElementNestedElementList m);
    }

    public interface IPodStub
    {
        object PackPod(object s);

        object UnpackPod(object m);
    }

    public abstract class PodStub<T> : IPodStub where T : struct
    {
        public abstract MessageElementNestedElementList PackPod(ref T s);

        public abstract T UnpackPod(MessageElementNestedElementList m);


        public virtual MessageElementNestedElementList PackPodToArray(ref T s2)
        {
            var mm = new List<MessageElement>();
            MessageElementUtil.AddMessageElement(mm,
                MessageElementUtil.NewMessageElement(0, PackPod(ref s2))
                );
            return new MessageElementNestedElementList(DataTypes.pod_array_t, TypeName, mm);
        }

        public virtual MessageElementNestedElementList PackPodArray(T[] s2)
        {
            if (s2 == null) return null;

            var mm = new List<MessageElement>();
            for (int i = 0; i < s2.Length; i++)
            {
                MessageElementUtil.AddMessageElement(mm,
                    MessageElementUtil.NewMessageElement(i, PackPod(ref s2[i]))
                    );
            }
            return new MessageElementNestedElementList(DataTypes.pod_array_t, TypeName, mm);
        }

        public virtual MessageElementNestedElementList PackPodMultiDimArray(PodMultiDimArray s3)
        {
            if (s3 == null) return null;
            var l = new List<MessageElement>();
            MessageElementUtil.AddMessageElement(l, "dims", s3.Dims);
            var s4 = PackPodArray((T[])s3.pod_array);
            MessageElementUtil.AddMessageElement(l, "array", s4);
            return new MessageElementNestedElementList(DataTypes.pod_multidimarray_t,TypeName, l);
        }

        public virtual T UnpackPodFromArray(MessageElementNestedElementList s2)
        {
            if (s2.TypeName != TypeName) throw new DataTypeException("pod type mismatch");
            var cdataElements = s2.Elements;
            if (cdataElements.Count != 1) throw new DataTypeException("pod type mismatch");
            var e = cdataElements[0];
            if (0 != MessageElementUtil.GetMessageElementNumber(e)) throw new DataTypeException("Error in list format");
            var md = e.CastDataToNestedList(DataTypes.pod_array_t);
            return UnpackPod(md);

        }

        public virtual T[] UnpackPodArray(MessageElementNestedElementList s2)
        {
            if (s2.TypeName != TypeName) throw new DataTypeException("pod type mismatch");
            int count = 0;
            {
                var cdataElements = s2.Elements;
                T[] o = new T[cdataElements.Count];
                foreach (MessageElement e in cdataElements)
                {
                    if (count != MessageElementUtil.GetMessageElementNumber(e)) throw new DataTypeException("Error in list format");
                    o[count] = UnpackPod(e.CastDataToNestedList(DataTypes.pod_t));
                    count++;
                }
                return o;
            }
        }

        public virtual PodMultiDimArray UnpackPodMultiDimArray(MessageElementNestedElementList s3)
        {
            if (s3.TypeName != TypeName) throw new DataTypeException("pod type mismatch");
            var o = new PodMultiDimArray();
            var marrayElements = s3.Elements;
            {
                o.Dims = (MessageElementUtil.FindElementAndCast<uint[]>(marrayElements, "dims"));
                var s2 = (MessageElementUtil.FindElementAndCast<MessageElementNestedElementList>(marrayElements, "array"));
                o.pod_array = UnpackPodArray(s2);                
            }
            return o;
        }

        public virtual object PackPod(object s)
        {
            if (s is T)
            {
                T s2 = (T)s;
                return PackPodToArray(ref s2);
            }

            var s3 = s as T[];
            if (s3 != null)
            {
                return PackPodArray(s3);
            }

            var s4 = s as PodMultiDimArray;
            if (s4 != null)
            {
                return PackPodMultiDimArray(s4);
            }

            throw new DataTypeException("Unexpected message element type for PackPod");
        }
        public virtual object UnpackPod(object m)
        {
            /*var m2 = m as MessageElementPod;
            if (m2 != null)
            {
                
                return UnpackPod(m2);           
            }*/
            var m2 = (MessageElementNestedElementList)m;
            
            if (m2.Type == DataTypes.pod_array_t)
            {
                return UnpackPodArray(m2);
            }


            if (m2.Type == DataTypes.pod_multidimarray_t)
            {
                return UnpackPodMultiDimArray(m2);
            }

            throw new DataTypeException("Unexpected message element type for UnpackPod");
        }

        public abstract string TypeName { get; }
    }

    public class RobotRaconteurServicePod : System.Attribute
    {
        public RobotRaconteurServicePod(string rr_type)
        {
            RRType = rr_type;            
        }

        public string RRType { get; }
    }

    public class RobotRaconteurNamedArrayElementTypeAndCount : System.Attribute
    {
        public RobotRaconteurNamedArrayElementTypeAndCount(string rr_type, Type element_type, int element_array_count)
        {
            RRType = rr_type;
            ElementArrayType = element_type;
            ElementArrayCount = element_array_count;
        }

        public string RRType { get; }
        public Type ElementArrayType { get; }
        public int ElementArrayCount { get; }

    }

    public interface INamedArrayStub
    {
        object PackNamedArray(object s);

        object UnpackNamedArray(object m);
    }

    public interface INamedArrayStub<T> : INamedArrayStub
    {
        MessageElementNestedElementList PackNamedArrayStructToArray(ref T s2);

        MessageElementNestedElementList PackNamedArray(T[] s2);

        MessageElementNestedElementList PackNamedMultiDimArray(NamedMultiDimArray s3);

        T UnpackNamedArrayStructFromArray(MessageElementNestedElementList s2);

        T[] UnpackNamedArray(MessageElementNestedElementList s2);

        NamedMultiDimArray UnpackNamedMultiDimArray(MessageElementNestedElementList s3);
    }

    public abstract class NamedArrayStub<T, U> : INamedArrayStub<T> where T : struct
    {
        public abstract U[] GetNumericArrayFromNamedArrayStruct(ref T s);

        public abstract T GetNamedArrayStructFromNumericArray(U[] m);

        public abstract U[] GetNumericArrayFromNamedArray(T[] s);

        public abstract T[] GetNamedArrayFromNumericArray(U[] m);


        public virtual MessageElementNestedElementList PackNamedArrayStructToArray(ref T s2)
        {
            var mm = new List<MessageElement>();
            MessageElementUtil.AddMessageElement(mm,
                MessageElementUtil.NewMessageElement("array", GetNumericArrayFromNamedArrayStruct(ref s2))
                );

            return new MessageElementNestedElementList(DataTypes.namedarray_array_t, TypeName, mm);
        }

        public virtual MessageElementNestedElementList PackNamedArray(T[] s2)
        {
            if (s2 == null) return null;
            var mm = new List<MessageElement>();
            MessageElementUtil.AddMessageElement(mm,
                MessageElementUtil.NewMessageElement("array", GetNumericArrayFromNamedArray(s2))
                );

            return new MessageElementNestedElementList(DataTypes.namedarray_array_t, TypeName, mm);
        }

        public virtual MessageElementNestedElementList PackNamedMultiDimArray(NamedMultiDimArray s3)
        {
            if (s3 == null) return null;
            var l = new List<MessageElement>();

            MessageElementUtil.AddMessageElement(l, "dims", s3.Dims);
            MessageElementUtil.AddMessageElement(l, "array", PackNamedArray((T[])s3.namedarray_array));
            return new MessageElementNestedElementList(DataTypes.namedarray_multidimarray_t, TypeName, l);
        }

        public virtual T UnpackNamedArrayStructFromArray(MessageElementNestedElementList s2)
        {
            if (s2.TypeName != TypeName) throw new DataTypeException("namedarray type mismatch");
            var cdataElements = s2.Elements;
            if (cdataElements.Count != 1) throw new DataTypeException("namedarray type mismatch");
            var a = MessageElementUtil.FindElementAndCast<U[]>(cdataElements, "array");
            return GetNamedArrayStructFromNumericArray(a);
        }

        public virtual T[] UnpackNamedArray(MessageElementNestedElementList s2)
        {
            if (s2.TypeName != TypeName) throw new DataTypeException("namedarray type mismatch");
            var cdataElements = s2.Elements;
            if (cdataElements.Count != 1) throw new DataTypeException("namedarray type mismatch");
            var a = MessageElementUtil.FindElementAndCast<U[]>(cdataElements, "array");
            return GetNamedArrayFromNumericArray(a);
        }

        public virtual NamedMultiDimArray UnpackNamedMultiDimArray(MessageElementNestedElementList s3)
        {
            if (s3.TypeName != TypeName) throw new DataTypeException("namedarray type mismatch");
            var o = new NamedMultiDimArray();
            var marrayElements = s3.Elements;
            o.Dims = (MessageElementUtil.FindElementAndCast<uint[]>(marrayElements, "dims"));
            var s2 = (MessageElementUtil.FindElementAndCast<MessageElementNestedElementList>(marrayElements, "array"));
            o.namedarray_array = UnpackNamedArray(s2);
            return o;
        }

        public virtual object PackNamedArray(object s)
        {
            if (s is T)
            {
                T s2 = (T)s;
                return PackNamedArrayStructToArray(ref s2);
            }

            var s3 = s as T[];
            if (s3 != null)
            {
                return PackNamedArray(s3);
            }

            var s4 = s as NamedMultiDimArray;
            if (s4 != null)
            {
                return PackNamedMultiDimArray(s4);
            }

            throw new DataTypeException("Unexpected message element type for PackNamedArray");
        }
        public virtual object UnpackNamedArray(object m)
        {
            /*var m2 = m as MessageElementPod;
            if (m2 != null)
            {
                
                return UnpackPod(m2);           
            }*/
            var m2 = (MessageElementNestedElementList)m;
            
            if (m2.Type ==DataTypes.namedarray_array_t)
            {
                return UnpackNamedArray(m2);
            }

            if (m2.Type == DataTypes.namedarray_array_t)                
            {
                return UnpackNamedMultiDimArray(m2);
            }

            throw new DataTypeException("Unexpected message element type for UnpackNamedArray");
        }

        public abstract string TypeName { get; }
    }

}