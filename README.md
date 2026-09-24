# Persistent Work Areas

Keep a building's working-area outline visible after deselecting it, including while painting crops, planting trees, or using other tools.

**Website:** <https://timbermods.github.io/PersistentWorkAreas/> has the feature overview, an install guide, troubleshooting, and an FAQ.

## New in 1.1.0

- **Planting tools show their planters.** Pick a crop or tree in the planting tools and the working areas of every building that can plant it appear on their own: farmhouses for crops, aquatic farmhouses for aquatic crops, and foresters for trees and bushes, including ones still under construction. They disappear when you leave the tool. **Clear pinned areas** clears only your pins.
- **Pins are remembered.** Each settlement's pins are kept on your own computer in `PersistentWorkAreas\Pins.txt`, next to your Timberborn `Saves` folder, and come back when you load that settlement. They are never written into the save, so each co-op player keeps their own.
- **Faster refreshes with many pins.** A path or terrain change now re-checks only the pinned buildings it can reach, and selecting a pinned building no longer recalculates every pin.
- **Translatable text.** All UI text now comes from `Localizations/enUS_PersistentWorkAreas.csv`, so a translation is one more CSV. The English text is unchanged.

When updating, replace the whole `PersistentWorkAreas` folder. The new DLL reads its text from the CSV, so copying only the DLL over 1.0.0 shows raw keys.

1.1.0 passes 142 automated checks and has been playtested extensively in game.

### Earlier versions

- **1.0.0:** First stable release, with no behavior changes from 0.1.3, tested in game single-player and in a two-player BeaverBuddies Stability Fork session. The game-log line reads its version number from the mod itself, and the release zip uses standard `/` folder separators.
- **0.1.3:** Documentation correction only. The game itself draws a Builder's Hut's range outline while it is selected; that is vanilla behavior, not something this mod adds or changes.
- **0.1.2:** Builder's Huts can no longer be pinned, so they no longer show a "Working area" panel.
- **0.1.1:** The pin control gained a clearly drawn checkbox, an ON/OFF badge, a bordered panel, and hover/keyboard-focus highlighting.
- **0.1.0:** First preview.

## Install

1. Close Timberborn.
2. Download `PersistentWorkAreas-v1.1.0.zip` from **Assets** on the [latest release](https://github.com/timbermods/PersistentWorkAreas/releases/latest). Don't use the `-source.zip` or the **Source code** archives. <!-- latest -->
3. Extract it into your Timberborn `Mods` folder (normally `Documents\Timberborn\Mods`). The result should be `Mods\PersistentWorkAreas\version-1.1\manifest.json` with `PersistentWorkAreas.dll` beside it.
4. Start Timberborn and enable **Persistent Work Areas** in the mod manager. Restart if prompted.

Requires Timberborn **1.1.2.4** or a compatible 1.1 build. Built and tested on Timberborn 1.1.2.4. Later versions may change the internal renderer API.

No other mods are required: not Harmony, Mod Settings or BeaverBuddies. The mod works the same in single-player and in co-op. If you play co-op with the BeaverBuddies Stability Fork, keep that installation as it is. For co-op, every player installs the same version of the mod and runs the same game version; that is the tested setup. Pins are always local to each player. The mod sends nothing over the network, so a player without it shouldn't cause a desync, but running it on only one computer has not been playtested.

## Use

- Select a farm, forester, lumberjack flag, gatherer, or another building with a terrain/road-spill working range.
- Check **Keep working area visible**.
- Deselect it and use your planting/building tools. The outline remains.
- Pick a crop or tree in the planting tools and the working areas of the buildings that plant it appear on their own: every farmhouse for crops (aquatic farmhouses for aquatic crops) and every forester for trees and bushes, including ones still under construction. They disappear when you leave the planting tool; your pins stay.
- Pin additional buildings as needed. Overlapping pinned areas merge into a combined outline.
- Click **Clear pinned areas (N)** at the top right to remove every pin, without finding or selecting any building. Areas the planting tool shows stay until you leave that tool.
- For a keyboard shortcut, assign **Clear all pinned working areas** under **Persistent Work Areas** in the game's key-binding settings. It starts unbound to avoid taking an existing shortcut.
- To remove just one pin, select its building and uncheck the checkbox.

The currently selected building still uses its normal game outline. After you clear the pins, deselecting a building hides its outline again, as in the base game. Deleting a pinned building removes its pin.

### Where pins are kept

Pins are remembered for each settlement on your own computer, in `PersistentWorkAreas\Pins.txt` next to your Timberborn `Saves` folder (on Windows, `Documents\Timberborn\PersistentWorkAreas\Pins.txt`). When you load any save of that settlement, the latest pins you set there come back. A different settlement starts with none.

- Pins are never written into the save, so co-op players each keep their own.
- A co-op guest's game names the settlement after the host's save, which changes with every resync, so a guest's pins usually do not come back. The host's should come back.
- The file keeps the 100 settlements you loaded or changed most recently.
- To forget every pin, delete that file.

### Limits and translations

The mod supports navigation-based working areas. It does not pin district road coloring, effect-radius overlays that are not navigation-based, or building-placement ghosts.

UI text is currently English. The panel, button and key-binding text is all in `version-1.1/Localizations/enUS_PersistentWorkAreas.csv` (in the repository, under `packaging/PersistentWorkAreas/`), so a translation is one more file in that folder with the same keys, named for the game's language code (for example `deDE_PersistentWorkAreas.csv`). Keep `{0}` in the clear-button text: it becomes the pin count.

## Compatibility and validation

Designed for compatibility with the **BeaverBuddies Stability Fork**, but it doesn't depend on any BeaverBuddies build and works in single-player without one. It is a local display only. It does not patch game methods, change simulation or building data, send multiplayer events, or modify saves; the pins live in their own small local file. It uses the same navigation queries as the game's selected-building visualizer and a separate instance of its outline renderer. The Stability Fork's co-op join check compares only the game and BeaverBuddies builds, so this mod doesn't affect joining. Co-op has been tested only with the Stability Fork, not with the original [BeaverBuddies](https://github.com/thomaswp/BeaverBuddies) or BeaverBuddies MultiColony.

The release build passes 142 automated checks. 77 of them test the pin, refresh-planning, planting-tool and pin-file logic without the game, and 65 check the mod against the installed game's assemblies and blueprints. Version 1.0.0 was tested in game on Timberborn 1.1.2.4, both single-player and in a two-player BeaverBuddies Stability Fork session with the mod installed on both computers. The features new in 1.1.0 have not been playtested in game yet. See `VALIDATION.md` for what was checked and how.

## Build from source

Install the .NET 8 SDK and have Timberborn installed, then run:

```powershell
.\build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'
```

The script builds the mod, runs checks, and creates `dist\PersistentWorkAreas-v1.1.0.zip`. No game, Unity, Harmony, or BeaverBuddies DLLs are redistributed. The game DLLs are used only as build references. <!-- latest -->

Without the game, `dotnet run --project tests/Checks.csproj -c Release` runs only the checks that need no game files (pin, refresh-planning, planting-tool and pin-file logic) and reports the rest as skipped. GitHub Actions runs them for pull requests and pushes to `main`.

## License

MIT. See [LICENSE](LICENSE).
