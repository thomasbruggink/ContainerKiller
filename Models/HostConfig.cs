using System.Collections.Generic;

namespace ContainerKiller.Models
{
    internal class Hostconfig
    {
        public string NetworkMode { get; set; }
        public List<BpsLimit> BlkioDeviceWriteBps { get; set; }
        public List<BpsLimit> BlkioDeviceReadBps { get; set; }
    }
}
