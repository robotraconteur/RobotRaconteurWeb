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
using System.IO;
using System.Linq;
using System.Text;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{

#if !ROBOTRACONTEUR_H5
    class MultiDimArrayTest
    {

        public static void Test()
        {
            TestDouble();
            TestByte();
        }

        public static void TestDouble()
        {
            // cSpell: ignore testmdarray
            MultiDimArray m1 = LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray1.bin"));
            MultiDimArray m2 = LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray2.bin"));
            MultiDimArray m3 = LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray3.bin"));
            MultiDimArray m4 = LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray4.bin"));
            MultiDimArray m5 = LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray5.bin"));

            m1.AssignSubArray(new uint[] { 2, 2, 3, 3, 4 }, m2, new uint[] { 0, 2, 0, 0, 0 }, new uint[] { 1, 5, 5, 2, 1 });



            ca<double>((double[])m1.Array_, (double[])m3.Array_);

            MultiDimArray m6 = new MultiDimArray(new uint[] { 2, 2, 1, 1, 10 }, new double[40]);
            m1.RetrieveSubArray(new uint[] { 4, 2, 2, 8, 0 }, m6, new uint[] { 0, 0, 0, 0, 0 }, new uint[] { 2, 2, 1, 1, 10 });
            ca<double>((double[])m4.Array_, (double[])m6.Array_);

            MultiDimArray m7 = new MultiDimArray(new uint[] { 4, 4, 4, 4, 10 }, new double[2560]);
            m1.RetrieveSubArray(new uint[] { 4, 2, 2, 8, 0 }, m7, new uint[] { 2, 1,2, 1, 0 }, new uint[] { 2, 2, 1, 1, 10 });
            ca<double>((double[])m5.Array_, (double[])m7.Array_);


        }

        public static void TestByte()
        {
            MultiDimArray m1 = LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b1.bin"));
            MultiDimArray m2 = LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b2.bin"));
            MultiDimArray m3 = LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b3.bin"));
            MultiDimArray m4 = LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b4.bin"));
            MultiDimArray m5 = LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b5.bin"));

            m1.AssignSubArray(new uint[] { 50,100 }, m2, new uint[] { 20,25}, new uint[] { 200,200 });



            ca<byte>((byte[])m1.Array_, (byte[])m3.Array_);


            MultiDimArray m6 = new MultiDimArray(new uint[] { 200,200 }, new byte[40000]);
            m1.RetrieveSubArray(new uint[] { 65,800 }, m6, new uint[] { 0, 0 }, new uint[] { 200,200 });
            ca<byte>((byte[])m4.Array_, (byte[])m6.Array_);


            MultiDimArray m7 = new MultiDimArray(new uint[] { 512,512 }, new byte[512*512]);
            m1.RetrieveSubArray(new uint[] { 65,800 }, m7, new uint[] { 100,230}, new uint[] { 200,200 });
            ca<byte>((byte[])m5.Array_, (byte[])m7.Array_);

        }

        public static void ca<T>(T[] v1, T[] v2)
        where T : IComparable, IComparable<T>
        {
            RRAssert.AreEqual(v1.Length, v2.Length);

            for (int i = 0; i < v1.Length; i++)
            {
                RRAssert.AreEqual<T>(v1[i], v2[i]);
            }
        }


        public static MultiDimArray LoadDoubleArrayFromFile(string fname)
        {
            FileStream f = new FileStream(fname, FileMode.Open, FileAccess.Read);
            MultiDimArray a = LoadDoubleArray(f);
            f.Close();
            return a;
        }

        public static MultiDimArray LoadDoubleArray(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            int dimcount = r.ReadInt32();
            uint[] dims = new uint[dimcount];
            uint count = 1;
            for (int i = 0; i < dimcount; i++)
            {
                dims[i] = (uint)r.ReadInt32();
                count *= dims[i];
            }

            double[] real = new double[count];

            for (int i = 0; i < count; i++)
            {
                real[i] = r.ReadDouble();
            }
            return new MultiDimArray(dims, real);

        }

        public static MultiDimArray LoadByteArrayFromFile(string fname)
        {
            FileStream f = new FileStream(fname, FileMode.Open, FileAccess.Read);
            MultiDimArray a = LoadByteArray(f);
            f.Close();
            return a;
        }

        public static MultiDimArray LoadByteArray(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            int dimcount = r.ReadInt32();
            uint[] dims = new uint[dimcount];
            uint count = 1;
            for (int i = 0; i < dimcount; i++)
            {
                dims[i] = (uint)r.ReadInt32();
                count *= dims[i];
            }

            byte[] real = new byte[count];

            for (int i = 0; i < count; i++)
            {
                real[i] = r.ReadByte();
            }

            return new MultiDimArray(dims, real);
        }
    }
#endif
}
