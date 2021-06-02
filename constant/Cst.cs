using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PocFwIpApp.constant
{
    public class Cst
    {
        public static readonly string DirectionOutboundString = "%%14593";
        public static readonly string DirectionIntboundString = "%%14592";

        public static readonly Regex WindowsAppRegex = new Regex(@"\\[wW]indows[aA]pps\\(?'name'.+?)_(?'version'.+?)_(?'pf'.*?)_(?'architecture'.*?)_(?'publisherid'.*?)\\(?'subDir'.+?)$");
    }
}
