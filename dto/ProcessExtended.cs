using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PocFwIpApp.utils;

namespace PocFwIpApp.dto
{
    public class ProcessExtended 
    {
        private FileVersionInfo _fileVersionInfo;

        public Process Process { get; set; }

        public String Filepath { get; set; }

        public FileVersionInfo FileVersionInfo
        {
            get => GetOrCreateFileVersion();
            set => _fileVersionInfo = value;
        }



        public int Id => Process.Id;
        public String ProcessName => Process.ProcessName;

        public ProcessExtended(Process process)
        {
            Process = process;
            //Filepath = MiscAppUtils.GetExecutablePath(process);
            /*
            if (Filepath != null && File.Exists(Filepath))
            {
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(Filepath);
                FileNiceName = fv.ProductName;
            }
            */
        }


        private FileVersionInfo GetOrCreateFileVersion()
        {
            if (_fileVersionInfo == null && Filepath != null)
            {
                if (File.Exists(Filepath))
                {
                    _fileVersionInfo = FileVersionInfo.GetVersionInfo(Filepath);
                    
                }
            }

            return _fileVersionInfo;
        }


    }
}
