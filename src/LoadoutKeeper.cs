using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using LoadoutKeeper.Enums;
using LoadoutKeeper.Utils;

namespace LoadoutKeeper
{
    public partial class LoadoutKeeper : BasePlugin
    {
        public override string ModuleName => "CS2 LoadoutKeeper";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private bool _isDisabledMapType = false;
        private readonly Dictionary<ulong, LoadoutConfig> _loadouts = [];
        private readonly HashSet<CCSPlayerController> _spawnCooldowns = [];

        private readonly HashSet<string> _primaryWeapons = new(StringComparer.OrdinalIgnoreCase)
        {
            "weapon_ak47",
            "weapon_aug",
            "weapon_awp",
            "weapon_bizon",
            "weapon_famas",
            "weapon_g3sg1",
            "weapon_galilar",
            "weapon_m249",
            "weapon_m4a1",
            "weapon_m4a1_silencer",
            "weapon_mac10",
            "weapon_mag7",
            "weapon_mp5sd",
            "weapon_mp7",
            "weapon_mp9",
            "weapon_negev",
            "weapon_nova",
            "weapon_p90",
            "weapon_sawedoff",
            "weapon_scar20",
            "weapon_sg556",
            "weapon_ssg08",
            "weapon_ump45",
            "weapon_xm1014"
        };
        private readonly HashSet<string> _secondaryWeapons = new(StringComparer.OrdinalIgnoreCase)
        {
            "weapon_cz75a",
            "weapon_deagle",
            "weapon_elite",
            "weapon_fiveseven",
            "weapon_glock",
            "weapon_p250",
            "weapon_revolver",
            "weapon_tec9",
            "weapon_usp_silencer",
            "weapon_hkp2000"
        };

        private readonly HashSet<string> _grenades = new(StringComparer.OrdinalIgnoreCase)
        {
            "weapon_flashbang",
            "weapon_hegrenade",
            "weapon_incgrenade",
            "weapon_molotov",
            "weapon_smokegrenade",
            "weapon_decoy"
        };

        private readonly HashSet<string> _items = new(StringComparer.OrdinalIgnoreCase)
        {
            "weapon_taser",
            "defuser"
        };

        private readonly HashSet<string> _itemsToKeep = new(StringComparer.OrdinalIgnoreCase)
        {
            "knife",
            "c4",
            CsItem.C4.ToString(),
            CsItem.Bomb.ToString(),
            CsItem.Knife.ToString(),
            CsItem.KnifeT.ToString(),
            CsItem.KnifeCT.ToString(),
            CsItem.DefaultKnifeT.ToString(),
            CsItem.DefaultKnifeCT.ToString()
        };

        private bool ShouldRemoveWeapon(string weaponName, LoadoutTypes loadoutType)
        {
            return loadoutType switch
            {
                LoadoutTypes.WEAPONS => _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)),
                LoadoutTypes.PRIMARY => _secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)),
                LoadoutTypes.SECONDARY => _primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)),
                LoadoutTypes.GRENADES => _primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || (!Config.EnableGrenades && _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))),
                LoadoutTypes.ITEMS => _primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)),
                LoadoutTypes.ALL => !Config.EnableGrenades && _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        private string? FindWeaponInCategory(string item, HashSet<string> category)
        {
            return category.FirstOrDefault(w => w.Contains(item, StringComparison.OrdinalIgnoreCase));
        }

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventItemPickup>(OnItemPickup);
            RegisterEventHandler<EventBotTakeover>(OnBotTakeover);
            RegisterEventHandler<EventPlayerChat>(OnPlayerChatCommand);
            if (hotReload)
            {
                foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(static p => !p.IsBot))
                {
                    LoadConfig(entry.SteamID);
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            DeregisterEventHandler<EventItemPickup>(OnItemPickup);
            DeregisterEventHandler<EventBotTakeover>(OnBotTakeover);
            DeregisterEventHandler<EventPlayerChat>(OnPlayerChatCommand);
            SaveConfigs();
        }

        private void OnMapStart(string mapName)
        {
            // check if map type is disabled
            _isDisabledMapType = Config.DisabledMapTypes.Any(type => mapName.StartsWith(type, StringComparison.CurrentCultureIgnoreCase) || mapName.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        private void OnMapEnd()
        {
            SaveConfigs();
            _loadouts.Clear();
            _spawnCooldowns.Clear();
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
            // try to load player loadout only if not already loaded
            if (!_loadouts.ContainsKey(player.SteamID))
            {
                LoadConfig(player.SteamID);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot)
            {
                return HookResult.Continue;
            }
            // save loadouts if no players are left
            if (Utilities.GetPlayers().Count(static p => !p.IsBot) == 1)
            {
                SaveConfigs();
                _loadouts.Clear();
            }
            _spawnCooldowns.Remove(player);
            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.Team == CsTeam.None
                || !Config.Enabled
                || _isDisabledMapType)
            {
                return HookResult.Continue;
            }
            // add player to cooldown list to prevent updating loadout on spawn..
            if (!_spawnCooldowns.Contains(player))
            {
                _spawnCooldowns.Add(player);
            }
            // give loadout to player
            Server.NextFrame(() => GivePlayerLoadout(player));
            // reset buy menu loadout if enabled in config
            if (Config.ResetBuyMenuLoadout)
            {
                Players.ResetBuyMenuLoadout(player);
            }
            AddTimer(0.1f, () =>
            {
                if (player?.IsValid == true)
                {
                    _spawnCooldowns.Remove(player);
                }
            });
            return HookResult.Continue;
        }

        private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !Config.Enabled
                || _isDisabledMapType
                || _spawnCooldowns.Contains(player))
            {
                return HookResult.Continue;
            }
            string item = @event.Item;
            Server.NextFrame(() =>
            {
                // update player loadout after item pickup
                UpdatePlayerLoadout(player, item);
            });
            return HookResult.Continue;
        }

        private HookResult OnBotTakeover(EventBotTakeover @event, GameEventInfo info)
        {
            // TODO: weapons get remove but cannot be given again
            CCSPlayerController? player = @event?.Userid;
            CCSPlayerController? bot = Utilities.GetPlayerFromIndex((int)(player?.ObserverPawn?.Value?.Controller?.Value?.Index ?? 0));
            if (_isDisabledMapType
                || !Config.GiveLoadoutOnBotTakeover
                || player == null
                || !player.IsValid
                || player.IsBot
                || bot == null
                || !bot.IsValid
                || !bot.IsBot
                || !Config.Enabled)
            {
                return HookResult.Continue;
            }
            // get bot from players
            //GivePlayerLoadout(bot);
            return HookResult.Continue;
        }

        private HookResult OnPlayerChatCommand(EventPlayerChat @event, GameEventInfo info)
        {
            // ignore if players cannot buy weapons via chat command
            if (_isDisabledMapType
                || (!Config.AllowChatCommandForPrimaryWeapons
                && !Config.AllowChatCommandForSecondaryWeapons))
            {
                return HookResult.Continue;
            }
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value?.WeaponServices == null
                || player.Pawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                return HookResult.Continue;
            }
            // check which weapon type was requested
            string message = @event.Text.Trim();
            // ignore messages without prefix
            if ((!message.StartsWith("!")
                && !message.StartsWith("/"))
                || message == "!lo"
                || message == "!loadout")
            {
                return HookResult.Continue;
            }
            // Remove non-alphanumeric characters and make lowercase
            message = new string([.. message.Where(c => char.IsLetterOrDigit(c) || c == '_')]);
            IEnumerable<string> allowedPrimary = Config.AllowedChatCommandPrimaryWeapons?.Count > 0
                ? Config.AllowedChatCommandPrimaryWeapons
                : _primaryWeapons;
            IEnumerable<string> allowedSecondary = Config.AllowedChatCommandSecondaryWeapons?.Count > 0
                ? Config.AllowedChatCommandSecondaryWeapons
                : _secondaryWeapons;
            bool isPrimary = allowedPrimary.Any(item => item.Contains(message, StringComparison.OrdinalIgnoreCase));
            bool isSecondary = allowedSecondary.Any(item => item.Contains(message, StringComparison.OrdinalIgnoreCase));
            // ignore if no valid weapon type was requested or if players cannot buy the requested weapon type
            if ((!isPrimary
                && !isSecondary)
                || (isPrimary && !Config.AllowChatCommandForPrimaryWeapons)
                || (isSecondary && !Config.AllowChatCommandForSecondaryWeapons))
            {
                return HookResult.Continue;
            }
            string? requestedWeapon = isPrimary
                ? allowedPrimary.FirstOrDefault(w => w.Contains(message, StringComparison.OrdinalIgnoreCase))
                : allowedSecondary.FirstOrDefault(w => w.Contains(message, StringComparison.OrdinalIgnoreCase));
            // check if requested weapon is already in loadout
            if (requestedWeapon == null)
            {
                return HookResult.Continue;
            }
            // remove weapon (if any) from loadout before giving new one
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
                string? weaponName = Entities.PlayerWeaponName(playerWeapon);
                if (weaponName == null)
                {
                    continue;
                }
                // ignore if not found in primary weapons
                if (isPrimary && !allowedPrimary.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                // ignore if not found in secondary weapons
                if (isSecondary && !allowedSecondary.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                // set weapon as currently active weapon
                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weaponHandle.Raw;
                // drop active weapon
                player.DropActiveWeapon();
                // delete weapon entity
                playerWeapon.AddEntityIOEvent("Kill", playerWeapon, null, "", 0.1f);
            }
            // give player the requested weapon
            _ = player.GiveNamedItem(requestedWeapon);
            UpdatePlayerLoadout(player, requestedWeapon);
            // announcement
            player.PrintToChat(Localizer["loadout.given.custom"].Value
                .Replace("{name}", requestedWeapon.Replace("weapon_", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
            if (isPrimary)
            {
                player.ExecuteClientCommand("slot1");
            }
            else
            {
                player.ExecuteClientCommand("slot2");
            }
            return HookResult.Continue;
        }

        private void GivePlayerLoadout(CCSPlayerController player)
        {
            if (player?.IsValid != true || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }

            if (!_loadouts.TryGetValue(player.SteamID, out LoadoutConfig? loadout) || loadout.Weapons.Count == 0)
            {
                return;
            }

            if (!Enum.TryParse(loadout.Type, out LoadoutTypes loadoutType))
            {
                return;
            }

            RemoveCurrentWeapons(player, loadoutType);
            GiveLoadoutWeapons(player, loadout, loadoutType);
            AnnounceLoadout(player);
        }

        private void RemoveCurrentWeapons(CCSPlayerController player, LoadoutTypes loadoutType)
        {
            if (player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }

            foreach (CHandle<CBasePlayerWeapon> weaponHandle in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                if (weaponHandle?.Value is not CBasePlayerWeapon playerWeapon || !playerWeapon.IsValid)
                {
                    continue;
                }

                string? weaponName = Entities.PlayerWeaponName(playerWeapon);
                if (weaponName == null || _itemsToKeep.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)) || ShouldRemoveWeapon(weaponName, loadoutType))
                {
                    continue;
                }

                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weaponHandle.Raw;
                player.DropActiveWeapon();
                playerWeapon.AddEntityIOEvent("Kill", playerWeapon, null, "", 0.1f);
            }
        }

        private void GiveLoadoutWeapons(CCSPlayerController player, LoadoutConfig loadout, LoadoutTypes loadoutType)
        {
            foreach (KeyValuePair<string, int> kvp in loadout.Weapons)
            {
                if (ShouldRemoveWeapon(kvp.Key, loadoutType))
                {
                    continue;
                }

                if (kvp.Key.Equals("defuser", StringComparison.OrdinalIgnoreCase) && player.Team == CsTeam.CounterTerrorist)
                {
                    if (player.Pawn?.Value?.ItemServices != null)
                    {
                        new CCSPlayer_ItemServices(player.Pawn.Value.ItemServices.Handle) { HasDefuser = true };
                    }
                    continue;
                }

                for (int i = 0; i < kvp.Value; i++)
                {
                    player.GiveNamedItem(kvp.Key);
                }
            }
        }

        private void AnnounceLoadout(CCSPlayerController player)
        {
            if (Config.AnnounceLoadoutGivenChat)
            {
                player.PrintToChat(Localizer["loadout.given.chat"]);
            }
            if (Config.AnnounceLoadoutGivenCenter)
            {
                player.PrintToCenter(Localizer["loadout.given.center"]);
            }
            if (Config.AnnounceLoadoutGivenCenterAlert)
            {
                player.PrintToCenterAlert(Localizer["loadout.given.center"]);
            }
        }

        private void UpdatePlayerLoadout(CCSPlayerController player, string item)
        {
            if (player?.IsValid != true || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }

            if (!_loadouts.TryGetValue(player.SteamID, out LoadoutConfig? loadout))
            {
                loadout = new LoadoutConfig();
                _loadouts[player.SteamID] = loadout;
            }

            string? primaryWeapon = FindWeaponInCategory(item, _primaryWeapons);
            if (primaryWeapon != null)
            {
                RemoveWeaponsFromCategory(loadout, _primaryWeapons);

                if (item.Equals("m4a1", StringComparison.OrdinalIgnoreCase))
                {
                    primaryWeapon = GetActualM4Variant(player) ?? primaryWeapon;
                }

                loadout.Weapons[primaryWeapon] = 1;
                return;
            }

            string? secondaryWeapon = FindWeaponInCategory(item, _secondaryWeapons);
            if (secondaryWeapon != null)
            {
                RemoveWeaponsFromCategory(loadout, _secondaryWeapons);
                loadout.Weapons[secondaryWeapon] = 1;
                return;
            }

            string? grenade = FindWeaponInCategory(item, _grenades);
            if (grenade != null)
            {
                if (!loadout.Weapons.ContainsKey(grenade))
                {
                    loadout.Weapons[grenade] = 1;
                }
                return;
            }

            string? itemName = FindWeaponInCategory(item, _items);
            if (itemName != null)
            {
                loadout.Weapons[itemName] = 1;
                return;
            }

            if (item.Equals("vest", StringComparison.OrdinalIgnoreCase))
            {
                loadout.Weapons.TryAdd("item_kevlar", 1);
            }
            else if (item.Equals("vesthelm", StringComparison.OrdinalIgnoreCase))
            {
                loadout.Weapons.TryAdd("item_assaultsuit", 1);
            }
        }

        private string? GetActualM4Variant(CCSPlayerController player)
        {
            if (player.Pawn?.Value?.WeaponServices == null)
            {
                return null;
            }

            foreach (CHandle<CBasePlayerWeapon> weaponHandle in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                if (weaponHandle?.Value is CBasePlayerWeapon playerWeapon && playerWeapon.IsValid)
                {
                    string? weaponName = Entities.PlayerWeaponName(playerWeapon);
                    if (weaponName?.Contains("m4a1", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return weaponName;
                    }
                }
            }
            return null;
        }

        private void RemoveWeaponsFromCategory(LoadoutConfig loadout, HashSet<string> weapons)
        {
            foreach (string weapon in weapons)
            {
                loadout.Weapons.Remove(weapon);
            }
        }
    }
}
