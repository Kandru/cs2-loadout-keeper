using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoadoutKeeper
{
    public class PluginConfig : BasePluginConfig
    {
        // whether the plugin is enabled or not
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // check which loadout to give on spawn per default
        [JsonPropertyName("default_setting")] public string DefaultLoadoutType { get; set; } = "ALL";
        // whether or not to reset the weapon purchases inside of the buy menu after respawn (allows to buy all weapons again)
        [JsonPropertyName("reset_buy_menu_loadout")] public bool ResetBuyMenuLoadout { get; set; } = true;
        // announcements
        [JsonPropertyName("announce_loadout_given_chat")] public bool AnnounceLoadoutGivenChat { get; set; } = true;
        [JsonPropertyName("announce_loadout_given_center")] public bool AnnounceLoadoutGivenCenter { get; set; } = false;
        [JsonPropertyName("announce_loadout_given_center_alert")] public bool AnnounceLoadoutGivenCenterAlert { get; set; } = true;

    }

    public class LoadoutConfig
    {
        // which loadout to give on spawn (defaults to PluginConfig.DefaultLoadoutType anyway)
        [JsonPropertyName("loadout_type")] public string Type { get; set; } = "ALL";
        // player loadout data
        [JsonPropertyName("loadout_data")] public Dictionary<string, int> Weapons { get; set; } = [];
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
            foreach (KeyValuePair<ulong, LoadoutConfig> kvp in _loadouts)
            {
                string jsonString = JsonSerializer.Serialize(kvp.Value, CachedJsonOptions);
                File.WriteAllText(Path.Combine(playerConfigPath, $"v1_{kvp.Key}.json"), jsonString);
            }
        }

        public void LoadConfig(ulong SteamID)
        {
            string playerConfigPath = Path.Combine(
                $"{Path.GetDirectoryName(Config.GetConfigPath())}/players/" ?? "./players/",
                $"v1_{SteamID}.json"
            );
            // skip if player config file does not exist
            if (!File.Exists(playerConfigPath))
            {
                _loadouts.Add(SteamID, new LoadoutConfig() { Type = Config.DefaultLoadoutType });
                return;
            }
            // check if player loadout file exists and load it
            try
            {
                string jsonString = File.ReadAllText(playerConfigPath);
                LoadoutConfig? playerLoadout = JsonSerializer.Deserialize<LoadoutConfig>(jsonString, CachedJsonOptions);
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
