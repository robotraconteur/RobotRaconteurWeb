using System;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: TimeSpec ordering operators
    public static class TimeSpecCompareTests
    {
        public static void RunTestTimeSpecCompare()
        {
            var a = new TimeSpec(10, 500);
            var a2 = new TimeSpec(10, 500);
            var b = new TimeSpec(10, 600);
            var c = new TimeSpec(11, 100);

            RRAssert.IsFalse(a < a2);
            RRAssert.IsTrue(a <= a2);
            RRAssert.IsFalse(a > a2);
            RRAssert.IsTrue(a >= a2);

            RRAssert.IsTrue(a < b);
            RRAssert.IsTrue(a <= b);
            RRAssert.IsFalse(b < a);
            RRAssert.IsFalse(b <= a);
            RRAssert.IsTrue(b > a);
            RRAssert.IsTrue(b >= a);

            RRAssert.IsTrue(b < c);
            RRAssert.IsTrue(b <= c);
            RRAssert.IsFalse(c < b);
            RRAssert.IsFalse(c <= b);
        }
    }
}
