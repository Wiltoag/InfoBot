using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal class Sharing : ISetup
    {
        #region Private Fields

        private static DiscordEmoji emoji;

        #endregion Private Fields

        #region Public Methods

        public void Connected()
        {
        }

        public void Setup()
        {
            emoji = null;
            Program.Discord.MessageCreated += async (e) =>
            {
                if (emoji != null)
                {
                    var lowered = e.Message.Content.ToLower();
                    if (lowered.Contains("sharing") ||
                    lowered.Contains("share") ||
                    lowered.Contains("partage"))
                    {
                        var task = e.Message.CreateReactionAsync(emoji);
                        if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                            Program.Logger.Error("Unable to share");
                    }
                }
            };
            Program.Discord.GuildAvailable += async (e) =>
            {
                if (emoji == null)
                {
                    await Task.CompletedTask;
                    emoji = e.Guild.Emojis.FirstOrDefault(ê̷̜͈͎͚͂̀̇̔͘m̸͎̣͍̞̗̰̮̅͐̅̈́̀̊̍͝o̵̟͚̟͚̝͇͚̹̱̾̊̐́͂͋̎͝j̵̤̈́́͂̔͒̊́ǐ̶̡̟͈̳͉ => ê̷̜͈͎͚͂̀̇̔͘m̸͎̣͍̞̗̰̮̅͐̅̈́̀̊̍͝o̵̟͚̟͚̝͇͚̹̱̾̊̐́͂͋̎͝j̵̤̈́́͂̔͒̊́ǐ̶̡̟͈̳͉.Name.Contains("sharing"));
                    Program.Logger.Info("Found Sharing");
                }
            };
        }

        #endregion Public Methods
    }
}