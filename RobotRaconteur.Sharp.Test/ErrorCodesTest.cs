using System;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    static class ErrorCodesTest
    {
        static void CheckRoundTrip<T>(Exception e, MessageErrorType code, string name) where T : Exception
        {
            var entry = new MessageEntry(MessageEntryType.FunctionCallRes, "f");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(e, entry);
            RRAssert.AreEqual(entry.Error, code);
            RRAssert.AreEqual(entry.FindElement("errorname").CastData<string>(), name);
            var e2 = RobotRaconteurExceptionUtil.MessageEntryToException(entry);
            RRAssert.IsTrue(e2 is T);
            RRAssert.AreEqual(e2.Message, "test message");
        }

        static public void TestCoreErrorCodes()
        {
            // Error codes defined by Core must have the same values and map to exceptions
            RRAssert.AreEqual((int)MessageErrorType.InvalidConfiguration, 31);
            RRAssert.AreEqual((int)MessageErrorType.InvalidState, 32);
            RRAssert.AreEqual((int)MessageErrorType.OperationTimeout, 110);
            RRAssert.AreEqual((int)MessageErrorType.OperationCancelled, 111);
            RRAssert.AreEqual((int)MessageErrorType.AuthenticationError, 150);

            CheckRoundTrip<InvalidConfigurationException>(new InvalidConfigurationException("test message"), MessageErrorType.InvalidConfiguration, "RobotRaconteur.InvalidConfiguration");
            CheckRoundTrip<InvalidStateException>(new InvalidStateException("test message"), MessageErrorType.InvalidState, "RobotRaconteur.InvalidState");
            CheckRoundTrip<OperationTimeoutException>(new OperationTimeoutException("test message"), MessageErrorType.OperationTimeout, "RobotRaconteur.OperationTimeout");
            CheckRoundTrip<OperationCancelledException>(new OperationCancelledException("test message"), MessageErrorType.OperationCancelled, "RobotRaconteur.OperationCancelled");
            Console.WriteLine("ErrorCodesTest.TestCoreErrorCodes passed");
        }
    }
}
