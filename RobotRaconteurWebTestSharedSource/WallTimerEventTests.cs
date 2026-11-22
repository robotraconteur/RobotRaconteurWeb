using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: a running periodic WallTimer must report stopped == false, and last_real must
    // be the previous callback's current_real
    public static class WallTimerEventTests
    {
        public static async Task RunTestWallTimerEvents()
        {
            var node = new RobotRaconteurNode();
            var events = new List<TimerEvent>();
            try
            {
                var timer = new WallTimer(20, ev => { lock (events) { events.Add(ev); } }, false, node);
                timer.Start();
                for (int i = 0; i < 100; i++)
                {
                    lock (events)
                    {
                        if (events.Count >= 4) break;
                    }
                    await Task.Delay(50);
                }
                timer.Stop();

                TimerEvent[] ev1;
                lock (events)
                {
                    ev1 = events.ToArray();
                }
                RRAssert.IsTrue(ev1.Length >= 4);
                for (int i = 0; i < 4; i++)
                {
                    RRAssert.IsFalse(ev1[i].stopped);
                    if (i > 0)
                    {
                        RRAssert.AreEqual(ev1[i].last_real, ev1[i - 1].current_real);
                        RRAssert.AreEqual(ev1[i].last_expected, ev1[i - 1].current_expected);
                    }
                }
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
