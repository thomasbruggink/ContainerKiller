namespace ContainerKiller.Models
{
    public class DockerDisconnectModel
    {
        public string Container { get; set; }
        public bool Force { get; set; }
    }
}