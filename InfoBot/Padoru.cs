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

            => await ev.Message.RespondWithFileAsync(
                await Program.Client.GetStreamAsync($"{Program.WildgoatApi}/padoru.php").ConfigureAwait(false),
                "padoru.jpeg").ConfigureAwait(false);

        #endregion Public Methods
    }
}