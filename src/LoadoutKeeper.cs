using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using LoadoutKeeper.Utils;

namespace LoadoutKeeper
{
    public partial class LoadoutKeeper : BasePlugin
    {
        public override string ModuleName => "CS2 LoadoutKeeper";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private readonly Dictionary<ulong, Dictionary<string, int>> _loadouts = [];

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventItemPickup>(OnItemPickup);
            if (hotReload)
            {
                foreach (CCSPlayerController player in Utilities.GetPlayers().Where(static p => !p.IsBot))
                {
                    // update player loadout
                    LoadConfig(player.SteamID);
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            DeregisterEventHandler<EventItemPickup>(OnItemPickup);
            SaveConfigs();
        }

        private void OnMapEnd()
        {
            SaveConfigs();
            _loadouts.Clear();
        }

        private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV)
            {
                return HookResult.Continue;
            }
            // try to load player loadout
            LoadConfig(player.SteamID);
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !Config.Enabled)
            {
                return HookResult.Continue;
            }
            // save loadouts if no players are left
            if (Utilities.GetPlayers().Count(static p => !p.IsBot) == 1)
            {
                SaveConfigs();
                _loadouts.Clear();
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.Team == CsTeam.None
                || !Config.Enabled)
            {
                return HookResult.Continue;
            }
            // give loadout to player
            GivePlayerLoadout(player);
            return HookResult.Continue;
        }

        private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !Config.Enabled)
            {
                return HookResult.Continue;
            }
            UpdatePlayerLoadout(player);
            return HookResult.Continue;
        }

        private void GivePlayerLoadout(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
            {
                return;
            }
            if (_loadouts.TryGetValue(player.SteamID, out Dictionary<string, int>? value) && value.Count > 0)
            {
                // remove old loadout
                player.RemoveWeapons();
                // give initial item(s)
                _ = player.GiveNamedItem("weapon_knife");
                // give loadout items
                Dictionary<string, int> loadout = new(value);
                foreach (var kvp in loadout)
                {
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        _ = player.GiveNamedItem(kvp.Key);
                    }
                }
            }
        }

        private void UpdatePlayerLoadout(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // set new loadout
            Dictionary<string, int> playerWeapons = [];
            foreach (CHandle<CBasePlayerWeapon> weaponHandle in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // skip invalid weapon handles
                if (weaponHandle == null
                    || !weaponHandle.IsValid)
                {
                    continue;
                }
                // get weapon from handle
                CBasePlayerWeapon? playerWeapon = weaponHandle.Value;
                // skip invalid weapon
                if (playerWeapon == null
                    || !playerWeapon.IsValid)
                {
                    continue;
                }
                // get weapon name
                string? weaponName = Entities.PlayerWeaponName(playerWeapon);
                if (string.IsNullOrEmpty(weaponName))
                {
                    continue;
                }
                // ignore knife & c4
                if (weaponName.Contains("knife", StringComparison.OrdinalIgnoreCase)
                    || weaponName.Contains("c4", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // add weapon to loadout
                if (playerWeapons.ContainsKey(weaponName))
                {
                    playerWeapons[weaponName]++;
                }
                else
                {
                    playerWeapons.Add(weaponName, 1);
                }
            }
            // do not add empty loadout
            if (playerWeapons.Count == 0)
            {
                return;
            }
            // update or add player's loadout
            if (!_loadouts.TryAdd(player.SteamID, playerWeapons))
            {
                _loadouts[player.SteamID] = playerWeapons;
            }
        }
    }
}
