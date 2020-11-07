using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Infobot
{
    internal static class SettingsManager
    {
        #region Public Properties

        public static Settings MostRecent
        {
            get
            {
                Directory.CreateDirectory("settings");
                var files = new SortedSet<string>(Directory.GetFiles("settings"), ReverseComparer<string>.Reverse(StringComparer.CurrentCulture));
                if (files.Count == 0)
                {
                    Program.Logger.Warning($"No settings found");
                    return Settings.Default;
                }
                else
                {
                    foreach (var file in files)
                        try
                        {
                            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(file));
                            Program.Logger.Info($"Settings '{file}' loaded");
                            return settings;
                        }
                        catch (JsonReaderException)
                        {
                            Program.Logger.Warning($"Corrupted file '{file}'");
                        }
                    Program.Logger.Warning($"No settings found");
                    return Settings.Default;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public static void Save(Settings settings)
        {
            Directory.CreateDirectory("settings");
            var path = Path.Combine("settings", $"{DateTime.Now:yyyyMMddHHmmss}.json");
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
                Program.Logger.Info($"Settings saved to '{path}'");
            }
            catch (IOException e)
            {
                Program.Logger.Warning($"Unable to save settings to '{path}' : {e.Message}");
            }
            catch (UnauthorizedAccessException e)
            {
                Program.Logger.Warning($"Unable to save settings to '{path}' : {e.Message}");
            }
        }

        #endregion Public Methods
    }
}