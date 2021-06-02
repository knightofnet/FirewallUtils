using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocFwIpApp.utils.sqlite
{
    public class SqlLiteKVPair
    {
        public string Key { get; set; }

        public object Value { get; set; }

        public bool IsUpdateField { get; set; }

    }
}
