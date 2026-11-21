using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    public class RobDefTest
    {
        public static void RunRobDefTest(string[] filenames)
        {
            var robdef_filenames = filenames;

            var defs = new Dictionary<string, ServiceDefinition>();
            var defs2 = new Dictionary<string, ServiceDefinition>();

            foreach (var fname in robdef_filenames)
            {
                string robdef_text = new StreamReader(fname).ReadToEnd();
                var def = new ServiceDefinition();
                def.FromString(robdef_text);
                defs.Add(def.Name, def);
                string robdef_text2 = def.ToString();
                var def3 = new ServiceDefinition();
                def3.FromString(robdef_text2);
                defs2.Add(def3.Name, def3);
            }

            ServiceDefinitionUtil.VerifyServiceDefinitions(defs);

            foreach (var n in defs.Keys)
            {
                if (!ServiceDefinitionUtil.CompareServiceDefinitions(defs[n], defs2[n]))
                {
                    throw new Exception("Service definition parse does not match");
                }
            }

            foreach (var def in defs.Values)
            {
                RRWebTest.WriteLine(def.ToString());
            }


            foreach (var def in defs.Values)

            {
                foreach (var c in def.Constants.Values)

                {
                    if (c.Name == "strconst")
                    {
                        var strconst = c.ValueToString();
                        RRWebTest.WriteLine("strconst " + strconst);

                        var strconst2 = ConstantDefinition.EscapeString(strconst);
                        var strconst3 = ConstantDefinition.UnescapeString(strconst2);

                        if (strconst3 != strconst)
                            throw new Exception("");
                    }

                    if (c.Name == "int32const")
                    {
                        RRWebTest.WriteLine("int32const: " + c.ValueToScalar<int>());
                    }

                    if (c.Name == "int32const_array")
                    {
                        var a = c.ValueToArray<int>();
                        RRWebTest.WriteLine("int32const_array: " + a.Length);
                    }

                    if (c.Name == "doubleconst_array")
                    {
                        var a = c.ValueToArray<double>();
                        RRWebTest.WriteLine("doubleconst_array: " + a.Length);
                    }

                    if (c.Name == "structconst")
                    {
                        var s = c.ValueToStructFields();
                        RRWebTest.WriteLine(string.Join(" ", s.Select(f => f.Name + ": " + f.ConstantRefName).ToArray()));

                        RRWebTest.WriteLine("");
                    }
                }
            }

            ServiceDefinition def1;
            if (defs.TryGetValue("com.robotraconteur.testing.TestService1", out def1))
            {
                var entry = def1.Objects["testroot"];

                var p1 = (PropertyDefinition)entry.Members["d1"];
                if (p1.Direction != MemberDefinition_Direction.both)
                    throw new Exception();

                var p2 = (PipeDefinition)entry.Members["p1"];
                if (p2.Direction != MemberDefinition_Direction.both)
                    throw new Exception();
                if (p2.IsUnreliable)
                    throw new Exception();

                var w1 = (WireDefinition)entry.Members["w1"];
                if (w1.Direction != MemberDefinition_Direction.both)
                    throw new Exception();

                var m1 = (MemoryDefinition)entry.Members["m1"];
                if (m1.Direction != MemberDefinition_Direction.both)
                    throw new Exception();
            }

            ServiceDefinition def2;
            if (defs.TryGetValue("com.robotraconteur.testing.TestService3", out def2))
            {
                var entry = def2.Objects["testroot3"];

                var p1 = (PropertyDefinition)entry.Members["readme"];
                if (p1.Direction != MemberDefinition_Direction.readonly_)
                    throw new Exception();

                var p2 = (PropertyDefinition)entry.Members["writeme"];
                if (p2.Direction != MemberDefinition_Direction.writeonly)
                    throw new Exception();

                var p3 = (PipeDefinition)entry.Members["unreliable1"];
                if (p3.Direction != MemberDefinition_Direction.readonly_)
                    throw new Exception();
                if (!p3.IsUnreliable)
                    throw new Exception();

                var p4 = (PipeDefinition)entry.Members["unreliable2"];
                if (p4.Direction != MemberDefinition_Direction.both)
                    throw new Exception();
                if (!p4.IsUnreliable)
                    throw new Exception();

                var w1 = (WireDefinition)entry.Members["peekwire"];
                if (w1.Direction != MemberDefinition_Direction.readonly_)
                    throw new Exception();

                var w2 = (WireDefinition)entry.Members["pokewire"];
                if (w2.Direction != MemberDefinition_Direction.writeonly)
                    throw new Exception();

                var m1 = (MemoryDefinition)entry.Members["readmem"];
                if (m1.Direction != MemberDefinition_Direction.readonly_)
                    throw new Exception();

                RRWebTest.WriteLine("Found it");
            }

            return;
        }

        public static void TestConstantInt64()
        {
            // int64 constants must be accepted
            var c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant int64 c1 -9223372036854775808");
            RRAssert.AreEqual(c.Value, "-9223372036854775808");
            c.FromString("constant int64 c2 1234");
            RRAssert.AreEqual(c.Value, "1234");
            RRWebTest.WriteLine("RobDefTest.TestConstantInt64 passed");
        }

        public static void TestConstantSignedRange()
        {
            // Signed constants outside the range of their type must be rejected
            var c = new ConstantDefinition((ServiceDefinition)null);
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => c.FromString("constant int8 c1 1000"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => c.FromString("constant int8 c1 -129"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => c.FromString("constant int16 c1 32768"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => c.FromString("constant int32 c1 -2147483649"));
            c.FromString("constant int8 c1 -128");
            c.FromString("constant int8 c1 127");
            c.FromString("constant int32 c1 -2147483648");
            RRWebTest.WriteLine("RobDefTest.TestConstantSignedRange passed");
        }

        public static void TestConstantSignedZero()
        {
            // Signed constants written as zero must parse (TrimStart('0') produced an empty string)
            var c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant int32 c1 0");
            c.FromString("constant int8 c1 00");
            c.FromString("constant int16 c1 0x0");
            c.FromString("constant int32 c1 -0x0");
            c.FromString("constant int32 c1 0x10");
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => c.FromString("constant int8 c1 0xZ"));
            RRWebTest.WriteLine("RobDefTest.TestConstantSignedZero passed");
        }

        public static void TestTypeDefinitionEquality()
        {
            // TypeDefinition == must compare by value, including Type and ArrayLength contents
            var a = new TypeDefinition();
            a.FromString("double[3] x");
            var b = new TypeDefinition();
            b.FromString("double[3] x");
            RRAssert.IsTrue(a == b);
            RRAssert.IsTrue(a.Equals(b));
            var c = new TypeDefinition();
            c.FromString("double[4] x");
            RRAssert.IsTrue(a != c);
            var d = new TypeDefinition();
            d.FromString("int32[3] x");
            RRAssert.IsTrue(a != d);
            RRWebTest.WriteLine("RobDefTest.TestTypeDefinitionEquality passed");
        }

        public static void TestTypeDefinitionMultiDimSingleToString()
        {
            // A single-dimension fixed multidim array must be written as [N,] so it re-parses as a multidim array
            var a = new TypeDefinition();
            a.FromString("double[3,] x");
            RRAssert.AreEqual(a.ArrayType, DataTypes_ArrayTypes.multidimarray);
            RRAssert.AreEqual(a.ToString(), "double[3,] x");
            var b = new TypeDefinition();
            b.FromString(a.ToString());
            RRAssert.AreEqual(b.ArrayType, DataTypes_ArrayTypes.multidimarray);
            RRAssert.IsFalse(b.ArrayVarLength);
            RRAssert.AreEqual(b.ArrayLength.Count, 1);
            RRAssert.AreEqual(b.ArrayLength[0], 3);
            RRWebTest.WriteLine("RobDefTest.TestTypeDefinitionMultiDimSingleToString passed");
        }

        public static void TestObjRefArrayContainer()
        {
            // An objref may have an array or a container, not both
            var entry = new ServiceEntryDefinition(new ServiceDefinition());
            var o = new ObjRefDefinition(entry);
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => o.FromString("objref varobject[]{string} x"));
            o = new ObjRefDefinition(entry);
            o.FromString("objref varobject[] x");
            RRAssert.AreEqual(o.ArrayType, DataTypes_ArrayTypes.array);
            RRAssert.AreEqual(o.ContainerType, DataTypes_ContainerTypes.none);
            o.ToString();
            o = new ObjRefDefinition(entry);
            o.FromString("objref varobject{string} x");
            RRAssert.AreEqual(o.ArrayType, DataTypes_ArrayTypes.none);
            RRAssert.AreEqual(o.ContainerType, DataTypes_ContainerTypes.map_string);
            o.ToString();
            RRWebTest.WriteLine("RobDefTest.TestObjRefArrayContainer passed");
        }

        public static void TestServiceDefinitionFirstLine()
        {
            // ParseInfo.Line of a parsed service definition must hold its first line
            var def = new ServiceDefinition();
            def.FromString("service test.firstline\n\nobject obj\n    property double d1\nend\n");
            RRAssert.IsTrue(def.ParseInfo.Line != null);
            RRAssert.AreEqual(def.ParseInfo.Line.Trim(), "service test.firstline");
            RRWebTest.WriteLine("RobDefTest.TestServiceDefinitionFirstLine passed");
        }

        public static void TestUsingAsWhitespace()
        {
            // Any amount of blanks may follow "as", as everywhere else in the parser
            var u = new UsingDefinition(null);
            u.FromString("using a.b.c as  d");
            RRAssert.AreEqual(u.QualifiedName, "a.b.c");
            RRAssert.AreEqual(u.UnqualifiedName, "d");
            u = new UsingDefinition(null);
            u.FromString("using a.b.c as\td");
            RRAssert.AreEqual(u.UnqualifiedName, "d");
            RRWebTest.WriteLine("RobDefTest.TestUsingAsWhitespace passed");
        }
    }
}
