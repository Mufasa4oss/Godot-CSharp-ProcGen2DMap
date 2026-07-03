# File & Dependency Map — Gameplay + UI

_Last updated 2026-07-03. Purpose: know what each gameplay/UI file does, where every
on-screen value comes from, and **what breaks what** before you trim a file._

How to read this: "reads X" = a hard runtime dependency (delete/rename X and this
breaks). Two wiring styles exist and fail differently:

- **C# constructor/`Initialize` wiring** — fails at *compile time* (safe: the build
  tells you). Most systems are wired this way through `Main.CreateObjs`.
- **`GetNode(path)` + `ResourceLoader.Load(path)`** — fails at *runtime* with a null /
  crash (dangerous: build is green, game crashes on boot). These are the real
  landmines and are listed in §7.

---

## 1. Boot order — the spine (`Systems/Game/Main.cs`)

`Scenes/main.tscn` → root node runs `Main.cs`. `Main._Ready()` is the one place
everything is constructed and wired. Order matters:

1. `Controls` (input hub) — added first so others can subscribe.
2. Seed chosen (new random, or `PendingLoad.Seed` on load).
3. `CreateObjs()` builds the whole world (see §3) and returns a `GameContext`.
4. `control.SetEconomy(economy)` — hands the HUD its data source.
5. On load: `economy.ApplySavedState(...)` overwrites regenerated state with the save.
6. Overlays spawned: `WorkshopSprites`, `StorageOverlay`, `Haulers`, `DepositOverlay`,
   `TerritoryOutline`, and the `TickManager.TickProcessed` → `GainFeedback` hook.
7. `DrawPlayerRoads()` paints saved road tiles onto `RailroadLayer`.

`Main._Process(delta)` → `tickManager.Update(delta)` is the **only** driver of the
simulation. Remove that call and the game freezes economically (rendering still runs).

---

## 2. Scene tree (`Scenes/main.tscn`) and the node-path contract

```
Main (Node2D, Main.cs)
├── 0zTileMapLayer (TileMapLayer)        ← ground data grid (transparent tiles)
│   ├── BordersLayer (TileMapLayer)      ← nation-highlight overlay (click tint)
│   └── RailroadLayer (TileMapLayer)     ← player road sprites
├── Camera2D (Camera.cs)                 ← pan/zoom
├── Nations (Sprite2D, ColoredNationsSprite.cs)  ← colored Voronoi overlay (Q toggles)
└── UIManager (Control, UIManager.cs)    ← the HUD
    └── PanelContainer / VBox
        ├── TreasuryLabel, IncomeLabel, PopLabel, GrainLabel, OreLabel, ToolsLabel
        └── RailroadButton, WorkshopButton, ExtractorButton, GranaryButton, WarehouseButton
```

These exact names are hard-coded in `GetNode` calls (§7). Renaming a node in the scene
without updating the code = **null-reference crash on boot**.

---

## 3. Core data model — `Economy` is the god-object

`Systems/Core/Economy/Economy.cs` owns essentially all gameplay state. Almost every UI
element and overlay reads it. Key members and who consumes them:

| Economy member | What it holds | Read by |
|---|---|---|
| `Nations` (capital→`Nation`) | treasuries, populations, AI stockpiles, price multipliers | UIManager rows, TickManager, export |
| `PlayerNation` | the Owner-0 nation | UIManager, MapManager, overlays |
| `TileDeposits` / `Deposits` | ore/grain deposits | DepositOverlay, MapManager inspect, save |
| `TileBuildings` | player-built Mine/Farm/Workshop/Granary/Warehouse | WorkshopSprites, StorageOverlay, DepositOverlay, save |
| `TileNation` | tile→owning nation (reverse map) | production routing, build ownership checks |
| `Railroads` | player road tiles | Haulers/Dispatcher, DrawPlayerRoads, export connectivity, save |
| `TileInventory` | **the player's physical goods** (on tiles) | Haulers renderer, `StoredAmountOf`, save |
| `Dispatcher` / `Haulers` | trucks in flight | Haulers renderer |
| `ProducedThisTick` | (tile,good,amount) events | GainFeedback "+N" flashes |
| `*LastTick` dicts (Produced/Consumed/Demand/InternalSold/Export) | per-tick flows | UIManager HUD lines |
| `IncomeByGood` / `TotalIncome`, `TaxCollected`, `UpkeepPaidLastInterval` | display stats | UIManager |

Supporting Core types (small, stable): `Nation`, `Deposit`, `Building` (+`BuildingType`),
`Good`, `NationStock`/`TileBuilding` (save DTOs), `RoadGraph`, `Pathfinder`, `Hauler`,
`Dispatcher`, `TileInventory`.

> **The #1 landmine (already bit us):** the player's goods live in `TileInventory`; the AI
> use `Nation.Stockpile`. **Always read a nation's goods via `Economy.StoredAmountOf(nation,
> good)`** — reading `Nation.Stockpile` directly for the player returns 0 and silently
> breaks the HUD/save/tests. Writes for the player go to `TileInventory` (at a tile); only
> AI use `Nation.AddGood`.

---

## 4. The tick loop

`Systems/GameTime/TickManager.cs`: `Update(delta)` accumulates real time, and every
`tickInterval` (1s) calls `economy.OnTick()` then fires `TickProcessed`. `OnTick` runs
production (extractors → workshops), demand, tax, upkeep, price recompute, the hauler
`Dispatcher`, internal market, and export — in that order.

Depends on: `Economy`. Consumed by: `Main._Process` (drives it) and the
`TickProcessed` subscriber in `Main` (drains `ProducedThisTick` → `GainFeedback`).

---

## 5. Input

`Systems/Controls/Controls.cs`: turns raw input (`project.godot [input]` actions) into
Godot signals (`SaveRequested`, `LoadRequested`, `NationsToggleRequested`,
`MapClickRequested`). Subscribers: `Main` (save/load), `MapManager` (map click, nations
toggle). `Systems/Cameras/Camera.cs` reads the pan/zoom actions directly (WASD / wheel /
middle-drag; `ZoomLimits` now 0.5–3×).

`Systems/Map/MapManager.cs`: on map click, **reads `UIManager.IsXBuildMode` flags** to
decide build-vs-inspect, then calls `Economy.TryBuild*`/`TryLayRoad`/`RemoveRoad` and
pokes the matching overlay (`WorkshopSprites`, `StorageOverlay`, `GainFeedback`) +
`ExportPanel`. So `MapManager` depends on: `UIManager`, `Economy`, `VoronoiDiagram`, and
the overlays + `ExportPanel` (all constructor-injected).

---

## 6. UI — every on-screen value and its source

### HUD — `UI/UIManager.cs` (reads `Economy` every frame in `_Process`)

| Label | Text | Data source (Economy unless noted) |
|---|---|---|
| Treasury | `Treasury: N` | `PlayerNation.Treasury` |
| Income | `Income: N (pops bought X/tick)` | `TotalIncome` (= Σ `IncomeByGood`); `InternalSoldLastTick` |
| Pop | `Pop: N (eats G grain/tick — tax +50/10t, earned…, upkeep…)` | `PlayerNation.Population`, `PlayerDemandLastTick[Grain]`, `TaxPerInterval`/`TaxIntervalTicks`, `TaxCollected`, `UpkeepPaidLastInterval` |
| Grain/Ore/Tools | `Name: amount/cap @price (+made / -used) — earned N` | `StoredAmountOf(player,g)`, `StorageCapacityOf(player,g)`, `PriceOf(g)`, `PlayerProducedLastTick`, `PlayerConsumedLastTick`, `IncomeOf(g)` |
| Nation rows | `▶ You / AI n:  G.. O.. T..` | `StoredAmountOf(nation, g)` per nation |
| Build buttons (text) | `Build X (cost)` | `Economy.RailroadCost`, `Building.WorkshopCost`, `Building.ExtractorCost`, `Building.Of(Granary/Warehouse).Cost` |
| Build buttons (state) | toggle → `IsRailroadBuildMode` … | **read by `MapManager`** to route clicks |

Note: `_Process` runs per-frame but the values only change per tick — cheap by design
(the goods lines now also compute `StorageCapacityOf`, a small per-build scan).

### Export panel — `UI/ExportPanel.cs` (opened by clicking an owned Granary/Warehouse)

| Field | Source |
|---|---|
| `— Export G (stored / capacity, N at capital) —` | `StoredAmountOf`, `StorageCapacityOf`, `TileInventory.AmountAt(capital, g)` |
| amount SpinBox max | `TileInventory.AmountAt(capital, g)` (what's actually sellable) |
| Export button | `Economy.TryExportManual(good, amount)` → result line |

---

## 7. `GetNode` / `ResourceLoader` landmines (green build, runtime crash)

| File | Hard path | Breaks if… |
|---|---|---|
| `Main.cs` | `"0zTileMapLayer"`, `"UIManager"`, `.../RailroadLayer` | those scene nodes renamed/removed |
| `Main.cs` | `res://Scenes/ResourcePopup.tscn` | popup scene moved |
| `MapManager.cs` | `"../Nations"`, `"../UIManager"`, `"../0zTileMapLayer/RailroadLayer"`, `"../0zTileMapLayer/BordersLayer"` | any of those nodes renamed |
| `UIManager.cs` | `PanelContainer/VBox/{TreasuryLabel…ToolsLabel, RailroadButton…WarehouseButton}` | any HUD child renamed/removed |
| `Camera.cs` | `res://Shaders/tr.gdshader` | shader moved |
| `WorkshopSprites.cs` | `res://Images/Blender & Krita Assets/Blender/SpriteFrames/{0000}.png` | those frames moved (dir is gitignored!) |
| `main.tscn` | `Systems/*` script `uid://…`, tileset `.tres` refs | a referenced script/tileset deleted |

---

## 8. Rendering / overlays (each is a leaf — safe-ish to trim individually)

All are constructed in `Main` and read `Economy` + the tilemap. Removing one drops a
*visual* only (no logic depends on them):

| File | Draws | Reads |
|---|---|---|
| `Systems/Rendering/GroundRenderer.cs` | the blended ground (shader) | `Initialize.TerrainMask` (not Economy) |
| `Systems/Map/Haulers.cs` | trucks + at-rest crates | `Economy.Haulers`, `Economy.TileInventory` |
| `Systems/Map/DepositOverlay.cs` | ore/grain dots | `Economy.TileDeposits`, `TileBuildings` |
| `Systems/Map/StorageOverlay.cs` | Granary/Warehouse markers | `Economy.TileBuildings` |
| `Systems/Map/TerritoryOutline.cs` | red border | `Economy.PlayerNation.Tiles` |
| `Systems/Map/WorkshopSprites.cs` | animated workshop | `Economy.TileBuildings` + the gitignored frames |
| `Systems/Map/GainFeedback.cs` | "+N"/"-cost" flashes | driven by `Main` draining `ProducedThisTick` |
| `Systems/Map/VoronoiDiagram.cs` | nation map texture + capitals | feeds `Economy` **and** `ColoredNationsSprite` (via `Main.MapTextureCreated`) |
| `Systems/Map/ColoredNationsSprite.cs` | the Q-toggle nation overlay | the texture from `VoronoiDiagram` |

`VoronoiDiagram` is **not** a pure leaf — `Economy` is built from `vD.nationsDictionary`
+ `vD.GetCapitals()`. Don't remove it; it seeds the whole economy.

---

## 9. Save / load

`Main` (F5→`InitSaveAndLoad.initializeSave`, F9→`Serialization.Load`+scene reload) →
`GameData` (DTO) ↔ `Serialization` (JSON + `SaveCrypto` AES) ↔ `Economy.ApplySavedState`.
World geometry is **regenerated from the seed**, not saved; only accumulated state
(reserves, treasuries, **player goods via `StoredAmountOf`**, buildings, roads) persists.
`Vector2IConverter` is required for JSON. Touching `Economy`'s goods model means touching
`InitSaveAndLoad` (save) **and** `ApplySavedState` (load) together.

---

## 10. Trim guide — what's safe, what's load-bearing

**Load-bearing (do NOT remove without a rewrite):** `Economy`, `Nation`, `Building`,
`Good`, `Deposit`, `TileInventory`, `Dispatcher`/`Hauler`/`Pathfinder`/`RoadGraph`,
`TickManager`, `Controls`, `Main`, `GameContext`, `VoronoiDiagram`, `Initialize`,
`MapManager`, `UIManager`, the save/load chain, and the scene nodes in §2/§7.

**Leaf overlays (safe to drop one at a time — you lose only its visual):** `Haulers`,
`DepositOverlay`, `StorageOverlay`, `TerritoryOutline`, `WorkshopSprites`, `GainFeedback`,
`ColoredNationsSprite` (+ the `Nations` node / Q toggle), `ResourcePopup` (splash).

**Already-removable dead code / candidates to verify:**
- `Systems/Core/Map/TileResolver.cs` — after the `Bitmask` deletion it's referenced only
  by `Tests/TileResolverTests.cs`. Orphaned in production; removable with its test.
- `Systems/Rendering/GroundBlend.cs` + `Scenes/GroundBlendTest.tscn` — a standalone test
  scene, not part of `main.tscn`. Verify then drop if unused.
- `MyGlobalClass.cs` — now only `GetSurroundingTiles` remains (used by `Economy` +
  deposit tests). Keep that method; the class is otherwise trimmed.

**Gotchas to know while trimming (verified, intentional — not bugs):**
- The storage cap (`StoredAmountOf >= StorageCapacityFor`) counts goods **stranded on
  producer tiles**, so a road-less extractor self-throttles at its cap and its reserve
  freezes. Intended ("build a road"). The HUD `amount/cap` shows *total* holdings, while
  pops eat / the market buys only from the **capital** — so a stranded pile can read
  "Grain 50/50" while pops starve. That's the roads lesson, not a bug.
- Scarcity price reads **capital-only** stock; the storage cap reads **whole-nation**
  total. They can disagree when goods are stranded (max price + paused extraction at once).
- `StoredAmountOf`/`TotalStored` assume exactly one player nation; fine today, would need
  per-nation scoping if a second player-flagged nation ever exists.
