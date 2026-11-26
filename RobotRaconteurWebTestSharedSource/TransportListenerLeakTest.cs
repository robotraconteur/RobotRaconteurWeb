using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class TransportListenerLeakTest
    {
        static int ListenerCount(Transport t)
        {
            var d = (Delegate)typeof(Transport).GetField("TransportListeners", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(t);
            return d == null ? 0 : d.GetInvocationList().Length;
        }

        static public async Task TestTransportListenerRemoved()
        {
            // Each connect added a transport listener holding the ClientContext that was never removed
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                var t = nodes.client_intra_transport;
                int count0 = ListenerCount(t);
                for (int i = 0; i < 5; i++)
                {
                    var c = await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));
                    await nodes.client_node.DisconnectService(c);
                }

                // Transport connections close asynchronously: bounded wait for the listeners to go away
                var sw = Stopwatch.StartNew();
                while (ListenerCount(t) > count0 && sw.ElapsedMilliseconds < 10000)
                {
                    await Task.Delay(10);
                }
                RRAssert.AreEqual(ListenerCount(t), count0);
            }
            Console.WriteLine("TransportListenerLeakTest.TestTransportListenerRemoved passed");
        }
    }
}
