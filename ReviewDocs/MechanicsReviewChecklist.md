# Mechanics Review Checklist

A curated, **skip-the-boilerplate** map of the classes and methods that actually control game mechanics —
so a review can focus on logic that matters (the economy tick, building, pricing, trade, persistence,
worldgen) and ignore wiring/drawing/glue. Ordered by importance. Check a box once you've read + are happy
with that method's logic.

> The single most important file is **`Systems/Core/Economy/Economy.cs`** — it owns the whole simulation.
> If you only review one thing, review `OnTick` and the methods it calls. Everything in `Systems/Core/` is
> plain C# (no Godot), so it's the testable heart; everything else is the Godot view/glue around it.

---

## 1. The economy core — `Systems/Core/Economy/Economy.cs`  ★ most important

The simulation. Almost every mechanic lives here.

- [ ] **`OnTick()`** — the master tick. Reads top-to-bottom as the per-tick order of operations:
  AI build → Pass 1 (extractors) → Pass 2 (workshops) → Pass 3 (pop consumption) → tax → prices → trade.
  *This is the spine — review the ordering and each pass.*
- [ ] **`RunAiBuild()`** — AI greedily auto-build one extractor/tick on their deposits (M6). Determinism +
  the greedy choice.
- [ ] **`RecomputePriceMultipliers()` + `DemandOf(Nation)`** — per-nation scarcity pricing (stock vs demand,
  clamped). The price model.
- [ ] **`RunTrade()`** — inter-nation auto-arbitrage along the MST (cheaper→dearer, midpoint price, transport
  cost, pure gold transfer). The monopoly mechanic.
- [ ] **`TryBuild(tile, building, builder)`** — the shared build gate (ownership + empty + affordable +
  charge). Used by player and AI.
- [ ] **`TryBuildExtractor` / `TryBuildWorkshop` / `TryBuildRailroad`** — the build entry points (cost +
  placement rules).
- [ ] **`SellAll(Good)`** — the manual "dump to market" income path (M4).
- [ ] **`SetTradeGraph(mstEdges, capitals)`** — how the MST becomes trade routes.
- [ ] **`PlaceDeposits(...)` + `GrowCluster(...)`** — deterministic deposit worldgen (clusters, ore/grain
  split, reserves). *Where the "zero-grain start" comes from.*
- [ ] **`PriceOf(Nation, Good)` / `PriceOf(Good)`** — the price read everything uses.
- [ ] **`ApplySavedState(...)`** — load path: re-applies saved reserves/treasuries/stockpiles/buildings.
- [ ] **Constructor + the tunables block** (top of file) — starting treasury/population, tax rate, price
  clamps, trade unit/cost, deposit counts. *The balance knobs (M7 placeholders).*

## 2. Economy data types — `Systems/Core/Economy/`

Small, but they define the rules. Quick reads.

- [ ] **`Building.cs`** — `Of(BuildingType)` recipe table (inputs/outputs/rate) + the `Cost` constants.
- [ ] **`Nation.cs`** — fields that matter: `Treasury`, `Stockpile`, `Population`, `PriceMultiplier`,
  `IsPlayer`. (`AddGood` is the one method.)
- [ ] **`Deposit.cs`** — `Good`, `Reserve`, `Rate`, `Size` (skim).
- [ ] **`Good.cs`** — the goods enum + `Category()` (Raw/Refined). (skim)

## 3. The tick driver — `Systems/GameTime/TickManager.cs`

- [ ] **`Update(delta)`** — the 1-second tick gate that calls `economy.OnTick()`, fires `TickProcessed`, and
  prints the heartbeat. (Short; the only logic is the timer + the dispatch.)

## 4. Player input → mechanics — `Systems/Map/MapManager.cs`

- [ ] **`OnMapClick()`** — routes a click to the right build (workshop/extractor/railroad) or to
  inspect/highlight. *The only place player actions enter the economy.*

## 5. Save / load — `Systems/SaveLoad/`

- [ ] **`InitSaveAndLoad.initializeSave()`** — exactly what gets serialized (reserves, per-nation
  treasury+stockpile, all buildings, railroads).
- [ ] **`Main.OnLoadRequested` + `Main._Ready` (load branch)** — the reproduce-from-seed load flow (regen
  world, then `ApplySavedState`). *Pair this with `ApplySavedState` above.*
- [ ] `GameData.cs` / `Serialization.cs` — the DTO + file I/O (skim; mostly plumbing).

## 6. Composition root — `Systems/Game/Main.cs`

Mostly wiring, but two spots are mechanic-relevant:

- [ ] **`CreateObjs()`** — how `Economy`/`TickManager`/`MapManager` are built and connected.
- [ ] **`BuildMst()` + `SetTradeGraph` wiring + `DrawMSTLines()`** — the trade backbone and how it's handed
  to the economy. (The rest of `_Ready` is scene setup — skip.)

## 7. Worldgen (geography that drives the economy)

- [ ] **`Systems/Map/VoronoiDiagram.cs`** — `GetCapitals()` + how provinces/capitals are assigned (the
  nations' territory). *Skim the drawing; focus on capital placement + the tile→capital map.*
- [ ] **`Systems/Core/Pathfinding/PrimsAlgorithmSimple.cs`** — `FindMST()` (the trade/rail backbone graph).
- [ ] `Systems/Game/Initialize.cs` `PlaceTiles()` — land vs water from noise (skim; affects where deposits
  can go).

## 8. UI readout (logic, not layout) — `UI/UIManager.cs`

- [ ] **`_Process()` + `FormatGood(...)`** — what the HUD reports (treasury, per-good price/flow/income,
  pop/tax, all-nations panel). *Verify it reflects the real economy state; skip the drag/clamp/theme code.*

---

## Explicitly skip (boilerplate / view glue — not mechanics)

`Systems/Cameras/Camera.cs`, `Systems/Controls/Controls.cs` (signal hub), `Systems/Map/` drawing helpers
(`DepositOverlay`, `TerritoryOutline`, `WorkshopSprites`, `GainFeedback`, `ColoredProvincesSprite`,
`Bitmask`, `RailroadBitmask`), `Systems/ResourcePopup/*` (the trade visualizer is **fake** packets),
`Systems/Core/Map/*` (tile-id catalogs/resolver), `MyGlobalClass`, `GameContext` (DTO bag),
`JsonConverters/*`. These are correct-by-inspection or pure presentation.

## Suggested review order
1. `Economy.OnTick` end-to-end (§1) → 2. the methods it calls (§1) → 3. `Building`/`Nation` rules (§2) →
4. `MapManager.OnMapClick` (§4) → 5. save/load pair (§5) → 6. worldgen determinism (§7). The tick driver
(§3) and UI readout (§8) are quick confidence checks at the end.
