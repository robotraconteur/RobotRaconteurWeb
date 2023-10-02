using System;

namespace RobotRaconteurTest
{
    public class RRWebTest
    {
        static public Action<string, object[]> WriteLineFunc;

        static public void WriteLine(string format, params object[] args)
        {
            if (WriteLineFunc != null)
            {
                WriteLineFunc(format, args);
            }
            else
            {
                Console.WriteLine(format, args);
            }
        }
    }
}