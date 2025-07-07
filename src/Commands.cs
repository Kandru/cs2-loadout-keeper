using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;

namespace LoadoutKeeper
{
    public partial class LoadoutKeeper
    {
        [ConsoleCommand("loadoutkeeper", "LoadoutKeeper admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandMapVote(CCSPlayerController player, CommandInfo command)
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
    }
}
