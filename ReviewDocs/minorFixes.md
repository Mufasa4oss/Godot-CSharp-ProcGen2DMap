# minorFixes

Backlog of flaws, inefficiencies, and cleanups found in a whole-codebase pass (2026-06-24, after the
save/load + Controls chunk). No game-breaking bugs — it runs, saves/loads, tests pass. This is
performance, dead code, and fragility. Tick items as you address or consciously defer them.

File refs are `file:line` at time of writing (may drift).

---

## 🔴 Performance — the one that bites

- [ ] **Prim's MST is O(V³) — this is the startup stall at high province counts.**
  [Main.MstBuilder.Build()](../Systems/Game/Main.cs) builds a *complete* graph (V² edges in a `List`),
  then [PrimsAlgorithmSimple.FindMST()](../Systems/Core/Pathfinding/PrimsAlgorithmSimple.cs) re-scans
  **all** edges every step → V × V² = O(V³). At V=500 ≈ 125M ops in `_Ready`, blocking the first frame
  ~1–2 s (the "pan delay" returns if provinces scale up). At np=10 it's nothing.
  **Fix:** dense Prim's — keep a `minDistToTree[]` array, pick the nearest unvisited vertex each step
  → O(V²). No external data structures needed. Covered by `MstTests` already.

## 🟠 Performance — smaller, real

- [ ] **Voronoi uses `DistanceTo` (sqrt) in the hot loop.**
  [VoronoiAssignment.AssignNearest](../Systems/Core/Map/VoronoiAssignment.cs) compares with `DistanceTo`
  (a `sqrt` per call) over land-tiles × capitals (~5M at scale). Switch to `DistanceSquaredTo` — same
  nearest-capital result, no sqrt. (Bigger win — spatial grid / jump-flood — stays deferred.)
- [ ] **`colorSet.ElementAt(i)` is O(V²).**
  [VoronoiDiagram.cs:132](../Systems/Map/VoronoiDiagram.cs) indexes a `HashSet<Color>` by position in a
  loop (`ElementAt` on a HashSet is O(n)). Have `CreateUniqueColorSet` return a `List<Color>` instead.
- [ ] **`Camera.shaderMat` does dead work every frame.**
  [Camera.cs](../Systems/Cameras/Camera.cs) creates a `ShaderMaterial`, loads a shader, and calls
  `SetShaderParameter` every frame in `_Process` — but it's never assigned to any node's `Material`, so
  it renders nothing. Remove it entirely (deletes per-frame waste + dead code).

## 🟡 Dead / stale code

- [ ] **`MyGlobalClass.ArrayConvertToWorldCoords` + `ConvertToWorldCoords` are unused**
  ([Systems/Core/MyGlobalClass.cs](../Systems/Core/MyGlobalClass.cs)) — and `ArrayConvertToWorldCoords`
  has an O(n²) `ElementAt`-in-loop (moot since nothing calls it). Delete both.
- [ ] **`UIManager` is vestigial from the old 7-resource system** ([UI/UIManager.cs](../UI/UIManager.cs)):
  empty `UpdateWoodDisplay/Stone/Grain`; Wood/Stone labels don't match current goods (Ore/Grain/Tools).
  Real rework belongs at M7 (inspector) — flagged so it doesn't mislead.
- [ ] Minor: unused `using System.Runtime.CompilerServices;` + stale `//GD.Print(size);` comment in
  Camera; unused `lastColor` in VoronoiDiagram; `TickManager` `GD.Print` every tick (gate before ship).
- [ ] **`Economy.OnTick`** loops over every province doing nothing each tick — harmless; it's the M2 hook.

## 🟡 Fragility worth knowing

- [ ] **`ColoredProvincesSprite` uses a `static` event + relies on `_Ready` ordering.**
  It subscribes to `Main.MapTextureCreated` in `_Ready`; Main fires it during `_Ready`. Works only
  because Godot runs child `_Ready` before parent. Same static-event footgun as Controls; safe today but
  brittle (and interacts with load-time scene reload). A direct call or a signal would be sturdier.
- [ ] **`Bitmask` has its own private `GetSurroundingTiles`** duplicating
  `MyGlobalClass.GetSurroundingTiles` — same code in two places.
- [ ] **`Deposit.Tiles` / `Province.Tiles` exposed as mutable `HashSet`** — any consumer can mutate the
  economy's internals. Consider read-only exposure later.
- [ ] **Cross-version save note:** `System.Random` isn't guaranteed identical across .NET *versions*, so
  a saved seed could regenerate a slightly different world on a different runtime. Fine within a shipped
  build (fixed runtime); don't rely on seeds being portable across engine/runtime upgrades.

---

## Suggested order (when you want a polish pass)
1. Prim's → O(V²) (#🔴) — removes the startup stall, unlocks higher province counts.
2. Voronoi `DistanceSquaredTo` + colorSet `List` (#🟠) — tiny, satisfying wins.
3. Dead-code sweep (#🟡) — ~10 min.
4. Defer the fragility items until they actually bite (or fold into the relevant milestone).

None of this blocks **M2 (production)**.
