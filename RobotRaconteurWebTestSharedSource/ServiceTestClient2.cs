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
using com.robotraconteur.testing.TestService3;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{

    public class ServiceTestClient2
    {
        RobotRaconteurNode node;

        public ServiceTestClient2(RobotRaconteurNode node)
        {
            this.node = node;
        }

        public ServiceTestClient2()
        {
            this.node = RobotRaconteurNode.s;
        }
        public async Task ConnectService(string url)
        {
            r = (testroot3)await node.ConnectService(url);
        }

        public async Task DisconnectService()
        {
            await node.DisconnectService(r);
        }


        testroot3 r;

        public async Task RunFullTest(string url)
        {
            await ConnectService(url);

            RRAssert.AreEqual((int)await r.get_testenum1_prop(), (int)testenum1.anothervalue);
            await r.set_testenum1_prop(testenum1.hexval1);

            await r.get_o4();

            await TestWirePeekPoke();

            await TestEnums();

            await TestPods();

            await TestGenerators();

            await TestMemories();

            await TestNamedArrays();

            await TestNamedArrayMemories();

            await TestComplex();

            await TestComplexMemories();

            await DisconnectService();
        }

        public async Task TestWirePeekPoke()
        {

            var v = await r.peekwire.PeekInValue();
            RRAssert.AreEqual(v.Item1, 56295674);


            await r.pokewire.PokeOutValue(75738265);
            var v2 = await r.pokewire.PeekOutValue();
            RRAssert.AreEqual(v2.Item1, 75738265);

            var w = await r.pokewire.Connect();
            for (int i = 0; i < 3; i++)
            {
                w.OutValue = 8638356;
            }

            Thread.Sleep(100);

            var v3 = await r.pokewire.PeekOutValue();
            RRAssert.AreEqual(v3.Item1, 8638356);
        }

        public async Task TestEnums()
        {
            RRAssert.AreEqual((int)await r.get_testenum1_prop(), (int)testenum1.anothervalue);

            await r.set_testenum1_prop(testenum1.hexval1);

        }

        public async Task TestPods()
        {
            /*var s1 = new testpod1();
            ServiceTest2_pod.fill_testpod1(ref s1, 563921043);
            ServiceTest2_pod.verify_testpod1(ref s1, 563921043);

            var s1_m = node.PackPodToArray(ref s1);
            var s1_1 = node.UnpackPodFromArray<testpod1>(s1_m);
            ServiceTest2_pod.verify_testpod1(ref s1_1, 563921043);

            var s2 = ServiceTest2_pod.fill_teststruct3(858362);
            ServiceTest2_pod.verify_teststruct3(s2, 858362);
            var s2_m = node.PackStructure(s2);
            var s2_1 = node.UnpackStructure<teststruct3>(s2_m);
            ServiceTest2_pod.verify_teststruct3(s2_1, 858362);*/

            var p1 = await r.get_testpod1_prop();
            ServiceTest2_pod.verify_testpod1(ref p1, 563921043);
            var p2 = new testpod1();
            ServiceTest2_pod.fill_testpod1(ref p2, 85932659);
            await r.set_testpod1_prop(p2);

            var f1 = await r.testpod1_func2();
            ServiceTest2_pod.verify_testpod1(ref f1, 95836295);
            var f2 = new testpod1();
            ServiceTest2_pod.fill_testpod1(ref f2, 29546592);
            await r.testpod1_func1(f2);

            ServiceTest2_pod.verify_teststruct3(await r.get_teststruct3_prop(), 16483675);
            await r.set_teststruct3_prop(ServiceTest2_pod.fill_teststruct3(858362));
        }

        public async Task TestGenerators()
        {
            var g = await r.gen_func1();
            var res = await g.NextAll();
            RRWebTest.WriteLine(string.Join(", ", res.Select(x => x.ToString())));

            var g2 = await r.gen_func1();
            await g2.Next();
            await g2.Abort();
            try
            {
                await g2.Next();
            }
            catch (OperationAbortedException)
            {
                RRWebTest.WriteLine("Operation aborted caught");
            }

            var g3 = await r.gen_func1();
            await g3.Next();
            await g3.Close();
            try
            {
                await g3.Next();
            }
            catch (StopIterationException)
            {
                RRWebTest.WriteLine("Stop iteration caught");
            }

            var gen2 = await r.gen_func4();
            await gen2.Next(new byte[] { 2, 3, 4 });
            await gen2.Close();
            try
            {
                await gen2.Next(new byte[] { 2, 3, 4 });
            }
            catch (StopIterationException)
            {
                RRWebTest.WriteLine("Stop iteration caught");
            }
        }

        public async Task TestMemories()
        {
            await test_m1();
            await test_m2();
        }

        public async Task test_m1()
        {
            testpod2[] o1 = new testpod2[32];

            for (uint i = 0; i < o1.Length; i++)
            {
                ServiceTest2_pod.fill_testpod2(ref o1[i], 59174 + i);
            }

            RRAssert.AreEqual(await r.pod_m1.GetLength(), 1024UL);

            await r.pod_m1.Write(52, o1, 3, 17);

            testpod2[] o2 = new testpod2[32];

            await r.pod_m1.Read(53, o2, 2, 16);

            for (uint i = 2; i < 16; i++)
            {
                ServiceTest2_pod.verify_testpod2(ref o2[i], 59174 + i + 2);
            }
        }

        public async Task test_m2()
        {
            PodMultiDimArray s = new PodMultiDimArray(new uint[] { 3, 3 }, new testpod2[9]);

            for (uint i = 0; i < s.pod_array.Length; i++)
            {
                ServiceTest2_pod.fill_testpod2(ref ((testpod2[])s.pod_array)[i], 75721 + i);
            }

            await r.pod_m2.Write(new ulong[] { 0, 0 }, s, new ulong[] { 0, 0 }, new ulong[] { 3, 3 });

            PodMultiDimArray s2 = new PodMultiDimArray(new uint[] { 3, 3 }, new testpod2[9]);

            await r.pod_m2.Read(new ulong[] { 0, 0 }, s2, new ulong[] { 0, 0 }, new ulong[] { 3, 3 });

            for (uint i = 0; i < s2.pod_array.Length; i++)
            {
                ServiceTest2_pod.verify_testpod2(ref ((testpod2[])s2.pod_array)[i], 75721 + i);
            }
        }

        public async Task TestNamedArrays()
        {
            var a1 = new transform();
            ServiceTest2_pod.fill_transform(ref a1, 3956378);
            await r.set_testnamedarray1(a1.translation);

            var a1_1 = new transform();
            a1_1.rotation = a1.rotation;
            a1_1.translation = await r.get_testnamedarray1();
            var a1_2 = new transform();
            ServiceTest2_pod.fill_transform(ref a1_2, 74637);
            a1_1.rotation = a1_2.rotation;
            ServiceTest2_pod.verify_transform(ref a1_1, 74637);

            var a2 = new transform();
            ServiceTest2_pod.fill_transform(ref a2, 827635);
            await r.set_testnamedarray2(a2);

            transform a2_1 = await r.get_testnamedarray2();
            ServiceTest2_pod.verify_transform(ref a2_1, 1294);

            await r.set_testnamedarray3(ServiceTest2_pod.fill_transform_array(6, 19274));
            ServiceTest2_pod.verify_transform_array(await r.get_testnamedarray3(), 8, 837512);

            await r.set_testnamedarray4(ServiceTest2_pod.fill_transform_multidimarray(5, 2, 6385));
            ServiceTest2_pod.verify_transform_multidimarray(await r.get_testnamedarray4(), 7, 2, 66134);

            await r.set_testnamedarray5(ServiceTest2_pod.fill_transform_multidimarray(3, 2, 7732));
            ServiceTest2_pod.verify_transform_multidimarray(await r.get_testnamedarray5(), 3, 2, 773142);

        }

        public async Task TestNamedArrayMemories()
        {
            await test_namedarray_m1();
            await test_namedarray_m2();
        }

        public async Task test_namedarray_m1()
        {
            var s = new transform[32];
            for (uint i = 0; i < s.Length; i++)
                ServiceTest2_pod.fill_transform(ref s[i], 79174 + i);

            RRAssert.AreEqual(await r.namedarray_m1.GetLength(), 512UL);
            await r.namedarray_m1.Write(23, s, 3, 21);

            var s2 = new transform[32];
            await r.namedarray_m1.Read(24, s2, 2, 18);

            for (uint i = 2; i < 18; i++)
            {
                ServiceTest2_pod.verify_transform(ref s2[i], 79174 + i + 2);
            }
        }

        public async Task test_namedarray_m2()
        {
            var s = new NamedMultiDimArray(new uint[] { 3, 3 }, new transform[9]);
            var s_array = (transform[])s.namedarray_array;
            for (uint i = 0; i < s.namedarray_array.Length; i++)
                ServiceTest2_pod.fill_transform(ref s_array[i], 15721 + i);

            await r.namedarray_m2.Write(new ulong[] { 0, 0 }, s, new ulong[] { 0, 0 }, new ulong[] { 3, 3 });

            var s2 = new NamedMultiDimArray(new uint[] { 3, 3 }, new transform[9]);
            await r.namedarray_m2.Read(new ulong[] { 0, 0 }, s2, new ulong[] { 0, 0 }, new ulong[] { 3, 3 });

            var s2_array = (transform[])s2.namedarray_array;
            for (uint i = 0; i < s2.namedarray_array.Length; i++)
                ServiceTest2_pod.fill_transform(ref s2_array[i], 15721 + i);

        }

        public void ca<T>(T[] v1, T[] v2, int count = -1)
        {
            RRAssert.AreEqual(v1.Length, v2.Length);
            int len = v1.Length;
            if (count > 0) len = count;
            for (int i = 0; i < count; i++)
            {
                RRAssert.IsTrue(Object.Equals(v1[i], v2[i]));
            }
        }

        public void ca(CDouble[] v1, CDouble[] v2, int count = -1)
        {
            RRAssert.AreEqual(v1.Length, v2.Length);
            int len = v1.Length;
            if (count > 0) len = count;
            for (int i = 0; i < count; i++)
            {
                RRAssert.AreEqual(v1[i], v2[i]);
            }
        }

        public void ca(CSingle[] v1, CSingle[] v2, int count = -1)
        {
            RRAssert.AreEqual(v1.Length, v2.Length);
            int len = v1.Length;
            if (count > 0) len = count;
            for (int i = 0; i < count; i++)
            {
                RRAssert.AreEqual(v1[i], v2[i]);
            }
        }


        static CDouble[] ComplexFromScalars(double[] a)
        {
            var o = new CDouble[a.Length / 2];
            for (int j = 0; j < o.Length; j++)
                o[j] = new CDouble(a[j * 2], a[j * 2 + 1]);
            return o;
        }

        static CSingle[] ComplexFromScalars(float[] a)
        {
            var o = new CSingle[a.Length / 2];
            for (int j = 0; j < o.Length; j++)
                o[j] = new CSingle(a[j * 2], a[j * 2 + 1]);
            return o;
        }

        async Task TestComplex()
        {
            var c1_1 = new CDouble(5.708705e+01, -2.328294e-03);
            RRAssert.AreEqual(await r.get_c1(), c1_1);

            var c1_2 = new CDouble(5.708705e+01, -2.328294e-03);
            await r.set_c1(c1_2);

            CDouble[] c2_1 = await r.get_c2();
            double[] c2_1_1 = TestVectors.Get<double>("ts3.c2.to_client");
            ca(c2_1, ComplexFromScalars(c2_1_1));

            double[] c2_2_1 = TestVectors.Get<double>("ts3.c2.to_service");

            await r.set_c2(ComplexFromScalars(c2_2_1));

            MultiDimArray c3_1 = await r.get_c3();
            uint[] c3_1_1 = TestVectors.Get<uint>("ts3.c3.to_client.dims");
            double[] c3_1_2 = TestVectors.Get<double>("ts3.c3.to_client");
            ca(c3_1.Dims, c3_1_1);
            ca((CDouble[])c3_1.Array_, ComplexFromScalars(c3_1_2));

            uint[] c3_2_1 = TestVectors.Get<uint>("ts3.c3.to_service.dims");
            double[] c3_2_2 = TestVectors.Get<double>("ts3.c3.to_service");
            await r.set_c3(new MultiDimArray(c3_2_1, ComplexFromScalars(c3_2_2)));

            List<CDouble[]> c5_1 = await r.get_c5();
            double[] c5_1_1 = TestVectors.Get<double>("ts3.c5.to_client");
            ca(c5_1[0], ComplexFromScalars(c5_1_1));

            var c5_2 = new List<CDouble[]>();
            double[] c5_2_1 = TestVectors.Get<double>("ts3.c5.to_service");
            c5_2.Add(ComplexFromScalars(c5_2_1));
            await r.set_c5(c5_2);

            var c7_1 = new CSingle(-5.527021e-18f, -9.848457e+03f);
            RRAssert.AreEqual(await r.get_c7(), c7_1);

            var c7_2 = new CSingle(9.303345e-12f, -3.865684e-05f);
            await r.set_c7(c7_2);

            var c8_1 = await r.get_c8();
            float[] c8_1_1 = TestVectors.Get<float>("ts3.c8.to_client");
            ca(c8_1, ComplexFromScalars(c8_1_1));

            float[] c8_2_1 = TestVectors.Get<float>("ts3.c8.to_service");
            await r.set_c8(ComplexFromScalars(c8_2_1));

            var c9_1 = await r.get_c9();
            uint[] c9_1_1 = TestVectors.Get<uint>("ts3.c9.to_client.dims");
            float[] c9_1_2 = TestVectors.Get<float>("ts3.c9.to_client");
            ca(c9_1.Dims, c9_1_1);
            ca((CSingle[])c9_1.Array_, ComplexFromScalars(c9_1_2));

            uint[] c9_2_1 = TestVectors.Get<uint>("ts3.c9.to_service.dims");
            float[] c9_2_2 = TestVectors.Get<float>("ts3.c9.to_service");
            await r.set_c9(new MultiDimArray(c9_2_1, ComplexFromScalars(c9_2_2)));
        }

        async Task TestComplexMemories()
        {
            var c_m1_1 = TestVectors.Get<double>("ts3.client.c_m1_1");
            await r.c_m1.Write(10, ComplexFromScalars(c_m1_1), 0, 8);

            var c_m1_3 = new CDouble[8];
            await r.c_m1.Read(10, c_m1_3, 0, 8);

            ca(ComplexFromScalars(c_m1_1), c_m1_3, 8);

            var z = new ulong[] { 0, 0 };
            var c = new ulong[] { 3, 3 };

            var c_m2_1 = TestVectors.Get<uint>("ts3.client.c_m2_2.dims");
            var c_m2_2 = TestVectors.Get<double>("ts3.client.c_m2_2");
            var c_m2_3 = new MultiDimArray(c_m2_1, ComplexFromScalars(c_m2_2));
            await r.c_m2.Write(z, c_m2_3, z, c);

            var c_m2_4 = new MultiDimArray(c_m2_1, new CDouble[9]);
            await r.c_m2.Read(z, c_m2_4, z, c);

            ca(c_m2_3.Dims, c_m2_4.Dims);
            ca((CDouble[])c_m2_3.Array_, (CDouble[])c_m2_4.Array_);
        }
    }

}
