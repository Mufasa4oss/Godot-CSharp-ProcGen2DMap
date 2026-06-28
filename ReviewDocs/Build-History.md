# L1 Build History

A single chronological record of every build chunk for L1 (the first playable economy), oldest →
newest. It absorbs the per-chunk review checklists that used to live as separate dated files in
`ReviewDocs/`. **Going forward, each finished chunk appends a new dated `## ` section here** instead of
adding a new file.

Two docs stay separate on purpose:
- [`minorFixes.md`](minorFixes.md) — the **active** cleanup/perf backlog (not history).
- [`M-Doc/L1-Milestone-Roadmap.md`](M-Doc/L1-Milestone-Roadmap.md) — the **living** milestone plan (the
  source of truth for what's next).

In-code edits to pre-existing files are tagged with searchable markers: `//CHANGES 23-Jun-26//` (M0/M1)
and `//CHANGES 23-Jun-26 tiles//` (tile refactor).

## Index
1. [2026-06-23 — M0 + M1: data model + tick](#2026-06-23--m0--m1-data-model--tick-on)
2. [2026-06-23 — Tile refactor (pre-M2)](#2026-06-23--tile-refactor-pre-m2)
3. [2026-06-23 — Whole-codebase review (backlog)](#2026-06-23--whole-codebase-review)
4. [2026-06-24 — M1.5 fixes & hardening (gate)](#2026-06-24--m15-fixes--hardening-gate)
5. [2026-06-24 — Save/load + Controls hub](#2026-06-24--saveload--controls-hub)
6. [2026-06-25 — M2 production](#2026-06-25--m2-production)
7. [2026-06-25 — Docs detour (consolidation + Documentation/ + roadmap re-scope)](#2026-06-25--docs-detour)
8. [2026-06-25 — M3 agency + economy UI](#2026-06-25--m3-agency--economy-ui)
9. [2026-06-25 — M4 value / prices](#2026-06-25--m4-value--prices)
10. [2026-06-26 — M4.5 clarity + raw/refined split (gate)](#2026-06-26--m45-clarity--rawrefined-split-gate)
11. [2026-06-27 — Build-cost legibility + M5 demand & scarcity](#2026-06-27--build-cost-legibility--m5-demand--scarcity)
12. [2026-06-27 — M6 phase-1: a living, trading multi-nation economy](#2026-06-27--m6-phase-1-a-living-trading-multi-nation-economy)
13. [2026-06-27 — Realistic blended ground + sand beaches & deserts (art overhaul, Option A)](#2026-06-27--realistic-blended-ground--sand-beaches--deserts)
14. [2026-06-27 — L1 economy completion: M6 phases 2–3, M7, M8](#2026-06-27--l1-economy-completion-m6-phases-23-m7-m8)

---

## 2026-06-23 — M0 + M1: data model + tick on

First two rungs: the economy data model and a real tick.

**New economy classes** (`Systems/Economy/`, later moved to `Systems/Core/Economy/` in M1.5):
- `Good { Grain, Ore, Tools }` — the 3 L1 goods (replacing the deleted 7-resource system).
- `BuildingType { Farm, Mine, Workshop }` + `Building.Of()` recipe table: Farm `[]→[Grain]` r2,
  Mine `[]→[Ore]` r2, Workshop `[Ore]→[Tools]` r1. Data only — production logic deferred to M2. Rates
  are placeholders to balance at M7.
- `Province` (M0 shape): `Capital`, `Owner`, `Tiles`, `Pop`, `Stockpile`, `Price`, `Deposits`,
  `Buildings`. `Owner` is `int` (0 = you), single-state for L1.
- `Economy`: inverts `provincesDictionary` (tile→capital) in one pass; seeded each province
  (`Pop=100`, prices 10, a Farm; ore via seeded RNG at `OreDepositChance=0.4` → +Mine). `OnTick()`
  incremented a counter and looped provinces doing nothing (M2 hook). Tunables centralized.

**Removed:** the 7-raw-resource system — `ResourceManager`, `ResourceItem`, and its two JSON converters
(+ `.uid`s). `Systems/Resources/` emptied.

**Edited existing files** (`//CHANGES 23-Jun-26//`): `Main.CreateObjs` builds `Economy` from the noise
seed and passes it to `TickManager`/`MapManager`/`GameContext`; `GameContext` swapped
`resourceManager`+`tilesResources` → `economy`; `TickManager` rewritten to a real **1-second** tick
(`tickInterval` 1/30 → 1.0) firing `OnTick()` + heartbeat; `MapManager` got the `economy` ref for
click-inspect; save/load DTO dropped `capitalsResources` and **fixed a width bug** (`mapSizeY` twice →
`mapSizeX, mapSizeY`).

**Decisions ratified:** 1-second tick; per-province (not per-tile) ore at M0; numbers 40%/Pop100/price10/
rates 2-2-1; `Owner` as `int`; Economy/Province/Building are **plain C# classes, not Nodes**.

**Known debt flagged:** the save/load LOAD path was broken/unbuilt (`initializeLoad()` empty,
`Serialization.Load()` would crash on `user://`); `GameContext` god-object; province economic state not
persisted (rebuilt from seed).

**Verified (user ran it):** boots; `Economy: N provinces…` prints; click prints province data; heartbeat
climbs; map/roads/F5 still work.

---

## 2026-06-23 — Tile refactor (pre-M2)

Moved resources & buildings from **province-bound to tile-bound**, made deposits multi-tile with rate +
reserve, collapsed to a **single world seed**, and added colored deposit dots. Markers:
`//CHANGES 23-Jun-26 tiles//`. (No M-number — prep slotted between M1 and M2 because M2 production reads
tile-bound deposits/buildings and depletes reserves.)

- **`Deposit` (new):** `Good`, `Tiles` (HashSet), `Reserve` (depletes at M2), `Rate`, `Size`.
  `Reserve = Size × ReservePerTile`, `Rate = Size × RatePerTile` — bigger = more rate **and** reserve.
- **`Province` (slimmed):** down to `Capital`, `Owner`, `Tiles`. Removed `Pop`/`Stockpile`/`Price`/
  `Buildings`/`Deposits` (per-province market returns at M3/M4). This is the "nothing province-bound"
  change.
- **`Economy` (rewritten):** `Provinces` (thin), `Deposits` (list), `TileDeposits` (tile→deposit),
  `TileBuildings` (tile→building). Placement = flood-grow 8-neighbour clusters over land, no overlap
  (`used` set); auto-places a Mine on each ore deposit, Farm on each grain deposit (Workshop
  player-placed later). `DeriveSeed()` mixes the world seed → a distinct deposit sub-seed (layout not
  pattern-correlated with terrain, still reproducible). `MyGlobalClass.GetSurroundingTiles` made public
  for reuse. Tunables: `DepositCountTarget=300`, sizes 1–5, `OreShare=0.5`, `ReservePerTile=100`,
  `RatePerTile=1`.
- **`DepositOverlay` (new, `Systems/Map`):** `Node2D` static `_Draw` (built once, no per-frame cost);
  gray dot = ore, neon-yellow = grain, every deposit tile dotted.
- **Single seed:** `Main.worldSeed` set once, pushed to the noise AND `Economy`.
- **Verification prints** added to `Main.CreateObjs`: provinces/land-tile count, `tiles check: OK/MISMATCH`
  (proves the land-tile count), deposits summary.

**Verified:** boots; the prints appear; `tiles check: OK`; dots render; click-inspect shows deposit
good/size/reserve/rate + building + owner.

---

## 2026-06-23 — Whole-codebase review

A worst-first audit of the repo at the post-tile-refactor point. **Most of this was resolved across the
M1.5 and save/load chunks below**; the still-open items live in `minorFixes.md`. Recorded here for the
rationale and the design backlog.

**The "91% land" answer (not a bug):** Perlin at `frequency=0.0022` over a 512 map → ~1 giant blob, so
land/water swings per seed. Optional knobs: raise frequency (~0.01–0.03), raise sea level (`nV > 0.1` in
`Initialize`), or radial falloff for islands.

**HIGH (done in M1.5):** seed `VoronoiDiagram` from `worldSeed` (capitals + colors used unseeded RNG →
non-reproducible province map — blocked save/load); square-map assumptions (`X==Y==512`) made honest
across Initialize/VoronoiDiagram/ColoredProvincesSprite/Camera after the default map went 256×128.

**MEDIUM:** Voronoi brute-force perf (deferred — `minorFixes`); per-click 240k province scan (fixed in
M1.5 — reuse `Economy.Provinces[capital].Tiles`); `Border.cs` + `GameTime.cs` dead/broken (deleted);
`TradeNetworkVisualizer` shows random packets unrelated to real flow (left alone until M5).

**LOW:** magic tile-source IDs → a constants registry (done: `TileId`/`TerrainTiles`/`RoadTiles`);
highlight-restore bug (non-issue until sand tiles exist); `DetermineTileType` dead-duplicate branch
(rewritten to `TileResolver` in M1.5); GameContext god-object (guidance: inject specific collaborators,
not the whole bag); naming sweep (`Initalize`→`Initialize`, namespaces, `Main.Example.Main`→`MstBuilder`)
— done; "no tests" → xUnit suite added in M1.5.

**Fun backlog (design reference — still the guiding star for L1's fun):**
1. **Supply lines = econ AND army** (the differentiator): capacity-limited roads carry both; cutting a
   road starves both. Make this the spine.
2. **Make the economy legible on the map:** tint provinces by price, pulse roads by congestion, flash
   depleted deposits. ~half the fun budget.
3. **Scarcity + finite reserves + chokepoints** = the routing puzzle; spatial monopoly → casus belli.
4. **Every government lever has a real cost** (tax vs growth, build vs reserve) — no free lunch.
5. **Pressure or it goes stale:** PvE shocks on nodes/edges (a dragon on the ore cluster) as win/lose
   pressure.
6. **Bottleneck-driven unlocks** (rail when roads choke, refineries when tools run short).
7. **Scope discipline:** smaller, denser, more-varied map (chokepoints!) beats vast+sparse; find the fun
   in ONE loop (3 deposits, 2 workshops, 1 capacity road, visible prices, 1 shortage) before scaling.

---

## 2026-06-24 — M1.5 fixes & hardening (GATE)

A hardening gate cleared **before** any new feature: implements the annotated codebase-review backlog
plus a real tile registry, the Core/Godot file split, and the first xUnit tests. Plan:
`~/.claude/plans/piped-shimmying-valley.md`.

**Gate-plan intent (the agreed scope going in):** square-map fix (now active at 256×128); seed
VoronoiDiagram from `worldSeed`; drop the per-click 240k scan; centralize tile-source IDs; naming sweep;
add unit tests. Needed-a-decision items: rewrite `Bitmask.DetermineTileType` (proposed: a single lookup
keyed by the 8-bit neighbour mask) and the GameContext god-object (guidance: inject specific
collaborators rather than passing the whole context).

**Done:**
- *Correctness:* square-map fix (swapped loop bounds in `Initialize`; `pixelSizeY=mapSizeY` in
  `VoronoiDiagram`; map-derived sizing in `ColoredProvincesSprite`; `MapSizeY` for vertical terms in
  `Camera`). Determinism: `worldSeed` threaded into `VoronoiDiagram`; all unseeded RNG → seeded
  `System.Random`. Same seed → identical world (verified).
- *Registry/readability:* new `TileId` struct + `TerrainTiles`/`RoadTiles` catalogs (`Systems/Core/Map/`),
  magic source ids removed everywhere. `DetermineTileType` → pure 16-entry cardinal lookup
  `TileResolver` (behavior locked by tests). Naming sweep done. Per-click scan replaced with
  `Economy.Provinces[capital].Tiles`.
- *Core/Godot split:* **`Systems/Core/`** created (plain C#, no Godot Node; `Vector2I` struct only) —
  Economy/*, Prims (dropped unused `GD.Print`), MyGlobalClass, extracted pure `VoronoiAssignment` +
  `TileResolver`. **`Tests/Tests.csproj`** (plain `Microsoft.NET.Sdk`, links only Core + GodotSharp) —
  **35 tests green** (MST, TileResolver all 16 cases, Economy deposits incl. determinism, Voronoi
  assignment). Core compiling outside Godot proves it is Node-free.

**Decisions:** `GameContext` kept as the composition root (not serialized — `GameData` is the DTO). Kept
`Vector2I` in Core rather than introducing a `Coord` struct (localized if swapped later).

**Verified:** build clean; 35 tests pass; ran at 256×128 (`tiles check: OK`, 500 provinces, 300 deposits,
no transposition); determinism re-confirmed with a hardcoded seed (then reverted).

---

## 2026-06-24 — Save/load + Controls hub

A signal-based input hub, reproduce-from-seed save/load (fixing the broken load path), tick→long, and a
camera start-position fix. Plan: `~/.claude/plans/piped-shimmying-valley.md`.

- **Controls hub (`Systems/Controls/Controls.cs`):** a `Node` that is the single source of truth for
  action-name strings and turns discrete input into **Godot signals** (`SaveRequested`, `LoadRequested`,
  `ProvincesToggleRequested`, `MapClickRequested`) — Godot signals (not static C# events) so connections
  auto-drop on the load-time scene reload. `Main` connects save/load; `MapManager` takes it via ctor and
  connects map-click + provinces-toggle (its old `_UnhandledInput` removed); `Camera` reads the shared
  `Controls.*` constants for per-frame pan/zoom.
- **Save/load v1 (reproduce-from-seed):** `Serialization.Load()` now **static** and reads via
  `Godot.FileAccess` so `user://` resolves (was `System.IO`). Load flow: `Main.PendingLoad` (static,
  survives `ReloadCurrentScene`) + deferred reload; `_Ready` regenerates the world from the saved seed
  and restores the tick. **F9 verified by the user.** `GameData` slimmed — `Capitals` dropped
  (regenerated from seed; avoids drift). Empty `initializeLoad()` removed.
- **Save location:** `project.godot` `use_custom_user_dir` → `%APPDATA%\ProceduralMapGenerator\
  savegame.json` (cross-OS via `user://`, Steam-ready).
- **Tick overflow → long:** `currentTick`/`Economy.tickCount`/`OnTick()`/`GameData.Tick` are now `long`.
- **Camera start/pan-delay fix:** `_Ready` starts **dead-center** (from `MapSizeX/Y × TileSize=64`);
  limits + shader size also map-derived. Removed the startup "pan delay."
- **Cleanups/roadmap:** deleted stray `ProceduralMapGenerator.csproj.old.1`; added **M8 — Obfuscation**
  (final) to the roadmap (save-file encryption keeping the seed in a plain header + assembly obfuscation).

**Verified:** build 0/0; 35 tests pass; boots clean (Godot 4.7); `tiles check: OK`; save round-trips
(small JSON, matching seed/size); F9 reload-from-seed resumes.

**Deferred:** persisting hand-drawn roads (~M6); save slots / load menu; map X/Y on a startup menu + move
Godot scripts under `Systems/Godot/`; Voronoi brute-force + Prim's O(V³) perf (only at high province
counts).

---

## 2026-06-25 — M2 production

Extractors now produce goods into province stockpiles and deplete deposit reserves; the accumulated
economy state is persisted. Plan: `~/.claude/plans/lets-get-back-at-tidy-dewdrop.md`.

**Scope decisions (with user):** extractors only (Workshop input→output chain deferred to M6, guarded by
`Inputs.Length`); auto-placed extractors kept (player-placed buildings → M6); persist stockpiles +
reserves in the save (not replay-on-load).

**Done:**
- *Production:* `Province` gained `Stockpile` (`Dictionary<Good,float>`) + `AddGood`. `Economy.TileProvince`
  (tile→province) routes output. `Economy.OnTick()` runs extractor production:
  `amount = min(Deposit.Rate, Reserve)` into the owning province's stockpile, `Reserve -= amount`.
  Deposit governs extractor throughput/reserve; `Building.Rate` reserved for input-consuming buildings.
  Order-independent (extractors have no inputs).
- *Persistence:* `GameData` gained `DepositReserves` (parallel to `Deposits` order) + `Stockpiles`
  (`List<ProvinceStock>`). `ProvinceStock` (Capital + flat Grain/Ore/Tools) lives in **Core** so
  `Economy.ApplySavedState` stays Godot-free/testable; flat per-good fields avoid a JSON dictionary-key
  converter (Capital serializes via the existing `Vector2IConverter`). `ApplySavedState` restores by
  index (count-guarded; `Console.Error` not `GD` to keep Core pure) + by capital. Save side fills the
  fields (`InitSaveAndLoad`); load side calls it after the tick restore in `Main._Ready`.
- *Visibility:* `TickManager` heartbeat prints `tick N | provinces=X | grain=… ore=…` (the alive check).
- *Tests:* `Tests/EconomyProductionTests.cs` (6) — output climbs; reserve depletes + caps; output lands
  in the owning province (2-province routing); same seed → identical stockpiles; `ApplySavedState`
  restores; count-mismatch guard.

**Verified:** `dotnet test` → **41 passed** (35 + 6); `dotnet build` → 0/0. In-engine alive-check (totals
climbing, F5/F9 round-trip) left for a user run in Godot.

**Notes:** `Building.Rate` is unused for extractors by design (drives the Workshop at M6); reserve restore
is index-based (deterministic deposit order), guarded against count drift but not portable across a
placement-algorithm change.

---

## 2026-06-25 — Docs detour

A housekeeping pass between M2 and M3. (1) **Consolidated** the seven per-chunk review docs into this
single `Build-History.md` and deleted the originals; `minorFixes.md` (active backlog) and the roadmap
stay separate. The convention is now "append a dated section here." (2) Wrote **`ReviewDocs/Documentation/`**
— system guides: README, GodotNodeLifecycle (constructor vs `_Ready`, the `_`-callbacks), EventSystem
(Godot signals vs the static `Main.MapTextureCreated` event vs the tick call-loop), Architecture,
WorldgenPipeline, Economy, TickLoop, SaveLoad. (3) **Re-scoped roadmap M3–M7** toward an MVP "one tight
economic-choice loop": agency + UI first (M3), full population sim deferred to a minimal demand sink (M5),
logistics/the differentiator after the choice loop is fun (M6); added per-milestone detail. Docs-only;
build + 41 tests stayed green.

---

## 2026-06-25 — M3 agency + economy UI

First player agency: spend a treasury to build Workshops (Ore→Tools), with a HUD showing the player
nation's stockpile + flows. Plan: `~/.claude/plans/lets-get-back-at-tidy-dewdrop.md`.

**Decisions (with user):** each capital is a **government/nation** (10 — 1 player, 9 passive AI), so
**`Province` was renamed → `Nation`** (`Nations`, `TileNation`, `NationStock`) and M2's per-province
stockpile is kept *as* the per-nation stockpile (no global pool). Player = the **first placed capital**
(`Owner 0`). Workshops go on **any owned land tile** without a building. Workshop sprite built **in code**
from the 60 Blender frames (no sheet, no editor authoring).

**Done:**
- *Rename:* `Province`→`Nation` across the economy/ownership layer (class, `Economy.Nations`/`TileNation`,
  `NationStock`, save DTO, MapManager, tests, docs). Worldgen's geographic "province/Voronoi" wording kept.
- *Model:* `Nation` gained `Treasury` + `IsPlayer`; `Economy` assigns owners by capital order (player = 0),
  caches `PlayerNation`, starts each nation at `StartingTreasury=500`. `Building` gained `Cost` (Workshop
  100).
- *Agency:* `Economy.TryBuildWorkshop(tile)` — validates player-owned + empty + affordable, deducts
  treasury, places the Workshop.
- *Production:* `OnTick` is now **two-pass** (extractors produce → workshops consume Ore→Tools same tick);
  records the player's per-good produced/consumed flows for the UI.
- *Sprite:* new `WorkshopSprites` node builds one shared `SpriteFrames` from the 60 frames and spawns an
  `AnimatedSprite2D` (scaled to ~2 tiles) per workshop; `Main` wires it, `MapManager` spawns on build.
- *UI:* `UIManager` rewritten to show the player nation's `Treasury` + Grain/Ore/Tools with `+prod/−cons`
  flows; added a **Build Workshop** toggle (mirrors Build Road) in `main.tscn`.
- *Save/load:* `GameData` gained `Workshops` (player-built tiles) + `Treasury` on `NationStock`;
  `ApplySavedState(reserves, nationStocks, workshops)` restores them; `Main`+`WorkshopSprites.Initialize`
  respawn loaded workshops. (Owners recomputed from seed, not saved.)
- *Tests:* `Tests/EconomyProductionTests.cs` updated for the rename + **5 new** (owners; build succeeds/
  charges; build fails unaffordable; build fails on other nations' land; workshop Ore→Tools only with ore;
  save round-trip of treasury+workshops).

**Verified:** `dotnet build` → 0/0; `dotnet test` → **46 passed** (41 + 5). In-engine check (animated
workshop appears on click, treasury/ore/tools update in the HUD, F5/F9 round-trip) left for a user run
(no Godot on this machine's PATH).

**Notes:** stale M2 saves (old `Stockpiles` field) degrade gracefully — unknown JSON is ignored, so a
loaded old save just starts with empty stockpiles/no workshops (re-save fixes it). The 60 frames are
full-res (~1.1 MB each) — heavy VRAM; a downscaled sheet later only touches `WorkshopSprites`.

---

## 2026-06-25 — M4 value / prices

Goods now have a sale value and can be sold for treasury income, closing the
**build → produce → sell → reinvest** loop. Static price table (supply/demand response is M5).

**Decisions:** prices **static/tabled** for now (Grain 1, Ore 2, **Tools 6** — Tools priced above the
Ore a Workshop consumes, so refining pays); **sell-all per good** (one button press sells the whole
stockpile of that good) as the simplest legible sell action; no save/load change (prices are static and
the treasury already persists).

**Done:**
- *Prices:* `Economy.PriceTable` (static) + `Prices`/`PriceOf(Good)` accessors.
- *Sell path:* `Economy.SellAll(Good)` — moves the player's whole stockpile of a good into the treasury
  at its price, returns the revenue (0 if nothing to sell), and accumulates `TotalIncome` (a UI display
  stat, **not** serialized — the Treasury is the real persisted state, so income resets on load).
  Player-only (AI nations don't trade yet).
- *UI:* `UIManager` gained an **Income** readout, a unit price on each good line (`Ore: 80 @2`), and three
  **Sell** buttons (Grain/Ore/Tools) in `main.tscn` wired straight to `SellAll` (plain, non-toggle).
- *Income breakdown (follow-up, same day, on user feedback "hard to see where the money is coming from"):*
  income is tracked **per good** — `Economy.IncomeByGood` + `IncomeOf(good)`, `TotalIncome` now the sum of
  the parts, `SellAll` attributes revenue to the good sold (via the existing `AddFlow` helper). The HUD
  appends `— earned N` to each good line (`Ore: 80 @2 — earned 160`), so the Ore-vs-Tools comparison sits
  on the goods instead of hiding in one lump total.
- *Tests:* `Tests/EconomyValueTests.cs` (5) — Tools priced above Ore; `SellAll` moves stockpile→treasury
  at price + zeroes the good + accrues income (per-good); `SellAll` no-ops on an empty stockpile;
  **income tracked per good** (attribution, total = sum, Tools income > Ore income); **alive check** —
  from the same Ore, refining to Tools and selling out-earns selling the raw Ore.

**Verified:** `dotnet build` (game + tests) → 0/0; `dotnet test` → **51 passed** (46 + 5). **In-engine
verified 2026-06-25** (Godot 4.7 now installed — see [[godot-engine-available]]): built a workshop
(`treasury 500→400`), Tools climbed from Ore each tick, sold Ore and Tools, treasury math exact
(`500 − 100 + 2816 income = 3216`), per-good `earned` breakdown correct in the HUD.

**Notes:** `SellAll` sells the entire stockpile each press — fine while selling is the only sink, but once
pops consume goods (M5) the player will want to keep a buffer, so this likely becomes sell-N or a market
panel then. **Balance gap the breakdown exposed:** one workshop consumes 1 ore/tick vs ~113 ore/tick
mined, so refined-Tools income is negligible against raw-Ore volume despite refining winning **per unit**
(6 vs 2) — the Ore→Tools choice only matters once you convert a real *share* of ore, so `Building.Rate`
(workshop throughput) is mis-scaled vs ore output (a M7 tuning knob; M5 demand may also lower raw-ore
value). Prices are static, so there's no downside to over-producing yet — M5's demand sink is what makes
allocation a real choice.

---

## 2026-06-26 — M4.5 clarity + raw/refined split (GATE)

A gate before M5/M6: split **raw vs refined** goods and make *where & why* the player gains resources
legible, so the build choice actually bites (the M4 balance note proved it didn't — ~113 ore/tick made
ore effectively free). Five ordered sub-rungs, each with its own alive check.

**Decisions (with the user):** **whole-world build-gating** (no auto-placed extractors anywhere; the 9 AI
nations have *zero* behaviour implemented, so their deposits stay idle); throttle = a flat **1 unit, every
other tick**; economy numbers are placeholders tuned for real in M7 (`Building.ExtractorCost = 50`,
`WorkshopCost = 100`, `StartingTreasury = ExtractorCost`).

**Done:**
- *Sub-rung 1 — raw/refined type:* `GoodCategory { Raw, Refined }` + `Good.Category()` extension
  (Grain/Ore = Raw, Tools = Refined). `Tests/EconomyGoodCategoryTests.cs`.
- *Sub-rung 2 — build-gated extraction:* removed the auto-placement in `Economy.PlaceDeposits`; raw goods
  now need a player-built Mine/Farm on the deposit. `Economy.TryBuildExtractor(tile)` auto-picks the type
  from the deposit; `TryBuildWorkshop`/`TryBuildExtractor` now share a private `TryBuild` helper. New
  **Build Extractor** toggle in `UIManager` + `main.tscn`, wired through `MapManager.OnMapClick`.
- *Sub-rung 3 — throttle + small treasury:* `OnTick` Pass 1 produces a flat **1 unit** and only on
  alternating ticks (deposit `Rate` bypassed for raws); `StartingTreasury` shrunk to one extractor's cost,
  so the first build is a real choice and a Workshop is only reachable after selling.
- *Sub-rung 4 — per-gain feedback:* `Economy.ProducedThisTick` (engine-free `(tile, good, amount)` list,
  cleared+filled each tick) → new `TickManager.TickProcessed` event → `Main` drains it onto
  `Systems/Map/GainFeedback.cs`, a fading upward-drifting **"+N"** `Label` per producing tile. No Godot
  signals in Core.
- *Sub-rung 5 — all-nations panel:* `UIManager` builds one stockpile row per nation (player first, row
  tinted), updated each frame from `Economy.Nations`. AI rows read 0 until AI behaviour exists.
- *Persistence (required by sub-rung 2):* extractors are no longer seed-derived, so every built building
  must be saved. New Core `TileBuilding { Tile, Type }`; `GameData.Workshops (List<Vector2I>)` →
  `Buildings (List<TileBuilding>)`; `InitSaveAndLoad` saves all tiles+types; `Economy.ApplySavedState`
  re-places them via `Building.Of(b.Type)`.
- *Tests:* new `Tests/EconomyBuildGateTests.cs` (unbuilt deposit produces nothing; `TryBuildExtractor`
  places the matching type + charges + starts the flow; fails with no deposit; starting treasury affords
  exactly one extractor; a producing tick records a gain event). Rewrote the four extractor tests in
  `EconomyProductionTests.cs` for the build-gated + throttled model and updated the workshop/persistence
  tests for the smaller treasury + `TileBuilding` save shape.

**Verified:** `dotnet build` (game + tests) → **0 warnings / 0 errors**; `dotnet test` → **59 passed**
(was 51 + the M4.5-1 category test + 5 build-gate). **Headless smoke** (Godot 4.7,
[[godot-engine-available]]): scene instantiates, all `GetNode` paths resolve (incl. the new
`ExtractorButton`), economy ticks, and the heartbeat now holds at `grain=0 ore=0 tools=0` — the visible
proof that nothing produces until the player builds (sub-rung 2).

**Pending:** sub-rungs 4 (the `+1` flash) and 5 (the all-nations panel) are code-complete and
headless-clean but their *on-screen* alive checks need a **windowed** run with clicks — best driven on the
user's display (HUD is tiny in downsampled screenshots; real legibility is M7). Same for a windowed
save/load round-trip of built buildings.

**Notes:** with `StartingTreasury = 50` and **1 ore / 2 ticks**, the early game is intentionally tight (a
long grind to the first Workshop) — that's a placeholder; real pacing is M7. The `+1` label font and the
nations panel are deliberately minimal — clarity over polish for the gate.

### Follow-ups (same day, after the user's windowed review)

The user verified M4.5 in-engine ("looks wonderful") and asked for polish + a rename:

- *HUD polish (`UIManager.cs`):* the three build toggles (Railroad/Workshop/Extractor) are now **mutually
  exclusive** (`OnBuildToggled` turns the others off); the panel is **draggable** (press falls through the
  container/labels — set to `MouseFilter.Ignore` — to the `PanelContainer`, which follows the cursor) and
  **clamped on-screen** (`ClampToViewport`, via the inverse canvas transform since the HUD sits under the
  Camera2D); and the **font size is doubled** (a `Theme { DefaultFontSize = 32 }` on the UIManager).
- *Road → railroad rename (the track was always rail):* renamed across the code — `RoadTiles`→
  `RailroadTiles`, `RoadBitmask`→`RailroadBitmask` (files + classes), `OrientRoadCells`→
  `OrientRailroadCells`, `IsRoadBuildMode`→`IsRailroadBuildMode`, the `RoadLayer` scene node →
  `RailroadLayer`, the "Build Road" button → "Build Railroad". The `road.tres` tileset asset keeps its
  filename (referenced by uid).
- *Railroads cost treasury (a real economic spend):* new `Economy.RailroadCost` (placeholder 10) +
  `Economy.Railroads` set + `Economy.TryBuildRailroad(tile)` (charges, dedupes, no ownership check since
  track crosses territory). `MapManager` gates laying track on it. Persisted: `GameData.Railroads`,
  saved in `InitSaveAndLoad`, restored in `ApplySavedState` (now 4-arg) and re-drawn in
  `Main.DrawMSTLines`. New test `TryBuildRailroad_charges_treasury_once_per_tile_and_respects_affordability`
  + railroad coverage in the persistence test.

**Verified:** `dotnet build` → 0/0; `dotnet test` → **60 passed**; headless smoke clean (renamed
`RailroadLayer`/`RailroadButton` nodes resolve). Interactive polish (drag/clamp, exclusive toggles, font,
railroad cost) confirmed on the user's display.

**Docs:** consolidated this session — folded the redundant `2026-06-25_Session-Summary.md` into this
history (deleted), refreshed the `Documentation/` system docs (Economy, SaveLoad, WorldgenPipeline,
Architecture, TickLoop, GodotNodeLifecycle, README) to M4.5 + the railroad rename, and ticked the
resolved items in `minorFixes.md`.

---

## 2026-06-27 — Build-cost legibility + M5 demand & scarcity

Two pieces: the queued "show the cost / flash the spend" clarity note (which also completes M4.5
sub-rungs 4–5's intent), then M5 — the first demand-side pressure.

**Part 1 — build-cost legibility (queued note).**
- *Cost on every buildable:* the three build toggles now read their price — "Build Railroad (10)",
  "Build Workshop (100)", "Build Extractor (50)" — set in `UIManager._Ready` from the cost constants
  (`Building.ExtractorCost`/`WorkshopCost`, `Economy.RailroadCost`) so the caption can never drift from
  what `TryBuild` charges. `Economy.RailroadCost` was made `public const` for this.
- *Spend flash:* new `GainFeedback.FlashCost(tile, amount)` — the red "−N" counterpart to the "+N" gain
  flash (same rise+fade `FloatingLabel`). `GainFeedback` is now created in `Main.CreateObjs` (before
  `MapManager`) and passed into the `MapManager` ctor (mirroring `WorkshopSprites`); each successful build
  branch in `OnMapClick` flashes the cost on the built tile. So paying reads on the map the same way
  gaining does.

**Part 2 — M5: demand + scarcity.**
- *Demand sink (pops eat grain):* `Nation.Population` (a consumption node — no labor sim). The player's
  pops want `Population * GrainPerPopPerTick` grain/tick; a new **Pass 3** in `Economy.OnTick` consumes
  `min(desired, onHand)` (stockpile floors at 0 — no negative grain, no starvation penalty yet), recorded
  into the existing `PlayerConsumedLastTick[Grain]` so the HUD's Grain line shows the `-N` pop outflow for
  free. Tunables (M7-placeholders): `StartingPopulation = 5`, `GrainPerPopPerTick = 0.1` → 0.5 grain/tick,
  sized to ~one farm's throttled output so "feed the pops vs. sell the grain" is a real choice from the
  first build. **Player-only** for now (AI have no production to consume).
- *Scarcity pricing:* the static price table became a **base** table (`BasePrice`, values unchanged:
  Grain 1 / Ore 2 / Tools 6) × a per-good `_priceMultiplier` recomputed each tick. Model is
  **stock-based**: a good is balanced (multiplier 1) at ~`PriceBufferTicks` (5) ticks of demand in stock;
  low stock vs demand drives the price toward the ×3 ceiling (shortage), a glut toward the ×0.5 floor; a
  good nothing demands stays at base. (Chose stock-based over the planned flow `demand/supply` ratio
  because the throttled flows are tiny and oscillate 0/1 per tick — stock is smoother and actually
  spikes.) Demand for pricing is the *desired* appetite (`PlayerDemandLastTick` = pop grain + workshop
  ore), not just satisfied consumption, so an unmet shortage still lifts the price. `PriceOf` returns
  `base × multiplier`; before the first tick / with no demand the multiplier is 1, keeping `SellAll` and
  the M4 value tests valid.
- *UI:* new `PopLabel` ("Pop: N (eats M grain/tick)") in `main.tscn`/`UIManager`; the grain shortage is
  otherwise visible through the existing Grain line (a `-N` drain + a rising `@price`).
- *No save/load change:* `Population` is deterministic from construction (regenerated like `Tiles`), and
  the price multiplier is a live display value (like `IncomeByGood`) — neither is serialized.

**Tests.** New `Tests/EconomyDemandTests.cs` (8): pops have grain demand; eat their full appetite when
supplied; grain floors at 0 under shortage; a shortage lifts grain's price above base; a glut sinks it
below; a no-demand good (Tools) stays at base; raising the grain buffer relieves the shortage price. Four
extraction-amount tests in `EconomyProductionTests`/`EconomyBuildGateTests` gained
`PlayerNation.Population = 0f` to isolate pure extraction from the new demand sink (same isolation pattern
as the existing `TileBuildings.Clear()`).

**Verified:** `dotnet build` → 0/0; `dotnet test` → **67 passed** (59 prior + 8 demand); headless smoke
clean (scene instantiates → new `PopLabel` resolves, economy ticks, heartbeat holds 0 under build-gating).

**Pop tax (same-day follow-up, user request).** A flat **poll tax**: `Economy.TaxPerInterval = 50` gold
into the player's treasury every `TaxIntervalTicks = 10` ticks (no pops ⇒ no tax), added at the end of
`OnTick`. Gives a steady income independent of selling, so an early grain shortage (grain pegged at the ×3
ceiling with no farm) isn't a dead end — bank the tax, buy a farm. `Economy.TaxCollected` tracks lifetime
tax (display stat, not serialized — the treasury it feeds is the real saved state). HUD `PopLabel` now reads
"Pop: 5 (eats 0.5 grain/tick — tax +50/10t, earned N)". Two tests in `EconomyDemandTests`
(levy lands every interval + accumulates; no pops ⇒ no tax). **69 tests green.**

**Pending (windowed, on the user's display).** Cost captions read on the buttons (✅ confirmed by user);
a red "−cost" flashes on every build (mirrors the "+1" gain); the Grain line shows a `-N` pop drain and its
`@price` ticks up as grain runs dry (✅ user saw grain pegged @3 under shortage), then back down after
building a farm; the treasury jumps +50 every 10 ticks from the tax; plus the still-open M4.5 carry-overs
(all-nations panel render — ✅ visible in user's screenshot, save/load of built buildings).

---

## 2026-06-27 — M6 phase-1: a living, trading multi-nation economy

The first **monopoly-matters payoff** from `Documentation/TradeDesign.md`. Until now only the player produced,
consumed, taxed, and had prices, so a monopoly had no victim and trade no counterparty. This brings the 9 AI
nations to life and lets goods auto-arbitrage between connected nations. Three rungs (full phase-1 in one push,
per the user). Spatial levers (capacity, connector roads, tariffs, blockades, diplomacy) are later phases.

**Shared refactor.** `TryBuild(tile, building, Nation builder)` now charges the *builder* and gates on the
builder owning the tile, so both the player (map click → `PlayerNation`) and the AI (auto-build → that AI
nation) build on their own land; the player still can't build on AI land (they don't own it). New
`Economy.EnableAiAutonomy` (default true) — one switch that gates AI population/auto-build/tax so
extraction-isolation tests keep the AI inert.

**Rung 1 — AI come alive (`Economy.cs`).** Every nation gets `StartingPopulation` in the constructor (demand +
tax base). Pass 3 (grain consumption) and the poll tax now loop **all** nations (each taxes/feeds its own pops;
`TaxCollected` stays a player HUD stat). New `RunAiBuild()` — each AI nation greedily builds **one extractor per
tick** on its lowest-coordinate un-built owned deposit when it can afford one (deterministic; extractors only,
no AI refining; treasury-gated so they grow off tax income). Production already routed output to the owning
nation, so AI accumulate real stockpiles with no further change.

**Rung 2 — per-nation prices (`Economy.cs`, `Nation.cs`).** The single global player-driven multiplier became
**per-nation**: `Nation.PriceMultiplier` (live, not serialized) recomputed each tick from that nation's own
demand (pop grain + its workshops' inputs, via `DemandOf`) vs. its own stock. `PriceOf(Nation, Good)` is the
new core; `PriceOf(Good)` = the player's local price (so `SellAll` + HUD are unchanged). Different nations now
price the same good differently — the gradient trade chases.

**Rung 3 — inter-nation trade (`Economy.cs`, `Main.cs`).** `SetTradeGraph(mstEdges, capitals)` hands the
economy the MST backbone as `(Nation, Nation, distance)` routes (wired in `Main._Ready` after `BuildMst()`;
rebuilds from the seed for new + loaded games). New `RunTrade()` at the end of `OnTick`: along each route, each
good flows **one unit** from the cheaper nation to the dearer when the price gap clears the distance-scaled
transport cost (`TradeCostPerTile * distance`) and the buyer can pay, transacted at the **midpoint price** — a
pure gold+goods transfer (no gold created), so a sole supplier's income is drained straight from its customers'
treasuries. Events recorded in an engine-free `TradedThisTick` (for tests now; real flow viz later can replace
`TradeNetworkVisualizer`'s fake packets). Tunables (M7): `TradeUnit = 1`, `TradeCostPerTile = 0.02`.

**Persistence: no schema change.** `InitSaveAndLoad` already saved *all* `TileBuildings` + *all* nations'
treasury/stockpile, and `ApplySavedState` re-places every building — so AI buildings/treasuries round-trip for
free. Population is deterministic (constructor); prices + `TradedThisTick` are live.

**Tests.** New `Tests/EconomyTradeTests.cs` (8): every nation has pops; AI develops + produces; tax lands for
all; inert-AI does nothing with autonomy off; per-nation prices diverge; grain flows surplus→shortage with gold
back; disconnected = no trade; a sole supplier's treasury climbs off a connected consumer. Updated
`Output_lands_in_the_player_nation` to set `EnableAiAutonomy = false` (keeps testing player extraction in
isolation). **77 tests green** (69 + 8).

**Verified:** `dotnet build` → 0/0; `dotnet test` → 77 passed; headless smoke — no exceptions and the heartbeat
now **moves on its own** (grain/ore climb with no player build, since the AI auto-build + produce; grain
oscillates as AI pops eat it, ore accumulates with no AI consumer).

**Pending (windowed, user's display):** the all-nations panel showing AI stockpiles growing + diverging; build
nothing and watch a connected neighbour's surplus flow to you (treasury moving); a save/load round-trip with AI
buildings present.

**Next (M6 phase-2+):** edge capacity + connector roads (geography becomes decisive), then tariffs/tolls,
chokepoints/blockades, diplomacy; AI workshops/strategy; the manual-vs-auto selling decision; per-province
stockpiles.

---

## 2026-06-27 — Realistic blended ground + sand beaches & deserts

An art/rendering overhaul (not an economy rung): swap the Tiny Swords pixel-tile *ground look* for a
continuous, realistic blended ground (grass + sand) rendered by a shader, and add **sand beaches +
deserts** driven by noise. Assets (seamless grass/sand textures + the "merge" blend shader) came from the
sibling `tile-tester` prototype. Chosen approach was **Option A** (full ground render replacement) over a
plain-tile swap, after weighing it with the user.

**Mental model — split the two things the tilemap was doing.** `0zTileMapLayer` was both the *data grid*
(which cell is water/grass/sand, provinces, capitals, clicks, save/load) and the *look* (64×64 stamps).
Option A keeps the data grid untouched and moves the ground *look* to one full-map shader sprite:
- **Data grid stays** on `0zTileMapLayer`: still placed by noise, still read by Voronoi/economy/clicks.
  Its water/grass/sand graphics for land are now **transparent** (single 0:0 tile on `transparent64.png`),
  so the shader ground shows through. Water keeps its existing teal tile (already matches), capitals/roads
  /borders keep drawing on top.
- **Look** = new `GroundRenderer` (full-map `Sprite2D`, `ZIndex -100`) + `Shaders/terrain_ground.gdshader`,
  fed a tiny **terrain-mask** `ImageTexture` (one texel per cell: R = sandiness, G = water — built in
  `Initialize` the same way `VoronoiDiagram.CreateProvinceMap()` builds its 256×128 image). The shader
  samples the seamless textures in **world space** (no per-cell repeating grid), `smoothstep`-blends
  grass↔sand (soft beaches/deserts), and draws flat water (crisp shore; soft ocean blend deferred until
  water gets a real texture). Mask texel centers align with tile centers (shared `(x+0.5)` convention).

**Sand logic** (pure, unit-tested in `Systems/Core/Map/TerrainRules.cs` + `TerrainType.cs`):
- **Beaches** = thin low-coast height band `0.1 < h < 0.135` → gentle low coasts go sandy, steep coasts
  stay grass (natural variety, not a uniform ring; coastal by construction).
- **Deserts** = a second low-amplitude **moisture** `FastNoiseLite` (seed `worldSeed + 7919`, freq `0.03`):
  dry inland (`moisture < -0.20`) → desert sand. Tuned empirically — a very low frequency made deserts
  feast-or-famine (3%→27% by seed); freq `0.03` + threshold `-0.20` gives a reliable **~11–19% of inland**
  every seed. Deterministic from the one world seed, so save/load reproduces.

**Highlight rework (required by the reveal).** Province click-highlight used to *swap* ground tiles to
glowing-grass and restore them to grass — which both assumed stamps and would have corrupted sand cells on
restore. Now `MapManager` draws a translucent tint on an **overlay layer** (the previously-unused
`BordersLayer`, child of `0zTileMapLayer`, using a new `Highlight` source in `BorderHighlight.tres` →
`highlight_overlay.png`) and `Clear()`s it on the next click — the ground data grid is never touched.
Dropped the `previousTiles` tracking and the `Bitmask.OrientCells()` call (grass is a single transparent
tile now; the shader does edges).

**Files.** New: `Systems/Core/Map/TerrainType.cs`, `TerrainRules.cs`; `Systems/Rendering/GroundRenderer.cs`
+ ported `GroundBlend.cs`; `Shaders/terrain_ground.gdshader` + ported `Shaders/ground_blend.gdshader`;
`Scenes/GroundBlendTest.tscn` (standalone preview of the raw blend shader); `Images/grassSeamless.png`,
`sandSeamless.png`, `transparent64.png`, `highlight_overlay.png`; `Tests/TerrainRulesTests.cs`. Changed:
`Initialize.cs` (classify + place sand + build mask), `Main.cs` (moisture noise, spawn `GroundRenderer`,
drop `OrientCells`), `Bitmask.cs` (skip sand), `MapManager.cs` (overlay highlight), `tilesets/tilesetTest.tres`
(grass/sand → transparent single tiles), `tilesets/BorderHighlight.tres` (+highlight source).

**Texture note.** Confirmed via an edge-wrap metric that `grass.png` was *not* seamless (~2.6× edge seam)
while `grassSeamless.png` (1.1×) and the user's new `sandSeamless.png` (1.07/0.93) tile cleanly — those two
are what ship. See `tile-tester/TEXTURE_NOTES.md` (copied to `ReviewDocs/Documentation/`).

**Verified:** `dotnet build` 0/0; `dotnet test` **88 passed** (77 + 11 new `TerrainRules`); headless smoke —
no exceptions, terrain-count log prints (`Terrain: water=… grass=… sand=… (beach=… desert=…)`), economy
heartbeat still ticks. Shader parses without error on load.

**Pending (windowed, user's display — computer-use disabled for this project):** confirm the realistic grass
+ soft sandy beaches on gentle coasts + inland deserts (no repeating grid); water flat teal with a clean
shore; capitals/borders/roads render above the ground; click a province → yellow tint highlight + inspect
still work; open `Scenes/GroundBlendTest.tscn` to compare the raw blend look; same seed (save/load) →
identical map. Tunables to taste: shader `ground_px` (pattern scale) + `softness`; `TerrainRules.BeachLine`
/`DryThreshold`; moisture `Frequency` in `Main`.

**Later:** real water texture/animated shader + soft shoreline (the deferred sand↔ocean blend); highlight
that respects sand vs grass; retire the now-unused Tiny Swords ground assets + `TerrainTiles.Highlight`
source.

---

## 2026-06-27 — L1 economy completion: M6 phases 2–3, M7, M8

Finished the remaining L1 ladder in one pass (M6 phase-1 + the art overhaul were earlier today). Core
mechanics are unit-tested; visual/interactive bits are headless-clean and flagged for a windowed pass.
Per-milestone commits on `main`.

### M6 phase 2 — route capacity + connector roads
Trade routes became `TradeRoute` objects with a per-tick **capacity** = `BaseRouteCapacity` (the MST
backbone) + `CapacityPerRail` per **player railroad tile in the route's corridor**. So laying track along a
route is a **connector road** that raises throughput — railroads finally have an economic purpose.
`RunTrade` caps total units/route/tick and records `FlowLastTick`/`CapacityLastTick`; `SetRouteBlocked` cuts
a route (carries nothing → the far side starves, prices diverge). Capacity is derived live from the
persisted `Railroads` set + deterministic MST corridors (passed from `Main` in `SetTradeGraph`), so **no new
save state**. Tests: `Tests/EconomyCapacityTests.cs`.

### M6 phase 3 — diplomacy (tariffs + blockades)
Pairwise `DiplomaticStance` (Hostile/Neutral/Friendly, default Neutral): **Hostile = blockade** (RunTrade
skips the route), **Neutral = a pure import-tariff barrier** (`NeutralTariffRate` × base price) the price gap
must clear, **Friendly = free trade**. The tariff is a barrier only, not a gold transfer (a nation is its own
government — taxing its own imports would mint money). `GetRelation`/`SetRelation`/`SetPlayerStance` give the
player the lever; relations are live state (not yet serialized). Tests: `Tests/EconomyDiplomacyTests.cs`.

### M7 — legibility + diplomacy controls
- **TradeNetworkVisualizer** rewritten to draw REAL data: routes are roads colored by live congestion (idle
  gray / flowing green / near-capacity orange / cut red) and widened by capacity; a colored packet spawns per
  actual trade (`Economy.TradedThisTick`, fed each tick from `Main`). No more fake random packets.
- **DepositOverlay** reflects live state: dim idle, bright + bigger when worked, pulsing red when played out.
- **Diplomacy is playable:** click a foreign province, press **B** to cycle the player's stance (raw key, no
  project.godot change); stance shows in the click-inspect line and on each AI's HUD nation row.
- **Balance** stays documented placeholders — final "tune to fun" needs a windowed playtest (can't judge fun
  headless). Province-price map tint deferred (trade roads + deposit flash + HUD cover legibility for now).

### M8 — encrypted saves + obfuscation config
- **Saves are AES-256 encrypted** (on by default). `Systems/Core/SaveCrypto.cs` (pure .NET, unit-tested)
  encrypts the JSON body; `Serialization` writes `SEED <n>` + base64 to `user://savegame.dat` — the **seed
  stays plain** (shareable; the world regenerates from it), everything else is unreadable in a text editor.
- **Assembly obfuscation** is a **manual, opt-in** step (`Obfuscar.xml` + `Documentation/Obfuscation.md`),
  deliberately not in the build: Godot binds C# by name, so naive renaming breaks it; the config keeps the
  Godot-facing surface and only obfuscates private internals. Tests: `Tests/SaveCryptoTests.cs`.

**Verified:** `dotnet test` **99 passed** (77 → 99 across the day: +TerrainRules, +Capacity, +Diplomacy,
+SaveCrypto); `dotnet build` 0/0; headless smoke clean each milestone (worldgen + terrain log + economy
heartbeat, no exceptions).

**Pending (windowed, user's display):** trade roads recoloring with congestion + packets moving on real
trades; deposit dots flashing when depleted; press **B** on a neighbour to blockade/ally and watch its road
turn red / goods stop; F5 → open `savegame.dat` (only `SEED` readable) → F9 restores. Economy balance tuning.

**L1 status: M0–M8 all code-complete.** Remaining is windowed balance/feel tuning and the optional manual
obfuscation pass at ship.
