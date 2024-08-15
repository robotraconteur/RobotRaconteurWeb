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

using static RobotRaconteurTest.MessageTestUtil;

namespace RobotRaconteurTest
{
    public static class MessageSerializationTest
    {

        static public void RandomTest()
        {
            int iterations = 100;
            var rng = new LFSRSeqGen((uint)DateTime.Now.Ticks, "message_serialization_random_test");

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
                    RRAssert.Equals(w.Position, m.ComputeSize());
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
            var rng = new LFSRSeqGen((uint)DateTime.Now.Ticks, "message_serialization_random_test");

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


        static public void RunMessageTests()
        {
            /*Test();
            RandomTest();*/
            Test4();
            RandomTest4();
        }
    }
}
