using System;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    public static class ErrorMappingTests
    {
        // Regression test: ArgumentOutOfRangeException must map to MessageErrorType.OutOfRange,
        // not to InvalidArgument (ArgumentOutOfRangeException derives from ArgumentException)
        public static void TestArgumentOutOfRangeMapping()
        {
            var entry = new MessageEntry(MessageEntryType.FunctionCallRes, "test");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(new ArgumentOutOfRangeException("p", "out of range"), entry);
            RRAssert.AreEqual(entry.Error, MessageErrorType.OutOfRange);

            var entry2 = new MessageEntry(MessageEntryType.FunctionCallRes, "test");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(new ArgumentException("bad"), entry2);
            RRAssert.AreEqual(entry2.Error, MessageErrorType.InvalidArgument);
        }
    }
}
