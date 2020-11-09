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

        public static string Key => "settings";
        public bool Admin => true;

        public IEnumerable<(string, string)> Detail => new (string, string)[] {
            ($"`{Key} get <setting> [<settings...>]`", "Displays the value of the given settings"),
            ($"`{Key} get all`", "Displays the value of all settings"),
            ($"`{Key} set <setting> <value>`", "Changes a specific setting for the given value")
        };

        string ICommand.Key => Key;
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
                                                if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                                    Program.Logger.Error("Unable to respond");
                                            }
                                            else
                                            {
                                                var task = ev.Message.RespondAsync($"Unable to change this setting");
                                                if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                                    Program.Logger.Error("Unable to respond");
                                            }
                                        }
                                        else
                                        {
                                            var task = ev.Message.RespondAsync($"Unknown setting");
                                            if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                                Program.Logger.Error("Unable to respond");
                                        }
                                    }
                                    else
                                    {
                                        var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key} {Key}` for more informations");
                                        if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                            Program.Logger.Error("Unable to respond");
                                    }
                                }
                                else
                                {
                                    var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key} {Key}` for more informations");
                                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Error("Unable to respond");
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
                                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Error("Unable to send settings");
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
                                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                        Program.Logger.Error("Unable to send settings");
                                }
                            }
                            break;

                        default:
                            {
                                var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key} {Key}` for more informations");
                                if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                                    Program.Logger.Error("Unable to respond");
                            }
                            break;
                    }
                }
                else
                {
                    var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key} {Key}` for more informations");
                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                        Program.Logger.Error("Unable to respond");
                }
            }
            else
            {
                var task = ev.Message.RespondAsync($"Invalid command, type `{Settings.CurrentSettings.commandIdentifier}{Help.Key} {Key}` for more informations");
                if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) != task || !task.IsCompletedSuccessfully)
                    Program.Logger.Error("Unable to respond");
            }
        }

        #endregion Public Methods
    }
}