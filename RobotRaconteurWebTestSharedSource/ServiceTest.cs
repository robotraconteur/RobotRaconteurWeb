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
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using com.robotraconteur.testing.TestService2;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{


    public class RobotRaconteurTestServiceSupport
    {
        public RobotRaconteurTest_testroot testservice;
        public RobotRaconteurTest_testroot testservice_auth;
        public RobotRaconteurNode node;
        public RobotRaconteurTestServiceSupport(RobotRaconteurNode node = null)
        {
            this.node = node;
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
        }

        public void RegisterServices(Transport t)
        {
            testservice = new RobotRaconteurTest_testroot(t);
            testservice_auth = new RobotRaconteurTest_testroot(t);

            var ctx1 = node.RegisterService("RobotRaconteurTestService", "com.robotraconteur.testing.TestService1", testservice);
            ctx1.Attributes = new Dictionary<string, object>() {
                {"test", "This is a test attribute"},
                { "test2", 42 }

            };
#if !ROBOTRACONTEUR_H5
            string authdata = "testuser1 0b91dec4fe98266a03b136b59219d0d6 objectlock\ntestuser2 841c4221c2e7e0cefbc0392a35222512 objectlock\ntestsuperuser 503ed776c50169f681ad7bbc14198b68 objectlock,objectlockoverride";
            PasswordFileUserAuthenticator p = new PasswordFileUserAuthenticator(authdata);
            Dictionary<string, string> policies = new Dictionary<string, string>();
            policies.Add("requirevaliduser", "true");
            policies.Add("allowobjectlock", "true");
            ServiceSecurityPolicy s = new ServiceSecurityPolicy(p, policies);

            node.RegisterService("RobotRaconteurTestService_auth", "com.robotraconteur.testing.TestService1", testservice_auth, s);

#endif
        }

        public void UnregisterServices()
        {
            node.CloseService("RobotRaconteurTestService");
            node.CloseService("RobotRaconteurTestService_auth");
        }

    }




    public class RobotRaconteurTest_testroot : com.robotraconteur.testing.TestService1.testroot
    {

        Transport transport;

        public RobotRaconteurTest_testroot(Transport t)
        {
            this.transport = t;
        }


        public Task<double> get_d1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(12.345);
        }
        public Task set_d1(double value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 3.456) throw new Exception();
            return Task.FromResult(0);
        }


        //public double[] d2 {
        public Task<double[]> get_d2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<double>("ts1.d2.to_client"));
        }
        public Task set_d2(double[] value, CancellationToken cancel = default(CancellationToken))
        {
            double[] testval = TestVectors.Get<double>("ts1.d2.to_service");
            ca<double>(testval, value);
            return Task.FromResult(0);
        }
        //}

        public void ca<T>(T[] v1, T[] v2)
        {
            if (v1.Length != v2.Length) throw new Exception();
            for (int i = 0; i < v1.Length; i++)
            {
                if (Math.Abs((double)((dynamic)v1[i] - (dynamic)v2[i])) > 1e-15) throw new Exception();
            }
        }

        //public double[] d3  {
        public Task<double[]> get_d3(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<double>("ts1.d3.to_client"));
        }
        public Task set_d3(double[] value, CancellationToken cancel = default(CancellationToken))
        {
            double[] testval = TestVectors.Get<double>("ts1.d3.to_service");
            ca<double>(testval, value);
            return Task.FromResult(0);
        }
        //}

        //public double[] d4 {
        public Task<double[]> get_d4(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<double>("ts1.d4.to_client"));
        }
        public Task set_d4(double[] value, CancellationToken cancel = default(CancellationToken))
        {
            double[] testval = TestVectors.Get<double>("ts1.d4.to_service");
            ca<double>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public MultiDimArray d5 {
        public Task<MultiDimArray> get_d5(CancellationToken cancel = default(CancellationToken))
        {
            double[] r = TestVectors.Get<double>("ts1.d5.to_client");
            uint[] dims = TestVectors.Get<uint>("ts1.d5.to_client.dims");
            return Task.FromResult(new MultiDimArray(dims, r));
        }
        public Task set_d5(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            double[] r = TestVectors.Get<double>("ts1.d5.to_service");
            uint[] dims = TestVectors.Get<uint>("ts1.d5.to_service.dims");

            ca<uint>(dims, value.Dims);
            ca<double>(r, (double[])value.Array_);
            return Task.FromResult(0);
        }

        public Task<MultiDimArray> get_d6(CancellationToken cancel = default(CancellationToken))
        {
            double[] r = TestVectors.Get<double>("ts1.d6.to_client");

            uint[] dims = TestVectors.Get<uint>("ts1.d6.to_client.dims");
            return Task.FromResult(new MultiDimArray(dims, r));
        }
        public Task set_d6(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {

            double[] r = TestVectors.Get<double>("ts1.d6.to_service");

            uint[] dims = TestVectors.Get<uint>("ts1.d6.to_service.dims");

            ca<uint>(dims, value.Dims);
            ca<double>(r, (double[])value.Array_);

            return Task.FromResult(0);
        }

        //}

        //public float s1  {
        public Task<float> get_s1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(7.8573f);
        }
        public Task set_s1(float value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 3847.9283f) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public float[] s2 {
        public Task<float[]> get_s2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<float>("ts1.s2.to_client"));
        }
        public Task set_s2(float[] value, CancellationToken cancel = default(CancellationToken))
        {
            float[] testval = TestVectors.Get<float>("ts1.s2.to_service");
            ca<float>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public sbyte i8_1 {
        public Task<sbyte> get_i8_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<sbyte>(-66);
        }
        public Task set_i8_1(sbyte value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 45) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public sbyte[] i8_2 {
        public Task<sbyte[]> get_i8_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<sbyte>("ts1.i8_2.to_client"));
        }
        public Task set_i8_2(sbyte[] value, CancellationToken cancel = default(CancellationToken))
        {
            sbyte[] testval = TestVectors.Get<sbyte>("ts1.i8_2.to_service");
            ca<sbyte>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public byte u8_1 {
        public Task<byte> get_u8_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<byte>(222);
        }
        public Task set_u8_1(byte value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 232) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public byte[] u8_2 {
        public Task<byte[]> get_u8_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<byte>("ts1.u8_2.to_client"));

        }
        public Task set_u8_2(byte[] value, CancellationToken cancel = default(CancellationToken))
        {
            byte[] testval = TestVectors.Get<byte>("ts1.u8_2.to_service");
            ca<byte>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public MultiDimArray u8_3 {
        public Task<MultiDimArray> get_u8_3(CancellationToken cancel = default(CancellationToken))
        {
            byte[] r = TestVectors.Get<byte>("ts1.u8_3.to_client");
            uint[] dims = TestVectors.Get<uint>("ts1.u8_3.to_client.dims");
            return Task.FromResult(new MultiDimArray(dims, r));

        }
        public Task set_u8_3(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            byte[] testval = TestVectors.Get<byte>("ts1.u8_3.to_service");
            uint[] testdims = TestVectors.Get<uint>("ts1.u8_3.to_service.dims");
            ca<uint>(value.Dims, testdims);
            ca<byte>((byte[])value.Array_, testval);
            return Task.FromResult(0);
        }
        //}
        //public short i16_1        {
        public Task<short> get_i16_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<short>(-13428);
        }
        public Task set_i16_1(short value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 2387) throw new Exception();
            return Task.FromResult(0);
        }
        //}
        //public short[] i16_2
        //{
        public Task<short[]> get_i16_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<short>("ts1.i16_2.to_client"));
        }
        public Task set_i16_2(short[] value, CancellationToken cancel = default(CancellationToken))
        {
            short[] testval = TestVectors.Get<short>("ts1.i16_2.to_service");
            ca<short>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public ushort u16_1
        //{
        public Task<ushort> get_u16_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<ushort>(60981);
        }
        public Task set_u16_1(ushort value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 54732) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public ushort[] u16_2
        //{
        public Task<ushort[]> get_u16_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<ushort>("ts1.u16_2.to_client"));
        }
        public Task set_u16_2(ushort[] value, CancellationToken cancel = default(CancellationToken))
        {
            ushort[] testval = TestVectors.Get<ushort>("ts1.u16_2.to_service");
            ca<ushort>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public int i32_1
        //{
        public Task<int> get_i32_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<int>(898734);
        }
        public Task set_i32_1(int value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != -9837284) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public int[] i32_2
        //{
        public Task<int[]> get_i32_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<int>("ts1.i32_2.to_client"));
        }
        public Task set_i32_2(int[] value, CancellationToken cancel = default(CancellationToken))
        {
            int[] testval = TestVectors.Get<int>("ts1.i32_2.to_service");
            ca<int>(value, testval);
            return Task.FromResult(0);
        }
        //}

        //public uint u32_1
        //{
        public Task<uint> get_u32_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<uint>(547919123);
        }
        public Task set_u32_1(uint value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 1550099195) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public uint[] u32_2
        //{
        public Task<uint[]> get_u32_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<uint>("ts1.u32_2.to_client"));
        }
        public Task set_u32_2(uint[] value, CancellationToken cancel = default(CancellationToken))
        {
            uint[] testval = TestVectors.Get<uint>("ts1.u32_2.to_service");
            ca<uint>(testval, value);
            return Task.FromResult(0);
        }
        //}
        //public long i64_1
        //{
        public Task<long> get_i64_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<long>(-1357833931563696072);
        }
        public Task set_i64_1(long value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 8621740821050813024) throw new Exception();
            return Task.FromResult(0);
        }

        //}
        //public long[] i64_2 {
        public Task<long[]> get_i64_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<long>("ts1.i64_2.to_client"));
        }
        public Task set_i64_2(long[] value, CancellationToken cancel = default(CancellationToken))
        {
            long[] testval = TestVectors.Get<long>("ts1.i64_2.to_service");
            return Task.FromResult(0);
        }
        //}

        //public ulong u64_1
        //{
        public Task<ulong> get_u64_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<ulong>(13389861970863644378);
        }
        public Task set_u64_1(ulong value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != 1465640522145789825) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public ulong[] u64_2
        //{
        public Task<ulong[]> get_u64_2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(TestVectors.Get<ulong>("ts1.u64_2.to_client"));
        }
        public Task set_u64_2(ulong[] value, CancellationToken cancel = default(CancellationToken))
        {
            ulong[] testval = TestVectors.Get<ulong>("ts1.u64_2.to_service");
            ca<ulong>(value, testval);
            return Task.FromResult(0);
        }
        //}
        //public string str1
        //{
        public Task<string> get_str1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult("Hello Client!");
        }
        public Task set_str1(string value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != "Hello Server!") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public teststruct1 struct1
        //{
        public Task<teststruct1> get_struct1(CancellationToken cancel = default(CancellationToken))
        {
            //Add list type
            teststruct1 s = new teststruct1();
            s.dat1 = TestVectors.Get<double>("ts1.struct1.dat1.to_client");
            s.str2 = "Hello world!";

            Dictionary<int, string> vec3 = new Dictionary<int, string>();
            vec3.Add(1, "Hello Client!");
            vec3.Add(2, "Hello Client, again");
            vec3.Add(4372, "This is yet another test string");
            s.vec3 = vec3;

            Dictionary<string, string> dict4 = new Dictionary<string, string>();
            dict4.Add("teststring1", "Hello Client!");
            dict4.Add("teststring2", "Hello Client, again");
            dict4.Add("anotherstr", "This is yet another test string");
            s.dict4 = dict4;

            List<string> list5 = new List<string>();
            list5.Add("Hello Client!");
            list5.Add("Hello Client, again");
            list5.Add("This is yet another test string");
            s.list5 = list5;

            teststruct2 s2 = new teststruct2();
            s2.mydat = TestVectors.Get<double>("ts1.struct1.struct1_mydat.to_client");
            s.struct1 = s2;

            Dictionary<string, teststruct2> d2 = new Dictionary<string, teststruct2>();
            teststruct2 d2_1 = new teststruct2();
            d2_1.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_client");
            d2.Add("test1", d2_1);
            teststruct2 d2_2 = new teststruct2();
            d2_2.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_3.to_client");
            d2.Add("anothertest", d2_2);

            s.dstruct2 = d2;


            List<teststruct2> d3 = new List<teststruct2>();
            teststruct2 d3_1 = new teststruct2();
            d3_1.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_client");
            d3.Add(d2_1);
            teststruct2 d3_2 = new teststruct2();
            d3_2.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_3.to_client");
            d3.Add(d2_2);

            s.lstruct3 = d3;


            double[] md = TestVectors.Get<double>("ts1.struct1.multidimarray.to_client");
            s.multidimarray = new MultiDimArray(TestVectors.Get<uint>("ts1.struct1.multidimarray.to_client.dims"), md);
            s.var3 = "This is a vartype string";
            return Task.FromResult(s);
        }
        public Task set_struct1(teststruct1 value, CancellationToken cancel = default(CancellationToken))
        {
            //Add list type
            ca<double>(value.dat1, TestVectors.Get<double>("ts1.struct1.dat1.to_service"));
            if (value.str2 != "Hello world 2!") throw new Exception();

            if (value.vec3.Count != 4) throw new Exception();
            if (value.vec3[10] != "Hello Server!") throw new Exception();
            if (value.vec3[11] != "Hello Server, again") throw new Exception();
            if (value.vec3[46372] != "Test string!") throw new Exception();
            if (value.vec3[46373] != "Test string again") throw new Exception();

            if (value.dict4.Count != 2) throw new Exception();
            if (value.dict4["cteststring1"] != "Hello Server!") throw new Exception();
            if (value.dict4["cteststring2"] != "Hello Server, again") throw new Exception();

            ca<double>(value.struct1.mydat, TestVectors.Get<double>("ts1.struct1.struct1_mydat.to_service"));
            ca<double>(value.dstruct2["ctest1"].mydat, TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_service"));
            ca<double>(value.dstruct2["anothertest"].mydat, TestVectors.Get<double>("ts1.struct1.dstruct2_2.to_service"));

            ca<uint>(value.multidimarray.Dims, TestVectors.Get<uint>("ts1.struct1.multidimarray.to_service.dims"));
            ca<double>((double[])value.multidimarray.Array_, TestVectors.Get<double>("ts1.struct1.multidimarray.to_service"));

            ca((double[])value.var3, TestVectors.Get<double>("ts1.struct1.var3.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public teststruct2 struct2
        //{
        public Task<teststruct2> get_struct2(CancellationToken cancel = default(CancellationToken))
        {
            teststruct2 a = new teststruct2();
            a.mydat = TestVectors.Get<double>("ts1.struct2.to_client");
            return Task.FromResult(a);
        }

        public Task set_struct2(teststruct2 value, CancellationToken cancel = default(CancellationToken))
        {
            ca<double>(value.mydat, TestVectors.Get<double>("ts1.struct2.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<int, double> is_d1
        //{
        public Task<Dictionary<int, double>> get_is_d1(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, double> o = new Dictionary<int, double>();
            o.Add(9285, 1.643392e-01);
            o.Add(74822, 1.537133e+09);
            o.Add(4, 1.369505e-03);
            return Task.FromResult(o);
        }

        public Task set_is_d1(Dictionary<int, double> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 3) throw new Exception();
            if (value[928] != 4.074501e-07) throw new Exception();
            if (value[394820] != -4.535303e+05) throw new Exception();
            if (value[623] != -2.956241e-20) throw new Exception();
            return Task.FromResult(0);
        }
        //}
        //public Dictionary<string, double> is_d2
        //{
        public Task<Dictionary<string, double>> get_is_d2(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, double> o = new Dictionary<string, double>();
            o.Add("testval1", -1.079664e+16);
            o.Add("testval2", 2.224846e+00);
            return Task.FromResult(o);
        }
        public Task set_is_d2(Dictionary<string, double> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 2) throw new Exception();
            if (value["testval3"] != 5.242474e+10) throw new Exception();
            if (value["testval4"] != 2.208636e+08) throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<int, double[]> is_d3
        //{
        public Task<Dictionary<int, double[]>> get_is_d3(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, double[]> o = new Dictionary<int, double[]>();
            o.Add(12, TestVectors.Get<double>("ts1.is_d3.to_client.a"));
            o.Add(832, TestVectors.Get<double>("ts1.is_d3.to_client.b"));
            return Task.FromResult(o);
        }
        public Task set_is_d3(Dictionary<int, double[]> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 2) throw new Exception();
            ca<double>(value[47], TestVectors.Get<double>("ts1.is_d3.to_service.a"));
            ca<double>(value[324], TestVectors.Get<double>("ts1.is_d3.to_service.b"));
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<string, double[]> is_d4
        //{
        public Task<Dictionary<string, double[]>> get_is_d4(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, double[]> o = new Dictionary<string, double[]>();
            o.Add("testval1", TestVectors.Get<double>("ts1.is_d4.to_client.a"));
            o.Add("testval2", TestVectors.Get<double>("ts1.is_d4.to_client.b"));
            return Task.FromResult(o);
        }
        public Task set_is_d4(Dictionary<string, double[]> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 2) throw new Exception();
            ca<double>(value["testval3"], TestVectors.Get<double>("ts1.is_d4.to_service.a"));
            ca<double>(value["testval4"], TestVectors.Get<double>("ts1.is_d4.to_service.b"));
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<int, MultiDimArray> is_d5
        //{
        public Task<Dictionary<int, MultiDimArray>> get_is_d5(CancellationToken cancel = default(CancellationToken))
        {
            MultiDimArray a = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d5.to_client.dims"), TestVectors.Get<double>("ts1.is_d5.to_client"));
            Dictionary<int, MultiDimArray> o = new Dictionary<int, MultiDimArray>();
            o.Add(564, a);
            return Task.FromResult(o);
        }
        public Task set_is_d5(Dictionary<int, MultiDimArray> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            MultiDimArray a = value[328];
            ca<uint>(a.Dims, TestVectors.Get<uint>("ts1.is_d5.to_service.dims"));
            ca<double>((double[])a.Array_, TestVectors.Get<double>("ts1.is_d5.to_service"));
            return Task.FromResult(0);

        }

        //}

        //public Dictionary<string, MultiDimArray> is_d6 {
        public Task<Dictionary<string, MultiDimArray>> get_is_d6(CancellationToken cancel = default(CancellationToken))
        {
            MultiDimArray a = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d6.to_client.dims"), TestVectors.Get<double>("ts1.is_d6.to_client"));
            Dictionary<string, MultiDimArray> o = new Dictionary<string, MultiDimArray>();
            o.Add("testval1", a);
            return Task.FromResult(o);
        }
        public Task set_is_d6(Dictionary<string, MultiDimArray> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            MultiDimArray a = value["testval2"];
            ca<uint>(a.Dims, TestVectors.Get<uint>("ts1.is_d6.to_service.dims"));
            ca<double>((double[])a.Array_, TestVectors.Get<double>("ts1.is_d6.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<int, string> is_str1 {
        public Task<Dictionary<int, string>> get_is_str1(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, string> o = new Dictionary<int, string>();
            o.Add(23, "Hello server");
            return Task.FromResult(o);
        }

        public Task set_is_str1(Dictionary<int, string> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            if (value[24] != "Hello client") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<string, string> is_str2
        //{
        public Task<Dictionary<string, string>> get_is_str2(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, string> o = new Dictionary<string, string>();
            o.Add("testval1", "Hello server");
            return Task.FromResult(o);
        }
        public Task set_is_str2(Dictionary<string, string> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            if (value["testval2"] != "Hello client") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<int, teststruct2> is_struct1 {
        public Task<Dictionary<int, teststruct2>> get_is_struct1(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, teststruct2> o = new Dictionary<int, teststruct2>();
            teststruct2 s = new teststruct2();
            s.mydat = TestVectors.Get<double>("ts1.is_struct1.to_client");
            o.Add(748, s);
            return Task.FromResult(o);
        }
        public Task set_is_struct1(Dictionary<int, teststruct2> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            teststruct2 s = value[372];
            ca<double>(s.mydat, TestVectors.Get<double>("ts1.is_struct1.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public Dictionary<string, teststruct2> is_struct2 {
        public Task<Dictionary<string, teststruct2>> get_is_struct2(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, teststruct2> o = new Dictionary<string, teststruct2>();
            teststruct2 s = new teststruct2();
            s.mydat = TestVectors.Get<double>("ts1.is_struct2.to_client");
            o.Add("testval1", s);
            return Task.FromResult(o);
        }
        public Task set_is_struct2(Dictionary<string, teststruct2> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            teststruct2 s = value["testval2"];
            ca<double>(s.mydat, TestVectors.Get<double>("ts1.is_struct2.to_service"));
            return Task.FromResult(0);
        }
        //}


        public Task<List<double>> get_list_d1(CancellationToken cancel = default(CancellationToken))
        {
            List<double> o = new List<double>();
            o.Add(1.643392e-01);
            o.Add(1.537133e+09);
            o.Add(1.369505e-03);
            return Task.FromResult(o);
        }

        public Task set_list_d1(List<double> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 3) throw new Exception();
            if (value[0] != 4.074501e-07) throw new Exception();
            if (value[1] != -4.535303e+05) throw new Exception();
            if (value[2] != -2.956241e-20) throw new Exception();
            return Task.FromResult(0);
        }

        public Task<List<double[]>> get_list_d3(CancellationToken cancel = default(CancellationToken))
        {
            List<double[]> o = new List<double[]>();
            o.Add(TestVectors.Get<double>("ts1.is_d3.to_client.a"));
            o.Add(TestVectors.Get<double>("ts1.is_d3.to_client.b"));
            return Task.FromResult(o);
        }
        public Task set_list_d3(List<double[]> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 2) throw new Exception();
            ca<double>(value[0], TestVectors.Get<double>("ts1.is_d3.to_service.a"));
            ca<double>(value[1], TestVectors.Get<double>("ts1.is_d3.to_service.b"));
            return Task.FromResult(0);
        }

        public Task<List<MultiDimArray>> get_list_d5(CancellationToken cancel = default(CancellationToken))
        {
            MultiDimArray a = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d5.to_client.dims"), TestVectors.Get<double>("ts1.is_d5.to_client"));
            List<MultiDimArray> o = new List<MultiDimArray>();
            o.Add(a);
            return Task.FromResult(o);
        }
        public Task set_list_d5(List<MultiDimArray> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            MultiDimArray a = value[0];
            ca<uint>(a.Dims, TestVectors.Get<uint>("ts1.is_d5.to_service.dims"));
            ca<double>((double[])a.Array_, TestVectors.Get<double>("ts1.is_d5.to_service"));
            return Task.FromResult(0);
        }

        public Task<List<string>> get_list_str1(CancellationToken cancel = default(CancellationToken))
        {
            List<string> o = new List<string>();
            o.Add("Hello server");
            return Task.FromResult(o);
        }

        public Task set_list_str1(List<string> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            if (value[0] != "Hello client") throw new Exception();
            return Task.FromResult(0);
        }

        public Task<List<teststruct2>> get_list_struct1(CancellationToken cancel = default(CancellationToken))
        {
            List<teststruct2> o = new List<teststruct2>();
            teststruct2 s = new teststruct2();
            s.mydat = TestVectors.Get<double>("ts1.is_struct1.to_client");
            o.Add(s);
            return Task.FromResult(o);
        }
        public Task set_list_struct1(List<teststruct2> value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Count != 1) throw new Exception();
            teststruct2 s = value[0];
            ca<double>(s.mydat, TestVectors.Get<double>("ts1.is_struct1.to_service"));
            return Task.FromResult(0);
        }

        //public com.robotraconteur.testing.TestService2.ostruct2 struct3 {
        public Task<com.robotraconteur.testing.TestService2.ostruct2> get_struct3(CancellationToken cancel = default(CancellationToken))
        {
            var o = new com.robotraconteur.testing.TestService2.ostruct2();
            o.a1 = TestVectors.Get<double>("ts1.struct3.to_client");
            return Task.FromResult(o);
        }
        public Task set_struct3(com.robotraconteur.testing.TestService2.ostruct2 value, CancellationToken cancel = default(CancellationToken))
        {
            ca<double>(value.a1, TestVectors.Get<double>("ts1.struct3.to_service"));
            return Task.FromResult(0);
        }
        //}

        object m_var1 = null;

        //public object var1 {
        public Task<object> get_var1(CancellationToken cancel = default(CancellationToken)) { return Task.FromResult(m_var1); }
        public Task set_var1(object value, CancellationToken cancel = default(CancellationToken)) { m_var1 = value; return Task.FromResult(0); }

        //}
        Dictionary<int, object> m_var2 = null;

        //public Dictionary<int, object> var2 {
        public Task<Dictionary<int, object>> get_var2(CancellationToken cancel = default(CancellationToken)) { return Task.FromResult(m_var2); }
        public Task set_var2(Dictionary<int, object> value, CancellationToken cancel = default(CancellationToken)) { m_var2 = value; return Task.FromResult(0); }
        //}

        //public object var_num {
        public Task<object> get_var_num(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<object>(TestVectors.Get<int>("ts1.var_num.to_client"));
        }
        public Task set_var_num(object value, CancellationToken cancel = default(CancellationToken))
        {
            ca<int>((int[])value, TestVectors.Get<int>("ts1.var_num.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public object var_str
        //{
        public Task<object> get_var_str(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<object>("Hello Client!");
        }
        public Task set_var_str(object value, CancellationToken cancel = default(CancellationToken))
        {
            if ((string)value != "Hello Server!") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public object var_struct
        //{
        public Task<object> get_var_struct(CancellationToken cancel = default(CancellationToken))
        {
            teststruct2 s = new teststruct2();
            s.mydat = TestVectors.Get<double>("ts1.var_struct.to_client");
            return Task.FromResult<object>(s);
        }
        public Task set_var_struct(object value, CancellationToken cancel = default(CancellationToken))
        {
            ca<double>(((teststruct2)value).mydat, TestVectors.Get<double>("ts1.var_struct.to_service"));
            return Task.FromResult(0);
        }
        //}

        //public object var_vector {
        public Task<object> get_var_vector(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, object> d = new Dictionary<int, object>();
            d.Add(10, "Hello Client!");
            return Task.FromResult<object>(d);
        }
        public Task set_var_vector(object value, CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<int, object> d = (Dictionary<int, object>)value;
            if (d.Count != 1) throw new Exception();
            if (((string)d[11]) != "Hello Server!") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public object var_dictionary
        //{
        public Task<object> get_var_dictionary(CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d.Add("test1", "Hello Client!");
            return Task.FromResult<object>(d);
        }
        public Task set_var_dictionary(object value, CancellationToken cancel = default(CancellationToken))
        {
            Dictionary<string, object> d = (Dictionary<string, object>)value;
            if (d.Count != 1) throw new Exception();
            if (((string)d["test2"]) != "Hello Server!") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public object var_list {
        public Task<object> get_var_list(CancellationToken cancel = default(CancellationToken))
        {
            List<object> d = new List<object>();
            d.Add("Hello Client!");
            return Task.FromResult<object>(d);
        }
        public Task set_var_list(object value, CancellationToken cancel = default(CancellationToken))
        {
            List<object> d = (List<object>)value;
            if (d.Count != 1) throw new Exception();
            if (((string)d[0]) != "Hello Server!") throw new Exception();
            return Task.FromResult(0);
        }
        //}

        //public object var_multidimarray
        //{
        public Task<object> get_var_multidimarray(CancellationToken cancel = default(CancellationToken))
        {
            double[] d = TestVectors.Get<double>("ts1.var_multidimarray.to_client");
            return Task.FromResult<object>(new MultiDimArray(TestVectors.Get<uint>("ts1.var_multidimarray.to_client.dims"), d));
        }
        public Task set_var_multidimarray(object value, CancellationToken cancel = default(CancellationToken))
        {
            MultiDimArray a = (MultiDimArray)value;
            ca<uint>(a.Dims, TestVectors.Get<uint>("ts1.var_multidimarray.to_service.dims"));
            double[] d = TestVectors.Get<double>("ts1.var_multidimarray.to_service");
            ca<double>((double[])a.Array_, d);
            return Task.FromResult(0);
        }
        //}

        //public double errtest
        //{
        public Task<double> get_errtest(CancellationToken cancel = default(CancellationToken))
        {
            throw new Exception("This is a test");
        }
        public Task set_errtest(double value, CancellationToken cancel = default(CancellationToken))
        {
            throw new Exception("This is a test");
        }
        //}

        //public teststruct1 nulltest
        //{
        public Task<teststruct1> get_nulltest(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<teststruct1>(null);
        }
        public Task set_nulltest(teststruct1 value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != null) throw new Exception();
            return Task.FromResult(0);
        }

        //}

        public Task func1(CancellationToken cancel = default(CancellationToken))
        {

            Task.Run(async delegate ()
            {
                await Task.Delay(100);
                try
                {
                    ev1();
                }
                catch
                {
                    return;
                }
            }
            );

            return Task.FromResult(0);
        }

        public Task func2(double d1, double d2, CancellationToken cancel = default(CancellationToken))
        {
            Task.Run(async delegate ()
            {
                await Task.Delay(100);
                teststruct2 s = new teststruct2();
                s.mydat = new double[] { d2 };
                try
                {
                    ev2(d1, s);
                }
                catch
                {
                    return;
                }
            }
           );

            return Task.FromResult(0);
        }

        public Task<double> func3(double d1, double d2, CancellationToken cancel = default(CancellationToken))
        {
#if !ROBOTRACONTEUR_H5
            if (this.transport != null)
            {
            TcpTransport transport = (TcpTransport)this.transport;
            RRWebTest.WriteLine("Is Connection Secure: {0}", transport.IsTransportConnectionSecure(ServerEndpoint.CurrentEndpoint));
            RRWebTest.WriteLine("Is Secure Peer Identity Verified: {0}", transport.IsSecurePeerIdentityVerified(ServerEndpoint.CurrentEndpoint));
            if (transport.IsSecurePeerIdentityVerified(ServerEndpoint.CurrentEndpoint))
            {
                RRWebTest.WriteLine(transport.GetSecurePeerIdentity(ServerEndpoint.CurrentEndpoint));
            }
            }
#endif


            return Task.FromResult(d1 + d2);
        }

        public Task<int> meaning_of_life(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(42);
        }


        public Task func_errtest(CancellationToken cancel = default(CancellationToken))
        {
            throw new Exception("This is a test");
        }

        public Task func_errtest1(CancellationToken cancel = default(CancellationToken))
        {
            throw new DataTypeException("This is a test 1");
        }

        public Task func_errtest2(CancellationToken cancel = default(CancellationToken))
        {
            throw new testexception1("This is a test 2");
        }

        public Task func_errtest3(CancellationToken cancel = default(CancellationToken))
        {
            throw new testexception3("This is a test");
        }

        public event Action ev1;

        public event Action<double, teststruct2> ev2;



        sub1_impl o1 = new sub1_impl();

        public Task<sub1> get_o1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<sub1>(o1);
        }

        Dictionary<int, sub1_impl> o2 = new Dictionary<int, sub1_impl>();

        public Task<sub1> get_o2(int ind, CancellationToken cancel = default(CancellationToken))
        {
            lock (o2)
            {
                if (o2.ContainsKey(ind)) return Task.FromResult<sub1>(o2[ind]);

                sub1_impl o = new sub1_impl();
                o.set_i_ind(ind).GetAwaiter().GetResult();
                o2.Add(ind, o);
                return Task.FromResult<sub1>(o);
            }
        }

        Dictionary<int, sub1_impl> o3 = new Dictionary<int, sub1_impl>();
        public Task<sub1> get_o3(int ind, CancellationToken cancel = default(CancellationToken))
        {
            lock (o3)
            {
                if (o3.ContainsKey(ind)) return Task.FromResult<sub1>(o3[ind]);

                sub1_impl o = new sub1_impl();
                o.set_i_ind(ind).GetAwaiter().GetResult();
                o3.Add(ind, o);
                return Task.FromResult<sub1>(o);
            }
        }

        Dictionary<string, sub1_impl> o4 = new Dictionary<string, sub1_impl>();
        public Task<sub1> get_o4(string ind, CancellationToken cancel = default(CancellationToken))
        {
            lock (o4)
            {
                if (o4.ContainsKey(ind)) return Task.FromResult<sub1>(o4[ind]);

                sub1_impl o = new sub1_impl();
                o.set_s_ind(ind).GetAwaiter().GetResult();
                o4.Add(ind, o);
                return Task.FromResult<sub1>(o);
            }
        }

        TestService2SubObj o5 = new TestService2SubObj();
        public Task<com.robotraconteur.testing.TestService2.subobj> get_o5(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<com.robotraconteur.testing.TestService2.subobj>(o5);
        }

        object o6 = null;
        public Task<object> get_o6(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<object>(o6);
        }

        public Task o6_op(int op, CancellationToken cancel = default(CancellationToken))
        {
            try
            {

                ServerContext.CurrentServerContext.ReleaseServicePath(ServerContext.CurrentServicePath + ".o6");
            }
            catch (ServiceException e) { }

            switch (op)
            {
                case 0:
                    o6 = new sub1_impl();
                    break;
                case 1:
                    o6 = new sub2_impl();
                    break;
                case 2:
                    o6 = new TestService2SubObj();
                    break;

                default:
                    throw new InvalidOperationException();
            }
            return Task.FromResult(0);
        }

        PipeServer<double[]> m_p1;

        public Pipe<double[]> p1
        {
            get
            {
                return m_p1;
            }
            set
            {
                m_p1 = (PipeServer<double[]>)value;
                m_p1.PipeConnectCallback = p1_connect_callback;
            }
        }

        bool packet_sent = false;
        bool ack_recv = false;
        uint packetnum = 0;

        void p1_connect_callback(Pipe<double[]>.PipeEndpoint p)
        {

            p.PacketReceivedEvent += p1_packet_received;
            p.PacketAckReceivedEvent += p1_packet_ack_received;
            p.RequestPacketAck = true;

        }

        object p1_lock = new object();
        void p1_packet_received(Pipe<double[]>.PipeEndpoint p)
        {
            Task.Run(async delegate ()
            {
                await Task.Delay(500);
                try
                {
                    lock (p1_lock)
                    {
                        while (p.Available > 0)
                        {
                            double[] dat = p.ReceivePacket();
                            uint pnum = p.SendPacket(dat).Result;
                            if (!packet_sent) packetnum = pnum;
                            packet_sent = true;
                        }
                    }
                }
                catch { }
            });

        }

        void p1_packet_ack_received(Pipe<double[]>.PipeEndpoint p, uint packetnum)
        {
            if (packetnum == this.packetnum) ack_recv = true;
        }

        Pipe<teststruct2> m_p2;
        public Pipe<teststruct2> p2
        {
            get
            {
                return m_p2;
            }
            set
            {
                m_p2 = (PipeServer<teststruct2>)value;
                m_p2.PipeConnectCallback = p2_connect_callback;
            }
        }
        void p2_connect_callback(Pipe<teststruct2>.PipeEndpoint p)
        {
            p.PacketReceivedEvent += p2_packet_received;
        }

        object p2_lock = new object();
        void p2_packet_received(Pipe<teststruct2>.PipeEndpoint p)
        {
            Task.Run(async delegate ()
            {
                await Task.Delay(500);
                lock (p2_lock)
                {
                    while (p.Available > 0)
                    {
                        teststruct2 dat = p.ReceivePacket();
                        p.SendPacket(dat);
                    }
                }
            });
        }

        public Task pipe_check_error(CancellationToken cancel = default(CancellationToken))
        {
            if (!ack_recv) throw new Exception();
            return Task.FromResult(0);
        }

        //TODO: implement broadcast
        PipeServer<double> m_broadcastpipe;
        public Pipe<double> broadcastpipe
        {
            get
            {
                return m_broadcastpipe;
            }
            set
            {
                m_broadcastpipe = (PipeServer<double>)value;
                //m_p1.PipeConnectCallback = p1_connect_callback;
            }
        }

        public async Task test_callbacks(CancellationToken cancel = default(CancellationToken))
        {
            var ep = ServerEndpoint.CurrentEndpoint;

            await cb1.GetClientFunction(ep)(default(CancellationToken));
            await cb2.GetClientFunction(ep)(739.2, 0.392, default(CancellationToken));
            double res = await cb3.GetClientFunction(ep)(34, 45, default(CancellationToken));
            if (res != (34 + 45 + 3.14)) throw new Exception();

            if (await cb_meaning_of_life.GetClientFunction(ep)(default(CancellationToken)) != 42) throw new Exception();

            bool errthrown = false;

            try
            {
                await cb_errtest.GetClientFunction(ep)(default(CancellationToken));
            }
            catch (RobotRaconteurRemoteException e)
            {
                errthrown = true;
            }

            if (!errthrown) throw new Exception();
            return;
        }

        Callback<Func<CancellationToken, Task>> m_cb1;
        public Callback<Func<CancellationToken, Task>> cb1
        {
            get { return m_cb1; }
            set { m_cb1 = value; }
        }

        Callback<Func<double, double, CancellationToken, Task>> m_cb2;
        public Callback<Func<double, double, CancellationToken, Task>> cb2
        {
            get { return m_cb2; }
            set { m_cb2 = value; }
        }

        Callback<Func<double, double, CancellationToken, Task<double>>> m_cb3;
        public Callback<Func<double, double, CancellationToken, Task<double>>> cb3
        {
            get { return m_cb3; }
            set { m_cb3 = value; }
        }

        Callback<Func<CancellationToken, Task<int>>> m_cb_meaning_of_life;
        public Callback<Func<CancellationToken, Task<int>>> cb_meaning_of_life
        {
            get { return m_cb_meaning_of_life; }
            set { m_cb_meaning_of_life = value; }
        }

        Callback<Func<CancellationToken, Task>> m_cb_errtest;
        public Callback<Func<CancellationToken, Task>> cb_errtest
        {
            get { return m_cb_errtest; }
            set { m_cb_errtest = value; }
        }

        Wire<double[]> m_w1;
        public Wire<double[]> w1
        {
            get
            {
                return m_w1;
            }
            set
            {
                m_w1 = value;
                m_w1.WireConnectCallback = w1_connect_callback;
            }
        }
        void w1_connect_callback(Wire<double[]> wire, Wire<double[]>.WireConnection con)
        {
            con.WireValueChanged += w1_value_changed;
        }

        void w1_value_changed(Wire<double[]>.WireConnection con, double[] val, TimeSpec time)
        {
            con.OutValue = con.InValue;
        }

        Wire<teststruct2> m_w2;
        public Wire<teststruct2> w2
        {
            get
            {
                return m_w2;
            }
            set
            {
                m_w2 = value;
                m_w2.WireConnectCallback = w2_connect_callback;
            }
        }
        void w2_connect_callback(Wire<teststruct2> wire, Wire<teststruct2>.WireConnection con)
        {
            con.WireValueChanged += w2_value_changed;
        }

        void w2_value_changed(Wire<teststruct2>.WireConnection con, teststruct2 val, TimeSpec time)
        {
            con.OutValue = con.InValue;
        }

        Wire<MultiDimArray> m_w3;
        public Wire<MultiDimArray> w3
        {
            get
            {
                return m_w3;
            }
            set
            {
                m_w3 = value;
                m_w3.WireConnectCallback = w3_connect_callback;
            }
        }
        void w3_connect_callback(Wire<MultiDimArray> wire, Wire<MultiDimArray>.WireConnection con)
        {
            con.WireValueChanged += w3_value_changed;
        }

        void w3_value_changed(Wire<MultiDimArray>.WireConnection con, MultiDimArray val, TimeSpec time)
        {
            con.OutValue = con.InValue;
        }

        public Task<int[]> get_i32_huge(CancellationToken cancel = default(CancellationToken))
        {
            var o = new int[2621440];
            for (int i = 0; i < 2621440; i++)
            {
                o[i] = i;
            }

            return Task.FromResult(o);
        }

        public Task set_i32_huge(int[] value, CancellationToken cancel = default(CancellationToken))
        {
            if (value.Length != 2621440) throw new Exception();
            for (int i = 0; i < 2621440; i++)
            {
                if (value[i] != -i)
                    throw new Exception();
            }
            return Task.FromResult(0);
        }

        //TODO: implement broadcast wire
        Wire<double> m_broadcastwire;
        public Wire<double> broadcastwire
        {
            get
            {
                return m_broadcastwire;
            }
            set
            {
                m_broadcastwire = value;
                //m_w1.WireConnectCallback = w1_connect_callback;
            }
        }

        ArrayMemory<double> m_m1 = new ArrayMemory<double>(TestVectors.Get<double>("ts1.m1.initial"));
        public ArrayMemory<double> m1
        {
            get
            {

                return m_m1;
            }
            set { throw new InvalidOperationException(); }
        }
        MultiDimArrayMemory<double> m_m2 = new MultiDimArrayMemory<double>(new MultiDimArray(new uint[] { 10, 10, 10, 10, 10 }, new double[100000]));
        public MultiDimArrayMemory<double> m2
        {
            get
            {

                return m_m2;
            }
            set { throw new InvalidOperationException(); }
        }
        MultiDimArrayMemory<byte> m_m3 = new MultiDimArrayMemory<byte>(new MultiDimArray(new uint[] { 1024, 1024 }, new byte[1024 * 1024]));
        public MultiDimArrayMemory<byte> m3
        {
            get
            {

                return m_m3;
            }
            set { throw new InvalidOperationException(); }
        }
    }

    public class sub1_impl : sub1, IRobotRaconteurMonitorObject
    {
        double[] m_d1;
        //public double[] d1 {
        public Task<double[]> get_d1(CancellationToken cancel = default(CancellationToken)) { return Task.FromResult(m_d1); }
        public Task set_d1(double[] value, CancellationToken cancel = default(CancellationToken)) { m_d1 = value; return Task.FromResult(0); }
        //}

        MultiDimArray m_d2;
        //public MultiDimArray d2 {
        public Task<MultiDimArray> get_d2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_d2);
        }
        public Task set_d2(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            m_d2 = value;
            return Task.FromResult(0);
        }
        //}

        public string m_s_ind;

        //public string s_ind {
        public Task<string> get_s_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_s_ind);
        }
        public Task set_s_ind(string value, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
        //}

        public int m_i_ind;
        //public int i_ind {
        public Task<int> get_i_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_i_ind);
        }
        public Task set_i_ind(int value, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
        //}

        sub2_impl o2_1 = new sub2_impl();

        public Task<sub2> get_o2_1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<sub2>(o2_1);
        }

        Dictionary<int, sub2_impl> o2_2 = new Dictionary<int, sub2_impl>();

        public Task<sub2> get_o2_2(int ind, CancellationToken cancel = default(CancellationToken))
        {
            if (o2_2.ContainsKey(ind)) return Task.FromResult<sub2>(o2_2[ind]);
            sub2_impl o = new sub2_impl();
            o.set_i_ind(ind).GetAwaiter().GetResult();
            o2_2[ind] = o;
            return Task.FromResult<sub2>(o);
        }

        Dictionary<string, sub2_impl> o2_3 = new Dictionary<string, sub2_impl>();

        public Task<sub2> get_o2_3(string ind, CancellationToken cancel = default(CancellationToken))
        {
            if (o2_3.ContainsKey(ind)) return Task.FromResult<sub2>(o2_3[ind]);
            sub2_impl o = new sub2_impl();
            o.set_s_ind(ind).GetAwaiter().GetResult();
            o2_3[ind] = o;
            return Task.FromResult<sub2>(o);
        }

        AsyncMutex mymutex = new AsyncMutex();

        public Task<IDisposable> RobotRaconteurMonitorEnter()
        {
            return mymutex.Lock();
        }

        public Task<IDisposable> RobotRaconteurMonitorEnter(int timeout)
        {
            return mymutex.LockWithTimeout(timeout);
            //if(!Monitor.TryEnter(this,timeout)) throw new TimeoutException();
        }
    }

    public class sub2_impl : sub2
    {
        public string m_s_ind;

        //public string s_ind
        //{
        public Task<string> get_s_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_s_ind);
        }
        public Task set_s_ind(string value, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
        //}

        public int m_i_ind;
        //public int i_ind
        //{
        public Task<int> get_i_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_i_ind);
        }
        public Task set_i_ind(int value, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
        //}

        string m_data;
        //public string data {
        public Task<string> get_data(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_data);
        }
        public Task set_data(string value, CancellationToken cancel = default(CancellationToken))
        {
            m_data = value;
            return Task.FromResult(0);
        }
        //}

        Dictionary<string, sub3_impl> o3_1 = new Dictionary<string, sub3_impl>();

        public Task<sub3> get_o3_1(string ind, CancellationToken cancel = default(CancellationToken))
        {
            if (o3_1.ContainsKey(ind)) return Task.FromResult<sub3>(o3_1[ind]);
            sub3_impl o = new sub3_impl();
            o.m_ind = ind;
            o3_1[ind] = o;
            return Task.FromResult<sub3>(o);
        }

    }

    public class sub3_impl : sub3
    {
        string m_data2;
        //public string data2 {
        public Task<string> get_data2(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_data2);
        }
        public Task set_data2(string value, CancellationToken cancel = default(CancellationToken))
        {
            m_data2 = value;
            return Task.FromResult(0);
        }
        //}

        double m_data3;
        //public double data3 {
        public Task<double> get_data3(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_data3);
        }
        public Task set_data3(double value, CancellationToken cancel = default(CancellationToken))
        {
            m_data3 = value;
            return Task.FromResult(0);
        }
        //}

        public Task<double> add(double d, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(d + 42);
        }

        public string m_ind;
        //public string ind
        //{
        public Task<string> get_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(m_ind);
        }
        public Task set_ind(string value, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
        //}



    }

    public class TestService2SubObj : com.robotraconteur.testing.TestService2.subobj
    {
        public Task<double> add_val(double v, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(v + 1);
        }
    }
}
