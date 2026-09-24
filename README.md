# Persistent Work Areas

A Timberborn mod that keeps a building's working-area outline on screen after you deselect it. Plant, build and plan with the outline in view.

**Website:** <https://timbermods.github.io/PersistentWorkAreas/>: overview, install guide, troubleshooting and FAQ.

## Install

1. Close Timberborn.
2. Download `PersistentWorkAreas-v1.1.0.zip` from **Assets** on the [latest release](https://github.com/timbermods/PersistentWorkAreas/releases/latest). Don't use the `-source.zip` or the **Source code** archives. <!-- latest -->
3. Extract it into your Timberborn `Mods` folder, normally `Documents\Timberborn\Mods`. You should get `Mods\PersistentWorkAreas\version-1.1\manifest.json`, with `PersistentWorkAreas.dll` beside it.
4. Start Timberborn and enable **Persistent Work Areas** in the mod manager. Restart if asked.

You need Timberborn **1.1.2.4** or a compatible 1.1 build. No other mods are needed: not Harmony, Mod Settings or BeaverBuddies.

**To update,** close the game and replace the whole `PersistentWorkAreas` folder, not just the DLL. Your pins are kept.

## Pin a working area

1. Select a farmhouse, forester, lumberjack flag, gatherer or another building that shows a working area.
2. In its **WORKING AREA** section, click **Keep working area visible** so it reads **ON**.
3. Deselect the building. The outline stays while you plant, build or use other tools.

Pin as many buildings as you like. Overlapping areas merge into one outline. Deleting a pinned building removes its pin.

## Planting tools

Pick a crop or tree in the planting tools, and every building that can plant it is outlined. That's farmhouses for crops, aquatic farmhouses for aquatic crops, and foresters for trees and bushes. The outlines go when you leave the tool; they aren't pins.

## Clear pins

- **One pin:** select the building and click **Keep working area visible** again, so it reads **OFF**.
- **All pins:** click **Clear pinned areas (N)** at the top right. It shows while anything is pinned.
- **With a key:** Settings → Key bindings → Persistent Work Areas → **Clear all pinned working areas**. It starts unbound.

## Pins are remembered

Each settlement's pins are kept on your computer in `Documents\Timberborn\PersistentWorkAreas\Pins.txt`, never in the save. They come back when you load any save of that settlement. To forget every pin, delete that file.

## Co-op

The mod works the same in single-player and co-op. For co-op, every player installs the same version of the mod and runs the same game version.

Pins are local: each player keeps their own, and nothing is sent over the network. A co-op guest's pins usually don't come back after a resync, because the guest's settlement is named after the host's save.

## Good to know

- **Display only.** It changes nothing in the game and never writes to your save, so you can remove it at any time.
- **If a game update breaks the outline,** pinning turns off for that map and the panel says so. Nothing else is affected.
- **Working areas only.** It doesn't pin district road coloring, effect-radius overlays, building-placement ghosts or Builder's Huts. The outline a selected Builder's Hut shows is the game's own.
- **English text.** A translation is one more CSV in `version-1.1\Localizations`, with the keys of `enUS_PersistentWorkAreas.csv` and named for the game's language code (for example `deDE_PersistentWorkAreas.csv`). Keep `{0}` in the clear-button text: it becomes the pin count.

Something not working? See [Troubleshooting](https://timbermods.github.io/PersistentWorkAreas/troubleshooting.html), or [open an issue](https://github.com/timbermods/PersistentWorkAreas/issues/new) with your game log.

## Status

The mod is **stable**.

**Tested**

- Every feature, played extensively in game on 1.1.2.4: pinning and clearing, the pinned outlines, the planting-tool outlines, pins remembered between sessions, outlines keeping up with terrain and path changes, and the panel text.
- Pinning, clearing and the outlines in a two-player Stability Fork co-op session, with the mod on both computers.
- 142 automated checks: 77 that need no game (they also run on every change), and 65 against the installed game's own assemblies and blueprints.

**Not played yet**

- The mod on only one computer in a co-op game.
- The original BeaverBuddies, and Timber Together.
- Frame rate in a very large colony, and other mods alongside it.

What changed in each version is in the [release notes](https://github.com/timbermods/PersistentWorkAreas/releases). To build from source or run the checks, see [DEVELOPING.md](DEVELOPING.md).

## License

MIT. See [LICENSE](LICENSE). Maintained by [Timbermods](https://github.com/timbermods).

An unofficial community mod for Timberborn. Not affiliated with or endorsed by Mechanistry.
