# bnetlauncher

[![Build](https://github.com/IgorSnaidermans/bnetlauncher/actions/workflows/bnetlauncher-build.yaml/badge.svg)](https://github.com/IgorSnaidermans/bnetlauncher/actions/workflows/bnetlauncher-build.yaml)
[![Latest release](https://img.shields.io/github/v/release/IgorSnaidermans/bnetlauncher?include_prereleases)](https://github.com/IgorSnaidermans/bnetlauncher/releases/latest)

Start Battle.net games from Steam with the Steam overlay, or from a regular desktop shortcut, already
logged in and with little or no interaction with the Battle.net client.

This is a maintained fork of [dafzor/bnetlauncher](https://github.com/dafzor/bnetlauncher), which is in
maintenance mode. It adds new games, bug fixes and a setup app.

## Getting started

You need Windows 7 SP1 or newer and [.NET Framework 4.7.2](https://www.microsoft.com/net/download/dotnet-framework-runtime),
which is already included in Windows 10 version 1803 and newer.

### With the setup app

1. Download `bnetlauncher.exe` and `bnetlauncher-setup.exe` from the
   [latest release](https://github.com/IgorSnaidermans/bnetlauncher/releases) into the same folder.
2. Run `bnetlauncher-setup.exe`.
3. Tick your games and choose where `bnetlauncher.exe` should go.
4. Under **Create shortcuts**, choose **Steam** and the accounts to add the games to, **Desktop**, or both.
5. Click **Install**. Steam has to be closed while Steam shortcuts are written, and the setup offers to close it for you.
   Desktop shortcuts don't involve Steam at all.
6. Start your game from the desktop, or start Steam and find it in your library as a non-Steam shortcut.

Steam is optional, but the Steam overlay only works when the game is started from a Steam shortcut.

Running it again updates existing shortcuts instead of duplicating them. Before changing Steam's shortcuts, the setup
saves them as `shortcuts.vdf.bnetlauncher-setup.bak` next to the original in `<Steam>\userdata\<account id>\config\`.

The setup app only lists Battle.net games. Add Epic Games Store titles manually.

### Manually

1. Put `bnetlauncher.exe` anywhere you like, such as your Steam folder.
2. Add it to Steam as a non-Steam game, or create a regular Windows shortcut to it.
3. Add a code from the [list below](#supported-games) to the launch options in Steam, or to the end of the Windows shortcut's target.

The shortcut's target and launch options end up looking like `"G:\Steam\bnetlauncher.exe" ow`.

## Supported games

### Battle.net

| Code | Game |
| --- | --- |
| `cb4` | Crash Bandicoot 4: It's About Time |
| `codbo4` | Call of Duty: Black Ops 4 |
| `codbocw` | Call of Duty: Black Ops Cold War |
| `codhq` | Call of Duty HQ (MW3 2023 and newer) |
| `codmw2` | Call of Duty: Modern Warfare II + Warzone 2.0 |
| `codmw2019` | Call of Duty: Modern Warfare (2019) |
| `codmw2cr` | Call of Duty: Modern Warfare 2 Campaign Remastered |
| `codvg` | Call of Duty: Vanguard |
| `d2r` | Diablo 2: Resurrected |
| `d3` | Diablo 3 |
| `d3ptr` | Diablo 3 Public Test Realm |
| `d4` | Diablo 4 |
| `di` | Diablo Immortal |
| `hots` | Heroes of the Storm |
| `hotsptr` | Heroes of the Storm Public Test Realm |
| `hs` | Hearthstone |
| `ow` | Overwatch |
| `owptr` | Overwatch Public Test Realm |
| `sc2` | Starcraft 2 |
| `scr` | Starcraft Remastered |
| `w1r` | Warcraft I: Remastered |
| `w2r` | Warcraft II: Remastered |
| `w3` | Warcraft 3: Reforged |
| `wow` | World of Warcraft |
| `wow_anniversary` | World of Warcraft Burning Crusade Anniversary |
| `wowclassic` | World of Warcraft Classic |
| `wowclassicbeta` | World of Warcraft Classic Beta |
| `wowclassicera` | World of Warcraft Classic Era |
| `wowclassictitan` | World of Warcraft Titan Reforged Classic |
| `wowforever` | World of Warcraft: Forever |
| `wowptr` | World of Warcraft Public Test Realm |

### Epic Games Store

| Code | Game |
| --- | --- |
| `dauntless` | Dauntless |
| `ebatmanarkknight` | Batman Arkham Knight |
| `ebl3` | Borderlands 3 |
| `egta5` | GTA V |
| `esubnautica` | Subnautica |
| `etalosprincipal` | Talos Principal |
| `ewwz` | World War Z |

## Launch options

Add these after the game code.

| Option | What it does |
| --- | --- |
| `--timeout <seconds>`, `-t <seconds>` | How long to look for the game before showing an error. The default is 15. |
| `--notask`, `-n` | Start the client directly instead of through Task Scheduler. Steam then applies the overlay to the client and shows you as playing until the client exits. |
| `--leaveopen`, `-l` | Leave the client open after launching the game. |

By default bnetlauncher keeps the client as it found it: if the client wasn't running, it's closed again afterwards.

## Troubleshooting

To turn on logging, create an empty `enablelog.txt` in `%localappdata%\madalien.com\bnetlauncher\`.
Logs aren't cleaned up automatically, so remove the file when you're done.

If the setup app can't find Steam or Battle.net, run `bnetlauncher-setup.exe --diag` from a terminal. It prints what
was detected and changes nothing. Include the output and the setup's saved log when you
[open an issue](https://github.com/IgorSnaidermans/bnetlauncher/issues).

Known issues:

- **Classic and PTR versions need a click sometimes.** Games on the `battlenet2` client are started by pressing the
  Play button automatically. If that fails, or a Battle.net ad gets in the way, press Play yourself.
- **The game isn't found in time.** Slow PCs may need a longer `--timeout`. Some Call of Duty: Cold War players do better with `--timeout 10`.
- **Permissions must match.** The game, bnetlauncher and Steam must all run elevated, or none of them.
- **Other overlays can crash the game.** Disable overlays such as MSI Afterburner or Fraps.
- **Multiple client instances can break launching.** Turn off that option in Battle.net.
- **Scheduled task problems.** If launching through Task Scheduler doesn't work on your setup, use `--notask`.
- **No region selection.** Create a custom entry with the `nolaunch` option and pick the region yourself before clicking Play.
- **Starcraft Remastered only allows one instance,** so starting a second copy may show an error.

## Adding your own games

bnetlauncher reads extra entries from a `gamesdb.ini` placed next to `bnetlauncher.exe` or in
`%localappdata%\madalien.com\bnetlauncher\`. The `gamesdb.ini.example` in each release is a copy of the built-in list.
Built-in entries always win, so use a new section name for anything you customize.

```ini
[codbo4]
name=Call of Duty: Black Ops 4
client=battlenet
cmd=VIPR
exe=BlackOps4.exe
options=noargs,waitforexit
```

| Key | Meaning |
| --- | --- |
| `[section]` | The code you pass to bnetlauncher. |
| `name` | Friendly name for messages. |
| `client` | `battlenet`, `battlenet2` or `epic`. `battlenet2` can launch PTR and Classic versions but can be less reliable. |
| `cmd` | The case-sensitive launch ID, which depends on the client (see below). |
| `exe` | The game executable to look for. `%` is a wildcard, as in `Diablo III%.exe`. |
| `options` | Comma-separated options (see below). |

Where to find `cmd`:

- **`battlenet`:** in `%LOCALAPPDATA%\Battle.net\Logs\battle.net*.log`.
- **`battlenet2`:** the product code, in `C:\ProgramData\Battle.net\Setup\<game>\*.log`.
- **`epic`:** create a desktop shortcut in the Epic launcher and copy the ID from its URL, `com.epicgames.launcher://apps/<id>?action=launch&silent=true`.

Options:

- `noargs`: don't fail when the game's arguments come back blank (needed for Black Ops 4).
- `waitforexit`: keep bnetlauncher open until the game exits.
- `nolaunch`: only open the client and look for the game for an extra 60 seconds, so you can start it yourself.
- `notask`: don't start the client through Task Scheduler.
- `noadmin`: try to avoid the UAC prompt with compatibility flags. This is an untested hack that can break the game.

## Uninstalling

1. Remove the shortcuts from your Steam library and delete `bnetlauncher.exe`.
2. Open `taskschd.msc` and delete the `bnetlauncher` folder from the Task Scheduler Library.
3. Delete `%localappdata%\madalien.com\bnetlauncher\`.

## Building

Open `bnetlauncher.sln` in Visual Studio, or run:

```
nuget restore bnetlauncher.sln
msbuild bnetlauncher.sln /p:Configuration=Release
```

Pushing a `v*` tag builds and publishes a GitHub release. Tags containing a hyphen, such as `v2.26-forever.2`, are published as pre-releases.

## Credits

bnetlauncher was created by [dafzor](https://github.com/dafzor). Thanks also to Maruf (ghost tray icon fix),
/u/fivetwofoureight (icon), /u/malecden, Maverick, /u/sumphatguy and these GitHub contributors: Ethan-BB, RobFreiburger,
iMintty, jbzdarkid, jacobmix, KyleStilkey, d3rt0xx, Sectimus, LazymanOnGithub, Aqvilinus, magnww and AdamsGH.

Licensed under the GPLv3. See [license.txt](license.txt).
