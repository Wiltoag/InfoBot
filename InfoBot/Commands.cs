using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace InfoBot
{
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

        public Choice[] choices;

        public TimeSpan duration;

        public SpecialMessage message;

        public bool open;

        public bool showUsers;

        #endregion Public Fields

        #region Public Structs

        public struct Choice
        {
            #region Public Fields

            public ulong id;
            public string name;

            #endregion Public Fields
        }

        #endregion Public Structs
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

        public DateTime currentSaveTime;
        public List<EdtDayMessage>[] edtMessages;
        public int[] oldEdT;
        public Poll[] polls;
        public Vote[] votes;

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

        public TimeSpan duration;
        public SpecialMessage message;
        public bool showUsers;

        #endregion Public Fields
    }

    public class PollMessage
    {
        #region Public Properties

        public List<Tuple<string, DiscordEmoji>> Choices { get; set; }
        public TimeSpan Lifetime { get; set; }
        public DiscordMessage Message { get; set; }
        public bool Open { get; set; }
        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    public class VoteMessage
    {
        #region Public Properties

        public TimeSpan Lifetime { get; set; }
        public DiscordMessage Message { get; set; }
        public bool ShowUsers { get; set; }

        #endregion Public Properties
    }

    partial class Program
    {
        #region Public Properties

        public static DiscordEmoji Downvote { get; set; }
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

vote <question> [-d<duration (hours)>] [-u]                  start a vote (upvote / downvote), by default : 24 hour duration. Use -u if the users are shown at the end.

figgle <text>                                   change the text into ascii art.

poll <question> <open|closed> [-d<duration (hours)>] [-u] [<option1>] [<emoji1>] ...    start a poll with multiple choices. open if the choices can be multiple, otherwise closed. By default 24h. Use -u if the users are shown at the end.
```");
                                break;

                            case "vote":
                                {
                                    string content = args[0];
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
                                        Votes.Add(new VoteMessage() { Message = message, Lifetime = duration, ShowUsers = showUsers });
                                        SaveData();
                                    }
                                }
                                break;

                            case "figgle":
                                var toFiggle = args[0];
                                await arg.Channel.SendMessageAsync("```\n" + Figgle.FiggleFonts.Standard.Render(toFiggle) + "\n```");
                                break;

                            case "poll":
                                {
                                    var question = args[0];
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
                                        var choice = args[choiceIndex];
                                        DiscordEmoji emoji;
                                        try
                                        {
                                            var id = args[choiceIndex + 1].Split(':').Last();
                                            emoji = DiscordEmoji.FromGuildEmote(Discord, ulong.Parse(id.Substring(0, id.Length - 1)));
                                        }
                                        catch (Exception e)
                                        {
                                            emoji = DiscordEmoji.FromUnicode(Discord, args[choiceIndex + 1]);
                                        }
                                        reactions.Add(new Tuple<string, DiscordEmoji>(choice, emoji));
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
                                        Open = open
                                    });
                                    SaveData();
                                    foreach (var item in reactions)
                                        await message.CreateReactionAsync(item.Item2);
                                }
                                break;

                            default:
                                await arg.Channel.SendMessageAsync("Unknown command, type \">ib help\"");
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