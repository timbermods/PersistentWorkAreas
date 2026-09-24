# CLAUDE.md

Persistent Work Areas (timbermods/PersistentWorkAreas): a Timberborn 1.1 mod (C#, `netstandard2.1`) that keeps a
building's working-area outline on screen after deselecting it and outlines the planters in the planting tools. Source
in `source/`, game assets and localization in `packaging/PersistentWorkAreas/version-1.1/`, checks in `tests/`
(`Program.cs`). No Harmony, no required mods. Changes land on `main` by PR → merge.

- Game-free checks (what CI runs, `.github/workflows/tests.yml`): `dotnet run --project tests/Checks.csproj -c Release`
  → "77 checks passed; 65 checks that need the game were skipped".
- Full build + all 142 checks + release zip (reads the installed game's DLLs; never launches it):
  `.\build.ps1 -OutputDir "$env:TEMP\pwa-dist"`. Keep that path short (a long one hits the Windows path limit). The
  zip ships `README.md` and `VALIDATION.md`; the site does not ship.

## Standing rules

- Never launch or drive Timberborn, and never touch installed mods or saves. The maintainer (Kyler) playtests himself.
- Commit on a branch and open a PR. Kyler has said to merge PRs automatically: merge, then check the page live.
- Assume fresh games: no old-save compatibility notes. Player-facing text never says which version added what.
- Sources of truth for facts: `README.md`, `VALIDATION.md`, the localization CSV
  (`packaging/PersistentWorkAreas/version-1.1/Localizations/enUS_PersistentWorkAreas.csv`), the release notes. Where
  the site and README disagree, the README wins (for example co-op: every player installs the same version).

## Website

- **Where:** `docs/`: `index.html` (Overview), `install.html`, `troubleshooting.html`, `faq.html`, `404.html`,
  `.nojekyll`, `assets/` (`style.css`, `site.js`, `demo.js`, `favicon.svg`, `fonts/`, `textures/`). Plain static
  HTML/CSS/JS, no build step. Live at https://timbermods.github.io/PersistentWorkAreas/.
- **Published:** GitHub Pages serves `main:/docs` (legacy build), so merging to main publishes; a build takes about a
  minute. No deploy script, no `gh-pages` branch.
- **Latest releases update themselves:** when a release becomes GitHub's Latest, `.github/workflows/latest-release.yml`
  (the shared timbermods workflow) appends the standard footer to its notes, sets the site's
  `data-release="version|tag|asset-name"` fallback text and the README lines ending in `<!-- latest -->` to the new
  version, runs the site checks and commits to main. Pre-releases change nothing. Descriptions, status lists and FAQs
  stay manual (the checklist below). Dry run: Actions → Latest release → Run workflow.
- **Look:** "The Tracing Overlay". A drafting table: warm plan paper with a surveyed grid, and every block of reading
  taped onto it as a translucent vellum sheet, the way a pinned outline stays on tracing paper while you work below.
  The look is fixed: updates extend it and never restyle it.
- **Design records (read before any site change):** `PRODUCT.md` (facts, voice, site contracts), `DESIGN.md` (the
  visual system and named rules; the source of truth for the look), `.impeccable/surfaces/docs-index-html.md`
  (direction contract), `.impeccable/design.json` (tokens, snippets), `.impeccable/critique/` (pre-redesign critique).

### Design rules (from DESIGN.md; keep them)

- **Real Panel**: the in-game panel replica (the demo, the install-page panel sample) keeps the mod's real panel
  colours in both themes (`.demo` vars: bg #1a2e2b, frame #0f1d1b, line #577560, cream #fff2c9, gold #ebb859, green
  #a8e075, hint #c4d6c4; map #223a2c; more shades in DESIGN.md). Never re-theme it to the page.
- **Three Inks**: no sheet carries more than graphite (with muted and rule tints), outline gold and pin green.
- **Gold Means Outlined**: outline gold marks outlined, pinned or current (drawn outlines, legend swatches, current nav
  underline, callout top rule, open FAQ rule). Never a general accent. Pin green = pinned/confirmed (checks, pin dots,
  the Tested heading rule). Links are survey teal in light, pale gold in dark.
- **Lettering**: Chivo for titles, terms, buttons and title-block labels only, never running text; headings
  `text-wrap: balance`, -0.015em.
- **Vellum, Not Paint**: a sheet's background is the vellum raster alone (about 74–77% alpha) so the grid reads
  through; opaque vellum only for code blocks, callouts and the skip link.
- **Tape Holds Every Sheet**: every `.sheet` is also `.taped`: 76×24px tape at both top corners, 12px above the edge,
  18px in, -4deg left / 3deg right.
- **Produced Surface**: plan, vellum and tape are rasters, never CSS gradients or stripes.
- Tokens (`:root` in `docs/assets/style.css`; dark in `@media (prefers-color-scheme: dark) { :root {…} }`),
  light / dark: plan #ecebe3 / #17221f, vellum #f7f6ef / #1f2c29, ink #262b2a / #e6e8e2, muted #545b58 / #a9b5ae,
  rule #cfccbd / #34443f, link #1f5c55 / #f0d27a (hover #123f3a / #f8e6b0), focus #1f5c55 / #f5e6a3, outline #b88a1e /
  #ebb859, outline-ink #7a5a0e / #ebb859, pin #4f7a2a / #a8e075. Selection #f5e6a3 in both.
- Fonts: Chivo 700 and 800 only, self-hosted in `docs/assets/fonts/` (`chivo-latin-{700,800}-normal.woff2`,
  `OFL-Chivo.txt`); 800 is preloaded on every page. Body is the system-ui stack, paths and log lines the mono stack.
  No other webfonts, and nothing from a CDN at runtime.
- Textures: `docs/assets/textures/` `plan-light.webp`, `plan-dark.webp`, `vellum-light.png`, `vellum-dark.png`,
  `tape.png`, made by `docs/assets/textures/make_textures.py` (numpy + Pillow, fixed seeds; run it from that folder).
  Re-run the script rather than editing images, then `embed-prompt` each new raster (`--scan docs`: 0 missing).
- Themes: follow the system (`prefers-color-scheme`); no toggle, no storage key; `theme-color` #ecebe3 / #17221f.
- Phones: no horizontal scroll at 390px, and tap targets ≥ 44px. Breakpoints 960 (hero stacks, demo moves up after the
  lead), 860 (grids and title block to one column), 720 (nav wraps, demo sheet bleeds to the edges).
- Motion: buttons lift 1px (0.15s, cubic-bezier(.2,.8,.2,1)); the FAQ chevron turns 180deg (0.2s). The signature is
  the interactive pin demo, not animation. Everything is still under `prefers-reduced-motion`.
- Structure: 2px ink rules open sections (`.ruled`), 1px pencil rules separate rows; corners 2–3px (4–5px in the panel).
- Don't: drop shadows or glows (the panel's 4px inset stripe is the only one), CSS-faked paper/grid/tape, eyebrows or
  kickers, pill chips, card grids with numbered badges, a numbered steps band on the home page, a centered CTA band,
  gold as decoration, glyph or icon-font icons (drawn SVG or CSS masks only), Chivo body text, Timberborn art or
  screenshots, a sticky header, new accent colours.
- New components: build them from the tokens and components above (sheet + taped, legend, notes sheet, title block,
  checked lists `.checks` / `.checks.open`, `.callout`, `.issue`, `details.q`, `.steps`), match the neighbouring
  sections, and add them to DESIGN.md.

### Content rules

- Describe the mod as it is now. No "New in", "added in", "since version" on player pages; version history lives in
  the README's "New in …" / "Earlier versions" and the GitHub release notes (no CHANGELOG file). Upgrade facts players
  need (replace the whole folder; pins are kept) are the exception.
- Status matches README/VALIDATION exactly. Every current feature has been played extensively in game (1.1.0 on
  1.1.2.4), so none carries the marker today. A new feature not yet played gets
  `<span class="unplayed">Checked, not played in game yet.</span>` after it, and loses it once played. Never invent numbers, reviews,
  screenshots, download counts. No og image exists; don't fake one.
- Keep the footer standard on every page: "maintained by Timbermods", "More mods from Timbermods"
  (https://timbermods.github.io/), and "An unofficial community mod for Timberborn. Not affiliated with or endorsed by
  Mechanistry." MIT, copyright Timbermods.
- Terminology: working area (not range/radius/zone, except "terrain or road-spill range"); pin / pinned / unpin;
  **Keep working area visible**; **WORKING AREA**; ON/OFF; **Clear pinned areas (N)**; **Clear all pinned working
  areas**; planting tools; settlement; resync; BeaverBuddies Stability Fork; "display only", "local".
- In-game strings on the site (demo, panel sample, FAQ, troubleshooting) are copied from the localization CSV exactly.
- `demo.js` is a captioned illustration ("Interactive illustration, not a game screenshot.") that must keep matching
  the mod: starts with the Forester pinned and nothing selected; select → toggle pin (ON/OFF, hint text) → deselect;
  overlapping pins merge into one outline; the planting switch (`[data-plant]`) outlines the Farmhouse (not a pin) and
  swaps `#demo-cap`; "Clear pinned areas (N)" shows while pinned and clears only pins; Esc or empty ground deselects and
  returns focus. Keep its DOM hooks (`#demo`, `svg.map`, `.ui-panel`, `.ui-empty`, `.ui-toggle`, `.who`, `.state`,
  `.hint`, `[data-clear]`, `[data-deselect]`, `[data-status]`, `[data-plant]`, `#demo-cap`). Pins are local to each
  player and display only; never show them syncing or saving.
- `assets/site.js` is this site's own (no shared `release.js` here): it fetches `releases/latest`, caches it in
  `sessionStorage` `pwa-latest-release`, fills `[data-latest-version]`, points `[data-latest-zip]` at the asset matching
  `^PersistentWorkAreas-v[\d.]+\.zip$`, fills `[data-zip-name]` and unhides `[data-zip-line]`. Download links keep
  `href=".../releases/latest"` so the page works without it; hidden-until-script elements stay `hidden`.
- Keep every anchor other pages link to (FAQ `#builder` `#how` …, troubleshooting `#report` `#log` `#unavailable`
  `#performance`, install `#update` `#install`). `404.html` uses absolute `/PersistentWorkAreas/` paths.

### Update the website for a new release

When asked to "update the website for the latest release, consistent with the design":
1. Read the release and the docs: `gh release list -R timbermods/PersistentWorkAreas -L 5`,
   `gh release view <tag> -R timbermods/PersistentWorkAreas`, README, VALIDATION.md, the localization CSV,
   `packaging/PersistentWorkAreas/version-1.1/manifest.json`. List every player-facing change.
2. Update every place the site states a changed fact. Find them with
   `grep -rnE "1\.1\.2\.4|version-1\.1|142|\b77\b|\b65\b|unplayed|Not played|playtested" docs`. There is no version
   number in the static HTML (the badge is filled by site.js), and no `data-release-pinned`. The places:
   - `index.html`: `<meta name="description">` and `og:description`; hero `.facts` (three) and `.status-note`
     ("Stable." + played summary); `#features` move sheets; `#how` legend (outlines / doesn't); `#details`
     notes sheet; `#compat` title block (Game version, Other mods, Co-op, Saves); `#status` Tested / Not played yet
     (check counts, game version); `.cta` install steps.
   - `install.html`: `#requirements` (game version), zip name pattern, the folder tree (`version-1.1` and its
     contents), `#verify` panel sample and log line, `#keybinding`, `#coop` callout, `#update`, `#uninstall`.
   - `troubleshooting.html`: `#not-listed` paths, `#unavailable` message, `#after-update` game version, `#coop`,
     `#performance`, `#pins-vanish`, `#log` log lines, `#report`.
   - `faq.html`: `#version`, `#multiplayer`, `#which`, `#persist`, `#planting`, `#language` (CSV path), `#perf`.
   - `demo.js` and the install panel sample if in-game strings or behaviour changed; `site.js` regex if the zip name
     changes; if the mod folder becomes `version-1.2`, every `version-1.1`.
   - PRODUCT.md's Operating Context and honest-status paragraph.
3. Put new content into the existing components: a feature → a move sheet or a notes-sheet note; a limit → a legend
   row; compatibility → a title-block cell; evidence → the status sheets; a problem → an `.issue` article plus a TOC
   link; a question → a `details.q` in its `.faq-group`. Remove claims that stopped being true. Don't restyle anything.
4. Test: there is no site test (CI runs only the dotnet checks). Run
   `dotnet run --project tests/Checks.csproj -c Release` (77 pass, 65 skipped), then check the site by hand.
5. Preview: `python -m http.server 8786 -d docs` (in the background), open http://localhost:8786/. Capture light, dark
   and a 390px phone. With the personal `impeccable-site-flow` skill:
   `python <skill>/scripts/capsite.py http://localhost:8786/ <out> "" install.html troubleshooting.html faq.html`
   (must print overflow 0 four times); otherwise the Browser pane in both schemes at desktop and mobile. Click through
   the demo (select, pin, planting switch, clear, Esc). Stop the server afterwards.
6. Optional: `"$(ls -d ~/.claude/plugins/cache/impeccable/impeccable/*/skills/impeccable | tail -1)/scripts/impeccable" detect --json docs`
   (exits 2 when it has findings; parse from the first `[`). Today: 64 findings, all by design. Known false
   positives: `side-tab` on `.ui-panel` (the mod's real 4px inset stripe) and on the `.ui-toggle .box i` tick;
   `border-accent-on-rounded`/`side-tab` on `.callout`'s 3px gold top rule; `cramped-padding` on `section.ruled` and
   `.page-head` (clamp padding); `design-system-color` for panel/map shades and the demo's building colours;
   `design-system-font-size` for sizes off the frontmatter ramp; 404's `rgb(0,0,0)` (absolute CSS path). Anything else
   is new and real. There is no `.impeccable/config.json` yet.
7. If the look changed (a new component or layout), update DESIGN.md and `.impeccable/design.json`.
8. Update the README (and VALIDATION.md) if they repeat the facts.
9. Ship: branch → commit → push → `gh pr create`. After Kyler says merge: `gh pr merge <n> --merge` (that publishes),
   then verify: `gh api repos/timbermods/PersistentWorkAreas/pages/builds/latest -q .status` is `built`, and
   `curl -s https://timbermods.github.io/PersistentWorkAreas/ | grep -c "<a changed string>"` finds the change.

### Open to-dos

- The direction contract says vellum at ~88%; as shipped (and in DESIGN.md) it is 74–77%. DESIGN.md wins.

### Full redesign

A new look goes through the whole Impeccable flow (init → critique → audit → direction → build → finish review →
DESIGN.md). With the personal skill: "use the impeccable-site-flow skill to redesign this site".
