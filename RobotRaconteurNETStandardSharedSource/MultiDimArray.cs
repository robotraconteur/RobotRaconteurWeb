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

using RobotRaconteur.Extensions;
using System;
using System.Collections.Generic;



namespace RobotRaconteur
{

    public class MultiDimArray_CalculateCopyIndicesIter
    {
        uint[] mema_dims;
        uint[] memb_dims;
        uint[] mema_pos;
        uint[] memb_pos;
        uint[] count;

        uint[] stridea;
        uint[] strideb;

        uint[] current_count;

        bool done;

        public static MultiDimArray_CalculateCopyIndicesIter BeginIter(uint[] mema_dims, uint[] mema_pos, uint[] memb_dims, uint[] memb_pos, uint[] count)
        {
            if (count.Length == 0) throw new ArgumentException("MultiDimArray count invalid");

            if (count.Length > mema_dims.Length || count.Length > memb_dims.Length) throw new ArgumentException("MultiDimArray copy count invalid");
            if (count.Length > memb_dims.Length || count.Length > memb_dims.Length) throw new ArgumentException("MultiDimArray copy count invalid");

            for (int i = 0; i < mema_dims.Length; i++)
            {
                if (mema_dims[i] < 0) throw new ArgumentException("MultiDimArray mema_dims invalid");
            }

            for (int i = 0; i < memb_dims.Length; i++)
            {
                if (memb_dims[i] < 0) throw new ArgumentException("MultiDimArray memb_dims invalid");
            }

            for (int i = 0; i < count.Length; i++)
            {
                if (count[i] < 0) throw new ArgumentException("MultiDimArray count invalid");
            }

            for (int i = 0; i < mema_dims.Length && i < count.Length; i++)
            {
                if (mema_pos[i] > mema_dims[i] || (mema_pos[i] + count[i]) > mema_dims[i])
                    throw new ArgumentException("MultiDimArray A count invalid");
            }

            for (int i = 0; i < memb_dims.Length && i < count.Length; i++)
            {
                if (memb_pos[i] > memb_dims[i] || (memb_pos[i] + count[i]) > memb_dims[i])
                    throw new ArgumentException("MultiDimArray B count invalid");
            }

            var stridea = new uint[count.Length];
            stridea[0] = 1;
            for (uint i = 1; i < (uint)count.Length; i++)
            {
                stridea[i] = stridea[i - 1] * mema_dims[i - 1];
            }

            var strideb = new uint[count.Length];
            strideb[0] = 1;
            for (uint i = 1; i < (uint)count.Length; i++)
            {
                strideb[i] = strideb[i - 1] * memb_dims[i - 1];
            }

            var o = new MultiDimArray_CalculateCopyIndicesIter();

            o.mema_dims = mema_dims;
            o.memb_dims = memb_dims;
            o.mema_pos = mema_pos;
            o.memb_pos = memb_pos;
            o.count = count;            
            o.current_count = new uint[count.Length];
            o.done = false;
            o.stridea = stridea;
            o.strideb = strideb;
            return o;
        }

        public bool Next(out uint indexa, out uint indexb, out uint len)
        {
            if (done)
            {
                indexa = 0;
                indexb = 0;
                len = 0;
                return false;
            }

            int a = 0;
            int b = 0;

            indexa = 0;
            for (uint j = 0; j < (uint)count.Length; j++)
                indexa += (current_count[j] + mema_pos[j]) * stridea[j];
            indexb = 0;
            for (uint j = 0; j < (uint)count.Length; j++)
                indexb += (current_count[j] + memb_pos[j]) * strideb[j];

            len = count[0];

            if (count.Length <= 1)
            {
                done = true;
                return true;
            }

            current_count[1]++;
            for (uint j = 1; j < (uint)count.Length; j++)
            {
                if (current_count[j] > (count[j] - 1))
                {
                    current_count[j] = current_count[j] - count[j];
                    if (j < (uint)count.Length - 1)
                    {
                        current_count[j + 1]++;
                    }
                    else
                    {
                        done = true;
                        return true;
                    }
                }
            }

            return true;
        }
    }



    public class MultiDimArray
    {

        public MultiDimArray() { }

        public MultiDimArray(uint[] Dims, Array Array_)
        {

            this.Dims = Dims;
            this.Array_ = Array_;
        }

        public uint[] Dims;
        public Array Array_;

        public virtual void RetrieveSubArray(uint[] memorypos, MultiDimArray buffer, uint[] bufferpos, uint[] count)
        {

            MultiDimArray mema = this;
            MultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(mema.Array_, (long)indexa, memb.Array_, (long)indexb, (long)len);
            }

        }

        public virtual void AssignSubArray(uint[] memorypos, MultiDimArray buffer, uint[] bufferpos, uint[] count)
        {


            MultiDimArray mema = this;
            MultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(memb.Array_, (long)indexb, mema.Array_, (long)indexa, (long)len);

            }
        }

    }

    public class PodMultiDimArray
    {
        public PodMultiDimArray()
        {
            Dims = new uint[] { 0 };
        }

        public PodMultiDimArray(uint[] dims, Array array)
        {
            Dims = dims;
            pod_array = array;
        }

        public uint[] Dims;
        public Array pod_array;

        public virtual void RetrieveSubArray(uint[] memorypos, PodMultiDimArray buffer, uint[] bufferpos, uint[] count)
        {

            PodMultiDimArray mema = this;
            PodMultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(mema.pod_array, (long)indexa, memb.pod_array, (long)indexb, (long)len);
            }

        }

        public virtual void AssignSubArray(uint[] memorypos, PodMultiDimArray buffer, uint[] bufferpos, uint[] count)
        {
            PodMultiDimArray mema = this;
            PodMultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(memb.pod_array, (long)indexb, mema.pod_array, (long)indexa, (long)len);
            }
        }

    }

    public class NamedMultiDimArray
    {
        public NamedMultiDimArray()
        {
            Dims = new uint[] { 0 };
        }

        public NamedMultiDimArray(uint[] dims, Array array)
        {
            Dims = dims;
            namedarray_array = array;
        }

        public uint[] Dims;
        public Array namedarray_array;

        public virtual void RetrieveSubArray(uint[] memorypos, NamedMultiDimArray buffer, uint[] bufferpos, uint[] count)
        {

            NamedMultiDimArray mema = this;
            NamedMultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(mema.namedarray_array, (long)indexa, memb.namedarray_array, (long)indexb, (long)len);
            }

        }

        public virtual void AssignSubArray(uint[] memorypos, NamedMultiDimArray buffer, uint[] bufferpos, uint[] count)
        {
            NamedMultiDimArray mema = this;
            NamedMultiDimArray memb = buffer;

            MultiDimArray_CalculateCopyIndicesIter iter = MultiDimArray_CalculateCopyIndicesIter.BeginIter(mema.Dims, memorypos, memb.Dims, bufferpos, count);

            uint indexa;
            uint indexb;
            uint len;

            while (iter.Next(out indexa, out indexb, out len))
            {
                RRArrayExtensions.Copy(memb.namedarray_array, (long)indexb, mema.namedarray_array, (long)indexa, (long)len);
            }
        }

    }
}