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
    /// <summary>
    /// Utility methods
    /// </summary>
    public static class Utilities
    {
        #region Public Methods

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
        /// Call a function for every item
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="list">list of the items</param>
        /// <param name="call">function to call</param>
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> call) => ForEach(list, (item, index) => call(item));

        /// <summary>
        /// Call a function for every item
        /// </summary>
        /// <typeparam name="T">type of the items</typeparam>
        /// <param name="list">list of the items</param>
        /// <param name="call">function to call</param>
        public static void ForEach<T>(this IEnumerable<T> list, Action<T, int> call)
        {
            var index = 0;
            foreach (var item in list)
            {
                call(item, index);
                ++index;
            }
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
                emoji = DiscordEmoji.FromGuildEmote(Program.Discord, ulong.Parse(id.Substring(0, id.Length - 1)));
            }
            catch (Exception)
            {
                //If the emoji is native (Unicode)
                emoji = DiscordEmoji.FromUnicode(Program.Discord, code);
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

        /// <summary>
        /// Returns true if the member is admin
        /// </summary>
        /// <param name="member">member to test</param>
        /// <returns>True if the member is an admin, false otherwise</returns>
        public static bool IsAdmin(this DiscordMember member)
                                                    => member.IsOwner || member.Roles.Any(r => r.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed);

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

        #endregion Private Methods
    }
}