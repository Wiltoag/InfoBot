using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal class Padoru : ICommand
    {
        #region Public Properties

        public bool Admin => false;

        public string Key => "padoru";

        #endregion Public Properties

        #region Public Methods

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args)
            => await ev.Message.RespondAsync(
                embed: new DiscordEmbedBuilder().WithImageUrl($"{Program.WildgoatApi}/padoru.php")).ConfigureAwait(false);

        #endregion Public Methods
    }
}