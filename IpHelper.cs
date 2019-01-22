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
            bytes = FlipEndianness(bytes);                
            return BitConverter.ToUInt32(bytes);
        }

        public IPAddress GetIP(uint iPAddress)
        {
            var bytes = BitConverter.GetBytes(iPAddress);
            bytes = FlipEndianness(bytes);
            return new IPAddress(bytes);
        }

        private static byte[] FlipEndianness(byte[] input)
        {
            if(!BitConverter.IsLittleEndian)
                return input;
            var output = new byte[input.Length];
            Buffer.BlockCopy(input, 0, output, 3, 1);
            Buffer.BlockCopy(input, 1, output, 2, 1);
            Buffer.BlockCopy(input, 2, output, 1, 1);
            Buffer.BlockCopy(input, 3, output, 0, 1);
            return output;
        }
    }
}