using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurWeb;

namespace RobotRaconteurTest
{
    static class RRAssert
    {
        // Hides object.Equals(object, object), which tests called as if it were an assertion (it only returns a
        // bool, so nothing was checked). Compares arrays element by element and numbers by value across types.
        public static new void Equals(object a, object b)
        {
            if (!ValuesEqual(a, b))
            {
                RRWebTest.WriteLine("Failure: {0} does not equal {1}", a, b);
                throw new Exception("Unit test failure: " + a + " does not equal " + b);
            }
        }

        static bool ValuesEqual(object a, object b)
        {
            if (object.Equals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            var aa = a as Array;
            var ba = b as Array;
            if (aa != null && ba != null)
            {
                if (aa.Length != ba.Length)
                    return false;
                var ea = aa.GetEnumerator();
                var eb = ba.GetEnumerator();
                while (ea.MoveNext() && eb.MoveNext())
                {
                    if (!ValuesEqual(ea.Current, eb.Current))
                        return false;
                }
                return true;
            }
            if (IsNumber(a) && IsNumber(b))
            {
                if (a is double || a is float || b is double || b is float)
                {
                    double da = Convert.ToDouble(a);
                    double db = Convert.ToDouble(b);
                    return da == db || (double.IsNaN(da) && double.IsNaN(db));
                }
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);
            }
            return false;
        }

        static bool IsNumber(object o)
        {
            return o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint || o is long ||
                   o is ulong || o is float || o is double;
        }

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
            if (a != b)
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
            if (Math.Abs(a.Real - b.Real) > 1e-2 || Math.Abs(a.Imag - b.Imag) > 1e-2)
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
