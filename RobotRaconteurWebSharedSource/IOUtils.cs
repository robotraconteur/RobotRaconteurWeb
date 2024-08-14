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
using System.Text;
using RobotRaconteurWeb.Extensions;

#pragma warning disable 1591

namespace RobotRaconteurWeb
{
    public class ArrayBinaryWriter : BinaryWriter
    {
        const int bufsize = 60000;
        private byte[] abuffer = new byte[bufsize];

        private bool memstream = false;
        private MemoryStream s_memstream;
        private byte[] membuf;

        public ArrayBinaryWriter(Stream s, int length) : base(s)
        {

            limits.Push((uint)length);
            memstream = false;
        }

        public ArrayBinaryWriter(MemoryStream s, byte[] membuf1, int length) : base(s)
        {
            s_memstream = s;
            memstream = true;
            membuf = membuf1;
            limits.Push((uint)length);
        }

        public void WriteArray(Array a)
        {

            if (a != null)
            {
                if (a.Length > 0)
                {

                    if (a is CDouble[])
                    {
                        var a1 = (CDouble[])a;
                        var a2 = new double[a1.Length * 2];
                        for (int i = 0; i < a1.Length; i++)
                        {
                            a2[i * 2] = a1[i].Real;
                            a2[i * 2 + 1] = a1[i].Imag;
                        }
                        WriteArray(a2);
                        return;
                    }

                    if (a is CSingle[])
                    {
                        var a1 = (CSingle[])a;
                        var a2 = new float[a1.Length * 2];
                        for (int i = 0; i < a1.Length; i++)
                        {
                            a2[i * 2] = a1[i].Real;
                            a2[i * 2 + 1] = a1[i].Imag;
                        }
                        WriteArray(a2);
                        return;
                    }

                    int l = a.Length;
                    int bl = Buffer.ByteLength(a);

                    if (bl + Position > CurrentLimit) throw new IOException("Message write error");

                    if (memstream)
                    {

                        m_Position += (uint)bl;
                        //byte[] b = s_memstream.GetBuffer();
                        Buffer.BlockCopy(a, 0, membuf, (int)s_memstream.Position, bl);
                        s_memstream.Position = s_memstream.Position + bl;
                    }
                    else
                    {

                        if (a.GetType().GetElementType().ToString() == "byte")
                        {
                            Write((byte[])a);
                        }
                        else
                        {


                            int n = bl / bufsize;
                            int nm = bl % bufsize;



                            for (int i = 0; i < n; i++)
                            {

                                Buffer.BlockCopy(a, i * bufsize, abuffer, 0, bufsize);
                                Write(abuffer, 0, bufsize);



                            }

                            if (nm > 0)
                            {
                                Buffer.BlockCopy(a, (n) * bufsize, abuffer, 0, nm);
                                Write(abuffer, 0, nm);
                            }
                        }

                        m_Position += (uint)bl;
                    }
                }
            }
        }


        public static int GetStringByteCount8(string s)
        {
            return UTF8Encoding.UTF8.GetByteCount(s);
        }

        public void WriteString8(String s)
        {

            byte[] b = UTF8Encoding.UTF8.GetBytes(s);
            if (b.Length + Position > CurrentLimit) throw new IOException("Message write error");
            Write(b);
            m_Position += (uint)b.Length;

        }


        public void WriteNumber(Object n, DataTypes t)
        {
            switch (t)
            {

                case DataTypes.double_t:
                    Write((double)n);
                    return;
                case DataTypes.single_t:
                    Write((float)n);
                    return;
                case DataTypes.int8_t:
                    Write((sbyte)n);
                    return;
                case DataTypes.uint8_t:
                    Write((byte)n);
                    return;
                case DataTypes.int16_t:
                    Write((short)n);
                    return;
                case DataTypes.uint16_t:
                    Write((ushort)n);
                    return;
                case DataTypes.int32_t:
                    Write((int)n);
                    return;
                case DataTypes.uint32_t:
                    Write((uint)n);
                    return;
                case DataTypes.int64_t:
                    Write((long)n);
                    return;
                case DataTypes.uint64_t:
                    Write((ulong)n);
                    return;

            }

            throw new DataTypeException("Unknown data type to write");
        }

        public override void Write(byte[] buffer, int index, int count)
        {
            if (count + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(buffer, index, count);
            //m_Position += (uint)count;
        }

        public override void Write(double value)
        {
            if (8 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 8;
        }

        public override void Write(float value)
        {
            if (4 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 4;
        }

        public override void Write(byte value)
        {
            if (1 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 1;
        }

        public override void Write(sbyte value)
        {
            if (1 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 1;
        }

        public override void Write(short value)
        {
            if (2 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 2;
        }

        public override void Write(ushort value)
        {
            if (2 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 2;
        }

        public override void Write(int value)
        {
            if (4 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 4;
        }

        public override void Write(uint value)
        {
            if (4 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 4;
        }

        public override void Write(long value)
        {
            if (8 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 8;
        }

        public override void Write(ulong value)
        {
            if (8 + Position > CurrentLimit) throw new IOException("Message write error");
            base.Write(value);
            m_Position += 8;
        }

        Stack<uint> limits = new Stack<uint>();

        public uint CurrentLimit
        {
            get
            {
                return limits.Peek();
            }
        }

        protected uint m_Position = 0;

        public uint Position
        {
            get
            {
                return m_Position;
            }
        }

        public void PushRelativeLimit(uint limit)
        {
            if (Position + limit > CurrentLimit)
            {
                throw new IOException("Error reading message");
            }

            limits.Push(Position + limit);
        }

        public void PushAbsoluteLimit(uint limit)
        {
            if (limit > CurrentLimit)
            {
                throw new IOException("Error reading message");
            }

            limits.Push(limit);
        }

        public void PopLimit()
        {
            limits.Pop();
        }

        public int DistanceFromLimit
        {
            get
            {
                return (int)CurrentLimit - (int)m_Position;
            }
        }

        public void Reset(int length)
        {
            limits.Clear();
            limits.Push((uint)length);
            m_Position = 0;
        }

    }

    // <summary>
    public class ArrayBinaryReader : BinaryReader
    {
        const int bufsize = 60000;
        private byte[] abuffer = new byte[bufsize];

        private bool memstream = false;
        private MemoryStream s_memstream;

        private byte[] membuf;

        public ArrayBinaryReader(Stream s, int length) : base(s)
        {

            memstream = false;

            limits.Push((uint)length);
        }

        public ArrayBinaryReader(MemoryStream s, byte[] membuf1, int length) : base(s)
        {
            s_memstream = s;
            memstream = true;
            membuf = membuf1;
            limits.Push((uint)length);
        }

        public void ReadArray(Array a)
        {


            if (a != null)
            {
                if (a.Length > 0)
                {
                    if (a is CDouble[])
                    {
                        var a1 = (CDouble[])a;
                        var a2 = new double[a1.Length * 2];
                        ReadArray(a2);
                        for (int i = 0; i < a1.Length; i++)
                        {
                            a1[i] = new CDouble(a2[i * 2], a2[i * 2 + 1]);
                        }
                        return;
                    }

                    if (a is CSingle[])
                    {
                        var a1 = (CSingle[])a;
                        var a2 = new float[a1.Length * 2];
                        ReadArray(a2);
                        for (int i = 0; i < a1.Length; i++)
                        {
                            a1[i] = new CSingle(a2[i * 2], a2[i * 2 + 1]);
                        }
                        return;
                    }

                    int l = a.Length;
                    int bl = Buffer.ByteLength(a);


                    if (memstream)
                    {
                        if (Position + bl > CurrentLimit) throw new IOException("Message read error");

                        //byte[] b = s_memstream.GetBuffer();
                        Buffer.BlockCopy(membuf, (int)s_memstream.Position, a, 0, bl);
                        s_memstream.Position = s_memstream.Position + bl;

                        m_Position += (uint)bl;
                    }
                    else
                    {
                        if (Position + bl > CurrentLimit) throw new IOException("Message read error");
                        if (a.GetType().GetElementType().ToString() == "byte")
                        {
                            Read((byte[])a, 0, a.Length);
                        }
                        else
                        {



                            int n = bl / bufsize;
                            int nm = bl % bufsize;



                            for (int i = 0; i < n; i++)
                            {
                                Read(abuffer, 0, bufsize);
                                Buffer.BlockCopy(abuffer, 0, a, i * bufsize, bufsize);



                            }

                            if (nm > 0)
                            {
                                Read(abuffer, 0, nm);
                                Buffer.BlockCopy(abuffer, 0, a, (n) * bufsize, nm);

                            }
                        }
                        m_Position += (uint)bl;

                    }
                }
            }
        }

        public String ReadString8(uint l)
        {
            byte[] b = new byte[l];
            if (Position + b.Length > CurrentLimit) throw new IOException("Message read error");
            int n = Read(b, 0, b.Length);

            string s = UTF8Encoding.UTF8.GetString(b);
            if (s.Contains("\0"))
            {
                Console.WriteLine("null");
            }

            m_Position += (uint)b.Length;
            return s;

        }

        public Object ReadNumber(DataTypes t)
        {

            switch (t)
            {

                case DataTypes.double_t:
                    return ReadDouble();
                case DataTypes.single_t:
                    return ReadSingle();
                case DataTypes.int8_t:
                    return ReadSByte();
                case DataTypes.uint8_t:
                    return ReadByte();
                case DataTypes.int16_t:
                    return ReadInt16();
                case DataTypes.uint16_t:
                    return ReadUInt16();
                case DataTypes.int32_t:
                    return ReadInt32();
                case DataTypes.uint32_t:
                    return ReadUInt32();
                case DataTypes.int64_t:
                    return ReadInt64();
                case DataTypes.uint64_t:
                    return ReadUInt64();
            }

            throw new DataTypeException("Unknown data type to read");
        }

        public override int Read()
        {
            if (Position + 1 > CurrentLimit) throw new IOException("Message read error");
            var i = base.Read();
            //m_Position += 1;
            return i;
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            if (Position + count > CurrentLimit) throw new IOException("Message read error");
            var i = base.Read(buffer, index, count);
            //m_Position += (uint)i;
            return i;
        }

        public override double ReadDouble()
        {
            if (Position + 8 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadDouble();
            m_Position += 8;
            return i;
        }

        public override float ReadSingle()
        {
            if (Position + 4 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadSingle();
            m_Position += 4;
            return i;
        }

        public override sbyte ReadSByte()
        {
            if (Position + 1 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadSByte();
            m_Position += 1;
            return i;
        }

        public override byte ReadByte()
        {
            if (Position + 1 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadByte();
            m_Position += 1;
            return i;
        }

        public override short ReadInt16()
        {
            if (Position + 2 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadInt16();
            m_Position += 2;
            return i;
        }

        public override ushort ReadUInt16()
        {
            if (Position + 2 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadUInt16();
            m_Position += 2;
            return i;
        }

        public override int ReadInt32()
        {
            if (Position + 4 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadInt32();
            m_Position += 4;
            return i;
        }

        public override uint ReadUInt32()
        {
            if (Position + 4 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadUInt32();
            m_Position += 4;
            return i;
        }

        public override long ReadInt64()
        {
            if (Position + 8 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadInt64();
            m_Position += 8;
            return i;
        }

        public override ulong ReadUInt64()
        {
            if (Position + 8 > CurrentLimit) throw new IOException("Message read error");
            var i = base.ReadUInt64();
            m_Position += 8;
            return i;
        }

        Stack<uint> limits = new Stack<uint>();

        public uint CurrentLimit
        {
            get
            {
                return limits.Peek();
            }
        }

        protected uint m_Position = 0;

        public uint Position
        {
            get
            {
                return m_Position;
            }
        }

        public void PushRelativeLimit(uint limit)
        {
            if (Position + limit > CurrentLimit)
            {
                throw new IOException("Error reading message");
            }

            limits.Push(Position + limit);
        }

        public void PushAbsoluteLimit(uint limit)
        {
            if (limit > CurrentLimit)
            {
                throw new IOException("Error reading message");
            }

            limits.Push(limit);
        }

        public void PopLimit()
        {
            limits.Pop();
        }

        public int DistanceFromLimit
        {
            get
            {
                return (int)CurrentLimit - (int)m_Position;
            }
        }

        public void Reset(int length)
        {
            limits.Clear();
            limits.Push((uint)length);
            m_Position = 0;
        }
    }
}
