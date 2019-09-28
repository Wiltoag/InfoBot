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
using Ical.Net.CalendarComponents;

namespace InfoBot
{
    internal partial class Program
    {
        #region Private Fields

        private const bool DEBUG = false;
        private static List<Autorun> Autoruns;
        private static WebClient Client;
        private static ConsoleColor DefaultColor;
        private static DiscordGuild DUTInfoServer;
        private static DiscordChannel[] EdtChannel;
        private static List<EdtDayMessage>[] EdTMessages;
        private static DateTime LastSaveDate;
        private static int[] OldICalHash;
        private static int RevengeCurrLine;
        private static string[] revengeLines;
        private static List<SavedPoll> SavedPolls;
        private static List<SavedVote> SavedVotes;
        private static DiscordGuild TestServer;
        private static DiscordRole[] TPRoles;

        #endregion Private Fields

        #region Private Properties

        private static string[] CalendarUrl { get; set; }

        private static DiscordClient Discord { get; set; }

        private static Dispatcher Dispatcher { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static async Task AsyncMain(string[] args)
        {
            RevengeCurrLine = 0;
            using (var stream = new StreamReader("revenge.txt"))
            {
                var lines = new List<string>();
                bool end = false;
                while (!end)
                {
                    var line = stream.ReadLine();
                    if (line == null)
                        end = true;
                    else
                        lines.Add(line);
                }
                revengeLines = lines.ToArray();
            }
            CalendarUrl = new string[8];
            TPRoles = new DiscordRole[8];
            CalendarUrl[0] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=e81e5e310001831"; //1.1
            CalendarUrl[1] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=4352c5485001785"; //1.2
            CalendarUrl[2] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=329314450001800"; //2.1
            CalendarUrl[3] = ""; //2.2
            CalendarUrl[4] = ""; //3.1
            CalendarUrl[5] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=b4a52df5e501843"; //3.2
            CalendarUrl[6] = ""; //4.1
            CalendarUrl[7] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=3a1ee9527101771"; //4.2
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

            Random = new Random();
            Dispatcher = new Dispatcher();
            var consoleThread = new Thread(ConsoleManager);
            if (!ExecuteAsyncMethod(() => Discord.GetGuildAsync(619513574850560010), out DUTInfoServer))
                DUTInfoServer = null;
            ExecuteAsyncMethod(() => Discord.GetGuildAsync(437704877221609472), out TestServer);
            InitCommands();
            ExecuteAsyncMethod(() => Discord.UpdateStatusAsync(new DiscordGame(">ib help")));
            LoadData();
            EdtChannel = new DiscordChannel[8];
            ExecuteAsyncMethod(async () =>
            {
                EdtChannel[0] = await Discord.GetChannelAsync(623557669810077697);
                EdtChannel[1] = await Discord.GetChannelAsync(623557699497623602);
                EdtChannel[2] = await Discord.GetChannelAsync(623557716694138890);
                EdtChannel[3] = await Discord.GetChannelAsync(623557732930289664);
                EdtChannel[4] = await Discord.GetChannelAsync(623557762101542923);
                EdtChannel[5] = await Discord.GetChannelAsync(623557780527382530);
                EdtChannel[6] = await Discord.GetChannelAsync(623557795660431390);
                EdtChannel[7] = await Discord.GetChannelAsync(623557977223200798);

                TPRoles[0] = DUTInfoServer.GetRole(619949947042791472);
                TPRoles[1] = DUTInfoServer.GetRole(619949993096118292);
                TPRoles[2] = DUTInfoServer.GetRole(619950016630358086);
                TPRoles[3] = DUTInfoServer.GetRole(619950035895058432);
                TPRoles[4] = DUTInfoServer.GetRole(619950048519913492);
                TPRoles[5] = DUTInfoServer.GetRole(619950099807862794);
                TPRoles[6] = DUTInfoServer.GetRole(619950111300255774);
                TPRoles[7] = DUTInfoServer.GetRole(619950125573472262);
            });

            if (DEBUG)
                ExecuteAsyncMethod(async () =>
                {
                    EdtChannel[0] = await Discord.GetChannelAsync(623874313539289125);
                    EdtChannel[1] = EdtChannel[0];
                    EdtChannel[2] = EdtChannel[0];
                    EdtChannel[3] = EdtChannel[0];
                    EdtChannel[4] = EdtChannel[0];
                    EdtChannel[5] = EdtChannel[0];
                    EdtChannel[6] = EdtChannel[0];
                    EdtChannel[7] = EdtChannel[0];

                    TPRoles[0] = DUTInfoServer.GetRole(623874435845324853);
                    TPRoles[1] = TPRoles[0];
                    TPRoles[2] = TPRoles[0];
                    TPRoles[3] = TPRoles[0];
                    TPRoles[4] = TPRoles[0];
                    TPRoles[5] = TPRoles[0];
                    TPRoles[6] = TPRoles[0];
                    TPRoles[7] = TPRoles[0];
                });
            Discord.MessageCreated += async (arg) =>
            {
                Dispatcher.Execute(async () =>
                {
                    ExecuteAsyncMethod(async () =>
                    {
                        var content = arg.Message.Content;
                        content = content.ToLower();
                        var newStr = "";
                        foreach (var c in content)
                        {
                            if (c != ' ' && c != '\'')
                            {
                                if (c == 'é')
                                    newStr += 'e';
                                else if (c == 'û')
                                    newStr += 'u';
                                else
                                    newStr += c;
                            }
                        }
                        if (newStr.Contains("cetaitsur"))
                            await arg.Message.RespondAsync("https://cdn.discordapp.com/attachments/619513575295418392/625709998692892682/sardoche.gif");
                    });
                });
            };
            Discord.MessageCreated += async (arg) =>
            {
                Dispatcher.Execute(async () =>
                {
                    ExecuteAsyncMethod(async () =>
                    {
                        var content = GetSimplifiedString(arg.Message.Content);
                        if (content.Contains(GetSimplifiedString(revengeLines[RevengeCurrLine])) && !arg.Author.IsBot)
                        {
                            RevengeCurrLine += 2;
                            if (RevengeCurrLine - 1 < revengeLines.Length)
                                await arg.Message.RespondAsync(revengeLines[RevengeCurrLine - 1]);
                            else
                                RevengeCurrLine = 0;
                        }
                        else if (content.Contains(GetSimplifiedString(revengeLines[0])) && !arg.Author.IsBot)
                        {
                            RevengeCurrLine = 2;
                            await arg.Message.RespondAsync(revengeLines[1]);
                        }
                        if (content.Contains("69") || content.Contains("420"))
                            await arg.Message.RespondAsync("nice");
                    });
                });
            };

            ///////////////////////////////////////
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Initialized");
            Console.ForegroundColor = DefaultColor;
            consoleThread.Start();

            DateTimeOffset lastListCheck = DateTimeOffset.UnixEpoch;
            DateTimeOffset lastCalendarCheck = DateTimeOffset.UnixEpoch;
            DateTimeOffset lastEdtDayCheck = DateTimeOffset.UnixEpoch;

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
                returnValue = default;
                return false;
            }
            return true;
        }

        private static string GetCode(DiscordEmoji emoji)
        {
            if (emoji.RequireColons)
                return "<" + emoji.GetDiscordName() + emoji.Id + ">";
            else
                return emoji.Name;
        }

        private static DiscordEmoji GetEmoji(string code)
        {
            DiscordEmoji emoji;
            try
            {
                var id = code.Split(':').Last();
                emoji = DiscordEmoji.FromGuildEmote(Discord, ulong.Parse(id.Substring(0, id.Length - 1)));
            }
            catch (Exception)
            {
                emoji = DiscordEmoji.FromUnicode(Discord, code);
            }
            return emoji;
        }

        private static string GetSimplifiedString(string str)
        {
            str = str.ToLower();
            var newStr = "";
            foreach (var c in str)
            {
                if (char.IsLetter(c))
                    newStr += c;
            }
            return newStr;
        }

        private static void LoadData()
        {
            if (!File.Exists("data.json"))
            {
                var file = new StreamWriter("data.json");
                var defaultObj = new Save();
                defaultObj.votes = new Vote[0];
                defaultObj.polls = new Poll[0];
                defaultObj.oldEdT = new int[8];
                defaultObj.currentSaveTime = DateTime.Now;
                defaultObj.edtMessages = new List<EdtDayMessage>[8];
                for (int i = 0; i < 8; i++)
                    defaultObj.edtMessages[i] = new List<EdtDayMessage>();
                file.Write(JsonConvert.SerializeObject(defaultObj, Formatting.Indented));
                file.Close();
            }
            var stream = new StreamReader("data.json");
            var obj = JsonConvert.DeserializeObject<Save>(stream.ReadToEnd());
            stream.Close();
            OldICalHash = obj.oldEdT;
            EdTMessages = obj.edtMessages;

            foreach (var item in obj.votes)
            {
                var vote = new VoteMessage();
                vote.ID = item.id;
                vote.Lifetime = item.duration;
                vote.ShowUsers = item.showUsers;
                DiscordChannel chan;
                ExecuteAsyncMethod(() => Discord.GetChannelAsync(item.message.channel), out chan);
                DiscordMessage mess;
                ExecuteAsyncMethod(() => chan.GetMessageAsync(item.message.id), out mess);
                vote.Message = mess;
                vote.Author = item.author;
                Votes.Add(vote);
            }
            foreach (var item in obj.polls)
            {
                var poll = new PollMessage();
                poll.ID = item.id;
                poll.Lifetime = item.duration;
                poll.ShowUsers = item.showUsers;
                DiscordChannel chan;
                ExecuteAsyncMethod(() => Discord.GetChannelAsync(item.message.channel), out chan);
                DiscordMessage mess;
                ExecuteAsyncMethod(() => chan.GetMessageAsync(item.message.id), out mess);
                poll.Author = item.author;
                poll.Message = mess;
                poll.Open = item.open;
                poll.Choices = new List<Tuple<string, DiscordEmoji>>();
                foreach (var item2 in item.choices)
                    poll.Choices.Add(new Tuple<string, DiscordEmoji>(item2.name, GetEmoji(item2.id)));

                Polls.Add(poll);
            }
            if (obj.savedVotes != null)
                SavedVotes = new List<SavedVote>(obj.savedVotes);
            else
                SavedVotes = new List<SavedVote>();
            if (obj.savedPolls != null)
                SavedPolls = new List<SavedPoll>(obj.savedPolls);
            else
                SavedPolls = new List<SavedPoll>();
            if (obj.autoruns != null)
                Autoruns = new List<Autorun>(obj.autoruns);
            else
                Autoruns = new List<Autorun>();
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
            obj.savedPolls = SavedPolls.ToArray();
            obj.savedVotes = SavedVotes.ToArray();
            obj.autoruns = Autoruns.ToArray();
            obj.oldEdT = OldICalHash;
            obj.edtMessages = EdTMessages;
            obj.currentSaveTime = DateTime.Now;
            List<Vote> v = new List<Vote>();
            foreach (var item in Votes)
                v.Add(new Vote()
                {
                    id = item.ID,
                    author = item.Author,
                    duration = item.Lifetime,
                    showUsers = item.ShowUsers,
                    message = new SpecialMessage() { channel = item.Message.ChannelId, id = item.Message.Id }
                });
            obj.votes = v.ToArray();
            List<Poll> p = new List<Poll>();
            foreach (var item in Polls)
            {
                List<Choice> choices = new List<Choice>();
                foreach (var item2 in item.Choices)
                    choices.Add(new Choice() { id = GetCode(item2.Item2), name = item2.Item1 });

                p.Add(new Poll()
                {
                    id = item.ID,
                    author = item.Author,
                    duration = item.Lifetime,
                    showUsers = item.ShowUsers,
                    open = item.Open,
                    message = new SpecialMessage() { channel = item.Message.ChannelId, id = item.Message.Id },
                    choices = choices.ToArray()
                });
            }
            obj.polls = p.ToArray();
            var stream = new StreamWriter("data.json");
            stream.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
            stream.Close();
        }

        private static void UpdateCalendars()
        {
            if (!DEBUG)
            {
                Console.WriteLine("Updating calendars...");
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
                    var stringICal = new string[CalendarUrl.Length];
                    var newEdtMessages = new List<EdtDayMessage>[8];
                    for (int i = 0; i < 8; i++)
                        newEdtMessages[i] = new List<EdtDayMessage>();

                    for (int i = 0; i < (DEBUG ? 1 : CalendarUrl.Length); i++)
                    {
                        try
                        {
                            stringICal[i] = Client.DownloadString(CalendarUrl[i]);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (OldICalHash[i] == 0 || OldICalHash[i] != stringICal[i].Length)
                        {
                            Calendar calendar;
                            try
                            {
                                calendar = Calendar.Load(stringICal[i]);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                            IReadOnlyList<DiscordMessage> messages;
                            ExecuteAsyncMethod(() => EdtChannel[i].GetMessagesAsync(), out messages);
                            if (messages != null)
                            {
                                foreach (var mess in messages)
                                {
                                    if (mess.Author.IsCurrent)
                                        ExecuteAsyncMethod(() => EdtChannel[i].DeleteMessageAsync(mess));
                                }
                            }
                            ExecuteAsyncMethod(() => EdtChannel[i].SendMessageAsync("Emploi du temps :\n\n"));
                            foreach (var day in daysToDisplay)
                            {
                                var currEdtMessage = new EdtDayMessage();
                                currEdtMessage.day = day;
                                var events = new List<CalendarEvent>(calendar.Events.Where((e) => e.Start.Date == day));
                                if (events.Count == 0)
                                    continue;
                                StringBuilder contenthead = new StringBuilder();
                                if (day.DayOfWeek == DayOfWeek.Monday && mondayThisWeek != day)
                                    contenthead.Append("\n`________________________________________________________________________________________________________________________`\n\n");
                                if (day.DayOfWeek == DayOfWeek.Saturday)
                                    contenthead.Append("**" + day.ToString("D") + "** 💀");
                                else
                                    contenthead.Append("**" + day.ToString("D") + "**");
                                currEdtMessage.description = contenthead.ToString();
                                contenthead.Append("\n```\n");
                                events.Sort((l, r) => l.Start.CompareTo(r.Start));
                                StringBuilder contentBuild = new StringBuilder();
                                foreach (var ev in events)
                                {
                                    contentBuild.Append("De " + ev.Start.Hour.ToString("00") + ":" + ev.Start.Minute.ToString("00") + " à " + ev.End.Hour.ToString("00") + ":" + ev.End.Minute.ToString("00") + " | ");
                                    var splitted = ev.Summary.Split('-', StringSplitOptions.RemoveEmptyEntries);
                                    for (int j = 0; j < splitted.Length; j++)
                                        splitted[j] = splitted[j].TrimStart().TrimEnd();
                                    contentBuild.Append(splitted[0] + ", " + splitted[3]);
                                    if (splitted.Length == 5)
                                        contentBuild.Append(", " + splitted[4]);
                                    contentBuild.Append("\n");
                                }
                                currEdtMessage.content = contentBuild.ToString();
                                contenthead.Append(contentBuild);
                                contenthead.Append("```");
                                DiscordMessage mess;
                                ExecuteAsyncMethod(() => EdtChannel[i].SendMessageAsync(contenthead.ToString()), out mess);
                                currEdtMessage.message = new SpecialMessage() { channel = mess.ChannelId, id = mess.Id };
                                newEdtMessages[i].Add(currEdtMessage);
                            }
                            OldICalHash[i] = stringICal[i].Length;
                        }
                        else
                            newEdtMessages[i] = EdTMessages[i];
                    }
                    EdTMessages = newEdtMessages;

                    SaveData();
                    Console.WriteLine("Calendars updated");
                }
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
            for (int i = 0; i < Autoruns.Count; i++)
            {
                var auto = Autoruns[i];
                if (auto.baseTime + auto.delay < DateTime.Now)
                {
                    auto.baseTime += auto.delay;
                    Autoruns[i] = auto;
                    var channel = await Discord.GetChannelAsync(auto.channel);
                    if (SavedVotes.Any((s) => s.name == auto.name))
                    {
                        var saved = SavedVotes.Find((s) => s.name == auto.name);
                        var buff = new byte[8];
                        Random.NextBytes(buff);
                        ulong id = BitConverter.ToUInt64(buff);
                        string content = saved.content + " \nid:" + id;
                        DiscordMessage message;
                        message = await channel.SendMessageAsync(content);
                        {
                            await message.CreateReactionAsync(Upvote);
                            await message.CreateReactionAsync(Downvote);
                            Votes.Add(new VoteMessage() { Author = auto.author, ID = id, Message = message, Lifetime = saved.duration, ShowUsers = saved.showUsers });
                            SaveData();
                        }
                    }
                    else if (SavedPolls.Any((s) => s.name == auto.name))
                    {
                        var saved = SavedPolls.Find((s) => s.name == auto.name);
                        var buff = new byte[8];
                        Random.NextBytes(buff);
                        ulong idPoll = BitConverter.ToUInt64(buff);
                        var question = saved.content + "\nid:" + idPoll;
                        List<Tuple<string, DiscordEmoji>> reactions = new List<Tuple<string, DiscordEmoji>>();
                        for (int j = 0; j < saved.choices.Length; j++)
                        {
                            var choice = saved.choices[j];
                            reactions.Add(new Tuple<string, DiscordEmoji>(choice.name, GetEmoji(choice.id)));
                        }
                        var builder = new StringBuilder();
                        builder.Append(question + "\n");
                        foreach (var item in reactions)
                        {
                            if (item.Item2.RequireColons)
                                builder.Append("<" + item.Item2.GetDiscordName() + item.Item2.Id + "> : " + item.Item1 + "\n");
                            else
                                builder.Append(item.Item2.Name + " : " + item.Item1 + "\n");
                        }

                        var message = await channel.SendMessageAsync(builder.ToString());
                        Polls.Add(new PollMessage()
                        {
                            Choices = reactions,
                            Lifetime = saved.duration,
                            Message = message,
                            ShowUsers = saved.showUsers,
                            Open = saved.open,
                            Author = auto.author,
                            ID = idPoll
                        });
                        foreach (var item in reactions)
                            await message.CreateReactionAsync(item.Item2);
                        SaveData();
                    }
                }
            }
            SaveData();
        }

        #endregion Private Methods
    }
}