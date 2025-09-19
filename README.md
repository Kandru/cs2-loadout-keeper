# CounterstrikeSharp - Loadout Keeper

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-loadout-keeper?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-loadout-keeper/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-map-modifier](https://img.shields.io/github/issues/Kandru/cs2-loadout-keeper)](https://github.com/Kandru/cs2-loadout-keeper/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

Simple plug-in that remembers the player's bought or picked up weapons and gives these back to the player after (re)spawn. Saves them into a config file for each player (via SteamID) and loads them when a player joins the server.

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-loadout-keeper/releases/).
2. Move the "LoadoutKeeper" folder to the `/addons/counterstrikesharp/plugins/` directory.
3. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically. To automate updates please use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/).


## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/LoadoutKeeper/LoadoutKeeper.json`.

```json
{
  "enabled": true,
  "default_setting": "ALL",
  "enable_grenades": false,
  "reset_buy_menu_loadout": true,
  "allow_chat_command_for_primary_weapons": true,
  "primary_weapons_for_chat_command": [],
  "allow_chat_command_for_secondary_weapons": true,
  "secondary_weapons_for_chat_command": [],
  "announce_loadout_given_chat": false,
  "announce_loadout_given_center": false,
  "announce_loadout_given_center_alert": false,
  "disabled_map_types": [
    "awp_",
    "aim_"
  ],
  "ConfigVersion": 1
}
```

### enabled

Whether or not this plug-in is enabled.

### default_setting

The default type of weapons to save and give on respawn (every weapon_ key will be saved anyway but only the default_setting will be given to a player by default). Can be: ALL, WEAPONS, PRIMARY, SECONDARY, GRENADES OR ITEMS.

### enable_grenades

Disabled by default. If a player gets grenades on spawn he could not re-buy them in the buy menu if the maximum allowed types for a grenade have been reached... not fix known currently.

### reset_buy_menu_loadout

Wether or not to reset the buy menu loadout history on respawn. Defaults to true. Otherwise players can not properly change their loadout if weapons are given.

### allow_chat_command_for_primary_weapons

Whether or not primary weapons can be acquired via !m4 or similar at any given time if alive.

### primary_weapons_for_chat_command

List of allowed primary weapons. Needs to be the whole weapons string e.g. "weapon_m4a1". If empty every weapon is allowed.

### allow_chat_command_for_secondary_weapons

Whether or not secondary weapons can be acquired via !deagle or similar at any given time if alive.

### secondary_weapons_for_chat_command

List of allowed secondary weapons. Needs to be the whole weapons string e.g. "weapon_deagle". If empty every weapon is allowed.

### announce_loadout_given_*

How the player will be notified when loadout has been modified.

### disabled_map_types

Map types this plug-in is disabled for. E.g. `aim_` will disable all aim_* maps (looks up the beginning of the map name in lower case). Can also be a full map name where this is disabled for.

## Commands

### !loadout / !la <all|weapons|primary|secondary|grenades|items>

Either shows the player their current setting or allows them to change it.

### loadoutkeeper reload (server console only)

Reloads the plug-in configuration.

### loadoutkeeper enable (server console only)

Enables the plug-in and saves this state to the configuration.

### loadoutkeeper disable (server console only)

Disables the plug-in and saves this state to the configuration.

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-loadout-keeper.git
```

Go to the project directory

```bash
  cd cs2-loadout-keeper
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

## FAQ

TODO

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
