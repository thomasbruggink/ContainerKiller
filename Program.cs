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
        private static int CurrentIndex = 1;

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
                var key = Console.ReadKey();
                input = key.KeyChar;
                Console.WriteLine("");
                Containers = GetContainers();
                if(int.TryParse(input.ToString(), out var id))
                {
                    id--;
                    ExecuteCommand(id);
                }
                else
                {
                    switch(key.Key)
                    {
                        case ConsoleKey.R:
                        {
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.S:
                        {
                            CurrentMode = ContainerAction.Stop;
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.K:
                        {
                            CurrentMode = ContainerAction.Kill;
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.N:
                        {
                            CurrentMode = ContainerAction.NetworkDown;
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.UpArrow:
                        {
                            if(CurrentIndex > 1)
                                CurrentIndex--;
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.DownArrow:
                        {
                            if(CurrentIndex < Containers.Count)
                                CurrentIndex++;
                            ReDraw();
                            break;
                        }
                        case ConsoleKey.Spacebar:
                        {
                            ExecuteCommand(CurrentIndex - 1);
                            ReDraw();
                            break;
                        }
                        default:
                        {
                            Write($"Unknown command: {input}");
                            break;
                        }
                    }
                }
            } while(input != 'q');
        }

        private static void ExecuteCommand(int id)
        {
            if(id < 0 || id > Containers.Count)
            {
                Write("Invalid id");
                return;
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
                return;
            }
            Containers = GetContainers();
            if(result == DockerResponse.Ok)
                ReDraw();
            else
                Write($"Result: {result.ToString()}");
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
            var containers = Config.ImageName.Equals("*") ? Finder.GetAllContainers() : Finder.GetContainersMatchingImage(Config.ImageName);
            return containers.OrderBy(c => c.Names.First()).ToList();
        }

        private static void ReDraw()
        {
            Console.Clear();
            var index = 1;
            var spaceCount = Containers.Count.ToString().Length + 1;
            foreach (var container in Containers)
            {
                var itemSpaceCount = spaceCount;
                if(index == CurrentIndex)
                {
                    Console.Write("➤");
                    itemSpaceCount--;
                }
                itemSpaceCount -= index.ToString().Length;
                for(var i = 0; i < itemSpaceCount; i++)
                {
                    Console.Write(" ");
                }
                Console.Write($"{index}: {container.Names.Last()} - ");
                if(container.State.Equals("running"))
                {
                    if(container.NetworkSettings.Networks.Any())
                        // All good
                        Console.Write("♡");
                    else
                        // Network segmentation
                        Console.Write("⌁");
                }
                else
                    // Container dead
                    Console.Write("☠");
                Console.WriteLine();
                index++;
            }
            Console.WriteLine($"Execution mode: {CurrentMode.ToString()}");
        }
    }
}
