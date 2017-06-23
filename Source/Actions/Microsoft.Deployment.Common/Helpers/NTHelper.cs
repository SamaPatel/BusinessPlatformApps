using System;

using Microsoft.Win32;
using System.Diagnostics;

namespace Microsoft.Deployment.Common.Helpers
{
    public class NTHelper
    {
        public static string CleanDomain(string domain)
        {
            string cleanedDomain = domain;

            if (string.IsNullOrEmpty(domain))
            {
                cleanedDomain = Environment.GetEnvironmentVariable("USERDOMAIN");
            }
            else if (domain.EqualsIgnoreCase("."))
            {
                cleanedDomain = Environment.MachineName;
            }

            return cleanedDomain;
        }

        public static string CleanUsername(string username)
        {
            string cleanedUsername = username;

            if (string.IsNullOrEmpty(username))
            {
                cleanedUsername = Environment.GetEnvironmentVariable("USERNAME");
            }

            return cleanedUsername;
        }

        public static bool IsCredentialGuardEnabled()
        {
            bool isCredentialGuardEnabled = false;
            int[] osVersion = NTHelper.OsVersionArray;

            if (osVersion[0] == 10 && osVersion[1] == 0 && osVersion[2] < 15011)
            {
                try
                {
                    using (RegistryKey rk = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa"))
                    {
                        object o = rk.GetValue("LsaCfgFlags");
                        int rv = (int)o;
                        isCredentialGuardEnabled = (rv == 1 || rv == 2);
                    }

                }
                catch
                {
                    // Checking credential guard failed
                }
            }
            return isCredentialGuardEnabled;
        }

        public static int[] OsVersionArray
        {
            get
            {
                int[] result = new int[3];

                using (RegistryKey k = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion"))
                {
                    result[0] = Convert.ToInt32(k.GetValue("CurrentMajorVersionNumber"));
                    result[1] = Convert.ToInt32(k.GetValue("CurrentMinorVersionNumber"));
                    result[2] = Convert.ToInt32(k.GetValue("CurrentBuildNumber") ?? k.GetValue("CurrentBuild"));
                }

                return result;
            }
        }

        public static string OsVersion
        {
            get
            {
                return string.Join(".", NTHelper.OsVersionArray);
            }
        }

        public static void KillProcess(string processName)
        {
            foreach (Process p in Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(10000);
                }
                catch { }

            }
        }
    }
}