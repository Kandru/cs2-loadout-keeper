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
            switch (subCommand.ToLower())
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
                command.ReplyToCommand(Localizer["command.loadout.usage"].Value
                    .Replace("{current}", _loadouts.ContainsKey(player.SteamID) ? _loadouts[player.SteamID].Type.ToString().ToLower() : Config.DefaultLoadoutType.ToString().ToLower()));
                return;
            }

            // Try exact match first
            if (Enum.TryParse<LoadoutTypes>(loadoutType.ToUpper(), out var exactMatch))
            {
                loadout = exactMatch;
            }
            else
            {
                // Try partial match
                var enumValues = Enum.GetValues<LoadoutTypes>();
                var partialMatch = enumValues.FirstOrDefault(e => e.ToString().StartsWith(loadoutType.ToUpper(), StringComparison.OrdinalIgnoreCase));
                if (partialMatch != default(LoadoutTypes))
                {
                    loadout = partialMatch;
                }
            }

            if (loadout.HasValue)
            {
                _loadouts[player.SteamID].Type = loadout.Value.ToString();
                command.ReplyToCommand(Localizer["command.loadout.changed"].Value
                    .Replace("{current}", loadout.Value.ToString().ToLower()));
            }
            else
            {
                command.ReplyToCommand(Localizer["command.loadout.invalid"].Value
                    .Replace("{type}", loadoutType.ToLower()));
            }
        }
    }
}
