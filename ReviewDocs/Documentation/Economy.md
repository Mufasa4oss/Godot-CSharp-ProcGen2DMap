# The Economy

**Purpose:** the economic model — what goods, deposits, buildings, and nations are, how they're wired
together, how production runs each tick, and how the player builds. All of this lives in
`Systems/Core/Economy/` as **plain C# (no Godot Node)**, so it's unit-tested and engine-free.

> Status: **M4.5 (clarity + raw/refined split).** Goods are split **raw** (Grain/Ore) vs **refined**
> (Tools). Raw extraction is **build-gated** — the player builds a Mine/Farm on a deposit (no
> auto-placement) — and **throttled** (a flat 1 unit every other tick). The player spends a **treasury**
> to build extractors, Workshops (Ore→Tools), and railroads, and **sells** goods for income (M4). Each
> capital is one of 10 nations (1 player, 9 passive AI — no AI behaviour yet). See
> [`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md) for what's next.

---

## The types

### `Good` — [`../../Systems/Core/Economy/Good.cs`](../../Systems/Core/Economy/Good.cs)
The three L1 goods: `Grain`, `Ore`, `Tools`. (More arrive in later layers.) Each reports a
**`GoodCategory`** via the `good.Category()` extension (M4.5): `Grain`/`Ore` = **Raw** (extraction-gated —
flow only from a built Mine/Farm), `Tools` = **Refined** (build-gated — produced by a Workshop).

### `Deposit` — [`../../Systems/Core/Economy/Deposit.cs`](../../Systems/Core/Economy/Deposit.cs)
A resource node bound to one or more tiles.
- `Good` — what it yields. `Tiles` — the cluster. `Size => Tiles.Count`.
- `Rate` — extraction throughput per tick (∝ size). `Reserve` — how much is left before it runs out
  (∝ size); **mutable**, depletes as the deposit is worked.

### `Building` — [`../../Systems/Core/Economy/Building.cs`](../../Systems/Core/Economy/Building.cs)
A transformation node: consumes `Inputs[]`, produces `Outputs[]`, at `Rate`, for a build `Cost`. Recipe
table is `Building.Of(type)`:

| Type | Inputs | Outputs | Rate | Cost |
|------|--------|---------|------|------|
| `Farm` | — | Grain | 2 | `ExtractorCost` (50) |
| `Mine` | — | Ore | 2 | `ExtractorCost` (50) |
| `Workshop` | Ore | Tools | 1 | `WorkshopCost` (100) |

Farm/Mine are **extractors** (no inputs — they pull from a deposit). Workshop is the **input-consumer**.
**All three are player-built (M4.5)** — there's no auto-placement; extractors cost `ExtractorCost`, the
Workshop `WorkshopCost`. (Note: for raw extractors the deposit governs the pull and the M4.5 throttle
overrides `Building.Rate` — see Production below.) Rates/costs are placeholders to balance at M7.

### `Nation` — [`../../Systems/Core/Economy/Nation.cs`](../../Systems/Core/Economy/Nation.cs)
A government: one capital, the tiles it owns, its treasury, and its stockpile. (10 nations in L1; the
player is `Owner 0`, the rest AI. Territory grows via war later.)
- `Capital`, `Owner` (int; `0` = player), `bool IsPlayer => Owner == 0`, `Tiles` (the set it owns).
- `Treasury` (float) — money spent to build (M3).
- `Stockpile` (`Dictionary<Good,float>`) — goods this nation holds; `AddGood(good, amount)` is the single
  accumulate path (negative subtracts). Filled by production, drawn down by workshops, persisted.

### `NationStock` — [`../../Systems/Core/Economy/NationStock.cs`](../../Systems/Core/Economy/NationStock.cs)
A serializable snapshot of one nation's persisted state (Capital + Treasury + flat Grain/Ore/Tools). Lives
in Core so the save/restore methods stay Godot-free; see [SaveLoad.md](SaveLoad.md).

---

## The `Economy` graph — [`../../Systems/Core/Economy/Economy.cs`](../../Systems/Core/Economy/Economy.cs)

Built once from the Voronoi `provincesDictionary` (tile→capital) + the world seed. Key members:

| Member | Type | Meaning |
|--------|------|---------|
| `Nations` | `Dictionary<Vector2I, Nation>` | by capital |
| `PlayerNation` | `Nation` | the `Owner 0` nation (cached) |
| `Deposits` | `List<Deposit>` | every deposit (stable, seed-deterministic order) |
| `TileDeposits` | `Dictionary<Vector2I, Deposit>` | tile → the deposit on it |
| `TileBuildings` | `Dictionary<Vector2I, Building>` | tile → the building on it |
| `TileNation` | `Dictionary<Vector2I, Nation>` | tile → its owning nation (routes output) |
| `Railroads` | `HashSet<Vector2I>` | player-built railroad tiles (paid-for; persisted) |
| `PlayerProducedLastTick` / `PlayerConsumedLastTick` | `Dictionary<Good,float>` | the player's per-good flow last tick (for the UI) |
| `ProducedThisTick` | `List<(Vector2I,Good,float)>` | per-tick production events for the Godot "+N" gain flash (M4.5); engine-free data, no signals |

**Construction:** the constructor inverts tile→capital into nations (filling `Nation.Tiles` and
`TileNation`), assigns owners by capital placement order (`capitals[0]` → `Owner 0` = player; rest AI),
gives each a starting treasury, then `PlaceDeposits` flood-grows deposit clusters (see
[WorldgenPipeline.md](WorldgenPipeline.md) §5). **M4.5: no extractor is auto-placed** — `TileBuildings`
starts empty and the player builds extractors themselves.

---

## Player building — `TryBuild*`

The player's agency. All build paths share a private `TryBuild(tile, building)` that validates the tile is
a land tile the **player's** nation owns, has **no** building yet, and is **affordable**, then charges the
treasury and places the building (no-op `false` otherwise):

- **`TryBuildWorkshop(tile)`** — a Workshop (refines Ore→Tools) on any owned, empty tile.
- **`TryBuildExtractor(tile)`** (M4.5) — the matching extractor on a **deposit** tile: `Mine` on Ore,
  `Farm` on Grain (auto-picked from `TileDeposits[tile].Good`, so the UI needs no type picker). No-op if
  the tile has no deposit.
- **`TryBuildRailroad(tile)`** (M4.5) — lays one railroad tile for `RailroadCost`, recording it in
  `Railroads`. Dedupes (no double-charge on an existing tile) and skips the ownership check, since track
  may cross any territory to connect nations.

(The Godot side — [MapManager](EventSystem.md) build modes, mutually exclusive — calls these on a click;
a successful Workshop build also spawns its animated sprite, and each gain flashes a "+N" — see below.)

---

## Production — `OnTick()` (two passes)

Called once per game-second by the [TickLoop](TickLoop.md). Two passes so a workshop can consume what an
extractor produced the **same** tick:

1. **Extractors** (`Inputs.Length == 0`): **build-gated + throttled (M4.5)** — only a player-built
   Mine/Farm produces, and only on **alternating ticks** (`tickCount % 2 == 0`), pulling a flat
   `min(1, Reserve)` into the owning nation and depleting the reserve. The throttle deliberately bypasses
   `Deposit.Rate` so raw goods stay scarce (1 unit per 2 ticks).
2. **Workshops** (`Inputs.Length != 0`): if the owning nation's stockpile has every input at the full
   `Building.Rate`, consume the inputs and add the outputs (Workshop: −1 Ore, +1 Tools per tick). No
   partial runs.

Each tick also records the **player** nation's produced/consumed amounts per good into
`PlayerProducedLastTick` / `PlayerConsumedLastTick` (reset each tick) so the UI can show `+prod / −cons`,
and appends every production to **`ProducedThisTick`** (tile, good, amount) — engine-free data the Godot
view drains after the tick to flash a fading **"+N"** on the source building (M4.5 sub-rung 4).

Design rules worth knowing:
- **M4.5 throttle:** raw extractors yield a flat 1 unit on alternating ticks; `Deposit.Rate` is bypassed
  for raws (kept only for the reserve cap). `Building.Rate` still governs input-consumers (the Workshop).
- **`min(1, Reserve)`** caps the final partial tick so a deposit can't over-produce past its reserve.
- **Two passes** make same-tick supply deterministic (extractors before workshops); within a pass, order
  doesn't matter (each building touches only its own nation/deposit).
- **No RNG in `OnTick`** → production is fully deterministic given the seed and tick count. This is what
  lets the determinism test pass and what save/load relies on.

The alive-check heartbeat (global grain/ore/tools totals) is printed by the [TickManager](TickLoop.md).

---

## How to extend

- **Add a good:** extend the `Good` enum. Note `NationStock` and `Economy.ApplySavedState` flatten the
  three known goods explicitly for serialization — add the new good there too ([SaveLoad.md](SaveLoad.md)).
- **Add a recipe / building:** add a `BuildingType` and a `Building.Of` case (with a `Cost`). An
  input-consuming building runs in pass 2 of `OnTick` automatically (it's gated only by `Inputs.Length`).
- **Read economic state:** ask the `Economy` (`economy.PlayerNation.Stockpile`,
  `economy.Nations[cap]`, `economy.TileDeposits[tile]`). `MapManager`'s click-inspect is the worked
  example of reading deposit + building + owner for a clicked tile.
- **Persist new state:** anything that accumulates over ticks (not derivable from the seed — treasury,
  stockpiles, player-built buildings via `TileBuilding`, railroad tiles) must be added to `GameData` +
  `ApplySavedState` ([SaveLoad.md](SaveLoad.md)). Owners are recomputed from capital order, so they aren't
  saved.

---

## Tunables

Deposit placement: `DepositCountTarget=300`, `MinSize=1`, `MaxSize=5`, `OreShare=0.5`,
`ReservePerTile=100`, `RatePerTile=1` (`Rate` is now only the reserve seed — the M4.5 throttle pulls a flat
1/2-ticks regardless). Economy (M4.5 placeholders, tuned at M7): `Building.ExtractorCost=50`,
`Building.WorkshopCost=100`, `Economy.RailroadCost=10`, and `StartingTreasury = ExtractorCost` (50) — sized
so the first build is a real choice (one extractor, not two). Tune for density/longevity/affordability/pacing.
