using System;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSubTest
{
    public static class IdentifierFullMatchTest
    {
        public static void TestIdentifierFullMatch()
        {
            // Identifier filters must match the whole identifier, as Core's regex_match does
            const string uuid = "cbfea7a4-0361-4aad-95bb-d4dcd967047a";
            var attr1 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeIdentifier("arm", "");
            RRAssert.IsTrue(attr1.IsMatch("arm"));
            RRAssert.IsTrue(attr1.IsMatch("arm|" + uuid));
            RRAssert.IsFalse(attr1.IsMatch("big_arm"));
            RRAssert.IsFalse(attr1.IsMatch("big_arm|" + uuid));
            RRAssert.IsFalse(attr1.IsMatch("arm2"));
            RRAssert.IsFalse(attr1.IsMatch("arm|" + uuid + "ff"));

            var attr2 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeIdentifier("", uuid);
            RRAssert.IsTrue(attr2.IsMatch("arm|" + uuid));
            RRAssert.IsFalse(attr2.IsMatch("arm|" + uuid + "00"));
            RRAssert.IsFalse(attr2.IsMatch("arm|00" + uuid));

            var attr3 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeCombinedIdentifier("arm|" + uuid);
            RRAssert.IsTrue(attr3.IsMatch("arm|" + uuid));
            RRAssert.IsFalse(attr3.IsMatch("big_arm|" + uuid));

            // Names and UUIDs that only contain a valid part are rejected
            RRAssert.ThrowsException<ArgumentException>(() =>
                ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeIdentifier("arm name", ""));
            RRAssert.ThrowsException<ArgumentException>(() =>
                ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeIdentifier("arm", uuid + "x"));
            Console.WriteLine("IdentifierFullMatchTest.TestIdentifierFullMatch passed");
        }
    }
}
