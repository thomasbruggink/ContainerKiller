using System;

namespace ContainerKiller.Exceptions
{
    public class NetworkNotFoundException : Exception
    {
        public NetworkNotFoundException(String message) : base(message)
        {
        }
    }
}