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
            double[] c2_1_1 = new double[] { 1.968551e+07, 2.380643e+18, 3.107374e-16, 7.249542e-16, -4.701135e-19, -6.092764e-17, 2.285854e+14, 2.776180e+05, -1.436152e-12, 3.626609e+11, 3.600952e-02, -3.118123e-16, -1.312210e-10, -1.738940e-07, -1.476586e-12, -2.899781e-20, 4.806642e+03, 4.476869e-05, -2.935084e-16, 3.114019e-20, -3.675955e+01, 3.779796e-21, 2.190594e-11, 4.251420e-06, -9.715221e+11, -3.483924e-01, 7.606428e+05, 5.418088e+15, 4.786378e+16, -1.202581e+08, -1.662061e+02, -2.392954e+03 };
            ca(c2_1, ComplexFromScalars(c2_1_1));

            double[] c2_2_1 = new double[] { 4.925965e-03, 5.695254e+13, -4.576890e-14, -6.056342e-07, -4.918571e-08, -1.940684e-10, 1.549104e-02, -1.954145e+04, -2.499019e-16, 4.010614e+09, -1.906811e-08, 3.297924e-10, 2.742399e-02, -4.372839e-01, -3.093171e-10, 4.311755e-01, -2.218220e-14, 5.399758e+10, 3.360304e+17, 1.340681e-18, -4.441140e+11, -1.845055e-09, -3.074586e-10, -1.754926e+01, -2.766799e+04, -2.307577e+10, 2.754875e+14, 1.179639e+15, 6.976204e-10, 1.901856e+08, -3.824351e-02, -1.414167e+08 };

            await r.set_c2(ComplexFromScalars(c2_2_1));

            MultiDimArray c3_1 = await r.get_c3();
            uint[] c3_1_1 = new uint[] { 2, 5 };
            double[] c3_1_2 = new double[] { 5.524802e+18, -2.443857e-05, 3.737932e-02, -4.883553e-03, -1.184347e+12, 4.537366e-08, -4.567913e-01, -1.683542e+15, -1.676517e+00, -8.911085e+12, -2.537376e-17, 1.835687e-10, -9.366069e-22, -5.426323e-12, -7.820969e-10, -1.061541e+12, -3.660854e-12, -4.969930e-03, 1.988428e+07, 1.860782e-16 };
            ca(c3_1.Dims, c3_1_1);
            ca((CDouble[])c3_1.Array_, ComplexFromScalars(c3_1_2));

            uint[] c3_2_1 = new uint[] { 3, 4 };
            double[] c3_2_2 = new double[] { 4.435180e+04, 5.198060e-18, -1.316737e-13, -4.821771e-03, -4.077550e-19, -1.659105e-09, -6.332363e-11, -1.128999e+16, 4.869912e+16, 2.680490e-04, -8.880119e-04, 3.960452e+11, 4.427784e-09, -2.813742e-18, 7.397516e+18, 1.196394e+13, 3.236906e-14, -4.219297e-17, 1.316282e-06, -2.771084e-18, -1.239118e-09, 2.887453e-08, -1.746515e+08, -2.312264e-11 };
            await r.set_c3(new MultiDimArray(c3_2_1, ComplexFromScalars(c3_2_2)));

            List<CDouble[]> c5_1 = await r.get_c5();
            double[] c5_1_1 = new double[] { 1.104801e+00, 4.871266e-10, -2.392938e-03, 4.210339e-07, 1.474114e-19, -1.147137e-01, -2.026434e+06, 4.450447e-19, 3.702953e-21, 9.722025e+12, 3.464073e-14, 4.628110e+15, 2.345453e-19, 3.730012e-04, 4.116650e+16, 4.380220e+08 };
            ca(c5_1[0], ComplexFromScalars(c5_1_1));

            var c5_2 = new List<CDouble[]>();
            double[] c5_2_1 = { 2.720831e-20, 2.853037e-16, -7.982497e+16, -2.684318e-09, -2.505796e+17, -4.743970e-12, -3.657056e+11, 2.718388e+15, 1.597672e+03, 2.611859e+14, 2.224926e+06, -1.431096e-09, 3.699894e+19, -5.936706e-01, -1.385395e-09, -4.248415e-13 };
            c5_2.Add(ComplexFromScalars(c5_2_1));
            await r.set_c5(c5_2);

            var c7_1 = new CSingle(-5.527021e-18f, -9.848457e+03f);
            RRAssert.AreEqual(await r.get_c7(), c7_1);

            var c7_2 = new CSingle(9.303345e-12f, -3.865684e-05f);
            await r.set_c7(c7_2);

            var c8_1 = await r.get_c8();
            float[] c8_1_1 = new float[] { -3.153395e-09f, 3.829492e-02f, -2.665239e+12f, 1.592927e-03f, 3.188444e+06f, -3.595015e-11f, 2.973887e-18f, -2.189921e+17f, 1.651567e+10f, 1.095838e+05f, 3.865249e-02f, 4.725510e+10f, -2.334376e+03f, 3.744977e-05f, -1.050821e+02f, 1.122660e-22f, 3.501520e-18f, -2.991601e-17f, 6.039622e-17f, 4.778095e-07f, -4.793136e-05f, 3.096513e+19f, 2.476004e+18f, 1.296297e-03f, 2.165336e-13f, 4.834427e+06f, 4.675370e-01f, -2.942290e-12f, -2.090883e-19f, 6.674942e+07f, -4.809047e-10f, -4.911772e-13f };
            ca(c8_1, ComplexFromScalars(c8_1_1));

            float[] c8_2_1 = new float[] { 1.324498e+06f, 1.341746e-04f, 4.292993e-04f, -3.844509e+15f, -3.804802e+10f, 3.785305e-12f, 2.628285e-19f, -1.664089e+15f, -4.246472e-10f, -3.334943e+03f, -3.305796e-01f, 1.878648e-03f, 1.420880e-05f, -3.024657e+14f, 2.227031e-21f, 2.044653e+17f, 9.753609e-20f, -6.581817e-03f, 3.271063e-03f, -1.726081e+06f, -1.614502e-06f, -2.641638e-19f, -2.977317e+07f, -1.278224e+03f, -1.760207e-05f, -4.877944e-07f, -2.171524e+02f, 1.620645e+01f, -4.334168e-02f, 1.871011e-09f, -3.066163e+06f, -3.533662e+07f };
            await r.set_c8(ComplexFromScalars(c8_2_1));

            var c9_1 = await r.get_c9();
            uint[] c9_1_1 = new uint[] { 2, 4 };
            float[] c9_1_2 = new float[] { 1.397743e+15f, 3.933042e+10f, -3.812329e+07f, 1.508109e+16f, -2.091397e-20f, 3.207851e+12f, -3.640702e+02f, 3.903769e+02f, -2.879727e+17f, -4.589604e-06f, 2.202769e-06f, 2.892523e+04f, -3.306489e-14f, 4.522308e-06f, 1.665807e+15f, 2.340476e+10f };
            ca(c9_1.Dims, c9_1_1);
            ca((CSingle[])c9_1.Array_, ComplexFromScalars(c9_1_2));

            uint[] c9_2_1 = new uint[] { 2, 2, 2 };
            float[] c9_2_2 = new float[] { 2.138322e-03f, 4.036979e-21f, 1.345236e+10f, -1.348460e-12f, -3.615340e+12f, -2.911340e-21f, 3.220362e+09f, 3.459909e-04f, 4.276259e-08f, -3.199451e+18f, 3.468308e+07f, -2.928506e-09f, -3.154288e+17f, -2.352920e-02f, 6.976385e-21f, 2.435472e+12f };
            await r.set_c9(new MultiDimArray(c9_2_1, ComplexFromScalars(c9_2_2)));
        }

        async Task TestComplexMemories()
        {
            var c_m1_1 = new double[] { 8.952764e-05, 4.348213e-04, -1.051215e+08, 1.458626e-09, -2.575954e+10, 2.118740e+03, -2.555026e-02, 2.192576e-18, -2.035082e+18, 2.951834e-09, -1.760731e+15, 4.620903e-11, -3.098798e+05, -8.883556e-07, 2.472289e+17, 7.059075e-12 };
            await r.c_m1.Write(10, ComplexFromScalars(c_m1_1), 0, 8);

            var c_m1_3 = new CDouble[8];
            await r.c_m1.Read(10, c_m1_3, 0, 8);

            ca(ComplexFromScalars(c_m1_1), c_m1_3, 8);

            var z = new ulong[] { 0, 0 };
            var c = new ulong[] { 3, 3 };

            var c_m2_1 = new uint[] { 3, 3 };
            var c_m2_2 = new double[] { -4.850043e-03, 3.545429e-07, 2.169430e+12, 1.175943e-09, 2.622300e+08, -4.439823e-11, -1.520489e+17, 8.250078e-14, 3.835439e-07, -1.424709e-02, 3.703099e+08, -1.971111e-08, -2.805354e+01, -2.093850e-17, -4.476148e+19, 9.914350e+11, 2.753067e+08, -1.745041e+14 };
            var c_m2_3 = new MultiDimArray(c_m2_1, ComplexFromScalars(c_m2_2));
            await r.c_m2.Write(z, c_m2_3, z, c);

            var c_m2_4 = new MultiDimArray(c_m2_1, new CDouble[9]);
            await r.c_m2.Read(z, c_m2_4, z, c);

            ca(c_m2_3.Dims, c_m2_4.Dims);
            ca((CDouble[])c_m2_3.Array_, (CDouble[])c_m2_4.Array_);
        }
    }

}
