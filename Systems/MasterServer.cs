using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lolibase.Discord.Utils;
using Newtonsoft.Json;

namespace Lolibase.Discord.Systems
{
    public class MasterServer : IApplyToClient, IApplicableSystem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }

        public void Activate()
        {
            Active = true;
            Name = "Master Server Subsystem";
            Description = "Protects bot against unauthorized acess to staff commands";
        }
        public void ApplyToClient(DiscordClient client)
        {
            if (Active)
            {
                client.GuildDownloadCompleted += async delegate (GuildDownloadCompletedEventArgs args)
                           {

                               if (Program.Config.MasterId == 0)
                               {
                                   if (client.Guilds.Count > 1)
                                   {
                                       var names = client.Guilds.Aggregate("", (current, guild) => current + $"{guild.Value.Name}, ");

                                       foreach (KeyValuePair<ulong, DiscordGuild> guild in client.Guilds)
                                       {
                                           var ch = guild.Value.GetDefaultChannel();
                                           var builder = new DiscordEmbedBuilder();

                                           builder
                                               .AddField("List of Servers", names)
                                               .WithDescription($"Bot is loaded on multiple servers, please use {client.CurrentUser.Mention}setMaster on the MASTER guild.")
                                               .WithAuthor("Error on determining MASTER server")
                                               .WithColor(DiscordColor.Red);
                                           await ch.SendMessageAsync(embed: builder);
                                       }

                                   }
                                   else
                                   {
                                       var ch = client.Guilds.Values.ToList()[0].GetDefaultChannel();
                                       var builder = new DiscordEmbedBuilder();
                                       builder
                                           .WithAuthor("Guild set as MASTER guild")
                                           .WithColor(DiscordColor.PhthaloGreen);
                                       Program.Config.MasterId = client.Guilds.Values.ToList()[0].Id;
                                       File.WriteAllText(Directory.GetCurrentDirectory() + "Config.json", JsonConvert.SerializeObject(Program.Config, Formatting.Indented));
                                   }
                               }
                           };
            }

        }
        public void Deactivate()
        {
            Active = false;
        }
    }
}