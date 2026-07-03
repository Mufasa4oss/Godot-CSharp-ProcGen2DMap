# File & Dependency Map — Map Viewer (post-teardown)

_Last updated 2026-07-03. All gameplay (economy, finance, pops, logistics, roads, save/load,
build UI) was removed. What remains is a **map viewer**: procedural terrain + the blended
ground shader + one capital per nation + the nation-colour overlay, and a blank UI panel to
rebuild onto._

## Boot (`Systems/Game/Main.cs`)

`Scenes/main.tscn` root → `Main.cs`. `Main._Ready()`:
1. Seed the height noise (`Nht`), set map size.
2. `GetNode("0zTileMapLayer")`.
3. `GenerateWorld()` — the whole map (below).
4. `PopupSceneLoad()` — 5s splash, then fade.

No `_Process`, no ticks, no save/load, no input hub instance.

## Map-generation pipeline (the part that must stay working)

```
FastNoiseLite (height, from Nht)  +  FastNoiseLite (moisture, seed+7919)
        │
        ▼
Initialize.PlaceTiles()            → paints terrain tiles on 0zTileMapLayer, builds TerrainMask
   ├─ TerrainRules.Classify()      → Water / Sand(beach|desert) / Grass  (pure, unit-tested)
   ├─ TerrainType                  → the enum those return
   └─ TerrainTiles (+ TileId)      → tile source ids used by SetCell
        │
        ▼
GroundRenderer.Initialize(mask)    → the "great-looking" blended ground (terrain_ground.gdshader)
        │
        ▼
VoronoiDiagram(...)                → places Capital tiles + builds the coloured nation texture
   └─ VoronoiAssignment.AssignNearest()  → tile→nearest-capital (pure, unit-tested)
        │
        ▼
Main.MapTextureCreated?.Invoke(tex) → ColoredNationsSprite (the "Nations" overlay, hidden; Q had toggled it)
```

## Files that remain, and their role

| File | Role | Depends on |
|---|---|---|
| `Systems/Game/Main.cs` | boot + worldgen orchestration | Initialize, GroundRenderer, VoronoiDiagram |
| `Systems/Game/Initialize.cs` | terrain tiles + mask | TerrainRules, TerrainType, TerrainTiles |
| `Systems/Map/TerrainRules.cs`, `TerrainType.cs` | terrain classification (pure) | — (tested) |
| `Systems/Map/TerrainTiles.cs`, `TileId.cs` | tileset source-id catalog | — |
| `Systems/Map/VoronoiAssignment.cs` | nearest-capital (pure) | — (tested) |
| `Systems/Map/VoronoiDiagram.cs` | capitals + colour texture | TerrainTiles, VoronoiAssignment |
| `Systems/Map/ColoredNationsSprite.cs` | nation-colour overlay (Nations node) | `Main.MapTextureCreated`, `tr.gdshader` |
| `Systems/Map/RegionSelector.cs` | **left-click → highlight a Voronoi region** on BordersLayer | `VoronoiDiagram.nationsDictionary`, `BorderHighlight.tres` source 2 |
| `Systems/Rendering/GroundRenderer.cs` | blended ground shader | `terrain_ground.gdshader`, grass/sand PNGs |
| `Systems/Cameras/Camera.cs` | pan/zoom | `Controls.*` action-name constants, `Main.MapSizeX/Y` |
| `Systems/Controls/Controls.cs` | input action-name constants | — (its signals now have no subscribers) |
| `UI/UIManager.cs` | **blank** Control (empty) | — |
| `Systems/ResourcePopup/ResourcePopup.cs` | startup splash | `ResourcePopup.tscn` |

## Scene tree (`main.tscn`)

```
Main → 0zTileMapLayer (+ BordersLayer, + RailroadLayer)   ← RailroadLayer/BordersLayer now unused, empty
     → Camera2D (Camera.cs)
     → Nations (ColoredNationsSprite.cs)
     → UIManager (UIManager.cs, blank) → PanelContainer → VBox   ← empty panel (HUD children deleted)
```

## `GetNode` / `ResourceLoader` landmines (green build, runtime crash)

- `Main.cs`: `"0zTileMapLayer"`, `res://Scenes/ResourcePopup.tscn`.
- `Camera.cs`: `res://Shaders/tr.gdshader`.
- `ColoredNationsSprite.cs`: `res://Shaders/tr.gdshader`.
- `GroundRenderer.cs`: `res://Shaders/terrain_ground.gdshader`, `res://Images/grassSeamless.png`, `sandSeamless.png`.

## Removable next, if you want to trim further

- `Systems/Rendering/GroundBlend.cs` + `Scenes/GroundBlendTest.tscn` — a standalone shader-test scene, not in `main.tscn`.
- `Systems/ResourcePopup/` — the splash (cosmetic).
- The empty `RailroadLayer` / `BordersLayer` scene nodes (+ their `road.tres` / `BorderHighlight.tres` tilesets) — left over from roads/highlight.
- `Systems/Controls/Controls.cs` signals + `_UnhandledInput` — only the string constants are still used (by Camera); the Save/Load/MapClick/NationsToggle signals are dead.
- The `Nations` overlay + `ColoredNationsSprite` + `VoronoiAssignment` — only needed if you want the coloured nation regions; capital *placement* itself is `VoronoiDiagram.GeneratePoints`.
