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
                RobotRaconteurNode.s.SetLogLevelFromEnvVariable();
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
            if (command == "subscribertest")
            {
                RobotRaconteurNode.s.SetLogLevelFromEnvVariable();

                if (args.Length < 2)
                {
                    Console.WriteLine("Usage for subscribertest:  RobotRaconteurTest subscribertest servicetype");
                    return;
                }

                var servicetype = args[1];

                LocalTransport t2 = new LocalTransport();
                t2.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t2);

                TcpTransport t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);

                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var subscription = RobotRaconteurNode.s.SubscribeServiceByType(new string[] { servicetype });

                subscription.ClientConnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                {
                    Console.WriteLine("Client connected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                    testroot e1 = (testroot)e;
                    e1.get_d1().ContinueWith(
                        delegate (Task<double> f)
                        {
                            if (f.IsFaulted)
                            {
                                return;
                            }
                            Console.WriteLine("d1 = " + f.Result);
                        }
                        );
                };

                subscription.ClientDisconnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                {
                    Console.WriteLine("Client disconnected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                };

                var wire_subscription = subscription.SubscribeWire<double>("broadcastwire");
                wire_subscription.WireValueChanged += delegate (WireSubscription<double> c, double d, TimeSpec e) {
                    // Console.WriteLine("Wire value changed: " + d);
                };

                var pipe_subscription = subscription.SubscribePipe<double>("broadcastpipe");
                pipe_subscription.PipePacketReceived += delegate (PipeSubscription<double> c)
                {
                    double val;
                    while (c.TryReceivePacket(out val))
                    {
                        Console.WriteLine("Received pipe packet: " + val);
                    }
                };

                System.Threading.Thread.Sleep(6000);

                var connected_clients = subscription.GetConnectedClients();

                foreach (var c in connected_clients)
                {
                    Console.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
                }

                TimeSpec w1_time = null;
                double w1_value;
                var w1_res = wire_subscription.TryGetInValue(out w1_value);

                if (w1_res)
                {
                    Console.WriteLine("Got broadcastwire value: " + w1_value + " " + w1_time?.seconds);
                }

                Console.WriteLine("Waiting for services...");

                Console.ReadLine();

                RobotRaconteurNode.s.Shutdown();

                return;
            }
            else
            if (command == "subscriberurltest")
            {
                RobotRaconteurNode.s.SetLogLevelFromEnvVariable();

                if (args.Length < 2)
                {
                    Console.WriteLine("Usage for subscriberurltest:  RobotRaconteurTest subscriberurltest url");
                    return;
                }

                var url = args[1];

                LocalTransport t2 = new LocalTransport();
                t2.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t2);

                TcpTransport t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);
                                
                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var subscription = RobotRaconteurNode.s.SubscribeService(new string[]{ url});

                subscription.ClientConnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                {
                    Console.WriteLine("Client connected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                    //testroot e1 = (testroot)e;
                    //Console.WriteLine("d1 = " + e1.get_d1().Result);
                };

                subscription.ClientDisconnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                {
                    Console.WriteLine("Client disconnected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                };

                subscription.ClientConnectFailed +=
                    delegate (ServiceSubscription c, ServiceSubscriptionClientID d, string[] url2, Exception err)
                    {
                        Console.WriteLine("Client connect failed: " + d.NodeID.ToString() + " url: " + String.Join(",", url2) +
                                      err.ToString());
                    };

                var cancel2 = new CancellationTokenSource();
                cancel2.CancelAfter(6000);
                subscription.GetDefaultClientWait<object>(cancel2.Token).ContinueWith(delegate (Task<object> res) {
                    if (res.IsFaulted)
                    {
                        Console.WriteLine("AsyncGetDefaultClient failed");
                    }
                    else if (res.Result == null)
                    {
                        Console.WriteLine("AsyncGetDefaultClient returned null");
                    }
                    else
                    {
                        Console.WriteLine($"AsyncGetDefaultClient successful: {res.Result}");
                    }
                });
                var cancel1 = new CancellationTokenSource();
                cancel1.CancelAfter(6000);
                var client2 = subscription.GetDefaultClientWait<object>(cancel1.Token).GetAwaiter().GetResult();
                object client3;
                var cancel3 = new CancellationTokenSource();
                cancel3.CancelAfter(6000);
                var try_res = subscription.TryGetDefaultClientWait<object>(cancel3.Token).Result;
                
                Console.WriteLine($"try_res = {try_res.Item1}");

                var connected_clients = subscription.GetConnectedClients();

                foreach (var c in connected_clients)
                {
                    Console.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
                }

                try
                {
                    Console.WriteLine(subscription.GetDefaultClient<testroot>().get_d1().Result);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Client not connected");
                }

                object client1;
                subscription.TryGetDefaultClient(out client1);

                Console.WriteLine("Waiting for services...");

                Console.ReadLine();

                RobotRaconteurNode.s.Shutdown();

                return;
            }
            else
            if (command == "subscriberfiltertest")
            {
                RobotRaconteurNode.s.SetLogLevelFromEnvVariable();

                if (args.Length < 2)
                {
                    throw new Exception(
                        "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest servicetype");
                }

                var servicetype = args[1];

                var f = new ServiceSubscriptionFilter();

                if (args.Length >= 3)
                {
                    var subcommand = args[2];

                    if (subcommand == "nodeid")
                    {
                        if (args.Length < 4)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest nodeid <nodeid>");
                        }

                        var n = new ServiceSubscriptionFilterNode();
                        n.NodeID = new NodeID(args[3]);
                        f.Nodes = new ServiceSubscriptionFilterNode[] { n };
                    }

                    else if (subcommand == "nodename")
                    {
                        if (args.Length < 4)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest nodename <nodename>");
                        }

                        var n = new ServiceSubscriptionFilterNode();
                        n.NodeName = args[3];
                        f.Nodes = new ServiceSubscriptionFilterNode[] { n };
                    }
                    else if (subcommand == "nodeidscheme")
                    {
                        if (args.Length < 5)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest nodeidscheme <nodeid> <schemes>");
                        }

                        var n = new ServiceSubscriptionFilterNode();
                        n.NodeID = new NodeID(args[3]);
                        f.Nodes = new ServiceSubscriptionFilterNode[] { n };
                        f.TransportSchemes = args[4].Split(new char[] { ',' });
                    }
                    else if (subcommand == "nodeidauth")
                    {
                        if (args.Length < 6)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest nodeidauth <nodeid> <username> <password>");
                        }

                        var n = new ServiceSubscriptionFilterNode();
                        n.NodeID = new NodeID(args[3]);
                        n.Username = args[4];
                        n.Credentials = new Dictionary<string, object>() { { "password", args[5] } };
                        f.Nodes = new ServiceSubscriptionFilterNode[] { n };
                    }
                    else if (subcommand == "servicename")
                    {
                        if (args.Length < 4)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest servicename <servicename>");
                        }

                        var n = new ServiceSubscriptionFilterNode();
                        f.ServiceNames = new string[] { args[3] };
                    }
                    else if (subcommand == "predicate")
                    {
                        f.Predicate = delegate (ServiceInfo2 info)
                        {
                            Console.WriteLine("Predicate: " + info.NodeName);
                            return info.NodeName == "testprog";
                        };
                    }
                    else
                    {
                        throw new Exception("Unknown subscriberfiltertest command");
                    }

                    LocalTransport t2 = new LocalTransport();
                    t2.EnableNodeDiscoveryListening();
                    RobotRaconteurNode.s.RegisterTransport(t2);

                    TcpTransport t = new TcpTransport();
                    t.EnableNodeDiscoveryListening();
                    RobotRaconteurNode.s.RegisterTransport(t);

                    RobotRaconteurNode.s.RegisterServiceType(
                        new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                    RobotRaconteurNode.s.RegisterServiceType(
                        new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                    var subscription = RobotRaconteurNode.s.SubscribeServiceByType(new string[] { servicetype }, f);

                    subscription.ClientConnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                    {
                        Console.WriteLine("Client connected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                        testroot e1 = (testroot)e;
                        Console.WriteLine("d1 = " + e1.get_d1().Result);
                    };

                    subscription.ClientDisconnected +=
                        delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
                        {
                            Console.WriteLine("Client disconnected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                        };

                    Console.ReadLine();

                    RobotRaconteurNode.s.Shutdown();

                    return;
                }

                return;
            }
            else
            if (command == "serviceinfo2subscribertest")
            {
                RobotRaconteurNode.s.SetLogLevelFromEnvVariable();

                if (args.Length < 2)
                {
                    Console.WriteLine("Usage for subscribertest:  RobotRaconteurTest subscribertest servicetype");
                    return;
                }

                var servicetype = args[1];

                LocalTransport t2 = new LocalTransport();
                t2.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t2);

                TcpTransport t = new TcpTransport();
                t.EnableNodeDiscoveryListening();
                RobotRaconteurNode.s.RegisterTransport(t);

              
                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var subscription = RobotRaconteurNode.s.SubscribeServiceInfo2(new string[] { servicetype });
                subscription.ServiceDetected +=
                    delegate (ServiceInfo2Subscription sub, ServiceSubscriptionClientID id, ServiceInfo2 info)
                    {
                        Console.WriteLine("Service detected: " + info.NodeID.ToString() + ", " + info.Name);
                    };

                subscription.ServiceLost +=
                    delegate (ServiceInfo2Subscription sub, ServiceSubscriptionClientID id, ServiceInfo2 info)
                    {
                        Console.WriteLine("Service lost: " + info.NodeID.ToString() + ", " + info.Name);
                    };

                System.Threading.Thread.Sleep(6000);

                var connected_clients = subscription.GetDetectedServiceInfo2();

                foreach (var c in connected_clients)
                {
                    Console.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
                }

                Console.WriteLine("Waiting for services...");

                Console.ReadLine();

                RobotRaconteurNode.s.Shutdown();

                return;
            }
            else if (command == "intraloopback")
            {
                RobotRaconteurNode.s.NodeName = "intra_testprog";
                var t = new TcpTransport();
                RobotRaconteurNode.s.RegisterTransport(t);

                var t2 = new IntraTransport();
                t2.StartServer();
                RobotRaconteurNode.s.RegisterTransport(t2);

                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var s1 = new RobotRaconteurTestServiceSupport();
                s1.RegisterServices(t);

                var client_node = new RobotRaconteurNode();

               client_node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
               client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var client_t2 = new IntraTransport(client_node);
                client_t2.StartClient();
                client_node.RegisterTransport(client_t2);

                ServiceTestClient s = new ServiceTestClient(client_node);
                s.RunFullTest("rr+intra:///?nodename=intra_testprog&service=RobotRaconteurTestService",
                    "rr+intra:///?nodename=intra_testprog&service=RobotRaconteurTestService_auth")
                    .GetAwaiter().GetResult();
                client_node.Shutdown();
                RobotRaconteurNode.s.Shutdown();

            }
            else if (command == "intraloopback2")
            {
                RobotRaconteurNode.s.NodeName = "intra_testprog";
                var t = new TcpTransport();
                RobotRaconteurNode.s.RegisterTransport(t);

                var t2 = new IntraTransport();
                t2.StartServer();
                RobotRaconteurNode.s.RegisterTransport(t2);

                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

                var s1 = new RobotRaconteurTestServiceSupport2();
                s1.RegisterServices(t);

                var client_node = new RobotRaconteurNode();

                client_node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
                client_node.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());

                var client_t2 = new IntraTransport(client_node);
                client_t2.StartClient();
                client_node.RegisterTransport(client_t2);

                ServiceTestClient2 s = new ServiceTestClient2(client_node);
                s.RunFullTest("rr+intra:///?nodename=intra_testprog&service=RobotRaconteurTestService2")
                    .GetAwaiter().GetResult();

                RobotRaconteurNode.s.Shutdown();

            }
            else if (command == "localloopback")
            {
                var t2 = new LocalTransport();
                t2.StartServerAsNodeName("local_testprog");
                RobotRaconteurNode.s.RegisterTransport(t2);
                
                var t = new TcpTransport();
                RobotRaconteurNode.s.RegisterTransport(t);


                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var s1 = new RobotRaconteurTestServiceSupport();
                s1.RegisterServices(t);

                var client_node = new RobotRaconteurNode();

                client_node.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
                client_node.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

                var client_t2 = new LocalTransport(client_node);
                client_node.RegisterTransport(client_t2);

                ServiceTestClient s = new ServiceTestClient(client_node);
                s.RunFullTest("rr+local:///?nodename=local_testprog&service=RobotRaconteurTestService",
                    "rr+local:///?nodename=local_testprog&service=RobotRaconteurTestService_auth")
                    .GetAwaiter().GetResult();
                client_node.Shutdown();
                RobotRaconteurNode.s.Shutdown();

            }
            else
            {
                throw new Exception("Unknown command");
            }

        }
    }
}
