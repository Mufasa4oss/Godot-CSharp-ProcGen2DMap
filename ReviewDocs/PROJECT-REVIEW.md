# Project Review — a top-to-bottom map of the codebase

**Who this is for:** you, the author, sitting down to actually *understand* the whole thing after a stretch
of fast building. It's an orientation doc: what each part is, where it lives, how to read it, and where the
rough edges are. It describes the code **as it is today (post-LR5, 2026-06-29)**.

> **Read-order TL;DR.** If you only do one thing: open [`../Systems/Core/Economy/Economy.cs`](../Systems/Core/Economy/Economy.cs),
> find `OnTick()`, and read *just the comment blocks* top to bottom. That one method is the whole simulation.
> Everything else either feeds it (worldgen) or draws it (rendering).

> **Heads-up on the older docs.** `ReviewDocs/Documentation/*` are good per-system guides but several predate
> the logistics rework and are **partly stale** — see [§10](#10-known-rough-edges--tech-debt). This doc is the
> current source of truth for the big picture; the source code always wins over any doc.

---

## 1. What the game is

A 2D strategy/economy game in Godot 4.7 (C#/.NET). One random **seed** grows a whole world — terrain, nations,
resource deposits. You play one nation's government: build extractors and workshops, **lay a road network by
hand**, and goods physically flow along it to your capital where your population buys them (income). Surplus is
exported to road-connected neighbors, with diplomacy setting the terms. A 1-second **tick** advances the
simulation.

The build history is in [`Build-History.md`](Build-History.md); the forward plan is the two roadmaps in
[`M-Doc/`](M-Doc/). The locked design vision is [`Documentation/TradeDesign.md`](Documentation/TradeDesign.md)
+ [`Documentation/EconomyDesign.md`](Documentation/EconomyDesign.md).

---

## 2. The one big idea: the Core / Godot split

This is the most important thing to internalize, because it explains the folder layout and why the tests work.

Every file is on one side of a hard line:

- **`Systems/Core/`** — **plain C#, no Godot `Node`.** Pure rules and math: the economy, logistics
  (roads/haulers), Voronoi assignment, terrain rules, tile resolving. It touches only the `Vector2I` *struct*
  from GodotSharp — nothing that needs the engine running.
- **`Systems/` (everything else) + `UI/`** — the **Godot adapter layer.** Nodes that draw, take input, run the
  loop, read files: `Main`, `MapManager`, the renderers/overlays, `Controls`, save/load, the HUD.

**Why it matters:** the test project [`Tests/Tests.csproj`](../Tests/Tests.csproj) compiles **only
`Systems/Core/**`** (plus GodotSharp for `Vector2I`). So Core is unit-testable headlessly, *and* if Core ever
takes a real engine dependency (a `Node`, a `GD.Print`, a `FileAccess`) the test project **stops compiling** —
an intentional tripwire that keeps the rules layer pure. (That's why `Economy.ApplySavedState` logs to
`System.Console.Error`, not `GD.PrintErr`.)

**Rule of thumb for new code:** rules/math/data → Core (+ a test). Anything that draws, takes input, sits in a
scene, emits signals, or reads files → the adapter layer, calling into Core.

---

## 3. The 30-second mental model

```
ONE SEED ──▶ WORLD (terrain, nations, deposits)        [built once, at startup]
                │
                ▼
        you build: extractors, workshops, ROADS
                │
   every 1s ──▶ TICK: make → consume → tax/upkeep → price → MOVE (haulers) → EARN (market + export)
                │
                ▼
        treasury goes up (income) and down (build + upkeep)
```

Goods currently live in **two layers at once** (this is the single most confusing thing, and it's
intentional/transitional — see [§6](#6-economy--logistics-the-core-of-the-game) and
[§10](#10-known-rough-edges--tech-debt)):
- **Abstract** — `Nation.Stockpile`, a pool number per good. The original model. Pricing, pop-eating, and the
  old inter-nation `RunTrade` read this.
- **Physical** — `TileInventory` + `Hauler`s. Goods sit on the tile that made them and ride trucks down your
  roads to the capital. This is what you *watch*, and what now drives income.

---

## 4. Boot sequence — how the game assembles itself

Entry point is set in [`../project.godot`](../project.godot): `run/main_scene` → `Scenes/main.tscn` → its root
script [`../Systems/Game/Main.cs`](../Systems/Game/Main.cs) (a `Node2D`). Godot calls `Main._Ready()`, which
runs startup in order:

1. **Grab the noise** from the exported `NoiseTexture2D`.
2. **Create the input hub first** — `controls = new Controls()` — so later systems share the one instance; wire
   its Save/Load signals.
3. **Decide the seed:** new game → `new Random().Next()`; load → the saved seed (so the *same* world regrows).
4. **`CreateObjs()`** — the composition step that builds the world + all systems in dependency order
   (worldgen → economy → tick → map/UI), then returns a `GameContext`.
5. **Restore state on load** — `tickManager` fields + `economy.ApplySavedState(...)`.
6. **Overlays + the physical layer** — `RebuildRoadRoutes()`, draw saved roads, then add the renderers:
   `TradeNetworkVisualizer`, `Haulers`, `DepositOverlay`, `TerritoryOutline`, and the **`DiplomacyPanel`** (on
   its own `CanvasLayer`).

**`GameContext`** ([`../Systems/Game/GameContext.cs`](../Systems/Game/GameContext.cs)) is the **composition
root** — a plain bag of references to every live system. It is *never serialized* (don't confuse it with the
save DTO `GameData`). Caveat: it trends toward a god-object; prefer injecting the specific collaborators a class
needs (as `MapManager`'s constructor does) over passing the whole bag.

---

## 5. Worldgen — one seed → a world

All triggered from `Main.CreateObjs()`, runs once at startup. Deterministic from the single `worldSeed`, which
is why a save only needs the seed + a few deltas.

| Step | Where | What happens |
|------|-------|--------------|
| **Terrain** | [`../Systems/Game/Initialize.cs`](../Systems/Game/Initialize.cs) `PlaceTiles()` + Core [`TerrainRules`](../Systems/Core/Map/TerrainRules.cs) | Two noise fields per cell — **height** (land/water + a thin beach band) and **moisture** (inland deserts). `TerrainRules.Classify(h, m)` → Water / Sand / Grass. Produces the land-tile list **and** a per-cell `TerrainMask` texture (R = sandiness, G = water) for the ground shader. |
| **Ground look** | [`../Systems/Rendering/GroundRenderer.cs`](../Systems/Rendering/GroundRenderer.cs) + [`../Shaders/terrain_ground.gdshader`](../Shaders/terrain_ground.gdshader) | One full-map `Sprite2D` at `ZIndex -100`; the shader blends seamless grass/sand textures from the mask. **The tilemap's ground tiles are transparent data-only cells** — the *look* is the shader, the *data* is the tilemap. (Don't "fix" the look by editing tiles.) |
| **Nations** | [`../Systems/Map/VoronoiDiagram.cs`](../Systems/Map/VoronoiDiagram.cs) + Core [`VoronoiAssignment`](../Systems/Core/Map/VoronoiAssignment.cs) | Shuffle land cells (seeded), pick `numNations` **capitals**, then assign every land tile to its nearest capital (brute-force Voronoi). Builds `nationsDictionary` (tile → capital) and a colored nation-map texture. |
| **Economy graph** | [`../Systems/Core/Economy/Economy.cs`](../Systems/Core/Economy/Economy.cs) ctor | Invert tile→capital into `Nation`s (capital + owned tiles). Owner by placement order: **capitals[0] = you (Owner 0)**, rest AI. Scatter ~300 ore/grain `Deposit`s (seeded sub-stream). |

The "look" vs "data" split (transparent tiles + shader ground + overlay highlight) is the gist of the
[`ground-render-architecture`] memory and worth remembering when something renders "wrong."

---

## 6. Economy & logistics — the core of the game

Everything is one method: **`Economy.OnTick()`**, run once per second. Read it top to bottom; each stage is one
loop or one helper call, each with a comment explaining *why*. The ordered stages:

1. **AI build** (`RunAiBuild`) — each AI nation auto-builds one extractor it can afford.
2. **Pass 1 — raw extraction** — player/AI Mines & Farms pull from deposits (throttled: 1 unit, alternate
   ticks). Player output is *mirrored* onto the producer's tile (`TileInventory`) — the physical layer.
3. **Pass 2 — workshops** — Ore → Tools (consume inputs, emit outputs; player output mirrored to its tile too).
4. **Pass 3 — pops eat grain** — demand drains `Stockpile`, floors at 0 (an empty larder + standing demand = a
   shortage that lifts the price).
5. **Tax & upkeep** (every `TaxIntervalTicks`) — `+TaxPerInterval` poll tax; `RunUpkeep` charges
   `UpkeepPerBuilding`/building + `UpkeepPerRoad`/road (the gold **sink**; floors treasury at 0).
6. **Recompute prices** (`RecomputePriceMultipliers`) — each nation prices each good off its own scarcity.
7. **`RunTrade`** — the **old** abstract inter-nation arbitrage over `Stockpile` (kept transitionally).
8. **Haulers** (`Dispatcher.Advance` then `.Dispatch`) — roll trucks (deliver/strand), then dispatch new ones to
   carry tile surplus toward the player capital.
9. **`RunInternalMarket`** — pops **buy** what was delivered to the capital → treasury income (the income faucet
   that replaced the old Sell button).
10. **`RunExport`** — leftover capital surplus sold to road-connected neighbors at their price, gated by
    diplomacy (Hostile blockade / Neutral tariff / Friendly free) and docked by distance.

### The logistics sub-system (Core, `Systems/Core/Logistics/`)
Pure and unit-tested, mirrors the worldgen Core pattern:
- [`RoadGraph`](../Systems/Core/Logistics/RoadGraph.cs) — your road tiles as a graph; 4-neighbor BFS for "are A
  and B connected?" + shortest road path. A capital is on-network if a road sits on/beside it.
- [`Pathfinder`](../Systems/Core/Logistics/Pathfinder.cs) — turns the road path into the **full walk** a truck
  follows (source tile → roads → destination), so a renderer can lerp a truck along it.
- [`TileInventory`](../Systems/Core/Logistics/TileInventory.cs) — goods at rest, per tile.
- [`Hauler`](../Systems/Core/Logistics/Hauler.cs) — one truck: a `Walk`, a `Progress`, a load; `PathAheadIntact`
  strands it if the road *ahead* is cut.
- [`Dispatcher`](../Systems/Core/Logistics/Dispatcher.cs) — the auto-hauling: `Dispatch` (surplus tile → its
  capital) + `Advance` (move/deliver/strand). One truck per origin at a time.

### The data model (the nouns — small files, read these to ground yourself)
| Type | File | Is |
|------|------|----|
| `Good` | [Good.cs](../Systems/Core/Economy/Good.cs) | Grain, Ore, Tools |
| `Deposit` | [Deposit.cs](../Systems/Core/Economy/Deposit.cs) | ore/grain in the ground; depleting `Reserve` |
| `Building` | [Building.cs](../Systems/Core/Economy/Building.cs) | Mine/Farm/Workshop = "inputs→outputs at Rate"; the recipe table |
| `Nation` | [Nation.cs](../Systems/Core/Economy/Nation.cs) | capital + tiles + `Treasury` + `Stockpile` + `Population` |
| `TradeRoute` | [TradeRoute.cs](../Systems/Core/Economy/TradeRoute.cs) | a capital↔capital link for the old abstract trade |
| `DiplomaticStance` | [DiplomaticStance.cs](../Systems/Core/Economy/DiplomaticStance.cs) | Friendly / Neutral / Hostile |

---

## 7. Rendering & overlays — drawing the simulation

The screen is a stack of independent layers, each a node that reads the economy and draws. None of them own
game state; they're all views.

| Layer | File | Draws |
|-------|------|-------|
| Ground (look) | `Systems/Rendering/GroundRenderer.cs` + shader | blended grass/sand terrain |
| Tilemap (data) | `Scenes/main.tscn` `0zTileMapLayer` | terrain *data* (transparent), capitals, the `RailroadLayer` |
| Nation colors | [`ColoredNationsSprite.cs`](../Systems/Map/ColoredNationsSprite.cs) | the Voronoi color map (toggle with **Q**) |
| Territory outline | [`TerritoryOutline.cs`](../Systems/Map/TerritoryOutline.cs) | red border around your nation |
| Deposits | [`DepositOverlay.cs`](../Systems/Map/DepositOverlay.cs) | a dot per deposit (worked/idle/depleted) |
| Abstract trade | [`TradeNetworkVisualizer.cs`](../Systems/ResourcePopup/TradeNetworkVisualizer.cs) | road routes colored by congestion + trade packets |
| **Physical haulers** | [`Haulers.cs`](../Systems/Map/Haulers.cs) | trucks crawling your roads + crate piles of at-rest goods |
| Build feedback | [`WorkshopSprites.cs`](../Systems/Map/WorkshopSprites.cs), [`GainFeedback.cs`](../Systems/Map/GainFeedback.cs) | workshop sprites; floating "+N"/"-cost" flashes |

Note: the abstract `TradeNetworkVisualizer` and the physical `Haulers` are two overlapping trade views — a
visible symptom of the [§10](#10-known-rough-edges--tech-debt) transitional duplication.

---

## 8. Input, UI, and the tick clock

- **Input map** lives in [`../project.godot`](../project.godot) `[input]` (F5 save, F9 load, **Q** toggle
  nations, LeftClick, mouse-wheel zoom, WASD pan).
- **[`Controls.cs`](../Systems/Controls/Controls.cs)** is the input hub: turns those raw actions into semantic
  **Godot signals** (`SaveRequested`, `MapClickRequested`, …). Godot signals (not static C# events) on purpose —
  they auto-drop when a node frees, so the load-flow scene reload can't leak handlers.
- **[`MapManager.cs`](../Systems/Map/MapManager.cs)** consumes the map-click signal: build-mode clicks (workshop
  / extractor / road), **left-drag to lay road, right-click to cut**, else inspect+highlight a nation.
- **[`Camera.cs`](../Systems/Cameras/Camera.cs)** reads the pan/zoom actions each frame.
- **[`UIManager.cs`](../UI/UIManager.cs)** is the HUD `Control`: treasury, income (with "pops bought X/tick"),
  pop/tax/**upkeep**, per-good stockpile + price, build toggles, all-nations panel. (Sell buttons were removed
  in LR3.)
- **[`DiplomacyPanel.cs`](../UI/DiplomacyPanel.cs)** — press **N**: per-nation Friendly/Neutral/Hostile buttons
  + road-link status; sets export terms.
- **[`TickManager.cs`](../Systems/GameTime/TickManager.cs)** — accumulates `delta`; every `tickInterval` (1s)
  calls `economy.OnTick()`, fires the `TickProcessed` event (views drain per-tick data here), and prints the
  heartbeat (`tick N | grain=… ore=… tools=…`).

---

## 9. Save / load — a seed plus deltas

The clever bit: **loading does not deserialize the map.** It re-runs worldgen from the saved seed (identical
world) and overlays the few things that actually changed.

- **Save (F5)** — [`InitSaveAndLoad.initializeSave()`](../Systems/SaveLoad/InitSaveAndLoad.cs) packs a
  [`GameData`](../Systems/SaveLoad/GameData.cs) DTO: seed + tick + noise params, plus the *deltas* — deposit
  reserves, per-nation treasury+stockpile, player-built buildings, road tiles. [`Serialization.Save`](../Systems/SaveLoad/Serialization.cs)
  writes a **plain `SEED <n>` header + an AES-encrypted JSON body** ([`SaveCrypto`](../Systems/Core/SaveCrypto.cs), M8).
- **Load (F9)** — `Serialization.Load` decrypts → stashes `GameData` in the `Main.PendingLoad` **static** →
  `ReloadCurrentScene()`. The static survives the reload; `_Ready` reads it, regenerates from the seed, then
  `ApplySavedState` overlays the deltas.

**Not persisted (regenerate or reset on load):** capitals (from seed), `TileInventory`/haulers (regenerate from
production), diplomatic relations (reset to Neutral), and display stats (income/tax/upkeep counters).

---

## 10. Known rough edges / tech debt

Being honest here so you can dig with eyes open:

1. **The transitional duplication (the big one).** To keep the suite green and the game balanced, the **abstract
   layer was kept alongside the physical one**: production mirrors into both `Nation.Stockpile` and
   `TileInventory`, and the old `RunTrade` still runs. So a produced unit is counted twice, and the player can
   earn via *both* `RunTrade` (abstract) and `RunExport` (physical). It's bounded and error-free, but **retiring
   `RunTrade` + `Nation.Stockpile` in favor of per-tile + capital inventory is the one real remaining cleanup.**
   It's deferred because it needs the `Stockpile`-based economy tests
   (`EconomyProduction/Demand/Trade/Capacity`) rewritten.
2. **Stale per-system docs.** [`Documentation/Architecture.md`](Documentation/Architecture.md) (and likely
   `Economy.md`, `WorldgenPipeline.md`) still say `BuildMst()`, "provinces", `vD.provincesDictionary`,
   `ColoredProvincesSprite` — all renamed/removed by LR0–LR1 (MST gone; "province" → "nation"). Trust *this* doc
   + the source over them until they're refreshed.
3. **`GameContext` god-object tendency** — convenient bag, but invites reach-throughs. Inject specific
   collaborators in new code.
4. **Dead LR1 WIP in the tree** — an unused `_lastRoadDragTile` field in `MapManager`, plus an uncommitted LR1
   corridor-path refinement (`Economy.RebuildRoadRoutes` / `RoadGraph.ShortestPath`) riding along uncommitted.
5. **Naming drift** — `project.godot` still names the app "ProceduralMapGeneratorC#"; the assembly is
   `ProceduralMapGenerator`; some files keep older names. Cosmetic, but confusing when grepping.
6. **Headless can't test interaction** — the smoke test proves load/compile/tick, but building, road-laying, and
   the panels need the windowed game (you verify those by hand).

---

## 11. A reading path (do this, in order)

1. **This doc, §2–§6.** The split, the boot, the tick.
2. **`Economy.OnTick()` comments only.** ~2 min. The spine.
3. **The nouns** — `Good`, `Deposit`, `Building`, `Nation` (~40 lines each).
4. **Pick one tick stage** and read its helper end to end (`RunInternalMarket` is a gentle start; `RunExport`
   next). Then open the matching test file and read the **test names** — each is a plain-English spec of what
   that stage must do.
5. **One worldgen file** — `Initialize.PlaceTiles()` + `TerrainRules.Classify` — to see one seed become land.
6. **One renderer** — `Haulers.cs` — to see how a view reads the economy without owning it.
7. **The save round-trip** — `InitSaveAndLoad` → `Serialization` → `Main._Ready` load branch — to see the
   "seed + deltas" trick.

The **tests are your leverage**: 128 of them, each named like a sentence. You can audit *intent* in minutes
without trusting any implementation. When something confuses you, find the test — it tells you what the code is
*supposed* to do.

---

## 12. Where everything lives (file map)

| Area | Files |
|------|-------|
| Bootstrap / composition | `Systems/Game/{Main,GameContext,Initialize}.cs` |
| Worldgen | `Systems/Map/VoronoiDiagram.cs`, `Systems/Core/Map/{TerrainRules,VoronoiAssignment,TileResolver,Terrain*}.cs`, `Systems/Map/Bitmask.cs` |
| Economy (Core) | `Systems/Core/Economy/*` |
| Logistics (Core) | `Systems/Core/Logistics/*` |
| Clock | `Systems/GameTime/TickManager.cs` |
| Input | `Systems/Controls/Controls.cs`, `Systems/Cameras/Camera.cs`, `project.godot [input]` |
| UI | `UI/UIManager.cs`, `UI/DiplomacyPanel.cs` |
| Rendering / overlays | `Systems/Rendering/GroundRenderer.cs`, `Systems/Map/{ColoredNationsSprite,DepositOverlay,TerritoryOutline,Haulers,WorkshopSprites,GainFeedback,RailroadBitmask}.cs`, `Systems/ResourcePopup/*`, `Shaders/*` |
| Save / load | `Systems/SaveLoad/*`, `Systems/Core/SaveCrypto.cs` |
| Tests | `Tests/*` (compiles `Systems/Core/**` only) |
| Docs | `ReviewDocs/` — this file, `Build-History.md`, `M-Doc/*` (plans), `Documentation/*` (per-system; partly stale) |

---

*Generated 2026-06-29 as a current-state orientation. When it disagrees with the source, the source wins —
and please fix whichever drifted.*
