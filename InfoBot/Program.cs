using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;

namespace Infobot
{
    internal static class Program
    {
        #region Private Fields

        private static string token;
        public static ICollection<ICommand> registeredCommands { get; private set; }

        #endregion Private Fields

        #region Public Properties

#if DEBUG
        public static string WildgoatApi => "http://wildgoat.fr/api";
        //public static string WildgoatApi => "http://localhost/Wildgoat_API/api";
#else
        public static string WildgoatApi => "http://wildgoat.fr/api";
#endif

        /// <summary>
        /// Global HTTP client
        /// </summary>
        public static HttpClient Client { get; private set; }

        /// <summary>
        /// Global discord client
        /// </summary>
        public static DiscordClient Discord { get; private set; }

        /// <summary>
        /// The main discord server
        /// </summary>
        public static DiscordGuild DUTInfoServer { get; private set; }

        public static Log Logger { get; private set; }

        /// <summary>
        /// The sharing emoji
        /// </summary>
        public static DiscordEmoji Sharing { get; private set; }

        public static TimeSpan Timeout { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public static void Connect()
        {
            Logger.Info("Connecting to Discord...");
            if (Discord.ConnectAsync().Wait(Timeout))
                Logger.Info($"Connected to Discord");
            else
            {
                Logger.Error($"Failed to connect to Discord");
                Environment.Exit(0);
            }
            {
#if DEBUG
                var task = Discord.GetGuildAsync(437704877221609472);
#else
                var task = Discord.GetGuildAsync(619513574850560010);
#endif
                if (task.Wait(Timeout) && task.IsCompletedSuccessfully)
                {
                    DUTInfoServer = task.Result;
                    Logger.Info($"Connected to '{DUTInfoServer.Name}'");
                }
                else
                {
                    Logger.Error($"Failed to connect to the server");
                    Environment.Exit(0);
                }
            }
            if (Discord.UpdateStatusAsync(new DiscordGame(Settings.CurrentSettings.status)).Wait(Timeout))
                Logger.Info("Status set");
            else
                Logger.Warning("Unable to set status");
            Sharing = DUTInfoServer.Emojis.FirstOrDefault((e) => e.Name == "partage");
            if (Sharing == null)
                Logger.Warning("Sharing emote not found");
        }

        #endregion Public Methods

        #region Private Methods

        private static async Task Main(string[] args)
        {
            Logger = new Log();
            Timeout = TimeSpan.FromSeconds(15);
            registeredCommands = new HashSet<ICommand>(new ICommand.Comparer());
            Settings.CurrentSettings = SettingsManager.MostRecent;
            Client = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = false
            });
            try
            {
                using (var sr = new StreamReader("token.txt"))
                    token = sr.ReadToEnd();
                Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
            }
            catch (Exception)
            {
                Console.Write("Token :");
                token = Console.ReadLine();
                Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
            }
            Connect();
            Discord.MessageCreated += MessageCreated;
            SettingsManager.Setup();
            UpdateTimetable.Setup();
            registeredCommands.Add(new Padoru());
            registeredCommands.Add(new Help());
            registeredCommands.Add(new UpdateTimetable());
            await Task.Delay(-1);
        }

        private static async Task MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Author.IsCurrent && !e.Channel.IsPrivate && e.Message.Content.StartsWith(Settings.CurrentSettings.commandIdentifier))
            {
                var content = e.Message.Content.Substring(Settings.CurrentSettings.commandIdentifier.Length);
                var args = new LinkedList<string>();
                var quoted = false;
                var currArg = "";
                foreach (var c in content)
                {
                    if (c == ' ' && !quoted)
                    {
                        if (currArg.Length > 0)
                        {
                            args.AddLast(currArg);
                            currArg = "";
                        }
                    }
                    else if (c == '"')
                        quoted = !quoted;
                    else
                        currArg += c;
                }
                if (currArg.Length > 0)
                    args.AddLast(currArg);
                var commands = registeredCommands
                                .Where(command => command.Key.ToLower() == args.First.Value.ToLower());
                if (commands.Any())
                    foreach (var command in commands)
                    {
                        var memberTask = DUTInfoServer.GetMemberAsync(e.Author.Id);
                        if ((await Task.WhenAny(memberTask, Task.Delay(Timeout)).ConfigureAwait(false)) == memberTask && memberTask.IsCompletedSuccessfully)
                            if (!command.Admin || memberTask.Result.IsAdmin())
                            {
                                Logger.Info($"{command.Key} called by {e.Author.Username}");
                                await command.Handle(e, args.Where((s, i) => i > 0));
                            }
                            else
                            {
                                Logger.Warning($"{command.Key} canceled for {e.Author.Username} : no admin rights");
                                await e.Message.RespondAsync("You have to be admin to call this command.").ConfigureAwait(false);
                            }
                        else
                            Logger.Warning($"Unable to find member rights");
                    }
                else
                    await e.Message.RespondAsync($"Unknown command `{args.First.Value}`, type `{Settings.CurrentSettings.commandIdentifier}help` for more informations").ConfigureAwait(false);
            }
        }

        #endregion Private Methods
    }
}