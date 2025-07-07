using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoadoutKeeper
{
    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }

    public partial class LoadoutKeeper : IPluginConfig<PluginConfig>
    {
        private static readonly JsonSerializerOptions CachedJsonOptions = new() { WriteIndented = true };
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }

        public void SaveConfigs()
        {
            string playerConfigPath = Path.Combine(
                $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/"
            );
            if (!Directory.Exists(playerConfigPath))
            {
                _ = Directory.CreateDirectory(playerConfigPath);
            }
            // save player loadouts
            foreach (KeyValuePair<ulong, Dictionary<string, int>> kvp in _loadouts)
            {
                string jsonString = JsonSerializer.Serialize(kvp.Value, CachedJsonOptions);
                File.WriteAllText(Path.Combine(playerConfigPath, $"{kvp.Key}.json"), jsonString);
            }
        }

        public void LoadConfig(ulong SteamID)
        {
            string playerConfigPath = Path.Combine(
                $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/",
                $"{SteamID}.json"
            );
            // skip if player config file does not exist
            if (!File.Exists(playerConfigPath))
            {
                return;
            }
            // check if player loadout file exists and load it
            try
            {
                string jsonString = File.ReadAllText(playerConfigPath);
                Dictionary<string, int>? playerLoadout = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString, CachedJsonOptions);
                if (playerLoadout != null)
                {
                    _loadouts[SteamID] = playerLoadout;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", ex.Message));
            }
        }
    }
}
