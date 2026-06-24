# Save/Load + Controls Hub Review (2026-06-24)

Closeout for the chunk after M1.5: a signal-based input hub, reproduce-from-seed save/load, the
tick-overflow fix, and a camera start-position fix. Plan:
`~/.claude/plans/piped-shimmying-valley.md`.

## Done

### Controls hub (new input layer)
- [x] `Systems/Controls/Controls.cs` — a `Node` that is the single source of truth for action-name
  strings and turns discrete input into **Godot signals** (`SaveRequested`, `LoadRequested`,
  `ProvincesToggleRequested`, `MapClickRequested`). Godot signals (not static C# events) so
  connections auto-drop on the load-time scene reload.
- [x] `Main` creates it first, connects save/load; `MapManager` takes it via ctor and connects
  map-click + provinces-toggle (its old `_UnhandledInput` is gone). `Camera` keeps per-frame pan/zoom
  but reads the shared `Controls.*` constants.

### Save/Load v1 (reproduce-from-seed)
- [x] `Serialization.Load()` now **static** and reads via `Godot.FileAccess` so `user://` resolves
  (was `System.IO`, which can't). Returns null when no save exists.
- [x] Load flow: `Main.PendingLoad` (static, survives `ReloadCurrentScene`) + deferred reload; `_Ready`
  starts worldgen from the saved seed and restores the tick. **F9 load confirmed working by user**
  (loads paused/resumed cleanly).
- [x] `GameData` slimmed — `Capitals` dropped (regenerated from seed; avoids saved-vs-regenerated
  drift). Empty `initializeLoad()` removed.

### Save location
- [x] `project.godot` `use_custom_user_dir` → saves at `%APPDATA%\ProceduralMapGenerator\savegame.json`
  (cross-OS via `user://`, Steam-ready). Verified the file lands there with correct small JSON.

### Tick overflow → long
- [x] `currentTick` / `Economy.tickCount` / `Economy.OnTick()` / `GameData.Tick` are now `long`.

### Camera start-position / pan-delay fix
- [x] `Camera._Ready` now starts **dead-center** on the map, computed from `Main.MapSizeX/Y * TileSize`
  (new `TileSize=64` const) instead of a hardcoded corner. Limits + shader size also derived from map
  size. This removed the startup "pan delay" (the camera was scrolling in from a far-off corner).

### Cleanups + roadmap
- [x] Deleted stray `ProceduralMapGenerator.csproj.old.1` (every `.cs` is in use).
- [x] Added **M8 — Obfuscation** (final) to `ReviewDocs/M-Doc/L1-Milestone-Roadmap.md`: save-file
  encryption (`FileAccess.OpenEncryptedWithPass`, seed kept in a plain header) + binary obfuscation.

## Verification
- `dotnet build` → 0 warnings / 0 errors; `dotnet test Tests/Tests.csproj` → 35 passed.
- Game boots clean (Godot 4.7); `tiles check: OK`; tick runs; no exceptions.
- Save round-trips: file written to the custom dir, deserializes back with matching seed/size; JSON is
  small (no per-tile data, no Capitals).
- F9 load verified by the user (reloads from seed, resumes).

## Notes
- `numProvinces` is currently `10` in `main.tscn` (working-tree tuning; HEAD had 500). Worldgen
  `_Ready` was ~171 ms at np=10. At high province counts the brute-force Voronoi + Prim's MST
  (O(V³)) dominate `_Ready` — relevant if the startup stall returns when scaling provinces up.
- `Camera.shaderMat` is created but never assigned to a node `Material`, so it currently renders
  nothing (pre-existing dead-ish code; left as-is).

## Deferred / backlog
- Persisting hand-drawn (road-build-mode) roads — until player state grows (~M6).
- Save slots / load menu (single `savegame.json` for now).
- Map X/Y entry on a startup menu; move Godot scripts under `Systems/Godot/` — with the main-menu work.
- Voronoi brute-force + Prim's O(V³) perf — only matters at high province counts.

## Next
M2 — production: tile buildings consume inputs → outputs, deplete `Deposit.Reserve`.
