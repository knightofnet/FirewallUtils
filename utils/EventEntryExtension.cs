using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PocFwIpApp.constant;

namespace PocFwIpApp.utils
{
    public static class EventEntryExtension
    {

        public static DirectionsEnum GetDirection(this EventLogEntry entry)
        {
            if (entry.InstanceId != 5157 && entry.InstanceId != 5152)
            {
                return DirectionsEnum.NULL;
            }

            if (entry.ReplacementStrings[2].Equals(Cst.DirectionOutboundString))
            {
                return DirectionsEnum.Outbound;
            }

            if (entry.ReplacementStrings[2].Equals(Cst.DirectionIntboundString))
            {
                return DirectionsEnum.Inboud;
            }

            return DirectionsEnum.NULL;
        }

        public static ProtocoleEnum GetProtocole(this EventLogEntry entry)
        {
            if (entry.InstanceId != 5157 && entry.InstanceId != 5152)
            {
                return ProtocoleEnum.NULL;
            }

            int intProtocole = 0;
            if (Int32.TryParse(entry.ReplacementStrings[7], out intProtocole))
            {
                switch (intProtocole)
                {
                    case 6:
                        return ProtocoleEnum.TCP;
                    case 17:
                        return ProtocoleEnum.UDP;
                    default:
                        return ProtocoleEnum.NULL;
                }
            }

            return ProtocoleEnum.NULL;
        }

        public static String GetRemoteAddress(this EventLogEntry entry)
        {
            if (entry.InstanceId != 5157 && entry.InstanceId != 5152)
            {
                return null;
            }

            return entry.ReplacementStrings[5];
        }
    }
}
