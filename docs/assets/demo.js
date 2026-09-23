// Interactive illustration of pinning. It is a simplified drawing, not the mod's renderer:
// the real mod uses the game's own outline renderer and navigation queries. It starts with the Forester pinned and
// nothing selected, so the kept outline is on screen at rest. The planting switch outlines every farmhouse, as the
// game's planting tools do with the mod installed; those outlines are not pins.
(function () {
  var root = document.getElementById("demo");
  if (!root) return;

  var COLS = 24, ROWS = 16, C = 20;
  var SVGNS = "http://www.w3.org/2000/svg";
  var ROAD_ROW = 8;

  var buildings = [
    { id: "farm", name: "Farmhouse", x: 5, y: 5, w: 2, h: 2, color: "#c9a25a", access: [6, 8], r: 4.6 },
    { id: "forester", name: "Forester", x: 16, y: 10, w: 2, h: 2, color: "#5f9a57", access: [17, 8], r: 4.6 }
  ];
  buildings.forEach(function (b) {
    b.cells = [];
    for (var cy = 0; cy < ROWS; cy++) {
      for (var cx = 0; cx < COLS; cx++) {
        var dx = cx + 0.5 - (b.access[0] + 0.5), dy = cy + 0.5 - (b.access[1] + 0.5);
        if (dx * dx + dy * dy <= b.r * b.r) b.cells.push(cx + "," + cy);
      }
    }
  });

  var svg = root.querySelector("svg.map");
  var panel = root.querySelector(".ui-panel");
  var empty = root.querySelector(".ui-empty");
  var toggle = root.querySelector(".ui-toggle");
  var who = root.querySelector(".who");
  var stateEl = root.querySelector(".state");
  var hint = root.querySelector(".hint");
  var clearBtn = root.querySelector("[data-clear]");
  var deselectBtn = root.querySelector("[data-deselect]");
  var status = root.querySelector("[data-status]");
  var plantBtn = root.querySelector("[data-plant]");
  var cap = document.getElementById("demo-cap");
  var restCap = cap ? cap.textContent : "";
  var planting = false;

  var selected = null;
  var pinned = { forester: true };

  function el(name, attrs, parent) {
    var n = document.createElementNS(SVGNS, name);
    for (var k in attrs) n.setAttribute(k, attrs[k]);
    if (parent) parent.appendChild(n);
    return n;
  }

  // Static layer: ground grid, road.
  var ground = el("g", {}, svg);
  el("rect", { class: "ground-hit", x: 0, y: 0, width: COLS * C, height: ROWS * C }, ground);
  for (var i = 0; i <= COLS; i++) el("line", { class: "grid-line", x1: i * C, y1: 0, x2: i * C, y2: ROWS * C }, ground);
  for (var j = 0; j <= ROWS; j++) el("line", { class: "grid-line", x1: 0, y1: j * C, x2: COLS * C, y2: j * C }, ground);
  el("rect", { class: "road", x: 0, y: ROAD_ROW * C, width: COLS * C, height: C }, ground);
  el("line", { class: "road-mark", x1: 0, y1: ROAD_ROW * C + C / 2, x2: COLS * C, y2: ROAD_ROW * C + C / 2 }, ground);
  ground.addEventListener("click", function () { select(null); });

  var areaFill = el("path", { class: "area-fill" }, svg);
  var areaEdge = el("path", { class: "area-edge" }, svg);
  areaFill.style.pointerEvents = areaEdge.style.pointerEvents = "none";

  var nodes = {};
  buildings.forEach(function (b) {
    var g = el("g", { class: "bld", tabindex: 0, role: "button", "aria-pressed": "false", "aria-label": "Select " + b.name }, svg);
    el("rect", { class: "body", x: b.x * C + 2, y: b.y * C + 2, width: b.w * C - 4, height: b.h * C - 4, rx: 4, fill: b.color }, g);
    var t = el("text", { class: "label", x: b.x * C + b.w * C / 2, y: b.y * C - 5, "text-anchor": "middle" }, g);
    t.textContent = b.name;
    var dot = el("circle", { class: "pin-dot", cx: (b.x + b.w) * C - 2, cy: b.y * C + 4, r: 6 }, g);
    dot.style.display = "none";
    g.addEventListener("click", function (e) { e.stopPropagation(); select(b.id); });
    g.addEventListener("keydown", function (e) {
      if (e.key === "Enter" || e.key === " ") { e.preventDefault(); select(b.id); }
    });
    nodes[b.id] = { g: g, dot: dot };
  });

  function byId(id) { return buildings.filter(function (b) { return b.id === id; })[0]; }
  function pinCount() { return Object.keys(pinned).length; }

  function drawArea() {
    var set = {};
    buildings.forEach(function (b) {
      if (pinned[b.id] || selected === b.id || (planting && b.id === "farm")) b.cells.forEach(function (c) { set[c] = true; });
    });
    var fill = "", edge = "";
    Object.keys(set).forEach(function (key) {
      var p = key.split(","), x = +p[0], y = +p[1], px = x * C, py = y * C;
      fill += "M" + px + " " + py + "h" + C + "v" + C + "h-" + C + "z";
      if (!set[x + "," + (y - 1)]) edge += "M" + px + " " + py + "h" + C;
      if (!set[x + "," + (y + 1)]) edge += "M" + px + " " + (py + C) + "h" + C;
      if (!set[(x - 1) + "," + y]) edge += "M" + px + " " + py + "v" + C;
      if (!set[(x + 1) + "," + y]) edge += "M" + (px + C) + " " + py + "v" + C;
    });
    areaFill.setAttribute("d", fill);
    areaEdge.setAttribute("d", edge);
  }

  function render(message) {
    drawArea();
    buildings.forEach(function (b) {
      nodes[b.id].g.setAttribute("aria-pressed", String(selected === b.id));
      nodes[b.id].dot.style.display = pinned[b.id] ? "" : "none";
    });
    var n = pinCount();
    clearBtn.textContent = "Clear pinned areas (" + n + ")";
    clearBtn.hidden = n === 0;
    deselectBtn.disabled = selected === null;
    var b = selected && byId(selected);
    empty.hidden = !!b;
    panel.hidden = !b;
    if (b) {
      var on = !!pinned[b.id];
      panel.classList.toggle("pinned", on);
      who.textContent = b.name;
      toggle.setAttribute("aria-pressed", String(on));
      stateEl.textContent = on ? "ON" : "OFF";
      hint.textContent = on
        ? "Pinned after deselection. Clear all using the top-right button."
        : "Click to pin this outline while planting or building.";
    }
    if (message) status.textContent = message;
  }

  function select(id) {
    selected = id;
    var b = id && byId(id);
    render(b ? b.name + " selected. Its working area is shown." : "Nothing selected. Only pinned areas remain visible.");
  }

  toggle.addEventListener("click", function () {
    if (!selected) return;
    var b = byId(selected);
    if (pinned[selected]) delete pinned[selected]; else pinned[selected] = true;
    render(b.name + (pinned[selected] ? " pinned." : " unpinned."));
  });
  clearBtn.addEventListener("click", function () {
    pinned = {}; render("All pins cleared.");
    // the button hides itself, so keep focus on the map
    nodes[buildings[0].id].g.focus();
  });
  deselectBtn.addEventListener("click", function () {
    var was = selected; select(null);
    if (was) nodes[was].g.focus();
  });
  if (plantBtn) {
    plantBtn.hidden = false;
    plantBtn.addEventListener("click", function () {
      planting = !planting;
      plantBtn.setAttribute("aria-pressed", String(planting));
      if (cap) cap.textContent = planting ? "Planting tool open: the Farmhouse is outlined for you. Not a pin." : restCap;
      render(planting ? "Planting tool open: the Farmhouse's working area is outlined." : "Planting tool closed.");
    });
  }
  root.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && selected) { var was = selected; select(null); nodes[was].g.focus(); }
  });

  render();
})();
