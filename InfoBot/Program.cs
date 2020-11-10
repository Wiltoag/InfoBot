using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        private static ICollection<ISetup> registeredSetups;

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

        public static Log Logger { get; private set; }

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
                Logger.Fatal($"Failed to connect to Discord");
                Environment.Exit(3);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static async Task Main(string[] args)
        {
            Logger = new Log();
            try
            {
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    if (Discord.DisconnectAsync().Wait(Timeout))
                        Logger.Info("Disconnected");
                };
                Timeout = TimeSpan.FromSeconds(15);
                registeredCommands = new HashSet<ICommand>(new ICommand.Comparer());
                registeredSetups = new LinkedList<ISetup>();
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
                Discord.MessageCreated += MessageCreated;
                RegisterCommands();
                RegisterSetups();
                foreach (var setup in registeredSetups)
                    setup.Setup();
                Discord.Ready += async (e) =>
                {
                    var task = Discord.UpdateStatusAsync(new DiscordGame(Settings.CurrentSettings.status));
                    if (await task.TimeoutTask())
                        Logger.Info("Status set");
                    else
                        Logger.Error("Unable to set status");
                    foreach (var setup in registeredSetups)
                        setup.Connected();
                };
                Discord.GuildAvailable += (e) =>
                {
                    Logger.Info($"'{e.Guild.Name}' available");
                    return Task.CompletedTask;
                };
                Discord.ClientErrored += (e) =>
                {
                    Logger.Fatal(e.Exception.InnerException.Message);
                    Logger.Fatal(e.Exception.InnerException);
                    Environment.Exit(4);
                    return Task.CompletedTask;
                };
                Connect();
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Logger.Fatal(e.InnerException.Message);
                Logger.Fatal(e.InnerException);
                Environment.Exit(4);
            }
        }

        private static void RegisterCommands()
            => Assembly.GetExecutingAssembly().DefinedTypes
            .Where(type => type.ImplementedInterfaces.Contains(typeof(ICommand)))
            .ForEach(type =>
            {
                var command = type.GetConstructor(new Type[0]).Invoke(null) as ICommand;
                registeredCommands.Add(command);
                Logger.Info($"Command '{command.GetType()}' registered, with key '{command.Key}'");
            });

        private static void RegisterSetups()
            => Assembly.GetExecutingAssembly().DefinedTypes
            .Where(type => type.ImplementedInterfaces.Contains(typeof(ISetup)))
            .ForEach(type =>
            {
                ISetup setup;
                if (type.ImplementedInterfaces.Contains(typeof(ICommand)))
                    registeredSetups.Add(setup = registeredCommands.First(c => c.GetType() == type) as ISetup);
                else
                    registeredSetups.Add(setup = type.GetConstructor(new Type[0]).Invoke(null) as ISetup);
                Logger.Info($"Setup '{setup.GetType()}' registered");
            });

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
                var sendArgs = args.Skip(1);
                bool autoRemoveCommand = false;
                if (sendArgs.Any())
                {
                    autoRemoveCommand = args.Last.Value == "--remove";
                    if (autoRemoveCommand)
                        sendArgs = sendArgs.SkipLast(1);
                }
                if (commands.Any())
                    foreach (var command in commands)
                    {
                        var memberTask = e.Guild.GetMemberAsync(e.Author.Id);
                        if (await memberTask.TimeoutTask())
                            if (!command.Admin || memberTask.Result.IsAdmin())
                            {
                                Logger.Info($"'{command.Key}' called by '{e.Author.Username}'");
                                await command.Handle(e, sendArgs);
                                if (autoRemoveCommand)
                                {
                                    var removeTask = e.Message.DeleteAsync();
                                    if (!await removeTask.TimeoutTask())
                                        Logger.Error($"Unable to remove the command");
                                }
                            }
                            else
                            {
                                Logger.Warning($"'{command.Key}' canceled for '{e.Author.Username}' : no admin rights");
                                await e.Message.RespondAsync("You have to be admin to call this command.");
                            }
                        else
                            Logger.Error($"Unable to find member rights");
                    }
                else
                    await e.Message.RespondAsync($"Unknown command `{args.First.Value}`, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key}` for more informations");
            }
        }

        #endregion Private Methods
    }
}