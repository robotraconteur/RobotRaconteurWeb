using System;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    // Regression test: values of the standard and user-added command line options must be stored
    // and returned by the getters
    public static class CommandLineConfigParserTests
    {
        public static void RunTestCommandLineOptions()
        {
            var c = new CommandLineConfigParser(RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride);
            c.SetDefaults("default_name", 1111, RobotRaconteurNodeSetupFlags.ServerDefault);
            c.AddStringOption("my-string", "test string option");
            c.AddIntOption("my-int", "test int option");
            c.AddBoolOption("my-bool", "test bool option");

            c.ParseCommandLine(new string[] { "--robotraconteur-nodename=from_cmdline", "--robotraconteur-tcp-port=2222",
                "--robotraconteur-tcp-enable=false", "--robotraconteur-local-enable=0", "--robotraconteur-my-string=hello",
                "--robotraconteur-my-int=42", "--robotraconteur-my-bool" });

            RRAssert.AreEqual(c.GetOptionOrDefaultAsString("nodename"), "from_cmdline");
            RRAssert.AreEqual(c.GetOptionOrDefaultAsInt("tcp-port"), 2222);
            RRAssert.IsFalse(c.GetOptionOrDefaultAsBool("tcp-enable"));
            RRAssert.IsFalse(c.GetOptionOrDefaultAsBool("local-enable"));
            RRAssert.AreEqual(c.GetOptionOrDefaultAsString("my-string", "default"), "hello");
            RRAssert.AreEqual(c.GetOptionOrDefaultAsInt("my-int", 0), 42);
            RRAssert.IsTrue(c.GetOptionOrDefaultAsBool("my-bool", false));

            // Defaults when not given on the command line
            var c2 = new CommandLineConfigParser(RobotRaconteurNodeSetupFlags.ServerDefaultAllowedOverride);
            c2.SetDefaults("default_name", 1111, RobotRaconteurNodeSetupFlags.ServerDefault);
            c2.ParseCommandLine(new string[0]);
            RRAssert.AreEqual(c2.GetOptionOrDefaultAsString("nodename"), "default_name");
            RRAssert.AreEqual(c2.GetOptionOrDefaultAsInt("tcp-port"), 1111);
            RRAssert.IsTrue(c2.GetOptionOrDefaultAsBool("tcp-enable"));
        }
    }
}
