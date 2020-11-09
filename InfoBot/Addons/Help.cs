using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal class Help : ICommand
    {
        #region Public Properties

        public static string Key => "help";
        public bool Admin => false;

        public IEnumerable<(string, string)> Detail => new (string, string)[] {
            ($"`{Help.Key}`", "Displays the help panel"),
            ($"`{Help.Key} <command> [<commands ...>]`", "Displays help for the specified commands")
        };

        string ICommand.Key => Key;
        public string Summary => "Displays help to use commands";

        #endregion Public Properties

        #region Public Methods

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args)
        {
            if (args.Any())
            {
                foreach (var key in args)
                {
                    var command = Program.registeredCommands.FirstOrDefault(c => c.Key.ToLower() == key.ToLower());
                    if (command != null)
                    {
                        var embed = new DiscordEmbedBuilder()
                            .WithTitle($"`{command.Key}`")
                            .WithDescription(command.Summary);
                        command.Detail?.ForEach(set => embed.AddField($"- `{set.Item1}`", $"{set.Item2}"));
                        var task = ev.Message.RespondAsync(embed: embed);
                        if (!await task.TimeoutTask())
                            Program.Logger.Error($"Unable to send help for '{command.Key}'");
                    }
                    else
                    {
                        var task = ev.Message.RespondAsync($"Unknown command `{key}`, type `{Settings.CurrentSettings.commandIdentifier}help` for more informations");
                        if (!await task.TimeoutTask())
                            Program.Logger.Error($"Unable to send help for '{command.Key}'");
                    }
                }
            }
            else
            {
                var builder = new StringBuilder();
                var embed = new DiscordEmbedBuilder();
                var admins = from command in Program.registeredCommands
                             where command.Admin
                             select command;
                var users = from command in Program.registeredCommands
                            where !command.Admin
                            select command;
                if (admins.Any())
                {
                    admins.ForEach((command, index) =>
                    {
                        if (index > 0)
                            builder.Append(", ");
                        builder.Append($"`{command.Key}`");
                    });
                    embed.AddField("Admin commands :", builder.ToString());
                    builder.Clear();
                }
                else
                    embed.AddField("Admin commands :", "No commands");
                if (users.Any())
                {
                    users.ForEach((command, index) =>
                    {
                        if (index > 0)
                            builder.Append(", ");
                        builder.Append($"`{command.Key}`");
                    });
                    embed.AddField("User commands :", builder.ToString());
                    builder.Clear();
                }
                else
                    embed.AddField("User commands :", "No commands");
                embed.AddField("Auto delete message",
                    $"Put `--remove` as last argument of the command to delete it automatically.\nEx : `{Settings.CurrentSettings.commandIdentifier}{Key} {Padoru.Key} --remove`");
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Use {Settings.CurrentSettings.commandIdentifier}{Help.Key} <command> for more informations about a command." };
                var task = ev.Message.RespondAsync(embed: embed);
                if (!await task.TimeoutTask())
                    Program.Logger.Error($"Unable to send help");
            }
        }

        #endregion Public Methods
    }
}