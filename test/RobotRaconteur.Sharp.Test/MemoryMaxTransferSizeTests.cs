using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.robotraconteur.testing.TestService1;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    // A service MaxTransferSize smaller than the element size must give an error, not a division by zero
    public static class MemoryMaxTransferSizeTests
    {
        public static async Task RunTestMaxTransferSizeTooSmall()
        {
            using (var nodes = new TestNodeConfig("testprog", false, false, true))
            {
                nodes.node.MemoryMaxTransferSize = 4;
                var r = (testroot)await nodes.client_node.ConnectService(nodes.GetServiceUrl("RobotRaconteurTestService"));

                await RRAssert.ThrowsExceptionAsync<InvalidOperationException>(async () => await r.m1.Read(0, new double[10], 0, 10));
                await RRAssert.ThrowsExceptionAsync<InvalidOperationException>(async () => await r.m1.Write(0, new double[10], 0, 10));
                await RRAssert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                    await r.m2.Read(new ulong[] { 0, 0 }, new MultiDimArray(new uint[] { 2, 2 }, new double[4]), new ulong[] { 0, 0 }, new ulong[] { 2, 2 }));
                await RRAssert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                    await r.m2.Write(new ulong[] { 0, 0 }, new MultiDimArray(new uint[] { 2, 2 }, new double[4]), new ulong[] { 0, 0 }, new ulong[] { 2, 2 }));

                await nodes.client_node.DisconnectService(r);
            }
        }
    }
}
