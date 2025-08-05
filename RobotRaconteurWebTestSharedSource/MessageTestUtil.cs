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
using System.Text;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    static class MessageTestUtil
    {

        static MessageElement CreateMessageElement(string name, object data)
        {
            return new MessageElement(name, data);
        }

        static MessageElement CreateMessageElement()
        {
            return new MessageElement();
        }

        static MessageEntry CreateMessageEntry(MessageEntryType type, string name)
        {
            return new MessageEntry(type, name);
        }

        static MessageEntry CreateMessageEntry()
        {
            return new MessageEntry();
        }

        public static Message CreateMessage()
        {
            return new Message();
        }

        public static MessageHeader CreateMessageHeader()
        {
            return new MessageHeader();
        }
        public static MessageElementNestedElementList CreateMessageElementNestedElementList(DataTypes type, string name, List<MessageElement> elements)
        {
            return new MessageElementNestedElementList(type, name, elements);
        }
        static MultiDimArray AllocateRRMultiDimArray<T>(uint[] dims, T[] data)
        {
            return new MultiDimArray(dims, data);
        }
        static public Message NewTestMessage()
        {
            var rand = new Random();

            var e1 = CreateMessageEntry(MessageEntryType.PropertyGetReq, "testprimitives");
            e1.ServicePath = "aservicepath";
            e1.RequestID = 134576;
            e1.MetaData = "md";

            // Test all primitive types
            double[] v1d = { 1, 2, 3, 4, 5, 7.45, 8.9832 };
            e1.AddElement("v1", (v1d));
            float[] v2d = { 1, 2, 34 };
            e1.AddElement("v2", (v2d));
            sbyte[] v3d = { 1, -2, 3, 127 };
            e1.AddElement("v3", (v3d));
            byte[] v4d = { 1, 2, 3, 4, 5 };
            e1.AddElement("v4", (v4d));
            short[] v5d = { 1, 2, 3, -4, 5, 19746, 9870 };
            e1.AddElement("v5", (v5d));
            ushort[] v6d = { 1, 2, 3, 4, 5, 19746, 9870 };
            e1.AddElement("v6", (v6d));
            int[] v7d = { 1, 2, 3, -4, 5, 19746, 9870, 2045323432 };
            e1.AddElement("v7", (v7d));
            uint[] v8d = { 1, 2, 3, 4, 5, 19746, 9870, 345323432 };
            e1.AddElement("v8", (v8d));
            long[] v9d = { 1, 2, 3, 4, 5, -19746, 9870, 9111111222345323432 };
            e1.AddElement("v9", (v9d));
            ulong[] v10d = { 1, 2, 3, 4, 5, 19746, 9870, 12111111222345323432u };
            e1.AddElement("v10", (v10d));
            e1.AddElement("v11", ("This is a test string"));
            var v12 = new uint[(1024 * 1024)];
            for (int i = 0; i < v12.Length; i++)
                v12[i] = (uint)i;
            e1.AddElement("v12", v12);

            // Test vector
            var e2 = CreateMessageEntry(MessageEntryType.FunctionCallReq, "testavector");
            e2.RequestID = 4563;
            e2.ServicePath = "aservicepath.o2";

            var mv = new List<MessageElement>();
            double[] mv_d1 = { 1, 2 };
            double[] mv_d2 = { 1000, -2000.10 };
            mv.Add(CreateMessageElement("0", (mv_d1)));
            mv.Add(CreateMessageElement("1", (mv_d2)));
            var v = CreateMessageElementNestedElementList(DataTypes.vector_t, "", mv);
            e2.AddElement("testavector", v);

            // Test dictionary
            var e3 = CreateMessageEntry(MessageEntryType.FunctionCallRes, "testadictionary");
            e3.RequestID = 4567;
            e3.ServicePath = "aservicepath.o3";

            var md = new List<MessageElement>();
            float[] md_d1 = { 1, 2 };
            float[] md_d2 = { 1000, -2000.10f };
            md.Add(CreateMessageElement("val1", (md_d1)));
            md.Add(CreateMessageElement("val2", (md_d2)));
            var d = CreateMessageElementNestedElementList(DataTypes.dictionary_t, "", md);
            e3.AddElement("testavector", d);

            // Test structure
            var e4 = CreateMessageEntry(MessageEntryType.EventReq, "testnamedarray");
            e4.RequestID = 4568;
            e4.ServicePath = "aservicepath.o4";

            var ms = new List<MessageElement>();
            long[] ms_d1 = { 1, 2, 3, 4, 5, -19746, 9870, 345323432 };
            ms.Add(CreateMessageElement("field1", (ms_d1)));
            ms.Add(CreateMessageElement("field2", v));
            var s = CreateMessageElementNestedElementList(DataTypes.structure_t, "RobotRaconteurTestService.TestStruct", ms);
            e4.AddElement("teststruct", s);

            // Test MultiDimArray
            var e5 = CreateMessageEntry(MessageEntryType.PipePacket, "testamultidimarray");
            e5.RequestID = 4569;
            e5.ServicePath = "aservicepath.o5";

            var real = new double[(125)];
            for (int i = 0; i < 125; i++)
                real[i] = (rand.NextDouble() - 0.5) * 1e5;

            uint[] dims1 = { 5, 5, 5 };
            uint[] dims2 = { 25, 5 };

            var a1 = AllocateRRMultiDimArray<double>((dims1), real);
            var a2 = AllocateRRMultiDimArray<double>((dims2), real);
            e5.AddElement("ar1", RobotRaconteurNode.s.PackMultiDimArray(a1));
            e5.AddElement("ar2", RobotRaconteurNode.s.PackMultiDimArray(a2));

            // Test list
            var e6 = CreateMessageEntry(MessageEntryType.PipePacket, "testalist");
            e6.RequestID = 459;
            e6.ServicePath = "aservicepath.o6";
            var ml = new List<MessageElement>();
            float[] md_l1 = { 1, 3 };
            float[] md_l2 = { 1003, -2000.10f };
            ml.Add(CreateMessageElement("val1", (md_l1)));
            ml.Add(CreateMessageElement("val2", (md_l2)));
            var l = CreateMessageElementNestedElementList(DataTypes.list_t, "", ml);
            e6.AddElement("testalist", l);

            // Create a new message
            var m = CreateMessage();
            var h = CreateMessageHeader();
            h.ReceiverEndpoint = 1023;
            h.SenderEndpoint = 9876;
            h.SenderNodeID = NodeID.NewUniqueID();
            h.ReceiverNodeID = NodeID.NewUniqueID();
            h.SenderNodeName = "Sender";
            h.ReceiverNodeName = "Recv";
            h.MetaData = "meta";

            m.header = h;
            m.entries.Add(e1);
            m.entries.Add(e2);
            m.entries.Add(e3);
            m.entries.Add(e4);
            m.entries.Add(e5);
            m.entries.Add(e6);

            return m;
        }

        static MessageElement MessageSerializationTest_NewRandomMessageElement(LFSRSeqGen rng, int depth)
        {
            MessageElement e = CreateMessageElement();
            if (rng.NextDist(0, 1) == 0 || depth > 2)
            {
                e.ElementType = (DataTypes)rng.NextDist(0, 11);
            }
            else
            {
                ushort t1 = (ushort)rng.NextDist(0, 4);
                switch (t1)
                {
                    case 0:
                        e.ElementType = DataTypes.structure_t;
                        break;
                    case 1:
                        e.ElementType = DataTypes.vector_t;
                        break;
                    case 2:
                        e.ElementType = DataTypes.dictionary_t;
                        break;
                    case 3:
                        e.ElementType = DataTypes.multidimarray_t;
                        break;
                    case 4:
                        e.ElementType = DataTypes.list_t;
                        break;
                }
            }

            e.ElementName = rng.NextStringVarLen(128);
            e.ElementTypeName = rng.NextStringVarLen(128);
            e.MetaData = rng.NextStringVarLen(128);

            switch (e.ElementType)
            {
                case DataTypes.void_t:
                    return e;

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
                    {
                        Array a = rng.NextArrayByTypeVarLen(e.ElementType, 256);
                        e.Data = (a);
                        return e;
                    }
                case DataTypes.structure_t:
                    {
                        var v = new List<MessageElement>();
                        int n = (int)rng.NextDist(1, 8);
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest_NewRandomMessageElement(rng, depth + 1));
                        }
                        e.Data = (CreateMessageElementNestedElementList(DataTypes.structure_t, rng.NextStringVarLen(128), v));
                        return e;
                    }
                case DataTypes.vector_t:
                    {
                        var v = new List<MessageElement>();
                        int n = (int)rng.NextDist(1, 8);
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest_NewRandomMessageElement(rng, depth + 1));
                        }
                        e.Data = (CreateMessageElementNestedElementList(DataTypes.vector_t, "", v));
                        return e;
                    }
                case DataTypes.dictionary_t:
                    {
                        var v = new List<MessageElement>();
                        int n = (int)rng.NextDist(1, 8);
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest_NewRandomMessageElement(rng, depth + 1));
                        }
                        e.Data = (CreateMessageElementNestedElementList(DataTypes.dictionary_t, "", v));
                        return e;
                    }
                case DataTypes.multidimarray_t:
                    {
                        var v = new List<MessageElement>();
                        int n = (int)rng.NextDist(1, 8);
                        if (n > 4)
                            n = 4;
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest_NewRandomMessageElement(rng, 10));
                        }
                        e.Data = (CreateMessageElementNestedElementList(DataTypes.multidimarray_t, "", v));
                        return e;
                    }
                case DataTypes.list_t:
                    {
                        var v = new List<MessageElement>();
                        int n = (int)rng.NextDist(1, 8);
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest_NewRandomMessageElement(rng, depth + 1));
                        }
                        e.Data = (CreateMessageElementNestedElementList(DataTypes.list_t, "", v));
                        return e;
                    }

                default:
                    throw new InvalidOperationException("Unexpected DataType");
            }
        }

        public static Message NewRandomTestMessage(LFSRSeqGen rng)
        {
            Message m = CreateMessage();
            MessageHeader h = CreateMessageHeader();
            m.header = h;

            byte[] b = new byte[16];
            for (int i = 0; i < 16; i++)
                b[i] = rng.NextUInt8();
            h.SenderNodeID = new NodeID(b);
            for (int i = 0; i < 16; i++)
                b[i] = rng.NextUInt8();
            h.ReceiverNodeID = new NodeID(b);
            h.SenderNodeName = rng.NextStringVarLen(64);
            h.ReceiverNodeName = rng.NextStringVarLen(64);

            h.SenderEndpoint = rng.NextUInt32();
            h.ReceiverEndpoint = rng.NextUInt32();
            h.MetaData = rng.NextStringVarLen(256);
            h.MessageID = rng.NextUInt16();
            h.MessageResID = rng.NextUInt16();
            h.EntryCount = (ushort)rng.NextDist(1, 4);

            // Add a large entry sometimes
            uint add_large_entry = rng.NextDist(1, 4);

            for (int i = 0; i < h.EntryCount; i++)
            {
                if (m.ComputeSize() > 10 * 1024 * 1024)
                    break;

                MessageEntry ee = CreateMessageEntry();

                ee.EntryType = (MessageEntryType)rng.NextDist(101, 120);

                ee.ServicePath = rng.NextStringVarLen(256);
                ee.RequestID = rng.NextUInt32();
                ee.Error = (MessageErrorType)rng.NextDist(1, 10);
                ee.MetaData = rng.NextStringVarLen(256);

                if (add_large_entry == 1 && i == 0)
                {
                    uint l = (uint)rng.NextDist(512 * 1024, 1024 * 1024);
                    var a = rng.NextUInt32Array(l);
                    var el = MessageSerializationTest_NewRandomMessageElement(rng, 10);
                    el.Data = (a);
                    ee.elements.Add(el);
                }
                else
                {
                    uint n1 = rng.NextDist(0, 16);
                    for (int j = 0; j < n1; j++)
                    {
                        var el = MessageSerializationTest_NewRandomMessageElement(rng, 0);
                        ee.elements.Add(el);
                    }
                }

                m.entries.Add(ee);
            }

            return m;
        }

        static byte MessageSerializationTest4_NewRandomMessageFlags(LFSRSeqGen rng)
        {
            byte o = 0;
            for (int i = 0; i < 8; i++)
            {
                o = (byte)((o << 1) | (byte)rng.NextDist(0, 1));
            }
            return o;
        }

        static byte MessageSerializationTest4_NewRandomFlags(LFSRSeqGen rng)
        {
            byte o = 0;
            for (int i = 0; i < 8; i++)
            {
                o = (byte)((o << 1) | (byte)rng.NextDist(0, 1));
            }
            return o;
        }

        static byte[] MessageSerializationTest4_NewRandomExtended(LFSRSeqGen rng, uint max_len)
        {
            uint l = rng.NextDist(0, max_len);

            List<byte> buf = new List<byte>((int)l);

            for (int i = 0; i < l; i++)
            {
                buf.Add(rng.NextUInt8());
            }

            return buf.ToArray();
        }

        public static MessageElement MessageSerializationTest4_NewRandomMessageElement(LFSRSeqGen rng, int depth)
        {
            MessageElement e = CreateMessageElement();
            e.ElementFlags = MessageSerializationTest4_NewRandomFlags(rng);
            e.ElementFlags &= (byte)((~0x40u) & 0XFF);
            if (rng.NextDist(0, 1) == 0 || depth > 2)
            {
                e.ElementType = (DataTypes)rng.NextDist(0, 14);
            }
            else
            {
                ushort t1 = (ushort)rng.NextDist(0, 9);
                switch (t1)
                {
                    case 0:
                        e.ElementType = DataTypes.structure_t;
                        break;
                    case 1:
                        e.ElementType = DataTypes.vector_t;
                        break;
                    case 2:
                        e.ElementType = DataTypes.dictionary_t;
                        break;
                    case 3:
                        e.ElementType = DataTypes.multidimarray_t;
                        break;
                    case 4:
                        e.ElementType = DataTypes.list_t;
                        break;
                    case 5:
                        e.ElementType = DataTypes.pod_t;
                        break;
                    case 6:
                        e.ElementType = DataTypes.pod_array_t;
                        break;
                    case 7:
                        e.ElementType = DataTypes.pod_multidimarray_t;
                        break;
                    case 8:
                        e.ElementType = DataTypes.namedarray_array_t;
                        break;
                    case 9:
                        e.ElementType = DataTypes.namedarray_multidimarray_t;
                        break;
                }
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.ElementNameStr) != 0)
            {
                e.ElementName = rng.NextStringVarLen(128);
                e.ElementFlags &= (byte)~MessageElementFlags.ElementNumber;
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.ElementNameCode) != 0)
            {
                e.ElementNameCode = rng.NextUInt32();
                e.ElementFlags &= (byte)~MessageElementFlags.ElementNumber;
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.ElementNumber) != 0)
            {
                e.ElementNumber = rng.NextInt32();
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.ElementTypeNameStr) != 0)
            {
                e.ElementTypeName = rng.NextStringVarLen(128);
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.ElementTypeNameCode) != 0)
            {
                e.ElementTypeNameCode = rng.NextUInt32();
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.MetaInfo) != 0)
            {
                e.MetaData = rng.NextStringVarLen(128);
            }

            if ((e.ElementFlags & (byte)MessageElementFlags.Extended) != 0)
            {
                e.Extended = MessageSerializationTest4_NewRandomExtended(rng, 32);
            }

            switch (e.ElementType)
            {
                case DataTypes.void_t:
                    return e;

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
                case DataTypes.cdouble_t:
                case DataTypes.csingle_t:
                case DataTypes.bool_t:
                    {
                        var a = rng.NextArrayByTypeVarLen(e.ElementType, 256);
                        e.Data = (a);
                        return e;
                    }
                case DataTypes.structure_t:
                case DataTypes.vector_t:
                case DataTypes.dictionary_t:
                case DataTypes.multidimarray_t:
                case DataTypes.list_t:
                case DataTypes.pod_t:
                case DataTypes.pod_array_t:
                case DataTypes.pod_multidimarray_t:
                case DataTypes.namedarray_array_t:
                case DataTypes.namedarray_multidimarray_t:
                    {
                        List<MessageElement> v = new List<MessageElement>();
                        uint n = rng.NextDist(1, 8);
                        for (int i = 0; i < n; i++)
                        {
                            v.Add(MessageSerializationTest4_NewRandomMessageElement(rng, depth + 1));
                        }
                        e.Data = (CreateMessageElementNestedElementList(e.ElementType, rng.NextStringVarLen(128), v));
                        return e;
                    }
                default:
                    throw new Exception("Unexpected DataType");
            }
        }

        public static Message NewRandomTestMessage4(LFSRSeqGen rng)
        {
            Message m = CreateMessage();
            MessageHeader h = CreateMessageHeader();
            m.header = h;
            h.MessageFlags_ = MessageSerializationTest4_NewRandomMessageFlags(rng);
            /*h.MessageFlags &= ~MessageFlags_ROUTING_INFO;
            h.MessageFlags &= ~MessageFlags_ENDPOINT_INFO;
            h.MessageFlags &= ~MessageFlags_PRIORITY;
            h.MessageFlags &= ~MessageFlags_META_INFO;
            h.MessageFlags &= ~MessageFlags_EXTENDED;*/
            h.MessageFlags_ &= (byte)~MessageFlags.StringTable;
            if ((h.MessageFlags_ & (byte)MessageFlags.Priority) != 0)
            {
                h.Priority = rng.NextUInt16();
            }
            if ((h.MessageFlags_ & (byte)MessageFlags.RoutingInfo) != 0)
            {
                byte[] b = new byte[16];
                for (int i = 0; i < 16; i++)
                    b[i] = rng.NextUInt8();
                h.SenderNodeID = new NodeID(b);
                for (int i = 0; i < 16; i++)
                    b[i] = rng.NextUInt8();
                h.ReceiverNodeID = new NodeID(b);
                h.SenderNodeName = rng.NextStringVarLen(64);
                h.ReceiverNodeName = rng.NextStringVarLen(64);
            }

            if ((h.MessageFlags_ & (byte)MessageFlags.EndpointInfo) != 0)
            {
                h.SenderEndpoint = rng.NextUInt32();
                h.ReceiverEndpoint = rng.NextUInt32();
            }

            if ((h.MessageFlags_ & (byte)MessageFlags.MetaInfo) != 0)
            {
                h.MetaData = rng.NextStringVarLen(256);
                h.MessageID = rng.NextUInt16();
                h.MessageResID = rng.NextUInt16();
            }

            if ((h.MessageFlags_ & (byte)MessageFlags.StringTable) != 0)
            {
                throw new NotImplementedException();
            }

            if ((h.MessageFlags_ & (byte)MessageFlags.MultipleEntries) != 0)
            {
                h.EntryCount = (ushort)rng.NextDist(1, 4);
            }
            else
            {
                h.EntryCount = 1;
            }

            if ((h.MessageFlags_ & (byte)MessageFlags.Extended) != 0)
            {
                h.Extended = MessageSerializationTest4_NewRandomExtended(rng, 32);
            }

            // Add a large entry sometimes
            uint add_large_entry = rng.NextDist(1, 4);

            for (int i = 0; i < h.EntryCount; i++)
            {
                if (m.ComputeSize4() > 10 * 1024 * 1024)
                    break;

                MessageEntry ee = CreateMessageEntry();

                ee.EntryFlags = MessageSerializationTest4_NewRandomFlags(rng);

                ee.EntryType = (MessageEntryType)rng.NextDist(101, 120);

                if ((ee.EntryFlags & (byte)MessageEntryFlags.ServicePathStr) != 0)
                {
                    ee.ServicePath = rng.NextStringVarLen(256);
                }
                if ((ee.EntryFlags & (byte)MessageEntryFlags.ServicePathCode) != 0)
                {
                    ee.ServicePathCode = rng.NextUInt32();
                }
                if ((ee.EntryFlags & (byte)MessageEntryFlags.MemberNameStr) != 0)
                {
                    ee.MemberName = rng.NextStringVarLen(256);
                }
                if ((ee.EntryFlags & (byte)MessageEntryFlags.MemberNameCode) != 0)
                {
                    ee.MemberNameCode = rng.NextUInt32();
                }

                if ((ee.EntryFlags & (byte)MessageEntryFlags.RequestID) != 0)
                {
                    ee.RequestID = rng.NextUInt32();
                }
                if ((ee.EntryFlags & (byte)MessageEntryFlags.Error) != 0)
                {
                    ee.Error = (MessageErrorType)rng.NextDist(1, 10);
                }
                if ((ee.EntryFlags & (byte)MessageEntryFlags.MetaInfo) != 0)
                {
                    ee.MetaData = rng.NextStringVarLen(256);
                }

                if ((ee.EntryFlags & (byte)MessageFlags.Extended) != 0)
                {
                    ee.Extended = MessageSerializationTest4_NewRandomExtended(rng, 32);
                }

                if (add_large_entry == 1 && i == 0)
                {
                    uint l = rng.NextDist(512 * 1024, 1024 * 1024);
                    uint[] a = new uint[l];
                    for (int j = 0; j < l; j++)
                    {
                        a[j] = rng.NextUInt32();
                    }

                    MessageElement el = MessageSerializationTest4_NewRandomMessageElement(rng, 10);
                    el.Data = (a);
                    ee.elements.Add(el);
                }
                else
                {
                    uint n1 = rng.NextDist(0, 16);
                    for (int j = 0; j < n1; j++)
                    {
                        MessageElement el = MessageSerializationTest4_NewRandomMessageElement(rng, 0);
                        ee.elements.Add(el);
                    }
                }

                m.entries.Add(ee);
            }

            return m;
        }

        public static void CompareMessage(Message m1, Message m2)
        {
            MessageHeader h1 = m1.header;
            MessageHeader h2 = m2.header;

            RRAssert.Equals(h1.MessageSize, h2.MessageSize);
            RRAssert.Equals(h1.MessageFlags_, h2.MessageFlags_);

            if ((h1.MessageFlags_ & (byte)MessageFlags.RoutingInfo) != 0)
            {
                RRAssert.Equals(h1.SenderNodeID, h2.SenderNodeID);
                RRAssert.Equals(h1.ReceiverNodeID, h2.ReceiverNodeID);
                RRAssert.Equals(h1.SenderNodeName, h2.SenderNodeName);
                RRAssert.Equals(h1.ReceiverNodeName, h2.ReceiverNodeName);
            }

            if ((h1.MessageFlags_ & (byte)MessageFlags.EndpointInfo) != 0)
            {
                RRAssert.Equals(h1.SenderEndpoint, h2.SenderEndpoint);
                RRAssert.Equals(h1.ReceiverEndpoint, h2.ReceiverEndpoint);
            }

            if ((h1.MessageFlags_ & (byte)MessageFlags.Priority) != 0)
            {
                RRAssert.Equals(h1.Priority, h2.Priority);
            }

            if ((h1.MessageFlags_ & (byte)MessageFlags.MetaInfo) != 0)
            {
                RRAssert.Equals(h1.MetaData, h2.MetaData);
                RRAssert.Equals(h1.MessageID, h2.MessageID);
                RRAssert.Equals(h1.MessageResID, h2.MessageResID);
            }

            if ((h1.MessageFlags_ & (byte)MessageFlags.StringTable) != 0)
            {
                throw new NotImplementedException();
            }

            if ((h1.MessageFlags_ & (byte)MessageFlags.Extended) != 0)
            {
                RRAssert.Equals(h1.Extended, h2.Extended);
            }

            RRAssert.Equals(h1.EntryCount, h2.EntryCount);
            RRAssert.Equals(m1.entries.Count, m2.entries.Count);
            for (int i = 0; i < m1.entries.Count && i < m2.entries.Count; i++)
            {
                CompareMessageEntry(m1.entries[i], m2.entries[i]);
            }
        }

        public static void CompareMessageEntry(MessageEntry m1, MessageEntry m2)
        {
            RRAssert.Equals(m1.EntrySize, m2.EntrySize);
            RRAssert.Equals(m1.EntryFlags, m2.EntryFlags);
            RRAssert.Equals(m1.EntryType, m2.EntryType);

            if ((m1.EntryFlags & (byte)MessageEntryFlags.ServicePathStr) != 0)
            {
                RRAssert.Equals(m1.ServicePath, m2.ServicePath);
            }
            if ((m1.EntryFlags & (byte)MessageEntryFlags.ServicePathCode) != 0)
            {
                RRAssert.Equals(m1.ServicePathCode, m2.ServicePathCode);
            }

            if ((m1.EntryFlags & (byte)MessageEntryFlags.MemberNameStr) != 0)
            {
                RRAssert.Equals(m1.MemberName, m2.MemberName);
            }
            if ((m1.EntryFlags & (byte)MessageEntryFlags.MemberNameCode) != 0)
            {
                RRAssert.Equals(m1.MemberNameCode, m2.MemberNameCode);
            }

            if ((m1.EntryFlags & (byte)MessageEntryFlags.RequestID) != 0)
            {
                RRAssert.Equals(m1.RequestID, m2.RequestID);
            }

            if ((m1.EntryFlags & (byte)MessageEntryFlags.Error) != 0)
            {
                RRAssert.Equals(m1.Error, m2.Error);
            }

            if ((m1.EntryFlags & (byte)MessageEntryFlags.MetaInfo) != 0)
            {
                RRAssert.Equals(m1.MetaData, m2.MetaData);
            }

            if ((m1.EntryFlags & (byte)MessageEntryFlags.Extended) != 0)
            {
                RRAssert.Equals(m1.Extended, m2.Extended);
            }

            RRAssert.Equals(m1.elements.Count, m2.elements.Count);

            for (int i = 0; i < m1.elements.Count; i++)
            {
                CompareMessageElement(m1.elements[i], m2.elements[i]);
            }
        }

        public static void MessageSerializationTest_CompareSubElements(MessageElement m1, MessageElement m2)
        {
            // cSpell: ignore sdat
            MessageElementNestedElementList sdat1 = m1.CastDataToNestedList();
            MessageElementNestedElementList sdat2 = m2.CastDataToNestedList();

            RRAssert.Equals(sdat1.Elements.Count, sdat2.Elements.Count);
            for (int i = 0; i < sdat1.Elements.Count && i < sdat2.Elements.Count; i++)
            {
                CompareMessageElement(sdat1.Elements[i], sdat2.Elements[i]);
            }
        }

        public static void CompareMessageElement(MessageElement m1, MessageElement m2)
        {
            RRAssert.Equals(m1.ElementSize, m2.ElementSize);
            RRAssert.Equals(m1.ElementFlags, m2.ElementFlags);
            if ((m1.ElementFlags & (byte)MessageElementFlags.ElementNameStr) != 0)
            {
                RRAssert.Equals(m1.ElementName, m2.ElementName);
            }
            if ((m1.ElementFlags & (byte)MessageElementFlags.ElementNameCode) != 0)
            {
                RRAssert.Equals(m1.ElementNameCode, m2.ElementNameCode);
            }

            if ((m1.ElementFlags & (byte)MessageElementFlags.ElementNumber) != 0)
            {
                RRAssert.Equals(m1.ElementNumber, m2.ElementNumber);
            }

            RRAssert.Equals(m1.ElementType, m2.ElementType);

            if ((m1.ElementFlags & (byte)MessageElementFlags.ElementTypeNameStr) != 0)
            {
                RRAssert.Equals(m1.ElementTypeName, m2.ElementTypeName);
            }
            if ((m1.ElementFlags & (byte)MessageElementFlags.ElementTypeNameCode) != 0)
            {
                RRAssert.Equals(m1.ElementTypeNameCode, m2.ElementTypeNameCode);
            }

            if ((m1.ElementFlags & (byte)MessageElementFlags.MetaInfo) != 0)
            {
                RRAssert.Equals(m1.MetaData, m2.MetaData);
            }

            RRAssert.Equals((m1.ElementFlags & 0x40), 0);

            if ((m1.ElementFlags & (byte)MessageElementFlags.Extended) != 0)
            {
                RRAssert.Equals(m1.Extended, m2.Extended);
            }

            RRAssert.Equals(m1.DataCount, m2.DataCount);

            if (m1.ElementType == DataTypes.void_t)
                RRAssert.Equals(m1.DataCount, 0);

            switch (m1.ElementType)
            {
                case DataTypes.void_t:
                    break;
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
                case DataTypes.bool_t:
                    {
                        var a1 = m1.CastData<Array>();
                        var a2 = m2.CastData<Array>();
                        RRAssert.Equals(a1.Length, m1.DataCount);
                        RRAssert.Equals(a2.Length, m2.DataCount);
                        RRAssert.Equals(DataTypeUtil.TypeIDFromType(a1.GetType()), m1.ElementType);
                        RRAssert.Equals(DataTypeUtil.TypeIDFromType(a2.GetType()), m2.ElementType);
                        RRAssert.IsTrue(a1.Cast<object>().SequenceEqual(a2.Cast<object>()));
                        break;
                    }
                case DataTypes.cdouble_t:
                    {
                        var a1 = m1.CastData<CDouble[]>();
                        var a2 = m2.CastData<CDouble[]>();
                        RRAssert.Equals(a1.Length, m1.DataCount);
                        RRAssert.Equals(a2.Length, m2.DataCount);
                        for (int j = 0; j < a1.Length; j++)
                        {
                            RRAssert.Equals(a1[j].Real, a2[j].Real);
                            RRAssert.Equals(a1[j].Imag, a2[j].Imag);
                        }

                        break;
                    }
                case DataTypes.csingle_t:
                    {
                        var a1 = m1.CastData<CSingle[]>();
                        var a2 = m2.CastData<CSingle[]>();
                        RRAssert.Equals(a1.Length, m1.DataCount);
                        RRAssert.Equals(a2.Length, m2.DataCount);
                        for (int j = 0; j < a1.Length; j++)
                        {
                            RRAssert.Equals(a1[j].Real, a2[j].Real);
                            RRAssert.Equals(a1[j].Imag, a2[j].Imag);
                        }

                        break;
                    }
                case DataTypes.string_t:
                    {
                        var a1 = m1.CastData<string>();
                        var a2 = m2.CastData<string>();
                        RRAssert.Equals(a1, a2);
                        break;
                    }
                case DataTypes.structure_t:
                case DataTypes.vector_t:
                case DataTypes.dictionary_t:
                case DataTypes.multidimarray_t:
                case DataTypes.list_t:
                case DataTypes.pod_t:
                case DataTypes.pod_array_t:
                case DataTypes.pod_multidimarray_t:
                case DataTypes.namedarray_array_t:
                case DataTypes.namedarray_multidimarray_t:
                    {
                        MessageSerializationTest_CompareSubElements(m1, m2);
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown data type");
            }
        }


    }
}
