# Logistics Rework — Milestone Roadmap (player-built network + physical goods)

> **Status: LR0–LR5 + LR-cleanup ALL COMPLETE (2026-06-29).** All milestones committed; abstract layer
> (`RunTrade` + `Nation.Stockpile` for player) retired; M-era economy tests rewritten; headless smoke clean.
> Windowed alive checks are pending user walkthrough (see checklist at bottom of each milestone detail). A focused arc that
> **supersedes L1's M6 (logistics) and the
> trade parts of M7**. It realizes the already-locked vision in
> [`../Documentation/TradeDesign.md`](../Documentation/TradeDesign.md) +
> [`../Documentation/EconomyDesign.md`](../Documentation/EconomyDesign.md). Sibling of
> [`L1-Milestone-Roadmap.md`](L1-Milestone-Roadmap.md) (M0–M8, code-complete). Same rule as the M-ladder:
> each milestone ends **runnable + inspectable** with an alive check; don't start the next until the current
> one's check passes.

## Why this rework

The current logistics/trade systems were scaffolding that **contradicts the project's own locked vision**
(`TradeDesign.md` already says *"you build the network,"* *"no buy/sell orders, ever,"* *"a deposit far from
the network is worth less"*):
- Trade routes are **auto-generated** from an MST over capitals (`Main.BuildMst` + `DrawMSTLines`) — the
  player never builds the network.
- Goods **teleport** via abstract arbitrage (`Economy.RunTrade`) and are sold via **manual Sell buttons**
  (`Economy.SellAll`) into an infinite abstract market.

We replace that with a **player-built, tile-based road network carrying physically-located goods**, where the
market emerges from production + transport + demand instead of buttons. This *finishes* the vision rather than
changing it.

## Locked decisions (2026-06-28)

- **Network = tile-based roads (Factorio-style).** Player places road tiles; two points are "connected" only
  when a contiguous path of road tiles links them; removing a tile can sever a connection.
- **Goods are PHYSICAL and per-tile.** A producer's output sits on its tile (a tile inventory); goods move
  **tile-by-tile** along the network and always have a location (on a tile, or on a hauler on a tile). The
  national stockpile pool and instant arbitrage go away.
- **Haulers are fully automatic.** Player builds roads; the sim auto-dispatches haulers to move surplus →
  demand. No vehicle placement or per-trip routing (honors the locked "automate the micro" rule).
- **Income is layered: internal pop market first, then export.** Pops *buy* what you deliver to them (main
  early income + "value to your population"); export to other nations comes second. **No manual Sell.**
- **Keep:** diplomacy (`DiplomaticStance`, relations, the hotkey **rebound B → N** — gets a real UI), the
  trade-packet visualizer (evolves into the haulers), and deposits/extractors/workshops/pops/tax.
- **"Province" = a Nation's Voronoi region** (no `Province` class since M3). Going **finer: per-tile.** Rename
  every residual `province` in code to `nation`.
- **Settlements are a future layer.** Goods deliver to a nation (its **capital**) for now; **towns/villages**
  are a later concept — not built in this rework.

## The model — how money & value work

- **Value = scarcity priced across space.** A good is cheap where abundant, dear where scarce-but-demanded;
  the price gap is the money, collectible only if your network can *physically* move the good across it.
- **Income faces:** (1) **internal market** — pops buy what haulers deliver to your capital; (2) **export** —
  surplus hauled to a connected neighbor sells at their price; (3) **pop tax** (steady base); (4)
  **tariffs/tolls** on goods crossing territory you control (the chokepoint lever).
- **Haulers are the physical face of the old arbitrage** — same "goods flow high→low price automatically,"
  now with location, travel time, throughput, and cut-able chokepoints.
- **Sinks (`EconomyDesign.md`):** build costs + recurring upkeep keep gold meaningful; add as the curve needs.

## Architecture

- **Road graph + pathfinding (new — none exists today).** Pure-Core `RoadGraph` over player road tiles
  (4-neighbour adjacency): **BFS reachability** ("is A connected to B?") + **A*/BFS shortest path** (hauler
  routing; transport cost = path length). Pure `Systems/Core/...`, unit-tested without Godot (mirrors the
  existing `TileResolver` / `VoronoiAssignment` Core pattern).
- **Goods location:** a tile inventory for goods at rest; goods in transit live on hauler entities.
- **Auto-dispatch + haulers:** a dispatcher matches a surplus source to the nearest road-connected demand
  sink, spawns a **hauler entity** that pathfinds over roads, travels the path over time carrying a load, and
  unloads. Sinks = the nation's **capital** (its pops buy delivered goods) + export borders.
- **Implementation choice — hauler *entities*, not a belt cellular-automaton.** Goods ride auto-dispatched
  haulers that pathfind tile-by-tile (the good "lives on that tile as it travels" = it's on the hauler on that
  tile). Tractable, matches "watch trucks move," gives distance/capacity/chokepoints without belt-gridlock.
  The packet visualizer already lerps dots along edges → upgrade to lerp along tile paths.

## Removed / replaced / kept

| Current | Fate |
|---|---|
| `Main.BuildMst`, `MstBuilder`, `PrimsAlgorithmSimple`, `DrawMSTLines`, `GetTilesAlongLine` | **Removed** — no auto network/backbone. |
| `Economy.SetTradeGraph`, `_tradeRoutes`, `TradeRoute`, `RunTrade` (abstract arbitrage) | **Replaced** by road graph + physical hauler flow (kept transitionally in LR1, removed by LR2–LR3). |
| `Economy.SellAll` + Sell buttons | **Removed** (LR3) — income from deliveries. |
| `Nation.Stockpile` (national pool) | **Replaced** by per-tile inventories + a capital inventory (LR2–LR3). |
| Railroad tiles/build (`RailroadLayer`, `RailroadBitmask`, `RailroadTiles`, `road.tres`, `TryBuildRailroad`) | **Kept & promoted** to the road-building system (ergonomic: click-drag, cheap). |
| `TradeNetworkVisualizer` | **Kept & evolved** into the hauler renderer. |
| Diplomacy (`DiplomaticStance`, relations, hotkey, HUD stance) | **Kept** — gates export terms (LR4) + gets a real UI; hotkey B → N. |
| Deposits, extractors, workshops, pops, tax, scarcity pricing | **Kept** — production emits to tile inventories; prices recompute from local supply/demand. |

---

## The ladder

| M | Name | What it adds | Alive check | Status |
|---|------|--------------|-------------|--------|
| **LR0** | Prep / cleanup | Rename every residual `province` → `nation` (code + `main.tscn` node + `GetNode` paths); rebind diplomacy hotkey **B → N**. No behavior change. | Game runs identically; no `province` identifiers remain; diplomacy is on **N**; 99 tests still green | ✅ (2026-06-28) |
| **LR1** | Player roads + connectivity (replace MST) | Remove MST gen + backbone; ergonomic road building (click-drag, cheap); Core `RoadGraph` (BFS); re-gate the *existing* trade on real road connectivity | Build a road; the auto-backbone is gone; two nations trade **only** when road-connected; cut a road → trade stops / region isolates | ✅ (2026-06-28) |
| **LR2** | Physical goods + auto-haulers | Tile inventories (producers emit onto their tile); road path + dispatcher + hauler entities carrying loads over time; `Haulers` renderer draws them + at-rest piles. (`RunTrade` **kept transitionally** like LR1, not removed — retired in LR-cleanup.) | Watch a hauler carry ore from a mine along your road; severing the road strands the goods; nothing teleports anymore | ✅ (2026-06-28) |
| **LR3** | Internal market & income (remove Sell) | `Economy.RunInternalMarket` — pops **buy** the goods haulers delivered to the capital → income (demand-capped; surplus → export); `SellAll` + Sell buttons removed. (National-pool reads kept transitionally, not migrated — see LR-cleanup.) | Income comes from supplying your own pops (no Sell button); an undelivered good earns nothing + strands | ✅ (2026-06-28) |
| **LR4** | Export + tariffs/chokepoints + diplomacy UI | `Economy.RunExport` — capital surplus sold to road-connected neighbours at their price, less importer tariff + transport; gated by stance (Hostile blockade / Neutral tariff / Friendly free). First-class `DiplomacyPanel` (press **N**) sets terms. (Tariff = importer's barrier; per-tile crossing tolls deferred.) | Export to a connected neighbor earns gold; Hostile blockades it; Friendly nets more than tariffed Neutral; panel sets terms | ✅ (2026-06-28) |
| **LR5** | Polish | `Economy.RunUpkeep` — recurring upkeep per building + per road tile (the gold sink), charged on the tax interval. (Vehicle-type variety deferred as future polish.) | Play ~10 min: build network, watch goods flow, money stays meaningful (upkeep bites) | ✅ (2026-06-28) |
| **LR-cleanup** | Retire abstract layer | `RunTrade` + `Nation.Stockpile` mirror writes removed; player uses `TileInventory` at capital as sole goods source; M-era economy tests rewritten. | Game earns income only from physical deliveries; no abstract arbitrage | ✅ (2026-06-29) |

---

## Milestone detail

### LR0 — Prep / cleanup (safe first step)
- **Goal.** Pay down terminology debt and the hotkey before the big changes, so later diffs are clean.
- **Adds.** Rename residual `province` → `nation`: `VoronoiDiagram.provincesDictionary` → a nation-tiles map,
  `VoronoiDiagram.CreateProvinceMap`, `Main.numProvinces`, the `Provinces` `Sprite2D` node in `main.tscn` +
  `ColoredProvincesSprite` + every `GetNode("../Provinces")`, and the "closest province center" comments.
  Rebind the diplomacy hotkey in `MapManager._UnhandledInput` from `Key.B` to `Key.N`.
- **Key files.** `Systems/Map/{VoronoiDiagram,ColoredProvincesSprite,MapManager}.cs`, `Systems/Game/Main.cs`,
  `Scenes/main.tscn`, plus any `GetNode`/string references.
- **Dependencies.** None. **Alive check.** Build 0/0; 99 tests green; headless smoke clean; in-engine identical
  behavior; diplomacy cycles on **N**; a grep for `province` (case-insensitive) finds only history/docs.

### LR1 — Player roads + connectivity (replace the MST)
- **Goal.** The player builds the network; trade only flows on it; cutting it isolates a region.
- **Adds.** Delete the MST path (`BuildMst`/`MstBuilder`/`DrawMSTLines`/`GetTilesAlongLine`, drop
  `PrimsAlgorithmSimple` from the flow). Promote road building: click-drag to lay cheap road tiles (reuse
  `RailroadBitmask` orientation + `road.tres`). New Core `RoadGraph` with BFS reachability. **Transitional:**
  keep the existing abstract trade but drive its routes/connectivity from `RoadGraph` (two nations trade iff
  their capitals are road-connected). Sell buttons stay until LR3.
- **Key files.** `Systems/Game/Main.cs` (remove MST), **new** `Systems/Core/Logistics/RoadGraph.cs` (+ tests),
  `Systems/Core/Economy/Economy.cs` (connectivity drives routes), `Systems/Map/MapManager.cs` +
  `UI/UIManager.cs` (drag road build), `Systems/ResourcePopup/TradeNetworkVisualizer.cs`.
- **Dependencies.** LR0. **Alive check.** Lay a road between two capitals → trade begins; cut a tile → it
  stops; no auto-backbone exists.

### LR2 — Physical goods + auto-haulers
- **Goal.** Goods stop teleporting — they physically travel the network.
- **Adds.** Tile inventories (producers emit onto their own tile). A* shortest-path + an auto-dispatcher
  (match surplus source → nearest connected demand) + **hauler entities** that travel the path over time
  carrying a load and unload at the sink. The visualizer renders haulers. Transport cost/time = path length;
  throughput = haulers. ~~Remove the instant `RunTrade` arbitrage.~~ **As built (2026-06-28): `RunTrade` is
  KEPT transitionally** (same call LR1 made — keeping the abstract layer so the suite stays green and the game
  stays balanced). Production *mirrors* into both `Nation.Stockpile` (abstract) and `TileInventory` (physical);
  the haulers are the visible physical truth. `RunTrade` + the mirror are retired in **LR3/LR4**, once income
  reads deliveries instead of the pool.
- **Key files.** **new** `Systems/Core/Logistics/{TileInventory,Pathfinder,Dispatcher}.cs` (+ tests), **new**
  `Systems/Map/Haulers.cs` (render/movement), `Economy.OnTick` (emit to tiles, not the pool).
- **Dependencies.** LR1. **Alive check.** A hauler visibly carries ore from a mine to a destination along your
  road; cutting the road strands the goods.

### LR3 — Internal market & income (remove Sell)
- **Goal.** Money comes from supplying your population, not a button.
- **Adds.** Capital settlement sink: pops **buy** goods haulers deliver → treasury income; remove `SellAll` +
  the Sell buttons. Migrate national-pool reads (pricing, consumption, tax, save/load) to per-tile + a capital
  inventory.
- **Key files.** `Economy.cs` (market sink; drop `SellAll`/`Nation.Stockpile`), `UI/UIManager.cs` +
  `Scenes/main.tscn` (drop Sell buttons; show deliveries/market), `Systems/SaveLoad/*` (persist tile
  inventories).
- **Dependencies.** LR2. **Alive check.** Deliver grain to your capital → income + fed pops; stop delivering →
  income drops + a visible shortage. No Sell button anywhere.

### LR4 — Export + tariffs/chokepoints + diplomacy UI
- **Goal.** The macro levers: trade abroad, tax/cut crossings, shape terms by diplomacy.
- **Adds.** Export sink: haul to a border connected to another nation → sell at their price. Tariffs/tolls
  when goods cross territory you control (own the tile a flow crosses → tax or cut = chokepoint). First-class
  diplomacy panel (reusing `DiplomaticStance`) gating export terms (friendly = free trade, hostile =
  blockade).
- **Key files.** `Economy.cs` (export sinks + tariffs at crossings), **new** diplomacy UI scene/script,
  `Systems/Map/MapManager.cs`.
- **Dependencies.** LR3. **Alive check.** Export to a connected neighbor earns gold; a tariff/chokepoint taxes
  or cuts a crossing; the diplomacy panel opens/closes trade.

### LR5 — Polish
- **Goal.** Make it feel good and keep money meaningful.
- **Adds.** Vehicle types (truck/train/ship by terrain/road tier), **recurring upkeep sinks** (the missing
  gold sink from `EconomyDesign.md`), and a balance pass toward the logarithmic curve.
- **Dependencies.** LR4. **Alive check.** ~10 min of play: build the network, watch goods flow, treasury
  stays meaningful (upkeep bites; no bloat).

---

## Verification (every milestone)

- **Core unit tests** (the existing `Tests/Tests.csproj` Core-linking pattern, headless): `RoadGraph`
  reachability (connected vs severed); pathfinding shortest path + "no path when cut"; dispatcher matches
  surplus → nearest demand; a delivered good produces income; a severed mine can't sell. Keep the suite green
  (currently **99**).
- **Headless smoke** (per the `godot-engine-available` memory): worldgen + economy heartbeat run with no MST;
  no exceptions.
- **Windowed checklist** (user's display, computer-use disabled): lay a road by click-drag; watch haulers
  carry goods from a mine to your capital; income rises as pops buy; cut a road tile → the hauler stops / a
  region starves; blockade a neighbour (**N**) → export stops.
- Append a dated [`../Build-History.md`](../Build-History.md) entry per milestone; update
  `Documentation/{TradeDesign,EconomyDesign}.md` (the per-tile physical + auto-hauler model refines "automate
  the micro") and agent memory.

## Open questions (carried from TradeDesign, decide as we reach them)

- The **strategic good** — what it is, what needs it (pop luxury? military input?), how rare.
- **Price scope** — one clearing price among connected nations, or a true per-node gradient.
- **Transport-cost shape** — flat per-edge, per-tile distance (current lean), or congestion-based.
- **Diplomacy depth** — single relations scalar vs treaties/embargoes/trade pacts.
- **Blockade granularity** — cut a whole route vs contest individual tiles (per-tile favors the chokepoint
  vision).
- **Hauler tuning** — speed, cargo size, how many spawn, how dispatch prioritizes competing demands.

## How to use this doc

- This is the **living plan** for the logistics rework (like `L1-Milestone-Roadmap.md` is for L1). Update it
  when scope shifts; it's a map, not a contract.
- Each finished milestone appends a dated section to [`../Build-History.md`](../Build-History.md).
- Source design lives in [`../Documentation/TradeDesign.md`](../Documentation/TradeDesign.md) +
  [`../Documentation/EconomyDesign.md`](../Documentation/EconomyDesign.md).
