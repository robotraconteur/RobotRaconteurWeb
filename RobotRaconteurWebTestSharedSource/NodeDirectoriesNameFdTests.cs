#if !ROBOTRACONTEUR_H5
using System;
using System.IO;
using System.Runtime.InteropServices;
using RobotRaconteurTest;
using RobotRaconteurWeb;

namespace RobotRaconteurWebTest
{
    // Regression test: GetUuidForNameAndLock must close the NodeID name file before returning
    // (only the pid lock file in the run directory stays locked)
    public static class NodeDirectoriesNameFdTests
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Flock
        {
            public short l_type;
            public short l_whence;
            public long l_start;
            public long l_len;
            public int l_pid;
        }

        [DllImport("libc", SetLastError = true)]
        static extern int open(string pathname, int flags, int mode);

        [DllImport("libc", SetLastError = true)]
        static extern int fcntl(int fd, int cmd, ref Flock lck);

        [DllImport("libc", SetLastError = true)]
        static extern int close(int fd);

        const int O_RDWR = 2;
        const int O_CREAT = 0x40;
        const int F_OFD_SETLK = 37;
        const short F_WRLCK = 1;

        // An open file description lock conflicts with every other lock on the file, also with
        // locks taken by this process through another file descriptor (Linux only)
        internal static int OfdLock(string path)
        {
            int fd = open(path, O_RDWR | O_CREAT, Convert.ToInt32("600", 8));
            if (fd < 0)
            {
                return -1;
            }
            var lck = new Flock { l_type = F_WRLCK, l_whence = 0, l_start = 0, l_len = 0, l_pid = 0 };
            if (fcntl(fd, F_OFD_SETLK, ref lck) < 0)
            {
                close(fd);
                return -1;
            }
            return fd;
        }

        public static void RunTestNameFileClosedAfterStart()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            string name = "nodedir_name_fd_test";
            var node = new RobotRaconteurNode();
            int fd = -1;
            try
            {
                var t = new LocalTransport(node);
                if (!t.IsLocalTransportSupported)
                {
                    return;
                }

                t.StartServerAsNodeName(name);
                node.RegisterTransport(t);

                // The name file must not stay open and locked after the NodeID was read
                fd = OfdLock(Path.Combine(node.NodeDirectories.user_config_dir, "nodeids", name));
                RRAssert.IsTrue(fd >= 0);
            }
            finally
            {
                if (fd >= 0)
                {
                    close(fd);
                }
                node.Shutdown();
            }
        }
    }
}
#endif
