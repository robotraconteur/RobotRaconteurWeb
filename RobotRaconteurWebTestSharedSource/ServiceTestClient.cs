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
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using com.robotraconteur.testing.TestService2;
using RobotRaconteurWeb;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurTest
{
    public class ServiceTestClient
    {
        public RobotRaconteurNode node;

        public ServiceTestClient()
        {
            node = RobotRaconteurNode.s;
        }

        public ServiceTestClient(RobotRaconteurNode node)
        {
            this.node = node;
        }

        public void ca<T>(T[] v1, T[] v2)
        {
            RRAssert.AreEqual(v1.Length, v2.Length);
            for (int i = 0; i < v1.Length; i++)
            {
                RRAssert.IsTrue(Math.Abs((double)((dynamic)v1[i] - (dynamic)v2[i])) < 1e-6);
            }
        }

        public async Task RunFullTest(string[] url, string[] authurl)
        {
            RRWebTest.WriteLine("Test Members");
            await ConnectService(url);
            await TestProperties();
            await TestFunctions();
            await TestEvents();
            await TestObjRefs();
            await TestPipes();
            await TestCallbacks();
            await TestWires();
            await TestMemories();
            await DisconnectService();

            RRWebTest.WriteLine("Test Authentication");
            await TestAuthentication(authurl);
            RRWebTest.WriteLine("Test Object Locks");
            await TestObjectLock(authurl);
            RRWebTest.WriteLine("Test Monitor Locks");
            await TestMonitorLock(url);
            RRWebTest.WriteLine("Done!");
        }

        testroot r;

        public async Task ConnectService(string[] url)
        {
            r = (testroot)await node.ConnectService(url);
        }


        public async Task TestProperties()
        {
            await r.set_d1(3.456);
            RRAssert.AreEqual(await r.get_d1(), 12.345);
            await r.set_d2(TestVectors.Get<double>("ts1.d2.to_service"));
            ca<double>(await r.get_d2(), TestVectors.Get<double>("ts1.d2.to_client"));
            await r.set_d3(TestVectors.Get<double>("ts1.d3.to_service"));
            ca<double>(await r.get_d3(), TestVectors.Get<double>("ts1.d3.to_client"));
            await r.set_d4(TestVectors.Get<double>("ts1.d4.to_service"));
            ca<double>(await r.get_d4(), TestVectors.Get<double>("ts1.d4.to_client"));


            MultiDimArray d5_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.d5.to_service.dims"),
                TestVectors.Get<double>("ts1.d5.to_service")
                );

            await r.set_d5(d5_1);

            MultiDimArray d5_2 = await r.get_d5();
            ca<uint>(d5_2.Dims, TestVectors.Get<uint>("ts1.d5.to_client.dims"));
            ca<double>((double[])d5_2.Array_, TestVectors.Get<double>("ts1.d5.to_client"));

            MultiDimArray d6_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.d6.to_service.dims"),
                TestVectors.Get<double>("ts1.d6.to_service")

                );

            await r.set_d6(d6_1);

            MultiDimArray d6_2 = await r.get_d6();
            ca<uint>(d6_2.Dims, TestVectors.Get<uint>("ts1.d6.to_client.dims"));
            ca<double>((double[])d6_2.Array_, TestVectors.Get<double>("ts1.d6.to_client"));



            await r.set_s1(3847.9283f);
            //if (await r.get_s1() != 7.8573f) throw new Exception();
            await r.get_s1();
            await r.set_s2(TestVectors.Get<float>("ts1.s2.to_service"));
            //ca<float>(await r.get_s2(), TestVectors.Get<float>("ts1.s2.to_client"));
            await r.get_s2();

            await r.set_i8_1(45);
            RRAssert.AreEqual(await r.get_i8_1(), -66);
            await r.set_i8_2(TestVectors.Get<sbyte>("ts1.i8_2.to_service"));
            ca<sbyte>(await r.get_i8_2(), TestVectors.Get<sbyte>("ts1.i8_2.to_client"));

            await r.set_u8_1(232);
            RRAssert.AreEqual(await r.get_u8_1(), 222);
            await r.set_u8_2(TestVectors.Get<byte>("ts1.u8_2.to_service"));
            ca<byte>(await r.get_u8_2(), TestVectors.Get<byte>("ts1.u8_2.to_client"));

            MultiDimArray u8_3_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.u8_3.to_service.dims"), TestVectors.Get<byte>("ts1.u8_3.to_service"));
            await r.set_u8_3(u8_3_1);
            MultiDimArray u8_3_2 = await r.get_u8_3();
            ca<uint>(TestVectors.Get<uint>("ts1.u8_3.to_client.dims"), u8_3_2.Dims);
            ca<byte>((byte[])u8_3_2.Array_, TestVectors.Get<byte>("ts1.u8_3.to_client"));

            await r.set_i16_1(2387);
            RRAssert.AreEqual(await r.get_i16_1(), -13428);
            await r.set_i16_2(TestVectors.Get<short>("ts1.i16_2.to_service"));
            ca<short>(await r.get_i16_2(), TestVectors.Get<short>("ts1.i16_2.to_client"));

            await r.set_u16_1(54732);
            RRAssert.AreEqual(await r.get_u16_1(), 60981);
            await r.set_u16_2(TestVectors.Get<ushort>("ts1.u16_2.to_service"));
            ca<ushort>(await r.get_u16_2(), TestVectors.Get<ushort>("ts1.u16_2.to_client"));

            await r.set_i32_1(-9837284);
            RRAssert.AreEqual(await r.get_i32_1(), 898734);
            await r.set_i32_2(TestVectors.Get<int>("ts1.i32_2.to_service"));
            ca<int>(await r.get_i32_2(), TestVectors.Get<int>("ts1.i32_2.to_client"));

            {
                var i32_huge_1 = new int[2621440];
                for (int i = 0; i < 2621440; i++)
                {
                    i32_huge_1[i] = -i;
                }
                await r.set_i32_huge(i32_huge_1);
            }
            {
                var i32_huge_2 = await r.get_i32_huge();
                RRAssert.AreEqual(i32_huge_2.Length, 2621440);
                for (int i = 0; i < 2621440; i++)
                {
                    RRAssert.AreEqual(i32_huge_2[i], i);
                }
            }

            await r.set_u32_1(1550099195);
            RRAssert.AreEqual(await r.get_u32_1(), 547919123U);
            await r.set_u32_2(TestVectors.Get<uint>("ts1.u32_2.to_service"));
            ca<uint>(await r.get_u32_2(), TestVectors.Get<uint>("ts1.u32_2.to_client"));

            await r.set_i64_1(8621740821050813024);
            RRAssert.AreEqual(await r.get_i64_1(), -1357833931563696072);
            await r.set_i64_2(TestVectors.Get<long>("ts1.i64_2.to_service"));
            ca<long>(await r.get_i64_2(), TestVectors.Get<long>("ts1.i64_2.to_client"));

            await r.set_u64_1(1465640522145789825);
            RRAssert.AreEqual(await r.get_u64_1(), 13389861970863644378);
            await r.set_u64_2(TestVectors.Get<ulong>("ts1.u64_2.to_service"));
            ca<ulong>(await r.get_u64_2(), TestVectors.Get<ulong>("ts1.u64_2.to_client"));

            await r.set_str1("Hello Server!");
            RRAssert.AreEqual(await r.get_str1(), "Hello Client!");

            //Read in and check the large structure struct1
            teststruct1 s1 = await r.get_struct1();
            ca<double>(s1.dat1, TestVectors.Get<double>("ts1.struct1.dat1.to_client"));
            RRAssert.AreEqual(s1.str2, "Hello world!");
            RRAssert.AreEqual(s1.vec3.Count, 3);
            RRAssert.AreEqual(s1.vec3[1], "Hello Client!");
            RRAssert.AreEqual(s1.vec3[2], "Hello Client, again");
            RRAssert.AreEqual(s1.vec3[4372], "This is yet another test string");

            RRAssert.AreEqual(s1.dict4.Count, 3);
            RRAssert.AreEqual(s1.dict4["teststring1"], "Hello Client!");
            RRAssert.AreEqual(s1.dict4["teststring2"], "Hello Client, again");
            RRAssert.AreEqual(s1.dict4["anotherstr"], "This is yet another test string");

            //TODO: test list

            ca<double>(s1.struct1.mydat, TestVectors.Get<double>("ts1.struct1.struct1_mydat.to_client"));

            RRAssert.AreEqual(s1.dstruct2.Count, 2);
            ca<double>(s1.dstruct2["test1"].mydat, TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_client"));
            ca<double>(s1.dstruct2["anothertest"].mydat, TestVectors.Get<double>("ts1.struct1.dstruct2_3.to_client"));

            ca<uint>(s1.multidimarray.Dims, TestVectors.Get<uint>("ts1.struct1.multidimarray.to_client.dims"));
            ca<double>((double[])s1.multidimarray.Array_, TestVectors.Get<double>("ts1.struct1.multidimarray.to_client"));

            RRAssert.AreEqual((string)s1.var3, "This is a vartype string");

            //Set large structure for struct1
            teststruct1 s2 = new teststruct1();
            s2.dat1 = TestVectors.Get<double>("ts1.struct1.dat1.to_service");
            s2.str2 = "Hello world 2!";
            Dictionary<int, string> s2_3 = new Dictionary<int, string>();
            s2_3.Add(10, "Hello Server!");
            s2_3.Add(11, "Hello Server, again");
            s2_3.Add(46372, "Test string!");
            s2_3.Add(46373, "Test string again");

            s2.vec3 = s2_3;

            Dictionary<string, string> s2_4 = new Dictionary<string, string>();
            s2_4.Add("cteststring1", "Hello Server!");
            s2_4.Add("cteststring2", "Hello Server, again");
            s2.dict4 = s2_4;

            List<string> s2_5 = new List<string>();
            s2_5.Add("Hello Server!");
            s2_5.Add("Hello Server, again");
            s2.list5 = s2_5;

            s2.struct1 = new teststruct2();
            s2.struct1.mydat = TestVectors.Get<double>("ts1.struct1.struct1_mydat.to_service");

            Dictionary<string, teststruct2> s2_ds2 = new Dictionary<string, teststruct2>();
            teststruct2 s2_ds2_1 = new teststruct2();
            s2_ds2_1.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_service");
            s2_ds2.Add("ctest1", s2_ds2_1);
            teststruct2 s2_ds2_2 = new teststruct2();
            s2_ds2_2.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_2.to_service");
            s2_ds2.Add("anothertest", s2_ds2_2);
            s2.dstruct2 = s2_ds2;

            List<teststruct2> s2_ds3 = new List<teststruct2>();
            teststruct2 s2_ds3_1 = new teststruct2();
            s2_ds3_1.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_1.to_service");
            s2_ds3.Add(s2_ds2_1);
            teststruct2 s2_ds3_2 = new teststruct2();
            s2_ds3_2.mydat = TestVectors.Get<double>("ts1.struct1.dstruct2_2.to_service");
            s2_ds3.Add(s2_ds2_2);
            s2.lstruct3 = s2_ds3;


            s2.multidimarray = new MultiDimArray(TestVectors.Get<uint>("ts1.struct1.multidimarray.to_service.dims"), TestVectors.Get<double>("ts1.struct1.multidimarray.to_service"));
            s2.var3 = TestVectors.Get<double>("ts1.struct1.var3.to_service");
            await r.set_struct1(s2);

            //Check struct2 in/out
            teststruct2 s3 = new teststruct2();
            s3.mydat = TestVectors.Get<double>("ts1.struct2.to_service");
            await r.set_struct2(s3);
            teststruct2 s4 = await r.get_struct2();
            ca<double>(s4.mydat, TestVectors.Get<double>("ts1.struct2.to_client"));

            //Test is_d1
            Dictionary<int, double> is_d1_1 = await r.get_is_d1();
            RRAssert.AreEqual(is_d1_1.Count, 3);
            RRAssert.AreEqual(is_d1_1[9285], 1.643392e-01);
            RRAssert.AreEqual(is_d1_1[74822], 1.537133e+09);
            RRAssert.AreEqual(is_d1_1[4], 1.369505e-03);

            Dictionary<int, double> is_d1_2 = new Dictionary<int, double>();
            is_d1_2.Add(928, 4.074501e-07);
            is_d1_2.Add(394820, -4.535303e+05);
            is_d1_2.Add(623, -2.956241e-20);
            await r.set_is_d1(is_d1_2);

            //Test is_d2
            Dictionary<string, double> is_d2_1 = await r.get_is_d2();
            RRAssert.AreEqual(is_d2_1.Count, 2);
            RRAssert.AreEqual(is_d2_1["testval1"], -1.079664e+16);
            RRAssert.AreEqual(is_d2_1["testval2"], 2.224846e+00);

            Dictionary<string, double> is_d2_2 = new Dictionary<string, double>();
            is_d2_2.Add("testval3", 5.242474e+10);
            is_d2_2.Add("testval4", 2.208636e+08);
            await r.set_is_d2(is_d2_2);

            //Test is_d3

            Dictionary<int, double[]> is_d3_1 = await r.get_is_d3();
            RRAssert.AreEqual(is_d3_1.Count, 2);
            ca<double>(is_d3_1[12], TestVectors.Get<double>("ts1.is_d3.to_client.a"));
            ca<double>(is_d3_1[832], TestVectors.Get<double>("ts1.is_d3.to_client.b"));

            Dictionary<int, double[]> is_d3_2 = new Dictionary<int, double[]>();
            is_d3_2.Add(47, TestVectors.Get<double>("ts1.is_d3.to_service.a"));
            is_d3_2.Add(324, TestVectors.Get<double>("ts1.is_d3.to_service.b"));
            await r.set_is_d3(is_d3_2);

            //Test is_d4

            Dictionary<string, double[]> is_d4_1 = await r.get_is_d4();
            RRAssert.AreEqual(is_d4_1.Count, 2);
            ca<double>(is_d4_1["testval1"], TestVectors.Get<double>("ts1.is_d4.to_client.a"));
            ca<double>(is_d4_1["testval2"], TestVectors.Get<double>("ts1.is_d4.to_client.b"));

            Dictionary<string, double[]> is_d4_2 = new Dictionary<string, double[]>();
            is_d4_2.Add("testval3", TestVectors.Get<double>("ts1.is_d4.to_service.a"));
            is_d4_2.Add("testval4", TestVectors.Get<double>("ts1.is_d4.to_service.b"));
            await r.set_is_d4(is_d4_2);

            //Test is_d5
            Dictionary<int, MultiDimArray> is_d5_1 = await r.get_is_d5();
            RRAssert.AreEqual(is_d5_1.Count, 1);
            MultiDimArray is_d5_1_1 = is_d5_1[564];
            ca<uint>(is_d5_1_1.Dims, TestVectors.Get<uint>("ts1.is_d5.to_client.dims"));
            ca<double>((double[])is_d5_1_1.Array_, TestVectors.Get<double>("ts1.is_d5.to_client"));

            MultiDimArray is_d5_2_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d5.to_service.dims"), TestVectors.Get<double>("ts1.is_d5.to_service"));
            Dictionary<int, MultiDimArray> is_d5_2 = new Dictionary<int, MultiDimArray>();
            is_d5_2.Add(328, is_d5_2_1);
            //TODO: need setter?

            //Test is_d6
            Dictionary<string, MultiDimArray> is_d6_1 = await r.get_is_d6();
            RRAssert.AreEqual(is_d6_1.Count, 1);
            MultiDimArray is_d6_1_1 = is_d6_1["testval1"];
            ca<uint>(is_d6_1_1.Dims, TestVectors.Get<uint>("ts1.is_d6.to_client.dims"));
            ca<double>((double[])is_d6_1_1.Array_, TestVectors.Get<double>("ts1.is_d6.to_client"));

            MultiDimArray is_d6_2_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d6.to_service.dims"), TestVectors.Get<double>("ts1.is_d6.to_service"));
            Dictionary<string, MultiDimArray> is_d6_2 = new Dictionary<string, MultiDimArray>();
            is_d6_2.Add("testval2", is_d6_2_1);
            await r.set_is_d6(is_d6_2);

            //TODO: add list checks

            //Test is_str1
            Dictionary<int, string> is_str1_1 = await r.get_is_str1();
            RRAssert.AreEqual(is_str1_1.Count, 1);
            RRAssert.AreEqual(is_str1_1[23], "Hello server");

            Dictionary<int, string> is_str1_2 = new Dictionary<int, string>();
            is_str1_2.Add(24, "Hello client");
            await r.set_is_str1(is_str1_2);

            //Test is_str2
            Dictionary<string, string> is_str2_1 = await r.get_is_str2();
            RRAssert.AreEqual(is_str2_1.Count, 1);
            RRAssert.AreEqual(is_str2_1["testval1"], "Hello server");

            Dictionary<string, string> is_str2_2 = new Dictionary<string, string>();
            is_str2_2.Add("testval2", "Hello client");
            await r.set_is_str2(is_str2_2);

            //Test is_struct1
            Dictionary<int, teststruct2> is_struct1_1 = await r.get_is_struct1();
            ca<double>(is_struct1_1[748].mydat, TestVectors.Get<double>("ts1.is_struct1.to_client"));

            Dictionary<int, teststruct2> is_struct1_2 = new Dictionary<int, teststruct2>();
            teststruct2 is_struct1_2_1 = new teststruct2();
            is_struct1_2_1.mydat = TestVectors.Get<double>("ts1.is_struct1.to_service");
            is_struct1_2.Add(372, is_struct1_2_1);
            await r.set_is_struct1(is_struct1_2);

            //Test is_struct2
            Dictionary<string, teststruct2> is_struct2_1 = await r.get_is_struct2();
            ca<double>(is_struct2_1["testval1"].mydat, TestVectors.Get<double>("ts1.is_struct2.to_client"));

            Dictionary<string, teststruct2> is_struct2_2 = new Dictionary<string, teststruct2>();
            teststruct2 is_struct2_2_1 = new teststruct2();
            is_struct2_2_1.mydat = TestVectors.Get<double>("ts1.is_struct2.to_service");
            is_struct2_2.Add("testval2", is_struct2_2_1);
            await r.set_is_struct2(is_struct2_2);

            //Test list_d1
            List<double> list_d1_1 = await r.get_list_d1();
            RRAssert.AreEqual(list_d1_1.Count, 3);
            RRAssert.AreEqual(list_d1_1[0], 1.643392e-01);
            RRAssert.AreEqual(list_d1_1[1], 1.537133e+09);
            RRAssert.AreEqual(list_d1_1[2], 1.369505e-03);

            List<double> list_d1_2 = new List<double>();
            list_d1_2.Add(4.074501e-07);
            list_d1_2.Add(-4.535303e+05);
            list_d1_2.Add(-2.956241e-20);
            await r.set_list_d1(list_d1_2);

            //Test list_d3
            List<double[]> list_d3_1 = await r.get_list_d3();
            RRAssert.AreEqual(list_d3_1.Count, 2);
            ca<double>(list_d3_1[0], TestVectors.Get<double>("ts1.is_d3.to_client.a"));
            ca<double>(list_d3_1[1], TestVectors.Get<double>("ts1.is_d3.to_client.b"));

            List<double[]> list_d3_2 = new List<double[]>();
            list_d3_2.Add(TestVectors.Get<double>("ts1.is_d3.to_service.a"));
            list_d3_2.Add(TestVectors.Get<double>("ts1.is_d3.to_service.b"));
            await r.set_list_d3(list_d3_2);

            //Test list_d5
            List<MultiDimArray> list_d5_1 = await r.get_list_d5();
            RRAssert.AreEqual(list_d5_1.Count, 1);
            MultiDimArray list_d5_1_1 = list_d5_1[0];
            ca<uint>(list_d5_1_1.Dims, TestVectors.Get<uint>("ts1.is_d5.to_client.dims"));
            ca<double>((double[])list_d5_1_1.Array_, TestVectors.Get<double>("ts1.is_d5.to_client"));

            MultiDimArray list_d5_2_1 = new MultiDimArray(TestVectors.Get<uint>("ts1.is_d5.to_service.dims"), TestVectors.Get<double>("ts1.is_d5.to_service"));
            List<MultiDimArray> list_d5_2 = new List<MultiDimArray>();
            list_d5_2.Add(list_d5_2_1);
            await r.set_list_d5(list_d5_2);

            //Test list_struct1
            List<teststruct2> list_struct1_1 = await r.get_list_struct1();
            ca<double>(list_struct1_1[0].mydat, TestVectors.Get<double>("ts1.is_struct1.to_client"));

            List<teststruct2> list_struct1_2 = new List<teststruct2>();
            teststruct2 list_struct1_2_1 = new teststruct2();
            list_struct1_2_1.mydat = TestVectors.Get<double>("ts1.is_struct1.to_service");
            list_struct1_2.Add(is_struct1_2_1);
            await r.set_list_struct1(list_struct1_2);

            //Test struct3
            ca<double>((await r.get_struct3()).a1, TestVectors.Get<double>("ts1.struct3.to_client"));

            ostruct2 struct3_1 = new ostruct2();
            struct3_1.a1 = TestVectors.Get<double>("ts1.struct3.to_service");
            await r.set_struct3(struct3_1);

            //Test var1  TODO...

            //Test var2 TODO...

            //Test var_num
            ca<int>((int[])await r.get_var_num(), TestVectors.Get<int>("ts1.var_num.to_client"));
            await r.set_var_num(TestVectors.Get<int>("ts1.var_num.to_service"));

            //Test var_str
            RRAssert.AreEqual(((string)await r.get_var_str()), "Hello Client!");
            await r.set_var_str("Hello Server!");

            //Test var_struct
            ca<double>(((teststruct2)await r.get_var_struct()).mydat, TestVectors.Get<double>("ts1.var_struct.to_client"));

            teststruct2 var_struct_1 = new teststruct2();
            var_struct_1.mydat = TestVectors.Get<double>("ts1.var_struct.to_service");
            await r.set_var_struct(var_struct_1);

            //Test var_vector
            RRAssert.AreEqual(((string)((Dictionary<int, object>)await r.get_var_vector())[10]), "Hello Client!");
            Dictionary<int, object> var_vector_1 = new Dictionary<int, object>();
            var_vector_1.Add(11, "Hello Server!");
            await r.set_var_vector(var_vector_1);

            //Test var_dictionary
            RRAssert.AreEqual(((string)((Dictionary<string, object>)await r.get_var_dictionary())["test1"]), "Hello Client!");
            Dictionary<string, object> var_dictionary_1 = new Dictionary<string, object>();
            var_dictionary_1.Add("test2", "Hello Server!");
            await r.set_var_dictionary(var_dictionary_1);

            //Test var_list
            RRAssert.AreEqual(((string)((List<object>)await r.get_var_list())[0]), "Hello Client!");
            List<object> var_list_1 = new List<object>();
            var_list_1.Add("Hello Server!");
            await r.set_var_list(var_list_1);

            //Test var_multidimarray
            MultiDimArray var_a_1 = (MultiDimArray)await r.get_var_multidimarray();
            ca<uint>(var_a_1.Dims, TestVectors.Get<uint>("ts1.var_multidimarray.to_client.dims"));
            ca<double>((double[])var_a_1.Array_, TestVectors.Get<double>("ts1.var_multidimarray.to_client"));
            await r.set_var_multidimarray(new MultiDimArray(TestVectors.Get<uint>("ts1.var_multidimarray.to_service.dims"), TestVectors.Get<double>("ts1.var_multidimarray.to_service")));

            //Test errtest and make sure an exception is thrown

            bool err1 = false;
            try
            {
                await r.set_errtest(1);
            }
            catch (Exception e)
            {
                err1 = true;
            }

            RRAssert.IsTrue(err1);

            bool err2 = false;
            try
            {
                double d = await r.get_errtest();
            }
            catch (Exception e)
            {
                err2 = true;
            }
            RRAssert.IsTrue(err2);

            //Test nulltest
            RRAssert.AreEqual((object)await r.get_nulltest(), (object)null);
            await r.set_nulltest(null);



        }

        public async Task TestFunctions()
        {
            await r.func1();
            await r.func2(10, 20.34);
            RRAssert.AreEqual(await r.func3(2, 3.45), 5.45);
            RRAssert.AreEqual(await r.meaning_of_life(), 42);

            bool errthrown = false;

            try
            {
                await r.func_errtest();
            }
            catch (RobotRaconteurRemoteException e)
            {
                errthrown = true;
            }
            RRAssert.IsTrue(errthrown);

            errthrown = false;
            try
            {
                await r.func_errtest1();
            }
            catch (DataTypeException e)
            {
                errthrown = true;
            }
            RRAssert.IsTrue(errthrown);

            errthrown = false;
            try
            {
                await r.func_errtest2();
            }
            catch (testexception1 e)
            {
                errthrown = true;
            }
            RRAssert.IsTrue(errthrown);

            errthrown = false;
            try
            {
                await r.func_errtest3();
            }
            catch (testexception3 e)
            {
                errthrown = true;
            }
            RRAssert.IsTrue(errthrown);
        }

        public async Task TestEvents()
        {
            //TODO: improve this function
            r.ev1 += ev1_cb;
            r.ev2 += ev2_cb;

            try
            {
                ev1_event = new TaskCompletionSource<int>();
                ev2_event = new TaskCompletionSource<int>();

                await r.func1();

                await r.func2(27.3, 98.23);

                await Task.Delay(2000);

                await ev1_event.Task.AwaitWithTimeout(2000);
                await ev2_event.Task.AwaitWithTimeout(2000);
            }
            finally
            {
                r.ev1 -= ev1_cb;
                r.ev2 -= ev2_cb;

                ev1_event = null;
                ev2_event = null;
            }



        }


        TaskCompletionSource<int> ev1_event;
        private void ev1_cb()
        {
            ev1_event.TrySetResult(1);
        }

        TaskCompletionSource<int> ev2_event;
        private void ev2_cb(double d, teststruct2 s)
        {
            if (d == 27.3 && s.mydat[0] == 98.23)
            {
                ev2_event.TrySetResult(1);
            }
        }

        public async Task TestObjRefs()
        {
            // cSpell: ignore myind
            sub1 o1 = await r.get_o1();
            sub1 o2_10 = await r.get_o2(10);
            sub1 o2_34 = await r.get_o2(34);
            sub1 o3_1 = await r.get_o3(1);
            sub1 o4_myind = await r.get_o4("myind");
            sub1 o4_specialind = await r.get_o4("ind!@#$%^&*().<>     ");

            sub2 o2_10_o2_1_32 = await o2_10.get_o2_2(32);

            sub3 o2_10_o2_1_32_o3_ind1 = await o2_10_o2_1_32.get_o3_1("ind1");
            sub3 o2_10_o2_1_32_o3_ind2 = await o2_10_o2_1_32.get_o3_1("ind2");

            await o1.set_d1(TestVectors.Get<double>("ts1.client.o_d1a"));
            await o2_10.set_d1(TestVectors.Get<double>("ts1.client.o_d1b"));
            await o2_34.set_d1(TestVectors.Get<double>("ts1.client.o_d1c"));
            await o3_1.set_d1(TestVectors.Get<double>("ts1.client.o_d1d"));
            await o4_myind.set_d1(TestVectors.Get<double>("ts1.client.o_d1e"));
            await o4_specialind.set_d1(TestVectors.Get<double>("ts1.client.o_d1f"));
            await o2_10_o2_1_32.set_data("Hello world!");
            await o2_10_o2_1_32_o3_ind1.set_data2("Test string 1");
            await o2_10_o2_1_32_o3_ind2.set_data2("Test string 2");

            //Test the varobject type and make sure the ReleaseServicePath works...

            await r.o6_op(0);
            sub1 o6_1 = (sub1)await r.get_o6();
            sub2 o6_1_1 = await o6_1.get_o2_1();
            await o6_1.set_d1(new double[] { 0.0 });
            await o6_1_1.set_data("Hello world!");


            await r.o6_op(1);
            await ShouldBeErr<ServiceException>(async delegate () { await o6_1.set_d1(new double[] { 0.0 }); });
            await ShouldBeErr<ServiceException>(async delegate () { await o6_1_1.set_data("Hello world!"); });

            sub2 o6_2 = (sub2)await r.get_o6();
            await o6_2.set_data("Hello world!");

            await r.o6_op(2);
            await ShouldBeErr<ServiceException>(async delegate () { await o6_2.set_data("Hello world!"); });

            com.robotraconteur.testing.TestService2.subobj o6_3 = (com.robotraconteur.testing.TestService2.subobj)await r.get_o6();
            await o6_3.add_val(2);

        }

        TaskCompletionSource<int> ee1;
        TaskCompletionSource<int> ee2;
        TaskCompletionSource<int> ee3;

        uint packetnum = 0;
        bool ack_recv = false;

        public async Task TestPipes()
        {
            //TODO: improve this function
            ee1 = new TaskCompletionSource<int>();
            ee2 = new TaskCompletionSource<int>();
            ee3 = new TaskCompletionSource<int>();

            Pipe<double[]>.PipeEndpoint e1 = await r.p1.Connect(-1);
            Pipe<double[]>.PipeEndpoint e2 = await r.p1.Connect(3432);
            Pipe<teststruct2>.PipeEndpoint e3 = await r.p2.Connect(-1);

            e1.RequestPacketAck = true;

            e1.PacketReceivedEvent += ee1_cb;
            e1.PacketAckReceivedEvent += ee1_ack_cb;
            e2.PacketReceivedEvent += ee2_cb;
            e3.PacketReceivedEvent += ee3_cb;

            packetnum = await e1.SendPacket(new double[] { 1, 2, 3, 4 });
            await e1.SendPacket(new double[] { 5, 6, 7, 8 });
            await e1.SendPacket(TestVectors.Get<double>("ts1.client.d3"));

            await e2.SendPacket(new double[] { 3.21 });
            await e2.SendPacket(new double[] { 4.72 });
            await e2.SendPacket(new double[] { 72.34 });

            teststruct2 s1 = new teststruct2();
            s1.mydat = new double[] { 738.29 };
            teststruct2 s2 = new teststruct2();
            s2.mydat = new double[] { 89.83 };

            await e3.SendPacket(s1);
            await e3.SendPacket(s2);

            await Task.Delay(2000);

            await ee1.Task.AwaitWithTimeout(5000);
            await ee2.Task.AwaitWithTimeout(5000);
            await ee3.Task.AwaitWithTimeout(5000);

            ca<double>(e1.ReceivePacket(), new double[] { 1, 2, 3, 4 });
            ca<double>(e1.ReceivePacket(), new double[] { 5, 6, 7, 8 });
            ca<double>(e1.ReceivePacket(), TestVectors.Get<double>("ts1.client.d3"));

            ca<double>(e2.ReceivePacket(), new double[] { 3.21 });
            ca<double>(e2.ReceivePacket(), new double[] { 4.72 });
            ca<double>(e2.ReceivePacket(), new double[] { 72.34 });

            ca<double>(e3.ReceivePacket().mydat, new double[] { 738.29 });
            ca<double>(e3.ReceivePacket().mydat, new double[] { 89.83 });

            RRAssert.IsTrue(ack_recv);

            await r.pipe_check_error();

            await e1.Close();
            await e2.Close();
            await e3.Close();


        }

        void ee1_cb(Pipe<double[]>.PipeEndpoint p)
        {
            if (p.Available < 3) return;
            try
            {
                ee1.TrySetResult(0);
            }
            catch { }
        }

        void ee1_ack_cb(Pipe<double[]>.PipeEndpoint p, uint packetnum)
        {
            if (packetnum == this.packetnum) ack_recv = true;
        }

        void ee2_cb(Pipe<double[]>.PipeEndpoint p)
        {
            if (p.Available < 3) return;
            try
            {
                ee2.TrySetResult(0);
            }
            catch { }
        }

        void ee3_cb(Pipe<teststruct2>.PipeEndpoint p)
        {
            if (p.Available < 2) return;

            try
            {
                ee3.TrySetResult(0);
            }
            catch { }
        }

        bool cb1_called;
        bool cb2_called;
        bool cb3_called;
        bool cb4_called;
        bool cb5_called;

        public async Task TestCallbacks()
        {
            cb1_called = false;
            cb2_called = false;
            cb3_called = false;
            cb4_called = false;
            cb5_called = false;

            r.cb1.Function = cb1_func;
            r.cb2.Function = cb2_func;
            r.cb3.Function = cb3_func;
            r.cb_meaning_of_life.Function = cb_meaning_of_life_func;
            r.cb_errtest.Function = cb_errtest;



            await r.test_callbacks();

            RRAssert.IsTrue(cb1_called);
            RRAssert.IsTrue(cb2_called);
            RRAssert.IsTrue(cb3_called);
            RRAssert.IsTrue(cb4_called);
            RRAssert.IsTrue(cb5_called);
        }

        public Task cb1_func(CancellationToken cancel = default(CancellationToken))
        {
            cb1_called = true;
            return Task.FromResult(0);

        }

        public Task cb2_func(double d1, double d2, CancellationToken cancel = default(CancellationToken))
        {
            if (d1 != 739.2 || d2 != 0.392) throw new Exception();
            cb2_called = true;
            return Task.FromResult(0);
        }

        public Task<double> cb3_func(double d1, double d2, CancellationToken cancel = default(CancellationToken))
        {
            cb3_called = true;
            return Task.FromResult(d1 + d2 + 3.14);
        }

        public Task<int> cb_meaning_of_life_func(CancellationToken cancel = default(CancellationToken))
        {
            cb4_called = true;
            return Task.FromResult(42);
        }

        public Task cb_errtest(CancellationToken cancel = default(CancellationToken))
        {
            cb5_called = true;
            throw new Exception("This is a test");
        }


        bool w1_called;
        bool w2_called;
        bool w3_called;


        public async Task TestWires()
        {
            w1_called = false;
            w2_called = false;
            w3_called = false;


            Wire<double[]>.WireConnection w1 = await r.w1.Connect();
            Wire<teststruct2>.WireConnection w2 = await r.w2.Connect();
            Wire<MultiDimArray>.WireConnection w3 = await r.w3.Connect();

            w1.WireValueChanged += w1_changed;
            w2.WireValueChanged += w2_changed;
            w3.WireValueChanged += w3_changed;

            w1.OutValue = new double[] { 0 };

            teststruct2 s1 = new teststruct2();
            s1.mydat = new double[] { 0 };
            w2.OutValue = s1;

            MultiDimArray a1 = new MultiDimArray(new uint[] { 1, 1 }, new int[] { 0 });
            w3.OutValue = a1;

            w1.OutValue = TestVectors.Get<double>("ts1.client.d1");
            teststruct2 s2 = new teststruct2();
            s2.mydat = TestVectors.Get<double>("ts1.client.d2");
            w2.OutValue = s2;

            MultiDimArray a2 = new MultiDimArray(TestVectors.Get<uint>("ts1.client.i3.dims"), TestVectors.Get<int>("ts1.client.i3"));
            w3.OutValue = a2;

            //Thread.Sleep(500);
            await Task.Delay(500);


            double[] in1 = w1.InValue;
            ca<double>(in1, TestVectors.Get<double>("ts1.client.d1"));

            teststruct2 in2 = w2.InValue;
            ca<double>(in2.mydat, TestVectors.Get<double>("ts1.client.d2"));

            MultiDimArray in3 = w3.InValue;
            ca<uint>(in3.Dims, TestVectors.Get<uint>("ts1.client.i3.dims"));
            ca<int>((int[])in3.Array_, TestVectors.Get<int>("ts1.client.i3"));

            RRAssert.IsTrue(w1_called);
            RRAssert.IsTrue(w2_called);
            RRAssert.IsTrue(w3_called);
        }

        void w1_changed(Wire<double[]>.WireConnection c, double[] value, TimeSpec t)
        {
            w1_called = true;
        }

        void w2_changed(Wire<teststruct2>.WireConnection c, teststruct2 value, TimeSpec t)
        {
            w2_called = true;
        }

        void w3_changed(Wire<MultiDimArray>.WireConnection c, MultiDimArray value, TimeSpec t)
        {
            w3_called = true;
        }


        public async Task TestMemories()
        {


            await test_m1();



            //Run the test twice, once with a large maximum transfer size, and one with a laughably small one
            /*await test_m2();
            await test_m3();
            uint l = node.MemoryMaxTransferSize;
            node.MemoryMaxTransferSize = 10240;
            await test_m1();
            await test_m2();
            node.MemoryMaxTransferSize = l;*/
        }

        private async Task test_m1()
        {
            RRAssert.AreEqual(await r.m1.GetLength(), 100UL);
            double[] m1_1 = new double[11];
            await r.m1.Read(10, m1_1, 1, 10);
            ca<double>(m1_1, TestVectors.Get<double>("ts1.client.m1_1a"));

            double[] m1_2 = TestVectors.Get<double>("ts1.client.m1_2a");
            await r.m1.Write(20, m1_2, 2, 8);

            double[] m1_3 = new double[100];
            await r.m1.Read(0, m1_3, 0, 100);

            double[] m1_4 = TestVectors.Get<double>("ts1.m1.initial");
            Array.Copy(m1_2, 2, m1_4, 20, 8);
            ca<double>(m1_3, m1_4);

        }
#if !ROBOTRACONTEUR_H5
        private async Task test_m2()
        {
            // cSpell: ignore testmdarray
            RRAssert.AreEqual(await r.m2.GetDimCount(), 5);
            ulong[] m2_dims = await r.m2.GetDimensions();
            ca<ulong>(m2_dims, new ulong[] { 10, 10, 10, 10, 10 });

            MultiDimArray m1 = MultiDimArrayTest.LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray1.bin"));
            MultiDimArray m2 = MultiDimArrayTest.LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray2.bin"));
            MultiDimArray m3 = MultiDimArrayTest.LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray3.bin"));
            MultiDimArray m4 = MultiDimArrayTest.LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray4.bin"));
            MultiDimArray m5 = MultiDimArrayTest.LoadDoubleArrayFromFile(Path.Combine("testdata", "testmdarray5.bin"));

            await r.m2.Write(new ulong[] { 0, 0, 0, 0, 0 }, m1, new ulong[] { 0, 0, 0, 0, 0 }, m1.Dims.Select(x => (ulong)x).ToArray());

            MultiDimArray m1_2 = new MultiDimArray(new uint[] { 10, 10, 10, 10, 10 }, new double[100000]);
            await r.m2.Read(new ulong[] { 0, 0, 0, 0, 0 }, m1_2, new ulong[] { 0, 0, 0, 0, 0 }, m1.Dims.Select(x => (ulong)x).ToArray());
            ca<uint>(m1.Dims, m1_2.Dims);
            ca<double>((double[])m1.Array_, (double[])m1_2.Array_);



            await r.m2.Write(new ulong[] { 2, 2, 3, 3, 4 }, m2, new ulong[] { 0, 2, 0, 0, 0 }, new ulong[] { 1, 5, 5, 2, 1 });
            MultiDimArray m2_2 = new MultiDimArray(new uint[] { 10, 10, 10, 10, 10 }, new double[100000]);
            await r.m2.Read(new ulong[] { 0, 0, 0, 0, 0 }, m2_2, new ulong[] { 0, 0, 0, 0, 0 }, m1.Dims.Select(x => (ulong)x).ToArray());

            ca<uint>(m2_2.Dims, m3.Dims);
            ca<double>((double[])m2_2.Array_, (double[])m3.Array_);

            MultiDimArray m6 = new MultiDimArray(new uint[] { 2, 2, 1, 1, 10 }, new double[40]);
            await r.m2.Read(new ulong[] { 4, 2, 2, 8, 0 }, m6, new ulong[] { 0, 0, 0, 0, 0 }, new ulong[] { 2, 2, 1, 1, 10 });
            ca<uint>(m4.Dims, m6.Dims);
            ca<double>((double[])m4.Array_, (double[])m6.Array_);

            MultiDimArray m7 = new MultiDimArray(new uint[] { 4, 4, 4, 4, 10 }, new double[2560]);
            await r.m2.Read(new ulong[] { 4, 2, 2, 8, 0 }, m7, new ulong[] { 2, 1, 2, 1, 0 }, new ulong[] { 2, 2, 1, 1, 10 });
            ca<uint>(m5.Dims, m7.Dims);
            ca<double>((double[])m5.Array_, (double[])m7.Array_);

        }

        private async Task test_m3()
        {
            MultiDimArray m1 = MultiDimArrayTest.LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b1.bin"));
            MultiDimArray m2 = MultiDimArrayTest.LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b2.bin"));
            MultiDimArray m3 = MultiDimArrayTest.LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b3.bin"));
            MultiDimArray m4 = MultiDimArrayTest.LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b4.bin"));
            MultiDimArray m5 = MultiDimArrayTest.LoadByteArrayFromFile(Path.Combine("testdata", "testmdarray_b5.bin"));

            await r.m3.Write(new ulong[] { 0, 0 }, m1, new ulong[] { 0, 0 }, new ulong[] { 1024, 1024 });

            MultiDimArray m1_2 = new MultiDimArray(new uint[] { 1024, 1024 }, new byte[1024 * 1024]);
            await r.m3.Read(new ulong[] { 0, 0 }, m1_2, new ulong[] { 0, 0 }, new ulong[] { 1024, 1024 });
            ca<uint>(m1.Dims, m1_2.Dims);
            ca<byte>((byte[])m1.Array_, (byte[])m1_2.Array_);


            await r.m3.Write(new ulong[] { 50, 100 }, m2, new ulong[] { 20, 25 }, new ulong[] { 200, 200 });
            MultiDimArray m2_2 = new MultiDimArray(new uint[] { 1024, 1024 }, new byte[1024 * 1024]);
            await r.m3.Read(new ulong[] { 0, 0 }, m2_2, new ulong[] { 0, 0 }, new ulong[] { 1024, 1024 });
            ca<uint>(m3.Dims, m2_2.Dims);
            ca<byte>((byte[])m3.Array_, (byte[])m2_2.Array_);

            MultiDimArray m6 = new MultiDimArray(new uint[] { 200,200 }, new byte[200*200]);
            await r.m3.Read(new ulong[] { 65,800 }, m6, new ulong[] { 0, 0 }, new ulong[] { 200, 200 });
            ca<uint>(m4.Dims, m6.Dims);
            ca<byte>((byte[])m4.Array_, (byte[])m6.Array_);


            MultiDimArray m7 = new MultiDimArray(new uint[] { 512, 512 }, new byte[512 * 512]);
            await r.m3.Read(new ulong[] { 65, 800 }, m7, new ulong[] { 100, 230 }, new ulong[] { 200, 200 });
            ca<uint>(m5.Dims, m7.Dims);
            ca<byte>((byte[])m5.Array_, (byte[])m7.Array_);
        }
#endif


        public async Task DisconnectService()
        {
            await node.DisconnectService(r);

        }

        public async Task TestAuthentication(string[] url)
        {
            //Test two different logins
            Dictionary<string, object> cred1 = new Dictionary<string, object>();
            cred1.Add("password", "testpass1");
            testroot r1 = (testroot)await node.ConnectService(url, "testuser1", cred1);
            await r1.func3(2.2, 3.3);
            await node.DisconnectService(r1);

            Dictionary<string, object> cred2 = new Dictionary<string, object>();
            cred2.Add("password", "testpass2");
            testroot r2 = (testroot)await node.ConnectService(url, "testuser2", cred2);
            await r2.func3(2.2, 3.3);
            await node.DisconnectService(r2);

            //Check an invalid password
            bool err = false;

            testroot r3 = null;
            try
            {
                r3 = (testroot)await node.ConnectService(url, "testuser2", cred1);
                await r3.func3(2.2, 3.3);
            }
            catch (AuthenticationException e)
            {
                err = true;
                try
                {
                    if (r3 != null)
                        node.DisconnectService(r3).IgnoreResult();
                }
                catch { }

            }

            RRAssert.IsTrue(err);

            bool err2 = false;
            //Check no password
            testroot r4 = null;
            try
            {
                r4 = (testroot)await node.ConnectService(url);
                await r4.func3(2.2, 3.3);
            }
            catch (AuthenticationException e)
            {
                err2 = true;
                try
                {
                    node.DisconnectService(r4).IgnoreResult();
                }
                catch { }

            }

            RRAssert.IsTrue(err2);


        }

        public async Task TestObjectLock(string[] url)
        {
            //Run a test, check 6 things:
            //1. Exclusive username lock works as expected (user with lock can access, other user can't)
            //2. Check that session-level lock works as expected (only one session can access the locked object)
            //3. Check that "sub-tree" objects lock as expected
            //5. Object lock release works
            //6. Object lock override works

            //Log in twice
            Dictionary<string, object> cred1 = new Dictionary<string, object>();
            cred1.Add("password", "testpass1");
            testroot r1 = (testroot)await node.ConnectService(url, "testuser1", cred1);


            //Log in again with "testuser1" username
            Dictionary<string, object> cred2 = new Dictionary<string, object>();
            cred2.Add("password", "testpass2");
            testroot r2 = (testroot)await node.ConnectService(url, "testuser1", cred1);



            sub1 r1_o = await r1.get_o1();
            sub2 r1_o_o2 = await r1_o.get_o2_1();
            sub1 r2_o = await r2.get_o1();
            sub2 r2_o_o2 = await r2_o.get_o2_1();



            testroot r3 = (testroot)await node.ConnectService(url, "testuser2", cred2);
            sub1 r3_o = await r3.get_o1();
            sub2 r3_o_o2 = await r3_o.get_o2_1();

            //Make sure all the objects work
            await r1.func3(2.2, 3.3);
            await r1_o.set_d1(new double[] { 1.0 });
            await r1_o_o2.set_data("Hello world");
            await r2.func3(2.2, 3.3);
            await r2_o.set_d1(new double[] { 1.0 });
            await r2_o_o2.set_data("Hello world");
            await r3.func3(2.2, 3.3);
            await r3_o.set_d1(new double[] { 1.0 });
            await r3_o_o2.set_data("Hello world");

            //Lock the object by username
            await node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.USER_LOCK);

            //Check that all users that should access the objects can
            await r1.func3(2.2, 3.3);
            await r1_o.set_d1(new double[] { 1.0 });
            await r1_o_o2.set_data("Hello world");
            await r2.func3(2.2, 3.3);
            await r2_o.set_d1(new double[] { 1.0 });
            await r2_o_o2.set_data("Hello world");
            await r3.func3(2.2, 3.3);

            //Check that objects that shouldn't be able to access the objects can't
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r3_o.set_d1(new double[] { 1.0 }); });
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r3_o_o2.set_data("Hello world"); });

            //Unlock and recheck all
            await node.ReleaseObjectLock(r1_o);

            await r1.func3(2.2, 3.3);
            await r1_o.set_d1(new double[] { 1.0 });
            await r1_o_o2.set_data("Hello world");
            await r2.func3(2.2, 3.3);
            await r2_o.set_d1(new double[] { 1.0 });
            await r2_o_o2.set_data("Hello world");
            await r3.func3(2.2, 3.3);
            await r3_o.set_d1(new double[] { 1.0 });
            await r3_o_o2.set_data("Hello world");

            //Relock, test that the lock is active, and then close the connection.  The lock should release.  The
            //second session is closed first, and should not release the lock.
            await node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.USER_LOCK);
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r3_o.set_d1(new double[] { 1.0 }); });

            await r2_o.set_d1(new double[] { 1.0 });
            await node.DisconnectService(r2);

            //Object still should be locked
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r3_o.set_d1(new double[] { 1.0 }); });

            //Now close the session and lock should be released
            await node.DisconnectService(r1);
            await r3_o.set_d1(new double[] { 1.0 });

            //Reconnect the first two test sessions
            r1 = (testroot)await node.ConnectService(url, "testuser1", cred1);
            r2 = (testroot)await node.ConnectService(url, "testuser2", cred2);
            r1_o = await r1.get_o1();
            r1_o_o2 = await r1_o.get_o2_1();
            r2_o = await r2.get_o1();
            r2_o_o2 = await r2_o.get_o2_1();

            //Test the exclusive client lock
            await node.RequestObjectLock(r1_o, RobotRaconteurObjectLockFlags.CLIENT_LOCK);
            await r1_o.set_d1(new double[] { 1.0 });
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r2_o.set_d1(new double[] { 1.0 }); });
            await ShouldBeErr<ObjectLockedException>(async delegate () { await r3_o.set_d1(new double[] { 1.0 }); });

            //Test the lock override by testsuperpass
            Dictionary<string, object> cred5 = new Dictionary<string, object>();
            cred5.Add("password", "superpass1");
            testroot r5 = (testroot)await node.ConnectService(url, "testsuperuser", cred5);
            sub1 r5_o = await r5.get_o1();
            await node.ReleaseObjectLock(r5_o);

            //Make sure the lock is released
            await r2_o.set_d1(new double[] { 1.0 });
            await r3_o.set_d1(new double[] { 1.0 });

            //Close all connections
            await node.DisconnectService(r1);
            await node.DisconnectService(r2);
            await node.DisconnectService(r3);
            await node.DisconnectService(r5);



        }

        private async Task ShouldBeErr<T>(Func<Task> a) where T : Exception
        {
            bool err = false;

            try
            {
                await a();
            }
            catch (T exp)
            {
                err = true;
            }

            RRAssert.IsTrue(err);
        }

        public async Task TestMonitorLock(string[] url)
        {

            //The monitor lock acquires an exclusive sock to a single thread on the client.
            //This lock is for a single thread, and works for all clients and the service if
            //it is checking for monitor locks.


            testroot r1 = (testroot)await node.ConnectService(url);
            testroot r2 = (testroot)await node.ConnectService(url);

            sub1 r1_o = await r1.get_o1();
            sub2 r1_o_o2 = await r1_o.get_o2_1();
            sub1 r2_o = await r2.get_o1();
            sub2 r2_o_o2 = await r2_o.get_o2_1();

            //Make sure everything works
            await r1.func3(2.2, 3.3);
            await r1_o.set_d1(new double[] { 1.0 });
            await r1_o_o2.set_data("Hello world");
            await r2.func3(2.2, 3.3);
            await r2_o.set_d1(new double[] { 1.0 });
            await r2_o_o2.set_data("Hello world");

            //Make sure that locks work as expected

            bool t1 = false;
            bool t2 = false;

            TaskCompletionSource<int> e1 = new TaskCompletionSource<int>();
            //AutoResetEvent e2 = new AutoResetEvent(false);

            bool threaderr = false;
            Func<Task> t_func = new Func<Task>(async delegate ()
            {
                try
                {
                    await r2.func3(2.2, 3.3);
                    await r2_o_o2.set_data("Hello world");
                    //ShouldBeErr<ObjectLockedException>(delegate() { r2_o.d1 = new double[] { 0.0 }; });
                    e1.TrySetResult(0);
                    var l = await node.MonitorEnter(r2_o);
                    RRAssert.IsFalse(t1);
                    t2 = true;

                    await r2_o.set_d1(new double[] { 0.0 });
                    await node.MonitorExit(l);
                }
                catch
                {
                    threaderr = true;
                }
            });

            var l2 = await node.MonitorEnter(r1_o);
            Task t = t_func();
            t1 = true;
            await e1.Task.AwaitWithTimeout(100);
            await Task.Delay(10);
            await r1.func3(2.2, 3.3);
            await r1_o.set_d1(new double[] { 0.0 });
            RRAssert.IsFalse(t2);
            t1 = false;
            await node.MonitorExit(l2);

            await t;

            RRAssert.IsFalse(threaderr);


        }


    }
}
