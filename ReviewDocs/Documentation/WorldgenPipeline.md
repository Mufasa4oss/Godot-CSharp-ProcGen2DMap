# Worldgen Pipeline

**Purpose:** how a single integer seed becomes a complete, reproducible world — terrain, provinces,
roads, and resource deposits. Everything below is deterministic from `Main.worldSeed`; the same seed
always produces the same world, which is what makes save/load tiny (see [SaveLoad.md](SaveLoad.md)).

The chain runs inside `Main.CreateObjs()` (see [Architecture.md](Architecture.md)):

```
seed ─► terrain (noise)  ─► autotiling ─► provinces (Voronoi) ─► roads (MST) ─► deposits (Economy)
```

---

## 0. The seed and sub-seeds

`Main` holds one `worldSeed` (a fresh `new Random().Next()` for a new game, or the saved value on load).
It is pushed to the noise (`noiseTexture.Set("seed", worldSeed)`) **and** passed to `VoronoiDiagram` and
`Economy`. Subsystems that need their *own* randomness derive a **distinct, deterministic sub-seed** from
the one world seed by hashing, so their output is reproducible but not pattern-correlated with each other
or the terrain:

```csharp
// e.g. Economy.DeriveSeed:
unchecked { return worldSeed * (int)0x9E3779B1 + 0x7F4A7C15; }
```

`VoronoiDiagram` does the same for its province colors (`DeriveColorSeed`). All randomness uses seeded
`System.Random` (not Godot's RNG) so the logic is reproducible *and* runnable in plain unit tests.

---

## 1. Terrain — noise → land/water

[`../../Systems/Game/Initialize.cs`](../../Systems/Game/Initialize.cs) `PlaceTiles()` walks every tile of
the map and samples the Perlin noise:

```csharp
for (int y = 0; y < msy; y++)          // height
  for (int x = 0; x < msx; x++) {      // width  (y-outer, x-inner so a non-square 256x128 map isn't transposed)
    float nV = noise.GetNoise2D(x, y);
    if (nV > 0.1f) { tml.SetCell(t, TerrainTiles.Grass, …); mapCoordsLand.Add(t); }  // land
    else           { tml.SetCell(t, TerrainTiles.Water, …); }                          // water
  }
return mapCoordsLand;                   // the land tiles feed every later stage
```

`0.1f` is the **sea level** — raise it for more water, lower for more land. The noise `frequency` (in
`main.tscn`) controls landmass size: low frequency (~0.0022) = one giant blob; higher (~0.01–0.03) =
more, smaller islands. (The "91% land" some seeds show is just the giant-blob effect, not a bug.)

---

## 2. Autotiling — choosing each tile's graphic

A grass tile's *graphic* depends on which neighbours are land (so coastlines look right).
[`../../Systems/Map/Bitmask.cs`](../../Systems/Map/Bitmask.cs) `OrientCells()` computes, per land tile, an
8-bit mask of its surrounding tiles, then asks the Core resolver which atlas tile to use.

[`../../Systems/Core/Map/TileResolver.cs`](../../Systems/Core/Map/TileResolver.cs) is the pure lookup:
a grass tile's graphic depends **only on its 4 cardinal neighbours** (diagonals are ignored), so there
are exactly **2⁴ = 16 cases**, one per entry in a flat table indexed by `(N|E|S|W)`:

```csharp
public static string FromCardinals(int cardinals) => Table[cardinals & 0b1111];
// 0 = "island" (no land neighbours) … 15 = "surrounded" (all four)
```

This replaced an older order-dependent stack of overlapping `(mask & X) == 0` checks (which had a dead
duplicate branch). The 16-case table is exhaustive, order-independent, and unit-tested.

**Tile registry** (`Systems/Core/Map/`): magic source IDs are gone — tiles are named constants.
- `TileId` — a `(SourceId, …)` value type identifying a tileset entry.
- `TerrainTiles` — `Water`, `Sand`, `Grass`, `Capital`, `Highlight`.
- `RoadTiles` — `Rails` and the road-orientation variants.

Use these constants (e.g. `tml.SetCell(tile, TerrainTiles.Grass, atlas)`) — never raw source ints.

---

## 3. Provinces — Voronoi over capitals

[`../../Systems/Map/VoronoiDiagram.cs`](../../Systems/Map/VoronoiDiagram.cs):

1. **Place capitals.** `numProvinces` capital tiles are chosen from the land tiles with a seeded
   `System.Random(worldSeed)` (reproducible per seed).
2. **Assign every land tile to its nearest capital.** The pure part is lifted into Core:
   [`../../Systems/Core/Map/VoronoiAssignment.cs`](../../Systems/Core/Map/VoronoiAssignment.cs)
   `AssignNearest(tiles, capitals)` returns the **`provincesDictionary`** — a `Dictionary<Vector2I,
   Vector2I>` mapping each tile → its capital. That dictionary is the bridge into the economy.
3. **Color** each province (seeded via `DeriveColorSeed`) and paint a province texture for the overlay
   sprite ([ColoredProvincesSprite — see EventSystem.md](EventSystem.md)).

> Perf note: assignment is brute force (every land tile × every capital, using `DistanceTo`). Fine at low
> province counts; a spatial grid / jump-flood is the deferred upgrade (see [`../minorFixes.md`](../minorFixes.md)).

---

## 4. Roads — the MST backbone

Roads follow the **minimum spanning tree** connecting all capitals (the "permanent highway network" in
the design notes). In `Main`:

1. `MstBuilder` builds a fully-connected graph of capitals weighted by straight-line distance and runs
   Prim's: [`../../Systems/Core/Pathfinding/PrimsAlgorithmSimple.cs`](../../Systems/Core/Pathfinding/PrimsAlgorithmSimple.cs).
   `FindMST()` repeatedly takes the smallest edge joining a visited to an unvisited vertex.
2. `DrawMSTLines()` walks each MST edge as a railroad-style L-path (one straight run, a single 90° corner,
   another straight run — avoids diagonal stair-stepping), paints `RoadTiles.Rails`, and
   [`../../Systems/Map/RoadBitmask.cs`](../../Systems/Map/RoadBitmask.cs) `OrientRoadCells()` autotiles
   the rails into straights/corners/junctions/bridges.

> Perf note: the current Prim's is brute-force **O(V³)** (rescans all V² edges each of V steps). At high
> province counts this dominates startup; the dense O(V²) rewrite is in [`../minorFixes.md`](../minorFixes.md).

---

## 5. Deposits — resources on tiles

Finally [`../../Systems/Core/Economy/Economy.cs`](../../Systems/Core/Economy/Economy.cs) places resource
**deposits** (see [Economy.md](Economy.md) for the full model). Driven by a seeded
`System.Random(DeriveSeed(worldSeed))`:

- Pick a random land tile, **flood-grow** an 8-neighbour cluster of unused land up to a random size
  (1–5), tag it Ore or Grain (`OreShare` split), and create a `Deposit` whose `Reserve` and `Rate` scale
  with cluster size. Repeat to `DepositCountTarget`.
- Auto-place an extractor on each deposit's start tile: a **Mine** on ore, a **Farm** on grain.

`DepositOverlay` then draws a gray dot on ore tiles and a neon-yellow dot on grain tiles.

---

## Putting it together

After `CreateObjs()` returns, the world is fully built and self-consistent — `Main` prints
`tiles check: OK` to confirm the province tiles sum back to the land-tile count. Because every stage is
seeded from the one `worldSeed`, re-running the whole pipeline with the same seed reproduces an identical
world — the foundation that [SaveLoad.md](SaveLoad.md) relies on.
