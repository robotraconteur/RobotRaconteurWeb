#if !ROBOTRACONTEUR_H5
using System;
using System.Runtime.InteropServices;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: ROBOTRACONTEUR_SYSTEM_PREFIX must be prepended to the default system directories
    public static class NodeDirectoriesPrefixTests
    {
        public static void RunTestSystemPrefix()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            string[] vars = { "ROBOTRACONTEUR_SYSTEM_PREFIX", "ROBOTRACONTEUR_SYSTEM_DATA_DIR", "ROBOTRACONTEUR_SYSTEM_CONFIG_DIR",
                "ROBOTRACONTEUR_SYSTEM_STATE_DIR", "ROBOTRACONTEUR_SYSTEM_CACHE_DIR", "ROBOTRACONTEUR_SYSTEM_RUN_DIR" };
            var old = new string[vars.Length];
            for (int i = 0; i < vars.Length; i++)
            {
                old[i] = Environment.GetEnvironmentVariable(vars[i]);
                Environment.SetEnvironmentVariable(vars[i], null);
            }

            var node = new RobotRaconteurNode();
            try
            {
                Environment.SetEnvironmentVariable("ROBOTRACONTEUR_SYSTEM_PREFIX", "/opt/rrprefix");
                var d = node.NodeDirectories;
                RRAssert.AreEqual(d.system_data_dir, "/opt/rrprefix/usr/local/share/robotraconteur");
                RRAssert.AreEqual(d.system_config_dir, "/opt/rrprefix/etc/robotraconteur");
                RRAssert.AreEqual(d.system_state_dir, "/opt/rrprefix/var/lib/robotraconteur");
                RRAssert.AreEqual(d.system_cache_dir, "/opt/rrprefix/var/cache/robotraconteur");
                RRAssert.AreEqual(d.system_run_dir, "/opt/rrprefix/var/run/robotraconteur");
            }
            finally
            {
                for (int i = 0; i < vars.Length; i++)
                {
                    Environment.SetEnvironmentVariable(vars[i], old[i]);
                }
                node.Shutdown();
            }
        }
    }
}
#endif
