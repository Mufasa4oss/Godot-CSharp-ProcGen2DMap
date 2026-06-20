# Development Summary — 2026-06-20

## Road System: MST-Based Road Generation

This session added an automatic road network that follows the Minimum Spanning Tree (MST) connecting province capitals, plus a manual "Build Road" toggle for hand-painting roads. Roads use proper directional tiles (straights + corners) that orient themselves based on their neighbors.

---

## Files Changed

### `UI/UIManager.cs` (edited)
- Added `public bool IsRoadBuildMode { get; private set; }` — exposes the toggle state to other systems.
- Wired the existing "Build Road" `Button` to its `Toggled` signal in `_Ready()`.
- On toggle, the button **modulates green** when active so the player can see it's on (the default theme had no visible pressed state).

### `Systems/Map/MapManager.cs` (edited)
- Added node references: `_uiManager` (`../UIManager`) and `_roadLayer` (`../0zTileMapLayer/RoadLayer`), resolved in `_Ready()`.
- In `_UnhandledInput()`, the left-click handler now checks `IsRoadBuildMode` first. If on, it paints a single road tile and `return`s early — otherwise it falls through to the normal province-highlight logic.

### `Systems/Game/Main.cs` (edited)
- Replaced the old `_Draw()` MST line rendering with `DrawMSTLines()`.
- Added `GetTilesAlongLine()` — a Bresenham line walk that converts each MST edge (capital → capital) into the list of tile coordinates the road passes through.
- Excludes capital tiles from the road set so roads don't draw on top of capitals.
- Calls `RoadBitmask.OrientRoadCells()` to pick the correct tile variant for each road piece.

### `Systems/Map/RoadBitmask.cs` (NEW FILE)
- A **separate, self-contained** class for road tile orientation — deliberately kept apart from `Bitmask.cs` so the terrain autotiling is never at risk.
- Uses a 4-bit cardinal mask (up/down/left/right) to choose between 6 tiles: horizontal, vertical, and four corners.
- Treats both road tiles **and capital tiles** as valid connections, so roads bend correctly toward a capital they touch.

### Editor changes (made by you, not in code)
- `Scenes/main.tscn` — `RoadLayer` was given a TileSet.
- `tilesets/road.tres` (NEW) — the 6-tile road atlas (2 straights + 4 corners) in a single row.

---

## Concepts Worth Understanding

These came up directly while building the feature and are the kind of thing worth internalizing.

### 1. Bresenham's Line Algorithm
A straight line between two points isn't obvious on a grid — you have to decide which cells it passes through. Bresenham walks from start to end one cell at a time, keeping a running "error" value to decide whether to step horizontally, vertically, or both. It's integer-only (no rounding bugs), hits every tile once, and leaves no gaps. It's the same math GPUs use to rasterize lines into pixels — a tilemap is just a grid with big pixels.

**The diagonal-gap gotcha:** when the algorithm steps *both* x and y in one iteration, it jumps diagonally and skips the corner tile that visually connects the two segments. We fixed this by detecting that double-step and inserting the missing corner cell, so the road stays visually connected through diagonals.

### 2. Bitmasking / Autotiling
Instead of manually placing every corner and straight, each tile looks at its neighbors and **computes a number** (a bitmask) describing which sides have a connection. That number maps to the correct sprite. Terrain uses an 8-direction mask (corners matter for blobs); roads only need a 4-direction cardinal mask (roads are lines, so diagonals don't connect). This is why roads needed only 6 tiles, not 16.

### 3. Godot Draw Order — why the MST lines vanished
`_Draw()` paints directly onto the node it lives on. Because the TileMapLayers are *children* drawn afterward, they covered the lines. The fix was to stop using `_Draw()` and instead place the roads as actual tiles on a layer that sits above the terrain. Lesson: in Godot, **render order follows the scene tree**, not just z-index.

### 4. Input Routing — `_Input` vs `_UnhandledInput`
`_UnhandledInput` only fires if the GUI didn't already consume the event, which is why a `Mouse Filter = Stop` panel correctly blocks map clicks. `_Input` fires for *everything* and is reserved for global actions that should never be swallowed (quick save/load, pause). Choosing the right one is what makes "click the UI vs click the map" behave correctly.

### 5. Separation of Concerns (the RoadBitmask decision)
Terrain bitmasking and road bitmasking *look* similar, but reusing one class for both would have coupled two unrelated systems — a change for roads could silently break the map. Making `RoadBitmask` its own class keeps each system free to evolve independently. This is a recurring theme in good architecture: similar code is not the same as shared responsibility.

---

## Project Analysis

### Where the project is now
This is a genuinely solid **procedural map generator**. The generation pipeline is the strongest part of the codebase:

- **Noise-based terrain** (FastNoiseLite) → land/water.
- **Voronoi partitioning** → provinces with capitals.
- **8-direction bitmask autotiling** → clean terrain edges.
- **Prim's MST** → an optimal connection network between capitals.
- **Road rendering** (this session) → the MST is now visualized as proper tiled roads.

That's a real, working generation stack. The algorithms are non-trivial and they're implemented correctly.

### The honest gap: there is no game *yet*
Your instinct is correct, and the code confirms it. Two things stand out:

- **`TickManager.Update()` is entirely commented out.** The tick loop — the heartbeat that would make resources accumulate over time — does nothing right now but increment a timer.
- **`ResourceManager` assigns resources once and never touches them again.** `UpdateQuantityForCapital()` and `GetResourceQuantity()` are both commented out. Resources exist as data but never produce, accumulate, or get consumed.

So the simulation layer is *scaffolded but inert*. Everything you've built either generates the world or visualizes/interacts with it. Nothing yet creates a reason to keep playing — no production, no scarcity, no decisions with consequences. That's exactly what "99% UI / no loop" means, and it's an accurate self-assessment.

### Loose ends worth cleaning before/after the push
- **`Systems/Map/Roads.cs` is an empty class** — dead code, safe to delete.
- **`.godot/` build artifacts are showing as modified** in git status. Confirm `.godot/` (or at least `.godot/mono/temp/`) is gitignored so you're not committing generated binaries and editor caches.
- **Commented-out blocks** in `TickManager` and `ResourceManager` — fine as a personal scratchpad, but consider whether they belong in a public push or should become real code.
- **Resource RNG edge case:** `RandiRange(0, count)` is inclusive on both ends, and the `== count` branch is what creates empty tiles. It works, but it's fragile — a future reader will likely misread it. A short comment or an explicit `RandiRange(0, count)` → "count means empty" note would help.

### What I'd build next (in priority order)
1. **Turn on the loop.** Uncomment and finish `TickManager` so capitals produce their resource each tick. This single step turns a map viewer into a simulation.
2. **Make resources mean something.** A cost, a cap, or a consumer. Even "capitals need X to grow" creates the first real decision.
3. **Let roads matter mechanically.** Right now roads are visual. Tie them to the trade network (`TradeNetworkVisualizer` already animates packets along the MST) so connectivity affects resource flow.
4. **Player agency.** The "Build Road" toggle is the seed of this — give the player something to *spend* and *choose*, not just paint.

---

## Commentary

You've built the hard half that most people never finish: a correct, layered procedural generation engine. Voronoi, MST, and dual bitmask systems working together is real systems work, not tutorial copy-paste.

But you're right to name the gap plainly — this is currently a **world generator and viewer**, not a game. The UI, camera, popups, and selection all exist to *look at* the world; nothing yet asks the player to *do* anything with consequences. The good news is that the inert pieces (tick loop, resource production, trade flow) are already stubbed in the right places — the architecture is anticipating the loop, it just hasn't been switched on.

The most valuable next move isn't more features — it's connecting three things you already have (ticks → resources → roads/trade) into one cycle that runs and changes over time. The moment that loop closes, everything you've already built suddenly has a purpose.
