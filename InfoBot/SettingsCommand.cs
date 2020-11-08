using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal class SettingsCommand : ICommand
    {
        #region Public Properties

        public bool Admin => true;

        public IEnumerable<(string, string)> Detail => new (string, string)[] {
            ("`settings get <setting> [<settings...>]`", "Displays the value of the given settings"),
            ("`settings get all`", "Displays the value of all settings"),
            ("`settings set <setting> <value>`", "Changes a specific setting for the given value")
        };

        public string Key => "settings";

        public string Summary => "See or edit settings";

        #endregion Public Properties

        #region Public Methods

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args)
        {
            if (args.Any())
            {
                var mode = args.First();
                args = args.Skip(1);
                if (args.Any())
                {
                    switch (mode)
                    {
                        case "set":
                            {
                                var iterator = args.GetEnumerator();
                                if (iterator.MoveNext())
                                {
                                    var setting = iterator.Current;
                                    if (iterator.MoveNext())
                                    {
                                        var value = iterator.Current;
                                        if (Settings.CurrentSettings.AvailableSettings.ContainsKey(setting.ToLower()))
                                        {
                                            if (Settings.CurrentSettings.AvailableSettings[setting.ToLower()].Item2(value))
                                            {
                                                SettingsManager.Save(Settings.CurrentSettings);
                                                var task = ev.Message.RespondAsync($"Setting changed");
                                                if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                                    Program.Logger.Warning("Unable to respond");
                                            }
                                            else
                                            {
                                                var task = ev.Message.RespondAsync($"Unable to change this setting");
                                                if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                                    Program.Logger.Warning("Unable to respond");
                                            }
                                        }
                                        else
                                        {
                                            var task = ev.Message.RespondAsync($"Unknown setting");
                                            if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                                Program.Logger.Warning("Unable to respond");
                                        }
                                    }
                                    else
                                    {
                                        var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}help {Key}` for more informations");
                                        if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                            Program.Logger.Warning("Unable to respond");
                                    }
                                }
                                else
                                {
                                    var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}help {Key}` for more informations");
                                    if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Warning("Unable to respond");
                                }
                            }
                            break;

                        case "get":
                            {
                                if (args.First().ToLower() == "all")
                                {
                                    var builder = new StringBuilder();
                                    foreach (var entry in Settings.CurrentSettings.AvailableSettings)
                                        builder.Append($"- `{entry.Key}` : {entry.Value.Item1()}\n");
                                    var embed = new DiscordEmbedBuilder()
                                        .WithTitle("All settings :")
                                        .WithDescription(builder.ToString());
                                    var task = ev.Message.RespondAsync(embed: embed);
                                    if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Warning("Unable to send settings");
                                }
                                else
                                {
                                    var builder = new StringBuilder();
                                    foreach (var setting in args)
                                    {
                                        if (Settings.CurrentSettings.AvailableSettings.ContainsKey(setting.ToLower()))
                                            builder.Append($"- `{setting.ToLower()}` : {Settings.CurrentSettings.AvailableSettings[setting.ToLower()].Item1()}\n");
                                        else
                                            builder.Append($"- `{setting.ToLower()}` : *Unknown setting*\n");
                                    }
                                    var embed = new DiscordEmbedBuilder()
                                        .WithTitle("Settings :")
                                        .WithDescription(builder.ToString());
                                    var task = ev.Message.RespondAsync(embed: embed);
                                    if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Warning("Unable to send settings");
                                }
                            }
                            break;

                        default:
                            {
                                var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}help {Key}` for more informations");
                                if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                                    Program.Logger.Warning("Unable to respond");
                            }
                            break;
                    }
                }
                else
                {
                    var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}help {Key}` for more informations");
                    if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                        Program.Logger.Warning("Unable to respond");
                }
            }
            else
            {
                var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}help {Key}` for more informations");
                if ((await Task.WhenAny(task, Task.Delay(Program.Timeout)).ConfigureAwait(false)) != task || !task.IsCompletedSuccessfully)
                    Program.Logger.Warning("Unable to respond");
            }
        }

        #endregion Public Methods
    }
}