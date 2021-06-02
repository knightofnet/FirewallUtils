using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PocFwIpApp.constant;
using PocFwIpApp.dto;

namespace PocFwIpApp.utils
{
    internal class DirectionProtocolAdressMap : IEquatable<DirectionProtocolAdressMap>
    {
        private readonly Dictionary<DirectionProtocolDto, List<EventLogEntry>> _innerDict =
            new Dictionary<DirectionProtocolDto, List<EventLogEntry>>();

        public void Add(EventLogEntry entry)
        {
            DirectionProtocolDto d = new DirectionProtocolDto()
            {
                Direction = entry.GetDirection(),
                Protocol = entry.GetProtocole()
            };

            List<EventLogEntry> list = null;
            if (!_innerDict.ContainsKey(d))
            {
                list = new List<EventLogEntry>();
                _innerDict.Add(d, list);
            }
            else
            {
                list = _innerDict[d];
            }

            if (!list.Any(r=>r.GetRemoteAddress().Equals(entry.GetRemoteAddress())))
            {
                list.Add(entry);
            }


        }

 

        public List<String> GetByDirectionProtocol(DirectionsEnum direction, ProtocoleEnum protocol, Func<EventLogEntry, String> entryGet)
        {
            List<String> retList = new List<string>();

            foreach (KeyValuePair<DirectionProtocolDto, List<EventLogEntry>> kv in _innerDict
                .Where(r => (r.Key.Direction.Equals(direction)) && (r.Key.Protocol.Equals(protocol))))
            {
                retList.AddRange(kv.Value.Select(entryGet));
            }

            return retList;
        }

        public bool Any()
        {
            return _innerDict.Any();
        }


        public bool Equals(DirectionProtocolAdressMap other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_innerDict, other._innerDict);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DirectionProtocolAdressMap) obj);
        }

        public override int GetHashCode()
        {
            return (_innerDict != null ? _innerDict.GetHashCode() : 0);
        }

        public static bool operator ==(DirectionProtocolAdressMap left, DirectionProtocolAdressMap right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DirectionProtocolAdressMap left, DirectionProtocolAdressMap right)
        {
            return !Equals(left, right);
        }
    }
}