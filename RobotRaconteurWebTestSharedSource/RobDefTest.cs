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

        public static void TestServiceDefinitionDuplicateStdver()
        {
            // A second stdver line must be rejected
            var def = new ServiceDefinition();
            def.FromString("service test.stdver1\nstdver 0.10\n\nobject obj\n    property double d1\nend\n");
            RRAssert.IsTrue(def.StdVer == new RobotRaconteurVersion(0, 10));
            var def2 = new ServiceDefinition();
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => def2.FromString("service test.stdver2\nstdver 0.10\nstdver 0.9\n\nobject obj\n    property double d1\nend\n"));
            RRWebTest.WriteLine("RobDefTest.TestServiceDefinitionDuplicateStdver passed");
        }

        public static void TestNumberParseException()
        {
            // Numbers that do not fit their type must raise ServiceDefinitionParseException
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => new RobotRaconteurVersion("99999999999.0"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => new TypeDefinition().FromString("double[99999999999] x"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => new TypeDefinition().FromString("double[2147483648] x"));
            RRAssert.ThrowsException<ServiceDefinitionParseException>(() => new TypeDefinition().FromString("double[3,,4] x"));
            var t = new TypeDefinition();
            t.FromString("double[3,4] x");
            RRAssert.AreEqual(t.ArrayLength.Count, 2);
            RRAssert.AreEqual(t.ArrayLength[1], 4);
            RRWebTest.WriteLine("RobDefTest.TestNumberParseException passed");
        }

        static bool ConstantAccepted(string s)
        {
            try
            {
                new ConstantDefinition((ServiceDefinition)null).FromString(s);
                return true;
            }
            catch (ServiceDefinitionParseException)
            {
                return false;
            }
        }

        public static void TestConstantIntegerValues()
        {
            // int64 constants were rejected (the signed range switch had uint64 for int64)
            RRAssert.IsTrue(ConstantAccepted("constant int64 x 5"));
            RRAssert.IsTrue(ConstantAccepted("constant int64 x -9223372036854775808"));
            RRAssert.IsTrue(ConstantAccepted("constant int64 x 9223372036854775807"));
            RRAssert.IsTrue(ConstantAccepted("constant int64 x 0x7FFFFFFFFFFFFFFF"));
            RRAssert.IsFalse(ConstantAccepted("constant int64 x 9223372036854775808"));
            RRAssert.IsFalse(ConstantAccepted("constant int64 x 0x8000000000000000"));
            RRAssert.IsTrue(ConstantAccepted("constant int64[] x {1, -2, 0x3}"));
            // The value zero threw ArgumentOutOfRangeException (leading zeros were trimmed to an empty string)
            RRAssert.IsTrue(ConstantAccepted("constant int32 z 0"));
            RRAssert.IsTrue(ConstantAccepted("constant int16 z 0x0"));
            RRAssert.IsTrue(ConstantAccepted("constant int8 z -0x0"));
            RRAssert.IsTrue(ConstantAccepted("constant int32[] z {0, 1}"));
            // Signed constants were not range checked (&& for ||)
            RRAssert.IsTrue(ConstantAccepted("constant int8 n 127"));
            RRAssert.IsTrue(ConstantAccepted("constant int8 n -128"));
            RRAssert.IsTrue(ConstantAccepted("constant int8 n -0x80"));
            RRAssert.IsFalse(ConstantAccepted("constant int8 n 200"));
            RRAssert.IsFalse(ConstantAccepted("constant int8 n -129"));
            RRAssert.IsFalse(ConstantAccepted("constant int8 n 0x80"));
            RRAssert.IsFalse(ConstantAccepted("constant int16 n 99999"));
            RRAssert.IsTrue(ConstantAccepted("constant int32 n 2147483647"));
            RRAssert.IsTrue(ConstantAccepted("constant int32 n -2147483648"));
            RRAssert.IsFalse(ConstantAccepted("constant int32 n 99999999999"));
            RRAssert.IsFalse(ConstantAccepted("constant int32 n 0xFFFFFFFF"));
            RRAssert.IsFalse(ConstantAccepted("constant int32[] n {1, 2147483648}"));
            RRAssert.IsTrue(ConstantAccepted("constant int32 n +7"));
            RRAssert.IsTrue(ConstantAccepted("constant uint8 n 255"));
            RRAssert.IsFalse(ConstantAccepted("constant uint8 n 256"));
            RRWebTest.WriteLine("RobDefTest.TestConstantIntegerValues passed");
        }

        public static void TestConstantStringUnicodeEscape()
        {
            // \uXXXX is big-endian (the code point in hex); the bytes were decoded little-endian, so e-acute
            // (U+00E9) became U+E900 and \u0001 became U+0100
            var c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant string s \"\\u00e9\\u0001A\\u4E2D\\uD83D\\uDE00\"");
            RRAssert.AreEqual(c.ValueToString(), "é\u0001A中\U0001F600");

            RRAssert.AreEqual(ConstantDefinition.EscapeString("é"), "\\u00E9");
            RRAssert.AreEqual(ConstantDefinition.EscapeString("\u0001"), "\\u0001");
            // Characters above U+00FF were not escaped at all, so the result was not a valid string constant
            RRAssert.AreEqual(ConstantDefinition.EscapeString("中\U0001F600"), "\\u4E2D\\uD83D\\uDE00");
            var c2 = new ConstantDefinition((ServiceDefinition)null);
            c2.FromString("constant string s \"" + ConstantDefinition.EscapeString("aé中\"") + "\"");
            RRAssert.AreEqual(c2.ValueToString(), "aé中\"");

            // The generator wrote the same swapped escapes into the C# string literal
            var gen = CSharpServiceLangGen.convert_constant(c, new Dictionary<string, ConstantDefinition>(), null);
            RRAssert.IsTrue(gen.Contains("\\u00E9\\u0001A"));
            RRWebTest.WriteLine("RobDefTest.TestConstantStringUnicodeEscape passed");
        }

        static Dictionary<string, ServiceDefinition> ParseDefs(params string[] texts)
        {
            var defs = new Dictionary<string, ServiceDefinition>();
            foreach (var t in texts)
            {
                var d = new ServiceDefinition();
                d.FromString(t);
                defs.Add(d.Name, d);
            }
            return defs;
        }

        public static void TestRecursiveImplementsAndImportsNotThroughRoot()
        {
            // A cycle that does not return to the object (or definition) being verified recursed until the stack
            // overflowed, which ends the process
            var defs1 = ParseDefs("service t7.impl\nstdver 0.10\n\nobject a\nimplements b\nproperty double p\nend\n\n" +
                                  "object b\nimplements c\nproperty double p\nend\n\n" +
                                  "object c\nimplements b\nproperty double p\nend\n");
            RRAssert.ThrowsException<ServiceDefinitionVerifyException>(
                () => ServiceDefinitionUtil.VerifyServiceDefinitions(defs1));

            var defs2 = ParseDefs("service t7.a\nstdver 0.10\nimport t7.b\n\nobject oa\nproperty double p\nend\n",
                                  "service t7.b\nstdver 0.10\nimport t7.c\n\nobject ob\nproperty double p\nend\n",
                                  "service t7.c\nstdver 0.10\nimport t7.b\n\nobject oc\nproperty double p\nend\n");
            RRAssert.ThrowsException<ServiceDefinitionVerifyException>(
                () => ServiceDefinitionUtil.VerifyServiceDefinitions(defs2));

            // Diamonds are not cycles
            var defs3 = ParseDefs("service t7.a\nstdver 0.10\nimport t7.b\nimport t7.c\n\nobject oa\nproperty double p\nend\n",
                                  "service t7.b\nstdver 0.10\nimport t7.c\n\nobject ob\nproperty double p\nend\n",
                                  "service t7.c\nstdver 0.10\n\nobject oc\nproperty double p\nend\n",
                                  "service t7.impl\nstdver 0.10\n\nobject a\nimplements b\nimplements c\nproperty double p\nend\n\n" +
                                  "object b\nimplements c\nproperty double p\nend\n\n" +
                                  "object c\nproperty double p\nend\n");
            ServiceDefinitionUtil.VerifyServiceDefinitions(defs3);
            RRWebTest.WriteLine("RobDefTest.TestRecursiveImplementsAndImportsNotThroughRoot passed");
        }

        public static void TestGenSkelKeywordObjectName()
        {
            // An object named after a C# keyword gave a class lock__skel with a constructor lock_skel and casts to
            // (lock), which does not compile
            var d = new ServiceDefinition();
            d.FromString("service t7.gen1\nstdver 0.10\n\nobject lock\nproperty double p\npipe double pp\nend\n");
            var w = new StringWriter();
            CSharpServiceLangGen.GenerateServiceFactory(d, "", w);
            CSharpServiceLangGen.GenerateSkel(d.Objects["lock"], w);
            var gen = w.ToString();
            RRAssert.IsTrue(gen.Contains("case \"lock\":"));
            RRAssert.IsTrue(gen.Contains("return new lock__skel(path,(lock_)obj,context);"));
            RRAssert.IsTrue(gen.Contains("public class lock__skel : ServiceSkel {"));
            RRAssert.IsTrue(gen.Contains("public lock__skel(string p,lock_ o,ServerContext c)"));
            RRAssert.IsTrue(gen.Contains("lock_ castobj=(lock_)o;"));
            RRAssert.IsFalse(gen.Contains("(lock)"));
            RRWebTest.WriteLine("RobDefTest.TestGenSkelKeywordObjectName passed");
        }

        public static void TestGenSingleConstants()
        {
            // "constant single f1 1.5" was written as "public const float f1=1.5;", which does not compile (a
            // literal without suffix is a double)
            var c2 = new Dictionary<string, ConstantDefinition>();
            var c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant single f1 1.5");
            RRAssert.AreEqual(CSharpServiceLangGen.convert_constant(c, c2, null), "public const float f1=1.5F;");
            c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant single[] fa {1.5, -2, 3e2}");
            RRAssert.AreEqual(CSharpServiceLangGen.convert_constant(c, c2, null),
                              "public static readonly float[] fa={1.5F, -2F, 3e2F};");
            c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant single[] fe {}");
            RRAssert.AreEqual(CSharpServiceLangGen.convert_constant(c, c2, null),
                              "public static readonly float[] fe={};");
            // Other types are unchanged
            c = new ConstantDefinition((ServiceDefinition)null);
            c.FromString("constant double d1 1.5");
            RRAssert.AreEqual(CSharpServiceLangGen.convert_constant(c, c2, null), "public const double d1=1.5;");
            RRWebTest.WriteLine("RobDefTest.TestGenSingleConstants passed");
        }

        public static void TestGenKeywordWireNames()
        {
            // Strings compared with names on the wire must be the Robot Raconteur names, not the escaped C#
            // identifiers: the indexed objref member name ("fixed_"), the objref type name ("t7.gen1.lock_") and
            // the client callback dispatch label ("checked_", the service sends "checked")
            var d = new ServiceDefinition();
            d.FromString("service t7.gen1\nstdver 0.10\n\nobject lock\nproperty double p\nend\n\n" +
                         "object o\nobjref lock{int32} fixed\nobjref lock while\ncallback void checked()\nend\n");
            var w = new StringWriter();
            CSharpServiceLangGen.GenerateStub(d.Objects["o"], w);
            var gen = w.ToString();
            RRAssert.IsTrue(gen.Contains("FindObjRefTyped(\"fixed\",ind.ToString(),\"t7.gen1.lock\",cancel)"));
            RRAssert.IsTrue(gen.Contains("FindObjRefTyped(\"while\",\"t7.gen1.lock\",cancel)"));
            RRAssert.IsTrue(gen.Contains("case \"checked\": {"));
            RRAssert.IsFalse(gen.Contains("\"fixed_\""));
            RRAssert.IsFalse(gen.Contains("\"checked_\""));
            RRAssert.IsFalse(gen.Contains("lock_\""));
            RRWebTest.WriteLine("RobDefTest.TestGenKeywordWireNames passed");
        }

        public static void TestGenAsyncDelegateReturnType()
        {
            // String.Format("Task<{1}{2}>", t1) had one argument and indexes 1 and 2, so it threw FormatException
            // for any non-void return type
            var r = new TypeDefinition();
            r.FromString("double[] ret");
            var p = new TypeDefinition();
            p.FromString("int32 a");
            RRAssert.AreEqual(CSharpServiceLangGen.str_pack_async_delegate(new List<TypeDefinition>() { p }, r),
                              "Func<int, CancellationToken, Task<double[]>>");
            RRWebTest.WriteLine("RobDefTest.TestGenAsyncDelegateReturnType passed");
        }

        static ServiceDefinitionParseInfo ParseErrorInfo(string robdef)
        {
            try
            {
                new ServiceDefinition().FromString(robdef);
            }
            catch (ServiceDefinitionParseException e)
            {
                return e.ParseInfo;
            }
            throw new Exception("Unit test failure: definition parsed without error");
        }

        public static void TestParseErrorLineNumbers()
        {
            // Line numbers started at 0 (one too low; Core starts at 1), and errors inside a struct / object block
            // and all "Invalid constant definition" errors had neither the service name nor a line number
            var i1 = ParseErrorInfo("service t7.lines\n\nstdver 0.10\n\nconstant int32 c 1.5\n");
            RRAssert.AreEqual(i1.ServiceName, "t7.lines");
            RRAssert.AreEqual(i1.LineNumber ?? -1, 5);
            RRAssert.AreEqual(i1.Line, "constant int32 c 1.5");

            var i2 = ParseErrorInfo("service t7.lines\nstdver 0.10\n\nobject o\nproperty double p\nproperty double q(\nend\n");
            RRAssert.AreEqual(i2.ServiceName, "t7.lines");
            RRAssert.AreEqual(i2.LineNumber ?? -1, 6);

            var i3 = ParseErrorInfo("service t7.lines\nstdver 0.10\n\nobject o\nconstant int32 c 1.5\nend\n");
            RRAssert.AreEqual(i3.ServiceName, "t7.lines");
            RRAssert.AreEqual(i3.LineNumber ?? -1, 5);

            var i4 = ParseErrorInfo("service t7.lines\nstdver 0.10\nbad line\n");
            RRAssert.AreEqual(i4.LineNumber ?? -1, 3);
            RRWebTest.WriteLine("RobDefTest.TestParseErrorLineNumbers passed");
        }

        static bool MemberAccepted(string member)
        {
            try
            {
                new ServiceDefinition().FromString("service t7.members\nstdver 0.10\n\nobject o\n" + member + "\nend\n");
                return true;
            }
            catch (ServiceDefinitionParseException)
            {
                return false;
            }
        }

        public static void TestMemberParentheses()
        {
            // Parameters were initialised to an empty list, so missing parentheses on function / callback / event
            // and empty parentheses on the other members were not detected
            RRAssert.IsFalse(MemberAccepted("function double f"));
            RRAssert.IsFalse(MemberAccepted("callback void cb"));
            RRAssert.IsFalse(MemberAccepted("event e"));
            RRAssert.IsFalse(MemberAccepted("property double p()"));
            RRAssert.IsFalse(MemberAccepted("pipe double[] p1()"));
            RRAssert.IsFalse(MemberAccepted("wire double w1()"));
            RRAssert.IsTrue(MemberAccepted("function double f()"));
            RRAssert.IsTrue(MemberAccepted("function double f(double a, int32 b)"));
            RRAssert.IsTrue(MemberAccepted("callback void cb()"));
            RRAssert.IsTrue(MemberAccepted("event e()"));
            RRAssert.IsTrue(MemberAccepted("property double p"));
            RRAssert.IsTrue(MemberAccepted("property double p [readonly]"));
            RRAssert.IsTrue(MemberAccepted("pipe double[] p1"));
            RRWebTest.WriteLine("RobDefTest.TestMemberParentheses passed");
        }

        public static void TestEntryConstantsToString()
        {
            // ServiceEntryDefinition.ToWriter wrote the key / value pair ("[c, constant int32 c 5]") for a constant
            // inside an object, so ToString() could not be parsed back
            var d = new ServiceDefinition();
            d.FromString("service t7.entryconst\nstdver 0.10\n\nobject o\nconstant int32 c 5\nproperty double p\nend\n");
            var s = d.ToString();
            RRAssert.IsTrue(s.Contains("    constant int32 c 5\n"));
            RRAssert.IsFalse(s.Contains("[c, "));
            var d2 = new ServiceDefinition();
            d2.FromString(s);
            RRAssert.AreEqual(d2.Objects["o"].Constants["c"].Value, "5");
            RRWebTest.WriteLine("RobDefTest.TestEntryConstantsToString passed");
        }

        public static void TestVerifyWithoutStdver()
        {
            // VerifyVersionSupport compared the StdVer struct with null, which is never true, so a definition
            // without stdver that uses a member modifier, constant, using or enum failed verification ("standard
            // version 0.9 or greater required"); Core skips the check when the version is unset
            var defs = ParseDefs2("service t7.nostdver\n\nconstant int32 c 5\n\nobject o\nproperty double p [readonly]\nend\n");
            ServiceDefinitionUtil.VerifyServiceDefinitions(defs);
            // A definition with an older stdver is still checked
            var defs2 = ParseDefs2("service t7.oldstdver\noption version 0.8\n\nobject o\nproperty double p [readonly]\nend\n");
            RRAssert.ThrowsException<ServiceDefinitionException>(() => ServiceDefinitionUtil.VerifyServiceDefinitions(defs2));
            RRWebTest.WriteLine("RobDefTest.TestVerifyWithoutStdver passed");
        }

        static Dictionary<string, ServiceDefinition> ParseDefs2(string text)
        {
            var d = new ServiceDefinition();
            d.FromString(text);
            var defs = new Dictionary<string, ServiceDefinition>();
            defs.Add(d.Name, d);
            return defs;
        }

        public static void TestImplementsEventMismatch()
        {
            // CompareMember tested ev2 where ev1 was meant: an implemented event matched against a member that is not
            // an event dereferenced null (NullReferenceException from VerifyServiceDefinitions)
            var d = new ServiceDefinition();
            d.FromString("service t7.ev\nstdver 0.10\n\nobject b\nevent x()\nend\n\nobject a\nimplements b\nobjref b x\nend\n");
            var defs = new Dictionary<string, ServiceDefinition>();
            defs.Add(d.Name, d);
            RRAssert.ThrowsException<ServiceDefinitionVerifyException>(() => ServiceDefinitionUtil.VerifyServiceDefinitions(defs));

            var d2 = new ServiceDefinition();
            d2.FromString("service t7.ev2\nstdver 0.10\n\nobject b\nevent x(double a)\nend\n\nobject a\nimplements b\nevent x(double a)\nend\n");
            var defs2 = new Dictionary<string, ServiceDefinition>();
            defs2.Add(d2.Name, d2);
            ServiceDefinitionUtil.VerifyServiceDefinitions(defs2);
            RRWebTest.WriteLine("RobDefTest.TestImplementsEventMismatch passed");
        }
    }
}
