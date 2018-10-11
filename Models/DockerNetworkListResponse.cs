namespace ContainerKiller.Models
{
    public class DockerNetworkListResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public object Created { get; set; }
        public string Scope { get; set; }
        public string Driver { get; set; }
        public bool EnableIPv6 { get; set; }
        public bool Internal { get; set; }
        public bool Attachable { get; set; }
        public bool Ingress { get; set; }
    }
}