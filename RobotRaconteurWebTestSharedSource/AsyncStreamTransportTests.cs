#if !ROBOTRACONTEUR_H5
using System;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    public class AsyncStreamTransportTests
    {
        // Regression test: bursts of wire packets for the same member are coalesced in the
        // AsyncStreamTransport send queue while a send is in progress. The queue must stay
        // consistent so later messages are still delivered (the coalescing previously queued the
        // replacing message twice, which stalled all further sends on the connection).
        public static async Task TestWirePacketBurstDoesNotStall()
        {
            using (var nodes = new TestNodeConfig("testprog", true, false, false))
            {
                var url = nodes.GetServiceUrl("RobotRaconteurTestService", "rr+tcp");
                var cancel = new CancellationTokenSource(15000);
                var r = (testroot)await nodes.client_node.ConnectService(url, cancel: cancel.Token);
                try
                {
                    var w = await r.w1.Connect(cancel.Token);

                    // Large packets so socket writes do not complete immediately and later packets
                    // are queued (and coalesced) behind them
                    for (int i = 0; i < 30; i++)
                    {
                        var a = new double[200000];
                        a[0] = i;
                        w.OutValue = a;
                    }

                    // A request after the burst must still be sent and answered
                    var t = r.get_d1(cancel.Token);
                    RRAssert.IsTrue(await Task.WhenAny(t, Task.Delay(15000)) == t);
                    await t;
                }
                finally
                {
                    await nodes.client_node.DisconnectService(r);
                }
            }
        }
    }
}
#endif
