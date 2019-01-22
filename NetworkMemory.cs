using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ContainerKiller.Models;

namespace ContainerKiller
{
    class NetworkMemory
    {
        private class IpInformation
        {
            public IPAddress Ip { get; set; }

            public IpInformation(IPAddress ip)
            {
                Ip = ip;
            }
        }

        public string NetworkId { get; }
        private readonly Dictionary<string, IpInformation> _containers;

        public NetworkMemory(List<Container> containers, string network)
        {
            _containers = new Dictionary<string, IpInformation>();
            NetworkId = containers.First().NetworkSettings.Networks.First().Value.NetworkID;
            foreach(var container in containers)
            {
                var ip = container.NetworkSettings.Networks.FirstOrDefault().Value?.IPAddress;
                if(ip == null)
                    _containers.Add(container.Id, new IpInformation(null));
                else
                    _containers.Add(container.Id, new IpInformation(IPAddress.Parse(ip)));
            }
            // Generate new ips for missing ips
            var subnet = Finder.GetDockerSubnet(network);
            var ipHelper = new IpHelper(subnet);
            // Get the first IP and convert to int
            var intIp = ipHelper.GetInt(_containers.Where(cip => cip.Value.Ip != null).First().Value.Ip);
            foreach(var container in _containers.Where(c => c.Value.Ip == null))
            {
                for(var i = intIp; i < UInt32.MaxValue; i++)
                {
                    var tempIp = ipHelper.GetIP(i);
                    if(!ipHelper.IsValid(tempIp) || _containers.Any(cip => cip.Value.Ip != null && cip.Value.Ip.Equals(tempIp)))
                        continue;
                    container.Value.Ip = tempIp;
                    break;
                }
            }
        }

        public string GetOriginalIpAddressFor(string containerId)
        {
            if(!_containers.ContainsKey(containerId))
                return null;
            return _containers[containerId].Ip.ToString();
        }
    }
}