# Persistent Work Areas

Keep a building's working-area outline visible after deselecting it, including while painting crops, planting trees, or using other tools.

**Website:** <https://timbermods.github.io/PersistentWorkAreas/> has the feature overview, an install guide, troubleshooting, and an FAQ.

## New in 1.0.0

First stable release, with no behavior changes from 0.1.3. That build has now been tested in game, single-player and in a two-player BeaverBuddies Stability Fork session. The game-log line now reads its version number from the mod itself, and the release zip now uses standard `/` folder separators, so it extracts correctly on macOS and with non-Windows tools.

### Earlier versions

- **0.1.3:** Documentation correction only. The game itself draws a Builder's Hut's range outline while it is selected; that is vanilla behavior, not something this mod adds or changes.
- **0.1.2:** Builder's Huts can no longer be pinned, so they no longer show a "Working area" panel.
- **0.1.1:** The pin control gained a clearly drawn checkbox, an ON/OFF badge, a bordered panel, and hover/keyboard-focus highlighting.
- **0.1.0:** First preview.

## Install

1. Close Timberborn.
2. Extract `PersistentWorkAreas-v1.0.0.zip` into your Timberborn `Mods` folder (normally `Documents\Timberborn\Mods`). The result should be `Mods\PersistentWorkAreas\version-1.1\manifest.json` and `PersistentWorkAreas.dll` beside it.
3. Start Timberborn and enable **Persistent Work Areas** in the mod manager. Restart if prompted.

Requires Timberborn **1.1.2.4** or a compatible 1.1 build. Built and checked against the installed 1.1.2.4 assemblies. Later versions may change the internal renderer API.

No extra dependency is required. Keep your existing BeaverBuddies Stability Fork installation as it is. For co-op, install this mod on both players' computers; that is the tested setup. Pins are always local to each player. The code does not require the other player to install it, but running it on only one computer has not been playtested.

## Use

- Select a farm, forester, lumberjack flag, gatherer, or another building with a terrain/road-spill working range.
- Check **Keep working area visible**.
- Deselect it and use your planting/building tools. The outline remains.
- Pin additional buildings as needed. Overlapping pinned areas merge into a combined outline.
- Click **Clear pinned areas (N)** at the top right to remove every pin, without finding or selecting any building.
- For a keyboard shortcut, assign **Clear all pinned working areas** under **Persistent Work Areas** in the game's key-binding settings. It starts unbound to avoid taking an existing shortcut.
- To remove just one pin, select its building and uncheck the checkbox.

The currently selected building still uses its normal game outline. Clearing pins restores normal selection behavior; deselecting that building then hides its normal outline. Pins reset on map exit, loading, and multiplayer resynchronization. They are not saved. Deleting a pinned building removes its pin.

The mod supports navigation-based working areas. It does not pin district road coloring, every kind of effect-radius overlay, or building-placement ghosts. UI text is currently English.

## Compatibility and validation

Designed for compatibility with the **BeaverBuddies Stability Fork**. The mod does not patch game methods, change simulation or building data, send multiplayer events, or modify saves. It uses the same navigation queries as the game's selected-building visualizer and a separate instance of its outline renderer.

The release build passes 38 automated lifecycle/API checks. The mod has been tested in game on Timberborn 1.1.2.4, both single-player and in a two-player BeaverBuddies Stability Fork session with the mod installed on both computers. See `VALIDATION.md` for what was checked and how.

## Build from source

Install the .NET 8 SDK and have Timberborn installed, then run:

```powershell
.\build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'
```

The script builds the mod, runs checks, and creates `dist\PersistentWorkAreas-v1.0.0.zip`. No game, Unity, Harmony, or BeaverBuddies DLLs are redistributed. The game DLLs are used only as build references.

## License

MIT. See [LICENSE](LICENSE).
