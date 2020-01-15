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
using IcalToImage;

namespace InfoBot
{
    internal partial class Program
    {
        #region Private Fields

        /// <summary>
        /// Used to change some actions, such as targeting a special debug channel for spam, and
        /// disable the edt check
        /// </summary>
        private const bool DEBUG = false;

        /// <summary>
        /// Current line of the all star song waiting to be written
        /// </summary>
        private static int AllstarCurrLine;

        /// <summary>
        /// Lyrics of all star
        /// </summary>
        private static string[] allstarLines;

        /// <summary>
        /// List of all autoruns created
        /// </summary>
        private static List<Autorun> Autoruns;

        /// <summary>
        /// Global HTTP client for edt purposes atm
        /// </summary>
        private static WebClient Client;

        /// <summary>
        /// The default front color of the local console
        /// </summary>
        private static ConsoleColor DefaultColor;

        /// <summary>
        /// Current line of the deja vu song waiting to be written
        /// </summary>
        private static int DejavuCurrLine;

        /// <summary>
        /// Lyrics of deja vu
        /// </summary>
        private static string[] dejavuLines;

        /// <summary>
        /// The main discord server
        /// </summary>
        private static DiscordGuild DUTInfoServer;

        /// <summary>
        /// list of 8 edt channels, sorted by their group
        /// </summary>
        private static DiscordChannel[] EdtChannel;

        /// <summary>
        /// Last time the edt got checked (used for updating every week)
        /// </summary>
        private static DateTime LastEdtCheck;

        /// <summary>
        /// The next time the bot will restart its client to stay connected in some cases
        /// </summary>
        private static DateTime NextRestart;

        /// <summary>
        /// The old hash of each edt (sorted by their group). Used to detect changes in differents edt.
        /// </summary>
        private static int[] OldICalHash;

        /// <summary>
        /// Current line of the revenge song waiting to be written
        /// </summary>
        private static int RevengeCurrLine;

        /// <summary>
        /// Lyrics of revenge
        /// </summary>
        private static string[] revengeLines;

        /// <summary>
        /// active saved polls (using template)
        /// </summary>
        private static List<SavedPoll> SavedPolls;

        /// <summary>
        /// active saved votes (using template)
        /// </summary>
        private static List<SavedVote> SavedVotes;

        /// <summary>
        /// The channels of the 2 shi fu mi players
        /// </summary>
        private static DiscordChannel[] ShiFuMiChannel;

        /// <summary>
        /// Channel for the nsfw tags
        /// </summary>
        private static DiscordChannel TagsChannel;

        /// <summary>
        /// Id of the test server (NOT the DUT INFO)
        /// </summary>
        private static DiscordGuild TestServer;

        /// <summary>
        /// The 8 roles sorted by their group
        /// </summary>
        private static DiscordRole[] TPRoles;

        #endregion Private Fields

        #region Private Properties

        /// <summary>
        /// List of urls to the ical sorted by their group
        /// </summary>
        private static string[] CalendarUrl { get; set; }

        /// <summary>
        /// Global discord client
        /// </summary>
        private static DiscordClient Discord { get; set; }

        /// <summary>
        /// Dispatcher used by the main func
        /// </summary>
        private static Dispatcher Dispatcher { get; set; }

        #endregion Private Properties

        #region Private Methods

        /// <summary>
        /// Main func, but async
        /// </summary>
        /// <param name="args">arguments</param>
        /// <returns>void</returns>
        private static async Task AsyncMain(string[] args)
        {
            //we initiate the differents lyrics
            RevengeCurrLine = 0;
            DejavuCurrLine = 0;
            AllstarCurrLine = 0;
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
            using (var stream = new StreamReader("dejavu.txt"))
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
                dejavuLines = lines.ToArray();
            }
            using (var stream = new StreamReader("allstar.txt"))
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
                allstarLines = lines.ToArray();
            }
            //now we initiate the ical
            CalendarUrl = new string[8];
            TPRoles = new DiscordRole[8];
            CalendarUrl[0] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=e81e5e310001831"; //1.1
            CalendarUrl[1] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=4352c5485001785"; //1.2
            CalendarUrl[2] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=329314450001800"; //2.1
            CalendarUrl[3] = ""; //2.2
            CalendarUrl[4] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=7a533fe97001770"; //3.1
            CalendarUrl[5] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=b4a52df5e501843"; //3.2
            CalendarUrl[6] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=561e49e97901779"; //4.1
            CalendarUrl[7] = "https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=3a1ee9527101771"; //4.2
            Client = new WebClient();
            DefaultColor = Console.ForegroundColor;
            string token;
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
            Console.WriteLine("Connecting...");
            if (!ExecuteAsyncMethod(() => Discord.ConnectAsync()))
                Environment.Exit(0);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connected");
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine("Initalization...");
            ///////////////////////////////////////
            NextRestart = DateTime.Now + TimeSpan.FromHours(1);
            Random = new Random();
            Dispatcher = new Dispatcher();
            var consoleThread = new Thread(ConsoleManager);
            //we connect to the DUT server
            if (!ExecuteAsyncMethod(() => Discord.GetGuildAsync(619513574850560010), out DUTInfoServer))
                DUTInfoServer = null;
            //we connect to the test server
            ExecuteAsyncMethod(() => Discord.GetGuildAsync(437704877221609472), out TestServer);
            InitCommands();
            ExecuteAsyncMethod(() => Discord.UpdateStatusAsync(new DiscordGame(">ib help")));
            LoadData();
            EdtChannel = new DiscordChannel[8];
            ShiFuMiChannel = new DiscordChannel[2];
            ExecuteAsyncMethod(async () =>
            {
                //all the specific chan/roles sorted by their group
                EdtChannel[0] = await Discord.GetChannelAsync(623557669810077697);
                EdtChannel[1] = await Discord.GetChannelAsync(623557699497623602);
                EdtChannel[2] = await Discord.GetChannelAsync(623557716694138890);
                EdtChannel[3] = await Discord.GetChannelAsync(623557732930289664);
                EdtChannel[4] = await Discord.GetChannelAsync(623557762101542923);
                EdtChannel[5] = await Discord.GetChannelAsync(623557780527382530);
                EdtChannel[6] = await Discord.GetChannelAsync(623557795660431390);
                EdtChannel[7] = await Discord.GetChannelAsync(623557977223200798);

                ShiFuMiChannel[0] = await Discord.GetChannelAsync(632490916342530058);
                ShiFuMiChannel[1] = await Discord.GetChannelAsync(632490998488104970);

                TPRoles[0] = DUTInfoServer.GetRole(619949947042791472);
                TPRoles[1] = DUTInfoServer.GetRole(619949993096118292);
                TPRoles[2] = DUTInfoServer.GetRole(619950016630358086);
                TPRoles[3] = DUTInfoServer.GetRole(619950035895058432);
                TPRoles[4] = DUTInfoServer.GetRole(619950048519913492);
                TPRoles[5] = DUTInfoServer.GetRole(619950099807862794);
                TPRoles[6] = DUTInfoServer.GetRole(619950111300255774);
                TPRoles[7] = DUTInfoServer.GetRole(619950125573472262);

                TagsChannel = await Discord.GetChannelAsync(646474262021931026);
            });

            if (DEBUG)
                ExecuteAsyncMethod(async () =>
                {
                    //debug equivalent, target #debug-bot and "@Developpeur du BOT"
                    EdtChannel[0] = await Discord.GetChannelAsync(623874313539289125);
                    EdtChannel[1] = EdtChannel[0];
                    EdtChannel[2] = EdtChannel[0];
                    EdtChannel[3] = EdtChannel[0];
                    EdtChannel[4] = EdtChannel[0];
                    EdtChannel[5] = EdtChannel[0];
                    EdtChannel[6] = EdtChannel[0];
                    EdtChannel[7] = EdtChannel[0];

                    ShiFuMiChannel[0] = EdtChannel[0];
                    ShiFuMiChannel[1] = EdtChannel[0];

                    TPRoles[0] = DUTInfoServer.GetRole(623874435845324853);
                    TPRoles[1] = TPRoles[0];
                    TPRoles[2] = TPRoles[0];
                    TPRoles[3] = TPRoles[0];
                    TPRoles[4] = TPRoles[0];
                    TPRoles[5] = TPRoles[0];
                    TPRoles[6] = TPRoles[0];
                    TPRoles[7] = TPRoles[0];

                    TagsChannel = EdtChannel[0];
                });
            async Task createdMess1(DSharpPlus.EventArgs.MessageCreateEventArgs arg)
            {
                //we execute in the same thread using the dispatcher
                Dispatcher.Execute(async () =>
                {
                    //native exception handling, preventing crashes
                    ExecuteAsyncMethod(async () =>
                    {
                        //for messages containing "c'était sûr" to respond with our lord and savior Sardoche
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
            //whenever a messages gets created...
            Discord.MessageCreated += createdMess1;
            async Task createdMess2(DSharpPlus.EventArgs.MessageCreateEventArgs arg)
            {
                Dispatcher.Execute(async () =>
                {
                    ExecuteAsyncMethod(async () =>
                    {
                        var content = arg.Message.Content;
                        try
                        {
                            //handling revenge lyrics
                            if (EvaluateWholeStringSimilarity(content, revengeLines[RevengeCurrLine]) >= .8 && !arg.Author.IsBot)
                            {
                                //if a similarity has been detected and it's not comming from a bot
                                RevengeCurrLine += 2;
                                //we test if we are at the end of the song
                                if (RevengeCurrLine - 1 < revengeLines.Length)
                                    await arg.Message.RespondAsync(revengeLines[RevengeCurrLine - 1]);
                                else
                                    RevengeCurrLine = 0;
                            }
                            else if (GetSimplifiedString(content).Contains(GetSimplifiedString(revengeLines[0])) && !arg.Author.IsBot)
                            {
                                //if we start over again (first line of the lyrics)
                                RevengeCurrLine = 2;
                                await arg.Message.RespondAsync(revengeLines[1]);
                            }
                            //handling all star lyrics
                            if (EvaluateWholeStringSimilarity(content, allstarLines[AllstarCurrLine]) >= .8 && !arg.Author.IsBot)
                            {
                                //if a similarity has been detected and it's not comming from a bot
                                AllstarCurrLine += 2;
                                //we test if we are at the end of the song
                                if (AllstarCurrLine - 1 < allstarLines.Length)
                                    await arg.Message.RespondAsync(allstarLines[AllstarCurrLine - 1]);
                                else
                                    AllstarCurrLine = 0;
                            }
                            else if (GetSimplifiedString(content).Contains(GetSimplifiedString(allstarLines[0])) && !arg.Author.IsBot)
                            {
                                //if we start over again (first line of the lyrics)
                                AllstarCurrLine = 2;
                                await arg.Message.RespondAsync(allstarLines[1]);
                            }
                            //same as above, but for deja vu
                            if (EvaluateWholeStringSimilarity(content, dejavuLines[DejavuCurrLine]) >= .8 && !arg.Author.IsBot)
                            {
                                DejavuCurrLine += 2;
                                if (DejavuCurrLine - 1 < dejavuLines.Length)
                                    await arg.Message.RespondAsync(dejavuLines[DejavuCurrLine - 1]);
                                else
                                    DejavuCurrLine = 0;
                            }
                            else if (GetSimplifiedString(content).Contains(GetSimplifiedString(dejavuLines[8])) && !arg.Author.IsBot)
                            {
                                //special starting point, where it it "deja vu !"
                                DejavuCurrLine = 10;
                                await arg.Message.RespondAsync(dejavuLines[9]);
                            }
                            else if (EvaluateWholeStringSimilarity(content, dejavuLines[0]) >= .8 && !arg.Author.IsBot)
                            {
                                DejavuCurrLine = 2;
                                await arg.Message.RespondAsync(dejavuLines[1]);
                            }
                            if (!arg.Author.IsBot)
                            {
                                for (int i = 0; i < content.Length - 5; i++)
                                {
                                    string strNumber = content.Substring(i, 6);
                                    bool correctPattern = true;
                                    if (i > 0 && !char.IsWhiteSpace(content[i - 1]))
                                        correctPattern = false;
                                    if (i < content.Length - 6 && !char.IsWhiteSpace(content[i + 6]))
                                        correctPattern = false;
                                    foreach (var item in strNumber)
                                        if (!char.IsDigit(item))
                                            correctPattern = false;
                                    if (correctPattern)
                                    {
                                        var address = "https://nhentai.net/g/" + strNumber;
                                        var html = Client.DownloadString(address);
                                        string title = "";
                                        {
                                            var begin = -1;
                                            for (int j = 0; j < html.Length - 4; j++)
                                                if (html.Substring(j, 4) == "<h1>")
                                                    begin = j + 4;
                                            for (int j = begin; j < html.Length - 5 && html.Substring(j, 5) != "</h1>"; j++)
                                                title += html[j];
                                            title.Trim(' ', '\t', '\n', '\r');
                                        }
                                        if (title.Contains("404"))
                                            continue;
                                        var sb = new StringBuilder();
                                        sb.Append("Tag :__");
                                        sb.Append(strNumber);
                                        sb.Append("__ posted by **");
                                        sb.Append(arg.Author.Username);
                                        sb.Append("** __");
                                        sb.Append(title);
                                        sb.Append("__\nAvailable here : ");
                                        sb.Append(address);
                                        await TagsChannel.SendMessageAsync(sb.ToString());
                                    }
                                }
                                for (int i = 0; i < content.Length - 4; i++)
                                {
                                    string strNumber = content.Substring(i, 5);
                                    bool correctPattern = true;
                                    if (i > 0 && !char.IsWhiteSpace(content[i - 1]))
                                        correctPattern = false;
                                    if (i < content.Length - 5 && !char.IsWhiteSpace(content[i + 5]))
                                        correctPattern = false;
                                    foreach (var item in strNumber)
                                        if (!char.IsDigit(item))
                                            correctPattern = false;
                                    if (correctPattern)
                                    {
                                        var address = "https://nhentai.net/g/" + strNumber;
                                        var html = Client.DownloadString(address);
                                        string title = "";
                                        {
                                            var begin = -1;
                                            for (int j = 0; j < html.Length - 4; j++)
                                                if (html.Substring(j, 4) == "<h1>")
                                                    begin = j + 4;
                                            for (int j = begin; j < html.Length - 5 && html.Substring(j, 5) != "</h1>"; j++)
                                                title += html[j];
                                            title.Trim(' ', '\t', '\n', '\r');
                                        }
                                        if (title.Contains("404"))
                                            continue;
                                        var sb = new StringBuilder();
                                        sb.Append("Tag :__");
                                        sb.Append(strNumber);
                                        sb.Append("__ posted by **");
                                        sb.Append(arg.Author.Username);
                                        sb.Append("** __");
                                        sb.Append(title);
                                        sb.Append("__\nAvailable here : ");
                                        sb.Append(address);
                                        await TagsChannel.SendMessageAsync(sb.ToString());
                                    }
                                }
                                {
                                    var vowels = "aeiouy";
                                    var lower = content.ToLower();
                                    if (new string(lower.ToArray()[^3..^0]) == "ine" && !vowels.Contains(RemoveDiacritics(lower).ToArray()[^4]))
                                    {
                                        int index = lower.Length - 1;
                                        while (index != 0 && char.IsLetter(lower[index]))
                                            index--;
                                        if (char.IsWhiteSpace(lower[index]))
                                            index++;
                                        await arg.Message.RespondAsync("on dit pain au " + new string(content.ToArray()[index..^3]) + ", pas " + new string(content.ToArray()[index..^0]));
                                    }
                                    for (int i = 0; i < lower.Length - 4; i++)
                                    {
                                        if (new string(lower.ToArray()[i..(i + 4)]) == "ine " && !vowels.Contains(RemoveDiacritics(lower).ToArray()[i - 1]))
                                        {
                                            int index = i + 1;
                                            while (index != 0 && char.IsLetter(lower[index]))
                                                index--;
                                            if (char.IsWhiteSpace(lower[index]))
                                                index++;
                                            await arg.Message.RespondAsync("on dit pain au " + new string(content.ToArray()[index..i]) + ", pas " + new string(content.ToArray()[index..(i + 3)]));
                                        }
                                    }
                                }
                                {
                                    var vowels = "aeiouy";
                                    var lower = content.ToLower();
                                    if (new string(lower.ToArray()[^4..^0]) == "ines" && !vowels.Contains(RemoveDiacritics(lower).ToArray()[^5]))
                                    {
                                        int index = lower.Length - 1;
                                        while (index != 0 && char.IsLetter(lower[index]))
                                            index--;
                                        if (char.IsWhiteSpace(lower[index]))
                                            index++;
                                        await arg.Message.RespondAsync("on dit pains aux " + new string(content.ToArray()[index..^4]) + ", pas " + new string(content.ToArray()[index..^0]));
                                    }
                                    for (int i = 0; i < content.Length - 5; i++)
                                    {
                                        if (new string(lower.ToArray()[i..(i + 5)]) == "ines " && !vowels.Contains(RemoveDiacritics(lower).ToArray()[i - 1]))
                                        {
                                            int index = i + 1;
                                            while (index != 0 && char.IsLetter(lower[index]))
                                                index--;
                                            if (char.IsWhiteSpace(lower[index]))
                                                index++;
                                            await arg.Message.RespondAsync("on dit pains aux " + new string(content.ToArray()[index..i]) + ", pas " + new string(content.ToArray()[index..(i + 4)]));
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        { }
                    });
                });
            };
            //same as above
            Discord.MessageCreated += createdMess2;

            ///////////////////////////////////////
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Initialized");
            Console.ForegroundColor = DefaultColor;
            consoleThread.Start();
            //we always want to check if everything is ok when starting, so we go a little bit back in time
            DateTimeOffset lastListCheck = DateTimeOffset.UnixEpoch;
            DateTimeOffset lastCalendarCheck = DateTimeOffset.UnixEpoch;
            DateTimeOffset lastEdtDayCheck = DateTimeOffset.UnixEpoch;

            while (true)
            {
                //dispatcher handling, whenever we add a func to execute to the dispatcher, it gets added here so everything is executed on the same thread
                var next = Dispatcher.GetNext();
                if (next != null)
                    ExecuteAsyncMethod(next);
                //We check for the votes/polls every 20 secs
                if (lastListCheck + TimeSpan.FromSeconds(20) < DateTimeOffset.Now)
                {
                    lastListCheck = DateTimeOffset.Now;
                    ExecuteAsyncMethod(UpdateLists);
                }
                //We check for the edt every 2 hours
                if (lastCalendarCheck + TimeSpan.FromHours(2) < DateTimeOffset.Now)
                {
                    lastCalendarCheck = DateTimeOffset.Now;
                    ExecuteAsyncMethod(async () => UpdateCalendars());
                }
                if (NextRestart < DateTime.Now)
                {
                    NextRestart = DateTime.Now + TimeSpan.FromHours(1);
                    ExecuteAsyncMethod(() => Discord.DisconnectAsync());
                    Discord.Dispose();
                    try
                    {
                        Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
                    }
                    catch (Exception)
                    {
                    }
                    Console.WriteLine("Reconnecting...");
                    if (!ExecuteAsyncMethod(() => Discord.ConnectAsync()))
                        Environment.Exit(0);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Connected");
                    Console.ForegroundColor = DefaultColor;
                    Console.WriteLine("Initalization...");
                    ExecuteAsyncMethod(() => Discord.GetGuildAsync(437704877221609472), out TestServer);
                    InitCommands();
                    ExecuteAsyncMethod(() => Discord.UpdateStatusAsync(new DiscordGame(">ib help")));
                    LoadData();
                    //whenever a messages gets created...
                    Discord.MessageCreated += createdMess1;
                    //same as above
                    Discord.MessageCreated += createdMess2;

                    ///////////////////////////////////////
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Initialized");
                    Console.ForegroundColor = DefaultColor;
                }
                //give some resting time to that poor CPU !
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        /// <summary>
        /// Function that return a value between 0 and 1 according to the similarity of 2 strings
        /// </summary>
        /// <param name="source">string 1</param>
        /// <param name="target">string 2</param>
        /// <returns>value between 0 and 1 according to the similarity of 2 strings</returns>
        private static double CalculateSimilarity(string source, string target)
        {
            //https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / Math.Max(source.Length, target.Length)));
        }

        /// <summary>
        /// Function that returns the number of steps required to transform a string into another
        /// </summary>
        /// <param name="source">string 1</param>
        /// <param name="target">string 2</param>
        /// <returns>the number of steps required to transform a string into another</returns>
        private static int ComputeLevenshteinDistance(string source, string target)
        {
            //I just copy/pasted this code from somewhere i don't remember, please don't touch it thx
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    // Step 3
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 4
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }

        /// <summary>
        /// Manages the local console, currently poorly implemented.
        /// </summary>
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
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// ALl in one function to give a value from 0 to 1 depending of the similarity of 2
        /// strings. It first simplifies them and check if the content contains a possible origin
        /// string. That means it can return a value of 1 if the content contains the origin even if
        /// it has more chars around.
        /// </summary>
        /// <param name="content">String that contains eventually the origin</param>
        /// <param name="origin">String to search for</param>
        /// <returns>value from 0 to 1</returns>
        private static double EvaluateWholeStringSimilarity(string content, string origin)
        {
            //we automatically simplify the strings
            content = GetSimplifiedString(content);
            origin = GetSimplifiedString(origin);
            double highest = 0;
            int i = 0;
            do
            {
                //we check every possible way that origin can be in content, and keep only the highest score
                highest = Math.Max(CalculateSimilarity(content.Substring(i, Math.Min(origin.Length, content.Length - i)), origin), highest);
                i++;
            } while (i <= content.Length - origin.Length);
            return highest;
        }

        /// <summary>
        /// Safe from crashes way to trun an async method
        /// </summary>
        /// <param name="func">async func to run</param>
        /// <returns>True if everything went well, false if it returned an exception.</returns>
        private static bool ExecuteAsyncMethod(Func<Task> func)
        {
            try
            {
                if (!func().Wait(TimeSpan.FromSeconds(30)))
                    throw new Exception("30 sec timeout passed, command canceled");
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

        /// <summary>
        /// Safe from crashes way to trun an async method
        /// </summary>
        /// <param name="func">async func to run</param>
        /// <param name="returnValue">Possible return value of the async func</param>
        /// <returns>True if everything went well, false if it returned an exception.</returns>
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

        /// <summary>
        /// Get the string implementation of an emoji, regardless of its origin
        /// </summary>
        /// <param name="emoji">emoji object</param>
        /// <returns>coded string</returns>
        private static string GetCode(DiscordEmoji emoji)
        {
            if (emoji.RequireColons)
                //if the emoji is custom
                return "<" + emoji.GetDiscordName() + emoji.Id + ">";
            else
                //if the emoji is native (Unicode)
                return emoji.Name;
        }

        /// <summary>
        /// Get the emoji object from a code string
        /// </summary>
        /// <param name="code">coded string</param>
        /// <returns>emoji object</returns>
        private static DiscordEmoji GetEmoji(string code)
        {
            DiscordEmoji emoji;
            try
            {
                //If the emoji is custom
                var id = code.Split(':').Last();
                emoji = DiscordEmoji.FromGuildEmote(Discord, ulong.Parse(id.Substring(0, id.Length - 1)));
            }
            catch (Exception)
            {
                //If the emoji is native (Unicode)
                emoji = DiscordEmoji.FromUnicode(Discord, code);
            }
            return emoji;
        }

        /// <summary>
        /// Simplify to the extreme a string, keeping only lowercase with no diacritics letters and digits
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string GetSimplifiedString(string str)
        {
            str = RemoveDiacritics(str).ToLower();
            var newStr = "";
            foreach (var c in str)
            {
                if (char.IsLetterOrDigit(c))
                    newStr += c;
            }
            return newStr;
        }

        /// <summary>
        /// Loads the saved data.json file
        /// </summary>
        private static void LoadData()
        {
            //here we extract everything saved from the last session, so we keep track of everything
            if (!File.Exists("data.json"))
            {
                var file = new StreamWriter("data.json");
                var defaultObj = new Save();
                defaultObj.votes = new Vote[0];
                defaultObj.polls = new Poll[0];
                defaultObj.oldEdT = new int[8];
                defaultObj.currentSaveTime = DateTime.Now;
                defaultObj.lastEdtCheck = DateTime.Now;
                file.Write(JsonConvert.SerializeObject(defaultObj, Formatting.Indented));
                file.Close();
            }
            var stream = new StreamReader("data.json");
            var obj = JsonConvert.DeserializeObject<Save>(stream.ReadToEnd());
            stream.Close();
            OldICalHash = obj.oldEdT;
            LastEdtCheck = obj.lastEdtCheck;

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

        /// <summary>
        /// Main func
        /// </summary>
        /// <param name="args">arguments</param>
        private static void Main(string[] args)
        {
            //we better run the async version asap
            Task.Run(() => AsyncMain(args)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Parse a command-like string
        /// </summary>
        /// <param name="input">command-like string</param>
        /// <param name="command">name of the command (first word)</param>
        /// <param name="args">arguments to that command (every other words/strings after the command)</param>
        private static void ParseInput(string input, out string command, out string[] args)
        {
            command = "";
            var listArgs = new List<string>();
            int index = 0;
            while (index < input.Length)
            {
                //we extract the first word
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
                //if we have to escape a character
                if (currChar == '\\' && input.Length > index + 1)
                {
                    currChar = input[++index];
                    currArg += currChar;
                }
                //if we got a blank space, we have to go to the other argument (unless we are in a string)
                else if (currChar == ' ' && !ignoreSpaces)
                {
                    if (currArg.Length > 0)
                        listArgs.Add(currArg);
                    currArg = "";
                }
                //we enter a string, we have to ignore spaces til the end of this string
                else if (currChar == '"')
                    ignoreSpaces = !ignoreSpaces;
                else
                    //otherwise, we just add the current char to the current argument
                    currArg += currChar;
                index++;
            }
            //if the last current argument is valid, we don't forget to add him too
            if (currArg.Length > 0)
                listArgs.Add(currArg);
            args = listArgs.ToArray();
        }

        /// <summary>
        /// remplace every diacritic by its base equivalent (a diacritic is for example "é, ç, â",
        /// resulting in "e, c, a"
        /// </summary>
        /// <param name="text">text to change</param>
        /// <returns>text without diacritics</returns>
        private static string RemoveDiacritics(string text)
        {
            //copy pasta, don't touch it
            //https://stackoverflow.com/a/249126
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Here we save all the current data to keep it for the next session
        /// </summary>
        private static void SaveData()
        {
            var obj = new Save();
            obj.lastEdtCheck = LastEdtCheck;
            obj.savedPolls = SavedPolls.ToArray();
            obj.savedVotes = SavedVotes.ToArray();
            obj.autoruns = Autoruns.ToArray();
            obj.oldEdT = OldICalHash;
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

        /// <summary>
        /// Here we check for the edt changes
        /// </summary>
        private static void UpdateCalendars()
        {
            //if we are in debug mode, we obviously don't take a look at it since it's can be pretty heavy for the bot
            if (!DEBUG)
            {
                Console.WriteLine("Updating calendars...");
                {
                    DateTime mondayThisWeek = DateTime.Today - TimeSpan.FromDays((int)DateTime.Today.DayOfWeek - 1);
                    //A list of the days to check, starting the current week up to the end of the next week
                    DateTime[] week1 = new DateTime[]
                    {
                mondayThisWeek,
                mondayThisWeek.AddDays(1),
                mondayThisWeek.AddDays(2),
                mondayThisWeek.AddDays(3),
                mondayThisWeek.AddDays(4),
                mondayThisWeek.AddDays(5)
                    };
                    DateTime[] week2 = new DateTime[]
                    {
                mondayThisWeek.AddDays(7),
                mondayThisWeek.AddDays(8),
                mondayThisWeek.AddDays(9),
                mondayThisWeek.AddDays(10),
                mondayThisWeek.AddDays(11),
                mondayThisWeek.AddDays(12)
                    };
                    //the new list of ical strings we will get from the urls
                    var stringICal = new string[CalendarUrl.Length];
                    //gets true if we are in a new week (or if we are in a sooner moment in a week than the last time we checked)
                    var newWeek = ((int)DateTime.Today.DayOfWeek - 1) % 7 < ((int)LastEdtCheck.DayOfWeek - 1) % 7;
                    for (int i = 0; i < (DEBUG ? 1 : CalendarUrl.Length); i++)
                    {
                        try
                        {
                            //we download the differents icals
                            stringICal[i] = Client.DownloadString(CalendarUrl[i]);
                        }
                        catch (Exception)
                        {
                            //if no ical is provided, we just ignore it. I already give enough the the promotion, if they don't give me the link, fuck them.
                            continue;
                        }
                        //if we find a difference in the ical (we check the length of the string, which isn't very smart i know) or if we changed week
                        if (OldICalHash[i] == 0 || OldICalHash[i] != stringICal[i].Length || newWeek)
                        {
                            Calendar calendar;
                            try
                            {
                                //we try to parse the ical
                                calendar = Calendar.Load(stringICal[i]);
                            }
                            catch (Exception)
                            {
                                //or... we just ignore it
                                continue;
                            }
                            //we search of the messages in the corresponding edt channel
                            IReadOnlyList<DiscordMessage> messages;
                            ExecuteAsyncMethod(() => EdtChannel[i].GetMessagesAsync(), out messages);
                            if (messages != null)
                            {
                                foreach (var mess in messages)
                                {
                                    //if they come from a bot (me), we just delete them
                                    if (mess.Author.IsCurrent)
                                        ExecuteAsyncMethod(() => EdtChannel[i].DeleteMessageAsync(mess));
                                }
                            }
                            foreach (var ev in calendar.Events)
                            {
                                //if we find a day to display, we remove useless informations from the summary
                                if (week1.Contains(ev.Start.Date) || week2.Contains(ev.Start.Date))
                                {
                                    var sb = new StringBuilder();
                                    var splitted = ev.Summary.Split('-', StringSplitOptions.RemoveEmptyEntries);
                                    //here we purge the differents informations from spaces
                                    for (int j = 0; j < splitted.Length; j++)
                                        splitted[j] = splitted[j].TrimStart().TrimEnd();
                                    //and we display other informations, such as the matter, the class, the location, ...
                                    sb.Append(splitted[0] + ", " + splitted[3]);
                                    //if the type of the course is given (CM/TD/TP)
                                    if (splitted.Length == 5)
                                        sb.Append(", " + splitted[4]);
                                    ev.Summary = sb.ToString();
                                }
                            }
                            var convert = new Converter(calendar);
                            var stream = new MemoryStream();
                            convert.ConvertToBitmap(650, week1).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Seek(0, SeekOrigin.Begin);
                            var stream2 = new MemoryStream();
                            convert.ConvertToBitmap(650, week2).Save(stream2, System.Drawing.Imaging.ImageFormat.Png);
                            stream2.Seek(0, SeekOrigin.Begin);
                            ExecuteAsyncMethod(() => EdtChannel[i].SendFileAsync(stream, "edtW1.png"));
                            ExecuteAsyncMethod(() => EdtChannel[i].SendFileAsync(stream2, "edtW2.png"));
                            //we also save the new length of the ical, to check differences
                            OldICalHash[i] = stringICal[i].Length;
                        }
                    }
                    LastEdtCheck = DateTime.Now;
                    SaveData();
                    Console.WriteLine("Calendars updated");
                }
            }
        }

        /// <summary>
        /// Here we update the votes/polls
        /// </summary>
        /// <returns>void</returns>
        private static async Task UpdateLists()
        {
            for (int i = Votes.Count - 1; i >= 0; i--)
            {
                var item = Votes[i];
                //if the lifitime of the vote has exceeded, we display the results
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
                        //we show the results (i know i have the GetCode() func now, but i'm stupid)
                        content.Append("<" + Upvote.GetDiscordName() + Upvote.Id + "> : **" + (upvotes.Count - 1).ToString() + "**\n");
                        content.Append("<" + Downvote.GetDiscordName() + Downvote.Id + "> : **" + (downvotes.Count - 1).ToString() + "**");
                    }
                    else
                    {
                        //we ping the people that voted if the vote had that parameter
                        content.Append("<" + Upvote.GetDiscordName() + Upvote.Id + "> : **" + (upvotes.Count - 1).ToString() + "** : ");
                        bool first = true;
                        foreach (var user in await item.Message.GetReactionsAsync(Upvote))
                        {
                            //let's ping everyone !
                            if (!user.IsBot)
                            {
                                if (!first)
                                    content.Append(", ");
                                content.Append(user.Mention);
                                first = false;
                            }
                        }
                        //same for the downvotes
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
                    //we send the results
                    ExecuteAsyncMethod(() => item.Message.Channel.SendMessageAsync(content.ToString()));
                    //we delete the old vote
                    await item.Message.DeleteAsync();
                }
            }
            //basically the same thing, but for the polls
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
                            //we look for every reactions possibles
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
                    //if the autorun has elapsed his cooldown, we post the new vote/poll
                    auto.baseTime += auto.delay;
                    Autoruns[i] = auto;
                    var channel = await Discord.GetChannelAsync(auto.channel);
                    if (SavedVotes.Any((s) => s.name == auto.name))
                    {
                        //if the saved one was a vote
                        var saved = SavedVotes.Find((s) => s.name == auto.name);
                        var buff = new byte[8];
                        Random.NextBytes(buff);
                        ulong id = BitConverter.ToUInt64(buff);
                        string content = saved.content + " \n||id:" + id + "||";
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
                        //if the saved one was a poll
                        var saved = SavedPolls.Find((s) => s.name == auto.name);
                        var buff = new byte[8];
                        Random.NextBytes(buff);
                        ulong idPoll = BitConverter.ToUInt64(buff);
                        var question = saved.content + "\n||id:" + idPoll + "||";
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