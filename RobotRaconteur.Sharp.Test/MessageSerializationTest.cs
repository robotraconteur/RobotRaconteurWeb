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
using RobotRaconteurSharp;

using static RobotRaconteurTest.MessageTestUtil;

namespace RobotRaconteurTest
{
    public static class MessageSerializationTest
    {
        // Seed from ROBOTRACONTEUR_TEST_SEED if set, otherwise from the clock. The seed is printed so a
        // failure can be reproduced.
        static uint GetRandomSeed()
        {
            uint seed;
            var seed_env = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_TEST_SEED");
            if (seed_env == null || !uint.TryParse(seed_env, out seed))
            {
                seed = (uint)DateTime.Now.Ticks;
            }
            Console.WriteLine("Message serialization random test seed: {0}", seed);
            return seed;
        }

        static public void RandomTest()
        {
            int iterations = 100;
            uint seed = GetRandomSeed();
            var rng = new LFSRSeqGen(seed, "message_serialization_random_test");

            for (int i = 0; i < iterations; i++)
            {
                var m = NewRandomTestMessage(rng);

                // Write to stream and read back
                uint messageSize = m.ComputeSize();
                var buf = new byte[messageSize];
                var w1 = new MemoryStream(buf);
                using (var w = new ArrayBinaryWriter(w1, (int)messageSize))
                {
                    m.Write(w);
                    RRAssert.AreEqual(w.Position, m.ComputeSize());
                }

                var r1 = new MemoryStream(buf);

                using (var r = new ArrayBinaryReader(r1, (int)messageSize))
                {
                    var m2 = CreateMessage();
                    m2.Read(r);
                    CompareMessage(m, m2);
                }
            }
        }


        static public void Test()
        {
            var m = NewTestMessage();

            // Write to stream and read back
            uint messageSize = m.ComputeSize();
            var buf = new byte[messageSize];
            var w1 = new MemoryStream(buf);
            using (var w = new ArrayBinaryWriter(w1, (int)messageSize))
            {
                m.Write(w);
                RRAssert.AreEqual(w.Position, m.ComputeSize());
            }

            var r1 = new MemoryStream(buf);

            using (var r = new ArrayBinaryReader(r1, (int)messageSize))
            {
                var m2 = CreateMessage();
                m2.Read(r);
                CompareMessage(m, m2);
            }
        }


        static public void RandomTest4()
        {
            int iterations = 100;
            uint seed = GetRandomSeed();
            var rng = new LFSRSeqGen(seed, "message_serialization_random_test");

            for (int i = 0; i < iterations; i++)
            {
                var m = NewRandomTestMessage4(rng);

                // Write to stream and read back
                uint messageSize = m.ComputeSize4();
                var buf = new byte[messageSize];
                var w1 = new MemoryStream(buf);
                using (var w = new ArrayBinaryWriter(w1, (int)messageSize))
                {
                    m.Write4(w);
                    RRAssert.AreEqual(w.Position, m.ComputeSize4());
                }

                var r1 = new MemoryStream(buf);

                using (var r = new ArrayBinaryReader(r1, (int)messageSize))
                {
                    var m2 = CreateMessage();
                    m2.Read4(r);
                    CompareMessage(m, m2);
                }
            }
        }


        static public void Test4()
        {
            var m = NewTestMessage();

            // Write to stream and read back
            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            var w1 = new MemoryStream(buf);
            using (var w = new ArrayBinaryWriter(w1, (int)messageSize))
            {
                m.Write4(w);
                RRAssert.AreEqual(w.Position, m.ComputeSize4());
            }

            var r1 = new MemoryStream(buf);

            using (var r = new ArrayBinaryReader(r1, (int)messageSize))
            {
                var m2 = CreateMessage();
                m2.Read4(r);
                CompareMessage(m, m2);
            }
        }


        static Message BuildNestedMessage(int levels)
        {
            var el = new MessageElement("e", new MessageElementNestedElementList(DataTypes.structure_t, "", new List<MessageElement>()));
            for (int i = 1; i < levels; i++)
            {
                var v = new List<MessageElement>();
                v.Add(el);
                el = new MessageElement("e", new MessageElementNestedElementList(DataTypes.structure_t, "", v));
            }
            var m = CreateMessage();
            m.header = CreateMessageHeader();
            var e = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e.elements.Add(el);
            m.entries.Add(e);
            return m;
        }

        static void ReadNested4(int levels)
        {
            var m = BuildNestedMessage(levels);
            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }
            using (var r = new ArrayBinaryReader(new MemoryStream(buf), (int)messageSize))
            {
                var m2 = CreateMessage();
                m2.Read4(r);
            }
        }

        static public void TestNestingDepth()
        {
            // Messages nested deeper than 512 levels must be rejected instead of recursing without limit
            ReadNested4(512);
            RRAssert.ThrowsException<ProtocolException>(() => ReadNested4(513));
            // The per-thread depth counter must be restored after a rejected message
            ReadNested4(512);
            Console.WriteLine("MessageSerializationTest.TestNestingDepth passed");
        }

        static public void TestElementCount()
        {
            // A nested list claiming more children than the message holds must be rejected before allocating
            var v = new List<MessageElement>();
            for (int i = 0; i < 70000; i++)
                v.Add(new MessageElement("a", null));
            var m = CreateMessage();
            m.header = CreateMessageHeader();
            var e = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e.elements.Add(new MessageElement("l", new MessageElementNestedElementList(DataTypes.list_t, "", v)));
            m.entries.Add(e);

            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }

            // The child count 70000 is encoded as uint_x 0xFE + uint32; replace it with 0xFFFFFFFF
            byte[] pattern = new byte[] { 0xFE, 0x70, 0x11, 0x01, 0x00 };
            int pos = -1;
            for (int i = 0; i + pattern.Length <= buf.Length && pos < 0; i++)
            {
                if (buf.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    pos = i;
            }
            RRAssert.IsTrue(pos >= 0);
            for (int i = 1; i < 5; i++)
                buf[pos + i] = 0xFF;

            RRAssert.ThrowsException<IOException>(() =>
            {
                using (var r = new ArrayBinaryReader(new MemoryStream(buf), (int)messageSize))
                {
                    var m2 = CreateMessage();
                    m2.Read4(r);
                }
            });
            Console.WriteLine("MessageSerializationTest.TestElementCount passed");
        }

        static byte[] SerializeAndPatch70000(Message m, uint new_count)
        {
            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }
            // 70000 is encoded as uint_x 0xFE + little-endian uint32
            byte[] pattern = new byte[] { 0xFE, 0x70, 0x11, 0x01, 0x00 };
            int pos = -1;
            for (int i = 0; i + pattern.Length <= buf.Length && pos < 0; i++)
            {
                if (buf[i] == 0xFE && buf.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    pos = i;
            }
            RRAssert.IsTrue(pos >= 0);
            byte[] c = BitConverter.GetBytes(new_count);
            Array.Copy(c, 0, buf, pos + 1, 4);
            return buf;
        }

        static void CheckRejectedWithoutLargeAllocation(Message m)
        {
            // Length patched to 0x10000000 (256 MiB) while ~70 KB is present
            byte[] buf = SerializeAndPatch70000(m, 0x10000000);
            long before = GC.GetTotalAllocatedBytes(true);
            RRAssert.ThrowsException<IOException>(() =>
            {
                using (var r = new ArrayBinaryReader(new MemoryStream(buf), buf.Length))
                {
                    var m2 = CreateMessage();
                    m2.Read4(r);
                }
            });
            long allocated = GC.GetTotalAllocatedBytes(true) - before;
            RRAssert.IsTrue(allocated < 64L * 1024 * 1024);
        }

        static public void TestLengthAllocation()
        {
            // String length
            var m1 = CreateMessage();
            m1.header = CreateMessageHeader();
            var e1 = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e1.elements.Add(new MessageElement(new string('a', 70000), null));
            m1.entries.Add(e1);
            CheckRejectedWithoutLargeAllocation(m1);

            // Entry Extended field
            var m2 = CreateMessage();
            m2.header = CreateMessageHeader();
            var e2 = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e2.EntryFlags |= (byte)MessageEntryFlags.Extended;
            e2.Extended = new byte[70000];
            m2.entries.Add(e2);
            CheckRejectedWithoutLargeAllocation(m2);

            // Entry element count
            var m3 = CreateMessage();
            m3.header = CreateMessageHeader();
            var e3 = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            for (int i = 0; i < 70000; i++)
                e3.elements.Add(new MessageElement("a", null));
            m3.entries.Add(e3);
            CheckRejectedWithoutLargeAllocation(m3);

            Console.WriteLine("MessageSerializationTest.TestLengthAllocation passed");
        }

        static public void RunMessageTests()
        {
            Test();
            RandomTest();
            Test4();
            RandomTest4();
            TestElementExtendedLength();
            TestReadString8LengthCheck();
            TestNestingDepth();
            TestElementCount();
            TestLengthAllocation();
        }

        // Regression test: a string length larger than the remaining message must be rejected
        // with IOException before the string buffer is allocated
        static public void TestReadString8LengthCheck()
        {
            var buf = new byte[16];
            using (var r = new ArrayBinaryReader(new MemoryStream(buf), buf.Length))
            {
                bool io_error = false;
                try
                {
                    r.ReadString8(0xFFFFFFF0);
                }
                catch (IOException)
                {
                    io_error = true;
                }
                RRAssert.IsTrue(io_error);
            }
        }

        // Pack data as a varvalue element and set its element type as when the message is sent
        static MessageElement VarValueElement(RobotRaconteurNode node, object data)
        {
            var me = new MessageElement("value", node.PackVarType(data, null));
            me.UpdateData();
            return me;
        }

        public static void TestVarValueComplexBool()
        {
            // varvalue must round trip complex and bool arrays
            var node = new RobotRaconteurNode();
            var cd = new CDouble[] { new CDouble(1, 2), new CDouble(3, 4) };
            var cs = new CSingle[] { new CSingle(5, 6) };
            var b = new bool[] { true, false, true };

            var cd2 = (CDouble[])node.UnpackVarType(VarValueElement(node, cd), null);
            RRAssert.AreEqual(cd2.Length, 2);
            RRAssert.AreEqual(cd2[0], cd[0]);
            RRAssert.AreEqual(cd2[1], cd[1]);

            var cs2 = (CSingle[])node.UnpackVarType(VarValueElement(node, cs), null);
            RRAssert.AreEqual(cs2.Length, 1);
            RRAssert.AreEqual(cs2[0], cs[0]);

            var b2 = (bool[])node.UnpackVarType(VarValueElement(node, b), null);
            RRAssert.AreEqual(b2.Length, 3);
            RRAssert.IsTrue(b2[0] && !b2[1] && b2[2]);
            node.Shutdown();

            RRWebTest.WriteLine("MessageSerializationTest.TestVarValueComplexBool passed");
        }

        static public void TestElementExtendedLength()
        {
            // An element Extended length larger than the message must be rejected before allocating
            var m = CreateMessage();
            m.header = CreateMessageHeader();
            var e = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            var el = new MessageElement("a", null);
            el.Extended = new byte[70000];
            e.elements.Add(el);
            m.entries.Add(e);

            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }

            // 70000 is encoded as uint_x 0xFE + little-endian uint32; claim 0x10000000 (256 MiB) instead
            byte[] pattern = new byte[] { 0xFE, 0x70, 0x11, 0x01, 0x00 };
            int pos = -1;
            for (int i = 0; i + pattern.Length <= buf.Length && pos < 0; i++)
            {
                if (buf[i] == 0xFE && buf.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    pos = i;
            }
            RRAssert.IsTrue(pos >= 0);
            Array.Copy(BitConverter.GetBytes((uint)0x10000000), 0, buf, pos + 1, 4);

            long before = GC.GetTotalAllocatedBytes(true);
            RRAssert.ThrowsException<IOException>(() =>
            {
                using (var r = new ArrayBinaryReader(new MemoryStream(buf), buf.Length))
                {
                    var m2 = CreateMessage();
                    m2.Read4(r);
                }
            });
            long allocated = GC.GetTotalAllocatedBytes(true) - before;
            RRAssert.IsTrue(allocated < 64L * 1024 * 1024);
            Console.WriteLine("MessageSerializationTest.TestElementExtendedLength passed");
        }

        public static void TestVarValueNestedContainers()
        {
            // A varvalue map or list that holds another map or list must unpack
            var node = new RobotRaconteurNode();
            var inner_map = new Dictionary<string, object>();
            inner_map.Add("x", new int[] { 2 });
            var inner_list = new List<object>();
            inner_list.Add(new double[] { 1.5 });
            inner_list.Add("s");
            var inner_vector = new Dictionary<int, object>();
            inner_vector.Add(3, new uint[] { 4 });
            var v = new Dictionary<string, object>();
            v.Add("m", inner_map);
            v.Add("l", inner_list);
            v.Add("v", inner_vector);

            // Send it through a message as a varvalue element
            var m = CreateMessage();
            m.header = CreateMessageHeader();
            var e = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e.AddElement("value", node.PackVarType(v, null));
            m.entries.Add(e);
            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }
            var m2 = CreateMessage();
            using (var r = new ArrayBinaryReader(new MemoryStream(buf), (int)messageSize))
            {
                m2.Read4(r);
            }

            var v2 = (Dictionary<string, object>)node.UnpackVarType(m2.entries[0].FindElement("value"), null);
            var inner_map2 = (Dictionary<string, object>)v2["m"];
            RRAssert.AreEqual(((int[])inner_map2["x"])[0], 2);
            var inner_list2 = (List<object>)v2["l"];
            RRAssert.AreEqual(inner_list2.Count, 2);
            RRAssert.AreEqual(((double[])inner_list2[0])[0], 1.5);
            RRAssert.AreEqual((string)inner_list2[1], "s");
            var inner_vector2 = (Dictionary<int, object>)v2["v"];
            RRAssert.AreEqual(((uint[])inner_vector2[3])[0], (uint)4);
            node.Shutdown();
            Console.WriteLine("MessageSerializationTest.TestVarValueNestedContainers passed");
        }

        // Send packed container data through a message and return the received element data
        static object RoundTripElementData(object packed)
        {
            var m = CreateMessage();
            m.header = CreateMessageHeader();
            var e = new MessageEntry(MessageEntryType.FunctionCallReq, "f");
            e.AddElement("value", packed);
            m.entries.Add(e);
            uint messageSize = m.ComputeSize4();
            var buf = new byte[messageSize];
            using (var w = new ArrayBinaryWriter(new MemoryStream(buf), (int)messageSize))
            {
                m.Write4(w);
            }
            var m2 = CreateMessage();
            using (var r = new ArrayBinaryReader(new MemoryStream(buf), (int)messageSize))
            {
                m2.Read4(r);
            }
            return m2.entries[0].FindElement("value").Data;
        }

        public static void TestPackEnumContainers()
        {
            // Lists and maps of an enum type must pack (as int32) and unpack
            var node = new RobotRaconteurNode();
            var l = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Friday };
            var l2 = (List<DayOfWeek>)node.UnpackListType<DayOfWeek>(RoundTripElementData(node.PackListType<DayOfWeek>(l, null)), null);
            RRAssert.AreEqual(l2.Count, 2);
            RRAssert.IsTrue(l2[0] == DayOfWeek.Monday && l2[1] == DayOfWeek.Friday);

            var d = new Dictionary<string, DayOfWeek>();
            d.Add("a", DayOfWeek.Sunday);
            d.Add("b", DayOfWeek.Saturday);
            var d2 = (Dictionary<string, DayOfWeek>)node.UnpackMapType<string, DayOfWeek>(RoundTripElementData(node.PackMapType<string, DayOfWeek>(d, null)), null);
            RRAssert.AreEqual(d2.Count, 2);
            RRAssert.IsTrue(d2["a"] == DayOfWeek.Sunday && d2["b"] == DayOfWeek.Saturday);

            var v = new Dictionary<int, DayOfWeek>();
            v.Add(5, DayOfWeek.Tuesday);
            var v2 = (Dictionary<int, DayOfWeek>)node.UnpackMapType<int, DayOfWeek>(RoundTripElementData(node.PackMapType<int, DayOfWeek>(v, null)), null);
            RRAssert.IsTrue(v2[5] == DayOfWeek.Tuesday);
            node.Shutdown();
            Console.WriteLine("MessageSerializationTest.TestPackEnumContainers passed");
        }
    }
}
