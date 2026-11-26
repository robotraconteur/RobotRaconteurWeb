using System;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // Regression test: WallRate.Sleep must return when the loop is late (a remaining time between
    // -1 and -2 ms used to become Task.Delay(-1), which waits forever)
    public static class WallRateSleepTests
    {
        class LateWallRate : WallRate
        {
            public LateWallRate(double frequency, RobotRaconteurNode node) : base(frequency, node) { }

            public void SetLate(double late_ms)
            {
                last_time = node.UtcNow - TimeSpan.FromMilliseconds(period + late_ms);
            }
        }

        public static async Task RunTestWallRateLateSleep()
        {
            var node = new RobotRaconteurNode();
            try
            {
                var rate = new LateWallRate(10, node);
                // Warm up (JIT) with a large delay first, so the following calls start within
                // microseconds of SetLate
                rate.SetLate(100);
                await rate.Sleep();
                foreach (var late in new double[] { 1.1, 1.3, 1.5, 1.7, 1.9 })
                {
                    rate.SetLate(late);
                    var sleep = rate.Sleep();
                    var done = await Task.WhenAny(sleep, Task.Delay(2000));
                    RRAssert.IsTrue(done == sleep);
                }
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
