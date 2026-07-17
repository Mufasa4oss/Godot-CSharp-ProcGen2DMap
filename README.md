# Godot 2D Strategy Prototype — procedural map + logistics (Godot 4.7, C# / .NET)

A procedural world growing into a logistics/economy game about **bottlenecks**: resources are
produced across the map, roads carry them, and cities consume them — and the game is finding where
the flow breaks down. The whole world is generated deterministically from a single seed.

## What's in the world today

- **Terrain** — Perlin noise (water / land, with beaches and deserts), auto-tiled via a bitmask
  resolver and drawn with a blended ground shader.
- **Capitals & nations** — a Voronoi diagram over `numNations` randomly-placed capitals, plus a
  per-nation colour map.
- **Cities** — every capital is a **city consumer** that demands all four resources each tick
  (infinite demand for now) and **grows its population** each tick. Click a city to open a live
  panel showing its per-resource demand, total population, and its worker job breakdown; the panel
  refreshes every tick.
- **Population** — each city has a population of workers + dependents. Workers are split into jobs
  (farmer / miner / craftsman / unemployed) weighted by the resource deposits the city owns, and the
  total grows a little every tick.
- **Resources** — grain / iron / water / wood, scattered on land and drawn as coloured dots
  (grain yellow, iron gray, water blue, wood brown).
- **Roads** — **player-built**: toggle build mode and left-click to lay rail tiles. A per-tick
  system flags any road carrying more vehicles than its capacity — the core *bottleneck* signal.
- **Tick** — a one-per-second heartbeat. It does no game work itself: it fires a `Ticked` event and
  each system (road-bottleneck check, city consumption, population growth, UI refresh) subscribes to
  it. All subscriptions live in one place, `Main.WireTickSubscribers`.

## Controls

| Input | Action |
|-------|--------|
| **WASD** | Pan the camera |
| **Mouse wheel** | Zoom in / out |
| **Middle-mouse drag** | Pan the camera |
| **R** | Toggle road-build mode (starts off) |
| **Left-click** | Build mode **on** → lay a road tile · Build mode **off** → select a city to see its demand |

> Note: input hooks for save/load (**F5**/**F9**) and a nation-overlay toggle (**Q**) still exist in
> the input map from an earlier build, but they aren't wired to any behaviour yet. There is currently
> no save/load.

## Build & run

Requires **Godot 4.7 (.NET / Mono build)** and the **.NET 8 SDK**.

```sh
dotnet build ProceduralMapGenerator.csproj   # compile the C#
```

Then open the project in the Godot editor and press play, or launch the Godot binary with
`--path .`. (Running Godot does not auto-rebuild C# — run `dotnet build` after code changes.)

## Tests

Pure, engine-free game logic is unit-tested with xUnit in a plain .NET project:

```sh
dotnet test Tests/Tests.csproj
```

The test project deliberately does **not** use the Godot SDK — it links only the pure sources under
test (the `Systems/Map` terrain/Voronoi logic and `Systems/Game/Roads.cs`), referencing GodotSharp
just for the `Vector2I` struct. That makes it a tripwire: if any of that logic takes a Godot `Node`
dependency, the tests stop compiling.

## Project structure

- `Systems/Game/` — the game loop and systems: `Main` (boot + tick wiring), `Tick` (heartbeat that
  fires the `Ticked` event), `Roads` + `RoadPlacer`, `City` + `Population`, `Resources` +
  `ResourceMap`, `Initialize` (terrain placement).
- `Systems/Map/` — worldgen and tiles: terrain rules/types, Voronoi assignment/diagram, tile ids.
- `Systems/Cameras/`, `Systems/Controls/`, `Systems/Rendering/` — camera, input mapping, ground shader.
- `UI/` — `UIManager`, the on-screen panel.
- `Scenes/`, `tilesets/`, `Images/`, `Shaders/` — Godot assets.
- `Tests/` — xUnit tests for the pure logic (`Roads`, terrain rules, Voronoi).
- `ReviewDocs/` — work logs (e.g. `worklog-2026-07-16.md`).

## License

Proprietary — all rights reserved. See [LICENSE](LICENSE).
