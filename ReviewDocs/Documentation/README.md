# Documentation

System-level guides for the Procedural Map Generator / economy-strategy game. Each guide explains a
major system: **what it's for → how it works (with the real types and flow) → how to interact with it
programmatically → gotchas.** These describe the code as it is today; when in doubt the source wins.

> For the build *history* (what changed each chunk and why) see
> [`../Build-History.md`](../Build-History.md). For what's *next* see the milestone roadmap at
> [`../M-Doc/L1-Milestone-Roadmap.md`](../M-Doc/L1-Milestone-Roadmap.md).

## Start here

If you're new to the codebase, read in this order:

1. **[GodotNodeLifecycle.md](GodotNodeLifecycle.md)** — how Godot creates and runs nodes (constructor vs
   `_Ready`, the `_`-callbacks). Read this first if you're used to instancing and owning objects
   yourself — Godot's model is different.
2. **[Architecture.md](Architecture.md)** — how the game boots: `Main → CreateObjs → GameContext`, and
   the Core/Godot split that keeps the game logic testable.
3. **[EventSystem.md](EventSystem.md)** — the three ways parts of the game talk to each other (Godot
   signals, static C# events, the tick call-loop) and when to use each.
4. **[WorldgenPipeline.md](WorldgenPipeline.md)** — how one seed becomes a whole world (terrain →
   provinces → roads → deposits).
5. **[Economy.md](Economy.md)** — the economic model (goods, deposits, buildings, production).
6. **[TickLoop.md](TickLoop.md)** — the heartbeat that drives the simulation.
7. **[SaveLoad.md](SaveLoad.md)** — how a game is saved and restored from a seed plus deltas.

## The one big idea: the Core/Godot split

The codebase is deliberately split in two:

- **`Systems/Core/`** — **plain C#, no Godot `Node`.** The pure game logic: the economy, pathfinding
  (Prim's), Voronoi assignment, the tile/bitmask resolver. It references only the `Vector2I` struct from
  GodotSharp, nothing that needs the engine running. Because it's engine-free it is **unit-tested**
  (`Tests/Tests.csproj`), and that test project compiles *only* `Systems/Core/**` — so if Core ever
  takes a real Godot dependency, the tests stop compiling. That's an intentional tripwire.
- **`Systems/` (everything else)** — the **Godot adapter layer**: scene nodes that render, take input,
  and run the game loop (`Main`, `MapManager`, `Camera`, `Controls`, the overlays, save/load).

Rule of thumb: **logic and rules go in Core; anything that touches the scene tree, input, drawing, or
files goes in the adapter layer.** See [Architecture.md](Architecture.md) for why and how to keep the
line clean.
