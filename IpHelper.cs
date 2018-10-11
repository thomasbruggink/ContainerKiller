using System;
using System.Linq;
using System.Net;

namespace ContainerKiller
{
    class IpHelper
    {
        private IPAddress _networkAddress;
        private IPAddress _defaultGateway;
        private int _cidrRange;

        public IpHelper(string subnet)
        {
            var split = subnet.Split("/");
            _networkAddress = IPAddress.Parse(split.First());
            _defaultGateway = GetIP(GetInt(_networkAddress) + 1);
            _cidrRange = int.Parse(split.Last());
        }

        public bool IsValid(IPAddress address)
        {
            return InSubnet(address) && address != _networkAddress && address != _defaultGateway;
        }

        private bool InSubnet(IPAddress iPAddress)
        {
            var baseAddress = BitConverter.ToUInt32(_networkAddress.GetAddressBytes());
            var address = BitConverter.ToUInt32(iPAddress.GetAddressBytes());
            var mask = IPAddress.HostToNetworkOrder(-1 << (32 - _cidrRange));
            return ((baseAddress & mask) == (address & mask));
        }

        public uint GetInt(IPAddress iPAddress)
        {
            var bytes = iPAddress.GetAddressBytes();
            if(BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt32(bytes);
        }

        public IPAddress GetIP(uint iPAddress)
        {
            var bytes = BitConverter.GetBytes(iPAddress);
            if(BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return new IPAddress(bytes);
        }
    }
}