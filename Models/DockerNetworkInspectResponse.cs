namespace ContainerKiller.Models
{
    public class DockerNetworkInspectResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Created { get; set; }
        public string Scope { get; set; }
        public string Driver { get; set; }
        public bool EnableIPv6 { get; set; }
        public IPAM IPAM { get; set; }
        public bool Internal { get; set; }
        public bool Attachable { get; set; }
        public bool Ingress { get; set; }
        public Options1 Options { get; set; }
    }
}