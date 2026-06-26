# Save / Load

**Purpose:** persist and restore a game. The model is **"reproduce from seed, then apply deltas"**: the
save stores the seed plus the few settings and accumulated values needed to rebuild the exact game state
— not the map itself. That keeps saves tiny and is possible only because worldgen is fully deterministic
(see [WorldgenPipeline.md](WorldgenPipeline.md)).

Controls: **F5** saves, **F9** loads (routed through the [Controls signal hub](EventSystem.md)).

---

## What gets saved — `GameData`

[`../../Systems/SaveLoad/GameData.cs`](../../Systems/SaveLoad/GameData.cs) is the **DTO** (not
`GameContext` — that's the live composition root; see [Architecture.md](Architecture.md)):

- **World identity:** `Seed`, `Width`, `Height`, `NumProvinces`, `Frequency`, `NoiseType`. These let
  `_Ready` regenerate the entire world (terrain, provinces, deposits, MST railroads) identically.
- **Clock:** `Tick` (`long`), `tickInterval`, `tickTimer` ([TickLoop.md](TickLoop.md)).
- **Accumulated economy state (deltas):**
  - `DepositReserves` — `List<float>` parallel to `Economy.Deposits` order (deterministic from the seed),
    so reserves restore by index.
  - `NationStocks` — `List<NationStock>` (one per nation; Capital + Treasury + flat Grain/Ore/Tools).
  - `Buildings` — `List<TileBuilding>` (M4.5), every player-built building as a `(Tile, Type)` pair.
    Extractors are no longer seed-derived (no auto-placement), so all buildings — not just workshops —
    must be saved and re-placed.
  - `Railroads` — `List<Vector2I>`, the player's paid-for railroad tiles (also not seed-derived).

**Not saved:** capitals, the tile→capital map, the deposits' layout, and nation **owners** — all
regenerated/recomputed from `Seed` + capital order (saving derived state would risk a drift bug).

### Why the flattened shapes

`System.Text.Json` can't use a `Vector2I` as a **dictionary key**, and Core must stay Godot-free. So:
- nation stocks are a **list** of `NationStock` (Capital as a *value* via the `Vector2IConverter`,
  treasury + goods as flat fields), not a `Dictionary<Vector2I, …>`;
- `NationStock` ([`../../Systems/Core/Economy/NationStock.cs`](../../Systems/Core/Economy/NationStock.cs))
  and `TileBuilding` ([`../../Systems/Core/Economy/TileBuilding.cs`](../../Systems/Core/Economy/TileBuilding.cs))
  live in **Core** so `Economy.ApplySavedState` can take them and still be unit-tested. (`TileBuilding`'s
  `BuildingType` round-trips through JSON as an int; its `Vector2I` via the `Vector2IConverter`.)

---

## Serialization — `Serialization`

[`../../Systems/SaveLoad/Serialization.cs`](../../Systems/SaveLoad/Serialization.cs) — static `Save` /
`Load` using `System.Text.Json` with a custom
[`Vector2IConverter`](../../Systems/SaveLoad/JsonConverters/Vector2IConverter.cs).

- Reads/writes through **`Godot.FileAccess`** (not `System.IO`) so the `user://` virtual path resolves.
- `savePath = "user://savegame.json"`. With `use_custom_user_dir` in `project.godot`, that lands at:
  - Windows: `%APPDATA%\ProceduralMapGenerator\savegame.json`
  - Linux: `~/.local/share/godot/app_userdata/ProceduralMapGenerator/savegame.json`
  - macOS: `~/Library/Application Support/Godot/app_userdata/ProceduralMapGenerator/savegame.json`
- `Load()` returns `null` when there's no save (callers null-check).

The save side, [`../../Systems/SaveLoad/InitSaveAndLoad.cs`](../../Systems/SaveLoad/InitSaveAndLoad.cs),
reads the live systems into a `GameData` (looping `economy.Deposits` for reserves, `economy.Nations` for
treasury + stockpiles, every entry in `economy.TileBuildings` as a `TileBuilding`, and `economy.Railroads`)
and calls `Serialization.Save`.

---

## The load flow (and why it reloads the scene)

Loading rebuilds the world rather than deserializing it, so it re-runs `_Ready` with the saved seed. The
tricky part is carrying the save *across* the scene reload:

```csharp
// Main.OnLoadRequested (F9):
GameData data = Serialization.Load();
if (data != null) {
    PendingLoad = data;                                  // static → survives the reload
    GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);  // deferred → not mid-signal
}
```

- `PendingLoad` is `static`, so it isn't destroyed when the scene is torn down and rebuilt.
- `CallDeferred` waits for a safe point instead of reloading in the middle of handling the input signal.

Then the fresh `Main._Ready()`:
1. sees `PendingLoad != null`, copies `Seed`/size/`NumProvinces`/`Frequency` from it, regenerates the
   whole world from that seed (see [Architecture.md](Architecture.md));
2. restores the clock (`currentTick`, `tickInterval`, `tickTimer`);
3. **restores the economy deltas:**
   `economy.ApplySavedState(loaded.DepositReserves, loaded.NationStocks, loaded.Buildings, loaded.Railroads)`
   (then `Main.DrawMSTLines` re-draws the player's railroad tiles onto the layer).

### `Economy.ApplySavedState`

[`../../Systems/Core/Economy/Economy.cs`](../../Systems/Core/Economy/Economy.cs):

- Reserves restore **by index** (`Deposits[i].Reserve = reserves[i]`), guarded by a count check — if the
  counts differ (e.g. the placement algorithm changed between builds), it skips the restore and logs to
  `Console.Error` (not `GD`, to keep Core Godot-free).
- Each nation's treasury + stockpile restore by **capital lookup** (`Nations.TryGetValue(ns.Capital, …)`).
- Player-built **buildings** (extractors + workshops) are re-placed into `TileBuildings` via
  `Building.Of(b.Type)`; **railroad** tiles are restored into `Railroads`. (The Godot side then respawns
  workshop sprites and re-draws the railroad tiles.)

Worldgen has already rebuilt deposits at *full* reserve and nations with *empty* stockpiles; this
overwrites those with the saved values.

---

## Determinism caveats

- The model assumes **the same build**: reserves restore by deposit index, so a save isn't portable
  across a change to the deposit placement algorithm (the count guard turns a mismatch into a safe skip,
  not a crash).
- `System.Random` isn't guaranteed identical across **.NET runtime versions**, so a seed could
  regenerate a slightly different world on a different runtime. Fine within a shipped build (fixed
  runtime); don't rely on seeds being portable across engine/runtime upgrades.

---

## How to extend

- **Persist new accumulated state** (anything that changes as the game runs and isn't derivable from the
  seed — e.g. a future treasury): add a field to `GameData`, fill it in `InitSaveAndLoad`, and restore it
  in `ApplySavedState` (or alongside it in `Main._Ready`). Keep restore types Core-friendly (no Godot
  dict-key types) and Godot-free.
- **Don't persist derived state** (capitals, deposit layout, province tiles) — regenerate it from the
  seed to avoid drift.
- **Future:** encrypted saves with the seed kept in a plain header (M8 — Obfuscation), and save slots /
  a load menu (currently one `savegame.json`).
