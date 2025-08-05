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

//cSpell: ignore mema, strideb,

namespace RobotRaconteurWeb
{
#pragma warning disable 1591
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
#pragma warning restore 1591

    /**
    <summary>
    Numeric primitive multidimensional array value type
    </summary>
    <remarks>
    This class stores a numeric primitive multidimensional arrays.
    Multidimensional arrays are stored as a uint array of
    dimensions, and an array of the flattened elements.
    Arrays are stored in column major, or "Fortran" order.

    Valid types for array are `bool`, `double`, `float`, `sbyte`, `byte`, `short`,
    `ushort`, `int`, `uint`, `long`, `ulong`, `CDouble`,
    or `CSingle`. Attempts to use any other types will result in a compiler error.
    </remarks>
    */

    [PublicApi]
    public class MultiDimArray
    {
        /**
        <summary>
        Construct empty MultiDimArray
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public MultiDimArray() { }
        /**
        <summary>
        Construct MultiDimArray with dims and array
        </summary>
        <remarks>None</remarks>
        <param name="Dims">The dimensions of the array</param>
        <param name="Array_">The array data in fortran order</param>
        */

        [PublicApi]
        public MultiDimArray(uint[] Dims, Array Array_)
        {

            this.Dims = Dims;
            this.Array_ = Array_;
        }
        /**
        <summary>
        The dimensions of the array
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public uint[] Dims;
        /**
        <summary>
        The data of the array in flattened "Fortran" order
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public Array Array_;
        /**
        <summary>
        Retrieve a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position in array to read</param>
        <param name="buffer">Buffer to store retrieved data</param>
        <param name="bufferpos">Position within buffer to store data</param>
        <param name="count">Count of data to retrieve</param>
        */

        [PublicApi]
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
                Array.Copy(mema.Array_, (long)indexa, memb.Array_, (long)indexb, (long)len);
            }

        }
        /**
        <summary>
        Assign a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position within array to store data</param>
        <param name="buffer">Buffer to assign data from</param>
        <param name="bufferpos">Position within buffer to assign from</param>
        <param name="count">Count of data to assign</param>
        */

        [PublicApi]
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
                Array.Copy(memb.Array_, (long)indexb, mema.Array_, (long)indexa, (long)len);

            }
        }

    }
    /**
    <summary>
    `pod` multidimensional array value type
    </summary>
    <remarks>
    This class stores a pod multidimensional array.
    Multidimensional arrays are stored as a uint32_t array of
    dimensions, and an array of the flattened elements.
    Arrays are stored in column major, or "Fortran" order.

    Stored type must be a od type that has been generated as part
    of the thunk source.
    </remarks>
    */

    [PublicApi]
    public class PodMultiDimArray
    {
        /**
        <summary>
        Construct empty PodMultiDimArray
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public PodMultiDimArray()
        {
            Dims = new uint[] { 0 };
        }
        /**
        <summary>
        Construct PodMultiDimArray with dims and array
        </summary>
        <remarks>None</remarks>
        <param name="dims">The dimensions of the array</param>
        <param name="array">The array data in fortran order</param>
        */

        [PublicApi]
        public PodMultiDimArray(uint[] dims, Array array)
        {
            Dims = dims;
            pod_array = array;
        }
        /**
        <summary>
        The dimensions of the array
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public uint[] Dims;
        /**
        <summary>
        The data of the array in flattened "Fortran" order
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public Array pod_array;
        /**
        <summary>
        Retrieve a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position in array to read</param>
        <param name="buffer">Buffer to store retrieved data</param>
        <param name="bufferpos">Position within buffer to store data</param>
        <param name="count">Count of data to retrieve</param>
        */


        [PublicApi]
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
                Array.Copy(mema.pod_array, (long)indexa, memb.pod_array, (long)indexb, (long)len);
            }

        }
        /**
        <summary>
        Assign a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position within array to store data</param>
        <param name="buffer">Buffer to assign data from</param>
        <param name="bufferpos">Position within buffer to assign from</param>
        <param name="count">Count of data to assign</param>
        */

        [PublicApi]
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
                Array.Copy(memb.pod_array, (long)indexb, mema.pod_array, (long)indexa, (long)len);
            }
        }

    }
    /**
    <summary>
    `namedarray` multidimensional array value type
    </summary>
    <remarks>
    This class stores a namedarray multidimensional array.
    Multidimensional arrays are stored as a uint32_t array of
    dimensions, and an array of the flattened elements.
    Arrays are stored in column major, or "Fortran" order.
    </remarks>
    */

    [PublicApi]
    public class NamedMultiDimArray
    {
        /**
        <summary>
        Construct empty NamedMultiDimArray
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public NamedMultiDimArray()
        {
            Dims = new uint[] { 0 };
        }
        /**
        <summary>
        Construct NamedMultiDimArray with dims and array
        </summary>
        <remarks>None</remarks>
        <param name="dims">The dimensions of the array</param>
        <param name="array">The array data in fortran order</param>
        */

        [PublicApi]
        public NamedMultiDimArray(uint[] dims, Array array)
        {
            Dims = dims;
            namedarray_array = array;
        }
        /**
        <summary>
        The dimensions of the array
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public uint[] Dims;
        /**
        <summary>
        The data of the array in flattened "Fortran" order
        </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public Array namedarray_array;
        /**
        <summary>
        Retrieve a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position in array to read</param>
        <param name="buffer">Buffer to store retrieved data</param>
        <param name="bufferpos">Position within buffer to store data</param>
        <param name="count">Count of data to retrieve</param>
        */


        [PublicApi]
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
                Array.Copy(mema.namedarray_array, (long)indexa, memb.namedarray_array, (long)indexb, (long)len);
            }

        }
        /**
        <summary>
        Assign a subset of an array
        </summary>
        <remarks>None</remarks>
        <param name="memorypos">Position within array to store data</param>
        <param name="buffer">Buffer to assign data from</param>
        <param name="bufferpos">Position within buffer to assign from</param>
        <param name="count">Count of data to assign</param>
        */


        [PublicApi]
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
                Array.Copy(memb.namedarray_array, (long)indexb, mema.namedarray_array, (long)indexa, (long)len);
            }
        }

    }
}
