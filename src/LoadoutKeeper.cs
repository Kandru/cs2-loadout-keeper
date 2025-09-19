using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
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
        private readonly List<CCSPlayerController> _spawnCooldowns = [];
        private readonly List<string> _primaryWeapons = [
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
        ];
        private readonly List<string> _secondaryWeapons = [
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
        ];

        private readonly List<string> _grenades = [
            "weapon_flashbang",
            "weapon_hegrenade",
            "weapon_incgrenade",
            "weapon_molotov",
            "weapon_smokegrenade",
            "weapon_decoy"
        ];

        private readonly List<string> _items = [
            "weapon_taser",
            //"item_assaultsuit",
            //"item_defuser",
            //"item_kevlar"
        ];

        private readonly List<string> _itemsToKeep = [
            "knife",
            "c4",
            CsItem.C4.ToString(),
            CsItem.Bomb.ToString(),
            CsItem.Knife.ToString(),
            CsItem.KnifeT.ToString(),
            CsItem.KnifeCT.ToString(),
            CsItem.DefaultKnifeT.ToString(),
            CsItem.DefaultKnifeCT.ToString()
        ];

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventItemPickup>(OnItemPickup);
            if (hotReload)
            {
                foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(static p => !p.IsBot))
                {
                    // update player loadout
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
            // remove player from cooldown list
            _ = _spawnCooldowns.Remove(player);
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
            // remove player from cooldown list after a short delay
            _ = AddTimer(0.1f, () =>
        {
            if (player == null
            || !player.IsValid)
            {
                return;
            }
            if (_spawnCooldowns.Contains(player))
            {
                _ = _spawnCooldowns.Remove(player);
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

        private void GivePlayerLoadout(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // get player loadout
            if (_loadouts.TryGetValue(player.SteamID, out LoadoutConfig? loadout) && loadout.Weapons.Count > 0)
            {
                // remove all non-essential items from player loadout
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
                    // ignore specific weapons
                    string? weaponName = Entities.PlayerWeaponName(playerWeapon);
                    if (weaponName == null
                        || _itemsToKeep.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    // ignore weapons if player wished so
                    switch (loadout.Type)
                    {
                        case nameof(LoadoutTypes.WEAPONS):
                            // do ignore everything which is NOT a weapon
                            if (_grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.PRIMARY):
                            // do ignore weapons which are NOT primary weapons
                            if (_secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.SECONDARY):
                            // do ignore weapons which are NOT secondary weapons
                            if (_primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.GRENADES):
                            // do ignore weapons which are NOT grenades
                            if (_primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.ITEMS):
                            // do ignore weapons which are NOT items
                            if (_primaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _secondaryWeapons.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => weaponName.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.ALL):
                        default:
                            break;
                    }
                    // set weapon as currently active weapon
                    player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weaponHandle.Raw;
                    // drop active weapon
                    player.DropActiveWeapon();
                    // delete weapon entity
                    playerWeapon.AddEntityIOEvent("Kill", playerWeapon, null, "", 0.1f);
                }
                // give loadout items
                foreach (KeyValuePair<string, int> kvp in loadout.Weapons)
                {
                    // ignore weapons if player wished so
                    switch (loadout.Type)
                    {
                        case nameof(LoadoutTypes.WEAPONS):
                            // do ignore everything which is NOT a weapon
                            if (_grenades.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.PRIMARY):
                            // do ignore weapons which are NOT primary weapons
                            if (_secondaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.SECONDARY):
                            // do ignore weapons which are NOT secondary weapons
                            if (_primaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.GRENADES):
                            // do ignore weapons which are NOT grenades
                            if (_primaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _secondaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _items.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.ITEMS):
                            // do ignore weapons which are NOT items
                            if (_primaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _secondaryWeapons.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase))
                                || _grenades.Any(item => kvp.Key.Contains(item, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
                            break;
                        case nameof(LoadoutTypes.ALL):
                        default:
                            break;
                    }
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        _ = player.GiveNamedItem(kvp.Key);
                    }
                }
                // announcement
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
        }

        private void UpdatePlayerLoadout(CCSPlayerController player, string Item)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // check if player has loadout or create otherwise
            if (!_loadouts.TryGetValue(player.SteamID, out LoadoutConfig? loadout))
            {
                loadout = new LoadoutConfig();
                _loadouts[player.SteamID] = loadout;
            }
            // check if weapon is found
            string? _primaryWeapon = _primaryWeapons.FirstOrDefault(w => w.Contains(Item, StringComparison.OrdinalIgnoreCase));
            string? _secondaryWeapon = _secondaryWeapons.FirstOrDefault(w => w.Contains(Item, StringComparison.OrdinalIgnoreCase));
            string? _grenade = _grenades.FirstOrDefault(w => w.Contains(Item, StringComparison.OrdinalIgnoreCase));
            string? _item = _items.FirstOrDefault(w => w.Contains(Item, StringComparison.OrdinalIgnoreCase));

            if (_primaryWeapon != null)
            {
                // Only allow one primary weapon in loadout
                foreach (string weapon in _primaryWeapons)
                {
                    _ = loadout.Weapons.Remove(weapon);
                }
                // check if m4a1 or m4a1_silencer was selected (both report back as m4a1 unfortunately)
                if (Item.Equals("m4a1", StringComparison.OrdinalIgnoreCase))
                {
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
                        if (weaponName != null && weaponName.Contains("m4a1", StringComparison.OrdinalIgnoreCase))
                        {
                            _primaryWeapon = weaponName;
                            break;
                        }
                    }
                }

                loadout.Weapons[_primaryWeapon] = 1;
            }
            else if (_secondaryWeapon != null)
            {
                // Only allow one secondary weapon in loadout
                foreach (string weapon in _secondaryWeapons)
                {
                    _ = loadout.Weapons.Remove(weapon);
                }
                loadout.Weapons[_secondaryWeapon] = 1;
            }
            else if (_grenade != null)
            {
                // Only add grenade if not already present
                if (!loadout.Weapons.ContainsKey(_grenade))
                {
                    loadout.Weapons[_grenade] = 1;
                }
            }
            else if (_item != null)
            {
                // Only allow one of each item
                loadout.Weapons[_item] = 1;
            }
            else if (Item.Equals("vest", StringComparison.OrdinalIgnoreCase))
            {
                // give kevlar if player does not have it
                if (!loadout.Weapons.ContainsKey("item_kevlar"))
                {
                    loadout.Weapons["item_kevlar"] = 1;
                }
            }
            else if (Item.Equals("vesthelm", StringComparison.OrdinalIgnoreCase))
            {
                // give kevlar if player does not have it
                if (!loadout.Weapons.ContainsKey("item_assaultsuit"))
                {
                    loadout.Weapons["item_assaultsuit"] = 1;
                }
            }
            else
            {
                Console.WriteLine($"Item {Item} is not a valid loadout item for player {player.SteamID}, skipping...");
            }
        }
    }
}
