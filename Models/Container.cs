namespace ContainerKiller.Models
{
    internal class Container
    {
        public string Id { get; set; }
        public string[] Names { get; set; }
        public string Image { get; set; }
        public string ImageID { get; set; }
        public string Command { get; set; }
        public int Created { get; set; }
        public Port[] Ports { get; set; }
        public Labels Labels { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public Hostconfig HostConfig { get; set; }
        public Networksettings NetworkSettings { get; set; }
        public object[] Mounts { get; set; }
    }
}