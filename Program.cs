﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Lolibase.Discord;
using Lolibase.Discord.Utils;
using Lolibase.Objects;
using Newtonsoft.Json;
using static System.Reflection.Assembly;

namespace Lolibase.Discord
{
    public class Program
    {
        public static DiscordClient Client { get; set; }
        public static CommandsNextExtension CommandsNext { get; set; }
        public static InteractivityExtension Interactivity { get; set; }
        public static Config Config { get; set; }
        public static List<Type> systems = new List<Type>();
        public static Program Instance { get; set; }
        public static List<Pair> pairs { get; set; }
        static void Main(string[] args)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "/Pairs.json"))
            {
                pairs = JsonConvert.DeserializeObject<List<Pair>>(File.ReadAllText(Directory.GetCurrentDirectory() + "/Pairs.json"));
            }
            else
            {
                pairs = new List<Pair>();
                File.WriteAllText(Directory.GetCurrentDirectory() + "/Pairs.json", JsonConvert.SerializeObject(pairs, Formatting.Indented));
            }

            if (File.Exists(Directory.GetCurrentDirectory() + "/Config.json"))
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Directory.GetCurrentDirectory() + "/Config.json"));
                systems = GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IApplicableSystem))).ToList();
                Instance = new Program();

                Init().GetAwaiter().GetResult();
            }
            else
            {
                Config = TUI_cfg();
                File.WriteAllText(Directory.GetCurrentDirectory() + "/Config.json", JsonConvert.SerializeObject(Config, Formatting.Indented));
                systems = GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IApplicableSystem))).ToList();
                Instance = new Program();

                Init().GetAwaiter().GetResult();
            }


        }

        private Program()
        {
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token,
                AutoReconnect = false,
                UseInternalLogHandler = false,
                TokenType = TokenType.Bot
            });

            CommandsNext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                PrefixResolver = PrefixResolver,
                EnableDefaultHelp = false,
                EnableDms = true
            });

            Interactivity = Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                Timeout = TimeSpan.FromMinutes(10),
                PaginationDeletion = PaginationDeletion.DeleteEmojis
            });
            foreach (var system in systems)
            {
                if (system.GetInterfaces().Contains(typeof(IApplyToInteractivity)))
                {
                    var instance = (IApplyToInteractivity)Activator.CreateInstance(system);
                    instance.ApplyToInteractivity(Interactivity);
                    Console.WriteLine($"[System] {system.Name} Loaded");
                }
                else if (system.GetInterfaces().Contains(typeof(IApplyToClient)))
                {
                    var instance = (IApplyToClient)Activator.CreateInstance(system);
                    instance.Activate();
                    instance.ApplyToClient(Client);
                    Console.WriteLine($"[System] {instance.Name} Loaded \n\tDescription : {instance.Description}");
                }
            }
            CommandsNext.RegisterCommands(GetExecutingAssembly());

        }
        private static Config TUI_cfg()
        {
            var c = new Config();
            Console.WriteLine("Please Input the TOKEN :");
            c.Token = Console.ReadLine();
            c.MasterId = 0;
            c.PuppetId = new List<ulong?>();
            c.Prefixes = new List<string>();
            Console.WriteLine("Configuration Completed.");
            return c;
        }

        private static async Task Init()
        {
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

#pragma warning disable CS1998
        private async Task<int> PrefixResolver(DiscordMessage msg)
        {
            switch (msg.GetMentionPrefixLength(Client.CurrentUser))
            {
                case -1:
                    int x;
                    foreach (var prefix in Config.Prefixes)
                    {
                        x = msg.GetStringPrefixLength(prefix);
                        if (x != -1)
                            return x;
                    }

                    break;
                default:
                    return msg.GetMentionPrefixLength(Client.CurrentUser);
            }

            return -1;
        }
    }
}