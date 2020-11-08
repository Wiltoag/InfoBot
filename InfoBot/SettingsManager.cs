using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Infobot
{
    internal class SettingsManager : ISetup
    {
        #region Private Fields

        private static Timer timer;

        #endregion Private Fields

        #region Public Properties

        public static Settings MostRecent
        {
            get
            {
                Directory.CreateDirectory("settings");
                var files = Directory.GetFiles("settings").OrderByDescending(f => f, StringComparer.CurrentCulture);
                if (!files.Any())
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
                            settings.CheckIntegrity();
                            return settings;
                        }
                        catch (JsonReaderException)
                        {
                            Program.Logger.Error($"Corrupted file '{file}'");
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

        public static async Task Update()
        {
            Directory.CreateDirectory("settings");
            if (Directory.GetFiles("settings").Length > 25)
            {
                Program.Logger.Info("Deleting last files");
                await Task.WhenAll(Directory.GetFiles("settings")
                    .OrderByDescending(file => file, StringComparer.CurrentCulture)
                    .Skip(25)
                    .Select(async file => await Task.Run(() => File.Delete(file)).ConfigureAwait(false))
                    ).ContinueWith((t) => Program.Logger.Info("Files deleted"));
            }
        }

        public void Connected()
        {
        }

        public void Setup()
        {
            timer = new Timer(TimeSpan.FromMinutes(30).TotalMilliseconds)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += async (sender, e) => await Update().ConfigureAwait(false);
            _ = Update();
        }

        #endregion Public Methods
    }
}