using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using PocFwIpApp.utils;

namespace PocFwIpApp.dto
{
    class FwRuleInvalid
    {


        public String RuleName { get; set; }
        public String Direction { get; set; }
        public String Filepath { get; set; }
        public String Message { get; set; }
        public String RuleState => GetRuleState();

        public String Action => GetActionString();

        public String Enabled => GetEnabledString();

        public INetFwRule FwRule { get; set; }
        public string FilepathCorrected { get; set; }
        public bool IsWindowsApp { get; set; }
        public bool IsRealRule => GetIsRealRule();
        public ProcessFileFwRule ProcessFileFwRule { get; set; }


        private bool GetIsRealRule()
        {
            return ProcessFileFwRule == null;
        }

        private string GetRuleState()
        {
            if (FwRule != null)
            {
                return FwRule.Action.ToString();
            }

            return "";
        }

        private string GetActionString()
        {
            if (FwRule == null)
            {
                return "";
            }

            switch (FwRule.Action)
            {
                case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
                    return "Autoriser";
                case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
                    return "Bloquée";
                case NET_FW_ACTION_.NET_FW_ACTION_MAX:
                    return "Max";
            }

            return "";
        }

        private string GetEnabledString()
        {
            if (FwRule == null)
            {
                return "";
            }

            switch (FwRule.Enabled)
            {
                case false:
                    return "Non";
                case true:
                    return "Oui";
            }

            return "";
        }



    }
}
