namespace ContainerKiller.Models
{
    public enum DockerResponse
    {
        Ok,
        AlreadyStopped,
        AlreadyStarted,
        NoSuchContainer,
        NoSuchContainerOrNetwork,
        ContainerNotRunning,
        Error
    }
}