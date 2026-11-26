using System;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    static class ServicePathTest
    {
        static void CheckRoundTrip(string index)
        {
            var segments = new ServicePathSegment[] { new ServicePathSegment() { name = "root" },
                                                      new ServicePathSegment() { name = "obj", index = index } };
            string path = ServicePathUtil.BuildServicePath(segments);
            var segments2 = ServicePathUtil.ParseServicePathIndex(path);
            RRAssert.AreEqual(segments2.Length, 2);
            RRAssert.AreEqual(segments2[1].name, "obj");
            RRAssert.AreEqual(segments2[1].index, index);
        }

        public static void RunServicePathTest()
        {
            // Encoded indexes must only contain characters accepted by the service path segment regex
            string encoded = ServicePathUtil.EncodeServicePathIndex("a-b~c.d_e");
            RRAssert.IsFalse(encoded.Contains("-"));
            RRAssert.IsFalse(encoded.Contains("~"));
            RRAssert.IsFalse(encoded.Contains("."));

            CheckRoundTrip("alpha");
            CheckRoundTrip("cbfea7a4-0361-4aad-95bb-d4dcd967047a");
            CheckRoundTrip("a-b~c.d_e");
            CheckRoundTrip("café");
            Console.WriteLine("ServicePathTest passed");
        }
    }
}
