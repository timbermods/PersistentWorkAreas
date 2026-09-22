# Validation — Persistent Work Areas 1.1.0

## In-game testing

- **1.1.0 has not been playtested in game yet.** Its new behavior (planting-tool outlines, pins remembered in a local file, per-pin refresh caching and localized text) is covered only by the automated checks below. Run the release playtest checklist, including steps 8 to 10, to confirm it.
- 1.0.0 was tested in game on Timberborn 1.1.2.4 with the BeaverBuddies Stability Fork, both single-player and in a two-player co-op session with the mod installed on both computers. Pinning, clearing and outline display worked as expected. That tested build was 0.1.3; the 1.0.0 code was the same except for the version number and the startup log line.
- Running the mod on only one of the two co-op computers has not been playtested.

## Automated checks

- Compiled for `netstandard2.1` against the installed Timberborn 1.1.2.4 assemblies: zero errors and zero warnings.
- 142 checks passed. 77 need no game files and also run on GitHub Actions for every pull request: pin-set identity and change tracking, refresh planning against a fake navigation world that counts range queries and outline rebuilds, the planting-tool rule and shown-building bookkeeping, and the pin-file format, escaping, 100-settlement cap, restore by entity id and atomic on-disk writes. 65 need the installed game: reference-identity pins, duplicate pin/unpin, independent pins, replacement/deletion behavior, global clear, new-map isolation, native renderer constructors/methods/cleanup fields, delegate binding to internal renderer methods, lifecycle/navigation/event interfaces, no simulation persistence interfaces, no Harmony/BeaverBuddies dependencies, package/key-binding consistency, manifest/assembly version agreement, presence of the Builder's Hut marker component used to exclude it from pinning, the navigation-change bounds against the game's `BoundingBox`, the service's refresh wiring on a live instance, planting-tool events and the planter pairing against the game's own blueprints, localization CSV layout and the absence of hard-coded UI text in the DLL, and pin-file saving, restoring and retrying on a live instance with the game's settlement and entity-registry types.
- Compared the display path with the installed game's `BuildingRangeDrawer`, `BoundsNavRangeDrawer`, and navigation query calls. The mod uses those same range queries and its own outline-renderer instance.
- Reviewed the BeaverBuddies Stability Fork's `BeaverBuddies/IO/BuildCompatibility.cs`: its handshake identifies Timberborn and the BeaverBuddies/TimberNet binaries. This mod changes none of them.
- The display service only implements frame-update, input, load, and navigation-notification interfaces. Pin state is per game-scoped service instance. Each settlement's pins are also kept, by entity id, in a local text file next to the `Saves` folder (`Documents\Timberborn\PersistentWorkAreas\Pins.txt` on Windows). The file is read once after the map loads. The frame update rewrites it after the pins change. It is also rewritten once after a load that finds the settlement below the top of the file, to keep that settlement among the 100 most recent. A failed write is retried every 10 seconds and again on leaving the map, which never erases the file. A file this version cannot read is kept as `Pins.txt.bak` before it is replaced. There are no Harmony patches, replay events, simulation ticks, random calls, or save writes.
- Each pinned or planting-tool building caches its own range. A navigation change re-queries only the buildings whose range it can reach (road-spill ranges are always re-queried), with those refreshes capped at five per second. Selection and visible-level changes redraw from the cache without a query; construction-mode changes re-query every building. With no pins and no planting tool open, nothing is queried or drawn. All outlines share a combined mesh, and the renderer's owned meshes and cloned materials are released once nothing is drawn.

## Not established by the automated checks

These are compiled API and logic checks, not Unity rendering tests or a co-op session. The in-game testing above covers those for the 1.0.0 behavior; for 1.1.0's new behavior, the playtest checklist below still has to be run. Neither establishes frame-rate impact in a very large colony or compatibility with every other mod. Internal renderer reflection is deliberately isolated in `NativeOutline.cs` and checked against the installed assemblies.

## Release playtest checklist

Re-run this for each release and after each Timberborn update.

1. With the BeaverBuddies Stability Fork enabled, load a colony, select a farm and enable its checkbox. Deselect, paint crops and leave the planting tool: the working-area outline should remain.
2. Pin a forester, paint trees and leave the planting tool: the forester's outline should remain. Pin a second nearby building; verify overlapping areas merge and separate areas both remain visible.
3. Select an unrelated building, then clear all from the top-right button. No pinned outlines should remain after deselecting. Reselect formerly pinned buildings and verify their checkboxes are off.
4. Assign the clear command in key bindings and verify it clears the pins while a planting tool is active; the buildings that tool shows stay until you leave it. Do not assign a shortcut already used by a tool.
5. Modify a path or terrain, switch visible levels and construction mode, and verify the outline updates. Pause the simulation and repeat selection/clear.
6. Delete a pinned building. Its outline and pin count should disappear. Load a different settlement: it should start with no pins.
7. Join with two BeaverBuddies Stability Fork clients. Pin different buildings on each computer and clear on only one. Only that player's pins should change; normal building/planting actions should remain synchronized.
8. With a few farmhouses (one still under construction) and foresters placed and none pinned, pick a crop in the Fields tools: every farmhouse's outline should appear and no forester's. Pick a tree or bush: only the foresters'. As Folktails, pick an aquatic crop: only the aquatic farmhouses'. Pin one forester and leave the planting tool: only that forester's outline should remain.
9. Pin two buildings, save, exit to the main menu and load that save: both outlines should come back and the button should read **Clear pinned areas (2)**. Unpin one, save, exit and load again: only the other should come back. Load an older save of the same settlement: the remembered buildings that exist in it should be pinned, with no error in the log. In co-op, pin different buildings on each computer and perform a resync: the host's pins should come back and the guest's should be empty.
10. While still in the map, pin a building: within a second its settlement's line in `Documents\Timberborn\PersistentWorkAreas\Pins.txt` should list one more id. Press **Clear pinned areas**: the line should disappear. Pin a building that is already in your latest save, exit to the desktop without saving, relaunch and load that save: the pin should come back. Load an older save that lacks one of the pinned buildings, then load the newest save again: every pin should come back. `Pins.txt.tmp` should never be left behind.

## References

- [Timberborn official modding tools](https://github.com/mechanistry/timberborn-modding)
- [BeaverBuddies Stability Fork](https://github.com/timbermods/BeaverBuddies-Stability-Fork)

The exact API decisions were verified against the installed game assemblies and local BeaverBuddies Stability Fork source, rather than assuming the latest online API matches the installed game.
