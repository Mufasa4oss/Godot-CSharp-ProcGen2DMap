# Procedural Map Generator (Godot 4.7, C# / .NET)

A procedural world that doubles as the foundation for an economy-strategy game. Each world is
generated deterministically from a single seed:

- **Terrain** from Perlin noise (water / land), auto-tiled via a bitmask resolver.
- **Provinces** via a Voronoi diagram over randomly placed capitals.
- **Roads** along the minimum spanning tree (Prim's) connecting the capitals, auto-tiled into
  rails/bridges/junctions.
- **Economy** of tile-bound resource **deposits** (ore / grain) with extractor buildings, all placed
  deterministically from the world seed.

Because everything derives from one seed, the whole world reproduces exactly — which is what makes
**save/load** tiny (it stores the seed + a few settings, not the map).

## Controls

| Input | Action |
|-------|--------|
| **WASD** | Pan the camera |
| **Mouse wheel** | Zoom in / out |
| **Q** | Toggle the province (Voronoi) map mode |
| **Left-click** | Inspect + highlight the clicked province (or lay track in road-build mode) |
| **F5** | Save the game |
| **F9** | Load the game (regenerates the world from the saved seed and resumes the tick) |

## Save location

Saves to Godot's per-OS user data dir under a custom folder:

- **Windows:** `%APPDATA%\ProceduralMapGenerator\savegame.json`
- **Linux:** `~/.local/share/godot/app_userdata/ProceduralMapGenerator/savegame.json`
- **macOS:** `~/Library/Application Support/Godot/app_userdata/ProceduralMapGenerator/savegame.json`

## Build & run

Requires **Godot 4.7 (.NET / Mono build)** and the **.NET 8 SDK**.

```sh
dotnet build ProceduralMapGenerator.csproj   # compile the C#
```

Then open the project in the Godot editor and press play, or launch the Godot binary with
`--path .`. (Running the Godot binary does not auto-rebuild C# — run `dotnet build` after code
changes.)

## Tests

Pure game logic lives in `Systems/Core/` (no Godot `Node` dependencies) and is unit-tested with
xUnit in a plain .NET project:

```sh
dotnet test Tests/Tests.csproj
```

The test project compiles only the `Systems/Core/` sources, so it also acts as a tripwire: if Core
ever takes a Godot-engine dependency, the tests stop compiling.

## Project structure

- `Systems/Core/` — **plain C#, no Godot `Node`** (engine-free, testable): economy, pathfinding
  (Prim's), the tile/bitmask/Voronoi logic. References only the `Vector2I` struct.
- `Systems/` (everything else) — the **Godot adapter layer**: scene nodes for rendering, input, and
  the game loop (`Main`, `MapManager`, `Camera`, `Controls`, save/load, …).
- `Scenes/`, `tilesets/`, `Images/`, `Shaders/` — Godot assets.
- `ReviewDocs/` — per-chunk review checklists; `ReviewDocs/M-Doc/` holds the milestone roadmap.

## License

Proprietary — all rights reserved. See [LICENSE](LICENSE).
