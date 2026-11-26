using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    static class TimerTest
    {
        public static async Task TestWallRateDefaultNode()
        {
            // WallRate with the default (null) node argument must use RobotRaconteurNode.s
            WallRate rate = null;
            try
            {
                rate = new WallRate(1000.0);
            }
            catch (NullReferenceException)
            {
                RRAssert.Fail();
            }
            await rate.Sleep();
            Console.WriteLine("TimerTest.TestWallRateDefaultNode passed");
        }
    }
}
