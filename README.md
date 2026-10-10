# Godot 2D Strategy Prototype: procedural map + logistics (Godot 4.7, C# / .NET 8)

A procedural world growing into a logistics/economy game about **bottlenecks**: buildings produce
resources across the map, player-built roads carry them, and cities consume them. The game is about
finding where that flow breaks down. Each run picks a random seed, and the world is generated
deterministically from it.

## What's in the world today

- **Terrain**: two Perlin noise fields. **Height** decides water vs. land plus a thin beach band on
  gentle coasts; **moisture** places inland deserts. The ground is drawn by a shader that blends grass
  and sand textures from a per-cell terrain mask, so biome edges are soft without edge-tile art.
- **Capitals & nations**: a Voronoi diagram over `numNations` seeded capitals. Every land tile belongs
  to its nearest capital, and a per-nation colour map is built from that.
- **Resources**: grain, iron, water and wood deposits scattered on land from the seed, drawn on their
  own tile layer.
- **Buildings**: Farm, Mine, Well and Forestry. Pick one from a city's panel and click a matching
  deposit to place it. Each building produces its resource into its own stockpile every tick.
- **Roads & haulage**: press **R** for road-build mode and click to lay road tiles. Every tick, goods
  move one tile along the road toward the nearest city, like a conveyor belt (routes come from a
  breadth-first search outward from the cities). Each road tile holds a limited amount of cargo, so
  when the tile ahead is full, goods wait, and a jam backs up along the road. That backpressure is the
  core *bottleneck* signal. Buildings load their output onto an adjacent road tile.
- **Cities & population**: every capital is a city. Its population (workers + dependents) grows each
  tick, workers are split into jobs (farmer / miner / craftsman / unemployed) weighted by the deposits
  in the city's territory, and its grain and water demand scales with population. Each tick the city
  consumes what it can from its stockpile.
- **UI**: click a city to open a draggable panel with its demand, population and job breakdown, plus
  the build buttons. Click a building to see its stockpile. Both panels refresh every tick.
- **Tick**: a one-per-second heartbeat. It does no game work itself; it fires a `Ticked` event, and
  `Main.OnTick` runs each system in a fixed order: buildings produce, haulage moves goods, cities
  consume and grow, then the UI refreshes. All wiring lives in `Main.WireTickSubscribers`.

## Controls

| Input | Action |
|-------|--------|
| **WASD** | Pan the camera |
| **Middle-mouse drag** | Pan the camera |
| **Mouse wheel** | Zoom in / out |
| **R** | Toggle road-build mode (starts off) |
| **Left-click** | Road mode on: lay a road tile. A building type armed: place it on a matching deposit. Otherwise: inspect a city or building |
| **Right-click** | Close the building panel and cancel building placement |

> Note: the input map still has **F5**/**F9** (save/load) and **Q** (nation-overlay toggle) from an
> earlier build, but they aren't wired to anything. There is currently no save/load.

## Build & run

Requires **Godot 4.7 (.NET version)** and the **.NET 8 SDK**.


The test project deliberately does **not** use the Godot SDK. It links only the pure sources it needs
and references GodotSharp just for the `Vector2I` struct, so if that logic ever takes a Godot `Node`
dependency, the tests stop compiling. Current tests cover terrain classification and Voronoi
assignment. -> I want to put an emphasis on modular code for easier iteration.

## Project structure

- `Systems/Game/`: the game loop and systems. `Main` (boot + tick wiring), `Tick`, `Roads` +
  `RoadPlacer` + `Haulage`, `Buildings`, `City` + `CitySimulation/` (population, demand),
  `Resources` + `ResourceMap`, `Tiles`, `GlobalConfig` (tunable constants), `Initialize` (terrain).
- `Systems/Map/`: worldgen. Terrain rules/types, Voronoi assignment/diagram, nation colour map, tile ids.
- `Systems/Cameras/`, `Systems/Controls/`, `Systems/Rendering/`: camera, input names, ground shader.
- `UI/`: `UIManager` (click routing) plus the `CityUI` and `BuildingUI` panels.
- `Scenes/`, `tilesets/`, `Images/`, `Shaders/`: Godot assets.
- `Tests/`: xUnit tests for the pure logic.
- `ReviewDocs/`: work logs and review notes.
- `DeprecatedCode/`: code from the original version (see below).

## Deprecated code

`DeprecatedCode/` holds source from the first version of this project, kept for reference. It is
**not part of the build**: the main `.csproj` excludes it and a `.gdignore` file keeps Godot from
importing it. Nothing in the current game uses it.

- `Bitmask.cs`: coastline auto-tiling with an 8-neighbour bitmask mapped onto a 16-tile atlas. Replaced
  by the shader-rendered ground.
- `VoronoiDiagram.cs`, `ColoredProvincesSprite.cs`, `MapManager.cs`, `Border.cs`: the original province
  generation, overlay, click-to-highlight and border experiments. Replaced by `Systems/Map/`.
- `PrimsAlgorithmSimple.cs`, `Roads.cs`: roads auto-generated between capitals along a minimum spanning
  tree (Prim's algorithm). Replaced by player-built roads.
- `SaveLoad/`: a JSON save system using System.Text.Json, with custom converters for `Vector2I` and
  `Vector2I`-keyed dictionaries. It stored the seed, map settings, capitals and tile resources. Loading
  was never finished.
- `Resources/`: the original random per-tile resource assignment the save system serialized.

## License

Proprietary. All rights reserved. See [LICENSE](LICENSE).
