# Whole-Codebase Review (2026-06-23)

Full review of the repo at the point after the tile refactor. Worst-first; tick items as you
address or consciously defer them. File refs are `file:line` at time of writing (may drift).

> **STATUS (resolved 2026-06-24):** Most of this backlog was addressed across two chunks — see
> `2026-06-24_M1.5_Cleanup_Review.md` (square-map fix, Voronoi `worldSeed` seeding, tile-source
> registry, `DetermineTileType`→`TileResolver`, naming sweep, Core/Godot split, xUnit tests,
> Border/GameTime deletion) and `2026-06-24_SaveLoad_Controls_Review.md` (per-click province scan,
> save/load, Controls hub). **Still deferred:** Voronoi brute-force perf, MST Prim's O(V³),
> TradeNetworkVisualizer wiring (M5). Highlight-restore is a non-issue until sand tiles exist.

---

## The "91% land" answer (reference)

Not a bug. Noise in [main.tscn:12-14](Scenes/main.tscn) is Perlin at `frequency = 0.0022` →
one noise period ≈ 455 tiles across a 512 map → the world is ~1 giant blob, so land/water
swings wildly per seed (this seed = 91.5% land). The `tiles check: OK` print proves the count.

- [ ] (optional) Raise `frequency` to ~0.01–0.03 for more/smaller landmasses.
- [ ] (optional) Raise sea level: `nV > 0.1` in [Initialize.cs:45](Systems/Game/Initialize.cs).
- [ ] (optional) Radial falloff for island-style maps.
- Note: `seamless=true` on the NoiseTexture2D is irrelevant — terrain samples `noise.GetNoise2D` directly, not the texture.

---

## 🔴 HIGH — do before/with save-load

- [ ] **Seed VoronoiDiagram from `worldSeed`.** [VoronoiDiagram.cs:47](Systems/Map/VoronoiDiagram.cs) places capitals with an **unseeded** `new Random()`; [VoronoiDiagram.cs:107](Systems/Map/VoronoiDiagram.cs) colors with an unseeded `RandomNumberGenerator`. → Same seed produces a *different* province map. **This breaks "one seed reproduces the world," which save/load is about to depend on.** Prerequisite for save/load.
- [ ] **Square-map assumptions (`X==Y==512`).** Latent transposition/clipping the day the map isn't square: FIX THIS I CHANGED THE DEFAULT MAP SIZE TO 256x128 in Main.cs
  - [Initialize.cs:37-39](Systems/Game/Initialize.cs) — loops use X-bound for Y and vice-versa.
  - [VoronoiDiagram.cs:128](Systems/Map/VoronoiDiagram.cs) — `pixelSizeY = mapSizeX`.
  - [ColoredProvincesSprite.cs:16](Systems/Map/ColoredProvincesSprite.cs) — hardcoded `512 * 64`.
  - [Camera.cs:24](Systems/Cameras/Camera.cs) + [Camera.cs:44](Systems/Cameras/Camera.cs) — `MapSizeX` used for Y.

---

## 🟠 MEDIUM

- [ ] **Voronoi is brute force** — [VoronoiDiagram.cs:67-95](Systems/Map/VoronoiDiagram.cs): ~240k tiles × 500 capitals ≈ 120M distance calls per world-gen (the bulk of load time). Spatial grid / jump-flood would slash it.
- [ ] **Per-click province scan** — [MapManager.cs:68](Systems/Map/MapManager.cs) LINQ-scans all 240k tiles per click via `AllCoordsWithCapital`. Replace with the precomputed `Economy.Provinces[capital].Tiles` (already held as `_economy`).
- [ ] **Dead code: [Border.cs](Systems/Map/Border.cs)** — whole file "Not used," stray `using System.Net.Http.Headers;`. DELETED
- [ ] **Dead/broken: [GameTime.cs](Systems/GameTime/GameTime.cs)** — `tickToMinute` double-divides (wrong); assumed old 30/sec model; now unused. DELETED
- [ ] **TradeNetworkVisualizer spawns random packets** unrelated to any real flow — eye-candy that misrepresents the economy. Wire to real edges at M5 or gate off. LEAVE ALONE FOR NOW

---

## 🟡 LOW (cleanups / fragility)

- [ ] **Magic tile-source IDs** (`0`=water,`1`=sand,`2`=grass,`3`=capital,`4`=highlight) hardcoded across Initialize/Bitmask/MapManager/VoronoiDiagram/RoadBitmask → one `static class TileSource`. GO AHEAD AND FIX THIS FOR READABILITY PURPOSES
- [ ] **Highlight restore bug** — [MapManager.cs:62](Systems/Map/MapManager.cs) restores tiles to source `2` (grass) unconditionally; sand tiles in a highlighted province get repainted grass. Store + restore the original source. SAND TILES AREN'T IMPLEMENTED SO THIS ISNT CURRENTLY AN ISSUE
- [ ] **`DetermineTileType` dead duplicate** — [Bitmask.cs:96 & 102](Systems/Map/Bitmask.cs) both map `0b01011000`→`botSpecial`; second is unreachable. Whole function is a brittle hand-rolled table. PROPOSE A FIX TO ME FOR THIS
- [ ] **GameContext god-object** + Law-of-Demeter reach-throughs (`gameContext.voronoiDiagram.placedCords`, etc.). Slim as part of save/load. HOW CAN WE AVOID THIS IN THE FUTURE?
- [ ] **Naming/consistency** — `Initalize` typo (pervasive); mixed namespaces (`ProceduralMapGenerator` vs global); MST in confusingly-named nested `Main.Example.Main()`. YEAH GO AHEAD AND FIX THESE ERRORS.
- [ ] **No tests.** Voronoi assignment, MST, bitmask, deposit flood-fill are pure logic = easy, high-value unit tests; would de-risk all the above. HELL YES MAKE SOME TESTS FOR THIS

---

## ✅ Keep (good as-is)
- MST → road → autotiling pipeline (Prim's + `RoadBitmask` cardinal mask + bridge variants).
- The tile-based `Systems/Economy/` refactor (clean separation, commented).
- Change-marker + review-doc process discipline.

---

## Suggested order before M2
1. **VoronoiDiagram `worldSeed` seeding** (HIGH — prerequisite for save/load determinism).
2. **Save/load working + GameContext slim** (next chunk).
3. Quick wins alongside #2: delete Border/GameTime; MapManager reuse `Economy` tiles; centralize tile-source constants.
4. Defer: Voronoi perf, square-map cleanup, tests.

---

## Fun backlog (design reference — not code TODOs)
1. **Supply lines = econ AND army** (the differentiator): capacity-limited roads carry both; cutting a road starves both. Make this the spine.
2. **Make the economy legible on the map**: tint provinces by price, pulse roads by congestion, flash depleted deposits. ~half the fun budget. (Highest-impact next-for-fun.)
3. **Scarcity + finite reserves + chokepoints** = the routing puzzle; spatial monopoly → economic casus belli.
4. **Every government lever has a real cost** (tax vs growth, build vs reserve) — no free lunch.
5. **Pressure or it goes stale**: PvE shocks on graph nodes/edges (dragon on the ore cluster) double as win/lose pressure.
6. **Bottleneck-driven unlocks** (rail when roads choke, refineries when tools run short) — earned relief.
7. **Scope discipline**: smaller, denser, more-varied map (chokepoints!) beats vast+sparse; find the fun in ONE loop (3 deposits, 2 workshops, 1 capacity road, visible prices, 1 shortage) before scaling.
