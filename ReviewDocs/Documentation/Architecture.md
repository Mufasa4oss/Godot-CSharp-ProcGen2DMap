# Architecture & Bootstrap

**Purpose:** how the game assembles itself when it starts, who owns what, and how the engine-free game
logic stays separated from the Godot rendering/input layer.

---

## The Core/Godot split

Every file lives on one side of a hard line:

- **`Systems/Core/`** — plain C# with **no Godot `Node` dependency**. The economy
  (`Economy`/`Nation`/`Deposit`/`Building`/`Good`/`NationStock`), pathfinding
  (`PrimsAlgorithmSimple`), Voronoi assignment (`VoronoiAssignment`), and the tile/bitmask resolver
  (`TileId`/`TerrainTiles`/`RailroadTiles`/`TileResolver`), plus `MyGlobalClass` helpers. It references only
  the `Vector2I` struct from GodotSharp — nothing that needs the engine to be running.
- **`Systems/` (everything else)** — the **Godot adapter layer**: `Main`, `MapManager`, `Camera`,
  `Controls`, `VoronoiDiagram`, `Bitmask`, the overlays, save/load, UI. These subclass `Node`/`Node2D`
  and do rendering, input, file IO, and scene wiring.

**Why:** Core is unit-tested by `Tests/Tests.csproj`, which compiles **only** `Systems/Core/**` (plus the
GodotSharp managed assembly for `Vector2I`). If Core ever takes a real engine dependency — a `Node`, a
`GD.Print`, a `FileAccess` — the test project stops compiling. That broken build is an intentional
tripwire that keeps the rules layer honest and fast to test. (This is why, for example,
`Economy.ApplySavedState` logs to `System.Console.Error` instead of `GD.PrintErr`: keeping Core
Godot-free.)

**Where to put new code:** rules, math, data transforms → Core (and add a test). Anything that draws,
takes input, sits in a scene, emits signals, or reads files → the adapter layer, calling into Core.

---

## Bootstrap: `Main._Ready()`

[`../../Systems/Game/Main.cs`](../../Systems/Game/Main.cs) is the entry point. Godot instantiates it from
`Scenes/main.tscn` and calls `_Ready()`, which runs the whole startup in order:

1. **Grab the noise** from the exported `NoiseTexture2D`.
2. **Create the input hub first:** `controls = new Controls(); AddChild(controls);` then connect Save/Load
   signals — created first so later collaborators (`MapManager`) can be handed the same instance. (See
   [EventSystem.md](EventSystem.md).)
3. **Decide the seed — new game vs load** (see next section). Set the noise seed.
4. **Size the noise texture** to the map (`Main.MapSizeX/Y`).
5. **`gameContext = CreateObjs();`** — builds the world and all systems (below).
6. **Restore tick + economy state** if this is a load (sets `currentTick`, calls
   `economy.ApplySavedState(...)`).
7. **Railroads & overlays:** popup, build the MST (`BuildMst()`), draw railroad tiles, add the
   `TradeNetworkVisualizer`, the `DepositOverlay`, and the `GainFeedback` "+N" node.

### `CreateObjs()` — the composition step

This is where the systems get wired together, in dependency order:

```
Initialize(tml, noise, …).PlaceTiles()  → land tiles painted from noise
Bitmask(...).OrientCells()              → terrain autotiling
VoronoiDiagram(... worldSeed)           → provinces (capitals + tile→capital map)
Economy(vD.provincesDictionary,         → the economic graph (provinces, deposits, buildings)
        vD.GetCapitals(), worldSeed)
TickManager(economy)                    → the clock that will drive economy.OnTick()
MapManager(vD, tml, …, economy, controls) → input + click-inspect + highlight; AddChild'd
MapTextureCreated?.Invoke(provinceTexture) → hands the province texture to ColoredProvincesSprite
return new GameContext(... all of the above ...)
```

It also prints sanity checks (`tiles check: OK`, province/deposit counts) — cheap invariants that catch a
broken worldgen immediately.

---

## New game vs load

The seed decision in `_Ready()` is the fork between a fresh world and a restored one:

```csharp
GameData loaded = PendingLoad;   // static, set by the load handler before the scene reload
PendingLoad = null;
if (loaded != null) {
    MapSizeX = loaded.Width; MapSizeY = loaded.Height;
    numProvinces = loaded.NumProvinces;
    worldSeed = loaded.Seed;                 // ← regenerate the SAME world from the saved seed
    noiseTexture.Set("frequency", loaded.Frequency);
} else {
    worldSeed = new Random().Next();          // ← fresh random world
}
```

Loading a game does **not** deserialize the map. It re-runs worldgen from the saved seed, which
reproduces terrain, provinces, deposits, and railroads identically, then overlays the small saved deltas
(tick, deposit reserves, stockpiles). This is why saves are tiny. The mechanics of carrying the save
across the scene reload (`PendingLoad` static + `CallDeferred(ReloadCurrentScene)`) are covered in
[SaveLoad.md](SaveLoad.md) and the reload note in [GodotNodeLifecycle.md](GodotNodeLifecycle.md).

---

## `GameContext` — the composition root

[`../../Systems/Game/GameContext.cs`](../../Systems/Game/GameContext.cs) is a plain aggregate that holds
references to everything `CreateObjs()` built (`bitmask`, `voronoiDiagram`, `economy`, `tickManager`,
`mapManager`, the texture, noise, map size, the tilemap layer). `Main` keeps one and reaches into it
(`gameContext.tickManager.Update(...)`, `gameContext.economy`).

**Important distinction:** `GameContext` is the **composition root**, *not* the save format. It is never
serialized. The save DTO is `GameData` ([SaveLoad.md](SaveLoad.md)). Don't conflate them — `GameContext`
is "all the live systems," `GameData` is "the few values needed to reproduce them."

### The god-object caution

`GameContext` is convenient but trends toward a god-object, and passing the whole bag invites
Law-of-Demeter reach-throughs like `gameContext.voronoiDiagram.placedCords.Count`. Guidance for new code
(from the codebase review, now in [`../Build-History.md`](../Build-History.md)):

- **Inject the specific collaborators a class needs**, not the whole `GameContext`. `MapManager`'s
  constructor is the model — it takes exactly `(vD, tml, nht, numProvince, tickManager, economy,
  controls)`, the things it uses.
- **Expose intent methods on the owner** instead of reaching through it (Tell-Don't-Ask). `MapManager`
  already does this by asking `economy.Nations[capital].Tiles` rather than re-scanning all tiles.

---

## File map

| Area | Files |
|------|-------|
| Bootstrap / composition | `Systems/Game/Main.cs`, `Systems/Game/GameContext.cs`, `Systems/Game/Initialize.cs` |
| Input | `Systems/Controls/Controls.cs`, `Systems/Cameras/Camera.cs` |
| Worldgen | `Systems/Map/VoronoiDiagram.cs`, `Systems/Map/Bitmask.cs`, `Systems/Map/RailroadBitmask.cs`, `Systems/Core/Map/*`, `Systems/Core/Pathfinding/PrimsAlgorithmSimple.cs` |
| Economy (Core) | `Systems/Core/Economy/*` |
| Clock | `Systems/GameTime/TickManager.cs` |
| Save/load | `Systems/SaveLoad/*` |
| Rendering / overlays | `Systems/Map/ColoredProvincesSprite.cs`, `Systems/Map/DepositOverlay.cs`, `Systems/ResourcePopup/*` |

See the per-system guides for each.
