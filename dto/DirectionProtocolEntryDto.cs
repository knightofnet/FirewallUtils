using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocFwIpApp.dto
{
    public class DirectionProtocolEntryDto : DirectionProtocolDto
    {
        public EventLogEntry Entry { get; set; }

    }
}
