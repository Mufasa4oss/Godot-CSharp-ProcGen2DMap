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
