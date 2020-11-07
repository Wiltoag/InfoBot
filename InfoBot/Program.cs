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
        /// ALl in one function to give a value from 0 to 1 depending of the similarity of 2
        /// strings. It first simplifies them and check if the content contains a possible origin
        /// string. That means it can return a value of 1 if the content contains the origin even if
        /// it has more chars around.
        /// </summary>
        /// <param name="content">String that contains eventually the origin</param>
        /// <param name="origin">String to search for</param>
        /// <returns>value from 0 to 1</returns>
        public static double EvaluateWholeStringSimilarity(string content, string origin)
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
        /// Get the string implementation of an emoji, regardless of its origin
        /// </summary>
        /// <param name="emoji">emoji object</param>
        /// <returns>coded string</returns>
        public static string GetCode(DiscordEmoji emoji)
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
        public static DiscordEmoji GetEmoji(string code)
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
        public static string GetSimplifiedString(string str)
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

        public static bool IsAdmin(this DiscordMember member)
            => member.IsOwner || member.Roles.Any(r => r.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed);

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