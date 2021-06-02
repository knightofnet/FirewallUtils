using System;
using PocFwIpApp.constant;

namespace PocFwIpApp.dto
{
    public class DirectionProtocolDto : IEquatable<DirectionProtocolDto>
    {
        public DirectionsEnum Direction { get; set; }
        public ProtocoleEnum Protocol { get; set; }

        public override string ToString()
        {
            return $"Direction: {Direction}, Protocol: {Protocol}";
        }

        public bool Equals(DirectionProtocolDto other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Direction == other.Direction && Protocol == other.Protocol;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DirectionProtocolDto) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Direction * 397) ^ (int) Protocol;
            }
        }

        public static bool operator ==(DirectionProtocolDto left, DirectionProtocolDto right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DirectionProtocolDto left, DirectionProtocolDto right)
        {
            return !Equals(left, right);
        }
    }
}