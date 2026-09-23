---
target_identity: "file:C:\\Users\\Kyler\\code\\PersistentWorkAreas-site-redesign\\file:C:\\Users\\Kyler\\code\\PersistentWorkAreas-site-redesign\\docs"
timestamp: 2026-09-23T20-16-21Z
slug: de-persistentworkareas-site-redesign-docs-61373bb8
---
---
target: Persistent Work Areas site
total_score: 29
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 5
---
# Critique: Persistent Work Areas site (docs/)
Method: dual-agent (A design review, B detector + browser + audit)
Tests: (1) PARTIAL - desktop headline/lede/demo carry it but the demo starts empty (no outline at rest), planting-tool outlines never shown, control names missing; phone FAIL (134px header, demo at y=742); (2) FAIL - no played/not-played status anywhere, six version-history leftovers (faq #builder/#persist/#planting/#language, troubleshooting #builder-hut, install #update), "safe in co-op" overclaims; (3) MOSTLY PASS - Assets vs source.zip, tree, nesting warning, fallbacks verified with JS off / 403 / mocked release; weak: the `loaded.` line not named, "restart if it asks", "choose Replace".
Heuristics 29/40 (Good). Cognitive load moderate (home repeats display-only/co-op/performance; 14-entry troubleshooting TOC).
Priority: [P1] no honest status; [P1] version history on player pages; [P1] phone first screen / sticky header covers anchor targets (134px vs 84px scroll-padding); [P1] light-theme focus ring 1.58:1; [P1] hero shows no outline at rest; [P2] co-op/version overclaims; [P2] demo focus lost after Deselect/Clear, focus = selected style, 28px buildings, 8px labels on phones, empty ground doesn't deselect, no Esc; [P2] install confirm step; [P2] template sameness (numbered cards, chips, spec table, centered CTA); [P2] 404 without header/footer; [P3] gold GitHub pill competes, inline styles, WORKING AREA casing.
Identity: the palette is the mod's own (in-game pin panel: #1a2e2b, gold #ebb859, green #a8e075, outline cream #f5e6a3) and the demo is authored; everything around it is template.
Perf: <13 KB gzipped per page, no fonts, CLS 0; site.js (own release lookup, sessionStorage cache) falls back cleanly on 403.
