namespace TelnetWebAccess
{
    public class TelnetEndPoint
    {
        public TelnetEndPoint(string host, int port)
        {
            Host = host;
            Port = port;
        }
        public string Host { get; private set; }

        public int Port { get; private set; }

        protected bool Equals(TelnetEndPoint other)
        {
            return string.Equals(Host, other.Host) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TelnetEndPoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Host.GetHashCode() * 397) ^ Port;
            }
        }

        public static bool operator ==(TelnetEndPoint left, TelnetEndPoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TelnetEndPoint left, TelnetEndPoint right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return Host + "_" + Port;
        }
    }
}