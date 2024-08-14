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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
#pragma warning disable 1591
    public abstract class ArrayMemoryBase
    {
        public abstract Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken));
    }
#pragma warning restore 1591

    /**
    <summary>
    Single dimensional numeric primitive random access memory region
    </summary>
    <remarks>
    <para>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    </para>
    <para>
    The ArrayMemory class is used to represent a single dimensional numeric
    primitive array. Multidimensional numeric primitive arrays should use
    MultiDimArrayMemory. Valid types for T are `double`, `float`, `sbyte`,
    `byte`, `short`, `ushort`, `uint`, `uint`, `long`,
    `ulong`, `bool`, `CDouble`, and `CSingle`.
    </para>
    <para>
    ArrayMemory instances are attached to an RRArray, either when
    constructed or later using Attach().
    </para>
    <para>
    ArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </para>
    </remarks>
    <typeparam name="T" />
    */
    [PublicApi]
    public class ArrayMemory<T> : ArrayMemoryBase
    {

        private T[] memory;
        /**
        <summary>
        Construct a new ArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public ArrayMemory()
        {

        }
        /**
        <summary>
        Construct a new ArrayMemory instance attached to an array
        </summary>
        <remarks>
        New instance will be constructed attached to an array.
        </remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public ArrayMemory(T[] memory)
        {
            this.memory = memory;
        }
        /**
        <summary>
        Attach ArrayMemory instance to an array
        </summary>
        <remarks>None</remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public virtual void Attach(T[] memory)
        {
            this.memory = memory;
        }
        /**
        <summary>
        Return the length of the array memory
        </summary>
        <remarks>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </remarks>
        */

        [PublicApi]
        public override Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken))
        {            
                return Task.FromResult((ulong)memory.LongLength);            
        }
        /**
        <summary>
        Read a segment from an array memory
        </summary>
        <remarks>
        <para>
        Read a segment of an array memory into a supplied buffer array. The start positions and length
        of the read are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start index in the memory array to read</param>
        <param name="buffer">The buffer to receive the read data</param>
        <param name="bufferpos">The start index in the buffer to write the data</param>
        <param name="count">The number of array elements to read</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Read(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel=default(CancellationToken))
        {
            lock (this)
            {
                Array.Copy(memory, (long)memorypos, buffer, (long)bufferpos, (long)count);
            }
            return Task.FromResult(0);
        }
        /**
        <summary>
        Write a segment to an array memory
        </summary>
        <remarks>
        <para>
        Writes a segment to an array memory from a supplied buffer array. The start positions and length
        of the write are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start index in the memory array to write</param>
        <param name="buffer">The buffer to write the data from</param>
        <param name="bufferpos">The start index in the buffer to read the data</param>
        <param name="count">The number of array elements to write</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Write(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            lock (this)
            {
                Array.Copy(buffer, (long)bufferpos, memory, (long)memorypos, (long)count);
            }
            return Task.FromResult(0);
        }
    }
#pragma warning disable 1591
    public abstract class MultiDimArrayMemoryBase
    {
        public abstract Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken));

        public abstract Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken));        
    }
#pragma warning restore 1591
    /**
    <summary>
    Multidimensional numeric primitive random access memory region
    </summary>
    <remarks>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    
    The MultiDimArrayMemory class is used to represent a multidimensional numeric
    primitive array. Single dimensional numeric primitive arrays should use
    ArrayMemory. Valid types for T are `double`, `float`, `sbyte`,
    `byte`, `short`, `ushort`, `int`, `uint`, `long`,
    `ulong`, `bool`, `CDouble`, and `CSingle`.
    
    MultiDimArrayMemory instances are attached to an MultiDimArray,
    either when constructed or later using Attach().
    
    MultiDimArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </remarks>
    <typeparam name="T">The numeric primitive type of the array</typeparam>
    */

    [PublicApi]
    public class MultiDimArrayMemory<T> : MultiDimArrayMemoryBase
    {
        private MultiDimArray multimemory;
        /**
        <summary>
        Construct a new MultiDimArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public MultiDimArrayMemory()
        {

        }
        /**
        <summary>
        Construct a new MultiDimArrayMemory instance attached to a MultiDimArray
        </summary>
        <remarks>
        New instance will be constructed attached to an array.
        </remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public MultiDimArrayMemory(MultiDimArray memory)
        {
            multimemory = memory;
        }
        /**
        <summary>
        Attach MultiDimArrayMemory instance to a MultiDimArray
        </summary>
        <remarks>None</remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public virtual void Attach(MultiDimArray memory)
        {
            this.multimemory = memory;
        }
        /**
        <summary>
        Dimensions of the memory array
        </summary>
        <remarks>
        <para>
        Returns the dimensions (shape) of the memory array
        </para>
        <para>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </para>
        </remarks>
        */

        [PublicApi]
        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {            
            return Task.FromResult(multimemory.Dims.Select(x => (ulong)x).ToArray());            
        }
        /**
        <summary>
        The number of dimensions in the memory array
        </summary>
        <remarks>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </remarks>
        */

        [PublicApi]
        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((ulong)multimemory.Dims.Length);
        }

        /**
        <summary>
        Read a block from a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Read a block of a multidimensional array memory into a supplied buffer multidimensional array.
        The start positions and count of the read are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to read</param>
        <param name="buffer">The buffer to receive the read data</param>
        <param name="bufferpos">The start position in the buffer to write the data</param>
        <param name="count">The count of array elements to read</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Read(ulong[] memorypos, MultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.RetrieveSubArray(memorypos.Select(x=>(uint)x).ToArray(), buffer, bufferpos.Select(x=>(uint)x).ToArray(), count.Select(x=>(uint)x).ToArray());
            return Task.FromResult(0);            
        }
        /**
        <summary>
        Write a segment to a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Writes a segment to a multidimensional array memory from a supplied buffer
        multidimensional array. The start positions and count
        of the write are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to write</param>
        <param name="buffer">The buffer to write the data from</param>
        <param name="bufferpos">The start position in the buffer to read the data</param>
        <param name="count">The count of array elements to write</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Write(ulong[] memorypos, MultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.AssignSubArray(memorypos.Select(x => (uint)x).ToArray(), buffer, bufferpos.Select(x => (uint)x).ToArray(), count.Select(x => (uint)x).ToArray());
            return Task.FromResult(0);
        }
    }

#pragma warning disable 1591
    public abstract class ArrayMemoryServiceSkelBase
    {
        protected string m_MemberName;
        protected ServiceSkel skel;        
        protected MemberDefinition_Direction direction;
        protected DataTypes element_type;
        protected uint element_size;

        public string MemberName { get => m_MemberName; }
        protected ArrayMemoryServiceSkelBase(string membername, ServiceSkel skel, DataTypes element_type, uint element_size, MemberDefinition_Direction direction)
        {
            this.m_MemberName = membername;
            this.skel = skel;            
            this.direction = direction;
            this.element_type = element_type;
            this.element_size = element_size;
        }

        public virtual async Task<MessageEntry> CallMemoryFunction(MessageEntry m, Endpoint e, ArrayMemoryBase mem)
        {
            switch (m.EntryType)
            {
                case MessageEntryType.MemoryRead:
                    {
                        if (direction == MemberDefinition_Direction.writeonly)
                        {
                            throw new WriteOnlyMemberException("Write only member");
                        }

                        ulong memorypos = m.FindElement("memorypos").CastData<ulong[]>()[0];
                        ulong count = m.FindElement("count").CastData<ulong[]>()[0];
                        var data = await DoRead(memorypos, 0, count, mem).ConfigureAwait(false);
                        var ret = new MessageEntry(MessageEntryType.MemoryReadRet, MemberName);
                        ret.AddElement("memorypos", memorypos);
                        ret.AddElement("count", count);
                        ret.AddElement("data", data);
                        return ret;

                    }
                case MessageEntryType.MemoryWrite:
                    {
                        if (direction == MemberDefinition_Direction.readonly_)
                        {
                            throw new ReadOnlyMemberException("Read only member");
                        }

                        ulong memorypos = m.FindElement("memorypos").CastData<ulong[]>()[0];
                        ulong count = m.FindElement("count").CastData<ulong[]>()[0];
                        var data = m.FindElement("data").Data;
                        await DoWrite(memorypos, data, 0, count, mem).ConfigureAwait(false);
                        var ret = new MessageEntry(MessageEntryType.MemoryReadRet, MemberName);
                        ret.AddElement("memorypos", memorypos);
                        ret.AddElement("count", count);
                        return ret;
                    }
                case MessageEntryType.MemoryGetParam:
                    {
                        var param = m.FindElement("parameter").CastData<string>();
                        if (param == "Length")
                        {
                            var ret = new MessageEntry(MessageEntryType.MemoryGetParamRet, MemberName);
                            var len = await mem.GetLength().ConfigureAwait(false);
                            ret.AddElement("return", len);
                            return ret;

                        }
                        else if (param == "MaxTransferSize")
                        {
                            var ret = new MessageEntry(MessageEntryType.MemoryGetParamRet, MemberName);
                            var MaxTransferSize = skel.rr_node.MemoryMaxTransferSize;
                            ret.AddElement("return", MaxTransferSize);
                            return ret;

                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown parameter");
                        }
                    }
                default:
                    throw new ProtocolException("Invalid command");

            }
        }

        protected abstract Task<object> DoRead(ulong memorypos, ulong bufferpos, ulong count, ArrayMemoryBase mem);
        protected abstract Task DoWrite(ulong memorypos, object buffer, ulong bufferpos, ulong count, ArrayMemoryBase mem);
    }

    public class ArrayMemoryServiceSkel<T> : ArrayMemoryServiceSkelBase
    {
        public ArrayMemoryServiceSkel(string membername, ServiceSkel skel, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
			: base(membername, skel, DataTypeUtil.TypeIDFromType(typeof(T)), DataTypeUtil.size(DataTypeUtil.TypeIDFromType(typeof(T))), direction)
        {

        }

        protected override async Task<object> DoRead(ulong memorypos, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (ArrayMemory<T>)mem;
            var buf1 = (T[])DataTypeUtil.ArrayFromDataType(element_type, (uint)count);
            await mem1.Read(memorypos, buf1, 0, count).ConfigureAwait(false);
            return buf1;
        }

        protected override async Task DoWrite(ulong memorypos, object buffer, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (ArrayMemory<T>)mem;
            var buf1 = (T[])buffer;
            await mem1.Write(memorypos, buf1, 0, count).ConfigureAwait(false);
        }
    }

    public abstract class MultiDimArrayMemoryServiceSkelBase
    {
        protected string m_MemberName;
        protected ServiceSkel skel;
        protected MemberDefinition_Direction direction;
        protected DataTypes element_type;
        protected uint element_size;

        public string MemberName { get => m_MemberName; }

        protected MultiDimArrayMemoryServiceSkelBase(string membername, ServiceSkel skel, DataTypes element_type, uint element_size, MemberDefinition_Direction direction)
        {
            this.m_MemberName = membername;
            this.skel = skel;            
            this.direction = direction;
            this.element_type = element_type;
            this.element_size = element_size;
        }

        public virtual async Task<MessageEntry> CallMemoryFunction(MessageEntry m, Endpoint e, MultiDimArrayMemoryBase mem)
        {
            switch (m.EntryType)
            {
                case MessageEntryType.MemoryRead:
                    {
                        if (direction == MemberDefinition_Direction.writeonly)
                        {
                            throw new WriteOnlyMemberException("Write only member");
                        }

                        ulong[] memorypos = m.FindElement("memorypos").CastData<ulong[]>();
                        ulong[] count = m.FindElement("count").CastData<ulong[]>();
                        ulong elem_count = count.Aggregate((ulong)1, (x, y) => x * y);
                        var data = await DoRead(memorypos, new ulong[count.Length], count, elem_count, mem).ConfigureAwait(false);
                        var ret = new MessageEntry(MessageEntryType.MemoryReadRet, MemberName);
                        ret.AddElement("memorypos", memorypos);
                        ret.AddElement("count", count);
                        ret.AddElement("data", data);
                        return ret;

                    }
                case MessageEntryType.MemoryWrite:
                    {
                        if (direction == MemberDefinition_Direction.readonly_)
                        {
                            throw new ReadOnlyMemberException("Read only member");
                        }

                        ulong[] memorypos = m.FindElement("memorypos").CastData<ulong[]>();
                        ulong[] count = m.FindElement("count").CastData<ulong[]>();
                        ulong elem_count = count.Aggregate((ulong)1, (x, y) => x * y);
                        var data = m.FindElement("data").Data;
                        await DoWrite(memorypos, data, new ulong[count.Length], count, elem_count, mem).ConfigureAwait(false);
                        var ret = new MessageEntry(MessageEntryType.MemoryReadRet, MemberName);
                        ret.AddElement("memorypos", memorypos);
                        ret.AddElement("count", count);
                        return ret;
                    }
                case MessageEntryType.MemoryGetParam:
                    {
                        var param = m.FindElement("parameter").CastData<string>();
                        if (param == "Dimensions")
                        {
                            var ret = new MessageEntry(MessageEntryType.MemoryGetParamRet, MemberName);
                            var l = await mem.GetDimensions().ConfigureAwait(false);
                            ret.AddElement("return", l);
                            return ret;

                        }
                        if (param == "DimCount")
                        {
                            var ret = new MessageEntry(MessageEntryType.MemoryGetParamRet, MemberName);
                            var l = await mem.GetDimCount().ConfigureAwait(false);
                            ret.AddElement("return", l);
                            return ret;

                        }
                        else if (param == "MaxTransferSize")
                        {
                            var ret = new MessageEntry(MessageEntryType.MemoryGetParamRet, MemberName);
                            var MaxTransferSize = skel.rr_node.MemoryMaxTransferSize;
                            ret.AddElement("return", MaxTransferSize);
                            return ret;

                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown parameter");
                        }
                    }
                default:
                    throw new ProtocolException("Invalid command");

            }
        }

        protected abstract Task<object> DoRead(ulong[] memorypos, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem);
        protected abstract Task DoWrite(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem);
    }

    public class MultiDimArrayMemoryServiceSkel<T> : MultiDimArrayMemoryServiceSkelBase
    {
        public MultiDimArrayMemoryServiceSkel(string membername, ServiceSkel skel, MemberDefinition_Direction direction=MemberDefinition_Direction.both)
            : base(membername, skel, DataTypeUtil.TypeIDFromType(typeof(T)), DataTypeUtil.size(DataTypeUtil.TypeIDFromType(typeof(T))), direction)
        {

        }

        protected override async Task<object> DoRead(ulong[] memorypos, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (MultiDimArrayMemory<T>)mem;
            var buf1 = new MultiDimArray(count.Select(x=>(uint)x).ToArray(), (T[])DataTypeUtil.ArrayFromDataType(element_type, (uint)elem_count));
            await mem1.Read(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
            return skel.RRContext.PackMultiDimArray(buf1);
        }

        protected override async Task DoWrite(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (MultiDimArrayMemory<T>)mem;
            var buf1 = skel.RRContext.UnpackMultiDimArray((MessageElementNestedElementList)buffer);
            await mem1.Write(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
        }
    }

    public abstract class ArrayMemoryClientImplBase
    {
        protected string m_MemberName;
        protected ServiceStub stub;
        protected MemberDefinition_Direction direction;
        protected DataTypes element_type;
        protected uint element_size;

        public string MemberName { get => m_MemberName; }        

        protected ArrayMemoryClientImplBase(string membername, ServiceStub stub, DataTypes element_type, uint element_size, MemberDefinition_Direction direction)
        {
            this.stub = stub;            
            m_MemberName = membername;
            this.direction = direction;
            this.element_type = element_type;
            this.element_size = element_size;
        }

        public virtual async Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.MemoryGetParam, MemberName);
            m.AddElement("parameter", "Length");
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            return ret.FindElement("return").CastData<ulong[]>()[0];
        }

        public MemberDefinition_Direction Direction { get => direction; }

        protected bool max_size_read;
        protected uint remote_max_size;
        public async Task<uint> GetMaxTransferSize(CancellationToken cancel = default(CancellationToken))
        {
            uint my_max_size = stub.rr_node.MemoryMaxTransferSize;
            lock (this)
            {
                if (max_size_read)
                {
                    if (remote_max_size > my_max_size)
                        return my_max_size;
                    else
                        return remote_max_size;
                }
            }

            var m = new MessageEntry(MessageEntryType.MemoryGetParam, MemberName);
            m.AddElement("parameter", "MaxTransferSize");
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            var remote_max_size1 = ret.FindElement("return").CastData<uint[]>()[0];
            lock (this)
            {
                if (!max_size_read)
                {
                    remote_max_size = remote_max_size1;
                    max_size_read = true;
                }

                if (remote_max_size > my_max_size)
                    return my_max_size;
                else
                    return remote_max_size;
            }
        }

        public async Task ReadImpl(ulong memorypos, object buffer, ulong bufferpos, ulong count, CancellationToken cancel=default(CancellationToken))
        {
            if (direction == MemberDefinition_Direction.writeonly)
            {
                throw new WriteOnlyMemberException("Write only member");
            }

            uint max_transfer_size = await GetMaxTransferSize(cancel).ConfigureAwait(false);
            uint max_elems = (max_transfer_size) / element_size;

            if (count <= max_elems)
            {
                //Transfer all data in one block
                var e = new MessageEntry(MessageEntryType.MemoryRead, MemberName);
                e.AddElement("memorypos", memorypos);
                e.AddElement("count", count);
                var ret = await stub.ProcessRequest(e, cancel).ConfigureAwait(false);
                UnpackReadResult(ret.FindElement("data").Data, buffer, bufferpos, count);
            }
            else
            {
                ulong blocks = count / max_elems;
                ulong blockrem = count % max_elems;

                for (ulong i = 0; i < blocks; i++)
                {
                    ulong bufferpos_i = bufferpos + max_elems * i;
                    ulong memorypos_i = memorypos + max_elems * i;

                    await ReadImpl(memorypos_i, buffer, bufferpos_i, max_elems).ConfigureAwait(false);

                }

                if (blockrem > 0)
                {
                    ulong bufferpos_i = bufferpos + max_elems * blocks;
                    ulong memorypos_i = memorypos + max_elems * blocks;

                    await ReadImpl(memorypos_i, buffer, bufferpos_i, blockrem).ConfigureAwait(false);
                }
            }
        }
        public async Task WriteImpl(ulong memorypos, object buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            if (direction == MemberDefinition_Direction.readonly_)
            {
                throw new ReadOnlyMemberException("Read only member");
            }

            ulong max_transfer_size = await GetMaxTransferSize(cancel).ConfigureAwait(false);
            ulong max_elems = max_transfer_size / element_size;

            if (count <= max_elems)
            {
                //Transfer all data in one block
                var e = new MessageEntry(MessageEntryType.MemoryWrite, MemberName);
                e.AddElement("memorypos", memorypos);
                e.AddElement("count", count);
                e.AddElement("data", PackWriteRequest(buffer, bufferpos, count));

                var ret = await stub.ProcessRequest(e, cancel).ConfigureAwait(false);
            }
            else
            {
                if (GetBufferLength(buffer) - bufferpos < count)
                    throw new ArgumentOutOfRangeException("");

                ulong blocks = count / max_elems;
                ulong blockrem = count % max_elems;

                for (ulong i = 0; i < blocks; i++)
                {
                    ulong bufferpos_i = bufferpos + max_elems * i;
                    ulong memorypos_i = memorypos + max_elems * i;
                    await WriteImpl(memorypos_i, buffer, bufferpos_i, max_elems).ConfigureAwait(false);
                }

                if (blockrem > 0)
                {
                    ulong bufferpos_i = bufferpos + max_elems * blocks;
                    ulong memorypos_i = memorypos + max_elems * blocks;
                    await WriteImpl(memorypos_i, buffer, bufferpos_i, blockrem).ConfigureAwait(false);
                }
            }
        }
                
        protected abstract void UnpackReadResult(object res, object buffer, ulong bufferpos, ulong count);
        protected abstract object PackWriteRequest(object buffer, ulong bufferpos, ulong count);
        protected abstract ulong GetBufferLength(object buffer);
    }

    internal class ArrayMemoryClientImpl<T> : ArrayMemoryClientImplBase
    {
        internal ArrayMemoryClientImpl(string membername, ServiceStub stub, MemberDefinition_Direction direction) 
            : base(membername, stub, DataTypeUtil.TypeIDFromType(typeof(T)), DataTypeUtil.size(DataTypeUtil.TypeIDFromType(typeof(T))), direction)
        {
        }

        protected override ulong GetBufferLength(object buffer)
        {
            return (ulong)((T[])buffer).LongLength;
        }

        protected override object PackWriteRequest(object buffer, ulong bufferpos, ulong count)
        {
            var buffer1 = (T[])buffer;
            if (bufferpos == 0 && buffer1.LongLength == (long)count)
            {
                return buffer1;
            }
            else if ((buffer1.LongLength - (long)(bufferpos)) >= (long)(count))
            {
                var data = new T[count];
                Array.Copy(buffer1, (long)bufferpos, data, 0, (long)count);                
                return data;
            }
            else
            {
                throw new ArgumentOutOfRangeException("");
            }
        }

        protected override void UnpackReadResult(object res, object buffer, ulong bufferpos, ulong count)
        {
            var data = (T[])res;
            var buffer1 = (T[])buffer;
            Array.Copy(data, 0, buffer1, (long)bufferpos, (long)count);
        }
    }

    public class ArrayMemoryClient<T> : ArrayMemory<T>
    {
        private ArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public ArrayMemoryClient(string membername, ServiceStub stub, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new ArrayMemoryClientImpl<T>(membername, stub, direction);
        }

        public override void Attach(T[] memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetLength();   
        }
                
        private Task<uint> GetMaxTransferSize(CancellationToken cancel)
        {
            return impl.GetMaxTransferSize();
        }

        public override Task Read(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel=default(CancellationToken))
        {
            return impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel);
        }

        public override Task Write(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            return impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel);
        }
    }


    internal abstract class MultiDimArrayMemoryClientImplBase
    {

        protected string m_MemberName;
        protected ServiceStub stub;
        protected MemberDefinition_Direction direction;
        protected DataTypes element_type;
        protected uint element_size;

        public string MemberName { get => m_MemberName; }
        public MemberDefinition_Direction Direction { get => direction; }

        protected MultiDimArrayMemoryClientImplBase(string membername, ServiceStub stub, DataTypes element_type, uint element_size, MemberDefinition_Direction direction)
        {
            this.stub = stub;
            m_MemberName = membername;
            this.direction = direction;
            this.element_type = element_type;
            this.element_size = element_size;
        }

        public virtual async Task<ulong> GetDimCount(CancellationToken cancel=default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.MemoryGetParam, MemberName);
            m.AddElement("parameter", "DimCount");
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            return ret.FindElement("return").CastData<ulong[]>()[0];
        }

        public virtual async Task<ulong[]> GetDimensions(CancellationToken cancel= default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.MemoryGetParam, MemberName);
            m.AddElement("parameter", "Dimensions");
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            return ret.FindElement("return").CastData<ulong[]>();
        }
        
        protected bool max_size_read;
        protected uint remote_max_size;
        public async Task<uint> GetMaxTransferSize(CancellationToken cancel = default(CancellationToken))
        {
            uint my_max_size = stub.rr_node.MemoryMaxTransferSize;
            lock (this)
            {
                if (max_size_read)
                {
                    if (remote_max_size > my_max_size)
                        return my_max_size;
                    else
                        return remote_max_size;
                }
            }

            var m = new MessageEntry(MessageEntryType.MemoryGetParam, MemberName);
            m.AddElement("parameter", "MaxTransferSize");
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            var remote_max_size1 = ret.FindElement("return").CastData<uint[]>()[0];
            lock (this)
            {
                if (!max_size_read)
                {
                    remote_max_size = remote_max_size1;
                    max_size_read = true;
                }

                if (remote_max_size > my_max_size)
                    return my_max_size;
                else
                    return remote_max_size;
            }
        }

        public async Task ReadImpl(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            if (direction == MemberDefinition_Direction.writeonly)
            {
                throw new WriteOnlyMemberException("Write only member");
            }

            uint max_transfer_size = await GetMaxTransferSize(cancel).ConfigureAwait(false);

            ulong elemcount = count.Aggregate((ulong)1,(x,y) => x*y);            
            ulong max_elems = max_transfer_size / element_size;

            if (elemcount <= max_elems)
            {

                //Transfer all data in one block
                var e = new MessageEntry(MessageEntryType.MemoryRead, MemberName);
                e.AddElement("memorypos", memorypos);
                e.AddElement("count", count);
                var ret = await stub.ProcessRequest(e, cancel).ConfigureAwait(false);

                UnpackReadResult(ret.FindElement("data").Data, buffer, bufferpos, count, elemcount);

            }
            else
            {
                //We need to read the array in chunks.  This is a little complicated...

                int split_dim;
                ulong split_dim_block;
                ulong split_elem_count;
                int splits_count;
                int split_remainder;
                ulong[] block_count;
                ulong[] block_count_edge;

                CalculateMatrixBlocks(element_size, count, max_elems, out split_dim, out split_dim_block, out split_elem_count, out splits_count, out split_remainder, out block_count, out block_count_edge);

                bool done = false;
                var current_pos = new ulong[count.Length];

                while (!done)
                {
                    for (uint i = 0; i < splits_count; i++)
                    {
                        current_pos[split_dim] = split_dim_block * i;

                        var current_buf_pos = new ulong[bufferpos.Length];
                        var current_mem_pos = new ulong[bufferpos.Length];

                        for (long j = 0; j < current_buf_pos.LongLength; j++)
                        {
                            current_buf_pos[j] = current_pos[j] + bufferpos[j];
                            current_mem_pos[j] = current_pos[j] + memorypos[j];
                        }

                        await ReadImpl(current_mem_pos, buffer, current_buf_pos, block_count).ConfigureAwait(false);
                    }

                    if (split_remainder != 0)
                    {
                        current_pos[split_dim] = split_dim_block * (ulong)splits_count;
                        var current_buf_pos = new ulong[bufferpos.Length];
                        var current_mem_pos = new ulong[bufferpos.Length];

                        for (long j = 0; j < current_buf_pos.LongLength; j++)
                        {
                            current_buf_pos[j] = current_pos[j] + bufferpos[j];
                            current_mem_pos[j] = current_pos[j] + memorypos[j];
                        }

                        await ReadImpl(current_mem_pos, buffer, current_buf_pos, block_count_edge).ConfigureAwait(false);
                    }

                    if (split_dim == count.Length - 1)
                    {
                        done = true;
                    }
                    else
                    {
                        current_pos[split_dim + 1]++;
                        if (current_pos[split_dim + 1] >= count[split_dim + 1])
                        {
                            if (split_dim + 1 == count.Length - 1)
                            {
                                done = true;
                            }
                            else
                            {
                                current_pos[split_dim + 1] = 0;
                                for (int j = split_dim + 2; j < count.Length; j++)
                                {
                                    if (current_pos[j - 1] >= count[j - 1])
                                    {
                                        current_pos[j]++;
                                    }
                                }
                                if (current_pos[count.Length - 1] >= count[count.Length - 1])
                                    done = true;
                            }
                        }
                    }
                }
            }
        }

        public async Task WriteImpl(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            if (direction == MemberDefinition_Direction.readonly_)
            {
                throw new ReadOnlyMemberException("Read only member");
            }

             uint max_transfer_size = await GetMaxTransferSize().ConfigureAwait(false);

            ulong elemcount = count.Aggregate((ulong)1, (x,y) => x*y);
            
            uint max_elems = max_transfer_size / element_size;

            if (elemcount <= max_elems)
            {

                //Transfer all data in one block
                var e = new MessageEntry(MessageEntryType.MemoryWrite, MemberName);
                e.AddElement("memorypos", memorypos);
                e.AddElement("count", count);

                e.AddElement("data", PackWriteRequest(buffer, bufferpos, count, elemcount));

                var ret = stub.ProcessRequest(e,cancel);

            }
            else
            {
                int split_dim;
                ulong split_dim_block;
                ulong split_elem_count;
                int splits_count;
                int split_remainder;
                ulong[] block_count;
                ulong[] block_count_edge;

                CalculateMatrixBlocks(element_size, count, max_elems, out split_dim, out split_dim_block, out split_elem_count, out splits_count, out split_remainder, out block_count, out block_count_edge);

                bool done = false;
                var current_pos = new ulong[count.Length];

                while (!done)
                {
                    for (uint i = 0; i < splits_count; i++)
                    {
                        current_pos[split_dim] = split_dim_block * i;

                        var current_buf_pos = new ulong[bufferpos.Length];
                        var current_mem_pos = new ulong[bufferpos.Length];

                        for (long j = 0; j < current_buf_pos.LongLength; j++)
                        {
                            current_buf_pos[j] = current_pos[j] + bufferpos[j];
                            current_mem_pos[j] = current_pos[j] + memorypos[j];
                        }

                        await WriteImpl(current_mem_pos, buffer, current_buf_pos, block_count).ConfigureAwait(false);
                    }

                    if (split_remainder != 0)
                    {
                        current_pos[split_dim] = split_dim_block * (ulong)splits_count;
                        var current_buf_pos = new ulong[bufferpos.Length];
                        var current_mem_pos = new ulong[bufferpos.Length];

                        for (long j = 0; j < current_buf_pos.LongLength; j++)
                        {
                            current_buf_pos[j] = current_pos[j] + bufferpos[j];
                            current_mem_pos[j] = current_pos[j] + memorypos[j];
                        }

                        await WriteImpl(current_mem_pos, buffer, current_buf_pos, block_count_edge).ConfigureAwait(false);
                    }

                    if (split_dim == (count.Length - 1))
                    {
                        done = true;
                    }
                    else
                    {
                        current_pos[split_dim + 1]++;
                        if (current_pos[split_dim + 1] >= count[split_dim + 1])
                        {
                            if (split_dim + 1 == (count.Length - 1))
                            {
                                done = true;
                            }
                            else
                            {
                                current_pos[split_dim + 1] = 0;
                                for (int j = split_dim + 2; j < count.Length; j++)
                                {
                                    if (current_pos[j - 1] >= count[j - 1])
                                    {
                                        current_pos[j]++;
                                    }
                                }
                                if (current_pos[count.LongLength - 1] >= count[count.LongLength - 1])
                                    done = true;
                            }
                        }
                    }
                }
            }
        }

        private static void CalculateMatrixBlocks(uint element_size, ulong[] count, ulong max_elems, out int split_dim, out ulong split_dim_block, out ulong split_elem_count, out int splits_count, out int split_remainder, out ulong[] block_count, out ulong[] block_count_edge)
        {
            split_elem_count = 1;
            split_dim = -1;
            split_dim_block = 0;
            bool split_dim_found = false;
            block_count = new ulong[count.Length];
            splits_count = 0;
            split_remainder = 0;
            for (int i = 0; i < count.Length; i++)
            {
                if (!split_dim_found)
                {
                    ulong temp_elem_count1 = split_elem_count * count[i];
                    if (temp_elem_count1 > max_elems)
                    {
                        split_dim = i;
                        split_dim_block = max_elems / split_elem_count;
                        split_dim_found = true;
                        block_count[i] = split_dim_block;
                        splits_count = (int)(count[i] / split_dim_block);
                        split_remainder = (int)(count[i] % split_dim_block);
                    }
                    else
                    {
                        split_elem_count = temp_elem_count1;
                        block_count[i] = count[i];
                    }
                }
                else
                {
                    block_count[i] = 1;
                }
            }

            block_count_edge = new ulong[block_count.Length];
            Array.Copy(block_count, block_count_edge, block_count.Length);
            block_count_edge[split_dim] = count[split_dim] % split_dim_block;
        }

        protected abstract void UnpackReadResult(object res, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count);
        protected abstract object PackWriteRequest(object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count);

    }

    internal class MultiDimArrayMemoryClientImpl<T> : MultiDimArrayMemoryClientImplBase
    {
        internal MultiDimArrayMemoryClientImpl(string membername, ServiceStub stub, MemberDefinition_Direction direction)
            : base(membername, stub, DataTypeUtil.TypeIDFromType(typeof(T)), DataTypeUtil.size(DataTypeUtil.TypeIDFromType(typeof(T))), direction)
        {
        }

        protected override object PackWriteRequest(object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (MultiDimArray)(buffer);

            bool equ = true;
            for (long i = 0; i < count.LongLength; i++)
            {
                if (bufferpos[i] != 0 || buffer1.Dims[i] != count[i])
                {
                    equ = false;
                    break;
                }
            }

            if (equ)
            {
                return stub.rr_node.PackMultiDimArray(buffer1);
            }
            else
            {
                var data = new MultiDimArray(count.Select(x=>(uint)x).ToArray(), new T[elem_count]);

                buffer1.RetrieveSubArray(bufferpos.Select(x=>(uint)x).ToArray(), data, new uint[count.Length], count.Select(x=>(uint)x).ToArray());
                return stub.rr_node.PackMultiDimArray(data);
            }
        }

        protected override void UnpackReadResult(object res, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (MultiDimArray)buffer;
            var data = stub.rr_node.UnpackMultiDimArray((MessageElementNestedElementList)res);

            var data2 = new MultiDimArrayMemory<T>(data);
            data2.Read(new ulong[count.Length], buffer1, bufferpos, count);
        }
    }

    public class MultiDimArrayMemoryClient<T> : MultiDimArrayMemory<T>
    {
        private MultiDimArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public MultiDimArrayMemoryClient(string membername, ServiceStub stub, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new MultiDimArrayMemoryClientImpl<T>(membername, stub, direction);
        }
               
        public override void Attach(MultiDimArray memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimensions(cancel);  
        }

        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimCount(cancel);
        }
        
        private Task<uint> GetMaxTransferSize(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetMaxTransferSize(cancel);
        }

        public override async Task Read(ulong[] memorypos, MultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }
        
        public override async Task Write(ulong[] memorypos, MultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }
    }

    /**
    <summary>
    Multidimensional pod random access memory region
    </summary>
    <remarks>
    <para>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    </para>
    <para>
    The PodMultiDimArrayMemory class is used to represent a multidimensional
    pod array. Single dimensional pod arrays should use PodArrayMemory.
    Type T must be declared in a service definition using the `pod`
    keyword, and generated using RobotRaconteurGen.
    </para>
    <para>
    PodMultiDimArrayMemory instances are attached to an MultiDimArray,
    either when constructed or later using Attach().
    </para>
    <para>
    PodMultiDimArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </para>
    </remarks>
    <typeparam name="T" />
    */
    [PublicApi] 
    public class PodArrayMemory<T> : ArrayMemory<T> where T : struct
    {
        /// <summary>
        /// Construct an empty PodArrayMemory
        /// </summary>
        [PublicApi]
        public PodArrayMemory() : base()
        {
        }
        /**
        <summary>
        Construct a new PodMultiDimArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public PodArrayMemory(T[] memory) : base(memory)
        {
        }
    }

    internal class PodArrayMemoryClientImpl<T> : ArrayMemoryClientImplBase where T: struct
    {
        internal PodArrayMemoryClientImpl(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction)
            : base(membername, stub, DataTypes.pod_t, element_size, direction)
        {
        }

        protected override ulong GetBufferLength(object buffer)
        {
            return (ulong)((T[])buffer).LongLength;
        }

        protected override object PackWriteRequest(object buffer, ulong bufferpos, ulong count)
        {
            var buffer1 = (T[])buffer;            
            var o = new T[count];
            Array.Copy(buffer1, (long)bufferpos, o, 0, (long)count);
            return stub.rr_node.PackPodArray(o, stub.RRContext);           
        }

        protected override void UnpackReadResult(object res, object buffer, ulong bufferpos, ulong count)
        {            
            var data = stub.rr_node.UnpackPodArray<T>((MessageElementNestedElementList)res, stub.RRContext);
            var buffer1 = (T[])buffer;
            Array.Copy(data, 0, buffer1, (long)bufferpos, (long)count);
        }
    }

    public class PodArrayMemoryClient<T> : PodArrayMemory<T>  where T: struct
    {
        private PodArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public PodArrayMemoryClient(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new PodArrayMemoryClientImpl<T>(membername, stub, element_size, direction);
        }

        public override void Attach(T[] memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetLength();
        }

        private Task<uint> GetMaxTransferSize(CancellationToken cancel)
        {
            return impl.GetMaxTransferSize();
        }

        public override Task Read(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            return impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel);
        }

        public override Task Write(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            return impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel);
        }
    }
    /**
    <summary>
    Multidimensional pod random access memory region
    </summary>
    <remarks>
    <para>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    </para>
    <para>
    The PodMultiDimArrayMemory class is used to represent a multidimensional
    pod array. Single dimensional pod arrays should use PodArrayMemory.
    Type T must be declared in a service definition using the `pod`
    keyword, and generated using RobotRaconteurGen.
    </para>
    <para>
    PodMultiDimArrayMemory instances are attached to an MultiDimArray,
    either when constructed or later using Attach().
    </para>
    <para>
    PodMultiDimArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </para>
    </remarks>
    <typeparam name="T" />
    */
    [PublicApi]
    public class PodMultiDimArrayMemory<T> : MultiDimArrayMemoryBase where T : struct
    {
        private PodMultiDimArray multimemory;
        /**
        <summary>
        Construct a new PodMultiDimArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public PodMultiDimArrayMemory()
        {
        }
        /**
        <summary>
        Construct a new PodMultiDimArrayMemory instance attached to a PodMultiDimArray
        </summary>
        <remarks>
        New instance will be constructed attached to an array.
        </remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public PodMultiDimArrayMemory(PodMultiDimArray memory)
        {
            multimemory = memory;
        }
        /**
        <summary>
        Attach PodMultiDimArrayMemory instance to a PodMultiDimArray
        </summary>
        <remarks>None</remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public virtual void Attach(PodMultiDimArray memory)
        {
            this.multimemory = memory;
        }
        /**
        <summary>
        Dimensions of the memory array
        </summary>
        <remarks>
        <para>
        Returns the dimensions (shape) of the memory array
        </para>
        <para>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </para>
        </remarks>
        */

        [PublicApi]
        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(multimemory.Dims.Select(x => (ulong)x).ToArray());
        }
        /**
        <summary>
        The number of dimensions in the memory array
        </summary>
        <remarks>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </remarks>
        */

        [PublicApi]
        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((ulong)multimemory.Dims.Length);
        }
        /**
        <summary>
        Read a block from a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Read a block of a multidimensional array memory into a supplied buffer multidimensional array.
        The start positions and count of the read are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to read</param>
        <param name="buffer">The buffer to receive the read data</param>
        <param name="bufferpos">The start position in the buffer to write the data</param>
        <param name="count">The count of array elements to read</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Read(ulong[] memorypos, PodMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.RetrieveSubArray(memorypos.Select(x => (uint)x).ToArray(), buffer, bufferpos.Select(x => (uint)x).ToArray(), count.Select(x => (uint)x).ToArray());
            return Task.FromResult(0);
        }
        /**
        <summary>
        Write a segment to a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Writes a segment to a multidimensional array memory from a supplied buffer
        multidimensional array. The start positions and count
        of the write are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to write</param>
        <param name="buffer">The buffer to write the data from</param>
        <param name="bufferpos">The start position in the buffer to read the data</param>
        <param name="count">The count of array elements to write</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Write(ulong[] memorypos, PodMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.AssignSubArray(memorypos.Select(x => (uint)x).ToArray(), buffer, bufferpos.Select(x => (uint)x).ToArray(), count.Select(x => (uint)x).ToArray());
            return Task.FromResult(0);
        }
    }

    internal class PodMultiDimArrayMemoryClientImpl<T> : MultiDimArrayMemoryClientImplBase where T : struct
    {
        internal PodMultiDimArrayMemoryClientImpl(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction)
            : base(membername, stub, DataTypes.pod_t, element_size, direction)
        {
        }

        protected override object PackWriteRequest(object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (PodMultiDimArray)(buffer);

            bool equ = true;
            for (long i = 0; i < count.LongLength; i++)
            {
                if (bufferpos[i] != 0 || buffer1.Dims[i] != count[i])
                {
                    equ = false;
                    break;
                }
            }

            if (equ)
            {
                return stub.rr_node.PackPodMultiDimArray<T>(buffer1, stub.RRContext);
            }
            else
            {
                var data = new PodMultiDimArray(count.Select(x => (uint)x).ToArray(), new T[elem_count]);

                buffer1.RetrieveSubArray(bufferpos.Select(x => (uint)x).ToArray(), data, new uint[count.Length], count.Select(x => (uint)x).ToArray());
                return stub.rr_node.PackPodMultiDimArray<T>(data, stub.RRContext);
            }
        }

        protected override void UnpackReadResult(object res, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (PodMultiDimArray)buffer;
            var data = stub.rr_node.UnpackPodMultiDimArray<T>((MessageElementNestedElementList)res, stub.RRContext);

            var data2 = new PodMultiDimArrayMemory<T>(data);
            data2.Read(new ulong[count.Length], buffer1, bufferpos, count);
        }
    }

    public class PodMultiDimArrayMemoryClient<T> : PodMultiDimArrayMemory<T> where T : struct
    {
        private PodMultiDimArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public PodMultiDimArrayMemoryClient(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new PodMultiDimArrayMemoryClientImpl<T>(membername, stub, element_size, direction);
        }

        public override void Attach(PodMultiDimArray memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimensions(cancel);
        }

        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimCount(cancel);
        }

        private Task<uint> GetMaxTransferSize(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetMaxTransferSize(cancel);
        }

        public override async Task Read(ulong[] memorypos, PodMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }

        public override async Task Write(ulong[] memorypos, PodMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }
    }

    public class PodArrayMemoryServiceSkel<T> : ArrayMemoryServiceSkelBase where T : struct
    {
        public PodArrayMemoryServiceSkel(string membername, ServiceSkel skel, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
            : base(membername, skel, DataTypes.pod_t, element_size, direction)
        {

        }

        protected override async Task<object> DoRead(ulong memorypos, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (PodArrayMemory<T>)mem;
            var buf1 = new T[count];
            await mem1.Read(memorypos, buf1, 0, count).ConfigureAwait(false);
            return skel.rr_node.PackPodArray(buf1, null);
        }

        protected override async Task DoWrite(ulong memorypos, object buffer, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (PodArrayMemory<T>)mem;
            var buf1 = skel.rr_node.UnpackPodArray<T>((MessageElementNestedElementList)buffer, null);
            await mem1.Write(memorypos, buf1, 0, count).ConfigureAwait(false);
        }
    }

    public class PodMultiDimArrayMemoryServiceSkel<T> : MultiDimArrayMemoryServiceSkelBase where T : struct
    {
        public PodMultiDimArrayMemoryServiceSkel(string membername, ServiceSkel skel, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
            : base(membername, skel, DataTypes.pod_t, element_size, direction)
        {
        }

        protected override async Task<object> DoRead(ulong[] memorypos, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (PodMultiDimArrayMemory<T>)mem;
            var buf1 = new PodMultiDimArray(count.Select(x => (uint)x).ToArray(), new T[elem_count]);
            await mem1.Read(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
            return skel.rr_node.PackPodMultiDimArray<T>(buf1, null);
        }

        protected override async Task DoWrite(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (PodMultiDimArrayMemory<T>)mem;
            var buf1 = skel.rr_node.UnpackPodMultiDimArray<T>((MessageElementNestedElementList)buffer, null);
            await mem1.Write(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
        }
}
    /**
    <summary>
    Single dimensional namedarray random access memory region
    </summary>
    <remarks>
    <para>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    </para>
    <para>
    The NamedArrayMemory class is used to represent a single dimensional named
    array. Multidimensional named arrays should use NamedMultiDimArrayMemory.
    Type T must be declared in a service definition using the `namedarray`
    keyword, and generated using RobotRaconteurGen.
    </para>
    <para>
    NamedArrayMemory instances are attached to an array, either when
    constructed or later using Attach().
    </para>
    <para>
    NamedArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </para>
    </remarks>
    <typeparam name="T">The namedarray type of the array</typeparam>
    */

        [PublicApi]
    public class NamedArrayMemory<T> : ArrayMemory<T> where T : struct
    {
        /**
        <summary>
        Construct a new NamedArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public NamedArrayMemory() : base()
        {
        }
        /**
        <summary>
        Construct a new NamedArrayMemory instance attached to an array
        </summary>
        <remarks>
        New instance will be constructed attached to an array.
        </remarks>
        <param name="memory">The array to attach</param>
        <returns />
        */
        [PublicApi] 
        public NamedArrayMemory(T[] memory) : base(memory)
        {
        }
    }

    internal class NamedArrayMemoryClientImpl<T> : ArrayMemoryClientImplBase where T : struct
    {
        internal NamedArrayMemoryClientImpl(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction)
            : base(membername, stub, DataTypes.namedarray_t, element_size, direction)
        {
        }

        protected override ulong GetBufferLength(object buffer)
        {
            return (ulong)((T[])buffer).LongLength;
        }

        protected override object PackWriteRequest(object buffer, ulong bufferpos, ulong count)
        {
            var buffer1 = (T[])buffer;
            var o = new T[count];
            Array.Copy(buffer1, (long)bufferpos, o, 0, (long)count);
            return stub.rr_node.PackNamedArray(o, stub.RRContext);
        }

        protected override void UnpackReadResult(object res, object buffer, ulong bufferpos, ulong count)
        {
            var data = stub.rr_node.UnpackNamedArray<T>((MessageElementNestedElementList)res, stub.RRContext);
            var buffer1 = (T[])buffer;
            Array.Copy(data, 0, buffer1, (long)bufferpos, (long)count);
        }
    }

    public class NamedArrayMemoryClient<T> : NamedArrayMemory<T> where T : struct
    {
        private NamedArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public NamedArrayMemoryClient(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new NamedArrayMemoryClientImpl<T>(membername, stub, element_size, direction);
        }

        public override void Attach(T[] memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong> GetLength(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetLength();
        }

        private Task<uint> GetMaxTransferSize(CancellationToken cancel)
        {
            return impl.GetMaxTransferSize();
        }

        public override Task Read(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            return impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel);
        }

        public override Task Write(ulong memorypos, T[] buffer, ulong bufferpos, ulong count, CancellationToken cancel = default(CancellationToken))
        {
            return impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel);
        }
    }
    /**
    <summary>
    Multidimensional namedarray random access memory region
    </summary>
    <remarks>
    <para>
    Memories represent random access memory regions that are typically
    represented as arrays of various shapes and types. Memories can be
    declared in service definition files using the `memory` member keyword
    within service definitions. Services expose memories to clients, and
    the nodes will proxy read, write, and parameter requests between the client
    and service. The node will also break up large requests to avoid the
    message size limit of the transport.
    </para>
    <para>
    The NamedMultiDimArrayMemory class is used to represent a multidimensional
    named array. Single dimensional named arrays should use NamedArrayMemory.
    Type T must be declared in a service definition using the `namedarray`
    keyword, and generated using RobotRaconteurGen.
    </para>
    <para>
    NamedMultiDimArrayMemory instances are attached to an NamedMultiDimArray,
    either when constructed or later using Attach().
    </para>
    <para>
    NamedMultiDimArrayMemory instances returned by clients are special implementations
    designed to proxy requests to the service. They cannot be attached
    to an arbitrary array.
    </para>
    </remarks>
    <typeparam name="T">The namedarray type of the array</typeparam>
    */

        [PublicApi]
    public class NamedMultiDimArrayMemory<T> : MultiDimArrayMemoryBase where T : struct
    {
        private NamedMultiDimArray multimemory;
        /**
        <summary>
        Construct a new NamedMultiDimArrayMemory instance
        </summary>
        <remarks>
        New instance will not be attached to an array.
        </remarks>
        */

        [PublicApi]
        public NamedMultiDimArrayMemory()
        {
        }
        /**
        <summary>
        Construct a new NamedMultiDimArrayMemory instance attached to an NamedMultiDimArray
        </summary>
        <remarks>
        New instance will be constructed attached to an array.
        </remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public NamedMultiDimArrayMemory(NamedMultiDimArray memory)
        {
            multimemory = memory;
        }
        /**
        <summary>
        Attach PodMultiDimArrayMemory instance to a PodMultiDimArray
        </summary>
        <remarks>None</remarks>
        <param name="memory">The array to attach</param>
        */

        [PublicApi]
        public virtual void Attach(NamedMultiDimArray memory)
        {
            this.multimemory = memory;
        }
        /**
        <summary>
        Dimensions of the memory array
        </summary>
        <remarks>
        <para>
        Returns the dimensions (shape) of the memory array
        </para>
        <para>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </para>
        </remarks>
        */

        [PublicApi]
        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(multimemory.Dims.Select(x => (ulong)x).ToArray());
        }
        /**
        <summary>
        The number of dimensions in the memory array
        </summary>
        <remarks>
        When used with a memory returned by a client, this function will
        call the service to execute the request.
        </remarks>
        */

        [PublicApi]
        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((ulong)multimemory.Dims.Length);
        }
        /**
        <summary>
        Read a block from a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Read a block of a multidimensional array memory into a supplied buffer multidimensional array.
        The start positions and count of the read are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to read</param>
        <param name="buffer">The buffer to receive the read data</param>
        <param name="bufferpos">The start position in the buffer to write the data</param>
        <param name="count">The count of array elements to read</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Read(ulong[] memorypos, NamedMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.RetrieveSubArray(memorypos.Select(x => (uint)x).ToArray(), buffer, bufferpos.Select(x => (uint)x).ToArray(), count.Select(x => (uint)x).ToArray());
            return Task.FromResult(0);
        }
        /**
        <summary>
        Write a segment to a multidimensional array memory
        </summary>
        <remarks>
        <para>
        Writes a segment to a multidimensional array memory from a supplied buffer
        multidimensional array. The start positions and count
        of the write are specified.
        </para>
        <para>
        When used with a memory returned by a client, this function will call
        the service to execute the request.
        </para>
        </remarks>
        <param name="memorypos">The start position in the memory array to write</param>
        <param name="buffer">The buffer to write the data from</param>
        <param name="bufferpos">The start position in the buffer to read the data</param>
        <param name="count">The count of array elements to write</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        public virtual Task Write(ulong[] memorypos, NamedMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            multimemory.AssignSubArray(memorypos.Select(x => (uint)x).ToArray(), buffer, bufferpos.Select(x => (uint)x).ToArray(), count.Select(x => (uint)x).ToArray());
            return Task.FromResult(0);
        }
    }

    internal class NamedMultiDimArrayMemoryClientImpl<T> : MultiDimArrayMemoryClientImplBase where T : struct
    {
        internal NamedMultiDimArrayMemoryClientImpl(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction)
            : base(membername, stub, DataTypes.namedarray_t, element_size, direction)
        {
        }

        protected override object PackWriteRequest(object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (NamedMultiDimArray)(buffer);

            bool equ = true;
            for (long i = 0; i < count.LongLength; i++)
            {
                if (bufferpos[i] != 0 || buffer1.Dims[i] != count[i])
                {
                    equ = false;
                    break;
                }
            }

            if (equ)
            {
                return stub.rr_node.PackNamedMultiDimArray<T>(buffer1, stub.RRContext);
            }
            else
            {
                var data = new NamedMultiDimArray(count.Select(x => (uint)x).ToArray(), new T[elem_count]);

                buffer1.RetrieveSubArray(bufferpos.Select(x => (uint)x).ToArray(), data, new uint[count.Length], count.Select(x => (uint)x).ToArray());
                return stub.rr_node.PackNamedMultiDimArray<T>(data, stub.RRContext);
            }
        }

        protected override void UnpackReadResult(object res, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count)
        {
            var buffer1 = (NamedMultiDimArray)buffer;
            var data = stub.rr_node.UnpackNamedMultiDimArray<T>((MessageElementNestedElementList)res, stub.RRContext);

            var data2 = new NamedMultiDimArrayMemory<T>(data);
            data2.Read(new ulong[count.Length], buffer1, bufferpos, count);
        }
    }

    public class NamedMultiDimArrayMemoryClient<T> : NamedMultiDimArrayMemory<T> where T : struct
    {
        private NamedMultiDimArrayMemoryClientImpl<T> impl;

        public string MemberName { get => impl.MemberName; }
        public MemberDefinition_Direction Direction { get => impl.Direction; }

        public NamedMultiDimArrayMemoryClient(string membername, ServiceStub stub, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
        {
            impl = new NamedMultiDimArrayMemoryClientImpl<T>(membername, stub, element_size, direction);
        }

        public override void Attach(NamedMultiDimArray memory)
        {
            throw new InvalidOperationException();
        }

        public override Task<ulong[]> GetDimensions(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimensions(cancel);
        }

        public override Task<ulong> GetDimCount(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetDimCount(cancel);
        }

        private Task<uint> GetMaxTransferSize(CancellationToken cancel = default(CancellationToken))
        {
            return impl.GetMaxTransferSize(cancel);
        }

        public override async Task Read(ulong[] memorypos, NamedMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.ReadImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }

        public override async Task Write(ulong[] memorypos, NamedMultiDimArray buffer, ulong[] bufferpos, ulong[] count, CancellationToken cancel = default(CancellationToken))
        {
            await impl.WriteImpl(memorypos, buffer, bufferpos, count, cancel).ConfigureAwait(false);
        }
    }

    public class NamedArrayMemoryServiceSkel<T> : ArrayMemoryServiceSkelBase where T : struct
    {
        public NamedArrayMemoryServiceSkel(string membername, ServiceSkel skel, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
            : base(membername, skel, DataTypes.namedarray_t, element_size, direction)
        {

        }

        protected override async Task<object> DoRead(ulong memorypos, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (NamedArrayMemory<T>)mem;
            var buf1 = new T[count];
            await mem1.Read(memorypos, buf1, 0, count).ConfigureAwait(false);
            return skel.rr_node.PackNamedArray(buf1, null);
        }

        protected override async Task DoWrite(ulong memorypos, object buffer, ulong bufferpos, ulong count, ArrayMemoryBase mem)
        {
            var mem1 = (NamedArrayMemory<T>)mem;
            var buf1 = skel.rr_node.UnpackNamedArray<T>((MessageElementNestedElementList)buffer, null);
            await mem1.Write(memorypos, buf1, 0, count).ConfigureAwait(false);
        }
    }

    public class NamedMultiDimArrayMemoryServiceSkel<T> : MultiDimArrayMemoryServiceSkelBase where T : struct
    {
        public NamedMultiDimArrayMemoryServiceSkel(string membername, ServiceSkel skel, uint element_size, MemberDefinition_Direction direction = MemberDefinition_Direction.both)
            : base(membername, skel, DataTypes.namedarray_t, element_size, direction)
        {
        }

        protected override async Task<object> DoRead(ulong[] memorypos, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (NamedMultiDimArrayMemory<T>)mem;
            var buf1 = new NamedMultiDimArray(count.Select(x => (uint)x).ToArray(), new T[elem_count]);
            await mem1.Read(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
            return skel.rr_node.PackNamedMultiDimArray<T>(buf1, null);
        }

        protected override async Task DoWrite(ulong[] memorypos, object buffer, ulong[] bufferpos, ulong[] count, ulong elem_count, MultiDimArrayMemoryBase mem)
        {
            var mem1 = (NamedMultiDimArrayMemory<T>)mem;
            var buf1 = skel.rr_node.UnpackNamedMultiDimArray<T>((MessageElementNestedElementList)buffer, null);
            await mem1.Write(memorypos, buf1, new ulong[count.Length], count).ConfigureAwait(false);
        }
    }

}
