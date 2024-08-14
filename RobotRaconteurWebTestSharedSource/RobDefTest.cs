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
    }
}
