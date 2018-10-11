using System;
using System.Collections.Generic;
using System.Linq;
using ContainerKiller.Models;
using Microsoft.Extensions.Configuration;

namespace ContainerKiller
{
    enum ContainerAction
    {
        Stop,
        Kill,
        NetworkDown
    }

    class Program
    {
        private static ContainerAction CurrentMode;
        private static List<Container> Containers;
        private static KillerConfig Config;
        private static NetworkMemory NetworkMemory;

        static void Main(string[] args)
        {
            var configurationRoot = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("config.json").Build();
            Config = new KillerConfig
            {
                ImageName = configurationRoot.GetSection("ImageName").Value,
                ExpectedNetwork = configurationRoot.GetSection("ExpectedNetwork").Value
            };

            CurrentMode = ContainerAction.Kill;
            Containers = GetContainers();
            NetworkMemory = new NetworkMemory(Containers, Config.ExpectedNetwork);

            ReDraw();
            char input = ' ';
            do
            {
                input = Console.ReadKey().KeyChar;
                Console.WriteLine("");
                Containers = GetContainers();
                if(int.TryParse(input.ToString(), out var id))
                {
                    id--;
                    if(id < 0 || id > Containers.Count)
                    {
                        Write("Invalid id");
                        continue;
                    }
                    var container = Containers[id];
                    DockerResponse result;
                    if(container.State == "running")
                    {
                        switch(CurrentMode)
                        {
                            case ContainerAction.Kill:
                                result = Finder.KillContainer(container.Id);
                                break;
                            case ContainerAction.Stop:
                                result = Finder.StopContainer(container.Id);
                                break;
                            case ContainerAction.NetworkDown:
                            {
                                var network = container.NetworkSettings.Networks.FirstOrDefault();
                                if(network.Key != null && network.Key.Equals(Config.ExpectedNetwork, StringComparison.InvariantCultureIgnoreCase))
                                    result = Finder.DisconnectContainer(container.Id, network.Value.NetworkID);
                                else
                                    result = Finder.ConnectContainer(container.Id, NetworkMemory.NetworkId, NetworkMemory.GetOriginalIpAddressFor(container.Id));
                                break;
                            }
                            default:
                                result = DockerResponse.Ok;
                                break;
                        }
                    }
                    else if(container.State == "stopped" || container.State == "exited")
                        result = Finder.StartContainer(container.Id);
                    else
                    {
                        Containers = GetContainers();
                        Write($"Unknown state: {container.State}");
                        continue;
                    }
                    Containers = GetContainers();
                    if(result == DockerResponse.Ok)
                        ReDraw();
                    else
                        Write($"Result: {result.ToString()}");
                }
                else
                {
                    switch(input)
                    {
                        case 'r':
                        {
                            ReDraw();
                            break;
                        }
                        case 's':
                        {
                            CurrentMode = ContainerAction.Stop;
                            ReDraw();
                            break;
                        }
                        case 'k':
                        {
                            CurrentMode = ContainerAction.Kill;
                            ReDraw();
                            break;
                        }
                        case 'n':
                        {
                            CurrentMode = ContainerAction.NetworkDown;
                            ReDraw();
                            break;
                        }
                        default:
                        {
                            Write("Unknown command");
                            break;
                        }
                    }
                }
            } while(input != 'q');
        }

        private static void Write(string text)
        {
            Console.WriteLine($"{text}");
            Console.ReadLine();
            Console.Clear();
            ReDraw();
        }

        private static List<Container> GetContainers()
        {
            var containers = Finder.GetContainersMatchingImage(Config.ImageName);
            return containers.OrderBy(c => c.Names.First()).ToList();
        }

        private static void ReDraw()
        {
            Console.Clear();
            var index = 1;
            foreach (var container in Containers)
            {
                Console.Write($"{index}: {container.Names.Last()} - ");
                if(container.State.Equals("running"))
                {
                    if(container.NetworkSettings.Networks.Any())
                        // All good
                        Console.WriteLine("♡");
                    else
                        // Network segmentation
                        Console.WriteLine("⌁");
                }
                else
                    // Container dead
                    Console.WriteLine("☠");
                index++;
            }
            Console.WriteLine($"Execution mode: {CurrentMode.ToString()}");
        }
    }
}
