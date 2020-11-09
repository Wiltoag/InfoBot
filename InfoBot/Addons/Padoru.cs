using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal class Padoru : ICommand
    {
        #region Public Properties

        public static string Key => "padoru";
        public bool Admin => false;

        public IEnumerable<(string, string)> Detail => null;
        string ICommand.Key => Key;
        public string Summary => "Displays the remaining number of days until Christmas";

        #endregion Public Properties

        #region Public Methods

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args)
        {
            var getTask = Program.Client.GetAsync($"{Program.WildgoatApi}/padoru.php");
            if (await Task.WhenAny(getTask, Task.Delay(Program.Timeout)) == getTask && getTask.IsCompletedSuccessfully)
            {
                var response = getTask.Result;
                var sendTask = ev.Message.RespondAsync(
                  embed: new DiscordEmbedBuilder().WithImageUrl($"{Program.WildgoatApi}/{response.Headers.Location}"));
                if (await Task.WhenAny(sendTask, Task.Delay(Program.Timeout)) != sendTask || !sendTask.IsCompletedSuccessfully)
                    Program.Logger.Error("Unable to send Padoru");
            }
            else
                Program.Logger.Error("Unable to request Padoru");
        }

        #endregion Public Methods
    }
}