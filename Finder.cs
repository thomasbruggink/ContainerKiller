using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using ContainerKiller.Exceptions;
using ContainerKiller.Models;

namespace ContainerKiller
{
    class Finder
    {
        public static DockerNetworkInspectResponse DockerNetwork = null;
        public static ConcurrentDictionary<string, string> ContainerIps = new ConcurrentDictionary<string, string>();

        public static string GetIpFor(string service)
        {
            return GetIpForContainer(service);
        }

        public static DockerResponse StartContainer(string id)
        {
            var response = RunDockerCommand($"containers/{id}/start", HttpMethod.Post);
            switch(response.StatusCode)
            {
                case 204:
                    return DockerResponse.Ok;
                case 304:
                    return DockerResponse.AlreadyStarted;
                case 404:
                    return DockerResponse.NoSuchContainer;
                default:
                    return DockerResponse.Error;
            }
        }

        public static DockerResponse StopContainer(string id)
        {
            var response = RunDockerCommand($"containers/{id}/stop?t=120", HttpMethod.Post);
            switch(response.StatusCode)
            {
                case 204:
                    return DockerResponse.Ok;
                case 304:
                    return DockerResponse.AlreadyStopped;
                case 404:
                    return DockerResponse.NoSuchContainer;
                default:
                    return DockerResponse.Error;
            }
        }

        public static DockerResponse KillContainer(string id)
        {
            var response = RunDockerCommand($"containers/{id}/kill?signal=SIGKILL", HttpMethod.Post);
            switch(response.StatusCode)
            {
                case 204:
                    return DockerResponse.Ok;
                case 404:
                    return DockerResponse.NoSuchContainer;
                case 409:
                    return DockerResponse.ContainerNotRunning;
                default:
                    return DockerResponse.Error;
            }
        }

        public static DockerResponse DisconnectContainer(string containerId, string networkId, bool force = true)
        {
            var model = new DockerDisconnectModel
            {
                Container = containerId,
                Force = force
            };
            var response = RunDockerCommand($"networks/{networkId}/disconnect", HttpMethod.Post, JsonSerializer.Serialize(model));
            switch(response.StatusCode)
            {
                case 200:
                    return DockerResponse.Ok;
                case 404:
                    return DockerResponse.NoSuchContainerOrNetwork;
                default:
                    return DockerResponse.Error;
            }
        }

        public static DockerResponse ConnectContainer(string containerId, string networkId, string ipAddress)
        {
            var model = new DockerConnectModel
            {
                Container = containerId,
                EndpointConfig = new EndpointConfig
                {
                    IPAMConfig = new IPAMConfig
                    {
                        IPv4Address = ipAddress
                    }
                }
            };
            var response = RunDockerCommand($"networks/{networkId}/connect", HttpMethod.Post, JsonSerializer.Serialize(model));
            switch(response.StatusCode)
            {
                case 200:
                    return DockerResponse.Ok;
                case 400:
                    return DockerResponse.RestoringNetworkNotAllowedWithDHCPNetwork;
                case 404:
                    return DockerResponse.NoSuchContainerOrNetwork;
                default:
                    return DockerResponse.Error;
            }
        }

        public static List<Container> GetAllContainers()
        {
            return RunDockerCommand<List<Container>>("containers/json?all=1", HttpMethod.Get);
        }

        public static List<Container> GetContainersMatchingImage(string imagename)
        {
            var response = GetAllContainers();
            var matchingContainers = response.Where(c => c.Image.Equals(imagename, StringComparison.InvariantCultureIgnoreCase));
            return matchingContainers.ToList();
        }

        public static List<Container> GetContainersMatching(string name)
        {
            var response = RunDockerCommand<List<Container>>("containers/json?all=1", HttpMethod.Get);
            var matchingContainers = response.Where(c => c.Names.Any(n => n.Equals(name, StringComparison.InvariantCultureIgnoreCase)));
            return matchingContainers.ToList();
        }

        public static string GetDockerNetworkId(string name)
        {
            var response = RunDockerCommand<List<DockerNetworkListResponse>>($"networks?name={name}", HttpMethod.Get).Where(adapter => adapter.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if(!response.Any())
                throw new NetworkNotFoundException($"No network found matching the name '{name}'");
            // Docker sometimes screws me with all network adapters so filter anyway
            return response.First().Id;
        }

        public static string GetDockerSubnet(string name)
        {
            if (DockerNetwork != null)
                return DockerNetwork.IPAM.Config.First().Subnet;
            var networkId = GetDockerNetworkId(name);
            DockerNetwork = RunDockerCommand<DockerNetworkInspectResponse>($"networks/{networkId}", HttpMethod.Get);
            if(!DockerNetwork.IPAM.Config.Any())
                throw new NullReferenceException($"No IPAM config found: '{JsonSerializer.Serialize(DockerNetwork)}' for {name} id '{networkId}'");
            return DockerNetwork.IPAM.Config.First().Subnet;
        }

        public static string GetDockerNatNetworkId()
        {
            var response = RunDockerCommand<List<DockerNetworkListResponse>>("networks?driver=nat", HttpMethod.Get);
            if(!response.Any())
                throw new NullReferenceException("No network found matching the driver 'nat'");
            // Docker sometimes screws me with all network adapters so filter anyway
            return response.First(adapter => adapter.Driver.Equals("nat", StringComparison.InvariantCultureIgnoreCase)).Id;
        }

        public static string GetDockerNatSubnet()
        {
            if (DockerNetwork != null)
                return DockerNetwork.IPAM.Config.First().Subnet;
            var natId = GetDockerNatNetworkId();
            DockerNetwork = RunDockerCommand<DockerNetworkInspectResponse>($"networks/{natId}", HttpMethod.Get);
            if(!DockerNetwork.IPAM.Config.Any())
                throw new NullReferenceException($"No IPAM config found: '{JsonSerializer.Serialize(DockerNetwork)}' for NAT id '{natId}'");
            return DockerNetwork.IPAM.Config.First().Subnet;
        }

        public static string GetDockerNatGateway()
        {
            if (DockerNetwork != null)
                return DockerNetwork.IPAM.Config.First().Gateway;
            var natId = GetDockerNatNetworkId();
            DockerNetwork = RunDockerCommand<DockerNetworkInspectResponse>($"networks/{natId}", HttpMethod.Get);
            return DockerNetwork.IPAM.Config.First().Gateway;
        }

        private static string GetIpForContainer(string imagename)
        {
            if (ContainerIps.ContainsKey(imagename.ToLower()))
                return ContainerIps[imagename.ToLower()];
            var response = RunDockerCommand<List<Container>>("containers/json?all=1", HttpMethod.Get);
            var matchingContainer = response.First(c => c.Image.Contains(imagename.ToLower()));
            //Try to add if it fails we can return the same IPaddress
            ContainerIps.TryAdd(imagename.ToLower(), matchingContainer.NetworkSettings.Networks.First().Value.IPAddress);
            return matchingContainer.NetworkSettings.Networks.First().Value.IPAddress;
        }

        private static T RunDockerCommand<T>(string endpoint, HttpMethod method, string body = null)
        {
            for(var i = 0; i < 10; i++)
            {
                try
                {
                    var httpResult = RunDockerCommand(endpoint, method, body);
                    if (httpResult.StatusCode != 200)
                        throw new Exception($"Unable to call docker engine: {httpResult.Content}");
                    var result = JsonSerializer.Deserialize<T>(httpResult.Content);
                    if(result == null)
                        throw new ArgumentNullException($"Unable to deserialize docker response: '{httpResult.StatusCode}' '{httpResult.Content}' into '{typeof(T)}'");
                    return JsonSerializer.Deserialize<T>(httpResult.Content);
                }
                catch(JsonException)
                {
                    Console.WriteLine($"Error while reading docker response retrying {i}/10");
                    Thread.Sleep(1000);
                    if(i + 1 == 10)
                        throw;
                }
            }
            return default(T);
        }

        private static HttpResponse RunDockerCommand(string endpoint, HttpMethod method, string body = null)
        {
            var unixSocket = "/var/run/docker.sock";
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            var unixEp = new UnixDomainSocketEndPoint(unixSocket);
            socket.Connect(unixEp);
            
            var rawHttpString = $"{method.ToString()} /v1.37/{endpoint} HTTP/1.1\nHost: .\n";
            if(body != null)
            {
                rawHttpString += $"Content-Length: {body.Length}\n";
                rawHttpString += $"Content-Type: application/json\n";
                rawHttpString += "\n";
                rawHttpString += body;
            }
            rawHttpString += "\n";
            socket.Send(Encoding.UTF8.GetBytes(rawHttpString), 0, Encoding.UTF8.GetByteCount(rawHttpString), SocketFlags.None);

            var httpResult = ReadAndParseResponse(socket);
            return httpResult;
        }

        private static HttpResponse ReadAndParseResponse(Socket socket)
        {
            var lineNumber = 0;
            string line;
            var httpResponse = new HttpResponse();
            var contentLength = 0;
            string contentType = null;
            do
            {
                line = ReadLine(socket);
                lineNumber++;

                if (lineNumber == 1)
                {
                    var statusSplit = line.Split(' ');
                    httpResponse.StatusCode = Convert.ToInt32(statusSplit[1]);
                }

                if(line.StartsWith("content-length", StringComparison.CurrentCultureIgnoreCase))
                {
                    contentLength = Convert.ToInt32(line.Split(' ').ToList().Last());
                }
                if(line.StartsWith("content-type", StringComparison.InvariantCultureIgnoreCase))
                {
                    contentType = line;
                }

            } while (line != "");

            if(contentType != null)
            {
                //Docker engine returns the first line as the content length
                if(contentLength == 0)
                    contentLength = Convert.ToInt32(ReadLine(socket), 16);

                var dataLeft = contentLength;
                do
                {
                    var contentDataBytes = new byte[dataLeft];
                    var resultLength = socket.Receive(contentDataBytes, 0, dataLeft, SocketFlags.None);
                    httpResponse.Content += Encoding.ASCII.GetString(contentDataBytes, 0, resultLength);
                    dataLeft = dataLeft - resultLength;
                } while(dataLeft > 0);
            }

            return httpResponse;
        }

        private static string ReadLine(Socket socket)
        {
            var str = "";
            var buffer = new byte[1];
            while (buffer[0] != '\n')
            {
                socket.Receive(buffer, 0, 1, SocketFlags.None);
                str += (char)buffer[0];
            }
            return str.Replace("\n", "").Replace("\r", "");
        }
    }
}
