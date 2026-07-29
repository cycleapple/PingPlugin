using System.Net;
using System.Threading.Tasks;

namespace PingPlugin.GameAddressDetectors
{
    public abstract class GameAddressDetector
    {
        protected IPAddress Address { get; set; }
        public IPEndPoint EndPoint { get; protected set; } = new(IPAddress.Loopback, 0);

        public abstract Task<IPAddress> GetAddress(bool verbose = false);
    }
}
