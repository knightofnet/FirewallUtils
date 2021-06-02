using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AryxDevLibrary.utils;
using NetFwTypeLib;
using PocFwIpApp.constant;
using PocFwIpApp.utils;

namespace PocFwIpApp.dto
{
    public class ProcessFileFwRule
    {

        // Saved
        public String RuleName { get; set; }

        // Saved
        public FileInfo FilePath { get; set; }

        // Saved (in two elements)
        public DirectionProtocolDto DirectionProtocol { get; set; }

        // Saved
        public bool IsModeManuel = false;

        // Saved
        public bool IsEnableOnlyFileName = false;


        public INetFwRule FwRule { get; private set; }

        public Process[] Processes => GetProcess();

        public String DisplayItem => GetDisplayName();

        public bool IsProcessUp => DetermineIsProcessUp();
        public DateTime? DateCreation { get; set; }
        public DateTime? DateLastUpdate { get; set; }


        public ProcessFileFwRule()
        {
            DirectionProtocol = new DirectionProtocolDto();
        }

        private string GetDisplayName()
        {
            return String.Format("{0} ({1}{2})", FilePath.Name, IsProcessUp ? "P" : "-", FwRule.Enabled ? "R" : "-");
        }


        public void RefreshRule()
        {
            if (FwRule == null) return;
            INetFwRule fwRule = FwUtils.GetRule(FwRule.Name, DirectionProtocol.Direction, DirectionProtocol.Protocol);
            HydrateWithFwRule(fwRule);
            
        }

        public void HydrateWithFwRule(INetFwRule fwRule)
        {
            if (fwRule == null) return;

            FwRule = fwRule;

            DirectionProtocolDto dp = new DirectionProtocolDto()
            {
                Direction = FwUtils.FwRuleDirectionToDirectionEnum(fwRule.Direction),
                Protocol = (ProtocoleEnum)fwRule.Protocol
            };

            DirectionProtocol = dp;
        }


        private bool DetermineIsProcessUp()
        {
            Process[] pS = GetProcess();
            if (pS == null) return false;

            return pS.Any();
        }

        private Process[] GetProcess()
        {
            if (FilePath == null) return null;
            String pName = FilePath.Name.Replace(".exe", "");

            return Process.GetProcessesByName(pName);

            if (IsEnableOnlyFileName)
            {
                return Process.GetProcessesByName(pName);
            }

            List<Process> processes = new List<Process>();
            foreach (ProcessExtended pX in MiscAppUtils.GetPorProcessExtendeds(pName).Where(r=>r.Filepath != null))
            {
                if (File.Exists(pX.Filepath))
                {
                    processes.Add(pX.Process);
                }
            }

            return processes.ToArray();

        }



    }
}
