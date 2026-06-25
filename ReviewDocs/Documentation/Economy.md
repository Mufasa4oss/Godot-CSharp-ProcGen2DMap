# The Economy

**Purpose:** the economic model — what goods, deposits, buildings, and nations are, how they're wired
together, how production runs each tick, and how the player builds. All of this lives in
`Systems/Core/Economy/` as **plain C# (no Godot Node)**, so it's unit-tested and engine-free.

> Status: **M3 (agency + economy UI).** Extractors (Mine/Farm) produce into the owning **nation's**
> stockpile and deplete deposit reserves; the player spends a **treasury** to build **Workshops**
> (Ore→Tools). Each capital is one of 10 nations (1 player, 9 passive AI). See
> [`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md) for what's next.

---

## The types

### `Good` — [`../../Systems/Core/Economy/Good.cs`](../../Systems/Core/Economy/Good.cs)
The three L1 goods: `Grain`, `Ore`, `Tools`. (More arrive in later layers.)

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
| `Farm` | — | Grain | 2 | 0 |
| `Mine` | — | Ore | 2 | 0 |
| `Workshop` | Ore | Tools | 1 | 100 |

Farm/Mine are **extractors** (no inputs — they pull from a deposit; auto-placed, cost 0). Workshop is the
**input-consumer** the player builds (M3). Rates/costs are placeholders to balance at M7.

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
| `PlayerProducedLastTick` / `PlayerConsumedLastTick` | `Dictionary<Good,float>` | the player's per-good flow last tick (for the UI) |

**Construction:** the constructor inverts tile→capital into nations (filling `Nation.Tiles` and
`TileNation`), assigns owners by capital placement order (`capitals[0]` → `Owner 0` = player; rest AI),
gives each a starting treasury, then `PlaceDeposits` flood-grows deposit clusters and seats an extractor
on each (see [WorldgenPipeline.md](WorldgenPipeline.md) §5).

---

## Building a workshop — `TryBuildWorkshop(tile)`

The player's M3 agency. Returns `true` (and mutates) only if the tile is a land tile the **player's**
nation owns, has **no** building yet, and the player can **afford** the workshop; otherwise it's a no-op
returning `false`. On success it deducts `Building.Of(Workshop).Cost` from `PlayerNation.Treasury` and
adds the workshop to `TileBuildings`. (The Godot side — [MapManager](EventSystem.md) build mode — calls
this on a click and spawns the sprite on success.)

---

## Production — `OnTick()` (two passes)

Called once per game-second by the [TickLoop](TickLoop.md). Two passes so a workshop can consume what an
extractor produced the **same** tick:

1. **Extractors** (`Inputs.Length == 0`): pull `min(Deposit.Rate, Reserve)` into the owning nation
   (`TileNation[tile].AddGood(output, amount)`), deplete the reserve.
2. **Workshops** (`Inputs.Length != 0`): if the owning nation's stockpile has every input at the full
   `Building.Rate`, consume the inputs and add the outputs (Workshop: −1 Ore, +1 Tools per tick). No
   partial runs.

Each tick also records the **player** nation's produced/consumed amounts per good into
`PlayerProducedLastTick` / `PlayerConsumedLastTick` (reset each tick) so the UI can show `+prod / −cons`.

Design rules worth knowing:
- **Deposit governs extractor throughput + reserve; `Building.Rate` governs input-consumers** (the
  Workshop). Avoids two competing rates on one extractor.
- **`min(Rate, Reserve)`** caps the final partial tick so a deposit can't over-produce past its reserve.
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
  stockpiles, player-built workshops) must be added to `GameData` + `ApplySavedState`
  ([SaveLoad.md](SaveLoad.md)). Owners are recomputed from capital order, so they aren't saved.

---

## Tunables

Deposit placement: `DepositCountTarget=300`, `MinSize=1`, `MaxSize=5`, `OreShare=0.5`,
`ReservePerTile=100`, `RatePerTile=1` (a 1-tile deposit holds 100 and mines 1/tick → ~100 ticks; size
scales both rate and reserve). Economy: `StartingTreasury=500` per nation; Workshop `Cost=100` (in the
recipe table). Tune for density/longevity/affordability.
