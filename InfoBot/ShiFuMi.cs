using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace InfoBot
{
    partial class Program
    {
        #region Public Enums

        public enum ShifumiChoice
        {
            ROCK = 0,
            PAPER,
            SCISSORS
        }

        #endregion Public Enums

        #region Public Methods

        public static async Task StartShiFuMiGame(DiscordMember user1, DiscordMember user2, int totalRounds, DiscordChannel call)
        {
            ShifumiChoice? choice1 = null, choice2 = null;
            //We give the permissions to the specials channels
            await ShiFuMiChannel[0].AddOverwriteAsync(user1, Permissions.AccessChannels | Permissions.SendMessages, Permissions.None, "start a shi fu mi game");
            await ShiFuMiChannel[1].AddOverwriteAsync(user2, Permissions.AccessChannels | Permissions.SendMessages, Permissions.None, "start a shi fu mi game");
            await call.SendMessageAsync(user2.DisplayName + " has to reply with \"start\"");
            bool gameStarted = false;
            async Task messCreated(MessageCreateEventArgs arg)
            {
                if (arg.Author == user2 && arg.Message.Content.ToLower() == "start")
                    gameStarted = true;
                //we parse the content of the messages
                if (arg.Channel == ShiFuMiChannel[0] && arg.Author == user1)
                {
                    switch (arg.Message.Content.ToLower())
                    {
                        case "r":
                            choice1 = ShifumiChoice.ROCK;
                            break;

                        case "p":
                            choice1 = ShifumiChoice.PAPER;
                            break;

                        case "s":
                            choice1 = ShifumiChoice.SCISSORS;
                            break;
                    }
                }
                if (arg.Channel == ShiFuMiChannel[1] && arg.Author == user2)
                {
                    switch (arg.Message.Content.ToLower())
                    {
                        case "r":
                            choice2 = ShifumiChoice.ROCK;
                            break;

                        case "p":
                            choice2 = ShifumiChoice.PAPER;
                            break;

                        case "s":
                            choice2 = ShifumiChoice.SCISSORS;
                            break;
                    }
                }
            };
            Discord.MessageCreated += messCreated;
            int round = 1;
            //pts is the score
            int pts1 = 0, pts2 = 0;
            var chrono = new Stopwatch();
            chrono.Start();
            //we wait 60 sec for the other to accept the game
            while (!gameStarted && chrono.Elapsed.TotalMinutes <= 1)
                Thread.Sleep(100);
            if (chrono.Elapsed > TimeSpan.FromMinutes(1))
            {
                //otherwise...
                await call.SendMessageAsync("60 seconds passed, game canceled");
                await ShiFuMiChannel[0].AddOverwriteAsync(user1, Permissions.None, Permissions.None, "finish a shi fu mi game");
                await ShiFuMiChannel[1].AddOverwriteAsync(user2, Permissions.None, Permissions.None, "finish a shi fu mi game");
                Discord.MessageCreated -= messCreated;
                return;
            }
            await call.SendMessageAsync("Go to " + ShiFuMiChannel[0].Mention + " or " + ShiFuMiChannel[1].Mention + " to start playing !");
            for (; round <= totalRounds; round++)
            {
                var sb = new StringBuilder();
                sb.Append("Round ");
                sb.Append(round.ToString() + "/" + totalRounds + "\n");
                sb.Append("Score : ");
                sb.Append(user1.DisplayName);
                sb.Append(" : ");
                sb.Append(pts1.ToString());
                sb.Append(" ; ");
                sb.Append(user2.DisplayName);
                sb.Append(" : ");
                sb.Append(pts2.ToString());
                sb.Append(
@"
You have 15 seconds to select one :
> ""r"" : rock
> ""p"" : paper
> ""s"" : scissors");
                foreach (var chan in ShiFuMiChannel)
                    await chan.SendMessageAsync(sb.ToString());
                chrono.Restart();
                //we wait 15 sec for the answers
                while ((choice1 == null || choice2 == null) && chrono.Elapsed.TotalSeconds < 15)
                    Thread.Sleep(100);
                if (choice1 == null && choice2 == null)
                {
                    foreach (var chan in ShiFuMiChannel)
                        await chan.SendMessageAsync("Both of you didn't reply, nothing changed.");
                }
                else if (choice1 == null)
                {
                    await ShiFuMiChannel[0].SendMessageAsync("You didn't reply, you automatically lose this round.");
                    await ShiFuMiChannel[1].SendMessageAsync("Your opponent didn't reply, you win this round.");
                }
                else if (choice2 == null)
                {
                    await ShiFuMiChannel[1].SendMessageAsync("You didn't reply, you automatically lose this round.");
                    await ShiFuMiChannel[0].SendMessageAsync("Your opponent didn't reply, you win this round.");
                }
                else
                {
                    await ShiFuMiChannel[0].SendMessageAsync("Results :\nYou : " + choice1.ToString() + "\n" + user2.DisplayName + " : " + choice2.ToString());
                    await ShiFuMiChannel[1].SendMessageAsync("Results :\nYou : " + choice2.ToString() + "\n" + user1.DisplayName + " : " + choice1.ToString());
                    if (((int)choice1 + 1) % 3 == (int)choice2)
                    {
                        await ShiFuMiChannel[0].SendMessageAsync("You lose !");
                        await ShiFuMiChannel[1].SendMessageAsync("You win !");
                        pts2++;
                    }
                    else if (((int)choice1 + 2) % 3 == (int)choice2)
                    {
                        await ShiFuMiChannel[0].SendMessageAsync("You win !");
                        await ShiFuMiChannel[1].SendMessageAsync("You lose !");
                        pts1++;
                    }
                    else
                    {
                        await ShiFuMiChannel[0].SendMessageAsync("Even !");
                        await ShiFuMiChannel[1].SendMessageAsync("Even !");
                    }
                }
                //we reset those obviously so the game doesn't continue by itself
                choice1 = null;
                choice2 = null;
            }
            {
                var sb = new StringBuilder();
                if (pts1 > pts2)
                {
                    sb.Append(user1.DisplayName);
                    sb.Append(" won the Shi fu mi !\n__Score__ :\n");
                    sb.Append(user1.DisplayName + " : " + pts1 + "\n");
                    sb.Append(user2.DisplayName + " : " + pts2 + "\n");
                }
                else if (pts2 > pts1)
                {
                    sb.Append(user2.DisplayName);
                    sb.Append(" won the Shi fu mi !\n__Score__ :\n");
                    sb.Append(user2.DisplayName + " : " + pts2 + "\n");
                    sb.Append(user1.DisplayName + " : " + pts1 + "\n");
                }
                else
                {
                    sb.Append("Even match of Shi fu mi !\n__Score__ :\n");
                    sb.Append(user1.DisplayName + " : " + pts1 + "\n");
                    sb.Append(user2.DisplayName + " : " + pts2 + "\n");
                }
                await call.SendMessageAsync(sb.ToString());
                //we remove the permissions
                await ShiFuMiChannel[0].AddOverwriteAsync(user1, Permissions.None, Permissions.None, "finish a shi fu mi game");
                await ShiFuMiChannel[1].AddOverwriteAsync(user2, Permissions.None, Permissions.None, "finish a shi fu mi game");
                Discord.MessageCreated -= messCreated;
            }
        }

        #endregion Public Methods
    }
}