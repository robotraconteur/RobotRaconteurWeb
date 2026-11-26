using System;
using System.IO;
using System.Runtime.InteropServices;
using RobotRaconteurSharp;
using RobotRaconteurTest;

namespace RobotRaconteurSharp.Test
{
    // Regression test: when the NodeID name file is too large, the new NodeID must replace its
    // contents (the file used to keep its length, so a new NodeID was generated on every start)
    public static class NodeDirectoriesNodeIdStoreTests
    {
        [DllImport("libc")]
        static extern uint geteuid();

        public static void RunTestOversizedNameFileReplaced()
        {
            // For root an existing name file in the user config dir is opened read-only (system
            // configuration), so this case only applies to other users
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || geteuid() == 0)
            {
                return;
            }

            string name = "nodedir_oversized_name_test";
            var node = new RobotRaconteurNode();
            try
            {
                var t = new LocalTransport(node);
                if (!t.IsLocalTransportSupported)
                {
                    return;
                }

                string dir = Path.Combine(node.NodeDirectories.user_config_dir, "nodeids");
                Directory.CreateDirectory(dir);
                string fname = Path.Combine(dir, name);
                File.WriteAllText(fname, new string('x', 17 * 1024));

                t.StartServerAsNodeName(name);
                node.RegisterTransport(t);

                string contents;
                using (var f = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var r = new StreamReader(f))
                {
                    contents = r.ReadToEnd();
                }
                RRAssert.IsTrue(contents.Length < 16 * 1024);
                RRAssert.AreEqual(new NodeID(contents.Trim()), node.NodeID);
            }
            finally
            {
                node.Shutdown();
            }
        }
    }
}
