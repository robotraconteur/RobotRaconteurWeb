using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RobotRaconteurWeb;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace RobotRaconteurTest
{
    static class RRAssert
    {
        public static void AreEqual<T>(T a, T b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
            where T : IComparable, IComparable<T>
        {
            if (a.CompareTo(b) != 0)
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreEqual(string a, string b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (a!=b)
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreEqual(NodeID a, NodeID b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (a != b)
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreEqual(CDouble a, CDouble b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (a != b)
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreEqual(CSingle a, CSingle b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Math.Abs(a.Real - b.Real) >1e-2 || Math.Abs(a.Imag - b.Imag) >1e-2)
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreEqual(object a, object b, [CallerFilePath] string sourceFilePath = "",
                                    [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!object.Equals(a, b))
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void AreNotEqual(object a, object b, [CallerFilePath] string sourceFilePath = "",
                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (object.Equals(a, b))
            {
                RRWebTest.WriteLine("Failure: {0} equals {1} at {2}:{3}", a, b, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void Fail([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            RRWebTest.WriteLine("Failure: at {0}:{1}", sourceFilePath, sourceLineNumber);
            throw new Exception("Unit test failure");
        }

        public static void IsTrue(bool val, [CallerFilePath] string sourceFilePath = "",
                                  [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!val)
            {
                RRWebTest.WriteLine("Failure: {0} is not true {1}:{2}", val, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void IsFalse(bool val, [CallerFilePath] string sourceFilePath = "",
                                   [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (val)
            {
                RRWebTest.WriteLine("Failure: {0} is not false {1}:{2}", val, sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static void ThrowsException<T>(Action f, [CallerFilePath] string sourceFilePath = "",
                                              [CallerLineNumber] int sourceLineNumber = 0)
            where T : Exception
        {
            bool thrown = false;
            try
            {
                f();
            }
            catch (T)
            {
                thrown = true;
            }
            if (!thrown)
            {
                RRWebTest.WriteLine("Failure: does not throw at {0}:{1}", sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }

        public static async Task ThrowsExceptionAsync<T>(Func<Task> f, [CallerFilePath] string sourceFilePath = "",
                                                         [CallerLineNumber] int sourceLineNumber = 0)
            where T : Exception
        {
            bool thrown = false;
            try
            {
                await f();
            }
            catch (T)
            {
                thrown = true;
            }
            if (!thrown)
            {
                RRWebTest.WriteLine("Failure: does not throw at {0}:{1}", sourceFilePath, sourceLineNumber);
                throw new Exception("Unit test failure");
            }
        }
    }
}