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
using System.Threading.Tasks;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string command = args[0];
            if (command == "loopback")
            {
                var t = new TcpTransport();
                t.StartServer(22332);
                RobotRaconteurNode.s.RegisterTransport(t);

                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var s1 = new RobotRaconteurTestServiceSupport();
                s1.RegisterServices(t);

                ServiceTestClient s = new ServiceTestClient();
                s.RunFullTest("rr+tcp://localhost:22332/?service=RobotRaconteurTestService",
                    "rr+tcp://localhost:22332/?service=RobotRaconteurTestService_auth")
                    .GetAwaiter().GetResult();

                RobotRaconteurNode.s.Shutdown();

            }
            else
            if (command == "client")
            {

                var url = args[1];
                var auth_url = args[2];
                int count = 1;
                if (args.Length >= 4)
                {
                    count = Int32.Parse(args[3]);
                }

                var t = new TcpTransport();
                var t2 = new LocalTransport();

                RobotRaconteurNode.s.RegisterTransport(t);
                RobotRaconteurNode.s.RegisterTransport(t2);


                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                for (int i=0; i<count; i++)
                {
                    ServiceTestClient s = new ServiceTestClient();
                    s.RunFullTest(url, auth_url).GetAwaiter().GetResult();
                }

                RobotRaconteurNode.s.Shutdown();

                Console.WriteLine("Test complete, no errors detected");
            }
            else if (command == "server")
            {
                int port = int.Parse(args[1]);

                //NodeID id = new NodeID(args[2]);
                string name = args[2];


                var t2 = new LocalTransport();
                t2.StartServerAsNodeName(name);


                var t = new TcpTransport();
                RobotRaconteurNode.s.RegisterTransport(t2);

                t.StartServer(port);

                RobotRaconteurNode.s.RegisterTransport(t);
                t.EnableNodeAnnounce();

                try
                {
                    t.LoadTlsNodeCertificate();
                }
                catch (Exception)
                {
                    Console.WriteLine("Warning: Could not load TLS certificate");
                }

                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

                var s = new RobotRaconteurTestServiceSupport();
                s.RegisterServices(t);

                var s2 = new RobotRaconteurTestServiceSupport2();
                s2.RegisterServices(t);

                Console.WriteLine("Press enter to quit...");
                Console.ReadLine();

                RobotRaconteurNode.s.Shutdown();

            }
            else if (command == "findservicebytype")
            {
                string servicetype = args[1];
                string[] schemes = args[2].Split(new char[] { ',' });

                var t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);


                System.Threading.Thread.Sleep(5000);

                var r = RobotRaconteurNode.s.FindServiceByType(servicetype, schemes).Result;

                foreach (var e in r)
                {
                    Console.WriteLine("Name: " + e.Name);
                    Console.WriteLine("RootObjectType: " + e.RootObjectType);
                    Console.WriteLine("RootObjectImplements: " + e.ConnectionURL);
                    Console.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    Console.WriteLine("NodeID: " + e.NodeID.ToString());
                    Console.WriteLine("NodeName: " + e.NodeName);
                    Console.WriteLine();
                }

                RobotRaconteurNode.s.Shutdown();

            }
            else if (command == "findnodebyid")
            {
                var nodeid = new NodeID(args[1]);
                string[] schemes = args[2].Split(new char[] { ',' });

                var t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);

                var c2 = new LocalTransport();
                RobotRaconteurNode.s.RegisterTransport(c2);

                System.Threading.Thread.Sleep(6000);

                var r = RobotRaconteurNode.s.FindNodeByID(nodeid, schemes).Result;

                foreach (var e in r)
                {
                    Console.WriteLine("NodeID: " + e.NodeID);
                    Console.WriteLine("NodeName: " + e.NodeName);
                    Console.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    Console.WriteLine();
                }

                RobotRaconteurNode.s.Shutdown();
            }
            else if (command == "findnodebyname")
            {
                var name = args[1];
                string[] schemes = args[2].Split(new char[] { ',' });

                var t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);

                var c2 = new LocalTransport();
                RobotRaconteurNode.s.RegisterTransport(c2);

                System.Threading.Thread.Sleep(6000);

                var r = RobotRaconteurNode.s.FindNodeByName(name, schemes).Result;

                foreach (var e in r)
                {
                    Console.WriteLine("NodeID: " + e.NodeID);
                    Console.WriteLine("NodeName: " + e.NodeName);
                    Console.WriteLine("ConnectionURL: " + String.Join(", ", e.ConnectionURL));
                    Console.WriteLine();
                }

                RobotRaconteurNode.s.Shutdown();
            }
            else if (command == "robdeftest")
            {
                var robdef_filenames = args.Skip(1).ToArray();

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
                    Console.WriteLine(def.ToString());
                }


                foreach (var def in defs.Values)

                {
                    foreach (var c in def.Constants.Values)

                    {
                        if (c.Name == "strconst")
                        {
                            var strconst = c.ValueToString();
                            Console.WriteLine("strconst " + strconst);

                            var strconst2 = ConstantDefinition.EscapeString(strconst);
                            var strconst3 = ConstantDefinition.UnescapeString(strconst2);

                            if (strconst3 != strconst)
                                throw new Exception("");
                        }

                        if (c.Name == "int32const")
                        {
                            Console.WriteLine("int32const: " + c.ValueToScalar<int>());
                        }

                        if (c.Name == "int32const_array")
                        {
                            var a = c.ValueToArray<int>();
                            Console.WriteLine("int32const_array: " + a.Length);
                        }

                        if (c.Name == "doubleconst_array")
                        {
                            var a = c.ValueToArray<double>();
                            Console.WriteLine("doubleconst_array: " + a.Length);
                        }

                        if (c.Name == "structconst")
                        {
                            var s = c.ValueToStructFields();
                            foreach (var f in s)
                            {
                                Console.Write(f.Name + ": " + f.ConstantRefName + " ");
                            }
                            Console.WriteLine();
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

                    Console.WriteLine("Found it");
                }

                return;
            }
            else
            if (command == "loopback2")
            {
                var t = new TcpTransport();
                t.StartServer(22332);
                RobotRaconteurNode.s.RegisterTransport(t);

                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

                var s1 = new RobotRaconteurTestServiceSupport2();
                s1.RegisterServices(t);

                ServiceTestClient2 s = new ServiceTestClient2();
                s.RunFullTest("rr+tcp://localhost:22332/?service=RobotRaconteurTestService2")
                    .GetAwaiter().GetResult();

                RobotRaconteurNode.s.Shutdown();

            }
            else
            if (command == "client2")
            {

                var url = args[1];                
                int count = 1;
                if (args.Length >= 3)
                {
                    count = Int32.Parse(args[2]);
                }

                var t = new TcpTransport();

                RobotRaconteurNode.s.RegisterTransport(t);


                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

                for (int i = 0; i < count; i++)
                {
                    ServiceTestClient2 s = new ServiceTestClient2();
                    s.RunFullTest(url).GetAwaiter().GetResult();
                }

                RobotRaconteurNode.s.Shutdown();

                Console.WriteLine("Test complete, no errors detected");
            }            
            else
            {
                throw new Exception("Unknown command");
            }

        }
    }
}
