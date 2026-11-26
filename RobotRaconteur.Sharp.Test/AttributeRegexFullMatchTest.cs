using System;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSubTest
{
    public static class AttributeRegexFullMatchTest
    {
        public static void TestAttributeRegexFullMatch()
        {
            // Attribute regexes must match the whole value, as Core's regex_match does
            var attr1 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeRegex("abc");
            RRAssert.IsTrue(attr1.IsMatch("abc"));
            RRAssert.IsFalse(attr1.IsMatch("xabcx"));
            RRAssert.IsFalse(attr1.IsMatch("abcd"));

            var attr2 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeRegex("a|ab");
            RRAssert.IsTrue(attr2.IsMatch("ab"));
            RRAssert.IsTrue(attr2.IsMatch("a"));

            var attr3 = ServiceSubscriptionFilterAttributeFactory.CreateServiceSubscriptionFilterAttributeRegex("name1", ".*_val1");
            RRAssert.IsTrue(attr3.IsMatch("name1", "my_val1"));
            RRAssert.IsFalse(attr3.IsMatch("name1", "my_val12"));
            Console.WriteLine("AttributeRegexFullMatchTest.TestAttributeRegexFullMatch passed");
        }
    }
}
