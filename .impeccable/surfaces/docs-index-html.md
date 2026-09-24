---
version: 1
slug: "docs-index-html"
primary_target: "docs/index.html"
related_targets: ["docs/install.html","docs/troubleshooting.html","docs/faq.html","docs/404.html"]
---

# Surface brief: Persistent Work Areas site (docs/index.html with install, troubleshooting, faq, 404)

Scope: docs/. Home: Persuade; guides: Read. Audience: players who lay out farms and forests and lose the working-area outline the moment they pick a planting tool; mostly single player, some Stability Fork co-op. Action: understand it (pin the outline so it stays after deselecting; the planting tools outline their planters on their own; display only; saves untouched; no other mods), install it right (the zip under Assets, not -source.zip or Source code; not nested), use it (Keep working area visible, Clear pinned areas (N), optional key), report problems usefully. Proof: the interactive demo, the exact in-game strings, 142 automated checks, 1.0's pinning played in a two-player Stability Fork session. Constraints: site.js release lookup (data-latest-version/-zip, data-zip-name/-line, pwa-latest-release session cache) and its no-JS fallbacks; hidden-until-script elements stay hidden (global [hidden] rule); demo.js DOM contract (#demo, svg.map, .ui-panel, .ui-empty, .ui-toggle, .who, .state, .hint, [data-clear], [data-deselect], [data-status]) and in-game strings; every id kept; 404 absolute /PersistentWorkAreas/ paths; honest status (1.0's pinning played; 1.1's additions checked, not played; one-computer co-op not played; original BeaverBuddies and Timber Together not tested); no version history on player pages; no Timberborn art. Decisions delegated to the agent.

## Direction contract

THESIS: Persistent Work Areas is a sheet of tracing paper taped over the site plan: the outline you drew stays on the vellum while you keep working on the plan underneath, and lifting the sheet changes nothing below. It refuses the template the site wears around its one authored part (eyebrow, pill chips, a 6-card grid with numbered badges, a numbered steps band, a spec table, a centered CTA).

OWN-WORLD: A drafting table. Light: the site plan in warm plan-paper (#ecebe3) with a faint surveyed grid (a produced raster, not CSS stripes), sheets of translucent vellum (#f7f6ef at ~88% over the plan, a produced fibre texture) held down with drafting tape (a produced raster), graphite ink (#262b2a), and the mod's own outline cream-gold (#ebb859 / #f5e6a3) as the pinned outline, the pin green (#a8e075, darkened to #4f7a2a for text on light) for "pinned". Dark: the same table under a lamp, plan #17221f (the mod's panel teal), vellum #1f2c29, ink #e6e8e2. The in-game panel (the demo) keeps the mod's real panel colours in both themes. Chivo (display, self-hosted, OFL) for sheet titles and headings in the manner of drawing lettering; body system-ui; paths and log lines in mono.

STORY: One look: pin a building's working area and it stays after you deselect; pick a crop or tree and its planters are outlined; clear them all in one click; display only, saves untouched, nothing else to install. Then: the three moves in game (pin, plant, clear) as taped sheets; what it outlines and what it doesn't; where pins live; co-op in plain words; what is tested and what isn't; install.

FIRST VIEWPORT: Left: "Persistent Work Areas", the pitch "Keep the outline. Keep planting.", a two-sentence lead, three facts (the game's own outline, kept; the planting tools show their planters; display only, saves untouched, no other mods), Download + Install guide, a short status note. Right: the demo as a taped sheet over the plan, already showing one pinned, deselected outline at rest, with a planting-tool switch that outlines the farmhouse's area on its own; the in-game panel replica beside the map.

FORM: Tracing Overlay, candidate 3 of 7 (seed b5deea89). Challengers weighed: the silk canopy (competitive: overlapping translucent layers fit merged areas; kept its discipline, overlaps labelled beyond colour), risograph, pickling calendar, darkroom, civic prospectus, flyer wall (declined). Raises taken: overlap is shown as one merged outline and said in words (canopy); no more than three inks on any sheet (riso); every layer has one job and can be lifted without changing the plan (flyer wall, without its history). Signature interaction: the demo (pin, deselect, planting switch, clear), keyboard and touch first-class.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
