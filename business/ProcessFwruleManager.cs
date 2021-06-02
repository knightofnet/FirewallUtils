using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AryxDevLibrary.utils.logger;
using NetFwTypeLib;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;

namespace PocFwIpApp.business
{
    class ProcessFwruleManager
    {
        private static Logger log = Logger.LastLoggerInstance;

        public ObservableCollection<ProcessFileFwRule> FileFwRules { get; private set; }

        public DateTime LastRefresh { get; private set; }
        
        public ProcessFwruleManager()
        {
            FileFwRules = new ObservableCollection<ProcessFileFwRule>();
           

        }


        public void Gather(List<ProcessFileFwRule> processFileFwRules)
        {
            foreach (ProcessFileFwRule pff in processFileFwRules)
            {
                INetFwRule fwRule = FwUtils.GetRule(pff.RuleName, pff.DirectionProtocol.Direction,
                    pff.DirectionProtocol.Protocol);
                if (fwRule == null)
                {
                    log.Warn("Aucune règle trouvée avec le nom {0}, la direction/procotole {1}", pff.RuleName, pff.DirectionProtocol);
                    continue;
                }

                pff.HydrateWithFwRule(fwRule);

                if (!FileFwRules.Any(r =>
                    r.FilePath.FullName.Equals(pff.FilePath.FullName) && r.DirectionProtocol.Equals(pff.DirectionProtocol)))
                {
                    FileFwRules.Add(pff);
                }

            }

         

            LastRefresh = DateTime.Now;
            
        }

        public bool NeedRefresh()
        {
            DateTime dtN = DateTime.Now;
            if ((dtN - LastRefresh) > new TimeSpan(0, 1, 0))
            {
                return true;
            }

            return false;
        }

        
    }
}
