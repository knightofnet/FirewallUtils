using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AryxDevLibrary.utils;
using PocFwIpApp.dto;

namespace PocFwIpApp.utils
{
    public static class MiscAppUtils
    {
        // private static Dictionary<Process, String> cacheGetExe = new Dictionary<Process, string>();

        private static Regex regexIp4 = new Regex(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$");
        private static Regex regexIp6 = new Regex(@"'/(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))/m'");

        public static List<ProcessExtended> GetPorProcessExtendeds(String processName = null)
        {
            String[] excludedProcess = new[] { "svchost", "Idle", "System", "wininit" };

            ConcurrentBag<ProcessExtended> retList = new ConcurrentBag<ProcessExtended>();

            ManagementClass processClass = new ManagementClass();
            processClass.Path = new ManagementPath("Win32_Process");
            ManagementObjectCollection managementObjectCollection = processClass.GetInstances();

            ConcurrentDictionary<uint, ProcessExtended> locCache = new ConcurrentDictionary<uint, ProcessExtended>();

            Process[] processes = processName == null ? Process.GetProcesses() : Process.GetProcessesByName(processName);

            Parallel.ForEach(processes, (p) =>
            {
                if (excludedProcess.Contains(p.ProcessName))
                {
                    return;
                }

                locCache.TryAdd((uint)p.Id, new ProcessExtended(p));

            });


            Parallel.ForEach(managementObjectCollection.Cast<ManagementBaseObject>(), (ManagementBaseObject o) =>
            {
                uint u = (uint)o["ProcessId"];

                if (locCache.ContainsKey(u))
                {
                    ProcessExtended pX = locCache[u];
                    pX.Filepath = (string)o["ExecutablePath"]; ;

                    retList.Add(pX);

                }


            });



            return retList.ToList();
        }


        public static string GetExecutablePath(Process process)
        {

            ManagementClass processClass = new ManagementClass();
            processClass.Path = new ManagementPath("Win32_Process");



            var managementObjectCollection = processClass.GetInstances();
            Dictionary<uint, string> locCache = new Dictionary<uint, string>(managementObjectCollection.Count);
            foreach (ManagementBaseObject o in managementObjectCollection)
            {
                uint u = (uint)o["ProcessId"];

                if (locCache.ContainsKey(u))
                {
                    return locCache[u];
                }

                if (u == process.Id)
                {
                    //cacheGetExe.Add(process, (string)o["ExecutablePath"]);
                    return (string)o["ExecutablePath"];
                }

                locCache.Add(u, (string)o["ExecutablePath"]);
            }

            return null;
        }

        public static FileVersionInfo GetProcessFVersion(Process pValue)
        {
            String filePath = MiscAppUtils.GetExecutablePath(pValue);
            if (!File.Exists(filePath)) return null;

            FileVersionInfo fVi = FileVersionInfo.GetVersionInfo(filePath);
            return fVi;
        }

        public static bool IsIp4(string ip)
        {
            return regexIp4.IsMatch(ip);
        }

        public static bool IsIp6(string ip)
        {
            return regexIp6.IsMatch(ip);
        }


        public static String ListStrToStr(ICollection<string> lstUdp)
        {
            return String.Join(",", lstUdp);
        }



        public static bool IsValidFilepath(String filePath)
        {
            if (StringUtils.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !Path.IsPathRooted(filePath))
            {
                return false;
            }

                

       

            return true;
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string GetLastValidDirectory(string fwFilepath)
        {
            FileInfo fi = new FileInfo(fwFilepath);

            DirectoryInfo dir = fi.Directory;
            while (dir != null && !dir.Exists)
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                dir = new DirectoryInfo(Path.GetPathRoot(fwFilepath));
            }
            return dir.FullName;
        }
    }
}
