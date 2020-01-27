using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net;
using System.Globalization;

namespace InfoBot
{
    /// <summary>
    /// Save class to keep autoruns
    /// </summary>
    internal struct Autorun
    {
        #region Public Fields

        public ulong author;
        public DateTime baseTime;
        public ulong channel;
        public TimeSpan delay;
        public string name;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep choices (reactions availables for polls)
    /// </summary>

    internal struct Choice
    {
        #region Public Fields

        public string id;
        public string name;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep polls
    /// </summary>

    internal struct Poll
    {
        #region Public Fields

        public ulong author;
        public Choice[] choices;
        public TimeSpan duration;
        public ulong id;
        public SpecialMessage message;

        public bool open;

        public bool showUsers;

        #endregion Public Fields
    }

    /// <summary>
    /// unused class for now, maybe i'll use it one day
    /// </summary>
    internal struct Range
    {
        #region Private Fields

        private float max;
        private float min;

        #endregion Private Fields
    }

    /// <summary>
    /// main class stored in data.json
    /// </summary>
    internal struct Save
    {
        #region Public Fields

        public Autorun[] autoruns;
        public DateTime currentSaveTime;
        public DateTime lastEdtCheck;
        public int[] oldEdT;
        public Poll[] polls;
        public SavedPoll[] savedPolls;
        public SavedVote[] savedVotes;
        public Vote[] votes;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep template polls
    /// </summary>

    internal struct SavedPoll
    {
        #region Public Fields

        public ulong author;
        public Choice[] choices;
        public string content;
        public TimeSpan duration;
        public string name;
        public bool open;
        public bool showUsers;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep template votes
    /// </summary>

    internal struct SavedVote
    {
        #region Public Fields

        public ulong author;
        public string content;
        public TimeSpan duration;
        public string name;
        public bool showUsers;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep messages that are important to retrieve
    /// </summary>

    internal struct SpecialMessage
    {
        #region Public Fields

        public ulong channel;
        public ulong id;

        #endregion Public Fields
    }

    /// <summary>
    /// Save class to keep votes
    /// </summary>

    internal struct Vote
    {
        #region Public Fields

        public ulong author;
        public TimeSpan duration;
        public ulong id;
        public SpecialMessage message;
        public bool showUsers;

        #endregion Public Fields
    }

    /// <summary>
    /// class for current polls
    /// </summary>
    public class PollMessage
    {
        #region Public Properties

        /// <summary>
        /// ... id of the author
        /// </summary>
        public ulong Author { get; set; }

        /// <summary>
        /// list of available reactions and their description
        /// </summary>
        public List<Tuple<string, DiscordEmoji>> Choices { get; set; }

        public ulong ID { get; set; }
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// Message of the poll
        /// </summary>
        public DiscordMessage Message { get; set; }

        public bool Open { get; set; }
        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    /// <summary>
    /// class for current votes
    /// </summary>
    public class VoteMessage
    {
        #region Public Properties

        /// <summary>
        /// ...
        /// </summary>
        public ulong Author { get; set; }

        public ulong ID { get; set; }
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// message of the vote
        /// </summary>
        public DiscordMessage Message { get; set; }

        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    partial class Program
    {
        #region Public Properties

        public static DiscordEmoji Downvote { get; set; }

        /// <summary>
        /// randomizer.
        /// </summary>
        public static Random Random { get; set; }

        public static DiscordEmoji Upvote { get; set; }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// active polls
        /// </summary>
        private static List<PollMessage> Polls { get; set; }

        /// <summary>
        /// active votes
        /// </summary>
        private static List<VoteMessage> Votes { get; set; }

        #endregion Private Properties

        #region Private Methods

        /// <summary>
        /// Here we initiate the commands declaration
        /// </summary>
        private static void InitCommands()
        {
            //these declarations don't belong here, i know
            Votes = new List<VoteMessage>();
            Polls = new List<PollMessage>();
            if (DUTInfoServer != null)
            {
                Upvote = DUTInfoServer.Emojis.First((e) => e.Name == "voteU");
                Downvote = DUTInfoServer.Emojis.First((e) => e.Name == "voteD");
            }
            else
            {
                Upvote = TestServer.Emojis.First((e) => e.Name == "upvote");
                Downvote = TestServer.Emojis.First((e) => e.Name == "downvote");
            }
            Discord.MessageCreated += async (arg) =>
            {
                Dispatcher.Execute(async () =>
                {
                    if (!arg.Channel.IsPrivate)
                        //if we detect a command using the prefix
                        if (arg.Message.Content.Length > 4 && arg.Message.Content.Substring(0, 4) == ">ib ")
                        {
                            var input = arg.Message.Content.Substring(4, arg.Message.Content.Length - 4);
                            ParseInput(input, out string command, out string[] args);
                            switch (command.ToLower())
                            {
                                case "help":
                                    await arg.Channel.SendMessageAsync(
@"Help pannel :
```
help                                            show this pannel.
    example : >ib help
______________________________________________________________
vote <question> [-d<duration (hours)>] [-u]                  start a vote (upvote / downvote), by default : 24 hour duration. Use -u if the users are shown at the end.
    example : >ib vote ""is pee stored in the balls"" -d1 -u
______________________________________________________________
figgle <text>                                   change the text into ascii art.
    example : >ib figgle ""big brain""
______________________________________________________________
poll <question> open|closed [-d<duration (hours)>] [-u] [<option1>] [<emoji1>] ...    start a poll with multiple choices. open if the choices can be multiple, otherwise closed. By default 24h. Use -u if the users are shown at the end.
    example : >ib poll ""who you gonna call ?"" closed -d1 ""ghostbusters !"" :+1: nobody :-1:
______________________________________________________________
finish <vote/poll id>                                       end the selected vote/poll to instantly show the results
    example : >ib finish 123456789
______________________________________________________________
template <name> poll|vote <poll/vote arguments>                  create a template for a poll/vote which you can call later using the ""call"" command
    example : >ib template peeStorageLocation vote ""is pee stored in the balls"" -d1 -u
______________________________________________________________
call <name>                                     start the poll/vote saved under the <name>
    example : >ib call peeStorageLocation
______________________________________________________________
```");
                                    await arg.Channel.SendMessageAsync(
    @"```
lt                                          display all the saved templates
    example : >ib lt
______________________________________________________________
rt <name>                                   remove a saved template
    example : >ib rt peeStorageLocation
______________________________________________________________
ct <origin> <target>                                copy a template under another name
    example : >ib peeStorageLocation copy
______________________________________________________________
automate <poll/vote name> [<delay>] [<start>]            call a saved poll/vote every <delay>. If defined, the first call will be delayed and happen at <hours>. by default, the automation is delayed by 24h, and it start immediately. units are in hours.
    example : >ib automate peeStorageLocation 24 18
______________________________________________________________
stopauto <name>                             stop the automated poll/vote
    example : >ib stopauto peeStorageLocation
______________________________________________________________
lauto                                       display all the current automated templates
    example : >ib lauto
______________________________________________________________
shifumi <other's tag> [<rounds>]                   start a shi fu mi game. The other has to reply with ""start"" within 60 seconds to accept. By default there is 3 turns.
    example : >ib shifumi @SomeGuy#1234 5
______________________________________________________________
```");
                                    await arg.Channel.SendMessageAsync(
    @"```
logic <equation>                                  generate a logic table of the equation
    example : >ib logic ""A -> !B""
______________________________________________________________
logic -help                                       display the help pannel for this command
______________________________________________________________
jpg [<quality>]                                   converts the linked image (or the image above the message) into a jpeg with a custom compression ranging from 0 to 100 (0 by default, to ""add more jpeg"")
    example : >ib jpg 50
______________________________________________________________
jpeg [<quality>]                                  same as above
    example : >ib jpeg 50
______________________________________________________________
deepfried [<amplitude>]                           transforms an image into a deep fried (or the image above the message) version. The amplitude ranges from 0 to 100 (100 by default for... D E E P   F R I E D)
    example : >ib deepfried 50
```");
                                    break;

                                case "deepfried":
                                    {
                                        Color CreateHSL(byte alpha, double hue, double saturation, double luminance)
                                        {
                                            double r = 0, g = 0, b = 0;
                                            double temp1, temp2;
                                            if (luminance == 0)
                                                r = g = b = 0;
                                            else
                                            {
                                                if (saturation == 0)
                                                    r = g = b = luminance;
                                                else
                                                {
                                                    temp2 = ((luminance <= 0.5) ? luminance * (1.0 + saturation) : luminance + saturation - (luminance * saturation));
                                                    temp1 = 2.0 * luminance - temp2;
                                                    hue /= 360;
                                                    double[] t3 = new double[] { hue + 1.0 / 3.0, hue, hue - 1.0 / 3.0 };
                                                    double[] clr = new double[] { 0, 0, 0 };
                                                    for (int i = 0; i < 3; i++)
                                                    {
                                                        if (t3[i] < 0)
                                                            t3[i] += 1.0;
                                                        if (t3[i] > 1)
                                                            t3[i] -= 1.0;
                                                        if (6.0 * t3[i] < 1.0)
                                                            clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0;
                                                        else if (2.0 * t3[i] < 1.0)
                                                            clr[i] = temp2;
                                                        else if (3.0 * t3[i] < 2.0)
                                                            clr[i] = (temp1 + (temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0);
                                                        else
                                                            clr[i] = temp1;
                                                    }
                                                    r = clr[0];
                                                    g = clr[1];
                                                    b = clr[2];
                                                }
                                            }
                                            return Color.FromArgb(alpha, (int)(255 * r), (int)(255 * g), (int)(255 * b));
                                        };
                                        double map(double value, double min, double max, double outMin, double outMax)
                                        {
                                            double perc = (value - min) / (max - min);
                                            return outMin + (outMax - outMin) * perc;
                                        };
                                        double amplitude = 1;
                                        if (args.Length > 0)
                                            amplitude = Math.Max(0, Math.Min(1, double.Parse(args.First()) / 100));
                                        var attachments = new List<Tuple<string, string>>();
                                        foreach (var attachment in arg.Message.Attachments)
                                            attachments.Add(new Tuple<string, string>(attachment.FileName, attachment.Url));
                                        if (attachments.Count == 0)
                                        {
                                            var messages = await arg.Channel.GetMessagesAsync(5, arg.Message.Id);
                                            foreach (var mess in messages)
                                            {
                                                var found = false;
                                                if (mess.Attachments.Count > 0)
                                                {
                                                    foreach (var att in mess.Attachments.Reverse())
                                                    {
                                                        var ext = Path.GetExtension(att.FileName);
                                                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
                                                        {
                                                            found = true;
                                                            attachments.Add(new Tuple<string, string>(att.FileName, att.Url));
                                                            break;
                                                        }
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                                {
                                                    for (int i = mess.Content.Length - 5; i >= 0; i--)
                                                    {
                                                        if (mess.Content.Substring(i, 4) == "http")
                                                        {
                                                            var url = "";
                                                            while (!char.IsWhiteSpace(mess.Content[i]))
                                                            {
                                                                url += mess.Content[i];
                                                                i++;
                                                                if (i == mess.Content.Length)
                                                                    break;
                                                            }
                                                            //https://stackoverflow.com/a/11083097
                                                            var req = WebRequest.Create(url);
                                                            req.Method = "HEAD";
                                                            using (var resp = req.GetResponse())
                                                            {
                                                                if (resp.ContentType.ToLower(CultureInfo.InvariantCulture).StartsWith("image/"))
                                                                {
                                                                    found = true;
                                                                    attachments.Add(new Tuple<string, string>("unknown.png", url));
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                            }
                                        }
                                        foreach (var attachment in attachments)
                                        {
                                            try
                                            {
                                                var filename = Path.GetFileNameWithoutExtension(attachment.Item1) + ".png";
                                                using var stream = Client.OpenRead(attachment.Item2);
                                                var img = new Bitmap(stream);
                                                for (int x = 0; x < img.Size.Width; x++)
                                                {
                                                    for (int y = 0; y < img.Size.Height; y++)
                                                    {
                                                        var pixel = img.GetPixel(x, y);
                                                        byte r = pixel.R;
                                                        byte g = pixel.G;
                                                        byte b = pixel.B;
                                                        byte noise = (byte)(Random.NextDouble() * amplitude * 150);
                                                        const double threshold = .2;
                                                        r = (byte)(r < amplitude * threshold * 255 ?
                                                                    0 :
                                                                    r > 255 - 255 * amplitude * threshold ?
                                                                        255 :
                                                                        map(r, 255 * amplitude * threshold, 255 - 255 * amplitude * threshold, 0, 255));
                                                        g = (byte)(g < amplitude * threshold * 255 ?
                                                                    0 :
                                                                    g > 255 - 255 * amplitude * threshold ?
                                                                        255 :
                                                                        map(g, 255 * amplitude * threshold, 255 - 255 * amplitude * threshold, 0, 255));
                                                        b = (byte)(b < amplitude * threshold * 255 ?
                                                                    0 :
                                                                    b > 255 - 255 * amplitude * threshold ?
                                                                        255 :
                                                                        map(b, 255 * amplitude * threshold, 255 - 255 * amplitude * threshold, 0, 255));
                                                        pixel = Color.FromArgb(pixel.A, r, g, b);
                                                        pixel = CreateHSL(
                                                            pixel.A,
                                                            pixel.GetHue(),
                                                            map(amplitude, 0, 1, pixel.GetSaturation(), 1),
                                                            map(amplitude, 0, 1, pixel.GetBrightness(), pixel.GetBrightness() > .97 ?
                                                                1 :
                                                                pixel.GetBrightness() < .05 ?
                                                                0 :
                                                                .5));
                                                        r = pixel.R;
                                                        g = pixel.G;
                                                        b = pixel.B;
                                                        if (Random.Next() % 2 == 0)
                                                        {
                                                            r += (byte)Math.Min(255 - r, noise);
                                                            g += (byte)Math.Min(255 - g, noise);
                                                            b += (byte)Math.Min(255 - b, noise);
                                                        }
                                                        else
                                                        {
                                                            r -= Math.Min(r, noise);
                                                            g -= Math.Min(g, noise);
                                                            b -= Math.Min(b, noise);
                                                        }
                                                        img.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                                                    }
                                                }
                                                using var memory = new MemoryStream();
                                                img.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                                memory.Seek(0, SeekOrigin.Begin);
                                                await arg.Message.RespondWithFileAsync(memory, filename);
                                            }
                                            catch (Exception) { }
                                        }
                                    }
                                    break;

                                case "jpeg":
                                case "jpg":
                                    {
                                        long quality = 0;
                                        if (args.Length > 0)
                                            quality = long.Parse(args.First());
                                        var attachments = new List<Tuple<string, string>>();
                                        foreach (var attachment in arg.Message.Attachments)
                                            attachments.Add(new Tuple<string, string>(attachment.FileName, attachment.Url));
                                        if (attachments.Count == 0)
                                        {
                                            var messages = await arg.Channel.GetMessagesAsync(5, arg.Message.Id);
                                            foreach (var mess in messages)
                                            {
                                                var found = false;
                                                if (mess.Attachments.Count > 0)
                                                {
                                                    foreach (var att in mess.Attachments.Reverse())
                                                    {
                                                        var ext = Path.GetExtension(att.FileName);
                                                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
                                                        {
                                                            found = true;
                                                            attachments.Add(new Tuple<string, string>(att.FileName, att.Url));
                                                            break;
                                                        }
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                                {
                                                    for (int i = mess.Content.Length - 5; i >= 0; i--)
                                                    {
                                                        if (mess.Content.Substring(i, 4) == "http")
                                                        {
                                                            var url = "";
                                                            while (!char.IsWhiteSpace(mess.Content[i]))
                                                            {
                                                                url += mess.Content[i];
                                                                i++;
                                                                if (i == mess.Content.Length)
                                                                    break;
                                                            }
                                                            //https://stackoverflow.com/a/11083097
                                                            var req = WebRequest.Create(url);
                                                            req.Method = "HEAD";
                                                            using (var resp = req.GetResponse())
                                                            {
                                                                if (resp.ContentType.ToLower(CultureInfo.InvariantCulture).StartsWith("image/"))
                                                                {
                                                                    found = true;
                                                                    attachments.Add(new Tuple<string, string>("unknown.png", url));
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                            }
                                        }
                                        foreach (var attachment in attachments)
                                        {
                                            var filename = Path.GetFileNameWithoutExtension(attachment.Item1) + ".jpg";
                                            try
                                            {
                                                using var stream = Client.OpenRead(attachment.Item2);
                                                var img = new Bitmap(stream);
                                                var jpgEncoder = ImageCodecInfo.GetImageDecoders().First(dec => dec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                                                var parameters = new EncoderParameters(1);
                                                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                                                using var memory = new MemoryStream();
                                                img.Save(memory, jpgEncoder, parameters);
                                                memory.Seek(0, SeekOrigin.Begin);
                                                await arg.Message.RespondWithFileAsync(memory, filename);
                                            }
                                            catch (Exception) { }
                                        }
                                    }
                                    break;

                                case "logic":
                                    {
                                        if (args[0] == "-help")
                                            await arg.Message.RespondAsync("The variables must contain only letters and/or digits, and need to start with a" +
                        "letter. Accepted operators : & (and), | (or), ->, <->, ! (not). Priority order : (), !, &, |, →, 🡘");
                                        else
                                        {
                                            var converter = new CoreHtmlToImage.HtmlConverter();
                                            var bytes = converter.FromHtmlString(LogicTable.Parsing.GenerateHTML(args[0]), 500, CoreHtmlToImage.ImageFormat.Png);
                                            await arg.Message.RespondWithFileAsync(new MemoryStream(bytes), "output.png");
                                        }
                                    }
                                    break;

                                case "shifumi":
                                    {
                                        if (args.Length > 0)
                                        {
                                            var rounds = args.Length > 1 ? int.Parse(args[1]) : 3;
                                            var user1 = await arg.Guild.GetMemberAsync(arg.Message.Author.Id);
                                            var user2 = await arg.Guild.GetMemberAsync(arg.Message.MentionedUsers.First().Id);
                                            _ = Task.Run(async () => await StartShiFuMiGame(user1, user2, rounds, arg.Channel));
                                        }
                                    }
                                    break;

                                case "stopauto":
                                    {
                                        //we stop the autorun given in paramter
                                        //this line is used to identify a template by its id, which is the id of the server AND the name of the template.
                                        //This won't be commented after this point
                                        string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                        if (Autoruns.Any(s => s.name == codedName))
                                        {
                                            var toRemove = Autoruns.FirstOrDefault((s) => s.name == codedName);
                                            //we test if the user has the rights to stop the autorun
                                            if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                Autoruns.RemoveAll((s) => s.name == codedName);
                                            else
                                                await arg.Message.RespondAsync("Error : You don't have the permission");
                                        }
                                        SaveData();
                                    }
                                    break;

                                case "lauto":
                                    {
                                        var builder = new StringBuilder();
                                        builder.Append("**Automations :**");
                                        foreach (var item in Autoruns)
                                        {
                                            //we just display all the current autoruns
                                            if (item.name.Split(':').First() == arg.Guild.Id.ToString())
                                                builder.Append("\n  -  __" + item.name.Split(':').Last() + "__ by " + (await arg.Guild.GetMemberAsync(arg.Author.Id)).Nickname + ", delayed by " + item.delay.TotalHours + "h, next call : " + (item.baseTime + item.delay).ToString());
                                        }
                                        await arg.Message.RespondAsync(builder.ToString());
                                    }
                                    break;

                                case "automate":
                                    {
                                        string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                        if (Autoruns.Any((a) => a.name == codedName))
                                        {
                                            await arg.Message.RespondAsync("Error : This template is already automated");
                                            break;
                                        }
                                        var auto = new Autorun();
                                        auto.channel = arg.Channel.Id;
                                        auto.name = codedName;
                                        auto.author = arg.Author.Id;
                                        //if the delay is specified, we parse it
                                        auto.delay = args.Length > 1 ? TimeSpan.FromHours(double.Parse(args[1].Replace('.', ','))) : TimeSpan.FromDays(1);
                                        auto.baseTime = DateTime.Now - auto.delay;
                                        if (args.Length > 2)
                                        {
                                            //if a starting hour is given, we parse it
                                            var hours = double.Parse(args[2].Replace('.', ','));
                                            //i kinda forgot how this works, but it checks that it will start at the moment
                                            auto.baseTime = hours > (DateTime.Now - DateTime.Today).TotalHours ?
                                                                DateTime.Today - auto.delay + TimeSpan.FromHours(hours) :
                                                                DateTime.Today + TimeSpan.FromHours(hours);
                                        }
                                        if (!SavedVotes.Any((v) => v.name == codedName) && !SavedPolls.Any((p) => p.name == codedName))
                                            await arg.Message.RespondAsync("Warning : no templates found for this name, it will have no effect while there are no templates found0");
                                        Autoruns.Add(auto);
                                        SaveData();
                                    }
                                    break;

                                case "ct":
                                    {
                                        string targetCode = arg.Guild.Id.ToString() + ":" + args[1];
                                        string originCode = arg.Guild.Id.ToString() + ":" + args[0];
                                        {
                                            //basic copy paste template, idk why you would use it tho
                                            if (SavedVotes.Any(s => s.name == targetCode))
                                            {
                                                //if it's a vote
                                                var toRemove = SavedVotes.First((s) => s.name == targetCode);
                                                if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                    SavedVotes.RemoveAll((s) => s.name == targetCode);
                                                else
                                                {
                                                    await arg.Message.RespondAsync("Error : You don't have the permission to overwrite an existing template");
                                                    break;
                                                }
                                            }
                                        }
                                        {
                                            if (SavedPolls.Any(s => s.name == targetCode))
                                            {
                                                //if it's a poll
                                                var toRemove = SavedPolls.FirstOrDefault((s) => s.name == targetCode);
                                                if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                    SavedPolls.RemoveAll((s) => s.name == targetCode);
                                                else
                                                {
                                                    await arg.Message.RespondAsync("Error : You don't have the permission to overwrite an existing template");
                                                    break;
                                                }
                                            }
                                        }
                                        //and we copy it
                                        if (SavedVotes.Any(s => s.name == originCode))
                                        {
                                            var toCopy = SavedVotes.First((s) => s.name == originCode);
                                            var copied = new SavedVote();
                                            copied.name = targetCode;
                                            copied.author = arg.Author.Id;
                                            copied.content = toCopy.content;
                                            copied.duration = toCopy.duration;
                                            copied.showUsers = toCopy.showUsers;
                                            SavedVotes.Add(copied);
                                        }
                                        else if (SavedPolls.Any(s => s.name == originCode))
                                        {
                                            var toCopy = SavedPolls.First((s) => s.name == originCode);
                                            var copied = new SavedPoll();
                                            copied.name = targetCode;
                                            copied.author = arg.Author.Id;
                                            copied.content = toCopy.content;
                                            copied.duration = toCopy.duration;
                                            copied.showUsers = toCopy.showUsers;
                                            copied.choices = toCopy.choices;
                                            copied.open = toCopy.open;
                                            SavedPolls.Add(copied);
                                        }
                                        else
                                            await arg.Message.RespondAsync("Error : No origin template found.");
                                        SaveData();
                                    }
                                    break;

                                case "rt":
                                    {
                                        string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                        //here we delete a template
                                        if (SavedVotes.Any(s => s.name == codedName))
                                        {
                                            //if it's a vote
                                            var toRemove = SavedVotes.First((s) => s.name == codedName);
                                            if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                SavedVotes.RemoveAll((s) => s.name == codedName);
                                            else
                                                await arg.Message.RespondAsync("Error : You don't have the permission");
                                        }
                                        if (SavedPolls.Any(s => s.name == codedName))
                                        {
                                            //if it's a poll
                                            var toRemove = SavedPolls.FirstOrDefault((s) => s.name == codedName);
                                            if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                SavedPolls.RemoveAll((s) => s.name == codedName);
                                            else
                                                await arg.Message.RespondAsync("Error : You don't have the permission");
                                        }
                                        SaveData();
                                    }
                                    break;

                                case "lt":
                                    {
                                        var builder = new StringBuilder();
                                        builder.Append("**Votes** :");
                                        //we basically displays vote templates and poll templates
                                        foreach (var item in SavedVotes)
                                        {
                                            if (arg.Guild.Id.ToString() == item.name.Split(':').First())
                                            {
                                                builder.Append("\n  -  __" + item.name.Split(':').Last()).Append("__ by " + (await arg.Guild.GetMemberAsync(item.author)).Nickname);
                                            }
                                        }
                                        builder.Append("\n**Polls** :");
                                        foreach (var item in SavedPolls)
                                        {
                                            if (arg.Guild.Id.ToString() == item.name.Split(':').First())
                                            {
                                                builder.Append("\n  -  __" + item.name.Split(':').Last()).Append("__ by " + (await arg.Guild.GetMemberAsync(item.author)).Nickname);
                                            }
                                        }
                                        await arg.Message.RespondAsync(builder.ToString());
                                    }
                                    break;

                                case "call":
                                    {
                                        //we call a vote template / poll template
                                        string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                        if (SavedVotes.Any((s) => s.name == codedName))
                                        {
                                            //if it's a vote
                                            var saved = SavedVotes.Find((s) => s.name == codedName);
                                            var buff = new byte[8];
                                            Random.NextBytes(buff);
                                            ulong id = BitConverter.ToUInt64(buff);
                                            string content = saved.content + " \n||id:" + id + "||";
                                            DiscordMessage message;
                                            message = await arg.Message.RespondAsync(content);
                                            {
                                                await message.CreateReactionAsync(Upvote);
                                                await message.CreateReactionAsync(Downvote);
                                                Votes.Add(new VoteMessage() { Author = arg.Author.Id, ID = id, Message = message, Lifetime = saved.duration, ShowUsers = saved.showUsers });
                                                SaveData();
                                            }
                                        }
                                        else if (SavedPolls.Any((s) => s.name == codedName))
                                        {
                                            //if it's a poll
                                            var saved = SavedPolls.Find((s) => s.name == codedName);
                                            var buff = new byte[8];
                                            Random.NextBytes(buff);
                                            ulong idPoll = BitConverter.ToUInt64(buff);
                                            var question = saved.content + "\n||id:" + idPoll + "||";
                                            List<Tuple<string, DiscordEmoji>> reactions = new List<Tuple<string, DiscordEmoji>>();
                                            for (int i = 0; i < saved.choices.Length; i++)
                                            {
                                                var choice = saved.choices[i];
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

                                            var message = await arg.Channel.SendMessageAsync(builder.ToString());
                                            Polls.Add(new PollMessage()
                                            {
                                                Choices = reactions,
                                                Lifetime = saved.duration,
                                                Message = message,
                                                ShowUsers = saved.showUsers,
                                                Open = saved.open,
                                                Author = arg.Author.Id,
                                                ID = idPoll
                                            });
                                            foreach (var item in reactions)
                                                await message.CreateReactionAsync(item.Item2);
                                            SaveData();
                                        }
                                        else
                                            await arg.Message.RespondAsync("Error : No template named `" + args[0] + "` found");
                                    }
                                    break;

                                case "template":
                                    {
                                        //we simply create a template
                                        string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                        {
                                            if (SavedVotes.Any(s => s.name == codedName))
                                            {
                                                //if a template vote already has a name for it
                                                var toRemove = SavedVotes.First((s) => s.name == codedName);
                                                if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                    SavedVotes.RemoveAll((s) => s.name == codedName);
                                                else
                                                {
                                                    await arg.Message.RespondAsync("Error : You don't have the permission to overwrite an existing template");
                                                    break;
                                                }
                                            }
                                        }
                                        {
                                            if (SavedPolls.Any(s => s.name == codedName))
                                            {
                                                //if a template poll already has a name for it
                                                var toRemove = SavedPolls.FirstOrDefault((s) => s.name == codedName);
                                                if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                    SavedPolls.RemoveAll((s) => s.name == codedName);
                                                else
                                                {
                                                    await arg.Message.RespondAsync("Error : You don't have the permission to overwrite an existing template");
                                                    break;
                                                }
                                            }
                                        }
                                        SaveData();
                                    }
                                    if (args[1] == "vote")
                                    {
                                        var toSave = new SavedVote();
                                        {
                                            TimeSpan duration = TimeSpan.FromHours(24);
                                            bool showUsers = false;
                                            if (args.Length > 3)
                                            {
                                                foreach (var item in args)
                                                {
                                                    if (item.Substring(0, 2) == "-d")
                                                        duration = TimeSpan.FromHours(double.Parse(item.Substring(2).Replace('.', ',')));
                                                    if (item == "-u")
                                                        showUsers = true;
                                                }
                                            }
                                            toSave.author = arg.Author.Id;
                                            toSave.name = arg.Guild.Id.ToString() + ":" + args[0];
                                            toSave.content = args[2];
                                            toSave.duration = duration;
                                            toSave.showUsers = showUsers;
                                            SavedVotes.Add(toSave);
                                            SaveData();
                                        }
                                    }
                                    else if (args[1] == "poll")
                                    {
                                        var toSave = new SavedPoll();
                                        {
                                            //good lord wtf is this spaghetti
                                            bool open = args[1] == "open";
                                            bool showUsers = false;
                                            TimeSpan duration = TimeSpan.FromHours(24);
                                            int choiceIndex = 4;
                                            if (args[choiceIndex].Substring(0, 2) == "-d")
                                            {
                                                duration = TimeSpan.FromHours(double.Parse(args[choiceIndex].Substring(2).Replace('.', ',')));
                                                choiceIndex++;
                                            }
                                            if (args[choiceIndex] == "-u")
                                            {
                                                showUsers = true;
                                                choiceIndex++;
                                            }
                                            if (args[choiceIndex].Substring(0, 2) == "-d")
                                            {
                                                duration = TimeSpan.FromHours(double.Parse(args[choiceIndex].Substring(2).Replace('.', ',')));
                                                choiceIndex++;
                                            }
                                            List<Tuple<string, DiscordEmoji>> reactions = new List<Tuple<string, DiscordEmoji>>();
                                            for (; choiceIndex < args.Length; choiceIndex += 2)
                                            {
                                                reactions.Add(new Tuple<string, DiscordEmoji>(args[choiceIndex], GetEmoji(args[choiceIndex + 1])));
                                            }
                                            toSave.author = arg.Author.Id;
                                            toSave.name = arg.Guild.Id.ToString() + ":" + args[0];
                                            toSave.content = args[2];
                                            toSave.duration = duration;
                                            toSave.showUsers = showUsers;
                                            toSave.open = open;
                                            toSave.choices = reactions.Select((t) => new Choice() { id = GetCode(t.Item2), name = t.Item1 }).ToArray();
                                            SavedPolls.Add(toSave);
                                            SaveData();
                                        }
                                    }
                                    break;

                                case "vote":
                                    {
                                        //we create a vote
                                        var buff = new byte[8];
                                        Random.NextBytes(buff);
                                        //here we just generate an id for the vote
                                        ulong id = BitConverter.ToUInt64(buff);
                                        string content = args[0] + " \n||id:" + id + "||";
                                        TimeSpan duration = TimeSpan.FromHours(24);
                                        bool showUsers = false;
                                        if (args.Length > 1)
                                        {
                                            foreach (var item in args)
                                            {
                                                if (item.Substring(0, 2) == "-d")
                                                    duration = TimeSpan.FromHours(double.Parse(item.Substring(2).Replace('.', ',')));
                                                if (item == "-u")
                                                    showUsers = true;
                                            }
                                        }
                                        DiscordMessage message;
                                        message = await arg.Message.RespondAsync(content);
                                        {
                                            await message.CreateReactionAsync(Upvote);
                                            await message.CreateReactionAsync(Downvote);
                                            Votes.Add(new VoteMessage() { Author = arg.Author.Id, ID = id, Message = message, Lifetime = duration, ShowUsers = showUsers });
                                            SaveData();
                                        }
                                    }
                                    break;

                                case "figgle":
                                    var toFiggle = args[0];
                                    await arg.Channel.SendMessageAsync("```\n" + Figgle.FiggleFonts.Standard.Render(toFiggle) + "\n```");
                                    break;

                                case "finish":
                                    {
                                        var toFinish = ulong.Parse(args[0]);
                                        foreach (var item in Votes)
                                        {
                                            //if it's a vote to finish
                                            if (item.ID == toFinish)
                                            {
                                                //we literally sets its lifetime to zero so he dies instantly lol
                                                if (arg.Author.Id == item.Author || (await arg.Guild.GetMemberAsync(arg.Author.Id)).PermissionsIn(arg.Channel) == Permissions.Administrator)
                                                    item.Lifetime = TimeSpan.Zero;
                                                else
                                                    await arg.Channel.SendMessageAsync("Error : You are not worthy enough !");
                                            }
                                        }
                                        foreach (var item in Polls)
                                        {
                                            //if it's a poll to finish
                                            if (item.ID == toFinish)
                                            {
                                                if (arg.Author.Id == item.Author || (await arg.Guild.GetMemberAsync(arg.Author.Id)).PermissionsIn(arg.Channel) == Permissions.Administrator)
                                                    item.Lifetime = TimeSpan.Zero;
                                                else
                                                    await arg.Channel.SendMessageAsync("Error : You are not worthy enough !");
                                            }
                                        }
                                    }
                                    break;

                                case "poll":
                                    {
                                        //we create a poll, and i'm not in the mood to comment this part, it just works
                                        var buff = new byte[8];
                                        Random.NextBytes(buff);
                                        ulong idPoll = BitConverter.ToUInt64(buff);
                                        var question = args[0] + "\n||id:" + idPoll + "||";
                                        bool open = args[1] == "open";
                                        bool showUsers = false;
                                        TimeSpan duration = TimeSpan.FromHours(24);
                                        int choiceIndex = 2;
                                        if (args[choiceIndex].Substring(0, 2) == "-d")
                                        {
                                            duration = TimeSpan.FromHours(double.Parse(args[choiceIndex].Substring(2).Replace('.', ',')));
                                            choiceIndex++;
                                        }
                                        if (args[choiceIndex] == "-u")
                                        {
                                            showUsers = true;
                                            choiceIndex++;
                                        }
                                        if (args[choiceIndex].Substring(0, 2) == "-d")
                                        {
                                            duration = TimeSpan.FromHours(double.Parse(args[choiceIndex].Substring(2).Replace('.', ',')));
                                            choiceIndex++;
                                        }
                                        List<Tuple<string, DiscordEmoji>> reactions = new List<Tuple<string, DiscordEmoji>>();
                                        for (; choiceIndex < args.Length; choiceIndex += 2)
                                        {
                                            reactions.Add(new Tuple<string, DiscordEmoji>(args[choiceIndex], GetEmoji(args[choiceIndex + 1])));
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

                                        var message = await arg.Channel.SendMessageAsync(builder.ToString());
                                        Polls.Add(new PollMessage()
                                        {
                                            Choices = reactions,
                                            Lifetime = duration,
                                            Message = message,
                                            ShowUsers = showUsers,
                                            Open = open,
                                            Author = arg.Author.Id,
                                            ID = idPoll
                                        });
                                        foreach (var item in reactions)
                                            await message.CreateReactionAsync(item.Item2);
                                        SaveData();
                                    }
                                    break;

                                default:
                                    await arg.Channel.SendMessageAsync("Error : Unknown command, type \">ib help\"");
                                    break;
                            }
                        }
                });
            };
            Discord.MessageReactionAdded += async (arg) =>
            {
                //this is triggered for the vote/poll, when a reaction has been added
                Dispatcher.Execute(async () =>
                {
                    {
                        //vote
                        //if the reaction belongs to a current vote
                        var message = Votes.Find((m) => arg.Message.ChannelId == m.Message.ChannelId && arg.Message.Id == m.Message.Id);
                        if (message != null && !arg.User.IsBot)
                        {
                            if (arg.Emoji.Id == Upvote.Id)
                            {
                                //if it's an upvote, we delete the eventual downvote
                                var downvotes = await message.Message.GetReactionsAsync(Downvote);
                                if (downvotes.Any((u) => u.Id == arg.User.Id))
                                    await message.Message.DeleteReactionAsync(Downvote, arg.User);
                            }
                            //if it's an downvote, we delete the eventual upvote
                            else if (arg.Emoji.Id == Downvote.Id)
                            {
                                var upvotes = await message.Message.GetReactionsAsync(Upvote);
                                if (upvotes.Any((u) => u.Id == arg.User.Id))
                                    await message.Message.DeleteReactionAsync(Upvote, arg.User);
                            }
                        }
                    }
                    {
                        //poll
                        //almost same as above, you can do it
                        var message = Polls.Find((m) => arg.Message.ChannelId == m.Message.ChannelId && arg.Message.Id == m.Message.Id);
                        if (message != null && !arg.User.IsBot)
                        {
                            if (!message.Choices.Any((t) => t.Item2.Id == arg.Emoji.Id))
                                await arg.Message.DeleteReactionAsync(arg.Emoji, arg.User);
                            else if (!message.Open)
                            {
                                foreach (var item in message.Choices)
                                {
                                    if (arg.Emoji.Id != item.Item2.Id)
                                    {
                                        if ((await arg.Message.GetReactionsAsync(item.Item2)).Contains(arg.User))
                                            await arg.Message.DeleteReactionAsync(item.Item2, arg.User);
                                    }
                                }
                            }
                        }
                    }
                });
            };

            Discord.MessageReactionRemoved += async (arg) =>
            {
                Dispatcher.Execute(async () =>
                {
                    //not used for now...
                });
            };
        }

        #endregion Private Methods
    }
}