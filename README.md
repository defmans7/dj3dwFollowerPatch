# dj3dwFollowerPatch

A small BepInEx mod for Valheim. Tamed creatures that follow you heal over time, so they survive longer on trips and in fights.

## Features

- Followers heal a configurable percentage of their max health every second.
- Healing can be limited to creatures that are actively following a player.
- Healing pauses while the creature is alerted (fighting or chasing).
- Works in multiplayer without a server install: the client that owns the creature applies the heal, and the game's own sync sends the new health to everyone else. Each player who wants the effect needs the mod.

## Config

The config file is created on first launch at `BepInEx/config/dj3dw.FollowerPatch.cfg` (inside your r2modman profile, or the game folder for a plain BepInEx install).

| Key | Default | What it does |
| --- | --- | --- |
| `General.Enabled` | `true` | Turn the mod on or off. |
| `Healing.HealPercentPerSecond` | `0.5` | Percent of max health restored per second. 0.5 means a full heal takes about 200 seconds. |
| `Healing.OnlyWhenFollowing` | `true` | Only heal creatures that are following a player. `false` heals every tamed creature. |
| `Healing.OnlyOutOfCombat` | `true` | Pause healing while the creature is alerted. |
| `Healing.TickSeconds` | `1.0` | How often the heal is applied. Lower is smoother, but sends more network updates. |

## Requirements

- Valheim (Steam) with [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) installed, either through r2modman / Thunderstore Mod Manager or manually.
- .NET 8 SDK to build. It is only needed on the machine that builds the DLL.

## One-time setup

### 1. Install the .NET 8 SDK

Building runs from WSL in this repo, but a Windows build works the same way.

WSL (installs into `~/.dotnet`, no sudo, remove by deleting the folder):

```sh
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet"
```

Add this to `~/.bashrc` so `dotnet` is on your path:

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
```

Windows: install the SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and use PowerShell for the commands below.

### 2. Tell the build where Valheim is

```sh
cp Directory.Build.user.props.example Directory.Build.user.props
```

Edit `Directory.Build.user.props` and set two paths:

- `ValheimInstall`: the folder that contains `valheim.exe`. The build reads the game DLLs (`assembly_valheim.dll`, `UnityEngine.dll`, ...) straight from `Valheim_Data/Managed` so they always match your game version.
- `PluginDeployDir`: where the built DLL is copied after each build. Leave it empty to skip the copy and install by hand.

To find the r2modman profile folder: open r2modman, select the profile, then `Settings > Locations > Browse profile folder`. On WSL that path looks like `/mnt/c/Users/<you>/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/<profile>/`. The plugins folder is `BepInEx/plugins` inside it.

This file is gitignored because the paths are specific to your machine.

## Build

```sh
dotnet build -c Release
```

The first build restores the BepInEx and Harmony packages from NuGet (the BepInEx feed is in `nuget.config`). The output is `bin/Release/net472/dj3dwFollowerPatch.dll`. If `PluginDeployDir` is set, the build ends with a `Deployed ... to ...` line and the DLL is already in place.

Only the mod DLL is needed. BepInEx and Harmony are already in the game's `BepInEx/core` folder, so nothing else is copied.

## Packaged zip

Every build also writes `dist/dj3dwFollowerPatch-<version>.zip` in Thunderstore layout (DLL, `manifest.json`, `icon.png`, `README.md`). `dist/` is committed, so the repo holds every released build. Bump the version before a build you intend to keep, otherwise the zip for the current version is overwritten.

Uses for the zip:

- r2modman: `Settings > Profile > Import local mod`, pick the zip. The manager then lists and manages the mod like any other.
- Share it with other players, or upload it to Thunderstore.

The version is set in two places and must match: `Version` in `dj3dwFollowerPatch.csproj` (names the zip and the DLL) and `version_number` in `package/manifest.json`. `package/icon.png` is a placeholder; replace it with a 256x256 PNG before publishing.

## Install

### r2modman / Thunderstore Mod Manager

Copy `dj3dwFollowerPatch.dll` to `<profile>/BepInEx/plugins/dj3dw-FollowerPatch/` (the subfolder name is up to you). Start the game with the manager's **Start modded** button. The manager does not list DLLs added by hand, but BepInEx loads every DLL under `plugins`.

### Plain BepInEx (no mod manager)

Copy `dj3dwFollowerPatch.dll` to `<Valheim>/BepInEx/plugins/dj3dw-FollowerPatch/` and start the game normally.

### Check it loaded

Open `BepInEx/LogOutput.log` after the game reaches the main menu and look for:

```
[Info   :dj3dwFollowerPatch] dj3dwFollowerPatch 0.1.0 loaded
```

If the line is missing, the DLL is in the wrong folder or BepInEx is not running. If there is a red Harmony error near it, the game updated and a patched method changed name.

### Update or remove

Rebuild and copy the DLL over the old one to update. Delete the DLL (and the config file if you want) to remove the mod.

## How it works

`src/FollowerHealPatch.cs` adds a Harmony postfix to `MonsterAI.UpdateAI`. That method runs every AI tick, but only on the client that owns the creature, so the heal is applied once and the game's `Character.Heal` RPC syncs the result. The patch skips creatures that are not tamed, not following (if configured), alerted (if configured), or already at full health.

## Project layout

```
dj3dwFollowerPatch.csproj             build definition, game DLL references, deploy step
nuget.config                          NuGet feeds (nuget.org + BepInEx)
Directory.Build.user.props.example    template for machine-specific paths
src/Plugin.cs                         BepInEx entry point and config
src/FollowerHealPatch.cs              the Harmony patch
package/manifest.json                 Thunderstore manifest (keep version in sync with the csproj)
package/icon.png                      Thunderstore icon, 256x256 placeholder
dist/                                 packaged zips, one per version (committed)
```
