using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RobotRaconteurTest;
using RobotRaconteurSharp;

namespace RobotRaconteurSharp.Test
{
    // The /tmp/robotraconteur-run-<uid> fallback user run dir must be private to the current user
    public static class TmpUserRunDirTests
    {
        static NodeDirectories TempNodeDirs(string b, string user_run_dir)
        {
            var d = new NodeDirectories();
            d.system_data_dir = Path.Combine(b, "sys_data");
            d.system_config_dir = Path.Combine(b, "sys_config");
            d.system_state_dir = Path.Combine(b, "sys_state");
            d.system_cache_dir = Path.Combine(b, "sys_cache");
            d.system_run_dir = Path.Combine(b, "sys_run");
            d.user_data_dir = Path.Combine(b, "user_data");
            d.user_config_dir = Path.Combine(b, "user_config");
            d.user_state_dir = Path.Combine(b, "user_state");
            d.user_cache_dir = Path.Combine(b, "user_cache");
            d.user_run_dir = user_run_dir;
            return d;
        }

        static bool StartLocalServer(NodeDirectories dirs)
        {
            var node = new RobotRaconteurNode();
            try
            {
                node.NodeDirectories = dirs;
                var t = new LocalTransport(node);
                node.RegisterTransport(t);
                t.StartServerAsNodeName("bughunt_tmp_run_dir");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                node.Shutdown();
            }
        }

        static void RemoveRunDir(string run_dir)
        {
            var fi = new FileInfo(run_dir);
            if (fi.Exists || fi.LinkTarget != null)
            {
                File.Delete(run_dir);
            }
            else if (Directory.Exists(run_dir))
            {
                Directory.Delete(run_dir, true);
            }
        }

        public static void RunTestTmpUserRunDirSecure()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            uint uid = Mono.Unix.Native.Syscall.getuid();
            string run_dir = "/tmp/robotraconteur-run-" + uid;
            string b = Path.Combine(Path.GetTempPath(), "rr_bughunt_" + Guid.NewGuid().ToString("N"));
            string other = Path.Combine(b, "other");
            Directory.CreateDirectory(other);
            var dirs = TempNodeDirs(b, run_dir);
            try
            {
                // Symlink planted by another user: rejected, nothing created in the link target
                RemoveRunDir(run_dir);
                Directory.CreateSymbolicLink(run_dir, other);
                RRAssert.IsFalse(StartLocalServer(TempNodeDirs(b, run_dir)));
                RRAssert.IsFalse(Directory.Exists(Path.Combine(other, "transport")));
                RemoveRunDir(run_dir);

                // Directory owned by another user: rejected
                if (uid == 0)
                {
                    Directory.CreateDirectory(run_dir);
                    Mono.Unix.Native.Syscall.chown(run_dir, 12345, 12345);
                    RRAssert.IsFalse(StartLocalServer(TempNodeDirs(b, run_dir)));
                    RemoveRunDir(run_dir);
                }

                // Own directory with group/other access: made private
                Directory.CreateDirectory(run_dir);
                Mono.Unix.Native.Syscall.chmod(run_dir, Mono.Unix.Native.FilePermissions.ACCESSPERMS);
                RRAssert.IsTrue(StartLocalServer(TempNodeDirs(b, run_dir)));
                Mono.Unix.Native.Stat st;
                Mono.Unix.Native.Syscall.stat(run_dir, out st);
                RRAssert.IsTrue((st.st_mode & (Mono.Unix.Native.FilePermissions.S_IRWXG | Mono.Unix.Native.FilePermissions.S_IRWXO)) == 0);
            }
            finally
            {
                try { RemoveRunDir(run_dir); } catch (Exception) { }
                try { Directory.Delete(b, true); } catch (Exception) { }
            }
        }
    }
}
