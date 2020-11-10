using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Infobot
{
    internal class Room : ISetup, ICommand
    {
        #region Private Fields

        private Dictionary<DiscordChannel, (DiscordChannel, Timer)> rooms;

        #endregion Private Fields

        #region Public Properties

        public bool Admin => false;

        public IEnumerable<(string, string)> Detail => null;

        public string Key => "room";

        public string Summary => "";

        #endregion Public Properties

        #region Public Methods

        public void Connected()
        {
        }

        public async Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args)
        {
            if (!await ev.Message.RespondAsync("Creating rooms...").TimeoutTask())
                Program.Logger.Error("Unable to send message");
            DiscordChannel category = null;
            {
                if (Settings.CurrentSettings.privateRooms.Value == 0)
                    Program.Logger.Warning($"No category set for private rooms");
                else
                {
                    var task = Program.Discord.GetChannelAsync(Settings.CurrentSettings.privateRooms.Value);
                    if (await task.TimeoutTask())
                        category = task.Result;
                    else
                        Program.Logger.Error($"Unable to find channel '{Settings.CurrentSettings.privateRooms.Value}'");
                }
            }
            var voiceChannelTask = ev.Guild.CreateChannelAsync("private voice", DSharpPlus.ChannelType.Voice, category);
            if (await voiceChannelTask.TimeoutTask())
            {
                var voiceChannel = voiceChannelTask.Result;
                var voiceEveryoneTask = voiceChannel.AddOverwriteAsync(ev.Guild.EveryoneRole, DSharpPlus.Permissions.None, DSharpPlus.Permissions.AccessChannels);
                if (await voiceEveryoneTask.TimeoutTask())
                {
                    foreach (var user in ev.MentionedUsers)
                    {
                        var memberTask = ev.Guild.GetMemberAsync(user.Id);
                        if (await memberTask.TimeoutTask())
                        {
                            var memberPermissionTask = voiceChannel.AddOverwriteAsync(memberTask.Result, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                            if (!await memberPermissionTask.TimeoutTask())
                            {
                                Program.Logger.Error($"Unable to change user permissions for voice channel");
                                if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                    Program.Logger.Error("Unable to delete voice channel");
                            }
                        }
                        else
                        {
                            Program.Logger.Error($"Unable to get member data");
                            if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete voice channel");
                        }
                    }
                    {
                        var authorTask = ev.Guild.GetMemberAsync(ev.Author.Id);
                        if (await authorTask.TimeoutTask())
                        {
                            var authorPermissionTask = voiceChannel.AddOverwriteAsync(authorTask.Result, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                            if (!await authorPermissionTask.TimeoutTask())
                            {
                                Program.Logger.Error($"Unable to change author permissions for voice channel");
                                if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                    Program.Logger.Error("Unable to delete voice channel");
                            }
                        }
                        else
                        {
                            Program.Logger.Error($"Unable to get member data");
                            if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete voice channel");
                        }
                    }
                    foreach (var role in ev.MentionedRoles)
                    {
                        var memberPermissionTask = voiceChannel.AddOverwriteAsync(role, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                        if (!await memberPermissionTask.TimeoutTask())
                        {
                            Program.Logger.Error($"Unable to change role permissions for voice channel");
                            if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete voice channel");
                        }
                    }
                    var textChannelTask = ev.Guild.CreateChannelAsync("private text", DSharpPlus.ChannelType.Text, category);
                    if (await textChannelTask.TimeoutTask())
                    {
                        var textChannel = textChannelTask.Result;
                        var textEveryoneTask = textChannel.AddOverwriteAsync(ev.Guild.EveryoneRole, DSharpPlus.Permissions.None, DSharpPlus.Permissions.AccessChannels);
                        if (await textEveryoneTask.TimeoutTask())
                        {
                            foreach (var user in ev.MentionedUsers)
                            {
                                var memberTask = ev.Guild.GetMemberAsync(user.Id);
                                if (await memberTask.TimeoutTask())
                                {
                                    var memberPermissionTask = textChannel.AddOverwriteAsync(memberTask.Result, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                                    if (!await memberPermissionTask.TimeoutTask())
                                    {
                                        Program.Logger.Error($"Unable to change user permissions for voice channel");
                                        if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                            Program.Logger.Error("Unable to delete voice channel");
                                        if (!await textChannel.DeleteAsync().TimeoutTask())
                                            Program.Logger.Error("Unable to delete text channel");
                                    }
                                }
                                else
                                {
                                    Program.Logger.Error($"Unable to get member data");
                                    if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete voice channel");
                                    if (!await textChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete text channel");
                                }
                            }
                            {
                                var authorTask = ev.Guild.GetMemberAsync(ev.Author.Id);
                                if (await authorTask.TimeoutTask())
                                {
                                    var authorPermissionTask = textChannel.AddOverwriteAsync(authorTask.Result, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                                    if (!await authorPermissionTask.TimeoutTask())
                                    {
                                        Program.Logger.Error($"Unable to change author permissions for voice channel");
                                        if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                            Program.Logger.Error("Unable to delete voice channel");
                                        if (!await textChannel.DeleteAsync().TimeoutTask())
                                            Program.Logger.Error("Unable to delete text channel");
                                    }
                                }
                                else
                                {
                                    Program.Logger.Error($"Unable to get member data");
                                    if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete voice channel");
                                    if (!await textChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete text channel");
                                }
                            }
                            foreach (var role in ev.MentionedRoles)
                            {
                                var memberPermissionTask = textChannel.AddOverwriteAsync(role, DSharpPlus.Permissions.AccessChannels, DSharpPlus.Permissions.None);
                                if (!await memberPermissionTask.TimeoutTask())
                                {
                                    Program.Logger.Error($"Unable to change role permissions for voice channel");
                                    if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete voice channel");
                                    if (!await textChannel.DeleteAsync().TimeoutTask())
                                        Program.Logger.Error("Unable to delete text channel");
                                }
                            }
                        }
                        else
                        {
                            Program.Logger.Error($"Unable to change everyone permissions for voice channel");
                            if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete voice channel");
                            if (!await textChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete text channel");
                        }
                        var timer = new Timer(Settings.CurrentSettings.customRoomDelay.Value.TotalMilliseconds)
                        {
                            Enabled = true,
                            AutoReset = false
                        };
                        timer.Elapsed += async (sender, e) =>
                        {
                            Program.Logger.Info("Private room delay elapsed, deleting channels.");
                            if (!await voiceChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete voice channel");
                            if (!await textChannel.DeleteAsync().TimeoutTask())
                                Program.Logger.Error("Unable to delete text channel");
                            rooms.Remove(voiceChannel);
                            timer.Close();
                        };
                        rooms.Add(voiceChannel, (textChannel, timer));
                    }
                    else
                    {
                        Program.Logger.Error($"Unable to create text channel");
                        if (!await voiceChannel.DeleteAsync().TimeoutTask())
                            Program.Logger.Error("Unable to delete voice channel");
                    }
                }
                else
                {
                    Program.Logger.Error($"Unable to change everyone permissions for voice channel");
                    if (!await voiceChannel.DeleteAsync().TimeoutTask())
                        Program.Logger.Error("Unable to delete voice channel");
                }
            }
            else
                Program.Logger.Error($"Unable to create voice channel");
        }

        public void Setup()
        {
            rooms = new Dictionary<DiscordChannel, (DiscordChannel, Timer)>();
            Program.Discord.VoiceStateUpdated += (e) =>
            {
                foreach (var chan in rooms)
                {
                    var timer = chan.Value.Item2;
                    if (e.Guild.VoiceStates.Any(state => state.Channel == chan.Key) && timer.Enabled)
                    {
                        Program.Logger.Info("Private room timer stopped");
                        timer.Stop();
                    }
                    else if (!timer.Enabled)
                    {
                        Program.Logger.Info("Private room timer resumed");
                        timer.Start();
                    }
                }
                return Task.CompletedTask;
            };
        }

        #endregion Public Methods
    }
}