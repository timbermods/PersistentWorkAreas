# Validation — Persistent Work Areas 0.1.3

## Completed

- Compiled for `netstandard2.1` against the installed Timberborn 1.1.2.4 assemblies: zero errors and zero warnings.
- 37 checks passed: reference-identity pins, duplicate pin/unpin, independent pins, replacement/deletion behavior, global clear, new-map isolation, native renderer constructors/methods/cleanup fields, delegate binding to internal renderer methods, lifecycle/navigation/event interfaces, no simulation persistence interfaces, no Harmony/BeaverBuddies dependencies, package/key-binding consistency, and presence of the Builder's Hut marker component used to exclude it from pinning.
- Compared the display path with the installed game's `BuildingRangeDrawer`, `BoundsNavRangeDrawer`, and navigation query calls. The mod uses those same range queries and its own outline-renderer instance.
- Reviewed Preview 6's `IO/BuildCompatibility.cs`: its handshake identifies Timberborn and the BeaverBuddies/TimberNet binaries. This mod changes none of them.
- The display service only implements frame-update, input, load, and navigation-notification interfaces. Pin state is per game-scoped service instance. There are no Harmony patches, replay events, simulation ticks, random calls, or save writes.
- Renderer geometry refreshes on pin, selection, construction-mode, height-visibility and navigation changes, with navigation refreshes capped at five per second. Empty pin sets do no range queries or drawing. All pins share a combined mesh. Clearing releases the renderer's owned meshes and cloned materials.

## Not established by these checks

These are compiled API and logic checks, not Unity rendering tests or an actual co-op session. They do not establish the visual placement of controls, frame-rate impact in a large colony, or compatibility with every other mod. Internal renderer reflection is deliberately isolated in `NativeOutline.cs` and checked against the installed assemblies.

## First in-game playtest

1. With Preview 6 enabled, load a colony, select a farm and enable its checkbox. Deselect and paint crops: the working-area outline should remain.
2. Pin a forester and paint trees. Pin a second nearby building; verify overlapping areas merge and separate areas both remain visible.
3. Select an unrelated building, then clear all from the top-right button. No pinned outlines should remain after deselecting. Reselect formerly pinned buildings and verify their checkboxes are off.
4. Assign the clear command in key bindings and verify it clears while a planting tool is active. Do not assign a shortcut already used by a tool.
5. Modify a path or terrain, switch visible levels and construction mode, and verify the outline updates. Pause the simulation and repeat selection/clear.
6. Delete a pinned building. Its outline and pin count should disappear. Exit/load another map or perform a co-op resync: pins should be empty.
7. Join with two Preview 6 clients. Pin different buildings on each computer and clear on only one. Only that player's pins should change; normal building/planting actions should remain synchronized.

## References

- [Timberborn official modding tools](https://github.com/mechanistry/timberborn-modding)
- [BeaverBuddies Stability Fork](https://github.com/timbermods/BeaverBuddies-Stability-Fork)

The exact API decisions were verified against the user's installed game assemblies and local Preview 6 source, rather than assuming the latest online API matches their installation.
