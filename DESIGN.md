---
name: Persistent Work Areas
description: A sheet of tracing paper taped over the site plan; the outline you drew stays on the vellum while you keep working underneath.
colors:
  plan: "#ecebe3"
  plan-dark: "#17221f"
  vellum: "#f7f6ef"
  vellum-dark: "#1f2c29"
  ink: "#262b2a"
  ink-dark: "#e6e8e2"
  muted: "#545b58"
  muted-dark: "#a9b5ae"
  rule: "#cfccbd"
  rule-dark: "#34443f"
  link: "#1f5c55"
  link-hover: "#123f3a"
  link-dark: "#f0d27a"
  link-hover-dark: "#f8e6b0"
  outline: "#b88a1e"
  outline-ink: "#7a5a0e"
  outline-dark: "#ebb859"
  outline-cream: "#f5e6a3"
  pin: "#4f7a2a"
  pin-dark: "#a8e075"
  panel-frame: "#0f1d1b"
  panel-bg: "#1a2e2b"
  panel-line: "#577560"
  panel-cream: "#fff2c9"
  panel-hint: "#c4d6c4"
  panel-button: "#294f45"
  panel-button-edge: "#85ab78"
  panel-check-ink: "#17301f"
  map-ground: "#223a2c"
typography:
  display:
    fontFamily: "Chivo, Arial Narrow, sans-serif"
    fontSize: "clamp(2.6rem, 6vw, 4.4rem)"
    fontWeight: 800
    lineHeight: 1.06
    letterSpacing: "-0.015em"
  headline:
    fontFamily: "Chivo, Arial Narrow, sans-serif"
    fontSize: "clamp(1.8rem, 3.6vw, 2.6rem)"
    fontWeight: 800
    lineHeight: 1.06
    letterSpacing: "-0.015em"
  pitch:
    fontFamily: "Chivo, Arial Narrow, sans-serif"
    fontSize: "clamp(1.4rem, 2.6vw, 1.9rem)"
    fontWeight: 800
    lineHeight: 1.1
    letterSpacing: "-0.01em"
  title:
    fontFamily: "Chivo, Arial Narrow, sans-serif"
    fontSize: "1.28rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.005em"
  body:
    fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 400
    lineHeight: 1.62
  label:
    fontFamily: "Chivo, Arial Narrow, sans-serif"
    fontSize: "0.82rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "0.06em"
  mono:
    fontFamily: "ui-monospace, Cascadia Code, SF Mono, Consolas, Liberation Mono, monospace"
    fontSize: "0.9em"
    fontWeight: 400
    lineHeight: 1.5
rounded:
  hairline: "2px"
  button: "3px"
  panel: "4px"
  panel-card: "5px"
  step: "50%"
spacing:
  gutter: "clamp(16px, 4vw, 32px)"
  wrap: "1160px"
  section: "clamp(56px, 8vw, 100px)"
  sheet-gap: "30px 24px"
  sheet-pad: "26px 22px 20px"
  row: "16px"
components:
  button-primary:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.plan}"
    typography: "{typography.title}"
    rounded: "{rounded.button}"
    padding: "0 22px"
    height: "50px"
  button-primary-hover:
    backgroundColor: "{colors.link-hover}"
    textColor: "{colors.plan}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.button}"
    padding: "0 22px"
    height: "50px"
  button-small:
    padding: "0 14px"
    height: "44px"
  sheet:
    backgroundColor: "{colors.vellum}"
    padding: "{spacing.sheet-pad}"
  callout:
    backgroundColor: "{colors.vellum}"
    textColor: "{colors.ink}"
    rounded: "{rounded.hairline}"
    padding: "18px 20px"
  nav-link:
    textColor: "{colors.muted}"
    padding: "0 12px"
    height: "44px"
  nav-link-current:
    textColor: "{colors.ink}"
  panel-toggle:
    backgroundColor: "#26382f"
    textColor: "{colors.panel-cream}"
    rounded: "{rounded.panel}"
    padding: "8px 10px"
    height: "46px"
  panel-toggle-pinned:
    backgroundColor: "#304d38"
    textColor: "{colors.panel-cream}"
  panel-button:
    backgroundColor: "{colors.panel-button}"
    textColor: "{colors.panel-cream}"
    rounded: "{rounded.panel}"
    padding: "6px 12px"
    height: "40px"
---

# Design System: Persistent Work Areas

## Overview

**Creative North Star: "The Tracing Overlay"**

The site is a drafting table. The page itself is the site plan: warm plan paper with a faint surveyed grid, a produced raster rather than CSS stripes. Everything worth reading sits on sheets of translucent vellum taped over that plan, so the grid reads faintly through each sheet the way a drawing shows through tracing paper. The ink is graphite; the only colours with meaning are the mod's own outline gold (what is outlined or pinned) and its pin green (what is pinned or confirmed). Lifting a sheet changes nothing below, and every layer has one job.

Under a lamp (dark, following the system; there is no toggle) the same table turns to the mod's panel teal, the vellum to a deeper teal, and the ink to pale grey; gold and green brighten to the mod's in-game values. The one authored exhibit, the interactive pin demo, is a replica of the in-game panel and keeps the mod's real panel colours in both themes, so it reads as the game on top of the drawing rather than as part of the drawing.

Density is a reference document's: ruled rows, bordered title-block cells, short notes, measured lines (56–62ch in leads, 760px prose). Drawing lettering (Chivo 800) for titles, system text for reading, mono for paths and log lines. The site refuses the template it would otherwise wear: eyebrows, pill chips, a card grid with numbered badges, a numbered steps band on the home page, a spec table, a centered CTA band.

**Key Characteristics:**
- Plan paper with a produced surveyed grid (24px fine, 96px firm lines) as the page ground.
- Translucent vellum sheets (raster only, about 74–77% alpha) with drafting tape at both top corners.
- Graphite ink plus at most two meaningful inks: outline gold and pin green.
- Drawn outlines (SVG, gold stroke over a 14% gold fill) instead of icons or illustrations.
- Horizontal rules as structure: 2px ink rules open sections, 1px rules separate rows.
- The in-game panel replica keeps the mod's real colours in both themes.
- No shadows, one small lift on buttons, everything still under reduced motion.

## Colors

Warm plan paper and graphite by day, panel teal and pale ink under the lamp, with the mod's own outline gold and pin green as the only signalling colours.

### Primary
- **Outline Gold** (light `outline`, dark `outline-dark`): the mod's outline. Drawn outlines on the move sheets, legend swatches, the current-page underline in the nav, the top rule of callouts, the top rule of an open FAQ answer. In light it is darkened to a bronze gold so a 2.4px stroke holds on plan paper; in dark it is the in-game value.
- **Outline Ink** (`outline-ink`): gold deep enough for text on light paper; used only for the pitch line. In dark the pitch uses Outline Gold.
- **Outline Cream** (`outline-cream`): the pale gold of the in-game area fill and edge; the dark theme's focus ring and the text-selection highlight in both themes.

### Secondary
- **Pin Green** (light `pin`, dark `pin-dark`): "pinned" and "confirmed". Pin dots in drawings, check marks in tested lists, the underline of the Tested heading. The light value is the in-game green darkened for text contrast on paper.

### Tertiary
- **Survey Teal** (light `link`, `link-hover`): links, the light focus ring, and the primary button's hover fill. In dark, links take a pale gold (`link-dark`, `link-hover-dark`) because teal would sink into the teal plan.

### Neutral
- **Plan Paper** (`plan`, `plan-dark`): the page ground, always carried by the plan raster; the colour is the fallback and the primary button's text.
- **Vellum** (`vellum`, `vellum-dark`): the sheet tone. On sheets it arrives only through the translucent raster; as a solid fill it backs code blocks, callouts and the skip link, where dense text needs an opaque ground.
- **Graphite Ink** (`ink`, `ink-dark`): text, 2px section rules, the primary button fill, title-block borders, drawn buildings.
- **Soft Graphite** (`muted`, `muted-dark`): secondary copy, section-head leads, the struck legend swatch, the "not played" marker and the open-items heading rule.
- **Pencil Rule** (`rule`, `rule-dark`): 1px row dividers, sheet borders, drawn grid lines.
- **Band** (`rgba(38,43,42,.045)` light, `rgba(230,232,226,.04)` dark): inline-code wash and secondary-button hover; a tint of ink, not a colour.

### In-game panel (the demo only)
- **Panel Teal** (`panel-bg`), **Panel Frame** (`panel-frame`), **Panel Line** (`panel-line`), **Panel Cream** (`panel-cream`), **Panel Hint** (`panel-hint`), **Panel Button** (`panel-button`, edge `panel-button-edge`), **Check Ink** (`panel-check-ink`), **Map Ground** (`map-ground`), with the in-game gold and green (`outline-dark`, `pin-dark`). Farmhouse and Forester bodies on the map are `#c9a25a` and `#5f9a57`.

### Named Rules
**The Real Panel Rule.** The in-game panel replica keeps the mod's real panel colours in both themes. Never re-theme it to the page.

**The Three Inks Rule.** No sheet carries more than three inks: graphite (with its muted and rule tints), outline gold, pin green. Anything else on a sheet is the panel replica.

**The Gold Means Outlined Rule.** Outline gold marks what is outlined, pinned or current. It is not decoration and not a general accent.

## Typography

**Display Font:** Chivo 700/800, self-hosted (OFL), with Arial Narrow and sans-serif fallback
**Body Font:** system-ui stack
**Label/Mono Font:** ui-monospace stack for paths, file names and log lines

**Character:** Heavy, slightly condensed drawing lettering over plain, fast system text. The titles read like the lettering on a drawing; the body stays out of the way.

### Hierarchy
- **Display** (800, clamp(2.6rem, 6vw, 4.4rem), 1.06): the home page title only. Guide page titles use clamp(2.4rem, 5.4vw, 3.6rem).
- **Headline** (800, clamp(1.8rem, 3.6vw, 2.6rem), 1.06): section titles. In guide prose clamp(1.6rem, 3vw, 2.1rem) with a 2px ink rule above.
- **Pitch** (800, clamp(1.4rem, 2.6vw, 1.9rem), 1.1): the one-line pitch under the home title, in Outline Ink.
- **Title** (700, 1.28rem, 1.2): sheet and card titles; also buttons (700, 1.05rem), definition terms (700, 1.1rem), table heads.
- **Body** (400, 1.0625rem, 1.62): reading text; leads 1.1rem at 56–62ch; prose capped at 760px. Bold is 650.
- **Label** (700, 0.82rem, 0.06em, uppercase): title-block cell labels only, in Soft Graphite, as a drawing's title block letters them.
- **Mono** (0.9em): inline code, code blocks (1.5 line height), file paths.

### Named Rules
**The Lettering Rule.** Chivo is for titles, terms and buttons, never for running text. Headings balance their lines and track slightly tight (-0.015em).

## Layout

A single 1160px column with a fluid gutter (clamp(16px, 4vw, 32px)). Sections are separated by 2px ink rules and open with clamp(56px, 8vw, 100px) of padding; follow-on sections that belong to the one before drop their top padding. Section heads sit left, never centered, capped at 760px.

The home hero is a two-column grid (0.9fr text, 1.1fr demo sheet) with the demo sheet on the right. The three moves are a three-column grid of taped sheets (gaps 30px 24px). The legend, the notes sheet, the title block and the status pair are two-column grids inside or across sheets. Guides use a 220px sticky table of contents beside 760px prose; FAQ prose is capped at 52em. Troubleshooting entries are ruled blocks with a 120px term column.

Breakpoints: at 960px the hero stacks and the demo sheet moves up to follow the lead (title, pitch, lead, then the demo, then facts, buttons and status), so the pinned outline is on the first screen; the moves become one column (max 560px). At 860px every two-column grid, the title block and the guide layout go to one column and the TOC becomes a static two-column list. At 720px the header nav wraps under the brand, the demo sheet bleeds to the viewport edges, and map labels grow to 16px.

The header is not sticky: a 66px bar with a 2px ink rule under it. Tap targets are at least 40–44px (nav links, TOC rows, footer links, buttons 44–50px).

## Elevation & Depth

Flat. There are no drop shadows anywhere. Depth is the drafting table's: the plan raster below, translucent vellum sheets over it (the grid shows through), and drafting tape strips that hold each sheet down. Rules and borders, not shadows, separate things. The only shadow-like device is the in-game panel's 4px inset left stripe (gold, green when pinned), copied from the mod.

### Named Rules
**The Vellum, Not Paint Rule.** A sheet's background is the vellum raster alone (about 74–77% alpha), never an opaque colour, so the survey grid reads through. Opaque vellum is reserved for code blocks, callouts and the skip link.

**The Tape Holds Every Sheet Rule.** Every sheet is taped at both top corners: 76 by 24px strips of the produced tape raster, 12px above the edge, 18px in from each side, turned -4deg on the left and 3deg on the right. A sheet without tape is not a sheet.

**The Produced Surface Rule.** Plan, vellum and tape are rasters made by `docs/assets/textures/make_textures.py` (fixed seeds, no source images, no generative model), with provenance beside them. Never fake them with CSS gradients or stripes.

## Shapes

Square drafting geometry. Corners are nearly sharp: 2px on code and focus rings, 3px on buttons and swatches, 4–5px only inside the in-game panel replica, which keeps the game's softer corners. Circles appear only as the install guide's step numbers (a 34px ink ring) and pin dots. Borders do the structural work: 1px pencil rules on sheets, 2px ink rules across sections, a 2px ink frame with 1px inner cell lines on the title block. Drawn outlines use round line joins; the legend's "doesn't" swatch is a dashed box struck through at -24deg.

## Components

### Buttons
Solid and plain, like a stamp.
- **Shape:** barely rounded (3px), 2px ink border, 50px tall (44px small).
- **Primary:** graphite fill with plan-paper text in Chivo 700; an optional 18px drawn download arrow.
- **Hover / Focus:** fill and border turn to the deep link colour and lift 1px (0.15s, cubic-bezier(.2,.8,.2,1)). Focus is the global 3px ring.
- **Secondary:** transparent with ink text and border; hover adds the band tint.

### Sheets
The one surface everything is drawn on.
- **Corner Style:** square.
- **Background:** the vellum raster only.
- **Shadow Strategy:** none; tape and translucency (see Elevation & Depth).
- **Border:** 1px pencil rule.
- **Internal Padding:** about 26px 22px 20px; the demo sheet 22px 16px 16px.
- **Variants:** move sheet (drawing, title, muted text), legend sheet, notes sheet (dashed rules between notes), title-block sheet, status sheets (the Tested heading underlined in pin green, Not played yet in soft graphite).

### Title Block
Compatibility as a drawing's title block: a 2px ink frame of two-column cells split by 1px ink lines, each with an uppercase tracked label and plain answer.

### Legend
Two columns, "What it outlines" and "What it doesn't". Each row is a 34 by 22px swatch and a line: the outline swatch is a 2.5px gold border over an 18% gold fill; the struck swatch is a dashed soft-graphite box with a diagonal strike.

### Checked Lists
Lists marked with drawn masks, never glyphs: a pin-green check for done, a muted dash for "doesn't", a dashed muted circle for open.

### Not-Played Marker
"Checked, not played in game yet." sits on its own line after each unplayed feature, 0.9rem italic in Soft Graphite. It follows every feature that has not been played in game, and never appears on one that has.

### Callouts and Code
Opaque vellum boxes with a 1px pencil border; callouts add a 3px gold top rule and a Chivo title line. Code blocks wrap rather than scroll where they can.

### Navigation
A non-sticky 66px bar: the brand mark (the mod's panel square with a gold outline and green pin) and name in Chivo 800 on the left, text links on the right in 600 weight, soft graphite at rest, ink on hover, the current page underlined 2px in outline gold. At 720px the links wrap under the brand.

### FAQ Questions
Ruled `details` rows at least 56px tall with a drawn chevron that turns 180deg (0.2s); an open question's top rule becomes 2px gold. Expand/collapse-all buttons are small secondary buttons.

### The Pin Demo (signature)
A replica of the in-game panel and a simplified map in the mod's real colours, framed in the taped demo sheet. It starts with the Forester pinned and nothing selected. Selecting a building shows the Working Area panel with the "Keep working area visible" toggle (gold border, OFF chip; green box, check and chip when pinned, with the inset stripe turning green). The planting switch adds a separate Farmhouse outline and changes the visible caption. "Clear pinned areas (N)" appears while anything is pinned. Esc or a click on empty ground deselects and returns focus. Focus on a building is a dashed cream stroke; selection is a solid gold stroke. Keyboard and touch are first-class; status changes are announced politely.

## Do's and Don'ts

### Do:
- **Do** put every block of reading content that stands apart on a taped vellum sheet, tape at both top corners.
- **Do** let the plan grid read through sheets: vellum raster only, about 74–77% alpha.
- **Do** draw outlines in SVG with the gold stroke (2.4px) over a 14% gold fill, on a faint pencil grid, when a feature needs a picture.
- **Do** keep the in-game panel replica in the mod's real colours in both themes.
- **Do** draw focus dashed in cream inside the demo and solid gold for selection, so the two never look alike.
- **Do** mark each unplayed feature with "Checked, not played in game yet." in the not-played style.
- **Do** separate sections with 2px ink rules and rows with 1px pencil rules.
- **Do** follow the system theme; there is no toggle.

### Don't:
- **Don't** add drop shadows, glows or offset shadows; depth is translucency and tape.
- **Don't** fake paper, grid or tape with CSS gradients or stripes; use the produced rasters with provenance.
- **Don't** use eyebrows or kickers, pill chips, card grids with numbered badges, a numbered steps band on the home page, or a centered CTA band.
- **Don't** use outline gold as a generic accent; it means outlined, pinned or current.
- **Don't** use glyph or icon-font icons; icons are drawn SVG or masks.
- **Don't** set running text in Chivo.
- **Don't** use Timberborn art or screenshots; the demo is labelled an illustration.
- **Don't** make the header sticky.
