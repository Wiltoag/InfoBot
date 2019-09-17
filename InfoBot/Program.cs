using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Ical.Net;

namespace InfoBot
{
    internal partial class Program
    {
        #region Private Fields

        private static WebClient Client;
        private static ConsoleColor DefaultColor;

        private static DiscordGuild DUTInfoServer;
        private static DiscordChannel[] Edt;
        private static DiscordGuild TestServer;

        #endregion Private Fields

        #region Private Properties

        private static string[] CalendarUrl { get; set; }

        private static DiscordClient Discord { get; set; }

        private static Dispatcher Dispatcher { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static async Task AsyncMain(string[] args)
        {
            CalendarUrl = new string[8];
            CalendarUrl[0] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=e81e5e310001831"; //1.1
            CalendarUrl[1] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=4352c5485001785"; //1.2
            CalendarUrl[2] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=329314450001800"; //2.1
            CalendarUrl[3] = ""; //2.2
            CalendarUrl[4] = ""; //3.1
            CalendarUrl[5] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=b4a52df5e501843"; //3.2
            CalendarUrl[6] = ""; //4.1
            CalendarUrl[7] = ""; //4.2
            Client = new WebClient();
            DefaultColor = Console.ForegroundColor;
            string token;
            Console.Write("Token :");
            token = Console.ReadLine();
            Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
            Console.WriteLine("Connecting...");
            if (!ExecuteAsyncMethod(() => Discord.ConnectAsync()))
                Environment.Exit(0);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connected");
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine("Initalization...");
            ///////////////////////////////////////

            Dispatcher = new Dispatcher();
            var consoleThread = new Thread(ConsoleManager);
            if (!ExecuteAsyncMethod(() => Discord.GetGuildAsync(619513574850560010), out DUTInfoServer))
                DUTInfoServer = null;
            ExecuteAsyncMethod(() => Discord.GetGuildAsync(437704877221609472), out TestServer);
            InitCommands();
            ExecuteAsyncMethod(() => Discord.UpdateStatusAsync(new DiscordGame(">ib help")));
            LoadData();
            ExecuteAsyncMethod(async () =>
            {
                Edt[0] = await Discord.GetChannelAsync(623557669810077697);
                Edt[1] = await Discord.GetChannelAsync(623557699497623602);
                Edt[2] = await Discord.GetChannelAsync(623557716694138890);
                Edt[3] = await Discord.GetChannelAsync(623557732930289664);
                Edt[4] = await Discord.GetChannelAsync(623557762101542923);
                Edt[5] = await Discord.GetChannelAsync(623557780527382530);
                Edt[6] = await Discord.GetChannelAsync(623557795660431390);
                Edt[7] = await Discord.GetChannelAsync(623557977223200798);
            });
            //debug
            ExecuteAsyncMethod(async () =>
            {
                Edt[0] = await Discord.GetChannelAsync(437704877221609474);
                Edt[1] = await Discord.GetChannelAsync(437704877221609474);
                Edt[2] = await Discord.GetChannelAsync(437704877221609474);
                Edt[3] = await Discord.GetChannelAsync(437704877221609474);
                Edt[4] = await Discord.GetChannelAsync(437704877221609474);
                Edt[5] = await Discord.GetChannelAsync(437704877221609474);
                Edt[6] = await Discord.GetChannelAsync(437704877221609474);
                Edt[7] = await Discord.GetChannelAsync(437704877221609474);
            });

            ///////////////////////////////////////
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Initialized");
            Console.ForegroundColor = DefaultColor;
            consoleThread.Start();

            DateTimeOffset lastListCheck = DateTimeOffset.UnixEpoch;
            DateTimeOffset lastCalendarCheck = DateTimeOffset.UnixEpoch;

            while (true)
            {
                var next = Dispatcher.GetNext();
                if (next != null)
                    ExecuteAsyncMethod(next);
                if (lastListCheck + TimeSpan.FromSeconds(20) < DateTimeOffset.Now)
                {
                    lastListCheck = DateTimeOffset.Now;
                    ExecuteAsyncMethod(UpdateLists);
                }
                if (lastCalendarCheck + TimeSpan.FromHours(2) < DateTimeOffset.Now)
                {
                    lastCalendarCheck = DateTimeOffset.Now;
                    ExecuteAsyncMethod(async () => UpdateCalendars());
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        private static void ConsoleManager()
        {
            while (true)
            {
                Console.Write('>');
                var input = Console.ReadLine();
                ParseInput(input, out string command, out string[] args);
                try
                {
                    switch (command)
                    {
                        case "help":
                            Console.WriteLine("quit : close the bot.");
                            Console.WriteLine("reconnect : reconnect the bot.");
                            Console.WriteLine("help : display this info.");
                            Console.WriteLine("disp <channel> <message> : send a message to a channel.");
                            break;

                        case "quit":
                            Environment.Exit(0);
                            break;

                        case "disp":
                            DiscordChannel chan;
                            if (!ExecuteAsyncMethod(() => Discord.GetChannelAsync(ulong.Parse(args[0])), out chan))
                                break;
                            ExecuteAsyncMethod(() => chan.SendMessageAsync(args[1]));
                            break;

                        case "reconnect":
                            Console.WriteLine("Connecting...");
                            if (ExecuteAsyncMethod(() => Discord.ReconnectAsync()))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Connected");
                                Console.ForegroundColor = DefaultColor;
                            }
                            break;

                        default:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Command not recognized, type \"help\"");
                            Console.ForegroundColor = DefaultColor;
                            break;
                    }
                }
                catch (Exception e)
                {
                }
            }
        }

        private static bool ExecuteAsyncMethod(Func<Task> func)
        {
            try
            {
                if (!func().Wait(TimeSpan.FromSeconds(10)))
                    throw new Exception("10 sec timeout passed, command canceled");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fatal error : " + e.ToString());
                Console.ForegroundColor = DefaultColor;
                return false;
            }
            return true;
        }

        private static bool ExecuteAsyncMethod<T>(Func<Task<T>> func, out T returnValue)
        {
            try
            {
                var task = func();
                if (!task.Wait(TimeSpan.FromSeconds(10)))
                    throw new Exception("10 sec timeout passed, command canceled");
                returnValue = task.Result;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fatal error : " + e.ToString());
                Console.ForegroundColor = DefaultColor;
                returnValue = default(T);
                return false;
            }
            return true;
        }

        private static void LoadData()
        {
            if (!File.Exists("data.json"))
            {
                var file = new StreamWriter("data.json");
                var defaultObj = new Save();
                defaultObj.votes = new Vote[0];
                defaultObj.polls = new Poll[0];
                file.Write(JsonConvert.SerializeObject(defaultObj));
                file.Close();
            }
            var stream = new StreamReader("data.json");
            var obj = JsonConvert.DeserializeObject<Save>(stream.ReadToEnd());
            stream.Close();

            foreach (var item in obj.votes)
            {
                var vote = new VoteMessage();
                vote.Lifetime = item.duration;
                vote.ShowUsers = item.showUsers;
                DiscordChannel chan;
                ExecuteAsyncMethod(() => Discord.GetChannelAsync(item.message.channel), out chan);
                DiscordMessage mess;
                ExecuteAsyncMethod(() => chan.GetMessageAsync(item.message.id), out mess);
                vote.Message = mess;
                Votes.Add(vote);
            }
            foreach (var item in obj.polls)
            {
                var poll = new PollMessage();
                poll.Lifetime = item.duration;
                poll.ShowUsers = item.showUsers;
                DiscordChannel chan;
                ExecuteAsyncMethod(() => Discord.GetChannelAsync(item.message.channel), out chan);
                DiscordMessage mess;
                ExecuteAsyncMethod(() => chan.GetMessageAsync(item.message.id), out mess);
                poll.Message = mess;
                poll.Open = item.open;
                poll.Choices = new List<Tuple<string, DiscordEmoji>>();
                foreach (var item2 in item.choices)
                    poll.Choices.Add(new Tuple<string, DiscordEmoji>(item2.name, DiscordEmoji.FromGuildEmote(Discord, item2.id)));

                Polls.Add(poll);
            }
        }

        private static void Main(string[] args)
        {
            Task.Run(() => AsyncMain(args)).GetAwaiter().GetResult();
        }

        private static void ParseInput(string input, out string command, out string[] args)
        {
            command = "";
            var listArgs = new List<string>();
            int index = 0;
            while (index < input.Length)
            {
                var currChar = input[index];
                if (currChar == ' ')
                    break;
                else
                    command += currChar;
                index++;
            }
            index++;
            var currArg = "";
            bool ignoreSpaces = false;
            while (index < input.Length)
            {
                var currChar = input[index];
                if (currChar == '\\' && input.Length > index + 1)
                {
                    currChar = input[++index];
                    currArg += currChar;
                }
                else if (currChar == ' ' && !ignoreSpaces)
                {
                    if (currArg.Length > 0)
                        listArgs.Add(currArg);
                    currArg = "";
                }
                else if (currChar == '"')
                    ignoreSpaces = !ignoreSpaces;
                else
                    currArg += currChar;
                index++;
            }
            if (currArg.Length > 0)
                listArgs.Add(currArg);
            args = listArgs.ToArray();
        }

        private static void SaveData()
        {
            var obj = new Save();
            List<Vote> v = new List<Vote>();
            foreach (var item in Votes)
                v.Add(new Vote()
                {
                    duration = item.Lifetime,
                    showUsers = item.ShowUsers,
                    message = new SpecialMessage() { channel = item.Message.ChannelId, id = item.Message.Id }
                });
            obj.votes = v.ToArray();
            List<Poll> p = new List<Poll>();
            foreach (var item in Polls)
            {
                List<Poll.Choice> choices = new List<Poll.Choice>();
                foreach (var item2 in item.Choices)
                    choices.Add(new Poll.Choice() { id = item2.Item2.Id, name = item2.Item1 });

                p.Add(new Poll()
                {
                    duration = item.Lifetime,
                    showUsers = item.ShowUsers,
                    open = item.Open,
                    message = new SpecialMessage() { channel = item.Message.ChannelId, id = item.Message.Id },
                    choices = choices.ToArray()
                });
            }
            obj.polls = p.ToArray();
            var stream = new StreamWriter("data.json");
            stream.Write(JsonConvert.SerializeObject(obj));
            stream.Close();
        }

        private static void UpdateCalendars()
        {
            DateTime mondayThisWeek = DateTime.Today - TimeSpan.FromDays((int)DateTime.Today.DayOfWeek - 1);
            DateTime[] daysToDisplay = new DateTime[]
            {
                mondayThisWeek,
                mondayThisWeek.AddDays(1),
                mondayThisWeek.AddDays(2),
                mondayThisWeek.AddDays(3),
                mondayThisWeek.AddDays(4),
                mondayThisWeek.AddDays(5),
                mondayThisWeek.AddDays(7),
                mondayThisWeek.AddDays(8),
                mondayThisWeek.AddDays(9),
                mondayThisWeek.AddDays(10),
                mondayThisWeek.AddDays(11),
                mondayThisWeek.AddDays(12)
            };
            Console.WriteLine(mondayThisWeek);
            for (int i = 0; i < Edt.Length; i++)
            {
            }
        }

        private static async Task UpdateLists()
        {
            for (int i = Votes.Count - 1; i >= 0; i--)
            {
                var item = Votes[i];
                if (item.Message.CreationTimestamp + item.Lifetime < DateTimeOffset.Now)
                {
                    Votes.RemoveAt(i);
                    StringBuilder content = new StringBuilder();
                    content.Append("**Résultats :** ");
                    content.Append(item.Message.Content + "\n");
                    var upvotes = await item.Message.GetReactionsAsync(Upvote);
                    var downvotes = await item.Message.GetReactionsAsync(Downvote);
                    if (!item.ShowUsers)
                    {
                        content.Append("<" + Upvote.GetDiscordName() + Upvote.Id + "> : **" + (upvotes.Count - 1).ToString() + "**\n");
                        content.Append("<" + Downvote.GetDiscordName() + Downvote.Id + "> : **" + (downvotes.Count - 1).ToString() + "**");
                    }
                    else
                    {
                        content.Append("<" + Upvote.GetDiscordName() + Upvote.Id + "> : **" + (upvotes.Count - 1).ToString() + "** : ");
                        bool first = true;
                        foreach (var user in await item.Message.GetReactionsAsync(Upvote))
                        {
                            if (!user.IsBot)
                            {
                                if (!first)
                                    content.Append(", ");
                                content.Append(user.Mention);
                                first = false;
                            }
                        }
                        content.Append("\n<" + Downvote.GetDiscordName() + Downvote.Id + "> : **" + (downvotes.Count - 1).ToString() + "** : ");
                        first = true;
                        foreach (var user in await item.Message.GetReactionsAsync(Downvote))
                        {
                            if (!user.IsBot)
                            {
                                if (!first)
                                    content.Append(", ");
                                content.Append(user.Mention);
                                first = false;
                            }
                        }
                    }
                    ExecuteAsyncMethod(() => item.Message.Channel.SendMessageAsync(content.ToString()));
                    await item.Message.DeleteAsync();
                }
            }
            for (int i = Polls.Count - 1; i >= 0; i--)
            {
                var item = Polls[i];
                if (item.Message.CreationTimestamp + item.Lifetime < DateTimeOffset.Now)
                {
                    Polls.RemoveAt(i);
                    StringBuilder content = new StringBuilder();
                    content.Append("**Résultats :** ");
                    content.Append(item.Message.Content + "\n\n");
                    if (!item.ShowUsers)
                    {
                        foreach (var choice in item.Choices)
                        {
                            if (choice.Item2.RequireColons)
                                content.Append("\n<" + choice.Item2.GetDiscordName() + choice.Item2.Id + "> : **" + ((await item.Message.GetReactionsAsync(choice.Item2)).Count - 1).ToString() + "**");
                            else
                                content.Append("\n" + choice.Item2.Name + " : **" + ((await item.Message.GetReactionsAsync(choice.Item2)).Count - 1).ToString() + "**");
                        }
                    }
                    else
                    {
                        foreach (var choice in item.Choices)
                        {
                            var users = await item.Message.GetReactionsAsync(choice.Item2);
                            if (choice.Item2.RequireColons)
                                content.Append("\n<" + choice.Item2.GetDiscordName() + choice.Item2.Id + "> : **" + (users.Count - 1).ToString() + "** : ");
                            else
                                content.Append("\n" + choice.Item2.Name + " : **" + (users.Count - 1).ToString() + "** : ");
                            bool first = true;
                            foreach (var user in users)
                            {
                                if (!user.IsBot)
                                {
                                    if (!first)
                                        content.Append(", ");
                                    first = false;
                                    content.Append(user.Mention);
                                }
                            }
                        }
                    }
                    ExecuteAsyncMethod(() => item.Message.Channel.SendMessageAsync(content.ToString()));
                    await item.Message.DeleteAsync();
                }
            }
            SaveData();
        }

        #endregion Private Methods
    }
}