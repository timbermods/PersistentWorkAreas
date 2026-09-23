# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Timberborn players who lay out farms and forests, mostly in single player; some play co-op through the BeaverBuddies
Stability Fork. They know the moment the mod fixes: you select a farmhouse or forester to see its working area, then
pick the planting tool, and the outline vanishes, so you paint crops or trees by memory or keep re-selecting the
building. Mostly non-technical. They find the mod through the Timbermods catalog (https://timbermods.github.io/), a
friend or a forum post, want to know in one look what it does and that it's safe for their saves, install it without
fuss, and pin their first building. Returning players come back to update, or to troubleshoot (no panel, no outline,
pins gone, stopped working after a game update).

## Product Purpose

The website for **Persistent Work Areas** (https://github.com/timbermods/PersistentWorkAreas), a Timberborn mod that
keeps a building's working-area outline on screen after you deselect it, so it stays visible while you plant, build
and plan.

In plain words:
- Select a building with a working area (farmhouse, forester, lumberjack flag, gatherer, or any other building that
  shows a terrain or road-spill range when selected) and check **Keep working area visible**. Deselect it: the outline
  stays. Pin as many as you like; overlapping areas merge into one outline, separate ones all stay.
- Pick a crop or tree in the planting tools and the working areas of every building that can plant it appear on their
  own: farmhouses for crops, aquatic farmhouses for aquatic crops, foresters for trees and bushes, including ones still
  under construction. They go away when you leave the tool. These are not pins.
- **Clear pinned areas (N)** at the top right removes every pin (only pins, not the planting-tool outlines); an
  optional key binding does the same. Uncheck one building to drop just its pin. Deleting a pinned building drops its
  pin.
- Pins are remembered for each settlement in a small local file, never in the save.

Success, in order:
1. The visitor understands it at a glance: the outline the game shows only while a building is selected now stays, and
   the planting tools show their planters. Display only; saves untouched; no other mods needed.
2. They install it right: the `PersistentWorkAreas-v1.1.0.zip` under **Assets** (not `-source.zip`, not **Source
   code**), extracted so that `Documents\Timberborn\Mods\PersistentWorkAreas\version-1.1\manifest.json` sits beside
   `PersistentWorkAreas.dll` (not nested one level too deep), then enabled in the mod manager.
3. They use it: pin the first building, find **Clear pinned areas (N)**, optionally bind the clear key.
4. When something's wrong, they find the fix on Troubleshooting or send a useful report: mod and game version,
   BeaverBuddies or not, other mods, what happened, and the `[PersistentWorkAreas]` lines from `Player.log`.

## Positioning

The base game draws a building's working area only while that building is selected. Persistent Work Areas keeps the
ones you choose on screen after you deselect, and outlines the planters for whatever you're planting. It draws with the
game's own outline renderer and the same navigation queries the game uses for a selected building, so a pinned outline
is the game's own outline, kept. It changes nothing in the game: no Harmony patches, no simulation changes, no save
writes, no network traffic, and no required mods. It pins navigation-based working areas only: not district road
coloring, not effect-radius overlays that aren't navigation-based, not building-placement ghosts, and not Builder's
Huts (the outline a Builder's Hut shows when selected is the base game's own, not this mod's).

Among Timbermods it is the small, standalone one: it needs neither Harmony nor Mod Settings nor BeaverBuddies, unlike
MixedStorage (Harmony) or the BeaverBuddies Stability Fork and MultiColony (co-op mods). No comparison with other
community range-overlay mods is on record; don't invent one.

## Operating Context

- **Current release: v1.1.0**, a stable **Latest** release on GitHub (2026-09-22), assets
  `PersistentWorkAreas-v1.1.0.zip` and `PersistentWorkAreas-v1.1.0-source.zip`. Earlier: 1.0.0 (first stable),
  0.1.3, 0.1.2, and 0.1.1/0.1.0 (pre-release previews). Downloads are GitHub Releases only (no Workshop listing is on
  record), so linking to `/releases/latest` is correct here.
- Mod name in the mod manager: **Persistent Work Areas** (manifest id `kyler.persistentworkareas`). Folder
  `PersistentWorkAreas\version-1.1\`, which also holds `KeyBindingGroups\`, `KeyBindings\` and `Localizations\`;
  `README.md` and `VALIDATION.md` sit beside `version-1.1` in the zip.
- **Game version:** Timberborn **1.1.2.4** or a compatible 1.1 build (manifest `MinimumGameVersion` 1.1.2.4). Built
  and tested on 1.1.2.4; a later game version may change the internal renderer the mod borrows. If it does, pinning
  disables itself for that map, the panel says *Working-area pinning is unavailable. Check the game log.* and the log
  says `Outline disabled for this map:`; nothing else in the game is affected. Built and tested on Windows.
- **Requirements:** none. No Harmony, no Mod Settings, no BeaverBuddies (`RequiredMods` is empty).
- **Co-op (the org rule, from the latest docs pass):** works the same in single player and co-op. Designed for
  compatibility with the **BeaverBuddies Stability Fork** but doesn't depend on any BeaverBuddies build; keep that
  installation as it is. For co-op, every player installs the same version of the mod and runs the same game version;
  that is the tested setup. Pins are always local to each player, and the mod sends nothing over the network, so a
  player without it shouldn't cause a desync, but running it on only one computer has not been played. The Stability
  Fork's join check compares only the game and BeaverBuddies builds, so this mod doesn't affect joining. Not tested
  with the original BeaverBuddies or with BeaverBuddies MultiColony. (The v1.1.0 release notes say players "don't need
  to update together"; the README and site, written after, use the same-version rule above. Follow the README.)
- **Where pins live:** `Documents\Timberborn\PersistentWorkAreas\Pins.txt` (next to the `Saves` folder), per
  settlement, the 100 most recently loaded or changed settlements. Loading any save of a settlement brings back its
  latest pins; a different settlement starts with none. Deleting the file forgets every pin. A co-op guest's pins
  usually don't come back after a resync (the guest's settlement is named after the host's save); the host's should.
- **What players meet in game** (exact text, from `Localizations/enUS_PersistentWorkAreas.csv`):
  - In the building's panel: a **WORKING AREA** section with a **Keep working area visible** row (the whole row is the
    button) and an **ON** / **OFF** badge. Hints: *Click to pin this outline while planting or building.* /
    *Pinned after deselection. Clear all using the top-right button.* Tooltip: *Click to keep this building's
    working-area outline visible after deselection. Click again to unpin.*
  - Top right, while anything is pinned: **Clear pinned areas (N)**, tooltip *Remove all your pinned working-area
    outlines. You can also assign a shortcut in Settings > Key bindings > Persistent Work Areas.*
  - Key binding: group **Persistent Work Areas**, action **Clear all pinned working areas**, unbound by default; it
    ignores presses with extra modifier keys.
  - No Mod Settings page. UI text is English only; a translation is one more CSV in `version-1.1/Localizations/`
    named for the game's language code (for example `deDE_PersistentWorkAreas.csv`), keeping `{0}` in the clear-button
    text.
- **Reporting:** GitHub issues (https://github.com/timbermods/PersistentWorkAreas/issues/new) with the mod version
  (mod manager or the `[PersistentWorkAreas] <version> loaded.` log line), the Timberborn version, BeaverBuddies or not
  and other mods, what happened, and every `[PersistentWorkAreas]` line from
  `%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log` (`Player-prev.log` after a crash). The log lines
  a player can meet: `loaded.`, `Outline disabled for this map:`, `Renderer cleanup failed:`,
  `Pinned areas not restored from` / `Pinned areas not saved to` (with the `Pins.txt` path).

## Capabilities and Constraints

- **Stack and hosting:** plain static HTML/CSS/JS in `docs/` on `main`, no build step: `index.html` (Overview),
  `install.html`, `troubleshooting.html`, `faq.html`, `404.html`, `.nojekyll`, and `assets/` (`style.css`, `site.js`,
  `demo.js`, `favicon.svg`). GitHub Pages serves `main:/docs` (legacy build) at
  https://timbermods.github.io/PersistentWorkAreas/, so a change is live once it's merged to `main`; there is no deploy
  script and no `gh-pages` branch. It must stay fast, light and mobile-friendly. It's one of the timbermods sites
  (MixedStorage, the BeaverBuddies Stability Fork and MultiColony are siblings; the org catalog is
  https://timbermods.github.io/).
- **Site-test / CI contracts: none.** The only workflow, `.github/workflows/tests.yml`, runs the mod's game-free checks
  (`dotnet run --project tests/Checks.csproj -c Release`) on pushes to `main` and pull requests; `tests/Program.cs`
  checks nothing in `docs/` or the README. `build.ps1` copies `README.md` and `VALIDATION.md` into the release zip, so
  those two files ship to players; the site doesn't. Nothing automated catches a broken site, so check it by hand.
- **Conventions the current site relies on (keep them working, no test enforces them):**
  - `assets/site.js` fetches `repos/timbermods/PersistentWorkAreas/releases/latest` (cached in `sessionStorage` under
    `pwa-latest-release`), fills every `[data-latest-version]` (unhiding it), points every `[data-latest-zip]` link at
    the asset matching `^PersistentWorkAreas-v[\d.]+\.zip$`, and fills `[data-zip-name]` / unhides `[data-zip-line]`.
    Every download link's `href` already points at `/releases/latest`, so the page works without the script.
  - Elements that start `hidden` (the version badge, the zip-name line, the demo's clear button) must stay hidden
    under the stylesheet until script reveals them.
  - `404.html` loads its assets and links by absolute `/PersistentWorkAreas/` paths.
  - `demo.js` is a simplified illustration, not the mod's renderer; it and the install-page panel sample use the
    in-game strings (WORKING AREA, Keep working area visible, ON/OFF, Clear pinned areas (N), the hints). Keep them
    captioned as illustrations and in step with the CSV.
  - The FAQ opens a `details.q` from the URL hash, and other pages link to FAQ and troubleshooting ids (`#builder`,
    `#report`, `#log`, `#unavailable`, `#performance`, `#update`, `#install`, `#how`); keep those anchors or update
    every link.
  - Footer standard on every page: "maintained by Timbermods", the catalog link "More mods from Timbermods", and the
    disclaimer "An unofficial community mod for Timberborn. Not affiliated with or endorsed by Mechanistry."
- **Shared files:** this site has no `release.js` today (`site.js` is its own). If the redesign adopts the shared
  timbermods `assets/release.js` (as the MixedStorage site does), it's a byte-for-byte copy shared across timbermods
  sites: replace it, never edit it.
- **Terminology:** working area (not range, radius or zone, except "road-spill range" / "terrain range" as the kinds
  that qualify); pin / pinned / unpin; **Keep working area visible**; **WORKING AREA**; **Clear pinned areas (N)**;
  **Clear all pinned working areas**; planting tools; farmhouses, aquatic farmhouses, foresters, lumberjack flags,
  gatherers, Builder's Huts; settlement (what pins are remembered per); resync; BeaverBuddies Stability Fork. "Display
  only", "local", "never written into the save".
- **Honest status:** 1.1.0 passes 142 automated checks (77 game-free, which also run in CI; 65 against the installed
  game's assemblies and blueprints). 1.0.0's behavior ("pinning, clearing and outline display worked as expected") was played
  in game on 1.1.2.4, single player and in a two-player Stability Fork co-op session with the mod on both computers.
  **1.1.0's additions have not been played in game yet:** the planting-tool outlines, pins remembered in `Pins.txt`,
  the faster per-pin refresh and the translatable text. Also not played: the mod on only one co-op computer, the
  original BeaverBuddies or MultiColony, frame rate in a very large colony, every other mod. Say so plainly, next to
  the feature, without alarm; the current site doesn't yet say it anywhere.
- **Describe the mod as it is now.** Version history belongs in the changelog: the README's "New in 1.1.0" / "Earlier
  versions" and the GitHub release notes (there is no separate CHANGELOG file). The current site has leftovers to
  remove: "since version 0.1.2" (FAQ `#builder`, troubleshooting `#builder-hut`) and "since version 1.1.0" / "since
  1.1.0" (FAQ `#persist`, `#planting`, `#language`, install `#update`). Keep only the upgrade facts players need:
  close the game, replace the whole `PersistentWorkAreas` folder (copying only the DLL shows raw text keys), pins are
  kept because they live outside the mod folder.
- **Sources of truth:** `README.md`, `VALIDATION.md`, the localization CSV and the release notes. Where the site and
  the README disagree, flag it; don't guess.

## Brand Commitments

- **Voice:** a fellow player explaining a small, useful mod. Clear, exact, friendly, never hype. Short sentences; the
  exact names players see in game.
- **Native fidelity is the claim:** the outline is the game's own; the site's illustrations say they're illustrations
  ("Interactive illustration, not a game screenshot.") until real screenshots exist.
- **No official Timberborn logos or key art.** The game's own item and building icons are allowed where used (none are
  on this site yet); credit them as Timberborn's. The brand mark (the outlined-area glyph in `favicon.svg`, repeated
  in the header) is the project's own.
- **License:** MIT, copyright Timbermods, for the code, docs and site. Game and Unity DLLs are build references only,
  never redistributed.
- **Unofficial community mod, not affiliated with or endorsed by Mechanistry.** Maintained by Timbermods.

## Evidence on Hand

- `docs/assets/favicon.svg`: the project's brand mark (also inline in every page header). It is the only image the
  repo has.
- The interactive pinning illustration on the home page (`docs/assets/demo.js`: a simplified map with a Farmhouse and a
  Forester, select → pin → deselect → clear) and a static panel sample on the install page, both captioned as
  illustrations.
- Exact in-game text in `packaging/PersistentWorkAreas/version-1.1/Localizations/enUS_PersistentWorkAreas.csv`.
- `VALIDATION.md`: what the 142 checks cover and the 10-step release playtest checklist.
- **Does not exist and must not be faked:** any in-game screenshot or clip (a pinned outline, the WORKING AREA panel,
  the Clear pinned areas button, the planting-tool outlines), an og/social image, a mod icon or thumbnail, Workshop
  page, download counts, player quotes or testimonials, press. Leave marked slots for the maintainer's own shots.

## Product Principles

1. **Show the outline staying.** The whole mod is one before/after: selected → deselected, outline still there. Every
   page's first job is to make that moment obvious.
2. **Nothing touched but the screen.** Display only, saves untouched, no patches, no network, no required mods: say it
   once, precisely, and back it with the specifics.
3. **Install right the first time.** The right zip, the exact folder layout, not nested: impossible to miss.
4. **The game's own words.** Name the panel, row, button and key binding exactly as the game shows them.
5. **Honest about what's been played.** 1.0.0's pinning was played; 1.1.0's additions are checked, not yet played.
   Say which, next to the feature.
