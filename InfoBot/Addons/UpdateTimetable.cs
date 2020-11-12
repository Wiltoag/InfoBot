using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Infobot
{
    internal class UpdateTimetable : ICommand, ISetup
    {
        #region Private Fields

        private static Timer timer;

        #endregion Private Fields

        #region Public Properties

        public static string Key => "edt";
        public bool Admin => true;

        public IEnumerable<(string, string)> Detail => new (string, string)[]{
            ($"`{Key}`","Proceeds to trigger a timetable update for all groups"),
            ($"`{Key} <groups>`", "Proceeds to trigger a timetable update for the given groups (`11`, `12`, `21`, `22`, `31`, `32`)"),
            ($"`{Key} force`","Forces the timetable update (even if nothing changed) for all groups"),
            ($"`{Key} force <groups>`", "Forces the timetable update (even if nothing changed) for the given groups (`11`, `12`, `21`, `22`, `31`, `32`)")
        };

        string ICommand.Key => Key;
        public string Summary => "Updates of the timetables";

        #endregion Public Properties

        #region Public Methods

        public static async Task Update(bool force = false, params int[] groups)
        {
            if (groups.Length == 0)
            {
                var list = new List<int>();
                Settings.CurrentSettings.timetableUrls.ForEach((u, index) => list.Add(index));
                groups = list.ToArray();
            }
            string regex = Uri.EscapeDataString("/^(.*) ?- ?.* ?- ?.* ?- ?.* ?- ?(.*)$/");
            await Task.WhenAll(Settings.CurrentSettings.timetableUrls
                .Where((u, index) => groups.Contains(index))
                .Select(async (url, index) =>
            {
                if (url.Length > 0)
                {
                    var oldHash = Settings.CurrentSettings.oldHash[index];
                    var jsonRequest = $"{Program.WildgoatApi}/ical-json.php?url={Uri.EscapeDataString(url)}&weeks=2";
                    Program.Logger.Info($"Updating {index / 2 + 1}.{1 + index % 2} timetable");
                    Program.Logger.Info($"Requesting '{jsonRequest}'");
                    {
                        var task = Program.Client.GetStringAsync(jsonRequest);
                        if (await task.TimeoutTask())
                        {
                            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(task.Result);
                            int newHash = json.code;
                            if (newHash != oldHash || force)
                            {
                                var channelTask = Program.Discord.GetChannelAsync(Settings.CurrentSettings.timetableChannels[index]);
                                if (await channelTask.TimeoutTask())
                                {
                                    var channel = channelTask.Result;
                                    var oldMessages = (await channel.GetMessagesAsync(20, null, null, channel.LastMessageId)).Where(m => m.Author.IsCurrent);
                                    if (oldMessages.Any())
                                        await channel.DeleteMessagesAsync(oldMessages);
                                    var error = false;
                                    {
                                        var getTask = Program.Client.GetAsync($"{Program.WildgoatApi}/ical-png.php?url={Uri.EscapeDataString(url)}&regex={regex}");
                                        if (await getTask.TimeoutTask())
                                        {
                                            var response = getTask.Result;
                                            var sendTask = channel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithTitle("Emploi du temps semaine en cours")
                                            .WithImageUrl($"{Program.WildgoatApi}/{response.Headers.Location}"));
                                            if (await sendTask.TimeoutTask())
                                            {
                                                error = true;
                                                Program.Logger.Error($"Unable to send the week 1 for {index / 2 + 1}.{1 + index % 2}");
                                            }
                                        }
                                        else
                                        {
                                            error = true;
                                            Program.Logger.Error($"Unable to get the week 1 for {index / 2 + 1}.{1 + index % 2}");
                                        }
                                    }
                                    {
                                        var getTask = Program.Client.GetAsync($"{Program.WildgoatApi}/ical-png.php?url={Uri.EscapeDataString(url)}&regex={regex}&offset=1");
                                        if (await getTask.TimeoutTask())
                                        {
                                            var response = getTask.Result;
                                            var sendTask = channel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithTitle("Emploi du temps semaine prochaine")
                                            .WithImageUrl($"{Program.WildgoatApi}/{response.Headers.Location}"));
                                            if (await sendTask.TimeoutTask())
                                            {
                                                error = true;
                                                Program.Logger.Error($"Unable to send the week 2 for {index / 2 + 1}.{1 + index % 2}");
                                            }
                                        }
                                        else
                                        {
                                            error = true;
                                            Program.Logger.Error($"Unable to get the week 2 for {index / 2 + 1}.{1 + index % 2}");
                                        }
                                    }
                                    if (!error)
                                        Settings.CurrentSettings.oldHash[index] = newHash;
                                }
                                else
                                    Program.Logger.Error($"Unable to find {index / 2 + 1}.{1 + index % 2} timetable channel.");
                            }
                            else
                                Program.Logger.Info($"No differences detected for {index / 2 + 1}.{1 + index % 2}");
                        }
                        else
                            Program.Logger.Error("Unable to get the JSON");
                    }
                }
                else
                    Program.Logger.Warning($"No url provided for {index / 2 + 1}.{1 + index % 2}");
            }));
            Program.Logger.Info("Timetables updated");
            SettingsManager.Save(Settings.CurrentSettings);
        }

        public static void UpdateTimerDelay()
            => timer.Interval = Settings.CurrentSettings.timetableDelay.Value.TotalMilliseconds;

        public void Connected()
        {
            timer = new Timer(Settings.CurrentSettings.timetableDelay.Value.TotalMilliseconds);
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += async (sender, e) => await Update();
            _ = Update();
        }

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> a)
        {
            var task = ev.Message.RespondAsync("Updating timetables");
            var args = new List<string>(a);
            if (args.Any())
            {
                if (args.First() == "force")
                {
                    args = new List<string>(a.Skip(1));
                    if (args.Any())
                    {
                        var groups = args.Select(arg => arg switch
                        {
                            "11" => 0,
                            "12" => 1,
                            "21" => 2,
                            "22" => 3,
                            "31" => 4,
                            "32" => 5,
                            _ => -1
                        }).ToArray();
                        groups.ForEach(async (a, index) =>
                        {
                            if (a == -1)
                            {
                                var task = ev.Message.RespondAsync($"Unknown group `{args[index]}`");
                                if (await task.TimeoutTask())
                                    Program.Logger.Error("Unable to respond");
                            }
                        });
                        await Update(true, groups);
                    }
                    else
                        await Update(true);
                }
                else
                {
                    var groups = args.Select(arg => arg switch
                    {
                        "11" => 0,
                        "12" => 1,
                        "21" => 2,
                        "22" => 3,
                        "31" => 4,
                        "32" => 5,
                        _ => -1
                    }).ToArray();
                    groups.ForEach(async (a, index) =>
                    {
                        if (a == -1)
                        {
                            var task = ev.Message.RespondAsync($"Unknown group `{args[index]}`");
                            if (await task.TimeoutTask())
                                Program.Logger.Error("Unable to respond");
                        }
                    });
                    await Update(false, groups);
                }
            }
            else
                await Update();
            if (await task.TimeoutTask())
                Program.Logger.Error("Unable to respond");
        }

        public void Setup()
        {
        }

        #endregion Public Methods
    }
}