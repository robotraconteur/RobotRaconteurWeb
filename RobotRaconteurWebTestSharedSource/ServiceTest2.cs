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
using com.robotraconteur.testing.TestService3;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{

#if !ROBOTRACONTEUR_H5
    public class RobotRaconteurTestServiceSupport2
    {

        public testroot3_impl testservice2;

        public RobotRaconteurNode node;
        public RobotRaconteurTestServiceSupport2(RobotRaconteurNode node = null)
        {
            this.node = node;
            if (node == null)
            {
                this.node = RobotRaconteurNode.s;
            }
        }

        public void RegisterServices()
        {
            testservice2 = new testroot3_impl();
            node.RegisterService("RobotRaconteurTestService2", "com.robotraconteur.testing.TestService3", testservice2);
        }

        public void UnregisterServices()
        {
            node.Shutdown();
        }
    }


    public class testroot3_impl : testroot3_default_impl
    {

        Timer _peekwire_b_timer;

        public override Wire<int> peekwire
        {
            get
            {
                return base.peekwire;
            }
            set
            {
                base.peekwire = value;
                _peekwire_b_timer = new Timer(delegate (object ev)
                {
                    rrvar_peekwire.OutValue = 56295674;
                }, null, 0, 100);
                rrvar_peekwire.OutValue = 56295674;

            }
        }


        public override Pipe<int> unreliable1 { get; set; }

        public override ArrayMemory<double> readmem { get; }

        public override Task<testenum1> get_testenum1_prop(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(testenum1.anothervalue);
        }

        public override Task set_testenum1_prop(testenum1 value, CancellationToken cancel = default(CancellationToken))
        {
            if (value != testenum1.hexval1) throw new Exception();
            return Task.FromResult(0);
        }

        public override Task<testpod1> get_testpod1_prop(CancellationToken cancel = default(CancellationToken))
        {

            var o = new testpod1();
            ServiceTest2_pod.fill_testpod1(ref o, 563921043);
            return Task.FromResult(o);
        }

        public override Task set_testpod1_prop(testpod1 value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_testpod1(ref value, 85932659);
            return Task.FromResult(0);
        }

        public override Task<teststruct3> get_teststruct3_prop(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(ServiceTest2_pod.fill_teststruct3(16483675));
        }

        public override Task set_teststruct3_prop(teststruct3 value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_teststruct3(value, 858362);
            return Task.FromResult(0);
        }

        //public PodArrayMemory<testpod2> pod_m1 { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        //public PodMultiDimArrayMemory<testpod2> pod_m2 { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override Task<Generator2<double>> gen_func1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((Generator2<double>)new EnumeratorGenerator<double>(new double[] { 5, 6, 7, 8 }));
        }

        public override Task<Generator2<byte[]>> gen_func2(string name, CancellationToken cancel = default(CancellationToken))
        {
            var o = new List<byte[]>();
            for (byte i = 0; i < 16; i++)
            {
                o.Add(new byte[] { i });
            }

            return Task.FromResult((Generator2<byte[]>)new EnumeratorGenerator<byte[]>(o));
        }

        public override Task<Generator3<byte[]>> gen_func3(string name, CancellationToken cancel = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        public override Task<Generator1<byte[], byte[]>> gen_func4(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((Generator1<byte[], byte[]>)new func4_gen());
        }

        public override Task<Generator1<teststruct2, teststruct2>> gen_func5(CancellationToken cancel = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        public override Task<obj4> get_o4(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult((obj4)new obj4_impl());
        }

        public override Task testpod1_func1(testpod1 s, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_testpod1(ref s, 29546592);
            return Task.FromResult(0);
        }

        public override Task<testpod1> testpod1_func2(CancellationToken cancel = default(CancellationToken))
        {
            var o = new testpod1();
            ServiceTest2_pod.fill_testpod1(ref o, 95836295);
            return Task.FromResult(o);
        }

        PodArrayMemory<testpod2> m_pod_m1 = new PodArrayMemory<testpod2>(new testpod2[1024]);
        public override PodArrayMemory<testpod2> pod_m1
        {
            get
            {
                return m_pod_m1;
            }
        }

        PodMultiDimArrayMemory<testpod2> m_pod_m2 = new PodMultiDimArrayMemory<testpod2>(new PodMultiDimArray(new uint[] { 3, 3 }, new testpod2[9]));
        public override PodMultiDimArrayMemory<testpod2> pod_m2
        {
            get
            {
                return m_pod_m2;
            }
        }

        public override Wire<int[]> w1 { get; set; }
        public override Wire<int[]> w2 { get; set; }
        public override Wire<MultiDimArray> w3 { get; set; }
        public override Pipe<int[]> p1 { get; set; }
        public override Pipe<int[]> p2 { get; set; }
        public override Pipe<MultiDimArray> p3 { get; set; }

        public override Task<vector3> get_testnamedarray1(CancellationToken cancel = default(CancellationToken))
        {
            var a1 = new transform();
            ServiceTest2_pod.fill_transform(ref a1, 74637);
            return Task.FromResult(a1.translation);
        }

        public override Task set_testnamedarray1(vector3 value, CancellationToken cancel = default(CancellationToken))
        {
            var a1 = new transform();
            ServiceTest2_pod.fill_transform(ref a1, 3956378);
            a1.translation = value;
            ServiceTest2_pod.verify_transform(ref a1, 3956378);
            return Task.FromResult(0);
        }


        public override Task<transform> get_testnamedarray2(CancellationToken cancel = default(CancellationToken))
        {
            var a2 = new transform();
            ServiceTest2_pod.fill_transform(ref a2, 1294);
            return Task.FromResult(a2);
        }

        public override Task set_testnamedarray2(transform value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_transform(ref value, 827635);
            return Task.FromResult(0);
        }

        public override Task<transform[]> get_testnamedarray3(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(ServiceTest2_pod.fill_transform_array(8, 837512));
        }

        public override Task set_testnamedarray3(transform[] value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_transform_array(value, 6, 19274);
            return Task.FromResult(0);
        }

        public override Task<NamedMultiDimArray> get_testnamedarray4(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(ServiceTest2_pod.fill_transform_multidimarray(7, 2, 66134));
        }

        public override Task set_testnamedarray4(NamedMultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_transform_multidimarray(value, 5, 2, 6385);
            return Task.FromResult(0);
        }

        public override Task<NamedMultiDimArray> get_testnamedarray5(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(ServiceTest2_pod.fill_transform_multidimarray(3, 2, 773142));
        }

        public override Task set_testnamedarray5(NamedMultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            ServiceTest2_pod.verify_transform_multidimarray(value, 3, 2, 7732);
            return Task.FromResult(0);
        }


        NamedArrayMemory<transform> m_namedarray_m1 = new NamedArrayMemory<transform>(new transform[512]);
        public override NamedArrayMemory<transform> namedarray_m1
        {
            get
            {
                return m_namedarray_m1;
            }
        }

        NamedMultiDimArrayMemory<transform> m_namedarray_m2 = new NamedMultiDimArrayMemory<transform>(new NamedMultiDimArray(new uint[] { 3, 3 }, new transform[9]));
        public override NamedMultiDimArrayMemory<transform> namedarray_m2
        {
            get
            {
                return m_namedarray_m2;
            }
        }

        public void ca<T>(T[] v1, T[] v2)
        {
            if (v1.Length != v2.Length) throw new Exception();
            for (int i = 0; i < v1.Length; i++)
            {
                if (!Object.Equals(v1[i], v2[i])) throw new Exception();
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

        public override Task<CDouble> get_c1(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(new CDouble(5.708705e+01, -2.328294e-03));
        }

        public override Task set_c1(CDouble value, CancellationToken cancel = default(CancellationToken))
        {
            var c1_2 = new CDouble(5.708705e+01, -2.328294e-03);
            if (value != c1_2) throw new Exception();
            return Task.FromResult(0);
        }

        public override Task<CDouble[]> get_c2(CancellationToken cancel = default(CancellationToken))
        {
            double[] c2_1_1 = TestVectors.Get<double>("ts3.c2.to_client");
            return Task.FromResult(ComplexFromScalars(c2_1_1));
        }

        public override Task set_c2(CDouble[] value, CancellationToken cancel = default(CancellationToken))
        {
            double[] c2_2_1 = TestVectors.Get<double>("ts3.c2.to_service");
            ca(ComplexFromScalars(c2_2_1), value);
            return Task.FromResult(0);
        }

        public override Task<MultiDimArray> get_c3(CancellationToken cancel = default(CancellationToken))
        {
            uint[] c3_1_1 = TestVectors.Get<uint>("ts3.c3.to_client.dims");
            double[] c3_1_2 = TestVectors.Get<double>("ts3.c3.to_client");
            return Task.FromResult(new MultiDimArray(c3_1_1, ComplexFromScalars(c3_1_2)));
        }

        public override Task set_c3(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            uint[] c3_2_1 = TestVectors.Get<uint>("ts3.c3.to_service.dims");
            double[] c3_2_2 = TestVectors.Get<double>("ts3.c3.to_service");
            ca(value.Dims, c3_2_1);
            ca((CDouble[])value.Array_, ComplexFromScalars(c3_2_2));
            return Task.FromResult(0);
        }

        public override Task<List<CDouble[]>> get_c5(CancellationToken cancel = default(CancellationToken))
        {
            var c5_1 = new List<CDouble[]>();
            double[] c5_1_1 = TestVectors.Get<double>("ts3.c5.to_client");
            c5_1.Add(ComplexFromScalars(c5_1_1));
            return Task.FromResult(c5_1);
        }

        public override Task set_c5(List<CDouble[]> value, CancellationToken cancel = default(CancellationToken))
        {
            double[] c5_2_1 = TestVectors.Get<double>("ts3.c5.to_service");
            ca(value[0], ComplexFromScalars(c5_2_1));
            return Task.FromResult(0);
        }

        public override Task<CSingle> get_c7(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(new CSingle(-5.527021e-18f, -9.848457e+03f));
        }

        public override Task set_c7(CSingle value, CancellationToken cancel = default(CancellationToken))
        {
            var c7_2 = new CSingle(9.303345e-12f, -3.865684e-05f);
            if (value != c7_2) throw new Exception();
            return Task.FromResult(0);
        }

        public override Task<CSingle[]> get_c8(CancellationToken cancel = default(CancellationToken))
        {
            float[] c8_1_1 = TestVectors.Get<float>("ts3.c8.to_client");
            return Task.FromResult(ComplexFromScalars(c8_1_1));
        }

        public override Task set_c8(CSingle[] value, CancellationToken cancel = default(CancellationToken))
        {
            float[] c8_2_1 = TestVectors.Get<float>("ts3.c8.to_service");
            ca(value, ComplexFromScalars(c8_2_1));
            return Task.FromResult(0);
        }

        public override Task<MultiDimArray> get_c9(CancellationToken cancel = default(CancellationToken))
        {
            uint[] c9_1_1 = TestVectors.Get<uint>("ts3.c9.to_client.dims");
            float[] c9_1_2 = TestVectors.Get<float>("ts3.c9.to_client");
            return Task.FromResult(new MultiDimArray(c9_1_1, ComplexFromScalars(c9_1_2)));
        }

        public override Task set_c9(MultiDimArray value, CancellationToken cancel = default(CancellationToken))
        {
            uint[] c9_2_1 = TestVectors.Get<uint>("ts3.c9.to_service.dims");
            float[] c9_2_2 = TestVectors.Get<float>("ts3.c9.to_service");
            ca(value.Dims, c9_2_1);
            ca((CSingle[])value.Array_, ComplexFromScalars(c9_2_2));
            return Task.FromResult(0);
        }

        ArrayMemory<CDouble> c_m1_v = new ArrayMemory<CDouble>(new CDouble[512]);
        public override ArrayMemory<CDouble> c_m1 => c_m1_v;

        MultiDimArrayMemory<CDouble> c_m2_v = new MultiDimArrayMemory<CDouble>(new MultiDimArray(new uint[] { 10, 10 }, new CDouble[100]));
        public override MultiDimArrayMemory<CDouble> c_m2 => c_m2_v;

    }

    class func4_gen : Generator1<byte[], byte[]>
    {
        byte j = 0;
        bool aborted = false;

        public Task Abort(CancellationToken cancel = default(CancellationToken))
        {
            aborted = true;
            return Task.FromResult(0);
        }

        public Task Close(CancellationToken cancel = default(CancellationToken))
        {
            j = 255;
            return Task.FromResult(0);
        }

        public Task<byte[]> Next(byte[] param, CancellationToken cancel = default(CancellationToken))
        {
            if (aborted)
            {
                throw new OperationAbortedException("");
            }
            if (j >= 8)
            {
                throw new StopIterationException("");
            }

            var a = new byte[param.Length];
            for (int i = 0; i < param.Length; i++)
            {
                a[i] = (byte)(param[i] + j);
            }
            j++;
            return Task.FromResult(a);
        }
    }

    class obj4_impl : obj4
    {
        public virtual string s_ind { get; set; }
        public virtual int i_ind { get; set; }
        public virtual string data { get; set; }

        public Task<string> get_data(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(data);
        }

        public Task<int> get_i_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(i_ind);
        }


        public Task<sub3> get_o3_1(string ind, CancellationToken rr_cancel = default(CancellationToken))
        {
            return Task.FromResult((sub3)null);
        }

        public Task<string> get_s_ind(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(s_ind);
        }

        public Task set_data(string value, CancellationToken cancel = default(CancellationToken))
        {
            data = value;
            return Task.FromResult(0);
        }

        public Task set_i_ind(int value, CancellationToken cancel = default(CancellationToken))
        {
            i_ind = value;
            return Task.FromResult(0);
        }

        public Task set_s_ind(string value, CancellationToken cancel = default(CancellationToken))
        {
            s_ind = value;
            return Task.FromResult(0);
        }
    }
#endif
    class ServiceTest2_test_sequence_gen
    {
        private uint counter;
        private uint step;
        static readonly double[] double_constants = TestVectors.Get<double>("seq.double_constants");
        static readonly uint[] uint32_constants = TestVectors.Get<uint>("seq.uint32_constants");

        public ServiceTest2_test_sequence_gen(uint seed)
        {
            counter = seed % 7823;
            step = seed % 17 + 7;
        }

        public double get_double()
        {
            counter += step;
            return double_constants[counter % double_constants.Length];
        }

        public uint get_uint32()
        {
            counter += step;
            return uint32_constants[counter % uint32_constants.Length];
        }

        public sbyte get_int8()
        {
            counter += step;
            return (sbyte)(((int)(counter % 0xFF)) - 128);
        }
    }

    static class ServiceTest2_pod
    {
        private static double[] create_double_array(ServiceTest2_test_sequence_gen gen, int len)
        {
            var o = new double[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = gen.get_double();
            }
            return o;
        }

        private static void verify_double_array(ServiceTest2_test_sequence_gen gen, double[] v, int len)
        {
            if (v.Length != len) throw new Exception();
            for (int i = 0; i < len; i++)
            {
                if (v[i] != gen.get_double()) throw new Exception();
            }
        }

        private static uint[] create_uint32_array(ServiceTest2_test_sequence_gen gen, int len)
        {
            var o = new uint[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = gen.get_uint32();
            }
            return o;
        }

        private static void verify_uint32_array(ServiceTest2_test_sequence_gen gen, uint[] v, int len)
        {
            if (v.Length != len) throw new Exception();
            for (int i = 0; i < len; i++)
            {
                if (v[i] != gen.get_uint32()) throw new Exception();
            }
        }


        private static sbyte[] create_int8_array(ServiceTest2_test_sequence_gen gen, int len)
        {
            var o = new sbyte[len];
            for (int i = 0; i < len; i++)
            {
                o[i] = gen.get_int8();
            }
            return o;
        }

        private static void verify_int8_array(ServiceTest2_test_sequence_gen gen, sbyte[] v, int len)
        {
            if (v.Length != len) throw new Exception();
            for (int i = 0; i < len; i++)
            {
                if (v[i] != gen.get_int8()) throw new Exception();
            }
        }


        public static void fill_testpod1(ref testpod1 s, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            s.d1 = gen.get_double();
            s.d2 = create_double_array(gen, 6);
            s.d3 = create_double_array(gen, (int)(gen.get_uint32() % 6));
            s.d4 = create_double_array(gen, 9);

            fill_testpod2(ref s.s1, gen.get_uint32());
            s.s2 = create_testpod2_array(gen, 8);
            s.s3 = create_testpod2_array(gen, (int)(gen.get_uint32() % 9));
            s.s4 = create_testpod2_array(gen, 8);

            fill_transform(ref s.t1, gen.get_uint32());

            s.t2 = new transform[4];
            for (int i = 0; i < 4; i++)
                fill_transform(ref s.t2[i], gen.get_uint32());

            int t3_len = (int)(gen.get_uint32() % 15);
            s.t3 = new transform[(t3_len)];
            for (int i = 0; i < t3_len; i++)
                fill_transform(ref s.t3[i], gen.get_uint32());

            s.t4 = new transform[8];
            for (int i = 0; i < 8; i++)
                fill_transform(ref s.t4[i], gen.get_uint32());
        }

        public static void verify_testpod1(ref testpod1 s, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            if (s.d1 != gen.get_double()) throw new Exception();
            verify_double_array(gen, s.d2, 6);
            verify_double_array(gen, s.d3, (int)(gen.get_uint32() % 6));

            verify_double_array(gen, (double[])s.d4, 9);
            verify_testpod2(ref s.s1, gen.get_uint32());
            verify_testpod2_array(gen, s.s2, 8);
            verify_testpod2_array(gen, s.s3, (int)(gen.get_uint32() % 9));
            verify_testpod2_array(gen, (testpod2[])s.s4, 8);

            verify_transform(ref s.t1, gen.get_uint32());

            for (int i = 0; i < 4; i++)
                verify_transform(ref s.t2[i], gen.get_uint32());

            int t3_len = (int)(gen.get_uint32() % 15);
            if (s.t3.Length != t3_len) throw new Exception("");
            for (int i = 0; i < t3_len; i++)
                verify_transform(ref s.t3[i], gen.get_uint32());

            for (int i = 0; i < 8; i++)
                verify_transform(ref s.t4[i], gen.get_uint32());
        }

        public static void fill_testpod2(ref testpod2 s, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            s.i1 = gen.get_int8();
            s.i2 = create_int8_array(gen, 15);
            s.i3 = create_int8_array(gen, (int)(gen.get_uint32() % 15));
        }

        public static void verify_testpod2(ref testpod2 s, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            if (s.i1 != gen.get_int8()) throw new Exception();
            verify_int8_array(gen, s.i2, 15);
            verify_int8_array(gen, s.i3, (int)(gen.get_uint32() % 15));
        }

        public static testpod1[] create_testpod1_array(int len, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var o = new testpod1[len];
            for (int i = 0; i < len; i++)
            {
                fill_testpod1(ref o[i], gen.get_uint32());
            }
            return o;
        }

        public static void verify_testpod1_array(testpod1[] v, int len, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            if (v.Length != len) throw new Exception();
            for (int i = 0; i < len; i++)
            {
                verify_testpod1(ref v[i], gen.get_uint32());
            }
        }

        public static PodMultiDimArray create_testpod1_multidimarray(uint m, uint n, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var o = new testpod1[m * n];
            for (int i = 0; i < m * n; i++)
            {
                fill_testpod1(ref o[i], gen.get_uint32());
            }
            return new PodMultiDimArray(new uint[] { m, n }, o);
        }

        public static void verify_testpod1_multidimarray(PodMultiDimArray v, uint m, uint n, uint seed)
        {
            if (!v.Dims.SequenceEqual(new uint[] { m, n })) throw new Exception();
            verify_testpod1_array((testpod1[])v.pod_array, (int)(m * n), seed);
        }

        public static void verify_testpod1_multidimarray(object v, uint m, uint n, uint seed)
        {
            if ((v is Array) && n == 1)
            {
                verify_testpod1_array((testpod1[])(v), (int)m, seed);
            }
            else
            {
                var v2 = (PodMultiDimArray)v;
                if (!v2.Dims.SequenceEqual(new uint[] { m, n })) throw new Exception();
                verify_testpod1_array((testpod1[])(v2.pod_array), (int)(m * n), seed);
            }
        }

        public static testpod2[] create_testpod2_array(ServiceTest2_test_sequence_gen gen, int len)
        {
            var o = new testpod2[len];
            for (int i = 0; i < len; i++)
            {
                fill_testpod2(ref o[i], gen.get_uint32());
            }
            return o;
        }

        public static void verify_testpod2_array(ServiceTest2_test_sequence_gen gen, testpod2[] v, int len)
        {
            if (v.Length != len) throw new Exception();
            for (int i = 0; i < len; i++)
            {
                verify_testpod2(ref v[i], gen.get_uint32());
            }
        }

        public static teststruct3 fill_teststruct3(uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var o = new teststruct3();
            fill_testpod1(ref o.s1, gen.get_uint32());
            var s2_seed = gen.get_uint32();
            o.s2 = create_testpod1_array((int)(s2_seed % 17), s2_seed);
            o.s3 = create_testpod1_array(11, gen.get_uint32());
            var s4_seed = gen.get_uint32();
            o.s4 = create_testpod1_array((int)(s4_seed % 16), s4_seed);
            o.s5 = create_testpod1_multidimarray(3, 3, gen.get_uint32());
            var s6_seed = gen.get_uint32();
            o.s6 = create_testpod1_multidimarray((uint)(s6_seed % 6), (uint)(s6_seed % 3), s6_seed);
            o.s7 = new List<testpod1>();
            var s7_1 = new testpod1();
            fill_testpod1(ref s7_1, gen.get_uint32());
            o.s7.Add(s7_1);

            o.s8 = new List<testpod1[]>();
            o.s8.Add(create_testpod1_array(2, gen.get_uint32()));
            o.s8.Add(create_testpod1_array(4, gen.get_uint32()));

            o.s9 = new List<PodMultiDimArray>();
            o.s9.Add(create_testpod1_multidimarray(2, 3, gen.get_uint32()));
            o.s9.Add(create_testpod1_multidimarray(4, 5, gen.get_uint32()));

            var s10 = new testpod1();
            fill_testpod1(ref s10, gen.get_uint32());
            o.s10 = new testpod1[] { s10 };

            o.s11 = create_testpod1_array(3, gen.get_uint32());
            o.s12 = create_testpod1_multidimarray(2, 2, gen.get_uint32());

            var s13 = new testpod1();
            fill_testpod1(ref s13, gen.get_uint32());
            o.s13 = new List<object>(new object[] { new testpod1[] { s13 } });

            var s14 = new List<object>();
            s14.Add(create_testpod1_array(3, gen.get_uint32()));
            s14.Add(create_testpod1_array(5, gen.get_uint32()));
            o.s14 = s14;

            var s15 = new List<object>();
            s15.Add(create_testpod1_multidimarray(7, 2, gen.get_uint32()));
            s15.Add(create_testpod1_multidimarray(5, 1, gen.get_uint32()));
            o.s15 = s15;

            fill_transform(ref o.t1, gen.get_uint32());

            o.t2 = fill_transform_array(4, gen.get_uint32());
            o.t3 = fill_transform_multidimarray(2, 4, gen.get_uint32());

            o.t4 = fill_transform_array(10, gen.get_uint32());
            o.t5 = fill_transform_multidimarray(6, 5, gen.get_uint32());

            o.t6 = new List<transform>();
            var t6_1 = new transform();
            fill_transform(ref t6_1, gen.get_uint32());
            o.t6.Add(t6_1);

            o.t7 = new List<transform[]>();
            o.t7.Add(fill_transform_array(4, gen.get_uint32()));
            o.t7.Add(fill_transform_array(4, gen.get_uint32()));

            o.t8 = new List<NamedMultiDimArray>();
            o.t8.Add(fill_transform_multidimarray(2, 4, gen.get_uint32()));
            o.t8.Add(fill_transform_multidimarray(2, 4, gen.get_uint32()));

            var t9 = new transform();
            fill_transform(ref t9, gen.get_uint32());
            o.t9 = new List<object>(new object[] { new transform[] { t9 } });

            var t10 = new List<object>();
            t10.Add(fill_transform_array(3, gen.get_uint32()));
            t10.Add(fill_transform_array(5, gen.get_uint32()));
            o.t10 = t10;

            var t11 = new List<object>();
            t11.Add(fill_transform_multidimarray(7, 2, gen.get_uint32()));
            t11.Add(fill_transform_multidimarray(5, 1, gen.get_uint32()));
            o.t11 = t11;

            return o;
        }

        public static void verify_teststruct3(teststruct3 v, uint seed)
        {
            if (v == null) throw new Exception("");
            var gen = new ServiceTest2_test_sequence_gen(seed);

            verify_testpod1(ref v.s1, gen.get_uint32());
            var s2_seed = gen.get_uint32();
            verify_testpod1_array(v.s2, (int)(s2_seed % 17), s2_seed);
            verify_testpod1_array(v.s3, 11, gen.get_uint32());
            var s4_seed = gen.get_uint32();
            verify_testpod1_array(v.s4, (int)(s4_seed % 16), s4_seed);
            verify_testpod1_multidimarray(v.s5, 3, 3, gen.get_uint32());
            var s6_seed = gen.get_uint32();
            verify_testpod1_multidimarray(v.s6, (uint)(s6_seed % 6), (uint)(s6_seed % 3), s6_seed);

            if (v.s7 == null) throw new Exception("");
            if (v.s7.Count != 1) throw new Exception("");
            var s7_0 = v.s7[0];
            verify_testpod1(ref s7_0, gen.get_uint32());

            if (v.s8 == null) throw new Exception("");
            if (v.s8.Count != 2) throw new Exception("");
            verify_testpod1_array(v.s8[0], 2, gen.get_uint32());
            verify_testpod1_array(v.s8[1], 4, gen.get_uint32());

            if (v.s9 == null) throw new Exception("");
            if (v.s9.Count != 2) throw new Exception("");
            verify_testpod1_multidimarray(v.s9[0], 2, 3, gen.get_uint32());
            verify_testpod1_multidimarray(v.s9[1], 4, 5, gen.get_uint32());

            var s10 = ((testpod1[])v.s10)[0];
            verify_testpod1(ref s10, gen.get_uint32());

            verify_testpod1_array((testpod1[])v.s11, 3, gen.get_uint32());
            verify_testpod1_multidimarray(v.s12, 2, 2, gen.get_uint32());

            if (v.s13 == null) throw new Exception("");
            var s13 = (testpod1[])((List<object>)v.s13)[0];
            verify_testpod1(ref s13[0], gen.get_uint32());

            if (v.s14 == null) throw new Exception("");
            var v14 = (List<object>)v.s14;
            if (v14.Count != 2) throw new Exception("");
            verify_testpod1_array((testpod1[])v14[0], 3, gen.get_uint32());
            verify_testpod1_array((testpod1[])v14[1], 5, gen.get_uint32());

            if (v.s15 == null) throw new Exception("");
            var v15 = (List<object>)v.s15;
            if (v15.Count != 2) throw new Exception("");
            verify_testpod1_multidimarray(v15[0], 7, 2, gen.get_uint32());
            verify_testpod1_multidimarray(v15[1], 5, 1, gen.get_uint32());

            verify_transform(ref v.t1, gen.get_uint32());

            verify_transform_array(v.t2, 4, gen.get_uint32());
            verify_transform_multidimarray(v.t3, 2, 4, gen.get_uint32());

            verify_transform_array((transform[])(v.t4), 10, gen.get_uint32());
            verify_transform_multidimarray((NamedMultiDimArray)(v.t5), 6, 5, gen.get_uint32());

            if (v.t6 == null) throw new Exception("");
            if (v.t6.Count != 1) throw new Exception("");
            var t6_0 = v.t6[0];
            verify_transform(ref t6_0, gen.get_uint32());

            if (v.t7 == null) throw new Exception("");
            if (v.t7.Count != 2) throw new Exception("");
            verify_transform_array(v.t7[0], 4, gen.get_uint32());
            verify_transform_array(v.t7[1], 4, gen.get_uint32());

            if (v.t8 == null) throw new Exception("");
            if (v.t8.Count != 2) throw new Exception("");
            verify_transform_multidimarray(v.t8[0], 2, 4, gen.get_uint32());
            verify_transform_multidimarray(v.t8[1], 2, 4, gen.get_uint32());

            if (v.t9 == null) throw new Exception("");
            var t9 = (transform[])((List<object>)v.t9)[0];
            verify_transform(ref t9[0], gen.get_uint32());

            if (v.t10 == null) throw new Exception("");
            var t10 = (List<object>)v.t10;
            if (t10.Count != 2) throw new Exception("");
            verify_transform_array((transform[])t10[0], 3, gen.get_uint32());
            verify_transform_array((transform[])t10[1], 5, gen.get_uint32());

            if (v.t11 == null) throw new Exception("");
            var t11 = (List<object>)v.t11;
            if (t11.Count != 2) throw new Exception("");
            verify_transform_multidimarray((NamedMultiDimArray)t11[0], 7, 2, gen.get_uint32());
            verify_transform_multidimarray((NamedMultiDimArray)t11[1], 5, 1, gen.get_uint32());

        }

        public static void fill_transform(ref transform t, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var a = new double[7];
            for (int i = 0; i < 7; i++) a[i] = gen.get_double();
            var a2 = new ArraySegment<double>(a);
            t.AssignFromNumericArray(ref a2);
        }

        public static void verify_transform(ref transform t, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var a = t.GetNumericArray();
            for (int i = 0; i < 7; i++) if (a[i] != gen.get_double()) throw new Exception();
        }

        public static transform[] fill_transform_array(int len, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            var o = new transform[len];
            for (int i = 0; i < len; i++) fill_transform(ref o[i], gen.get_uint32());
            return o;
        }

        public static void verify_transform_array(transform[] t, int len, uint seed)
        {
            var gen = new ServiceTest2_test_sequence_gen(seed);
            if (t.Length != len) throw new Exception();
            for (int i = 0; i < len; i++) verify_transform(ref t[i], gen.get_uint32());
        }

        public static NamedMultiDimArray fill_transform_multidimarray(uint m, uint n, uint seed)
        {
            return new NamedMultiDimArray(new uint[] { m, n }, fill_transform_array((int)(m * n), seed));
        }

        public static void verify_transform_multidimarray(NamedMultiDimArray a, uint m, uint n, uint seed)
        {
            if (!a.Dims.SequenceEqual(new uint[] { m, n })) throw new Exception();
            verify_transform_array((transform[])a.namedarray_array, (int)(m * n), seed);
        }
    }

}
