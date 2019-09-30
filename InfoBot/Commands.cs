using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace InfoBot
{
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

    internal struct Choice
    {
        #region Public Fields

        public string id;
        public string name;

        #endregion Public Fields
    }

    internal struct EdtDayMessage
    {
        #region Public Fields

        public string content;
        public DateTime day;
        public string description;
        public SpecialMessage message;

        #endregion Public Fields
    }

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

    internal struct Range
    {
        #region Private Fields

        private float max;
        private float min;

        #endregion Private Fields
    }

    internal struct Save
    {
        #region Public Fields

        public Autorun[] autoruns;
        public DateTime currentSaveTime;
        public List<EdtDayMessage>[] edtMessages;
        public int[] oldEdT;
        public Poll[] polls;
        public SavedPoll[] savedPolls;
        public SavedVote[] savedVotes;
        public Vote[] votes;

        #endregion Public Fields
    }

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

    internal struct SpecialMessage
    {
        #region Public Fields

        public ulong channel;
        public ulong id;

        #endregion Public Fields
    }

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

    public class PollMessage
    {
        #region Public Properties

        public ulong Author { get; set; }
        public List<Tuple<string, DiscordEmoji>> Choices { get; set; }
        public ulong ID { get; set; }
        public TimeSpan Lifetime { get; set; }
        public DiscordMessage Message { get; set; }
        public bool Open { get; set; }
        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    public class VoteMessage
    {
        #region Public Properties

        public ulong Author { get; set; }
        public ulong ID { get; set; }
        public TimeSpan Lifetime { get; set; }
        public DiscordMessage Message { get; set; }
        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    partial class Program
    {
        #region Public Properties

        public static DiscordEmoji Downvote { get; set; }
        public static Random Random { get; set; }
        public static DiscordEmoji Upvote { get; set; }

        #endregion Public Properties

        #region Private Properties

        private static List<PollMessage> Polls { get; set; }
        private static List<VoteMessage> Votes { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static void InitCommands()
        {
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
                    if (arg.Message.Content.Length > 4 && arg.Message.Content.Substring(0, 4) == ">ib ")
                    {
                        var input = arg.Message.Content.Substring(4, arg.Message.Content.Length - 4);
                        ParseInput(input, out string command, out string[] args);
                        switch (command)
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
```");
                                break;

                            case "stopauto":
                                {
                                    string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                    if (Autoruns.Any(s => s.name == codedName))
                                    {
                                        var toRemove = Autoruns.FirstOrDefault((s) => s.name == codedName);
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
                                    auto.delay = args.Length > 1 ? TimeSpan.FromHours(double.Parse(args[1].Replace('.', ','))) : TimeSpan.FromDays(1);
                                    auto.baseTime = DateTime.Now - auto.delay;
                                    if (args.Length > 2)
                                    {
                                        var hours = double.Parse(args[2].Replace('.', ','));
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
                                        if (SavedVotes.Any(s => s.name == targetCode))
                                        {
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
                                    {
                                        if (SavedVotes.Any(s => s.name == codedName))
                                        {
                                            var toRemove = SavedVotes.First((s) => s.name == codedName);
                                            if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                SavedVotes.RemoveAll((s) => s.name == codedName);
                                            else
                                                await arg.Message.RespondAsync("Error : You don't have the permission");
                                        }
                                    }
                                    {
                                        if (SavedPolls.Any(s => s.name == codedName))
                                        {
                                            var toRemove = SavedPolls.FirstOrDefault((s) => s.name == codedName);
                                            if (toRemove.author == arg.Author.Id || arg.Channel.PermissionsFor(await arg.Guild.GetMemberAsync(arg.Author.Id)) == Permissions.Administrator)
                                                SavedPolls.RemoveAll((s) => s.name == codedName);
                                            else
                                                await arg.Message.RespondAsync("Error : You don't have the permission");
                                        }
                                    }
                                    SaveData();
                                }
                                break;

                            case "lt":
                                {
                                    var builder = new StringBuilder();
                                    builder.Append("**Votes** :");
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
                                    string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                    if (SavedVotes.Any((s) => s.name == codedName))
                                    {
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
                                    string codedName = arg.Guild.Id.ToString() + ":" + args[0];
                                    {
                                        if (SavedVotes.Any(s => s.name == codedName))
                                        {
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
                                    var buff = new byte[8];
                                    Random.NextBytes(buff);
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
                                        if (item.ID == toFinish)
                                        {
                                            if (arg.Author.Id == item.Author || (await arg.Guild.GetMemberAsync(arg.Author.Id)).PermissionsIn(arg.Channel) == Permissions.Administrator)
                                                item.Lifetime = TimeSpan.Zero;
                                            else
                                                await arg.Channel.SendMessageAsync("Error : You are not worthy enough !");
                                        }
                                    }
                                    foreach (var item in Polls)
                                    {
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
                Dispatcher.Execute(async () =>
                {
                    {
                        //vote
                        var message = Votes.Find((m) => arg.Message.ChannelId == m.Message.ChannelId && arg.Message.Id == m.Message.Id);
                        if (message != null && !arg.User.IsBot)
                        {
                            if (arg.Emoji.Id == Upvote.Id)
                            {
                                var downvotes = await message.Message.GetReactionsAsync(Downvote);
                                if (downvotes.Any((u) => u.Id == arg.User.Id))
                                    await message.Message.DeleteReactionAsync(Downvote, arg.User);
                            }
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
                });
            };
        }

        #endregion Private Methods
    }
}