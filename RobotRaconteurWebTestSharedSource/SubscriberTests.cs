using System;
using System.Collections.Generic;
using System.Text;
using RobotRaconteurWeb;
using com.robotraconteur.testing.TestService1;
using System.Threading.Tasks;
using System.Linq;

namespace RobotRaconteurTest
{
    public class SubscriberTestProgram : IDisposable
    {
        TestNodeConfig config = null;
        ServiceSubscription subscription;

        public void StartSubscriberTestProgram(string[] servicetype)
        {
            config = new TestNodeConfig("", true, true, true, false);

            subscription = config.client_node.SubscribeServiceByType(servicetype);

            StartSubscriberTestProgram2();
        }        

        public void StartSubscriberUrlTestProgram(string[] url)
        {
            config = new TestNodeConfig("", true, true, true, false);

            subscription = config.client_node.SubscribeService(url);

            StartSubscriberTestProgram2();
        }

        public void StartSubscriberFilterTestProgram(string[] args)
        {
            if (args.Length < 2)
            {
                throw new Exception(
                    "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest servicetype");
            }

            var servicetype = args[1];
            config = new TestNodeConfig("", true, true, true, false);

            var f = LoadFilter(args);

            subscription = config.client_node.SubscribeServiceByType(new string[] { servicetype }, f);

            StartSubscriberTestProgram2();
        }

        public static ServiceSubscriptionFilter LoadFilter(string[] args)
        {
            var f = new ServiceSubscriptionFilter();
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
                    RRWebTest.WriteLine("Predicate: " + info.NodeName);
                    return info.NodeName == "testprog";
                };
            }
            else
            {
                throw new Exception("Unknown subscriberfiltertest command");
            }
            return f;
        }

        public async void StartSubscriberTestProgram2()
        {           

            subscription.ClientConnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
            {
                RRWebTest.WriteLine("Client connected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                testroot e1 = (testroot)e;
                e1.get_d1().ContinueWith(
                    delegate (Task<double> f)
                    {
                        if (f.IsFaulted)
                        {
                            return;
                        }
                        RRWebTest.WriteLine("d1 = " + f.Result);
                    }
                    );
            };

            subscription.ClientDisconnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
            {
                RRWebTest.WriteLine("Client disconnected: " + d.NodeID.ToString() + ", " + d.ServiceName);
            };

            var wire_subscription = subscription.SubscribeWire<double>("broadcastwire");
            wire_subscription.WireValueChanged += delegate (WireSubscription<double> c, double d, TimeSpec e)
            {
                // RRWebTest.WriteLine("Wire value changed: " + d);
            };

            var pipe_subscription = subscription.SubscribePipe<double>("broadcastpipe");
            pipe_subscription.PipePacketReceived += delegate (PipeSubscription<double> c)
            {
                double val;
                while (c.TryReceivePacket(out val))
                {
                    RRWebTest.WriteLine("Received pipe packet: " + val);
                }
            };

            await Task.Delay(6000);

            var connected_clients = subscription.GetConnectedClients();

            foreach (var c in connected_clients)
            {
                RRWebTest.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
            }

            TimeSpec w1_time = null;
            double w1_value;
            var w1_res = wire_subscription.TryGetInValue(out w1_value);

            if (w1_res)
            {
                RRWebTest.WriteLine("Got broadcastwire value: " + w1_value + " " + w1_time?.seconds);
            }
        }

        public void Dispose()
        {
            config?.Dispose();
        }
        public static async Task RunSubscriberTestProgram(string[] servicetype)
        {
            using (var test = new SubscriberTestProgram())
            {
                test.StartSubscriberTestProgram(servicetype);

                RRWebTest.WriteLine("Waiting for services...");

                await Task.Run(() => Console.ReadLine());
            }
        }

        public static async Task RunSubscriberUrlTestProgram(string[] url)
        {
            using (var test = new SubscriberTestProgram())
            {
                test.StartSubscriberTestProgram(url);

                RRWebTest.WriteLine("Waiting for services...");

                await Task.Run(() => Console.ReadLine());
            }
        }

        public static async Task RunSubscriberFilterTestProgram(string[] args)
        {
            using (var test = new SubscriberTestProgram())
            {
                test.StartSubscriberFilterTestProgram(args);

                RRWebTest.WriteLine("Waiting for services...");

                await Task.Run(() => Console.ReadLine());
            }
        }

        public static async Task RunSubscriberTest(string[] args)
        {
            switch (args[0])
            {
                case "subscribertest":
                    {
                        if (args.Length < 2)
                        {
                            throw new ArgumentException("Usage for subscribertest:  RobotRaconteurTest subscribertest servicetype");
                        }

                        await RunSubscriberTestProgram(new string[] { args[1] });
                        break;
                    }
                case "subscriberurltest":
                    {
                        if (args.Length < 2)
                        {
                            throw new ArgumentException("Usage for subscriberurltest:  RobotRaconteurTest subscriberurltest url");                            
                        }

                        await RunSubscriberUrlTestProgram(new string[] { args[1] });
                        break;
                    }
                case "subscriberfiltertest":
                    {
                        if (args.Length < 2)
                        {
                            throw new Exception(
                                "Usage for subscriberfiltertest:  RobotRaconteurTest subscriberfiltertest servicetype");
                        }

                        await RunSubscriberFilterTestProgram(args);
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid test command");
            }
        }
    }

    public class ServiceInfo2SubscriberTestProgram : IDisposable
    {
        TestNodeConfig config = null;
        ServiceInfo2Subscription subscription;

        public void StartServiceInfo2SubscriberTestProgram(string[] servicetype)
        {
            config = new TestNodeConfig("", true, true, true, false);

            subscription = config.client_node.SubscribeServiceInfo2(servicetype);

            StartServiceInfo2SubscriberTestProgram2();
        }

        public async void StartServiceInfo2SubscriberTestProgram2()
        {

            subscription.ServiceDetected +=
                    delegate (ServiceInfo2Subscription sub, ServiceSubscriptionClientID id, ServiceInfo2 info)
                    {
                        RRWebTest.WriteLine("Service detected: " + info.NodeID.ToString() + ", " + info.Name);
                    };

            subscription.ServiceLost +=
                delegate (ServiceInfo2Subscription sub, ServiceSubscriptionClientID id, ServiceInfo2 info)
                {
                    RRWebTest.WriteLine("Service lost: " + info.NodeID.ToString() + ", " + info.Name);
                };

            await Task.Delay(6000);

            var connected_clients = subscription.GetDetectedServiceInfo2();

            foreach (var c in connected_clients)
            {
                RRWebTest.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
            }
        }

        public static async Task RunServiceInfo2SubscriberTest(string[] servicetype)
        {
            using (var test = new ServiceInfo2SubscriberTestProgram())
            {
                test.StartServiceInfo2SubscriberTestProgram(servicetype);

                RRWebTest.WriteLine("Waiting for services...");

                await Task.Run(() => Console.ReadLine());
            }
        }

        public void Dispose()
        {
            config?.Dispose();
        }
    }

}
