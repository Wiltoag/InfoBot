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

        /// <summary>
        /// list of 6 edt channels, sorted by their group
        /// </summary>
        public static DiscordChannel[] EdtChannel { get; private set; }

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
                if (task.Wait(Timeout))
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
            EdtChannel = new DiscordChannel[6];
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557669810077697);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[0] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 1.1");
            }
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557699497623602);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[1] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 1.2");
            }
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557716694138890);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[2] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 2.1");
            }
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557732930289664);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[3] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 2.2");
            }
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557762101542923);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[4] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 3.1");
            }
            {
#if DEBUG
                var task = Discord.GetChannelAsync(437704877221609474);
#else
                var task = Discord.GetChannelAsync(623557780527382530);
#endif
                if (task.Wait(Timeout))
                {
                    EdtChannel[5] = task.Result;
                    Logger.Info($"'{task.Result.Name}' found");
                }
                else
                    Logger.Warning("Unable to find EdtChannel 3.2");
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static async Task Main(string[] args)
        {
            Logger = new Log();
            Timeout = TimeSpan.FromSeconds(15);
            registeredCommands = new LinkedList<ICommand>();
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
            await Task.Delay(-1);
        }

        private static async Task MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Message.Content.StartsWith(Settings.CurrentSettings.commandIdentifier) && !e.Channel.IsPrivate)
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
                await Task.WhenAll(
                                registeredCommands
                                .Where(command => command.Key.ToLower() == args.First.Value.ToLower())
                                .Select(async command =>
                                {
                                    if (!command.Admin || (await DUTInfoServer.GetMemberAsync(e.Author.Id).ConfigureAwait(false)).IsAdmin())
                                    {
                                        Logger.Info($"{command.Key} called by {e.Author.Username}");
                                        await command.Handle(e, args.Where((s, i) => i > 0)).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        Logger.Warning($"{command.Key} canceled for {e.Author.Username} : no admin rights");
                                        await e.Message.RespondAsync("You have to be admin to call this command.");
                                    }
                                }));
            }
        }

        #endregion Private Methods
    }
}