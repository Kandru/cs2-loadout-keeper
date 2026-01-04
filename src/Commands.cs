using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using LoadoutKeeper.Enums;

namespace LoadoutKeeper
{
    public partial class LoadoutKeeper
    {
        [ConsoleCommand("loadoutkeeper", "LoadoutKeeper admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandPluginAdmin(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower(System.Globalization.CultureInfo.CurrentCulture))
            {
                case "reload":
                    Config.Reload();
                    command.ReplyToCommand(Localizer["admin.reload"]);
                    break;
                case "enable":
                    Config.Enabled = true;
                    Config.Update();
                    foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(static p => !p.IsBot))
                    {
                        // update player loadout
                        LoadConfig(entry.SteamID);
                    }
                    command.ReplyToCommand(Localizer["admin.enabled"]);
                    break;
                case "disable":
                    SaveConfigs();
                    _loadouts.Clear();
                    _spawnCooldowns.Clear();
                    Config.Enabled = false;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.disabled"]);
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }

        [ConsoleCommand("loadout", "LoadoutKeeper user settings")]
        [ConsoleCommand("la", "LoadoutKeeper user settings")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "<all|weapons|primary|secondary|grenades|items>")]
        public void CommandLoadout(CCSPlayerController player, CommandInfo command)
        {
            string loadoutType = command.GetArg(1);
            LoadoutTypes? loadout = null;

            // check if loadoutType is omitted
            if (string.IsNullOrWhiteSpace(loadoutType))
            {
                if (Config.EnableGrenades)
                {
                    command.ReplyToCommand(Localizer["command.loadout.usage_with_grenades"].Value
                        .Replace("{current}", _loadouts.TryGetValue(player.SteamID, out LoadoutConfig? value1) ? value1.Type.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture) : Config.DefaultLoadoutType.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)));
                    return;
                }
                command.ReplyToCommand(Localizer["command.loadout.usage"].Value
                    .Replace("{current}", _loadouts.TryGetValue(player.SteamID, out LoadoutConfig? value2) ? value2.Type.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture) : Config.DefaultLoadoutType.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)));
                return;
            }

            // Try exact match first
            if (Enum.TryParse(loadoutType.ToUpper(System.Globalization.CultureInfo.CurrentCulture), out LoadoutTypes exactMatch))
            {
                loadout = exactMatch;
            }
            else
            {
                // Try partial match
                LoadoutTypes[] enumValues = Enum.GetValues<LoadoutTypes>();
                LoadoutTypes partialMatch = enumValues.FirstOrDefault(e => e.ToString().StartsWith(loadoutType.ToUpper(System.Globalization.CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase));
                if (partialMatch != default)
                {
                    loadout = partialMatch;
                }
            }

            if (loadout.HasValue)
            {
                _loadouts[player.SteamID].Type = loadout.Value.ToString();
                command.ReplyToCommand(Localizer["command.loadout.changed"].Value
                    .Replace("{current}", loadout.Value.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)));
            }
            else
            {
                command.ReplyToCommand(Localizer["command.loadout.invalid"].Value
                    .Replace("{type}", loadoutType.ToLower(System.Globalization.CultureInfo.CurrentCulture)));
            }
        }
    }
}
