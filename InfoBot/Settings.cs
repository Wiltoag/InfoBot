using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    public class Settings
    {
        #region Public Fields

        public static Settings CurrentSettings;
        public string commandIdentifier;
        public int[] oldHash;
        public string status;
        public ulong[] timetableChannels;
        public TimeSpan? timetableDelay;
        public string[] timetableUrls;

        #endregion Public Fields

        #region Public Properties

        public static Settings Default
        {
            get
            {
                var result = new Settings
                {
                    oldHash = new int[] { 0, 0, 0, 0, 0, 0 },
                    timetableUrls = new string[] { "", "", "", "", "", "" },
                    timetableChannels = new ulong[] { 0, 0, 0, 0, 0, 0 },
                    timetableDelay = TimeSpan.FromHours(2),
                    commandIdentifier = "$"
                };
                result.status = $"{result.commandIdentifier}{Help.Key}";
                return result;
            }
        }

        [JsonIgnore]
        public Dictionary<string, (Func<string>, Func<string, bool>)> AvailableSettings
        {
            get
            {
                bool changeTimetableChannel(int index, string value)
                {
                    if (ulong.TryParse(value, out ulong parsed))
                    {
                        timetableChannels[index] = parsed;
                        return true;
                    }
                    else
                        return false;
                }
                bool changeTimetableUrl(int index, string value)
                {
                    timetableUrls[index] = value;
                    return true;
                }
                var result = new Dictionary<string, (Func<string>, Func<string, bool>)>
                {
                    { "edt-url-11", (() => timetableUrls[0], value => changeTimetableUrl(0, value)) },
                    { "edt-url-12", (() => timetableUrls[1], value => changeTimetableUrl(1, value)) },
                    { "edt-url-21", (() => timetableUrls[2], value => changeTimetableUrl(2, value)) },
                    { "edt-url-22", (() => timetableUrls[3], value => changeTimetableUrl(3, value)) },
                    { "edt-url-31", (() => timetableUrls[4], value => changeTimetableUrl(4, value)) },
                    { "edt-url-32", (() => timetableUrls[5], value => changeTimetableUrl(5, value)) },

                    { "edt-chan-11", (() => timetableChannels[0].ToString(), value => changeTimetableChannel(0, value)) },
                    { "edt-chan-12", (() => timetableChannels[1].ToString(), value => changeTimetableChannel(1, value)) },
                    { "edt-chan-21", (() => timetableChannels[2].ToString(), value => changeTimetableChannel(2, value)) },
                    { "edt-chan-22", (() => timetableChannels[3].ToString(), value => changeTimetableChannel(3, value)) },
                    { "edt-chan-31", (() => timetableChannels[4].ToString(), value => changeTimetableChannel(4, value)) },
                    { "edt-chan-32", (() => timetableChannels[5].ToString(), value => changeTimetableChannel(5, value)) },

                    {"edt-check-timer", (() => timetableDelay.ToString(), value => {
                        if (TimeSpan.TryParse(value, out TimeSpan parsed))
                        {
                            timetableDelay = parsed;
                            UpdateTimetable.UpdateTimerDelay();
                            return true;
                        }
                        else
                            return false;
                    }) },

                    { "command-prompt", (() => commandIdentifier, value => {
                        commandIdentifier = value;
                        return true;
                    }) },

                    { "status", (() => status, value => {
                        var task = Program.Discord.UpdateStatusAsync(new DSharpPlus.Entities.DiscordGame(value));
                        if (Task.WhenAny(task, Task.Delay(Program.Timeout)).Result == task && task.IsCompletedSuccessfully)
                        {
                            status = value;
                            return true;
                        }
                        else
                            return false;
                    }) }
                };

                return result;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void CheckIntegrity()
        {
            var defaultSettings = Default;
            if (oldHash == null)
            {
                Program.Logger.Warning("Missing Settings.oldHash");
                oldHash = defaultSettings.oldHash;
            }
            if (timetableUrls == null)
            {
                Program.Logger.Warning("Missing Settings.timeTableUrls");
                timetableUrls = defaultSettings.timetableUrls;
            }
            if (timetableDelay == null)
            {
                Program.Logger.Warning("Missing Settings.timetableDelay");
                timetableDelay = defaultSettings.timetableDelay;
            }
            if (commandIdentifier == null)
            {
                Program.Logger.Warning("Missing Settings.commandIdentifier");
                commandIdentifier = defaultSettings.commandIdentifier;
            }
            if (status == null)
            {
                Program.Logger.Warning("Missing Settings.status");
                status = defaultSettings.status;
            }
            if (timetableChannels == null)
            {
                Program.Logger.Warning("Missing Settings.timetableChannels");
                timetableChannels = defaultSettings.timetableChannels;
            }
        }

        #endregion Public Methods
    }
}