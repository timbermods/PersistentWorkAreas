# Validation — Persistent Work Areas 1.0.0

## In-game testing

- Tested in game on Timberborn 1.1.2.4 with the BeaverBuddies Stability Fork, both single-player and in a two-player co-op session with the mod installed on both computers. Pinning, clearing and outline display worked as expected.
- The tested build was 0.1.3. The 1.0.0 code is the same except for the version number and the startup log line, which now reads the version from the assembly.
- Running the mod on only one of the two co-op computers has not been playtested.

## Automated checks

- Compiled for `netstandard2.1` against the installed Timberborn 1.1.2.4 assemblies: zero errors and zero warnings.
- 38 checks passed: reference-identity pins, duplicate pin/unpin, independent pins, replacement/deletion behavior, global clear, new-map isolation, native renderer constructors/methods/cleanup fields, delegate binding to internal renderer methods, lifecycle/navigation/event interfaces, no simulation persistence interfaces, no Harmony/BeaverBuddies dependencies, package/key-binding consistency, manifest/assembly version agreement, and presence of the Builder's Hut marker component used to exclude it from pinning.
- Compared the display path with the installed game's `BuildingRangeDrawer`, `BoundsNavRangeDrawer`, and navigation query calls. The mod uses those same range queries and its own outline-renderer instance.
- Reviewed the BeaverBuddies Stability Fork's `IO/BuildCompatibility.cs`: its handshake identifies Timberborn and the BeaverBuddies/TimberNet binaries. This mod changes none of them.
- The display service only implements frame-update, input, load, and navigation-notification interfaces. Pin state is per game-scoped service instance. There are no Harmony patches, replay events, simulation ticks, random calls, or save writes.
- Renderer geometry refreshes on pin, planting-tool, selection, construction-mode, height-visibility and navigation changes, with navigation refreshes capped at five per second. With no pins and no crop or tree planting tool open, the mod does no range queries or drawing. The planting tool's farmhouses or foresters are found when the tool opens, from the game's workplace registry, and share the pins' cached ranges. All pins share a combined mesh. Clearing releases the renderer's owned meshes and cloned materials.

## Not established by the automated checks

These are compiled API and logic checks, not Unity rendering tests or a co-op session; the in-game testing above covers those. Neither establishes frame-rate impact in a very large colony or compatibility with every other mod. Internal renderer reflection is deliberately isolated in `NativeOutline.cs` and checked against the installed assemblies.

## Release playtest checklist

Re-run this for each release and after each Timberborn update.

1. With the BeaverBuddies Stability Fork enabled, load a colony, select a farm and enable its checkbox. Deselect, paint crops and leave the planting tool: the working-area outline should remain.
2. Pin a forester and paint trees. Pin a second nearby building; verify overlapping areas merge and separate areas both remain visible.
3. Select an unrelated building, then clear all from the top-right button. No pinned outlines should remain after deselecting. Reselect formerly pinned buildings and verify their checkboxes are off.
4. Assign the clear command in key bindings and verify it clears the pins while a planting tool is active; the buildings that tool shows stay until you leave it. Do not assign a shortcut already used by a tool.
5. Modify a path or terrain, switch visible levels and construction mode, and verify the outline updates. Pause the simulation and repeat selection/clear.
6. Delete a pinned building. Its outline and pin count should disappear. Exit/load another map or perform a co-op resync: pins should be empty.
7. Join with two BeaverBuddies Stability Fork clients. Pin different buildings on each computer and clear on only one. Only that player's pins should change; normal building/planting actions should remain synchronized.
8. With a few farmhouses (one still under construction) and foresters placed and none pinned, pick a crop in the Fields tools: every farmhouse's outline should appear and no forester's. Pick a tree or bush: only the foresters'. As Folktails, pick an aquatic crop: only the aquatic farmhouses'. Pin one forester and leave the planting tool: only that forester's outline should remain.

## References

- [Timberborn official modding tools](https://github.com/mechanistry/timberborn-modding)
- [BeaverBuddies Stability Fork](https://github.com/timbermods/BeaverBuddies-Stability-Fork)

The exact API decisions were verified against the installed game assemblies and local BeaverBuddies Stability Fork source, rather than assuming the latest online API matches the installed game.
