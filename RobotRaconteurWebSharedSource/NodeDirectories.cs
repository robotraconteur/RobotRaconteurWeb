// Copyright 2011-2024 Wason Technology, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if !ROBOTRACONTEUR_H5
using System.Runtime.InteropServices;
using System.Security.Principal;
using Mono.Unix;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static RobotRaconteurWeb.RRLogFuncs;

namespace RobotRaconteurWeb
{

    /// <summary>
    /// Directories on local system used by the node
    /// </summary>
    /// <remarks>
    /// The node uses local directories to load configuration information,
    /// cache data, communicate with other processes, etc. These directories
    /// can be configured using the NodeDirectories structure and
    /// RobotRaconteurNode.GetNodeDirectories() and RobotRaconteurNode.SetNodeDirectories().
    /// Use GetDefaultNodeDirectories() to retrieve the default directories.
    /// 
    /// Note: for root user, system and user directories are identical.
    /// </remarks>
    [PublicApi] 
    public class NodeDirectories
    {
        /// <summary>
        /// Robot Raconteur System data directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: /usr/local/share/robotraconteur
        /// Default value Windows: %PROGRAMDATA%\RobotRaconteur\data
        /// Environmental variable override: ROBOTRACONTEUR_SYSTEM_DATA_DIR
        /// </remarks>
        [PublicApi] 
        public string system_data_dir;
        /// <summary>
        /// Robot Raconteur System config directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: /etc/robotraconteur
        /// Default value Windows: %PROGRAMDATA%\RobotRaconteur\
        /// Environmental variable override: ROBOTRACONTEUR_SYSTEM_CONFIG_DIR
        /// </remarks>
        [PublicApi] 
        public string system_config_dir;
        /// <summary>
        /// Robot Raconteur System config directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: /var/lib/robotraconteur
        /// Default value Windows: %PROGRAMDATA%\RobotRaconteur\state
        /// Environmental variable override: ROBOTRACONTEUR_SYSTEM_STATE_DIR
        /// </remarks>
        [PublicApi] 
        public string system_state_dir;
        /// <summary>
        /// Robot Raconteur System cache directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: /var/cache/robotraconteur
        /// Default value Windows: %PROGRAMDATA%\RobotRaconteur\cache
        /// Environmental variable override: ROBOTRACONTEUR_SYSTEM_CACHE_DIR
        /// </remarks>
        [PublicApi] 
        public string system_cache_dir;
        /// <summary>
        /// Robot Raconteur System run directory
        /// </summary>
        /// <remarks>
        /// Default value: /var/run/robotraconteur
        /// Default value Windows: %PROGRAMDATA%\RobotRaconteur\run
        /// Environmental variable override: ROBOTRACONTEUR_SYSTEM_RUN_DIR
        /// </remarks>
        [PublicApi] 
        public string system_run_dir;
        /// <summary>
        /// Robot Raconteur User data directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: $HOME/.local/share/RobotRaconteur or $XDG_DATA_HOME/RobotRaconteur
        /// Default value Windows: %LOCALAPPDATA%\RobotRaconteur\data
        /// Environmental variable override: ROBOTRACONTEUR_USER_DATA_DIR
        /// </remarks>
        [PublicApi] 
        public string user_data_dir;
        /// <summary>
        /// Robot Raconteur User config directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: $HOME/.config/RobotRaconteur or $XDG_CONFIG_HOME/RobotRaconteur
        /// Default value Windows: %LOCALAPPDATA%\RobotRaconteur\
        /// Environmental variable override: ROBOTRACONTEUR_USER_CONFIG_DIR
        /// </remarks>
        [PublicApi] 
        public string user_config_dir;
        /// <summary>
        /// Robot Raconteur User state directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: $HOME/.local/state/RobotRaconteur or $XDG_STATE_HOME/RobotRaconteur
        /// Default value Windows: %LOCALAPPDATA%\RobotRaconteur\state
        /// Environmental variable override: ROBOTRACONTEUR_USER_STATE_DIR
        /// </remarks>
        [PublicApi] 
        public string user_state_dir;
        /// <summary>
        /// Robot Raconteur User cache directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: $HOME/.cache/RobotRaconteur or $XDG_CACHE_HOME/RobotRaconteur
        /// Default value Windows: %LOCALAPPDATA%\RobotRaconteur\cache
        /// Environmental variable override: ROBOTRACONTEUR_USER_CACHE_DIR
        /// </remarks>
        [PublicApi] 
        public string user_cache_dir;
        /// <summary>
        /// Robot Raconteur User state directory
        /// </summary>
        /// <remarks>
        /// Default value Unix: $XDG_RUNTIME_DIR/robotraconteur or /tmp/robotraconteur-run-$UID
        /// Default value Windows: %LOCALAPPDATA%\RobotRaconteur\run
        /// Default value for root: {system_run_dir}/root
        /// Environmental variable override: ROBOTRACONTEUR_USER_RUN_DIR
        /// </remarks>
        [PublicApi] 
        public string user_run_dir;
    }

#pragma warning disable 1591

#if !ROBOTRACONTEUR_H5
    static class NodeDirectoriesUtil
    {
        internal static string replace_default_val_with_env(string default_val, string rr_env_var)
        {
            // Get environmental variable
            string env_val = Environment.GetEnvironmentVariable(rr_env_var);
            if (env_val == null)
            {
                return default_val;
            }
            else
            {
                return env_val;
            }
        }

        internal static string user_unix_home_dir(string default_rel_dir, string xdg_env, string rr_env_var)
        {
            string rr_env_var_val = Environment.GetEnvironmentVariable(rr_env_var);
            if (!string.IsNullOrEmpty(rr_env_var_val))
            {
                return rr_env_var_val;
            }

            string rr_user_home = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_USER_HOME");
            if (!string.IsNullOrEmpty(rr_user_home))
            {
                return Path.Combine(rr_user_home, default_rel_dir);
            }

            string xdg_env_val = Environment.GetEnvironmentVariable(xdg_env);
            if (!string.IsNullOrEmpty(xdg_env_val))
            {
                return Path.Combine(xdg_env_val, "RobotRaconteur");
            }

            string home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
            {
                throw new InvalidOperationException("Home directory not set");
            }

            return Path.Combine(home, default_rel_dir);
        }

        internal static string system_unix_dir(string default_dir, string rr_env_var)
        {
            string rr_env_var_val = Environment.GetEnvironmentVariable(rr_env_var);
            if (!string.IsNullOrEmpty(rr_env_var_val))
            {
                return rr_env_var_val;
            }

            string rr_sys_prefix = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_SYSTEM_PREFIX");
            if (!string.IsNullOrEmpty(rr_sys_prefix))
            {
                return Path.Combine(rr_sys_prefix, default_dir);
            }

            return default_dir;
        }

        public static string user_unix_run_dir(string xdg_env, string rr_env_var)
        {
            string rr_env_var_val = Environment.GetEnvironmentVariable(rr_env_var);
            if (!string.IsNullOrEmpty(rr_env_var_val))
            {
                return rr_env_var_val;
            }

            string xdg_env_val = Environment.GetEnvironmentVariable(xdg_env);
            if (!string.IsNullOrEmpty(xdg_env_val))
            {
                return Path.Combine(xdg_env_val, "robotraconteur");
            }

            int u = (int)UnixEnvironment.RealUserId;
            string run_path = $"/tmp/robotraconteur-run-{u}";

            return run_path;
        }

        public static string user_apple_run_dir(string rr_env_var)
        {
            string rr_env_var_val = Environment.GetEnvironmentVariable(rr_env_var);
            if (!string.IsNullOrEmpty(rr_env_var_val))
            {
                return rr_env_var_val;
            }

            string path1 = Environment.GetEnvironmentVariable("TMPDIR");
            if (string.IsNullOrEmpty(path1))
            {
                throw new InvalidOperationException("Could not activate system for local transport");
            }

            string path = Path.GetDirectoryName(path1.TrimEnd(Path.DirectorySeparatorChar));
            string combined_path = Path.Combine(path, "C");

            if (!Directory.Exists(combined_path))
            {
                throw new InvalidOperationException("Could not activate system for local transport");
            }

            return Path.Combine(combined_path, "robotraconteur");
        }

        internal static string get_user_win_localappdata()
        {
            string rr_user_local_appdata = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_USER_LOCALAPPDATA");
            if (!string.IsNullOrEmpty(rr_user_local_appdata))
            {
                return rr_user_local_appdata;
            }

            string sysdata_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(sysdata_path))
            {
                throw new InvalidOperationException("Could not get system information");
            }

            return sysdata_path;
        }
     
        public static string get_common_appdata()
        {
            string rr_user_common_appdata = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_SYSTEM_PROGRAMDATA");
            if (!string.IsNullOrEmpty(rr_user_common_appdata))
            {
                return rr_user_common_appdata;
            }

            string sysdata_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrEmpty(sysdata_path))
            {
                throw new InvalidOperationException("Could not get system information");
            }

            return sysdata_path;
        }

        internal static bool is_sub_dir(string p, string root)
        {
            root = Path.GetFullPath(root);  // Ensure absolute path

            while (!string.IsNullOrEmpty(p))
            {
                if (string.Equals(p, root, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                p = Path.GetDirectoryName(p);
            }
            return false;
        }

        internal static string GetLogonUserName()
        {
            return Environment.UserName;
        }

        private static bool IsLogonUserRootWinCompareRid(SecurityIdentifier userSid, WellKnownSidType wellKnownSidType)
        {
            SecurityIdentifier sidToCompare = new SecurityIdentifier(wellKnownSidType, null);
            return userSid.Equals(sidToCompare);
        }

        internal static bool IsLogonUserRootWin()
        {
            using (WindowsIdentity currentUser = WindowsIdentity.GetCurrent())
            {
                if (currentUser == null)
                {
                    return false;
                }

                SecurityIdentifier userSid = currentUser.User;
                if (userSid == null)
                {
                    return false;
                }

                if (IsLogonUserRootWinCompareRid(userSid, WellKnownSidType.LocalSystemSid))
                {
                    return true;
                }

                if (IsLogonUserRootWinCompareRid(userSid, WellKnownSidType.LocalServiceSid))
                {
                    return true;
                }

                if (IsLogonUserRootWinCompareRid(userSid, WellKnownSidType.NetworkServiceSid))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsLogonUserRoot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsLogonUserRootWin();
            }
           
            return UnixEnvironment.RealUserId == 0;           
        }

        internal static NodeDirectories GetDefaultNodeDirectories_unix(RobotRaconteurNode node)
        {
            NodeDirectories ret = new NodeDirectories();

            TryCatchDirResolve(() => ret.system_data_dir = system_unix_dir("/usr/local/share/robotraconteur", "ROBOTRACONTEUR_SYSTEM_DATA_DIR"),
                               "system_data_dir", node);
            TryCatchDirResolve(() => ret.system_config_dir = system_unix_dir("/etc/robotraconteur", "ROBOTRACONTEUR_SYSTEM_CONFIG_DIR"),
                               "system_config_dir", node);
            TryCatchDirResolve(() => ret.system_state_dir = system_unix_dir("/var/lib/robotraconteur", "ROBOTRACONTEUR_SYSTEM_STATE_DIR"),
                               "system_state_dir", node);
            TryCatchDirResolve(() => ret.system_cache_dir = system_unix_dir("/var/cache/robotraconteur", "ROBOTRACONTEUR_SYSTEM_CACHE_DIR"),
                               "system_cache_dir", node);
            TryCatchDirResolve(() => ret.system_run_dir = system_unix_dir("/var/run/robotraconteur", "ROBOTRACONTEUR_SYSTEM_RUN_DIR"),
                               "system_run_dir", node);

            if (IsLogonUserRoot())
            {
                ret.user_data_dir = ret.system_data_dir;
                ret.user_config_dir = ret.system_config_dir;
                ret.user_state_dir = ret.system_state_dir;
                ret.user_cache_dir = ret.system_cache_dir;
                ret.user_run_dir = Path.Combine(ret.system_run_dir, "root");
            }
            else
            {
                TryCatchDirResolve(() => ret.user_data_dir = user_unix_home_dir(".local/share/RobotRaconteur", "XDG_DATA_HOME", "ROBOTRACONTEUR_USER_DATA_DIR"),
                                   "user_data_dir", node);
                TryCatchDirResolve(() => ret.user_config_dir = user_unix_home_dir(".config/RobotRaconteur", "XDG_CONFIG_HOME", "ROBOTRACONTEUR_USER_CONFIG_DIR"),
                                   "user_config_dir", node);
                TryCatchDirResolve(() => ret.user_state_dir = user_unix_home_dir(".local/state/RobotRaconteur", "XDG_STATE_HOME", "ROBOTRACONTEUR_USER_CONFIG_DIR"),
                                   "user_state_dir", node);
                TryCatchDirResolve(() => ret.user_cache_dir = user_unix_home_dir(".cache/RobotRaconteur", "XDG_CACHE_HOME", "ROBOTRACONTEUR_USER_CACHE_DIR"),
                                   "user_cache_dir", node);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    TryCatchDirResolve(() => ret.user_run_dir = user_apple_run_dir("ROBOTRACONTEUR_USER_RUN_DIR"), "user_run_dir", node);
                }
                else
                {
                    TryCatchDirResolve(() => ret.user_run_dir = user_unix_run_dir("XDG_RUNTIME_DIR", "ROBOTRACONTEUR_USER_RUN_DIR"),
                                       "user_run_dir", node);
                }
            }

            return ret;
        }

        private static void TryCatchDirResolve(Action cmd, string dirType, RobotRaconteurNode node)
        {
            try
            {
                cmd.Invoke();
            }
            catch (Exception e)
            {
                if (node != null)
                {
                    // Assuming you have a suitable logging method in your RobotRaconteurNode class
                    LogWarning($"Error resolving {dirType}: {e.Message}", node, RobotRaconteur_LogComponent.Node, "NodeDirectories");
                }
            }
        }

        internal static NodeDirectories GetDefaultNodeDirectories_win(RobotRaconteurNode node)
        {
            NodeDirectories ret = new NodeDirectories();

            string userLocalAppData = get_user_win_localappdata();
            string commonAppData = get_common_appdata();

            TryCatchDirResolve(() => ret.system_data_dir = replace_default_val_with_env(
                               Path.Combine(commonAppData, "RobotRaconteur", "data"), "ROBOTRACONTEUR_SYSTEM_DATA_DIR"),
                               "system_data_dir", node);

            TryCatchDirResolve(() => ret.system_config_dir = replace_default_val_with_env(
                               Path.Combine(commonAppData, "RobotRaconteur"), "ROBOTRACONTEUR_SYSTEM_CONFIG_DIR"),
                               "system_config_dir", node);

            TryCatchDirResolve(() => ret.system_state_dir = replace_default_val_with_env(
                               Path.Combine(commonAppData, "RobotRaconteur", "state"), "ROBOTRACONTEUR_SYSTEM_STATE_DIR"),
                               "system_state_dir", node);

            TryCatchDirResolve(() => ret.system_cache_dir = replace_default_val_with_env(
                               Path.Combine(commonAppData, "RobotRaconteur", "cache"), "ROBOTRACONTEUR_SYSTEM_CACHE_DIR"),
                               "system_cache_dir", node);

            TryCatchDirResolve(() => ret.system_run_dir = replace_default_val_with_env(
                               Path.Combine(commonAppData, "RobotRaconteur", "run"), "ROBOTRACONTEUR_SYSTEM_RUN_DIR"),
                               "system_run_dir", node);

            if (IsLogonUserRoot())
            {
                ret.user_data_dir = ret.system_data_dir;
                ret.user_config_dir = ret.system_config_dir;
                ret.user_state_dir = ret.system_state_dir;
                ret.user_cache_dir = ret.system_cache_dir;
                ret.user_run_dir = Path.Combine(ret.system_run_dir, "root");
            }
            else
            {
                TryCatchDirResolve(() => ret.user_data_dir = replace_default_val_with_env(
                                   Path.Combine(userLocalAppData, "RobotRaconteur", "data"), "ROBOTRACONTEUR_USER_DATA_DIR"),
                                   "user_data_dir", node);

                TryCatchDirResolve(() => ret.user_config_dir = replace_default_val_with_env(
                                   Path.Combine(userLocalAppData, "RobotRaconteur"), "ROBOTRACONTEUR_USER_CONFIG_DIR"),
                                   "user_config_dir", node);

                TryCatchDirResolve(() => ret.user_state_dir = replace_default_val_with_env(
                                   Path.Combine(userLocalAppData, "RobotRaconteur", "state"), "ROBOTRACONTEUR_USER_STATE_DIR"),
                                   "user_state_dir", node);

                TryCatchDirResolve(() => ret.user_cache_dir = replace_default_val_with_env(
                                   Path.Combine(userLocalAppData, "RobotRaconteur", "cache"), "ROBOTRACONTEUR_USER_CACHE_DIR"),
                                   "user_cache_dir", node);

                TryCatchDirResolve(() => ret.user_run_dir = replace_default_val_with_env(
                                   Path.Combine(userLocalAppData, "RobotRaconteur", "run"), "ROBOTRACONTEUR_USER_RUN_DIR"),
                                   "user_run_dir", node);
            }

            return ret;
        }
        public static NodeDirectories GetDefaultNodeDirectories(RobotRaconteurNode node)
        {            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetDefaultNodeDirectories_win(node);
            }
            else
            {
                return GetDefaultNodeDirectories_unix(node);
            }            
        }

        public static GetUuidForNameAndLockResult GetUuidForNameAndLock(NodeDirectories node_dirs, string name, string[] scope)
        {
            NodeID nodeid = null;

            if (scope == null)
            {
                throw new InvalidOperationException("GetUuidForNameAndLock scope cannot be empty");
            }

            if (!Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9_\\.\\-]*$"))
            {
                throw new ArgumentException("\"" + name + "\" is an invalid NodeName");
            }

            string p = node_dirs.user_config_dir;

            foreach (var s in scope)
            {
                p = Path.Combine(p, s);
            }

            // Create p directory
            Directory.CreateDirectory(p);

            p = Path.Join(p, name);

            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);


            NodeDirectoriesFD fd = null;
            NodeDirectoriesFD fd_run = null;
            try
            {
                if (is_windows)
                {
                    fd = new NodeDirectoriesFD();

                    int error_code;
                    if (!fd.OpenLockWrite(p, false, out error_code))
                    {
                        if (error_code == 32)
                        {
                            throw new NodeDirectoriesResourceAlreadyInUse();
                        }
                        throw new SystemResourceException("Could not initialize UUID name store");
                    }
                }
                else
                {
                    string p_lock = node_dirs.user_run_dir;
                    foreach(var s in scope)
                    {
                        p_lock = Path.Combine(p, s);
                    }

                    Directory.CreateDirectory(p_lock);

                    p_lock = Path.Combine(p_lock, name + ".pid");

                    string p_state = node_dirs.user_state_dir;
                    foreach(var s in scope)
                    {
                        p_state = Path.Combine(p, s);
                    }

                    Directory.CreateDirectory (p_state);

                    p_state = Path.Combine(p_state, name);

                    fd_run = new NodeDirectoriesFD();

                    int open_run_err;
                    if (!fd_run.OpenLockWrite(p_lock, false, out open_run_err))
                    {
                        if (open_run_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                        {
                            throw new NodeDirectoriesResourceAlreadyInUse();
                        }
                        throw new SystemResourceException("Could not initialize UUID name store");
                    }

                    string pid_str = Process.GetCurrentProcess().Id.ToString();
                    if (!fd_run.Write(pid_str))
                    {
                        throw new SystemResourceException("Could not initialize UUID name store");
                    }

                    bool is_root = IsLogonUserRoot();
                    if (is_root)
                    {
                        var fd_etc = new NodeDirectoriesFD();
                        int open_err;
                        if (fd_etc.OpenRead(p, out open_err))
                        {
                            fd = fd_etc;
                        }
                    }

                    if (fd == null)
                    {
                        fd = new NodeDirectoriesFD();
                        int open_err;
                        if (!fd.OpenLockWrite(p, false, out open_err))
                        {
                            if (open_err == (int)Mono.Unix.Native.Errno.EROFS)
                            {
                                open_err = 0;
                                if (!fd.OpenRead(p, out open_err))
                                {
                                    throw new InvalidOperationException("UUID name not set on read only filesystem");
                                }
                            }
                            else
                            {
                                throw new SystemResourceException("Could not initialize UUID store");
                            }
                        }
                    }
                }
                int len = fd.FileLen;

                if (len == 0 || len == -1 || len > 16 * 1024)
                {
                    nodeid = NodeID.NewUniqueID();
                    string dat = nodeid.ToString();
                    fd.Write(dat);
                }
                else
                {
                    string nodeid_str;
                    fd.Read(out nodeid_str);
                    try
                    {
                        nodeid_str = nodeid_str.Trim();
                        nodeid = new NodeID(nodeid_str);
                    }
                    catch (Exception)
                    {
                        throw new IOException("Error in NodeID mapping settings file");
                    }
                }

                var res = new GetUuidForNameAndLockResult();
                res.name = name;
                res.scope = scope;
                res.uuid = nodeid;


                if (is_windows)
                {
                    res.fd = fd;
                }
                else
                {
                    res.fd = fd_run;
                }
                return res;
            }
            catch (Exception)
            {
                fd?.Dispose();
                fd_run?.Dispose();
                throw;
            }
        }

        public static bool ReadInfoFile(string fname, out Dictionary<string, string> data)
        {
            try
            {
                using (var fd = new NodeDirectoriesFD())
                {
                    int err_code;
                    if (!fd.OpenRead(fname, out err_code))
                    {
                        data = null;
                        return false;
                    }

                    if (!fd.ReadInfo())
                    {
                        data = null;
                        return false;
                    }

                    data = fd.Info;
                    return true;
                }
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        public static NodeDirectoriesFD CreatePidFile(string path, bool for_name)
        {
            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string pid_str = Process.GetCurrentProcess().Id.ToString();
            var fd = new NodeDirectoriesFD();
            try
            {
                if (is_windows)
                {
                    int open_err;
                    if (!fd.OpenLockWrite(path, true, out open_err))
                    {
                        if (!fd.OpenLockWrite(path, false, out open_err))
                        {
                            if (open_err == 32)
                            {
                                throw new NodeDirectoriesResourceAlreadyInUse();
                            }
                            throw new SystemResourcePermissionDeniedException("Could not initialize server");
                        }
                    }
                }
                else
                {
                    var old_mode = Mono.Unix.Native.Syscall.umask(~(Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IRGRP));
                    try
                    {
                        int open_err;
                        if (!fd.OpenLockWrite(path, true, out open_err))
                        {
                            if (!fd.OpenLockWrite(path, false, out open_err))
                            {
                                if (open_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                                {
                                    throw new NodeDirectoriesResourceAlreadyInUse();
                                }
                                throw new SystemResourcePermissionDeniedException("Could not initialize server");
                            }
                        }
                    }
                    finally
                    {
                        Mono.Unix.Native.Syscall.umask(old_mode);
                    }
                }

                fd.Write(pid_str);
                return fd;
            }
            catch (Exception)
            {
                fd?.Dispose();
                throw;
            }

        }

        public static NodeDirectoriesFD CreateInfoFile(string path, Dictionary<string, string> info, bool for_name)
        {
            bool is_windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            string pid_str = Process.GetCurrentProcess().Id.ToString();
            string username = GetLogonUserName();

            var fd = new NodeDirectoriesFD();
            try
            {
                if (is_windows)
                {
                    int open_err;
                    if (!fd.OpenLockWrite(path, true, out open_err))
                    {
                        if (!fd.OpenLockWrite(path, false, out open_err))
                        {
                            if (open_err == 32)
                            {
                                throw new NodeDirectoriesResourceAlreadyInUse();
                            }
                            throw new SystemResourcePermissionDeniedException("Could not initialize server");
                        }
                    }
                }
                else
                {
                    var old_mode = Mono.Unix.Native.Syscall.umask(~(Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR | Mono.Unix.Native.FilePermissions.S_IRGRP));
                    try
                    {
                        int open_err;
                        if (!fd.OpenLockWrite(path, true, out open_err))
                        {
                            if (!fd.OpenLockWrite(path, false, out open_err))
                            {
                                if (open_err == (int)Mono.Unix.Native.Errno.ENOLCK)
                                {
                                    throw new NodeDirectoriesResourceAlreadyInUse();
                                }
                                throw new SystemResourcePermissionDeniedException("Could not initialize server");
                            }
                        }
                    }
                    finally
                    {
                        Mono.Unix.Native.Syscall.umask(old_mode);
                    }
                }

                info["pid"] = pid_str;
                info["username"] = username;

                fd.Info = info;
                if (!fd.WriteInfo())
                {
                    throw new SystemResourceException("Could not initialize server");
                }
                return fd;
            }
            catch (Exception)
            {
                fd?.Dispose();
                throw;
            }
        }

        public static void RefreshInfoFile(NodeDirectoriesFD h_info, Dictionary<string,string> updated_info)
        {
            if (h_info == null) return;

            lock (h_info)
            {
                // Add or update contents of h_info.info
                foreach(var e in updated_info)
                {
                    h_info.Info[e.Key] = e.Value;
                }
            }

            h_info.Reset();
            h_info.WriteInfo();
        }
    }

    public class NodeDirectoriesFD : IDisposable
    {
        FileStream f;

        public Dictionary<string, string> Info { get; set; }

        public NodeDirectoriesFD()
        {

        }

        public bool OpenRead(string path, out int error_code)
        {
            try
            {
                var h = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                f = h;
                error_code = 0;
                return true;
            }
            catch (Exception ee)
            {
                error_code = 0xFFFF & ee.HResult;
                return false;
            }

        }

        public bool OpenLockWrite(string path, bool delete_on_close, out int error_code)
        {
            FileOptions file_options = default(FileOptions);
            if (delete_on_close)
            {
                file_options |= FileOptions.DeleteOnClose;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var h = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, file_options);
                    f = h;
                }
                else
                {
                    var h = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 1024, file_options);
                    h.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        h.Lock(0, 0);
                    }
                    catch (Exception)
                    {
                        h.Dispose();
                        throw;
                    }

                    f = h;
                }

                error_code = 0;
                return true;
            }
            catch (Exception ee)
            {
                error_code = 0xFFFF & ee.HResult;
                return false;
            }
        }

        public bool Read(out string data)
        {
            try
            {
                f.Seek(0, SeekOrigin.Begin);
                long len = f.Length;
                var reader = new StreamReader(f);
                data = reader.ReadToEnd();
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        public bool ReadInfo()
        {
            string in_;
            if (!Read(out in_))
            {
                return false;
            }

            var lines = in_.Split('\n');
            Info = new Dictionary<string, string>();

            var r = new Regex("^\\s*([\\w+\\.\\-]+)\\s*\\:\\s*(.*)\\s*$");

            foreach (var l in lines)
            {
                var r_match = r.Match(l);
                if (!r_match.Success)
                    continue;

                Info.Add(r_match.Groups[1].Value, r_match.Groups[2].Value);
            }

            return true;
        }

        public bool Write(string data)
        {
            try
            {
                var w = new StreamWriter(f);
                w.Write(data);
                w.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool WriteInfo()
        {
            string data = String.Join("\n", Info.Select((v) => String.Format("{0}: {1}", v.Key, v.Value)));
            try
            {
                return Write(data);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Reset()
        {
            try
            {
                f.Seek(0, SeekOrigin.Begin);
                f.SetLength(0);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int FileLen
        {
            get
            {
                return (int)f.Length;
            }
        }

        public void Dispose()
        {
            f?.Dispose();
        }
    }

    public class GetUuidForNameAndLockResult : IDisposable
    {
        public NodeID uuid;
        public string name;
        public string[] scope;
        public NodeDirectoriesFD fd;

        public void Dispose()
        {
            fd?.Dispose();
        }
    }
#else
    public static class NodeDirectoriesUtil
    {
        public static NodeDirectories GetDefaultNodeDirectories(RobotRaconteurNode node)
        {
            return new NodeDirectories();
        }
    }
#endif
    public class NodeDirectoriesResourceAlreadyInUse : IOException
    {
        public NodeDirectoriesResourceAlreadyInUse() : base("Identifier UUID or Name already in use") { }
        public NodeDirectoriesResourceAlreadyInUse(string message) : base(message) { }
    }

}

