using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace PocFwIpApp.dto
{
    class FwRuleInvalid
    {

        public String RuleName { get; set; }
        public String Direction { get; set; }
        public String Filepath { get; set; }
        public String Message { get; set; }

        public INetFwRule FwRule { get; set; }
        public string FilepathCorrected { get; set; }
    }
}
