using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;
using static H5.Core.dom;

namespace RobotRaconteurH5Test
{
    [RobotRaconteurWeb.RobotRaconteurServiceObjectInterface("blah")]
    public static class RegisterServiceTypes
    {
        public static void DoRegisterServiceTypes()
        {
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());
            RobotRaconteurNode.s.RegisterServiceType(new com.robotraconteur.testing.TestService3.com__robotraconteur__testing__TestService3Factory());
        }

        public static void SetWriteLine()
        {
            RRWebTest.WriteLineFunc = delegate(string format, object[] args)
            {
                var log_elem = document.getElementById("log");
                log_elem.innerHTML += string.Format(format,args) + "<br>";
            };
        }

        public static async Task SubscriberTest()
        {
            string url = "rr+tcp://localhost:22222/?service=RobotRaconteurTestService";
            RobotRaconteurNode.s.RegisterServiceType(
                    new com.robotraconteur.testing.TestService1.com__robotraconteur__testing__TestService1Factory());
            RobotRaconteurNode.s.RegisterServiceType(
                new com.robotraconteur.testing.TestService2.com__robotraconteur__testing__TestService2Factory());

            var subscription = RobotRaconteurNode.s.SubscribeService(new string[] { url });

            subscription.ClientConnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
            {
                RRWebTest.WriteLine("Client connected: " + d.NodeID.ToString() + ", " + d.ServiceName);
                //testroot e1 = (testroot)e;
                //Console.WriteLine("d1 = " + e1.get_d1().Result);
            };

            subscription.ClientDisconnected += delegate (ServiceSubscription c, ServiceSubscriptionClientID d, object e)
            {
                RRWebTest.WriteLine("Client disconnected: " + d.NodeID.ToString() + ", " + d.ServiceName);
            };

            subscription.ClientConnectFailed +=
                delegate (ServiceSubscription c, ServiceSubscriptionClientID d, string[] url2, Exception err)
                {
                    RRWebTest.WriteLine("Client connect failed: " + d.NodeID.ToString() + " url: " + String.Join(",", url2) +
                                  err.ToString());
                };

            var cancel2 = new CancellationTokenSource();
            cancel2.CancelAfter(6000);
            await subscription.GetDefaultClientWait<object>(cancel2.Token);
               
            var cancel1 = new CancellationTokenSource();
            cancel1.CancelAfter(6000);
            var client2 = await subscription.GetDefaultClientWait<object>(cancel1.Token);
            object client3;
            var cancel3 = new CancellationTokenSource();
            cancel3.CancelAfter(6000);
            var try_res = await subscription.TryGetDefaultClientWait<object>(cancel3.Token);

            RRWebTest.WriteLine($"try_res = {try_res.Item1}");

            var connected_clients = subscription.GetConnectedClients();

            foreach (var c in connected_clients)
            {
                RRWebTest.WriteLine("Client: " + c.Key.NodeID + ", " + c.Key.ServiceName);
            }

            try
            {
                RRWebTest.WriteLine("{0}", await subscription.GetDefaultClient<testroot>().get_d1());
            }
            catch (Exception e)
            {
                RRWebTest.WriteLine("{0}", e);
                RRWebTest.WriteLine("Client not connected");
            }

            object client1;
            subscription.TryGetDefaultClient(out client1);
        }
    }
}
