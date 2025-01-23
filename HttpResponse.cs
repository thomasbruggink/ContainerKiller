using System;

namespace ContainerKiller
{
    internal class HttpResponse
    {
        public int StatusCode { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return $"""
            Statuscode: {StatusCode}
            Content: {Content}
            """;
        }
    }
}
