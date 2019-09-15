using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace InfoBot
{
    internal struct Save
    {
        #region Public Fields

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

        #endregion Public Fields
    }

    internal class DynamicMessage
    {
        #region Public Properties

        public TimeSpan Lifetime { get; set; }
        public DiscordMessage Message { get; set; }

        #endregion Public Properties
    }

    partial class Program
    {
        #region Public Properties

        public static DiscordEmoji Downvote { get; set; }
        public static DiscordEmoji Upvote { get; set; }

        #endregion Public Properties

        #region Private Properties

        private static List<DynamicMessage> Votes { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static void InitCommands()
        {
            Votes = new List<DynamicMessage>();
            Upvote = DUTInfoServer.Emojis.First((e) => e.Name == "voteU");
            Downvote = DUTInfoServer.Emojis.First((e) => e.Name == "voteD");
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
vote <question> [duration (hours)]                  start a vote (upvote / downvote), by default : 24 hour duration.
figgle <text>                                   change the text into ascii art.
```");
                                break;

                            case "vote":
                                string content = args[0];
                                TimeSpan duration = TimeSpan.FromHours(24);
                                if (args.Length > 1)
                                    duration = TimeSpan.FromHours(double.Parse(args[1].Replace('.', ',')));
                                DiscordMessage message;
                                message = await arg.Message.RespondAsync(content);
                                {
                                    await message.CreateReactionAsync(Upvote);
                                    await message.CreateReactionAsync(Downvote);
                                    Votes.Add(new DynamicMessage() { Message = message, Lifetime = duration });
                                    SaveData();
                                }
                                break;

                            case "figgle":
                                var toFiggle = args[0];
                                await arg.Channel.SendMessageAsync("```\n" + Figgle.FiggleFonts.Standard.Render(toFiggle) + "\n```");
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
                    var message = Votes.Find((m) => arg.Message.ChannelId == m.Message.ChannelId && arg.Message.CreationTimestamp == m.Message.CreationTimestamp);
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